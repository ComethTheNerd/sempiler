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

namespace Sempiler.Bundler
{
    using static BundlerHelpers;

    public class FirebaseFunctionsBundler : IBundler
    {
        static readonly string[] DiagnosticTags = new string[] { "bundler", "firebase/functions" };

        const string UserCodeDirName = "functions";
        static readonly string AppFileName = $"{UserCodeDirName}/src/index";

        // [dho] NOTE for some reason firebase functions doesn't work properly if the name
        // we use here is long and complex (like a component ID).. but "api" seems to work fine...
        // It won't clash with any user code symbol called `api` either, because we use `exports.api = ...` - 24/09/19
        static readonly string APISymbolicLexeme = "api";

        public async Task<Result<OutFileCollection>> Bundle(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            if (artifact.Role != ArtifactRole.Server)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                );

                return result;
            }

            
            var inlined = default(Component);
            var ofc = default(OutFileCollection);//new OutFileCollection();

            // [dho] emit source files - 21/05/19
            {
                var emitter = default(IEmitter);

                if (artifact.TargetLang == ArtifactTargetLang.TypeScript)
                {
                    inlined = result.AddMessages(TypeScriptInlining(session, artifact, ast, token));

                    if (HasErrors(result) || token.IsCancellationRequested) return result;

                    emitter = new TypeScriptEmitter();

                    ofc = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ast, token));
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
                var dependencies = session.Dependencies[artifact.Name];
                
                if(dependencies.Count > 0)
                {
                    dependenciesContent.Append(",");
                    dependenciesContent.AppendLine();

                    for(int i = 0; i < dependencies.Count; ++i)
                    {
                        var dependency = dependencies[i];
                        var name = dependency.Name;
                        var version = dependency.Version ?? "*";

                        dependenciesContent.Append($@"""{name}"": ""{version}""");

                        if(i < dependencies.Count - 1)
                        {
                            dependenciesContent.Append(",");
                        }

                        dependenciesContent.AppendLine();
                    }
                }

                AddRawFileIfMissing(ofc, $"{UserCodeDirName}/package.json", 
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
                AddRawFileIfMissing(ofc, $"{UserCodeDirName}/tsconfig.json", 
@"{
  ""compilerOptions"": {
    ""lib"": [""es2017""],
    ""module"": ""commonjs"",
    ""noImplicitReturns"": true,
    ""outDir"": ""lib"",
    ""sourceMap"": true,
    ""target"": ""es2017""
  },
  ""include"": [
    ""src""
  ]
}");

                result.Value = ofc;
            }


