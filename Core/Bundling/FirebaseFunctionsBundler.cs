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
        private class FirebaseFunctionsBundler_TypeScriptEmitter : TypeScriptEmitter
        {
            public override string RelativeComponentOutFilePath(Session session, Artifact artifact, Shard shard, Component node)
            {
                return UserCodeRelDirPath + '/' + node.ID + FileExtension;
            }
        }

        private readonly FirebaseFunctionsBundler_TypeScriptEmitter Emitter = new FirebaseFunctionsBundler_TypeScriptEmitter();



        static readonly string[] DiagnosticTags = new string[] { "bundler", "firebase/functions" };

        const string FunctionsCodeDirName = "functions";
        const string UserCodeDirName = "src";
        const string IsProductionSymbolicLexeme = "isProduction";

        static readonly string UserCodeRelDirPath = FunctionsCodeDirName + '/' + UserCodeDirName;

        // static readonly string AppFileName = $"{UserCodeDirName}/src/index";

        // [dho] NOTE for some reason firebase functions doesn't work properly if the name
        // we use here is long and complex (like a component ID).. but "api" seems to work fine...
        // It won't clash with any user code symbol called `api` either, because we use `exports.api = ...` - 24/09/19
        const string APISymbolicLexeme = "api";
        const string UserCodeSymbolicLexeme = "userCode";

        const string UserParserFunctionNameLexeme = "parseUserFromFirebaseIDToken";
        const string RouteParamsSourceNameLexeme = "params";

        // [dho] adapted from : https://github.com/firebase/functions-samples/blob/master/authorized-https-endpoint/functions/index.js#L26 - 04/10/19
        static readonly string UserParserFunctionImplementation = $@"
