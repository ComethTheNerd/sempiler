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

    public class FirebaseFunctionsBundler : IBundler
    {
        private class FirebaseFunctionsBundler_FunctionsTypeScriptEmitter : TypeScriptEmitter
        {
            public override string RelativeComponentOutFilePath(Session session, Artifact artifact, Shard shard, Component node)
            {
                return UserCodeRelDirPath + '/' + node.ID + FileExtension;
            }
        }

        private class FirebaseFunctionsBundler_StaticSiteEmitter : BaseEmitter
        {
            string Name;
            public FirebaseFunctionsBundler_StaticSiteEmitter(string name)
            {
                Name = name;
            }

            public override string RelativeComponentOutFilePath(Session session, Artifact artifact, Shard shard, Component node)
            {
                // xxx;
                /*
                TODO : 
                    - shard entrypoint file
                    - make all components relative to shard entrypoint file
                    - filter empty components should remove component if just contains empty code constants...
                        but really, why are directives being replaced with empty code constants in first place
                
                
                */

                var relPath = node.Name;
                var index = relPath.LastIndexOf(Name + System.IO.Path.DirectorySeparatorChar);
          
                if(index > -1)
                {
                    relPath = relPath.Substring(index + (Name + System.IO.Path.DirectorySeparatorChar).Length);
                }

                return StaticSiteDirName + '/' + relPath;
            }
        }


        public static List<Dependency> RequiredDependencies = new List<Dependency>{
            // new Dependency { Name = "typescript", Version = "^3.2.4", PackageManager = PackageManager.NPM },
            // new Dependency { Name = "@types/node", PackageManager = PackageManager.NPM },
            new Dependency { Name = "firebase-functions", PackageManager = PackageManager.NPM }
        };

        // private class FirebaseFunctionsBundler_TypeScriptEmitter : TypeScriptEmitter
        // {
        //     public override string RelativeComponentOutFilePath(Session session, Artifact artifact, Shard shard, Component node)
        //     {
        //         return UserCodeRelDirPath + '/' + node.ID + FileExtension;
        //     }
        // }

  


        static readonly string[] DiagnosticTags = new string[] { "bundler", ArtifactTargetPlatform.FirebaseFunctions };

        const string StaticSiteDirName = "public";
        const string FunctionsCodeDirName = "functions";
        const string UserCodeDirName = "src";
        static readonly string UserCodeRelDirPath = FunctionsCodeDirName + '/' + UserCodeDirName;

        // static readonly string AppFileName = $"{UserCodeDirName}/src/index";

        // [dho] NOTE for some reason firebase functions doesn't work properly if the name
        // we use here is long and complex (like a component ID).. but "api" seems to work fine...
        // It won't clash with any user code symbol called `api` either, because we use `exports.api = ...` - 24/09/19
        const string APISymbolicLexeme = "api";
        const string UserCodeSymbolicLexeme = "userCode";

        const string EntrypointFilenameWithoutExt = "index";



        public IList<string> GetPreservedDebugEmissionRelPaths(Session session, Artifact artifact, CancellationToken token) => new string[] { $"{FunctionsCodeDirName}/node_modules" };

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


            var hasHosting = false;

            foreach (var shard in shards)
            {
                if (shard.Role == ShardRole.MainApp)
                {
                    var functionsOFC = result.AddMessages(
                        EmitFunctionsShard(session, artifact, shard, token)
                    );

                    if(functionsOFC != null)
                    {
                        ofc.AddAll(functionsOFC);
                    }
                }
                else if (shard.Role == ShardRole.StaticSite)
                {
                    var staticSiteOFC = result.AddMessages(
                        EmitStaticSiteShard(session, artifact, shard, token)
                    );

                    if(staticSiteOFC != null)
                    {
                        hasHosting = !hasHosting && staticSiteOFC.Count > 0;

                        ofc.AddAll(staticSiteOFC);
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


            if(!hasHosting)
            {
                /* [dho] TODO investigate! deploying without a public folder causes an error - 06/02/20 */
                AddRawFileIfNotPresent(ofc, StaticSiteDirName + "/a.txt", "");
            }

            // [dho] synthesize any requisite files for the target platform - 01/06/19
            {
                AddRawFileIfNotPresent(ofc, "firebase.json",
$@"{{
  ""hosting"": {{
    {/* [dho] deploying without a public folder causes an error - 06/02/20 */""}
    ""public"": ""{StaticSiteDirName}"",
    ""rewrites"": [
      {{
        ""source"": ""**"",
        ""function"": ""{APISymbolicLexeme}""
      }}
    ],
    {/* [dho] means you do not have to append `.html` in the URL for it to find the page - 15/02/20 */""}
    ""cleanUrls"": true
  }},
  ""functions"": {{
    ""predeploy"": ""npm --prefix functions run build""
  }}
}}");

            }

            result.Value = ofc;

            return result;
        }

        private Result<OutFileCollection> EmitFunctionsShard(Session session, Artifact artifact, Shard shard, CancellationToken token)
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
                foreach(var dependency in FirebaseFunctionsBundler.RequiredDependencies)
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
                foreach(var emittedFile in nodeOutputFiles)
                {
                    ofc[FileSystem.ParseFileLocation($"{FunctionsCodeDirName}/{emittedFile.Path}")] = emittedFile.Emission;
                }
            }

            result.AddMessages(
                EmitFirebaseFunctionsEntrypoint(session, artifact, shard, ofc, token)
            );
            
            var relResourcePaths = result.AddMessages(
                AddResourceFiles(session, artifact, shard, ofc, $"{FunctionsCodeDirName}/res/")
            );

            // DEPENDENCIES - 25/02/20
            {
                // [dho] make sure all the dependencies for firebase functions are added to the shard - 25/02/20
                foreach(var dependency in FirebaseFunctionsBundler.RequiredDependencies)
                {
                    result.AddMessages(
                        DependencyHelpers.AddIfNotPresent(ref shard.Dependencies, dependency)
                    );
                }
                    
                var packageJSONCode = result.AddMessages(
                    NodeJS.PackageJSONHelpers.EmitManifestCode(shard.AST, artifact.Name, "1.0.0", $"lib/{EntrypointFilenameWithoutExt}.js", shard.Dependencies, token)
                );

                if(!string.IsNullOrEmpty(packageJSONCode))
                {
                    var packageJSONRelFilePath = FunctionsCodeDirName + "/package.json";

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
                    NodeJS.TSConfigHelpers.EmitConfigCode(shard.AST, new string[] { UserCodeDirName }, "lib", token)
                );

                if(!string.IsNullOrEmpty(tsConfigCode))
                {
                    var tsConfigRelFilePath = FunctionsCodeDirName + "/tsconfig.json";

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

        private Result<OutFileCollection> EmitStaticSiteShard(Session session, Artifact artifact, Shard shard, CancellationToken token)
        {
            var ast = shard.AST;

            FilterNonEmptyComponents(ast);

            // [dho] by convention we take the name of the entrypoint file to be the name of the 
            // directory that contains the static site source files - 10/02/20
            var inferredSrcDirName = System.IO.Path.GetFileNameWithoutExtension(shard.AbsoluteEntryPointPath);
            
            var emitter = new FirebaseFunctionsBundler_StaticSiteEmitter(inferredSrcDirName);

            return CompilerHelpers.Emit(emitter, session, artifact, shard, shard.AST, token);
        }

        
        private static Result<object> EmitFirebaseFunctionsEntrypoint(Session session, Artifact artifact, Shard shard, OutFileCollection ofc, CancellationToken token)
        {
            var result = new Result<object>();

            var entrypointContent = new System.Text.StringBuilder();
            entrypointContent.AppendLine($"import $expressApp from './{NodeJSPartialBundler.IndexFilenameWithoutExt}'");
            entrypointContent.AppendLine($"const {APISymbolicLexeme} = require('firebase-functions').https.onRequest($expressApp)");
            entrypointContent.AppendLine($"export {{ {APISymbolicLexeme} }};");

            // [dho] OTHER EXPORTED SYMBOLS (eg. Firebase function hooks) - 25/02/20
            {                
                var exportedSymbolsCode = result.AddMessages(EmitExportedSymbolsCode(session, artifact, shard, token));

                if(!string.IsNullOrEmpty(exportedSymbolsCode))
                {
                    entrypointContent.AppendLine(exportedSymbolsCode);
                }
            }

            var entrypointRelFilePath = UserCodeRelDirPath + "/" + EntrypointFilenameWithoutExt + ".ts";

            var didAddEntrypoint = AddRawFileIfNotPresent(ofc, entrypointRelFilePath, entrypointContent.ToString());

            // [dho] guard to ensure we did add the router file to the output successfully - 25/02/20
            if(!didAddEntrypoint)
            {
                result.AddMessages(
                    new Message(
                        MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                        $"Could not create Firebase functions entrypoint because file called '{entrypointRelFilePath}' already exists in output"
                    )
                );
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

            if(entrypointComponent != null)
            {
                sb.AppendLine("import * as " + UserCodeSymbolicLexeme + " from './" + entrypointComponent.ID + "';");
            }
            else
            {
                System.Diagnostics.Debug.Assert(exportedSymbols.Count == 0, 
                    "Did not expect to find exported symbols without an entrypoint component"
                );
            }

            foreach (var exportedSymbol in exportedSymbols)
            {
                System.Diagnostics.Debug.Assert(exportedSymbol.Symbol.Kind == SemanticKind.DataValueDeclaration);

                var name = ASTNodeFactory.DataValueDeclaration(ast, exportedSymbol.Symbol).Name;

                System.Diagnostics.Debug.Assert(name.Kind == SemanticKind.Identifier);

                var nameLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;

                if (nameLexeme == APISymbolicLexeme)
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"The use of exported symbol name '{APISymbolicLexeme}' is reserved", name)
                        {
                            Hint = GetHint(name.Origin),
                        }
                    );
                    continue;
                }

                sb.AppendLine("export const " + nameLexeme + " = " + UserCodeSymbolicLexeme + "." + nameLexeme + ";");
            }

            result.Value = sb.ToString();

            return result;
        }
    }

}