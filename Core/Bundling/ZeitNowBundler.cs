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

namespace Sempiler.Bundling
{
    using static BundlerHelpers;

    public class ZeitNowBundler : IBundler
    {
        static readonly string[] DiagnosticTags = new string[] { "bundler", "zeit/now" };

        const string APIDirName = "api";
        const string AppFileName = "app";

        public IList<string> GetPreservedDebugEmissionRelPaths(Session session, Artifact artifact, CancellationToken token) => new string[]{ "node_modules" };

        public async Task<Result<OutFileCollection>> Bundle(Session session, Artifact artifact, List<Shard> shards, CancellationToken token)
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

            if(shards.Count > 1)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"Artifact '{artifact.Role.ToString()}' does not currently support multiple shards")
                );
            }

            // [dho] TODO FIXUP TEMPORARY HACK - need to add proper support for multiple targets!! - 16/10/19
            var shard = shards[0];


            if(shard.Dependencies.Count > 0)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"'{shard.Role.ToString()}' in artifact '{artifact.Role.ToString()}' does not currently support dependencies")
                );
            }


            var routeInfos = default(List<ServerInlining.ServerRouteInfo>);

            var ofc = default(OutFileCollection);//new OutFileCollection();

            // [dho] emit source files - 21/05/19
            {
                var emitter = default(IEmitter);

                if (artifact.TargetLang == ArtifactTargetLang.TypeScript)
                {
                    routeInfos = result.AddMessages(TypeScriptInlining(session, artifact, shard.AST, token));

                    if (HasErrors(result) || token.IsCancellationRequested) return result;

                    emitter = new TypeScriptEmitter();

                    ofc = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, shard, shard.AST, token));
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
                // var nowRoutes = new string[routeInfos.Count];

                // for(int i = 0; i < nowRoutes.Length; ++i)
                // {
                //     var ii = $"{APIDirName}/{string.Join("/", routeInfos[i].APIRelPath).ToLower()}";

                //     nowRoutes[i] = $"{{ \"src\" : \"{ii}\", \"dest\" : \"{ii}.ts\" }}";
                // }



                AddRawFileIfMissing(ofc, "now.json", 
$@"{{
  ""version"": 2,
  ""builds"": [
    {{
      ""src"": ""{APIDirName}**/*.ts"",
      ""use"": ""@now/node""
    }}
  ],
  ""env"": {{
    ""IS_NOW"": ""true""
  }},
  ""routes"": [
    {{ ""src"": ""{APIDirName}(.*)"", ""dest"": ""{APIDirName}/$1"" }}
  ]
}}");



                AddRawFileIfMissing(ofc, "package.json", 
$@"{{
  ""name"": ""{artifact.Name}"",
  ""private"": true,
  ""version"": ""1.0.0"",
  ""license"": ""MIT"",
  ""devDependencies"": {{
    ""@types/node-fetch"": ""^2.1.4"",
    ""ts-node"": ""^7.0.1"",
    ""typescript"": ""^3.2.4""
  }},
  ""dependencies"": {{
    ""node-fetch"": ""^2.3.0""
  }}
}}");


                AddRawFileIfMissing(ofc, $"tsconfig.json", 
@"{
    ""compilerOptions"": {
       ""target"": ""es5"" /* Specify ECMAScript target version: 'ES3' (default), 'ES5', 'ES2015', 'ES2016', 'ES2017','ES2018' or 'ESNEXT'. */,
        ""module"": ""commonjs"" /* Specify module code generation: 'none', 'commonjs', 'amd', 'system', 'umd', 'es2015', or 'ESNext'. */,
        ""lib"": [
            ""es2015""
        ] /* Specify library files to be included in the compilation. */,
        ""strict"": true /* Enable all strict type-checking options. */,
        ""noImplicitAny"": true /* Raise error on expressions and declarations with an implied 'any' type. */,
        ""strictNullChecks"": true /* Enable strict null checks. */,
        ""strictFunctionTypes"": true /* Enable strict checking of function types. */,
        ""strictBindCallApply"": true /* Enable strict 'bind', 'call', and 'apply' methods on functions. */,
        ""strictPropertyInitialization"": true /* Enable strict checking of property initialization in classes. */,
        ""noImplicitThis"": true /* Raise error on 'this' expressions with an implied 'any' type. */,
        ""alwaysStrict"": true /* Parse in strict mode and emit ""use strict"" for each source file. */,
        ""esModuleInterop"": true /* Enables emit interoperability between CommonJS and ES Modules via creation of namespace objects for all imports. Implies 'allowSyntheticDefaultImports'. */
    }
}");

                AddRawFileIfMissing(ofc, $".nowignore", "node_modules");
                
                AddRawFileIfMissing(ofc, $"nodemon.json", @"{""ext"": ""ts"" }");

                result.Value = ofc;
            }


            return result;
        }

        private static Result<List<ServerInlining.ServerRouteInfo>> TypeScriptInlining(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<List<ServerInlining.ServerRouteInfo>>();

            var domain = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

            var component = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Bundling), AppFileName);

            {
                var componentIDsToRemove = new List<string>();
                var inlinedNamespaceDecls = new List<Node>();
                var importDecls = new List<Node>();
                var routeInfos = new List<ServerInlining.ServerRouteInfo>();

                foreach(var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, domain.ID))
                {
                    System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

                    var c = ASTNodeFactory.Component(ast, (DataNode<string>)child);
                    
                    var staticClass = result.AddMessages(ConvertToInlinedNamespaceDeclaration(session, artifact, ast, c, token, ref importDecls, ref routeInfos));

                    if(staticClass != null)
                    {
                        inlinedNamespaceDecls.Add(staticClass.Node);
                    }

                    componentIDsToRemove.Add(child.ID);
                }

                // [dho] remove the components from the tree because now they have all been inlined - 01/06/19
                ASTHelpers.DisableNodes(ast, componentIDsToRemove.ToArray());

                // [dho] combine the imports - 01/06/19
                ASTHelpers.Connect(ast, component.ID, importDecls.ToArray(), SemanticRole.None);

                // [dho] inline all the existing components as static classes - 01/06/19
                ASTHelpers.Connect(ast, component.ID, inlinedNamespaceDecls.ToArray(), SemanticRole.None);

                // [dho] add the route handlers that act as the interface between the Now server and the user code - 01/06/19
                {
                    var routeNodes = new Node[routeInfos.Count];

                    for(int i = 0; i < routeNodes.Length; ++i){
                        var route = result.AddMessages(CreateRouteHandler(session, artifact, ast, routeInfos[i], token));

                        if(route != null)
                        {
                            routeNodes[i] = route.Node;
                        }
                    }

                    ASTHelpers.Connect(ast, domain.ID, routeNodes, SemanticRole.Component);
                }
                
                result.Value = routeInfos;
            }

            // [dho] add the component containing the inlined app code to the tree - 01/06/19
            ASTHelpers.Connect(ast, domain.ID, new [] { component.Node }, SemanticRole.Component);
            

            return result;
        }


        private static Result<NamespaceDeclaration> ConvertToInlinedNamespaceDeclaration(Session session, Artifact artifact, RawAST ast, Component component, CancellationToken token, ref List<Node> imports, ref List<ServerInlining.ServerRouteInfo> routes)
        {
            var result = new Result<NamespaceDeclaration>();

            var inlinedNamespaceDecl = NodeFactory.NamespaceDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
        
            // [dho] create name of namespace - 01/06/19
            var componentName = component.Name;

            var relParentDirPath = componentName.Replace(session.BaseDirectory.ToPathString(), "");

            var apiRelPath = default(string[]);
            var qualifiedName = default(string[]);

            {
                var apiRelPathInputStr = relParentDirPath.ToLower();
        
                if(apiRelPathInputStr.StartsWith("/"))
                {
                    apiRelPathInputStr = apiRelPathInputStr.Substring(1);
                
                    if(apiRelPathInputStr.StartsWith(Sempiler.Core.Main.InferredConfig.SourceDirName + "/"))
                    {
                        apiRelPathInputStr = apiRelPathInputStr.Substring(Sempiler.Core.Main.InferredConfig.SourceDirName.Length + 1);
                    
                        if(apiRelPathInputStr.StartsWith(artifact.Name.ToLower() + "/"))
                        {
                            apiRelPathInputStr = apiRelPathInputStr.Substring(artifact.Name.Length + 1);

                            if(apiRelPathInputStr.StartsWith(Sempiler.Core.Main.InferredConfig.EntrypointFileName))
                            {
                                apiRelPathInputStr = apiRelPathInputStr.Substring(Sempiler.Core.Main.InferredConfig.EntrypointFileName.Length);
                            }
                        }
                    }
                }

                var ext = System.IO.Path.GetExtension(apiRelPathInputStr);

                if(ext?.Length > 0)
                {
                    apiRelPathInputStr = apiRelPathInputStr.Substring(0, apiRelPathInputStr.Length - ext.Length);
                }
            

                apiRelPath = apiRelPathInputStr.Length > 0 ? apiRelPathInputStr.Split('/') : new string[] {};
            }

            {
                var qualifiedNameInputStr = relParentDirPath.ToLower();

                if(qualifiedNameInputStr.StartsWith("/"))
                {
                    qualifiedNameInputStr = qualifiedNameInputStr.Substring(1);
                }

                var ext = System.IO.Path.GetExtension(qualifiedNameInputStr);

                if(ext?.Length > 0)
                {
                    qualifiedNameInputStr = qualifiedNameInputStr.Substring(0, qualifiedNameInputStr.Length - ext.Length);
                }

                qualifiedName = qualifiedNameInputStr.Length > 0 ? qualifiedNameInputStr.Split('/') : new string[] {};
            }


            var classIdentifier = string.Join(".", qualifiedName); //ToInlinedNamespaceIdentifier(ctid, componentName);
        
            var rootNamespaceName = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), classIdentifier);

            ASTHelpers.Connect(ast, inlinedNamespaceDecl.ID, new [] { rootNamespaceName.Node }, SemanticRole.Name);
        
        
            {
                // [dho] export the namespace - 01/06/19
                var exportFlag = NodeFactory.Meta(
                    ast,
                    new PhaseNodeOrigin(PhaseKind.Bundling),
                    MetaFlag.WorldVisibility
                );

                ASTHelpers.Connect(ast, inlinedNamespaceDecl.ID, new [] { exportFlag.Node }, SemanticRole.Meta);
            }

            var inlinerInfo = result.AddMessages(ServerInlining.GetInlinerInfo(session, ast, component, LanguageSemantics.TypeScript, apiRelPath, qualifiedName, token));

            if(HasErrors(result) || token.IsCancellationRequested) return result;

            {
                imports.AddRange(inlinerInfo.Imports);
            }

            {
                ASTHelpers.Connect(ast, inlinedNamespaceDecl.ID, inlinerInfo.Members.ToArray(), SemanticRole.Member);
            }

            {
                routes.AddRange(inlinerInfo.RouteInfos);
            }

            result.Value = inlinedNamespaceDecl;
              
            return result;
        }

        ///<summary>Creates a lightweight component that acts as the interface between the Now server, and the user defined function (handler)</summary> 
        private static Result<Component> CreateRouteHandler(Session session, Artifact artifact, RawAST ast, ServerInlining.ServerRouteInfo route, CancellationToken token)
        {
            var result = new Result<Component>();

            // [dho] TODO support - 04/10/19
            if(route.EnforceAuthAnnotation != null)
            {
                result.AddMessages(new Sempiler.AST.Diagnostics.NodeMessage(MessageKind.Error, "Authenticated routes are not currently supported", route.EnforceAuthAnnotation)
                {
                    Hint = GetHint(route.EnforceAuthAnnotation.Origin),
                    Tags = DiagnosticTags
                });
            }


            var handlerAlias = "handler";

            var routePath = $"{APIDirName}/{string.Join("/", route.APIRelPath).ToLower()}";

            var component = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Bundling), routePath);

            var sb = new System.Text.StringBuilder(
$@"import {{ IncomingMessage, ServerResponse }} from ""http"";
import * as App from ""../{AppFileName}"";

const {handlerAlias} = App.{string.Join(".", route.QualifiedHandlerName)};

" // [dho] bit of breathing space - 01/06/19
            );

            sb.Append(
$@"export default async (req: IncomingMessage, res: ServerResponse) => {{
    res.setHeader(""Content-Type"", ""application/json"");
    
    try {{
        const data = await {handlerAlias}();
        
        res.statusCode = 200;

        res.end(JSON.stringify({{ data : data || void 0 }}));
    }} catch (error) {{
        res.statusCode = 500;

        res.end(JSON.stringify({{ error: error.message }}));
    }}
}};"
            );


            var code = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), sb.ToString());

            ASTHelpers.Connect(ast, component.ID, new [] { code.Node }, SemanticRole.None);

            result.Value = component;

            return result;
        }


        // private static string ToInlinedNamespaceIdentifier(string ctid, string inputString)
        // {
        //     var sb = new System.Text.StringBuilder(ctid + "$$");

        //     foreach (byte b in System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(inputString)))
        //     {
        //         sb.Append(b.ToString("X2"));
        //     }

        //     return sb.ToString();
        // }
    }

}