// Express middleware that validates Firebase ID Tokens passed in the Authorization HTTP header.
// The Firebase ID token needs to be passed as a Bearer token in the Authorization HTTP header like this:
// `Authorization: Bearer <Firebase ID Token>`.
// when decoded successfully, the ID Token content will be added as `req.user`.
async function {UserParserFunctionNameLexeme}(req : any) : Promise<{{ uid : string }}>
{{

  if ((!req.headers.authorization || !req.headers.authorization.startsWith('Bearer ')) &&
      !(req.cookies && req.cookies.__session)) 
  {{
    return Promise.resolve(null);
  }}

  let idToken;
  if (req.headers.authorization && req.headers.authorization.startsWith('Bearer ')) 
  {{

    // Read the ID Token from the Authorization header.
    idToken = req.headers.authorization.split('Bearer ')[1];

  }} 
  else if(req.cookies) 
  {{

    // Read the ID Token from cookie.
    idToken = req.cookies.__session;

  }} 
  else 
  {{

    // No cookie
    return Promise.resolve(null);

  }}

  try {{
    const decodedIDToken = await require(""firebase-admin"").auth().verifyIdToken(idToken);
    
    return Promise.resolve(decodedIDToken);

  }} 
  catch (error) 
  {{
    return Promise.resolve(null);
  }}
}};";

        public IList<string> GetPreservedDebugEmissionRelPaths() => new string[] { $"{FunctionsCodeDirName}/node_modules" };

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

            Shard shard = default(Shard);

            if(shards.Count == 1)
            {
                shard = shards[0];

                if(shard.Role != ShardRole.MainApp)
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"Firebase functions bundler expected 1 shard to be '{ShardRole.MainApp}' but found '{shard.Role}'")
                    );
                }
            }
            else
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"Firebase functions bundler expected 1 shard but found {shards.Count}")
                );
            }
            

            if(HasErrors(result) || token.IsCancellationRequested) return result;


            // var inlined = default(Component);
            var ofc = default(OutFileCollection);//new OutFileCollection();

            // [dho] emit source files - 21/05/19
            {
                // var emitter = default(IEmitter);

                if (artifact.TargetLang == ArtifactTargetLang.TypeScript)
                {
                    // inlined = result.AddMessages(TypeScriptInlining(session, artifact, shard.AST, token));

                    // if (HasErrors(result) || token.IsCancellationRequested) return result;

                    // emitter = new TypeScriptEmitter();

                    var ast = shard.AST;

                    FilterNonEmptyComponents(ast);

                    var entrypointComponent = default(Component);
                    var routeInfos = default(List<ServerInlining.ServerRouteInfo>);
                    var exportedSymbols = default(List<ServerInlining.ServerExportedSymbolInfo>);

                    var nodeIDsToRemove = new List<string>();


                    foreach(var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, ASTHelpers.GetRoot(ast).ID))
                    {
                        System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

                        var component = ASTNodeFactory.Component(ast, (DataNode<string>)child);

                        var r = ServerInlining.GetInlinerInfo(
                            session, ast, component, LanguageSemantics.TypeScript, new string[] {}, new string[] {}, token
                        );

                        var inlinerInfo = result.AddMessages(r);

                        if(HasErrors(r)) continue;
                        else if(token.IsCancellationRequested) return result;


                        if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
                        {
                            entrypointComponent = component;
                            routeInfos = inlinerInfo.RouteInfos;
                            exportedSymbols = inlinerInfo.ExportedSymbols;
                        }




                        foreach(var im in inlinerInfo.Imports)
                        {
                            var imDecl = ASTNodeFactory.ImportDeclaration(ast, im);

                            var r2 = ImportHelpers.ParseImportDescriptor(imDecl, token);

                            var imDescriptor = result.AddMessages(r2);

                            if(HasErrors(r)) continue;
                            else if(token.IsCancellationRequested) return result;

                            switch(imDescriptor.Type)
                            {
                                case ImportHelpers.ImportType.Component:
                                {
                                    var importedComponent = result.AddMessages(
                                        ImportHelpers.ResolveComponentImport(session, artifact, ast, imDescriptor, component, token)
                                    );

                                    if(importedComponent != null)
                                    {
                                        var newSpecifierLexeme = "./" + importedComponent.ID;

                                        ASTHelpers.Replace(ast, imDecl.Specifier.ID, new [] { 
                                            NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), newSpecifierLexeme).Node
                                        });
                                    }
                                    else
                                    {
                                        result.AddMessages(
                                            new NodeMessage(MessageKind.Error,
                                                $"Could not resolve Component import '{imDescriptor.SpecifierLexeme}'", im)
                                        );
                                    }
                                }
                                break;

                                case ImportHelpers.ImportType.Compiler:{
                                    nodeIDsToRemove.Add(im.ID);
                                }
                                break;


                                case ImportHelpers.ImportType.Platform:
                                case ImportHelpers.ImportType.Unknown:
                                break;

                                default:{
                                    System.Diagnostics.Debug.Fail(
                                        $"Unhandled import type in Firebase Functions Bundler '{imDescriptor.Type}'"
                                    );
                                }
                                break;
                            }                
                        } 


                    }


                    System.Diagnostics.Debug.Assert(entrypointComponent != null);



                    if (HasErrors(result) || token.IsCancellationRequested) return result;


                    if(nodeIDsToRemove.Count > 0)
                    {
                        ASTHelpers.DisableNodes(ast, nodeIDsToRemove.ToArray());
                    }


                    ofc = result.AddMessages(CompilerHelpers.Emit(Emitter, session, artifact, shard, shard.AST, token));


                    if (HasErrors(result) || token.IsCancellationRequested) return result;



                    var indexContent = new System.Text.StringBuilder();

                
                    indexContent.AppendLine("import * as " + UserCodeSymbolicLexeme + " from './" + entrypointComponent.ID + "';");

                    indexContent.AppendLine($"const {IsProductionSymbolicLexeme} = process.env.ENV === 'production';");

                    result.AddMessages(AppendRouterCode(session, artifact, ast, routeInfos, indexContent, token));

                    result.AddMessages(AppendExportedSymbolsCode(session, artifact, ast, exportedSymbols, indexContent, token));


                    AddRawFileIfMissing(ofc, UserCodeRelDirPath + "/index.ts", indexContent.ToString());

                }
                // [dho] TODO JavaScript! - 01/06/19
                else
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                    );
                }

                if (HasErrors(result) || token.IsCancellationRequested) return result;
            }

            // [dho] synthesize any requisite files for the target platform - 01/06/19
            {
                AddRawFileIfMissing(ofc, "firebase.json",
$@"{{
  ""hosting"": {{
    ""rewrites"": [
      {{
        ""source"": ""**"",
        ""function"": ""{APISymbolicLexeme}""
      }}
    ]
  }},
  ""functions"": {{
    ""predeploy"": ""npm --prefix functions run build""
  }}
}}");
                var dependenciesContent = new System.Text.StringBuilder();
                var dependencies = shard.Dependencies;

                if (dependencies.Count > 0)
                {
                    dependenciesContent.Append(",");
                    dependenciesContent.AppendLine();

                    for (int i = 0; i < dependencies.Count; ++i)
                    {
                        var dependency = dependencies[i];
                        var name = dependency.Name;
                        var version = dependency.Version ?? "*";

                        dependenciesContent.Append($@"""{name}"": ""{version}""");

                        if (i < dependencies.Count - 1)
                        {
                            dependenciesContent.Append(",");
                        }

                        dependenciesContent.AppendLine();
                    }
                }

                AddRawFileIfMissing(ofc, $"{FunctionsCodeDirName}/package.json",
$@"{{
  ""name"": ""{artifact.Name}"",
  ""private"": true,
  ""version"": ""1.0.0"",
  ""engines"": {{
    ""node"": ""8""
  }},
  ""scripts"": {{
    ""build"": ""./node_modules/.bin/tsc""
  }},
  ""devDependencies"": {{
    ""typescript"": ""^3.2.4"",
    ""@types/node"": ""*""
  }},
  ""dependencies"": {{
    ""firebase-functions"": ""*""{dependenciesContent.ToString()}
  }},
  ""main"": ""lib/index.js""
}}");

                // [dho] adapted from https://raw.githubusercontent.com/firebase/functions-samples/master/typescript-getting-started/functions/tsconfig.json - 24/09/19
                AddRawFileIfMissing(ofc, $"{FunctionsCodeDirName}/tsconfig.json",
                // [dho] https://stackoverflow.com/a/52384384/300037 - 26/12/19
                // [dho] https://stackoverflow.com/a/57653497/300037 - 26/12/19
                // [dho] NOTE `esModuleInterop` so we can hoist the imports to top level and bind them to a variable, because we
                // cannot do the imports asynchronously inside an IIFE in the `module.exports` because Firebase doesn't like this - 26/12/19
$@"{{
  ""compilerOptions"": {{
    ""lib"": [""es2017""],
    ""module"": ""commonjs"",
    ""noImplicitReturns"": true,
    ""outDir"": ""lib"",
    ""sourceMap"": true,
    ""target"": ""es2017"",
    ""skipLibCheck"": true,
    ""esModuleInterop"" : true
  }},
  ""include"": [
    ""{UserCodeDirName}""
  ]
}}");

                result.Value = ofc;
            }


            return result;
        }

        // private static Result<Component> TypeScriptInlining(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        // {
        //     var result = new Result<Component>();

        //     var root = ASTHelpers.GetRoot(ast);

        //     System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

        //     var domain = ASTNodeFactory.Domain(ast, root);

        //     var newComponentNodes = new List<Node>();

        //     // [dho] the component (eg. file) that contains the entrypoint view for the application - 31/08/19
        //     var entrypointComponent = default(Component);


        //     // [dho] a component containing all the inlined constituent components for the compilation,
        //     // thereby creating a single larger file of all components in one file - 31/08/19
        //     var inlined = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Bundling), AppFileName);


        //     // var importDecls = new List<Node>();
        //     // [dho] these statements live outside of the `node1234` object type declaration
        //     // wrapper that we emit around inlined components - 31/08/19
        //     // var globalStatements = new List<Node>();
        //     {
        //         var inlinedContent = new List<Node>();
        //         var componentIDsToRemove = new List<string>();
        //         var hoistedImports = new List<Node>();
        //         var routeInfos = new List<ServerInlining.ServerRouteInfo>();
        //         var exportedSymbolInfos = new List<ServerInlining.ServerExportedSymbolInfo>();

        //         foreach (var cNode in domain.Components)
        //         {
        //             var component = ASTNodeFactory.Component(ast, (DataNode<string>)cNode);

        //             // [dho] every component in the AST (ie. every input file) will be turned into an IIFE data value declaration and inlined - 22/09/19
        //             var r = ConvertToInlinedIIFEDataValueDeclaration(session, artifact, ast, component, token, ref hoistedImports, ref routeInfos, ref exportedSymbolInfos);

        //             result.AddMessages(r);

        //             if (HasErrors(r))
        //             {
        //                 continue;
        //             }
        //             else if (token.IsCancellationRequested)
        //             {
        //                 return result;
        //             }
        //             // System.Console.WriteLine("COMPONENT NAME :  " + component.Name);
        //             // [dho] is this component the entrypoint for the whole artifact - 28/06/19
        //             if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
        //             {
        //                 entrypointComponent = component;
        //                 // entrypointView = r.Value;
        //             }
        //             // // [dho] any code that was outside an artifact root is just emitted without a class wrapper, so we have a way
        //             // // in the input sources of declaring global symbols, or things like protocols which cannot be nested inside other
        //             // // declarations in Swift - 18/07/19
        //             // else if (BundlerHelpers.IsOutsideArtifactInferredSourceDir(session, component))
        //             // {
        //             //     globalStatements.AddRange(r.Value.Members);
        //             // }


        //             inlinedContent.Add(r.Value.Node);

        //             componentIDsToRemove.Add(component.ID);
        //         }

        //         if (entrypointComponent == null)
        //         {
        //             result.AddMessages(
        //                 new Message(MessageKind.Error, $"Could not create Firebase functions bundle because an entrypoint component was not found {artifact.Name} (expected '{BundlerHelpers.GetNameOfExpectedArtifactEntrypointComponent(session, artifact)}' to exist)")
        //             );

        //             return result;
        //         }


        //         // if (globalStatements.Count > 0)
        //         // {
        //         //     ASTHelpers.Connect(ast, inlined.ID, globalStatements.ToArray(), SemanticRole.None);
        //         // }

        //         var (routerDVDecl, routerInvocation) = result.AddMessages(
        //             CreateRouter(session, artifact, ast, inlined, token, ref routeInfos)
        //         );

        //         inlinedContent.Add(routerDVDecl.Node);


        //         var exported = result.AddMessages(
        //             CreateExportedSymbols(session, artifact, ast, inlined, token, ref exportedSymbolInfos)
        //         );

        //         if (HasErrors(result) || token.IsCancellationRequested) return result;

        //         // [dho] add the router handle to the returned symbols - 24/12/19
        //         exported.Add(
        //             ASTNodeHelpers.CreateAssignment(ast, 
        //                 NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "exports." + APISymbolicLexeme).Node,
        //                 NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), routerInvocation.ID).Node
        //             ).Node
        //         );


        //         ASTHelpers.Connect(ast, inlined.ID, hoistedImports.ToArray(), SemanticRole.None);

        //         // [dho] inline all the existing components as iife declarations - 22/09/19
        //         ASTHelpers.Connect(ast, inlined.ID, inlinedContent.ToArray(), SemanticRole.None);

        //         ASTHelpers.Connect(ast, inlined.ID, exported.ToArray(), SemanticRole.None);

        //         // [dho] remove the components from the tree because now they have all been inlined - 22/09/19
        //         ASTHelpers.DisableNodes(ast, componentIDsToRemove.ToArray());

        //         result.Value = inlined;
        //     }

        //     newComponentNodes.Add(inlined.Node);

        //     ASTHelpers.Connect(ast, domain.ID, newComponentNodes.ToArray(), SemanticRole.Component);

        //     return result;
        // }