            return result;
        }

        private static Result<Component> TypeScriptInlining(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<Component>();

            var root = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

            var domain = ASTNodeFactory.Domain(ast, root);

            var newComponentNodes = new List<Node>();

            // [dho] the component (eg. file) that contains the entrypoint view for the application - 31/08/19
            var entrypointComponent = default(Component);

    
            // [dho] a component containing all the inlined constituent components for the compilation,
            // thereby creating a single larger file of all components in one file - 31/08/19
            var inlined = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Bundling), AppFileName);


            // var importDecls = new List<Node>();
            // [dho] these statements live outside of the `node1234` object type declaration
            // wrapper that we emit around inlined components - 31/08/19
            // var globalStatements = new List<Node>();
            {
                var inlinedContent = new List<Node>();
                var componentIDsToRemove = new List<string>();
                var routeInfos = new List<ServerInlining.ServerRouteInfo>();

                foreach (var cNode in domain.Components)
                {
                    var component = ASTNodeFactory.Component(ast, (DataNode<string>)cNode);

                    // [dho] every component in the AST (ie. every input file) will be turned into an IIFE data value declaration and inlined - 22/09/19
                    var r = ConvertToInlinedIIFEDataValueDeclaration(session, artifact, ast, component, token, ref routeInfos);

                    result.AddMessages(r);

                    if (HasErrors(r))
                    {
                        continue;
                    }
                    else if(token.IsCancellationRequested) 
                    {
                        return result;
                    }

                    // [dho] is this component the entrypoint for the whole artifact - 28/06/19
                    if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
                    {
                        entrypointComponent = component;
                        // entrypointView = r.Value;
                    }
                    // // [dho] any code that was outside an artifact root is just emitted without a class wrapper, so we have a way
                    // // in the input sources of declaring global symbols, or things like protocols which cannot be nested inside other
                    // // declarations in Swift - 18/07/19
                    // else if (BundlerHelpers.IsOutsideArtifactInferredSourceDir(session, component))
                    // {
                    //     globalStatements.AddRange(r.Value.Members);
                    // }


                    inlinedContent.Add(r.Value.Node);

                    componentIDsToRemove.Add(component.ID);
                }

                if(entrypointComponent == null)
                {
                    result.AddMessages(
                        new Message(MessageKind.Error, $"Could not create Firebase functions bundle because an entrypoint component was not found {artifact.Name} (expected '{BundlerHelpers.GetNameOfExpectedArtifactEntrypointComponent(session, artifact)}' to exist)")
                    );

                    return result;
                }


                // if (globalStatements.Count > 0)
                // {
                //     ASTHelpers.Connect(ast, inlined.ID, globalStatements.ToArray(), SemanticRole.None);
                // }

                var router = result.AddMessages(
                    CreateRouter(session, artifact, ast, inlined, token, ref routeInfos)
                );

                inlinedContent.Add(router);


                if (HasErrors(result) || token.IsCancellationRequested) return result;

                // [dho] inline all the existing components as iife declarations - 22/09/19
                ASTHelpers.Connect(ast, inlined.ID, inlinedContent.ToArray(), SemanticRole.None);


                // [dho] remove the components from the tree because now they have all been inlined - 22/09/19
                ASTHelpers.RemoveNodes(ast, componentIDsToRemove.ToArray());
                
                result.Value = inlined;
            }

            newComponentNodes.Add(inlined.Node);

            ASTHelpers.Connect(ast, domain.ID, newComponentNodes.ToArray(), SemanticRole.Component);

            return result;
        }

        private static Result<Node> CreateRouter(Session session, Artifact artifact, RawAST ast, Component entrypointComponent, CancellationToken token, ref List<ServerInlining.ServerRouteInfo> routes)
        {
            var result = new Result<Node>();

            var assignment = NodeFactory.Assignment(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
            {
                // [dho] `exports.api` - 24/09/19
                var storage = ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, new PhaseNodeOrigin(PhaseKind.Bundling), new [] {
                    NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "exports").Node,
                    NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), APISymbolicLexeme).Node,
                });

                ASTHelpers.Connect(ast, assignment.ID, new [] { storage }, SemanticRole.Storage);


                var content = new List<Node>();



                var invocation = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                {
                    var subject = ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, new PhaseNodeOrigin(PhaseKind.Bundling), new [] {
                        CreateRequireInvocation(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "firebase-functions").Node,
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "https").Node,
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "onRequest").Node,
                    });

                    ASTHelpers.Connect(ast, invocation.ID, new [] { subject }, SemanticRole.Subject);


                    {
                        content.Add(
                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), 
$@"const express = require(""express"");
const cors = require(""cors"");
const app = express();
app.use(cors({{ origin : true }}));").Node
                        );

                        foreach(var route in routes)
                        {
                            var routeHandler = result.AddMessages(
                                CreateRouteHandler(session, artifact, ast, route, token)
                            );

                            if(routeHandler != null)
                            {
                                content.Add(routeHandler.Node);
                            }
                        }

                        content.Add(
                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "return app;").Node
                        );
                    }

                    var (iife, _) = ASTNodeHelpers.CreateIIFE(ast, content);
                    
                    ASTHelpers.Connect(ast, invocation.ID, new [] { iife.Node }, SemanticRole.Argument);
                }

                ASTHelpers.Connect(ast, assignment.ID, new [] { invocation.Node }, SemanticRole.Value);
            }
        

            result.Value = assignment.Node;



            // var exportDecl = NodeFactory.ExportDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
            // {
            //     var name = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), $"{entrypointComponent.ID}{ExportSuffix}");

            //     var clause = CreateConstantDataValueDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling), name.Node, invocation.Node);

            //     ASTHelpers.Connect(ast, exportDecl.ID, new [] { clause.Node }, SemanticRole.Clause);
            // }

            // result.Value = exportDecl.Node;

            return result;
        }

        private static Result<DataValueDeclaration> ConvertToInlinedIIFEDataValueDeclaration(Session session, Artifact artifact, RawAST ast, Component component, CancellationToken token, ref List<ServerInlining.ServerRouteInfo> routes)
        {
            var result = new Result<DataValueDeclaration>();
            var exportedMembers = new List<Node>();
            var content = new List<Node>();

            var apiRelPath = new string[]{};
            var qualifiedName = new string[] { component.ID };

            var inlinerInfo = result.AddMessages(
                ServerInlining.GetInlinerInfo(session, ast, component, LanguageSemantics.TypeScript, apiRelPath, qualifiedName, token)
            );

            if(HasErrors(result) || token.IsCancellationRequested) return result;

            result.AddMessages(
                ProcessImports(session, artifact, ast, component, inlinerInfo.Imports, token)
            );

            // [dho] TODO FIX the server inlining code currently treats exports from any file to be a 'route', but I think
            // we only want exports from the artifact entrypoint to be considered 'routes'.. everything else is just an export
            // for sharing symbols between files - 22/09/19
            if(BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
            {
                routes.AddRange(inlinerInfo.RouteInfos);
            }
            // [dho] for now we are just going to complain about 'routes' in the session entrypoint file.. because otherwise we would
            // struggle to route those symbols if they clash with symbols in the artifact entrypoint for route names - 22/09/19
            else if(IsOutsideArtifactInferredSourceDir(session, component))
            {
                if(BundlerHelpers.IsInferredSessionEntrypointComponent(session, component))
                {
                    foreach(var routeInfo in inlinerInfo.RouteInfos)
                    {
                        result.AddMessages(
                            new NodeMessage(MessageKind.Error, "Cannot export from session entrypoint file", routeInfo.Handler)
                            {
                                Hint = GetHint(routeInfo.Handler.Origin),
                            }
                        );
                    }
                }
            }


            foreach(var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, component))
            {
                if(child.Kind == SemanticKind.ExportDeclaration)
                {
                    var exportDecl = ASTNodeFactory.ExportDeclaration(ast, child);
                    var specifier = exportDecl.Specifier;
                    var clauses = exportDecl.Clauses;

                    if(exportDecl.Specifier != default(Node))
                    {
                        result.AddMessages(
                            CreateUnsupportedFeatureResult(specifier, "Exporting symbols defined in other files")
                        );
                        continue;
                    }

                    /*
                        export { x, y } from "hello-world"
                        
                        return { x : node12345.x, y : node1234.y, }

                        export * from "hello-world"

                        return { ...node1234 }
                    
                        export const 
                     */

                    foreach(var clause in clauses)
                    {
                        if(LanguageSemantics.TypeScript.IsFunctionLikeDeclarationStatement(ast, clause))
                        {   
                            var exportedSymbolName = ASTHelpers.GetSingleMatch(ast, clause.ID, SemanticRole.Name);

                            if(exportedSymbolName != null)
                            {
                                System.Diagnostics.Debug.Assert(exportedSymbolName.Kind == SemanticKind.Identifier);

                                // [dho] make sure the symbol is exported from the IIFE we are creating for the component - 22/09/19
                                exportedMembers.Add(
                                    NodeFactory.Identifier(
                                        ast, 
                                        clause.Origin, 
                                        ASTNodeFactory.Identifier(ast, (DataNode<string>)exportedSymbolName).Lexeme
                                    ).Node
                                );

                                // [dho] we just want to keep the thing that was being exported, not the actual exporting 
                                // (which would not be legal inside the IIFE we are creating) - 22/09/19
                                content.Add(clause);
                            }
                            else
                            {
                                // [dho] wriing up `export default` etc. - 22/09/19
                                result.AddMessages(
                                    CreateUnsupportedFeatureResult(specifier, "Exporting anonymous or default declarations")
                                );
                                continue;
                            }
                        }
                        else
                        {
                            // [dho] TODO implement other reasonable cases - 22/09/19
                            result.AddMessages(
                                CreateUnsupportedFeatureResult(specifier, $"Exporting '{clause.Kind}'")
                            );
                            continue;
                        }
                    }
                }
                else 
                {
                    // [dho] we expect all imports to have been rewritten as constant require statements - 22/09/19
                    System.Diagnostics.Debug.Assert(child.Kind != SemanticKind.ImportDeclaration);

                    content.Add(child);
                }
            }


            var iifeReturnStatement = NodeFactory.FunctionTermination(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
            {
                var iifeReturnValue = NodeFactory.DynamicTypeConstruction(ast, new PhaseNodeOrigin(PhaseKind.Bundling));

                ASTHelpers.Connect(ast, iifeReturnValue.ID, exportedMembers.ToArray(), SemanticRole.Member);

                ASTHelpers.Connect(ast, iifeReturnStatement.ID, new [] { iifeReturnValue.Node }, SemanticRole.Value);
            }
            content.Add(iifeReturnStatement.Node);



            var (iife, lambdaDecl) = ASTNodeHelpers.CreateIIFE(ast, content);

            var name = NodeFactory.Identifier(ast, component.Origin, component.ID);

            var dataValueDecl = CreateConstantDataValueDeclaration(
                ast, 
                component.Node.Origin, 
                name.Node,
                iife.Node
            );

            result.Value = dataValueDecl;

            return result;
        }

        // [dho] we need to basically rewrite the imports so they resolve in the new tree after we have inlined it all - 22/09/19
        private static Result<object> ProcessImports(Session session, Artifact artifact, RawAST ast, Component component, List<Node> importDeclarations, CancellationToken token)
        {
            var result = new Result<object>();

            if (importDeclarations?.Count > 0)
            {
                var importsSortedByType = result.AddMessages(
                    ImportHelpers.SortImportDeclarationsByType(session, artifact, ast, component, importDeclarations, LanguageSemantics.Swift, token)
                );

                if (!HasErrors(result) && !token.IsCancellationRequested)
                {
                    foreach (var im in importsSortedByType.SempilerImports)
                    {
                        // [dho] For now we just complain about any uses of the Sempiler imports - 22/09/19
                        foreach (var kv in im.ImportInfo.SymbolReferences)
                        {
                            var symbol = kv.Key;
                            var references = kv.Value;

                            var clause = im.ImportInfo.Clauses[symbol];

                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"Symbol '{symbol}' from package '{im.ImportInfo.SpecifierLexeme}' is a symbolic alias not mapped for {artifact.TargetPlatform}", clause)
                                {
                                    Hint = GetHint(clause.Origin),
                                    Tags = DiagnosticTags
                                }
                            );
                        }

                        // [dho] remove the "sempiler" import because it is a _fake_
                        // import we just use to be sure that the symbols the user refers
                        // to are for sempiler, and not something in global scope for a particular target platform - 24/06/19 (ported : 22/09/19)
                        ASTHelpers.RemoveNodes(ast, new[] { im.ImportDeclaration.ID });
                    }

                    foreach (var im in importsSortedByType.ComponentImports)
                    {
                        var importedComponentInlinedName = ToInlinedConstantIdentifier(ast, im.Component.Node);

                        result.AddMessages(
                            ImportHelpers.QualifyImportReferences(ast, im, importedComponentInlinedName)
                        );

                        // [dho] remove the import because all components are inlined into the same output file - 24/06/19
                        ASTHelpers.RemoveNodes(ast, new[] { im.ImportDeclaration.ID });
                    }

                    foreach (var im in importsSortedByType.PlatformImports)
                    {
                        var specifier = im.ImportDeclaration.Specifier;

                        var importReplacementNodes = new List<Node>();
                        var importPackageHandleLexeme = im.ImportDeclaration.ID;

                        // [dho] `import .... from "hello"` - 22/09/19
                        if(specifier.Kind == SemanticKind.StringConstant)
                        {                  
                            importReplacementNodes.Add(
                                CreateConstantDataValueDeclaration(
                                    ast, 
                                    im.ImportDeclaration.Node.Origin, 
                                    NodeFactory.Identifier(ast, im.ImportDeclaration.Origin, importPackageHandleLexeme).Node,
                                    CreateRequireInvocation(ast, specifier.Origin, im.ImportInfo.SpecifierLexeme).Node
                                ).Node
                            );
                        }
                        else
                        {
                            importReplacementNodes.Add(
                                CreateConstantDataValueDeclaration(
                                    ast, 
                                    im.ImportDeclaration.Node.Origin, 
                                    NodeFactory.Identifier(ast, im.ImportDeclaration.Origin, importPackageHandleLexeme).Node,
                                    specifier
                                ).Node
                            );
                        }


                        foreach(var kv in im.ImportInfo.Clauses)
                        {   
                            var clause = kv.Value;
                            var initializer = default(Node);

                            if(clause.Kind == SemanticKind.DefaultExportReference)
                            {
                                initializer = ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, clause.Origin, new [] {
                                    NodeFactory.Identifier(ast, clause.Origin, importPackageHandleLexeme).Node,
                                    NodeFactory.Identifier(ast, clause.Origin, "default").Node
                                });
                            }
                            // [dho] TODO CHECK is this case necessary? I just wrote this before running any of the code
                            // so might be a condition that is never true! - 22/09/19
                            else if(clause.Kind == SemanticKind.Identifier)
                            {
                                var identifierLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)clause).Lexeme;

                                initializer = ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, clause.Origin, new [] {
                                    NodeFactory.Identifier(ast, clause.Origin, importPackageHandleLexeme).Node,
                                    NodeFactory.Identifier(ast, clause.Origin, identifierLexeme).Node
                                });
                            }
                            else
                            {
                                initializer = NodeFactory.Identifier(ast, clause.Origin, importPackageHandleLexeme).Node;
                            }

                            importReplacementNodes.Add(
                                CreateConstantDataValueDeclaration(
                                    ast, 
                                    kv.Value.Origin, 
                                    NodeFactory.Identifier(ast, specifier.Origin, kv.Key).Node,
                                    initializer
                                ).Node
                            );
                        }

                        // [dho] replace the original import declaration, with the rewritten require constants - 22/09/19
                        ASTHelpers.Replace(ast, im.ImportDeclaration.ID, importReplacementNodes.ToArray());
                    }
                }
            }

            return result;
        }

        private static string ToInlinedConstantIdentifier(RawAST ast, Node node)
        {
            return node.ID;
        }

        private static Invocation CreateRequireInvocation(RawAST ast, INodeOrigin origin, string packageName)
        {
            var requireInvocation = NodeFactory.Invocation(ast, origin);
            
            ASTHelpers.Connect(ast, requireInvocation.ID, new [] { 
                NodeFactory.Identifier(ast, origin, "require").Node 
            }, SemanticRole.Subject);

            ASTHelpers.Connect(ast, requireInvocation.ID, new [] { 
                NodeFactory.StringConstant(ast, origin, packageName).Node 
            }, SemanticRole.Argument);

            return requireInvocation;
        }

        private static DataValueDeclaration CreateConstantDataValueDeclaration(RawAST ast, INodeOrigin origin, Node name, Node initializer)
        {
            var dataValueDecl = NodeFactory.DataValueDeclaration(ast, origin);
            {
                // [dho] make it a constant declaration - 22/09/19
                var constantFlag = NodeFactory.Meta(
                    ast,
                    new PhaseNodeOrigin(PhaseKind.Bundling),
                    MetaFlag.Constant
                );

                ASTHelpers.Connect(ast, dataValueDecl.ID, new[] { constantFlag.Node }, SemanticRole.Meta);
                
                ASTHelpers.Connect(ast, dataValueDecl.ID, new [] { name }, SemanticRole.Name);

                ASTHelpers.Connect(ast, dataValueDecl.ID, new [] { initializer }, SemanticRole.Initializer);                
            }

            return dataValueDecl;
        }

        ///<summary>Creates a lightweight component that acts as the interface between the Now server, and the user defined function (handler)</summary> 
        private static Result<Invocation> CreateRouteHandler(Session session, Artifact artifact, RawAST ast, ServerInlining.ServerRouteInfo route, CancellationToken token)
        {
            var result = new Result<Invocation>();

            if(route.Handler.Kind != SemanticKind.FunctionDeclaration)
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
            
            var qualifiedDelegateName = string.Join(".", route.QualifiedHandlerName);

            var handlerBodyContent = new List<Node>();

            var invocationArguments = new InvocationArgument[parameters.Length];

            var paramNameLexemes = ASTNodeHelpers.ExtractParameterNameLexemes(ast, parameters);
            var reqBodyAccesses = new string[paramNameLexemes.Length];

            for(int i = 0; i < parameters.Length; ++i)
            {
                var paramDecl = ASTNodeFactory.ParameterDeclaration(ast, parameters[i]);

                var paramNameLexeme = paramNameLexemes[i];
    
                // [dho] guard against **route parameters** not having a clear name or label, because
                // we need a name to pick out of the `req.body` json, eg. `export function foo({ x, y } : Bar){ ... }` - 22/09/19            
                if(paramNameLexeme == null)
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

                reqBodyAccesses[i] = $"req.body['{paramNameLexeme}']";


                var isRequired = (MetaHelpers.ReduceFlags(paramDecl) & MetaFlag.Optional) == 0;

                if(isRequired)
                {
                    // [dho] TODO check type of parameter!! Use joi? - 22/09/19 
                   handlerBodyContent.Add(
                        NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), 
$@"
if({reqBodyAccesses[i]} === void 0)
{{
    res.statusCode = 400;

    res.json({{ error: ""Parameter '{paramNameLexeme}' is required"" }});

    return;
}}"
                        ).Node
                    ); 
                }
            }

            
            handlerBodyContent.Add(
                NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), 
