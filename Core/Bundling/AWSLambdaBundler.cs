using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Emission;
using Sempiler.Languages;
using Sempiler.Inlining;
using Sempiler.Core;

namespace Sempiler.Bundling
{
    using static BundlerHelpers;

    public class AWSLambdaBundler : IBundler
    {
        private class AWSLambdaBundler_FunctionsTypeScriptEmitter : TypeScriptEmitter
        {
            public override string RelativeComponentOutFilePath(Session session, Artifact artifact, Shard shard, Component node)
            {
                return UserCodeRelDirPath + '/' + node.ID + FileExtension;
            }
        }

        public static List<Dependency> RequiredDependencies = new List<Dependency>{
            new Dependency { Name = "aws-serverless-express", PackageManager = PackageManager.NPM }
        };

        static readonly string[] DiagnosticTags = new string[] { "bundler", ArtifactTargetPlatform.AWSLambda };

        const string UserCodeDirName = "src";
        const string CompiledUserCodeDirName = "lib";

        static readonly string UserCodeRelDirPath = UserCodeDirName;

        const string UserCodeSymbolicLexeme = "userCode";

        const string EventHandlerFunctionSymbolicLexeme = "handleEvent";
        const string EventHandlerAnnotationSymbolicLexeme = "event";
        const string S3EventSourceName = "aws:s3";
        const string S3RecordPredicateExp = "!!r.s3";
        static readonly Dictionary<string, string> RecordBasedEvents = new Dictionary<string, string>
        {
            { S3EventSourceName, S3RecordPredicateExp }
        };

        const string ExpressAppSymbolicLexeme = "$expressApp";
        const string LambdaFilenameWithoutExt = "index";
        // const string AppFilenameWithoutExt = "app";

        public IList<string> GetPreservedDebugEmissionRelPaths(Session session, Artifact artifact, CancellationToken token) => new string[] { "node_modules" };

        public async Task<Result<OutFileCollection>> Bundle(Session session, Artifact artifact, List<Shard> shards, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            if (artifact.Role != ArtifactRole.Server)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                );
            }