//         private static Result<(DataValueDeclaration, Invocation)> CreateRouter(Session session, Artifact artifact, RawAST ast, Component entrypointComponent, CancellationToken token, ref List<ServerInlining.ServerRouteInfo> routes)
//         {
//             var result = new Result<(DataValueDeclaration, Invocation)>();

//             var content = new List<Node>();

//             var invocation = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
//             {
//                 var subject = ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, new PhaseNodeOrigin(PhaseKind.Bundling), new[] {
//                         CreateRequireInvocation(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "firebase-functions"),
//                         NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "https").Node,
//                         NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "onRequest").Node,
//                     });

//                 ASTHelpers.Connect(ast, invocation.ID, new[] { subject }, SemanticRole.Subject);


//                 {
//                     content.Add(
//                         NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
// $@"const express = require(""express"");
// const cors = require(""cors"");
// const app = express();
// {UserParserFunctionImplementation}
// app.use(cors({{ origin : true }}));").Node
//                     );

//                     foreach (var route in routes)
//                     {
//                         var routeHandler = result.AddMessages(
//                             CreateRouteHandler(session, artifact, ast, route, token)
//                         );

//                         if (routeHandler != null)
//                         {
//                             content.Add(routeHandler.Node);
//                         }
//                     }

//                     content.Add(
//                         NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "return app;").Node
//                     );
//                 }

//                 var (iife, _) = ASTNodeHelpers.CreateIIFE(ast, content);

//                 ASTHelpers.Connect(ast, invocation.ID, new[] { iife.Node }, SemanticRole.Argument);
//             }


//             var dataValueDecl = CreateConstantDataValueDeclaration(
//                 ast,
//                 new PhaseNodeOrigin(PhaseKind.Bundling),
//                 NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), invocation.ID).Node,
//                 invocation.Node
//             );


//             result.Value = (dataValueDecl, invocation);


//             // var exportDecl = NodeFactory.ExportDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
//             // {
//             //     var name = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), $"{entrypointComponent.ID}{ExportSuffix}");

//             //     var clause = CreateConstantDataValueDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling), name.Node, invocation.Node);

//             //     ASTHelpers.Connect(ast, exportDecl.ID, new [] { clause.Node }, SemanticRole.Clause);
//             // }

//             // result.Value = exportDecl.Node;