$@"
try {{
    const data = await {qualifiedDelegateName}({string.Join(",", reqBodyAccesses)});

    res.statusCode = 200;

    res.json({{ data : data || void 0 }});
}} catch (error) {{
    res.statusCode = 500;

    res.json({{ error: error.message }});
}}"
                ).Node
            );



            var inv = NodeFactory.Invocation(ast, handler.Origin);
            {
                ASTHelpers.Connect(ast, inv.ID, new [] {
                    ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, new PhaseNodeOrigin(PhaseKind.Bundling), new [] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "app").Node,
                        // [dho] TODO choose HTTP method based on parameter requirements!
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), expressHTTPVerb).Node
                    })
                }, SemanticRole.Subject);

                var routePath = NodeFactory.StringConstant(ast, 
                    new PhaseNodeOrigin(PhaseKind.Transformation), 
                    $"/{string.Join("/", route.APIRelPath)}"
                );

                var lambdaDecl = ASTNodeHelpers.CreateLambda(ast, handlerBodyContent);
                {
                    var asyncFlag = NodeFactory.Meta(
                        ast,
                        new PhaseNodeOrigin(PhaseKind.Bundling),
                        MetaFlag.Asynchronous
                    );

                    ASTHelpers.Connect(ast, lambdaDecl.ID, new [] { asyncFlag.Node }, SemanticRole.Meta);


                    var requestParam = NodeFactory.ParameterDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        ASTHelpers.Connect(ast, requestParam.ID, new [] { 
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "req").Node
                        }, SemanticRole.Name);
                    }

                    var responseParam = NodeFactory.ParameterDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        ASTHelpers.Connect(ast, responseParam.ID, new [] { 
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "res").Node
                        }, SemanticRole.Name);
                    }

                    ASTHelpers.Connect(ast, lambdaDecl.ID, new [] { requestParam.Node, responseParam.Node }, SemanticRole.Parameter);
                }


                ASTHelpers.Connect(ast, inv.ID, new [] { routePath.Node, lambdaDecl.Node }, SemanticRole.Argument);
            }


            result.Value = inv;

            return result;
        }
    }

}