            if (artifact.TargetLang != ArtifactTargetLang.TypeScript)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                );
            }

            if(HasErrors(result) || token.IsCancellationRequested) return result;


            var ofc = new OutFileCollection();


            foreach (var shard in shards)
            {
                if (shard.Role == ShardRole.MainApp)
                {
                    var functionsOFC = result.AddMessages(
                        EmitMainAppShard(session, artifact, shard, token)
                    );

                    if(functionsOFC != null)
                    {
                        ofc.AddAll(functionsOFC);
                    }
                }
                else
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"Unsupported shard role '{shard.Role.ToString()}' in bundler for artifact role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                    );
                }
            }

            result.Value = ofc;

            return result;
        }

        private Result<OutFileCollection> EmitMainAppShard(Session session, Artifact artifact, Shard shard, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();
            var ofc = new OutFileCollection();

            // [dho] DEPENDENCIES - 25/02/20
            {
                // [dho] make sure all the dependencies are added to the shard - 25/02/20
                foreach(var dependency in NodeJSPartialBundler.RequiredDependencies)
                {
                    result.AddMessages(
                        DependencyHelpers.AddIfNotPresent(ref shard.Dependencies, dependency)
                    );
                }
                foreach(var dependency in AWSLambdaBundler.RequiredDependencies)
                {
                    result.AddMessages(
                        DependencyHelpers.AddIfNotPresent(ref shard.Dependencies, dependency)
                    );
                }
            }

            var nodeOutputFiles = result.AddMessages(
                new NodeJSPartialBundler().EmitMainAppShard(session, artifact, shard, token)
            );

            if(nodeOutputFiles?.Count > 0)
            {
                ofc.AddAll(nodeOutputFiles);
                // foreach(var emittedFile in nodeOutputFiles)
                // {
                //     ofc[FileSystem.ParseFileLocation($"{UserCodeRelDirPath}/{emittedFile.Path}")] = emittedFile.Emission;
                // }
            }

            result.AddMessages(
                EmitLambdaEntrypoint(session, artifact, shard, ofc, token)
            );
            
            var relResourcePaths = result.AddMessages(
                AddResourceFiles(session, artifact, shard, ofc, $"res/")
            );

            // DEPENDENCIES - 25/02/20
            {
                // [dho] make sure all the dependencies for firebase functions are added to the shard - 25/02/20
                foreach(var dependency in AWSLambdaBundler.RequiredDependencies)
                {
                    result.AddMessages(
                        DependencyHelpers.AddIfNotPresent(ref shard.Dependencies, dependency)
                    );
                }
                    
                var packageJSONCode = result.AddMessages(
                    NodeJS.PackageJSONHelpers.EmitManifestCode(shard.AST, artifact.Name, "1.0.0", $"{LambdaFilenameWithoutExt}.js", shard.Dependencies, token)
                );

                if(!string.IsNullOrEmpty(packageJSONCode))
                {
                    var packageJSONRelFilePath = "/package.json";

                    // [dho] create the file containing the manifest code - 25/02/20
                    var didAddPackageJSON = AddRawFileIfNotPresent(ofc, packageJSONRelFilePath, packageJSONCode);

                    // [dho] guard to ensure we did add the manifest to the output successfully - 25/02/20
                    if(!didAddPackageJSON)
                    {
                        result.AddMessages(
                            new Message(
                                MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                                $"Could not create package.json manifest because file called '{packageJSONRelFilePath}' already exists in output"
                            )
                        );
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(HasErrors(result), 
                        "Expected result to have errors if package.json manifest code is null or empty, but did not find any errors");
                }
            }

            // TSCONFIG - 25/02/20
            {  
                var tsConfigCode = result.AddMessages(
                    NodeJS.TSConfigHelpers.EmitConfigCode(shard.AST, new string[] { UserCodeDirName }, CompiledUserCodeDirName, token)
                );

                if(!string.IsNullOrEmpty(tsConfigCode))
                {
                    var tsConfigRelFilePath = "/tsconfig.json";

                    // [dho] create the file containing the manifest code - 25/02/20
                    var didAddTSConfig = AddRawFileIfNotPresent(ofc, tsConfigRelFilePath, tsConfigCode);

                    // [dho] guard to ensure we did add the manifest to the output successfully - 25/02/20
                    if(!didAddTSConfig)
                    {
                        result.AddMessages(
                            new Message(
                                MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                                $"Could not create tsconfig.json because file called '{tsConfigRelFilePath}' already exists in output"
                            )
                        );
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(HasErrors(result), 
                        "Expected result to have errors if tsconfig.json code is null or empty, but did not find any errors");
                }
            }

            result.Value = ofc;

            return result;
        }

        private static Result<object> EmitLambdaEntrypoint(Session session, Artifact artifact, Shard shard, OutFileCollection ofc, CancellationToken token)
        {
            var result = new Result<object>();

            {
                var lambdaJSContent = new System.Text.StringBuilder();
                lambdaJSContent.AppendLine($"const awsServerlessExpress = require('aws-serverless-express')");
                // lambdaJSContent.AppendLine("const awsServerlessExpressMiddleware = require('aws-serverless-express/middleware')");
                lambdaJSContent.AppendLine($"const {ExpressAppSymbolicLexeme} = require('./{CompiledUserCodeDirName}/{NodeJSPartialBundler.IndexFilenameWithoutExt}').default;");
                // lambdaJSContent.AppendLine($"{ExpressAppSymbolicLexeme}.use(awsServerlessExpressMiddleware.eventContext())");
                
                // [dho] OTHER EXPORTED SYMBOLS - 23/03/20
                {                
                    var exportedSymbolsCode = result.AddMessages(EmitExportedSymbolsCode(session, artifact, shard, token));

                    if(!string.IsNullOrEmpty(exportedSymbolsCode))
                    {
                        lambdaJSContent.AppendLine(exportedSymbolsCode);
                    }
                }

                lambdaJSContent.AppendLine($"const server = awsServerlessExpress.createServer({ExpressAppSymbolicLexeme})");
   
                
                lambdaJSContent.AppendLine($"const setup = {NodeJSPartialBundler.UserCodeSymbolicLexeme}.default || (() => {{}});");
                                    
                lambdaJSContent.AppendLine(
$@"exports.handler = (event, context) => {{
    // [dho] DISABLED because for some reason it causes the serverless
    // proxy to then break... TODO FIX - 17/04/20
    // await setup({{ event, context }});

    if('httpMethod' in event)
    {{
        return awsServerlessExpress.proxy(server, event, context).promise;
    }}
    else
    {{
        return new Promise((resolve, reject) => {EventHandlerFunctionSymbolicLexeme}(event, context).then(resolve, reject));
    }}
}}");           
                var relPath = "/" + LambdaFilenameWithoutExt + ".js";
                var didAddEntrypoint = (
                    AddRawFileIfNotPresent(ofc, relPath, lambdaJSContent.ToString())
                );

                // [dho] guard to ensure we did add the router file to the output successfully - 25/02/20
                if(!didAddEntrypoint)
                {
                    result.AddMessages(
                        new Message(
                            MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                            $"Could not create Lambda entrypoint because file called '{relPath}' already exists in output"
                        )
                    );
                }
            }

            return result;
        }
        
        private static Result<string> EmitExportedSymbolsCode(Session session, Artifact artifact, Shard shard, CancellationToken token)
        {
            var result = new Result<string>();
            var ast = shard.AST;
            var sb = new System.Text.StringBuilder();

            var entrypointComponent = default(Component);
            var exportedSymbols = new List<ServerInlining.ServerExportedSymbolInfo>();

            var eventHandlers = new Dictionary<string, Dictionary<string, List<ServerInlining.ServerExportedSymbolInfo>>>();


            foreach(var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, ASTHelpers.GetRoot(ast).ID))
            {
                System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

                var component = ASTNodeFactory.Component(ast, (DataNode<string>)child);

                if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
                {
                    var r = ServerInlining.GetInlinerInfo(
                        session, ast, component, LanguageSemantics.TypeScript, new string[] {}, new string[] {}, token
                    );

                    var inlinerInfo = result.AddMessages(r);

                    if(HasErrors(r)) continue;
                    else if(token.IsCancellationRequested) return result;

                    entrypointComponent = component;
                    exportedSymbols = inlinerInfo.ExportedSymbols;
                    break;
                }
            }

            if(entrypointComponent is null)
            {
                System.Diagnostics.Debug.Assert(exportedSymbols.Count == 0, 
                    "Did not expect to find exported symbols without an entrypoint component"
                );
            }

            foreach (var exportedSymbol in exportedSymbols)
            {
                var symbol = exportedSymbol.Symbol;

                var events = result.AddMessages(ParseEvents(ast, exportedSymbol));

                if(events != null)
                {
                    if(events.Count == 0)
                    {
                        result.AddMessages(
                            new NodeMessage(MessageKind.Warning, $"Exported symbol does not declare any events and so will not be auto wired", symbol)
                            {
                                Hint = GetHint(symbol.Origin),
                            }
                        );
                    }
                    
                    foreach(var e in events)
                    {
                        if(!eventHandlers.ContainsKey(e.Source))
                        {
                            eventHandlers[e.Source] = new Dictionary<string, List<ServerInlining.ServerExportedSymbolInfo>>();
                        }

                        var names = eventHandlers[e.Source];

                        if(!names.ContainsKey(e.Name))
                        {
                            names[e.Name] = new List<ServerInlining.ServerExportedSymbolInfo>();
                        }

                        names[e.Name].Add(exportedSymbol);
                    }
                }
            }

            if(eventHandlers.Count > 0)
            {
                var userCodeImportPath = new NodeJSPartialBundler.TypeScriptEmitter("./" + CompiledUserCodeDirName).RelativeComponentOutFilePath(session, artifact, shard, entrypointComponent);
                sb.AppendLine("const " + UserCodeSymbolicLexeme + " = require('./" + CompiledUserCodeDirName + System.IO.Path.DirectorySeparatorChar +  System.IO.Path.GetFileNameWithoutExtension(userCodeImportPath) + "');");

                sb.AppendLine(
                $@"async function {EventHandlerFunctionSymbolicLexeme}(event, context)
                {{
                    {result.AddMessages(EmitEventHandlerRouterCode(eventHandlers))}    
                }}");
            }

            result.Value = sb.ToString();

            return result;
        }

        struct AWSEvent 
        {
            public string Source;
            public string Name;
        }

        private static Result<List<AWSEvent>> ParseEvents(RawAST ast, ServerInlining.ServerExportedSymbolInfo exportedSymbol)
        {
            var result = new Result<List<AWSEvent>>();
            var events = new List<AWSEvent>();

            if(exportedSymbol.SourceDeclaration.Kind == SemanticKind.ExportDeclaration)
            {
                var exportDecl = ASTNodeFactory.ExportDeclaration(ast, exportedSymbol.SourceDeclaration);

                foreach(var a in exportDecl.Annotations)
                {
                    var annotation = ASTNodeFactory.Annotation(ast, a);
                    var expression = annotation.Expression;

                    if(expression.Kind == SemanticKind.Invocation)
                    {
                        var inv = ASTNodeFactory.Invocation(ast, expression);

                        if(ASTNodeHelpers.IsIdentifierWithName(ast, inv.Subject, EventHandlerAnnotationSymbolicLexeme))
                        {
                            var args = inv.Arguments;

                            if(args.Length == 2)
                            {
                                string source = null;
                                string name = null;

                                var arg0 = ASTNodeHelpers.UnwrapInvocationArgument(ast, args[0]);
                                var arg1 = ASTNodeHelpers.UnwrapInvocationArgument(ast, args[1]);


                                if(arg0.Kind == SemanticKind.StringConstant)
                                {
                                    source = ASTNodeFactory.StringConstant(ast, (DataNode<string>)arg0).Value;
                                }
                                else
                                {
                                    result.AddMessages(
                                        new NodeMessage(MessageKind.Error, $"Expected argument to be string constant", arg0)
                                        {
                                            Hint = GetHint(arg0.Origin),
                                        }
                                    );
                                }

                                if(arg1.Kind == SemanticKind.StringConstant)
                                {
                                    name = ASTNodeFactory.StringConstant(ast, (DataNode<string>)arg1).Value;
                                }
                                else
                                {
                                    result.AddMessages(
                                        new NodeMessage(MessageKind.Error, $"Expected argument to be string constant", arg1)
                                        {
                                            Hint = GetHint(arg1.Origin),
                                        }
                                    );
                                }

                                events.Add(new AWSEvent {
                                    Source = source,
                                    Name = name
                                });
                            }
                            else
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Error, $"{EventHandlerAnnotationSymbolicLexeme} annotation expects 2 arguments, but found {args.Length}", annotation)
                                    {
                                        Hint = GetHint(annotation.Origin),
                                    }
                                );
                            }
                        }   
                        else
                        {
                            result.AddMessages(
                                new NodeMessage(MessageKind.Warning, $"Unsupported annotation", annotation)
                                {
                                    Hint = GetHint(annotation.Origin),
                                }
                            );
                        }
                    }
                    else
                    {
                        result.AddMessages(
                            new NodeMessage(MessageKind.Warning, $"Unsupported annotation", annotation)
                            {
                                Hint = GetHint(annotation.Origin),
                            }
                        );
                    }
                }
            }

            if(!HasErrors(result))
            {
                result.Value = events;
            }

            return result;
        }

        private static Result<string> EmitEventHandlerRouterCode(Dictionary<string, Dictionary<string, List<ServerInlining.ServerExportedSymbolInfo>>> eventHandlers)
        {
            var result = new Result<string>();

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("if(event.Records){");
            {
                sb.AppendLine($"const unhandledRecords = event.Records.filter(r => !({string.Join("||", RecordBasedEvents.Values)}));");
                sb.AppendLine("if(unhandledRecords.length){");
                {
                    sb.AppendLine($"console.error(`{EventHandlerFunctionSymbolicLexeme} : Unhandled event records`, JSON.stringify(unhandledRecords, null, 4));");
                    sb.AppendLine($"throw new Error(`{EventHandlerFunctionSymbolicLexeme} : No handler configured for some records`)");
                }
                sb.AppendLine("}");

                foreach(var sourceKV in eventHandlers)
                {
                    var sourceName = sourceKV.Key;

                    if(sourceName == S3EventSourceName)
                    {
                        sb.AppendLine($"for(const {{ eventName }} of event.Records.filter(r => {S3RecordPredicateExp})){{");
                        {
                            sb.AppendLine("switch(eventName){");
                            foreach(var eventKV in sourceKV.Value)
                            {
                                var eventName = eventKV.Key;

                                sb.AppendLine($"case '{eventName}':{{");
                                {
                                    foreach(var handler in eventKV.Value)
                                    {
                                        sb.AppendLine($"await {UserCodeSymbolicLexeme}.{string.Join(".",handler.QualifiedHandlerName)}(event, context);");
                                    }
                                }
                                sb.AppendLine("break; }");
                            }

                            sb.AppendLine($"default: throw new Error(`{EventHandlerFunctionSymbolicLexeme} : No handler configured for '${{eventName}}' event`);");

                            sb.AppendLine("}");
                        }
                        sb.AppendLine("}");
                    }
                    else
                    {
                        foreach(var eventNames in eventHandlers[sourceName])
                        {
                            foreach(var handler in eventNames.Value)
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Warning, $"Unsupported source and event combination", handler.SourceDeclaration)
                                    {
                                        Hint = GetHint(handler.SourceDeclaration.Origin),
                                    }
                                );
                            }
                        }
                    }
                }
                // [dho] NOTE this return stops us hitting the throw statement below
                // that signifies an unhandled event - 26/03/20
                sb.AppendLine("return;");
            }
            sb.AppendLine("}");

            sb.AppendLine($"throw new Error(`{EventHandlerFunctionSymbolicLexeme} : No handler found`);");

            result.Value = sb.ToString();

            return result;
        }
    }

}