//             return result;
//         }

        


        // private static Result<List<Node>> CreateExportedSymbols(Session session, Artifact artifact, RawAST ast, Component entrypointComponent, CancellationToken token, ref List<ServerInlining.ServerExportedSymbolInfo> exportedSymbols)
        // {
        //     var result = new Result<List<Node>>();

        //     var assignments = new List<Node>();

        //     foreach (var exportedSymbol in exportedSymbols)
        //     {
        //         System.Diagnostics.Debug.Assert(exportedSymbol.Symbol.Kind == SemanticKind.DataValueDeclaration);

        //         // var initializer = exportedDVDecl.Initializer;


        //         // if(initializer == null)
        //         // {
        //         //     result.AddMessages(
        //         //         new NodeMessage(MessageKind.Error, $"Export must have initializer", exportedDVDecl)
        //         //         {
        //         //             Hint = GetHint(exportedDVDecl.Origin),
        //         //         }
        //         //     );
        //         //     continue;
        //         // }

        //         var name = ASTNodeFactory.DataValueDeclaration(ast, exportedSymbol.Symbol).Name;

        //         System.Diagnostics.Debug.Assert(name.Kind == SemanticKind.Identifier);

        //         var nameLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;

        //         if (nameLexeme == APISymbolicLexeme)
        //         {
        //             result.AddMessages(
        //                 new NodeMessage(MessageKind.Error, $"The use of exported symbol name '{APISymbolicLexeme}' is reserved", name)
        //                 {
        //                     Hint = GetHint(name.Origin),
        //                 }
        //             );
        //             continue;
        //         }

        //         assignments.Add(ASTNodeHelpers.CreateAssignment(ast,
        //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "exports." + nameLexeme).Node,
        //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), string.Join(".", exportedSymbol.QualifiedHandlerName)).Node
        //         ).Node);
        //     }

        //     result.Value = assignments;


        //     return result;
        // }

        // private static Result<DataValueDeclaration> ConvertToInlinedIIFEDataValueDeclaration(Session session, Artifact artifact, RawAST ast, Component component, CancellationToken token, ref List<Node> hoistedImports, ref List<ServerInlining.ServerRouteInfo> routes, ref List<ServerInlining.ServerExportedSymbolInfo> exportedSymbols)
        // {
        //     var result = new Result<DataValueDeclaration>();
        //     var exportedMembers = new List<Node>();
        //     var content = new List<Node>();

        //     var apiRelPath = new string[] { };
        //     var qualifiedName = new string[] { component.ID };

        //     var inlinerInfo = result.AddMessages(
        //         ServerInlining.GetInlinerInfo(session, ast, component, LanguageSemantics.TypeScript, apiRelPath, qualifiedName, token)
        //     );

        //     if (HasErrors(result) || token.IsCancellationRequested) return result;

        //     result.AddMessages(
        //         ProcessImports(session, artifact, ast, component, inlinerInfo.Imports, hoistedImports, token)
        //     );

        //     if (HasErrors(result) || token.IsCancellationRequested) return result;

        //     // [dho] TODO FIX the server inlining code currently treats exports from any file to be a 'route', but I think
        //     // we only want exports from the artifact entrypoint to be considered 'routes'.. everything else is just an export
        //     // for sharing symbols between files - 22/09/19
        //     if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
        //     {
        //         routes.AddRange(inlinerInfo.RouteInfos);
        //         exportedSymbols.AddRange(inlinerInfo.ExportedSymbols);
        //     }
        //     // [dho] for now we are just going to complain about 'routes' in the session entrypoint file.. because otherwise we would
        //     // struggle to route those symbols if they clash with symbols in the artifact entrypoint for route names - 22/09/19
        //     else if (IsOutsideArtifactInferredSourceDir(session, component))
        //     {
        //         if (BundlerHelpers.IsInferredSessionEntrypointComponent(session, component))
        //         {
        //             foreach (var routeInfo in inlinerInfo.RouteInfos)
        //             {
        //                 result.AddMessages(
        //                     new NodeMessage(MessageKind.Error, "Cannot export from session entrypoint file", routeInfo.Handler)
        //                     {
        //                         Hint = GetHint(routeInfo.Handler.Origin),
        //                     }
        //                 );
        //             }
        //         }
        //     }


        //     foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, component))
        //     {
        //         if (child.Kind == SemanticKind.ExportDeclaration)
        //         {
        //             var exportDecl = ASTNodeFactory.ExportDeclaration(ast, child);
        //             var specifier = exportDecl.Specifier;
        //             var clauses = exportDecl.Clauses;

        //             // [dho] for now we do not support exporting symbols defined in other files,
        //             // so we insist that the specifier is not set - 27/12/19
        //             if (specifier != default(Node))
        //             {
        //                 result.AddMessages(
        //                     CreateUnsupportedFeatureResult(specifier, "Exporting symbols defined in other files")
        //                 );
        //                 continue;
        //             }

        //             /*
        //                 export { x, y } from "hello-world"
                        
        //                 return { x : node12345.x, y : node1234.y, }

        //                 export * from "hello-world"

        //                 return { ...node1234 }
                    
        //                 export const 
        //              */

        //             foreach (var clause in clauses)
        //             {
        //                 if (clause.Kind == SemanticKind.DataValueDeclaration || LanguageSemantics.TypeScript.IsFunctionLikeDeclarationStatement(ast, clause))
        //                 {
        //                     var exportedSymbolName = ASTHelpers.GetSingleLiveMatch(ast, clause.ID, SemanticRole.Name);

        //                     if (exportedSymbolName != null)
        //                     {
        //                         System.Diagnostics.Debug.Assert(exportedSymbolName.Kind == SemanticKind.Identifier);

        //                         // [dho] make sure the symbol is exported from the IIFE we are creating for the component - 22/09/19
        //                         exportedMembers.Add(
        //                             NodeFactory.Identifier(
        //                                 ast,
        //                                 clause.Origin,
        //                                 ASTNodeFactory.Identifier(ast, (DataNode<string>)exportedSymbolName).Lexeme
        //                             ).Node
        //                         );

        //                         // [dho] we just want to keep the thing that was being exported, not the actual exporting 
        //                         // (which would not be legal inside the IIFE we are creating) - 22/09/19
        //                         content.Add(clause);
        //                     }
        //                     else
        //                     {
        //                         // [dho] wriing up `export default` etc. - 22/09/19
        //                         result.AddMessages(
        //                             CreateUnsupportedFeatureResult(specifier, "Exporting anonymous or default declarations")
        //                         );
        //                         continue;
        //                     }
        //                 }
        //                 else
        //                 {
        //                     // [dho] TODO implement other reasonable cases - 22/09/19
        //                     result.AddMessages(
        //                         CreateUnsupportedFeatureResult(clause, $"Exporting '{clause.Kind}'")
        //                     );
        //                     continue;
        //                 }
        //             }
        //         }
        //         else
        //         {
        //             // [dho] we expect all imports to have been rewritten as constant require statements - 22/09/19
        //             System.Diagnostics.Debug.Assert(child.Kind != SemanticKind.ImportDeclaration);

        //             content.Add(child);
        //         }
        //     }




            
        
        //     var iifeReturnStatement = NodeFactory.FunctionTermination(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
        //     {
        //         var iifeReturnValue = NodeFactory.DynamicTypeConstruction(ast, new PhaseNodeOrigin(PhaseKind.Bundling));

        //         ASTHelpers.Connect(ast, iifeReturnValue.ID, exportedMembers.ToArray(), SemanticRole.Member);

        //         ASTHelpers.Connect(ast, iifeReturnStatement.ID, new[] { iifeReturnValue.Node }, SemanticRole.Value);
        //     }
        //     content.Add(iifeReturnStatement.Node);

        //     var (iife, lambdaDecl) = ASTNodeHelpers.CreateIIFE(ast, content);
        

        //     var name = NodeFactory.Identifier(ast, component.Origin, component.ID);

        //     var dataValueDecl = CreateConstantDataValueDeclaration(
        //         ast,
        //         component.Node.Origin,
        //         name.Node,
        //         iife.Node
        //     );

        //     result.Value = dataValueDecl;

        //     return result;
        // }

        // // [dho] we need to basically rewrite the imports so they resolve in the new tree after we have inlined it all - 22/09/19
        // private static Result<object> ProcessImports(Session session, Artifact artifact, RawAST ast, Component component, List<Node> importDeclarations, List<Node> hoistedImports, CancellationToken token)
        // {
        //     var result = new Result<object>();

        //     if (importDeclarations?.Count > 0)
        //     {
        //         var importsSortedByType = result.AddMessages(
        //             ImportHelpers.SortImportDeclarationsByType(session, artifact, ast, component, importDeclarations, LanguageSemantics.Swift, token)
        //         );

        //         if (!HasErrors(result) && !token.IsCancellationRequested)
        //         {
        //             foreach (var im in importsSortedByType.SempilerImports)
        //             {
        //                 // [dho] For now we just complain about any uses of the Sempiler imports - 22/09/19
        //                 foreach (var kv in im.ImportInfo.SymbolReferences)
        //                 {
        //                     var symbol = kv.Key;
        //                     var references = kv.Value;

        //                     var clause = im.ImportInfo.Clauses[symbol];

        //                     result.AddMessages(
        //                         new NodeMessage(MessageKind.Error, $"Symbol '{symbol}' from package '{im.ImportInfo.SpecifierLexeme}' is a symbolic alias not mapped for {artifact.TargetPlatform}", clause)
        //                         {
        //                             Hint = GetHint(clause.Origin),
        //                             Tags = DiagnosticTags
        //                         }
        //                     );
        //                 }

        //                 // [dho] remove the "sempiler" import because it is a _fake_
        //                 // import we just use to be sure that the symbols the user refers
        //                 // to are for sempiler, and not something in global scope for a particular target platform - 24/06/19 (ported : 22/09/19)
        //                 ASTHelpers.DisableNodes(ast, new[] { im.ImportDeclaration.ID });
        //             }

        //             foreach (var im in importsSortedByType.ComponentImports)
        //             {
        //                 var importedComponentInlinedName = ToInlinedConstantIdentifier(ast, im.Component.Node);

        //                 result.AddMessages(
        //                     ImportHelpers.QualifyImportReferences(ast, im, importedComponentInlinedName)
        //                 );

        //                 // [dho] remove the import because all components are inlined into the same output file - 24/06/19
        //                 ASTHelpers.DisableNodes(ast, new[] { im.ImportDeclaration.ID });
        //             }

        //             foreach (var im in importsSortedByType.PlatformImports)
        //             {
        //                 var specifier = im.ImportDeclaration.Specifier;

        //                 var importReplacementNodes = new List<Node>();
        //                 var importPackageHandleLexeme = im.ImportDeclaration.ID;

        //                 // [dho] `import .... from "hello"` - 22/09/19
        //                 var hoistedImport = NodeFactory.ImportDeclaration(ast, im.ImportDeclaration.Node.Origin);
        //                 {
        //                     ASTHelpers.Connect(ast, hoistedImport.ID, new [] {
        //                         im.ImportDeclaration.Specifier
        //                     }, SemanticRole.Specifier);

        //                     var refAliasDecl = NodeFactory.ReferenceAliasDeclaration(ast, im.ImportDeclaration.Origin);
        //                     {
        //                         ASTHelpers.Connect(ast, refAliasDecl.ID, new [] {
        //                             NodeFactory.DefaultExportReference(ast, im.ImportDeclaration.Origin).Node
        //                         }, SemanticRole.From);

        //                         ASTHelpers.Connect(ast, refAliasDecl.ID, new [] {
        //                             NodeFactory.Identifier(ast, im.ImportDeclaration.Origin, importPackageHandleLexeme).Node
        //                         }, SemanticRole.Name);
        //                     }

        //                     ASTHelpers.Connect(ast, hoistedImport.ID, new [] { refAliasDecl.Node }, SemanticRole.Clause);
        //                 }
        //                 hoistedImports.Add(hoistedImport.Node);



        //                 foreach (var kv in im.ImportInfo.Clauses)
        //                 {
        //                     var clause = kv.Value;
        //                     var initializer = default(Node);

        //                     if (clause.Kind == SemanticKind.DefaultExportReference)
        //                     {
        //                         initializer = ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, clause.Origin, new[] {
        //                             NodeFactory.Identifier(ast, clause.Origin, importPackageHandleLexeme).Node,
        //                             NodeFactory.Identifier(ast, clause.Origin, "default").Node
        //                         });
        //                     }
        //                     // [dho] TODO CHECK is this case necessary? I just wrote this before running any of the code
        //                     // so might be a condition that is never true! - 22/09/19
        //                     else if (clause.Kind == SemanticKind.Identifier)
        //                     {
        //                         var identifierLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)clause).Lexeme;

        //                         initializer = ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, clause.Origin, new[] {
        //                             NodeFactory.Identifier(ast, clause.Origin, importPackageHandleLexeme).Node,
        //                             NodeFactory.Identifier(ast, clause.Origin, identifierLexeme).Node
        //                         });
        //                     }
        //                     else
        //                     {
        //                         initializer = NodeFactory.Identifier(ast, clause.Origin, importPackageHandleLexeme).Node;
        //                     }

        //                     importReplacementNodes.Add(
        //                         CreateConstantDataValueDeclaration(
        //                             ast,
        //                             kv.Value.Origin,
        //                             NodeFactory.Identifier(ast, specifier.Origin, kv.Key).Node,
        //                             initializer
        //                         ).Node
        //                     );
        //                 }

        //                 // [dho] replace the original import declaration, with the rewritten require constants - 22/09/19
        //                 ASTHelpers.Replace(ast, im.ImportDeclaration.ID, importReplacementNodes.ToArray());
        //             }
        //         }
        //     }

        //     return result;
        // }

        // private static string ToInlinedConstantIdentifier(RawAST ast, Node node)
        // {
        //     return node.ID;
        // }

        // [dho] for the sake of importing the typescript types into scope we have to use
        // a dynamic import instead of a simple require in some cases - 23/12/19
        // private static Node CreateDynamicImport(RawAST ast, INodeOrigin origin, string packageName)
        // {
        //     var dynamicImportInvocation = NodeFactory.Invocation(ast, origin);

        //     ASTHelpers.Connect(ast, dynamicImportInvocation.ID, new[] {
        //         NodeFactory.Identifier(ast, origin, "import").Node
        //     }, SemanticRole.Subject);

        //     ASTHelpers.Connect(ast, dynamicImportInvocation.ID, new[] {
        //         NodeFactory.StringConstant(ast, origin, packageName).Node
        //     }, SemanticRole.Argument);

        //     return ASTNodeHelpers.CreateAwait(ast, dynamicImportInvocation.Node).Item1.Node;
        // }

        // private static Node CreateRequireInvocation(RawAST ast, INodeOrigin origin, string packageName)
        // {
        //     var requireInvocation = NodeFactory.Invocation(ast, origin);

        //     ASTHelpers.Connect(ast, requireInvocation.ID, new[] {
        //         NodeFactory.Identifier(ast, origin, "require").Node
        //     }, SemanticRole.Subject);

        //     ASTHelpers.Connect(ast, requireInvocation.ID, new[] {
        //         NodeFactory.StringConstant(ast, origin, packageName).Node
        //     }, SemanticRole.Argument);

        //     return requireInvocation.Node;
        // }

        // private static DataValueDeclaration CreateConstantDataValueDeclaration(RawAST ast, INodeOrigin origin, Node name, Node initializer)
        // {
        //     var dataValueDecl = NodeFactory.DataValueDeclaration(ast, origin);
        //     {
        //         // [dho] make it a constant declaration - 22/09/19
        //         var constantFlag = NodeFactory.Meta(
        //             ast,
        //             new PhaseNodeOrigin(PhaseKind.Bundling),
        //             MetaFlag.Constant
        //         );

        //         ASTHelpers.Connect(ast, dataValueDecl.ID, new[] { constantFlag.Node }, SemanticRole.Meta);

        //         ASTHelpers.Connect(ast, dataValueDecl.ID, new[] { name }, SemanticRole.Name);

        //         ASTHelpers.Connect(ast, dataValueDecl.ID, new[] { initializer }, SemanticRole.Initializer);
        //     }

        //     return dataValueDecl;
        // }

