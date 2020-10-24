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

namespace Sempiler.Bundling.NodeJS
{
    public static class PackageJSONHelpers 
    {
        public static Result<string> EmitManifestCode(RawAST ast, string productName, string productVersion, string mainRelPath, List<Dependency> dependencies, CancellationToken token)
        {
            var result = new Result<string>();

            var devDependenciesContent = new List<string>();
            var dependenciesContent = new List<string>();

            for (int i = 0; i < dependencies.Count; ++i)
            {
                if(token.IsCancellationRequested)
                {
                    return result;
                }

                var dependency = dependencies[i];
                var name = dependency.Name;
                var version = dependency.Version ?? "*";
                var packageManager = dependency.PackageManager;
                var url = dependency.URL;

                if(packageManager != PackageManager.NPM && packageManager != null)
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"Unsupported package manager '{packageManager}' for depedency '{name}'")
                    );
                }

                if(url != null)
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"Specifying a URL for {packageManager} dependency '{dependency.Name}' is not currently supported")
                    );
                }

                (name == "typescript" || name.StartsWith("@types/") ? 
                    devDependenciesContent : 
                    dependenciesContent
                ).Add($@"""{name}"": ""{version}""");
            }
        
            var packageJSONCode = $@"{{
  ""name"": ""{productName}"",
  ""private"": true,
  ""version"": ""{productVersion}"",
  ""engines"": {{
    ""node"": ""10""
  }},
  ""scripts"": {{
    ""build"": ""./node_modules/.bin/tsc""
  }},
  ""devDependencies"": {{
    {string.Join(",\n", devDependenciesContent)}
  }},
  ""dependencies"": {{
    {string.Join(",\n", dependenciesContent)}
  }},
  ""main"": ""{mainRelPath}""
}}";

            result.Value = packageJSONCode;

            return result;
        }
    }

    public static class TSConfigHelpers 
    {
        // [dho] adapted from https://raw.githubusercontent.com/firebase/functions-samples/master/typescript-getting-started/functions/tsconfig.json - 24/09/19
        public static Result<string> EmitConfigCode(RawAST ast, IEnumerable<string> includePaths, string outDir, CancellationToken token)
        {
            var result = new Result<string>();

            var configCode = // [dho] https://stackoverflow.com/a/52384384/300037 - 26/12/19
            // [dho] https://stackoverflow.com/a/57653497/300037 - 26/12/19
            // [dho] NOTE `esModuleInterop` so we can hoist the imports to top level and bind them to a variable, because we
            // cannot do the imports asynchronously inside an IIFE in the `module.exports` because Firebase doesn't like this - 26/12/19
$@"{{
  ""compilerOptions"": {{
    ""lib"": [""es2017"",""dom""],
    ""module"": ""commonjs"",
    ""noImplicitReturns"": true,
    ""outDir"": ""{outDir}"",
    ""sourceMap"": true,
    ""target"": ""es2017"",
    ""skipLibCheck"": true,
    ""esModuleInterop"" : true
  }},
  ""include"": [
    ""{string.Join("\",\"", includePaths)}""
  ]
}}";

            result.Value = configCode;

            return result;
        }
    }

    public static class ExpressHelpers
    {
        public static readonly List<Dependency> RequiredDependencies = new List<Dependency>{
            new Dependency { Name = "express", PackageManager = PackageManager.NPM },
            new Dependency { Name = "@types/express", PackageManager = PackageManager.NPM },
            new Dependency { Name = "cors", PackageManager = PackageManager.NPM },
            new Dependency { Name = "body-parser", PackageManager = PackageManager.NPM },
            // [dho] For auth handling.. TODO move this out of here - 26/02/20
            new Dependency { Name = "firebase-admin", PackageManager = PackageManager.NPM }
        };


        /**/
        public const string IsProductionSymbolicLexeme = "isProduction";
        public static readonly string IsProductionDeclaration = $"const {IsProductionSymbolicLexeme} = process.env.ENV === 'production' || process.env.ENV === 'PROD';";

        // static readonly string AppFileName = $"{UserCodeDirName}/src/index";


        const string ExpressSymbolNameLexeme = "express";
        const string CORSSymbolNameLexeme = "cors";
        const string BodyParserSymbolNameLexeme = "bodyParser";

        const string UserParserFunctionNameLexeme = "parseUserFromFirebaseIDToken";
        const string ErrorHandlerFunctionNameLexeme = "handleError";

        const string ErrorResponseJSONFunctionNameLexeme = "createErrorJSON";

        const string RouteParamsSourceNameLexeme = "params";

        const string DelegatePrefixLexeme = "userCode";
        const string ContextArgLexeme = "contextArg";
        const string ExpressAppNameLexeme = "app";
        const string AppFactoryNameLexeme = "createExpressApp";

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


static readonly string ErrorHandlerFunctionImplementation = $@"function {ErrorHandlerFunctionNameLexeme}(routeName : string, error : Error & {{ code? : string | number }}, req : any, res : any) : void
{{
    {/* [dho] infer whether the error was expected (explicitly thrown === 'expected') - 28/12/19 */""}
    const isUnexpectedError = ( 
        error instanceof TypeError || 
        error instanceof ReferenceError || 
        error instanceof EvalError ||
        error instanceof RangeError
    );

    res.statusCode = isUnexpectedError ? 500 : 400;

    if(isUnexpectedError)
    {{
        console.error(`Unexpected error processing route '${{routeName}}'`);
        console.error(error);

        if(!res.headersSent)
        {{
            if({IsProductionSymbolicLexeme})
            {{
                {/* [dho] suppress internal diagnostic information for unexpected errors if in production - 28/12/19 */""}
                res.json({{ message : 'Something went wrong. Please try again' }});
            }}
            else {{
                res.json({ErrorResponseJSONFunctionNameLexeme}(error));
            }}
        }}
    }}
    else if(!res.headersSent) {{
        res.json({ErrorResponseJSONFunctionNameLexeme}(error));
    }}
}}";

static readonly string ErrorResponseJSONFunctionImplementation = $@"function {ErrorResponseJSONFunctionNameLexeme}(error : Error & {{ code? : string | number }}) : any
{{
    const {{ message, stack, code }} = error;
        
    const json : any = {{ message, stack }};

    if(code !== void 0){{
        json.code = code;
    }}

    return json;
}}";

        public static Result<string> EmitRouterCode(RawAST ast, List<ServerInlining.ServerRouteInfo> routes, CancellationToken token)
        {
            var result = new Result<string>();


            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"import {ExpressSymbolNameLexeme} from 'express'");
            sb.AppendLine($"import {CORSSymbolNameLexeme} from 'cors'");
            sb.AppendLine($"import {BodyParserSymbolNameLexeme} from 'body-parser'");

            sb.AppendLine(IsProductionDeclaration);

            sb.AppendLine(UserParserFunctionImplementation);

            sb.AppendLine(ErrorHandlerFunctionImplementation);

            sb.AppendLine(ErrorResponseJSONFunctionImplementation);

            sb.Append($"function {AppFactoryNameLexeme}({DelegatePrefixLexeme}, {ContextArgLexeme}?) : express.Application {{");

            sb.AppendLine(
$@"const {ExpressAppNameLexeme} = {ExpressSymbolNameLexeme}();
{ExpressAppNameLexeme}.use({CORSSymbolNameLexeme}({{ origin : true }}));
{ExpressAppNameLexeme}.use({BodyParserSymbolNameLexeme}.json({{ limit: '50mb' }}));
{ExpressAppNameLexeme}.use({BodyParserSymbolNameLexeme}.urlencoded({{ limit: '50mb', extended: true, parameterLimit : 50000 }}));"
            ); // [dho] adapted from : https://stackoverflow.com/a/36514330/300037 - 08/03/20


            foreach (var route in routes)
            {
                if(token.IsCancellationRequested)
                {
                    return result;
                }

                var httpVerb = result.AddMessages(
                    GetRouteHTTPVerb(ast, route)
                );

                var relPath = string.Join("/", route.APIRelPath);

                var routeCode =result.AddMessages(
                    EmitRouteHandlerCode(
                        ast, 
                        httpVerb, 
                        relPath,
                        route
                    )
                );

                if(!string.IsNullOrEmpty(routeCode))
                {   
                    sb.AppendLine(routeCode);
                }
            }

            sb.AppendLine($"return {ExpressAppNameLexeme}; }}"); // [dho] end of app factory declaration - 25/02/20

            sb.AppendLine($"export default {AppFactoryNameLexeme}");
            
            result.Value = sb.ToString();

            return result;
        }

        static Result<string> GetRouteHTTPVerb(RawAST ast, ServerInlining.ServerRouteInfo route)
        {
            var result = new Result<string>
            {
                Value = "get"
            };

            if (route.Handler.Kind == SemanticKind.FunctionDeclaration)
            {
                var handler = ASTNodeFactory.FunctionDeclaration(ast, route.Handler);

                var parameters = handler.Parameters;

                // [dho] TODO choose HTTP method based on more intelligent parameter requirements and function role! - 22/09/19
                // string expressHTTPVerb = parameters.Length > 0 ? "post" : "get";

                if (route.HTTPVerbAnnotation != null)
                {
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
                                result.Value = ASTNodeFactory.StringConstant(ast, (DataNode<string>)invArgValue).Value.ToLower();
                                return result;
                            }
                        }
                    }

                    result.AddMessages(
                        new NodeMessage(MessageKind.Error,
                            "HTTP annotation must contain string constant expression", httpExp)
                        {
                            Hint = GetHint(httpExp.Origin),
                        }
                    );
                    
                }

                // [dho] TODO choose HTTP method based on more intelligent parameter requirements and function role! - 22/09/19
                result.Value = parameters.Length > 0 ? "post" : "get";
            }

            return result;
        }

   
        /** [dho] emits a route handler of the form:
            ```
            app.post('/foo', async (req, res) => { ... })
            ```
            - 25/02/20
        */
        private static Result<string> EmitRouteHandlerCode(RawAST ast, string expressHTTPVerb, string relPath, ServerInlining.ServerRouteInfo route)
        {
            var result = new Result<string>();

            var sb = new System.Text.StringBuilder();

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

            // [dho] lambda declaration - 27/12/19
            sb.AppendLine($"{ExpressAppNameLexeme}.{expressHTTPVerb}('/{relPath}', async (req, res) => {{");

    
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

            var paramNameLexemes = ASTNodeHelpers.ExtractParameterNameLexemes(ast, parameters);
            var reqParamAccesses = new string[paramNameLexemes.Length];

            if (parameters.Length > 0)
            {
                sb.AppendLine(
                    $"const {RouteParamsSourceNameLexeme}=" + (expressHTTPVerb == "post" ? "req.body" : "{...req.query, req.params }") + " ?? {};"
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
    const data = await {DelegatePrefixLexeme}.{string.Join(".", route.QualifiedHandlerName)}.apply({{ ...{ContextArgLexeme}, user, req, res }}, [{string.Join(",", reqParamAccesses)}]);

    if(!res.headersSent)
    {{
        res.statusCode = 200;

        res.json(data === void 0 ? {{ }} : data);
    }}
}} catch (error) {{
    {ErrorHandlerFunctionNameLexeme}('{string.Join(".", route.QualifiedHandlerName)}', error, req, res);
}}"
            );

            // [dho] end of `app.get(...)` - 27/12/19
            sb.AppendLine("})");


            result.Value = sb.ToString();

            return result;
        }
    }
}