//         ///<summary>Creates a lightweight component that acts as the interface between the Now server, and the user defined function (handler)</summary> 
//         private static Result<Invocation> CreateRouteHandler(Session session, Artifact artifact, RawAST ast, ServerInlining.ServerRouteInfo route, CancellationToken token)
//         {
//             var result = new Result<Invocation>();

//             // [dho] NOTE changing this will impact the code below!!! Do not just remove
//             // this guard without updating the rest of the code!! - 04/10/19
//             if (route.Handler.Kind != SemanticKind.FunctionDeclaration)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(route.Handler, $"'{route.Handler.Kind}' route handlers")
//                 );

//                 return result;
//             }

//             var handler = ASTNodeFactory.FunctionDeclaration(ast, route.Handler);

//             var parameters = handler.Parameters;

//             // [dho] TODO choose HTTP method based on more intelligent parameter requirements and function role! - 22/09/19
//             string expressHTTPVerb = parameters.Length > 0 ? "post" : "get";

//             if (route.HTTPVerbAnnotation != null)
//             {
//                 var isValid = false;

//                 var httpExp = route.HTTPVerbAnnotation.Expression;

//                 if (httpExp?.Kind == SemanticKind.Invocation)
//                 {
//                     var args = ASTNodeFactory.Invocation(ast, httpExp).Arguments;

//                     if (args.Length == 1 && args[0]?.Kind == SemanticKind.InvocationArgument)
//                     {
//                         var invArg = ASTNodeFactory.InvocationArgument(ast, args[0]);
//                         var invArgValue = invArg.Value;

//                         if (invArgValue.Kind == SemanticKind.StringConstant)
//                         {
//                             isValid = true;
//                             expressHTTPVerb = ASTNodeFactory.StringConstant(ast, (DataNode<string>)invArgValue).Value.ToLower();
//                         }
//                     }
//                 }

//                 if (!isValid)
//                 {
//                     result.AddMessages(
//                         new NodeMessage(MessageKind.Error,
//                             "HTTP annotation must contain string constant expression", httpExp)
//                         {
//                             Hint = GetHint(httpExp.Origin),
//                         }
//                     );
//                 }
//             }

//             var handlerBodyContent = new List<Node>();

//             handlerBodyContent.Add(
//                 NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
//                     $"const user = await {UserParserFunctionNameLexeme}(req);"
//                 ).Node
//             );

//             if (route.EnforceAuthAnnotation != null)
//             {
//                 handlerBodyContent.Add(
//                             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
//     $@"
//     if(user === null)
//     {{
//         res.statusCode = 401;

//         res.json({{ message : ""Unauthorized"" }});

//         return;
//     }}"
//                     ).Node
//                 );
//             }

//             // [dho] NOTE disabling in favour of just using `this.user` inside function block etc. - 29/10/19
//             // // [dho] unwrap user for request in handler body - 04/10/19
//             // {
//             //     var body = handler.Body;

//             //     if(body?.Kind == SemanticKind.Block)
//             //     {
//             //         ASTHelpers.Connect(ast, body.ID, new [] {
//             //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), 
//             //                 "const context = this;"
//             //             ).Node
//             //         }, SemanticRole.Content, 0);
//             //     }
//             // }

//             var paramNameLexemes = ASTNodeHelpers.ExtractParameterNameLexemes(ast, parameters);
//             var reqParamAccesses = new string[paramNameLexemes.Length];

//             if (parameters.Length > 0)
//             {
//                 handlerBodyContent.Add(
//                     NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
//                         $"const {RouteParamsSourceNameLexeme}=" + (expressHTTPVerb == "post" ? "req.body" : "{...req.query, req.params }") + ";"
//                     ).Node
//                 );

//                 // var invocationArguments = new InvocationArgument[parameters.Length];

//                 for (int i = 0; i < parameters.Length; ++i)
//                 {
//                     var paramDecl = ASTNodeFactory.ParameterDeclaration(ast, parameters[i]);

//                     var paramNameLexeme = paramNameLexemes[i];

//                     // [dho] guard against **route parameters** not having a clear name or label, because
//                     // we need a name to pick out of the `req.query`/`req.body`, eg. `export function foo({ x, y } : Bar){ ... }` - 22/09/19            
//                     if (paramNameLexeme == null)
//                     {
//                         var hint = paramDecl.Label ?? paramDecl.Name ?? paramDecl.Node;

//                         result.AddMessages(
//                             new NodeMessage(MessageKind.Error,
//                                 "Parameters in routes must have a name or label with a single identifier", hint)
//                             {
//                                 Hint = GetHint(hint.Origin),
//                             }
//                         );

//                         continue;
//                     }

//                     reqParamAccesses[i] = $"{RouteParamsSourceNameLexeme}['{paramNameLexeme}']";

//                     var isRequiredParameter = (MetaHelpers.ReduceFlags(paramDecl) & MetaFlag.Optional) == 0;

//                     if (isRequiredParameter)
//                     {
//                         // [dho] TODO check type of parameter!! Use joi? - 22/09/19 
//                         handlerBodyContent.Add(
//                                 NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
//         $@"
//     if({reqParamAccesses[i]} === void 0)
//     {{
//         res.statusCode = 400;

//         res.json({{ message: ""Parameter '{paramNameLexeme}' is required"" }});

//         return;
//     }}"
//                                 ).Node
//                             );
//                     }
//                 }
//             }



//             // [dho] passing in `user` in execution context for function, and we insert a line in the handler
//             // to unwrap the user object and expose it to the function body scope - 04/10/19
//             handlerBodyContent.Add(
//                 NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
// $@"
// try {{
//     const data = await {string.Join(".", route.QualifiedHandlerName)}.apply({{ user, req, res }}, [{string.Join(",", reqParamAccesses)}]);

//     res.statusCode = 200;

//     res.json(data || {{ }});
// }} catch (error) {{
    
//     const {{ message, stack, code }} = error;

//     const isUnexpectedError = ( 
//         error instanceof TypeError || 
//         error instanceof ReferenceError || 
//         error instanceof EvalError ||
//         error instanceof RangeError
//     );

//     res.statusCode = isUnexpectedError ? 500 : 400;

//     res.json({{ message, stack, code }});
// }}"
//                 ).Node
//             );



//             var inv = NodeFactory.Invocation(ast, handler.Origin);
//             {
//                 ASTHelpers.Connect(ast, inv.ID, new[] {
//                     ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, new PhaseNodeOrigin(PhaseKind.Bundling), new [] {
//                         NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "app").Node,
//                         // [dho] TODO choose HTTP method based on parameter requirements!
//                         NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), expressHTTPVerb).Node
//                     })
//                 }, SemanticRole.Subject);

//                 var routePath = NodeFactory.StringConstant(ast,
//                     new PhaseNodeOrigin(PhaseKind.Bundling),
//                     $"/{string.Join("/", route.APIRelPath)}"
//                 );

//                 var lambdaDecl = ASTNodeHelpers.CreateLambda(ast, handlerBodyContent);
//                 {
//                     var asyncFlag = NodeFactory.Meta(
//                         ast,
//                         new PhaseNodeOrigin(PhaseKind.Bundling),
//                         MetaFlag.Asynchronous
//                     );

//                     ASTHelpers.Connect(ast, lambdaDecl.ID, new[] { asyncFlag.Node }, SemanticRole.Meta);


//                     var requestParam = NodeFactory.ParameterDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
//                     {
//                         ASTHelpers.Connect(ast, requestParam.ID, new[] {
//                             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "req").Node
//                         }, SemanticRole.Name);
//                     }

//                     var responseParam = NodeFactory.ParameterDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
//                     {
//                         ASTHelpers.Connect(ast, responseParam.ID, new[] {
//                             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "res").Node
//                         }, SemanticRole.Name);
//                     }

//                     ASTHelpers.Connect(ast, lambdaDecl.ID, new[] { requestParam.Node, responseParam.Node }, SemanticRole.Parameter);
//                 }


//                 ASTHelpers.Connect(ast, inv.ID, new[] { routePath.Node, lambdaDecl.Node }, SemanticRole.Argument);
//             }


//             result.Value = inv;

//             return result;
//         }
        
        private static Result<object> AppendRouterCode(Session session, Artifact artifact, RawAST ast, List<ServerInlining.ServerRouteInfo> routes, System.Text.StringBuilder sb, CancellationToken token)
        {
            var result = new Result<object>();

            sb.AppendLine($"const {APISymbolicLexeme} = require('firebase-functions').https.onRequest((() => {{");
                
            sb.AppendLine(
$@"const express = require(""express"");
const cors = require(""cors"");
const app = express();
{UserParserFunctionImplementation}
app.use(cors({{ origin : true }}));"
            );

            foreach (var route in routes)
            {
                result.AddMessages(
                    AppendRouteHandlerCode(session, artifact, ast, route, sb, token)
                );
            }

            sb.Append(
                "return app;"
            );
        
            // [dho] close IIFE - 27/12/19
            sb.AppendLine("})());");

            sb.AppendLine($"export {{ {APISymbolicLexeme} }};");
            
            return result;
        }
        private static Result<object> AppendRouteHandlerCode(Session session, Artifact artifact, RawAST ast, ServerInlining.ServerRouteInfo route, System.Text.StringBuilder sb, CancellationToken token)
        {
            var result = new Result<object>();

            // [dho] NOTE changing this will impact the code below!!! Do not just remove
            // this guard without updating the rest of the code!! - 04/10/19
            if (route.Handler.Kind != SemanticKind.FunctionDeclaration)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(route.Handler, $"'{route.Handler.Kind}' route handlers")
                );

                return result;
            }

            var handler = ASTNodeFactory.FunctionDeclaration(ast, route.Handler);

            var parameters = handler.Parameters;

            // [dho] TODO choose HTTP method based on more intelligent parameter requirements and function role! - 22/09/19
            string expressHTTPVerb = parameters.Length > 0 ? "post" : "get";

            if (route.HTTPVerbAnnotation != null)
            {
                var isValid = false;

                var httpExp = route.HTTPVerbAnnotation.Expression;

                if (httpExp?.Kind == SemanticKind.Invocation)
                {
                    var args = ASTNodeFactory.Invocation(ast, httpExp).Arguments;

                    if (args.Length == 1 && args[0]?.Kind == SemanticKind.InvocationArgument)
                    {
                        var invArg = ASTNodeFactory.InvocationArgument(ast, args[0]);
                        var invArgValue = invArg.Value;

                        if (invArgValue.Kind == SemanticKind.StringConstant)
                        {
                            isValid = true;
                            expressHTTPVerb = ASTNodeFactory.StringConstant(ast, (DataNode<string>)invArgValue).Value.ToLower();
                        }
                    }
                }

                if (!isValid)
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error,
                            "HTTP annotation must contain string constant expression", httpExp)
                        {
                            Hint = GetHint(httpExp.Origin),
                        }
                    );
                }
            }

            // [dho] lambda declaration - 27/12/19
            sb.AppendLine($"app.{expressHTTPVerb}('/{string.Join("/", route.APIRelPath)}', async (req, res) => {{");

    
            sb.AppendLine(
                $"const user = await {UserParserFunctionNameLexeme}(req);"
            );

            if (route.EnforceAuthAnnotation != null)
            {
                sb.AppendLine(
    $@"
    if(user === null)
    {{
        res.statusCode = 401;

        res.json({{ message : ""Unauthorized"" }});

        return;
    }}"
                );
            }

            // [dho] NOTE disabling in favour of just using `this.user` inside function block etc. - 29/10/19
            // // [dho] unwrap user for request in handler body - 04/10/19
            // {
            //     var body = handler.Body;

            //     if(body?.Kind == SemanticKind.Block)
            //     {
            //         ASTHelpers.Connect(ast, body.ID, new [] {
            //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), 
            //                 "const context = this;"
            //             ).Node
            //         }, SemanticRole.Content, 0);
            //     }
            // }

            var paramNameLexemes = ASTNodeHelpers.ExtractParameterNameLexemes(ast, parameters);
            var reqParamAccesses = new string[paramNameLexemes.Length];

            if (parameters.Length > 0)
            {
                sb.AppendLine(
                    $"const {RouteParamsSourceNameLexeme}=" + (expressHTTPVerb == "post" ? "req.body" : "{...req.query, req.params }") + ";"
                );

                // var invocationArguments = new InvocationArgument[parameters.Length];

                for (int i = 0; i < parameters.Length; ++i)
                {
                    var paramDecl = ASTNodeFactory.ParameterDeclaration(ast, parameters[i]);

                    var paramNameLexeme = paramNameLexemes[i];

                    // [dho] guard against **route parameters** not having a clear name or label, because
                    // we need a name to pick out of the `req.query`/`req.body`, eg. `export function foo({ x, y } : Bar){ ... }` - 22/09/19            
                    if (paramNameLexeme == null)
                    {
                        var hint = paramDecl.Label ?? paramDecl.Name ?? paramDecl.Node;

                        result.AddMessages(
                            new NodeMessage(MessageKind.Error,
                                "Parameters in routes must have a name or label with a single identifier", hint)
                            {
                                Hint = GetHint(hint.Origin),
                            }
                        );

                        continue;
                    }

                    reqParamAccesses[i] = $"{RouteParamsSourceNameLexeme}['{paramNameLexeme}']";

                    var isRequiredParameter = (MetaHelpers.ReduceFlags(paramDecl) & MetaFlag.Optional) == 0;

                    if (isRequiredParameter)
                    {
                        // [dho] TODO check type of parameter!! Use joi? - 22/09/19 
                        sb.AppendLine(
        $@"
    if({reqParamAccesses[i]} === void 0)
    {{
        res.statusCode = 400;

        res.json({{ message: ""Parameter '{paramNameLexeme}' is required"" }});

        return;
    }}"
                        );
                    }
                }
            }



            // [dho] passing in `user` in execution context for function, and we insert a line in the handler
            // to unwrap the user object and expose it to the function body scope - 04/10/19
            sb.AppendLine(
$@"
try {{
    {/* [dho] invoke user code for route - 28/12/19 */""}
    const data = await {UserCodeSymbolicLexeme}.{string.Join(".", route.QualifiedHandlerName)}.apply({{ user, req, res }}, [{string.Join(",", reqParamAccesses)}]);

    res.statusCode = 200;

    res.json(data || {{ }});
}} catch (error) {{
    
    {/* [dho] infer whether the error was expected (explicitly thrown) - 28/12/19 */""}
    const isUnexpectedError = ( 
        error instanceof TypeError || 
        error instanceof ReferenceError || 
        error instanceof EvalError ||
        error instanceof RangeError
    );

    res.statusCode = isUnexpectedError ? 500 : 400;

    if(isUnexpectedError && {IsProductionSymbolicLexeme})
    {{
        {/* [dho] suppress internal diagnostic information for unexpected errors if in production - 28/12/19 */""}
        res.json({{ message : 'Something went wrong. Please try again' }});
    }}
    else 
    {{
        console.error(`Unexpected error processing route '{string.Join(".", route.QualifiedHandlerName)}'`);
        console.error(error);

        const {{ message, stack, code }} = error;
        
        res.json({{ message, stack, code }});
    }}

}}"
            );


            // [dho] end of `app.get(...)` - 27/12/19
            sb.AppendLine("})");


            return result;
        }

        private static Result<object> AppendExportedSymbolsCode(Session session, Artifact artifact, RawAST ast, List<ServerInlining.ServerExportedSymbolInfo> exportedSymbols, System.Text.StringBuilder sb, CancellationToken token)
        {
            var result = new Result<object>();

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


            return result;
        }
    }

}