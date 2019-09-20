// using Sempiler.AST;
// using Sempiler.AST.Diagnostics;
// using Sempiler.Diagnostics;
// using System.Runtime.CompilerServices; 
// using System.Threading;
// using System.Threading.Tasks;

// namespace Sempiler.Transformation
// {
//     using static Sempiler.Diagnostics.DiagnosticsHelpers;
//     using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

//     public class AndroidArtifactBridgingTransformer : ITransformer
//     {
//         protected readonly string[] DiagnosticTags;

//         private bool RequiresVolleyImport;
//         private bool HasVolleyImportAlready;
//         private bool ImportServerLib;

//         public AndroidArtifactBridgingTransformer()
//         {
//             DiagnosticTags = new [] { "transformer", "androidbridging" };
//         }

//         public Task<Diagnostics.Result<RawAST>> Transform(Session session, Artifact artifact, RawAST ast, CancellationToken token)
//         {
//             var result = new Result<RawAST>();

//             var clonedAST = ast.Clone();

//             var context = new Context 
//             {
//                 Artifact = artifact,
//                 Session = session,
//                 AST = clonedAST
//             };

//             var root = ASTHelpers.GetRoot(clonedAST);

//             var transformedRoot = result.AddMessages(TransformNode(root, context, token));

//             if(!HasErrors(result))
//             {
//                 result.Value = clonedAST;
//             }

//             return Task.FromResult(result);
//         }

//         public virtual Result<Node> TransformNode(Node node, Context context, CancellationToken token)
//         {
//             if(node.Kind == SemanticKind.Domain)
//             {
//                 var result = new Result<Node>();

//                 var domain = ASTNodeFactory.Domain(context.AST, node);

//                 var c = ContextHelpers.Clone(context);
//                 c.Domain = domain;

//                 ImportServerLib = false;

//                 result.AddMessages(TransformChildren(domain, c, token));

//                 if(ImportServerLib)
//                 {
//                     // [dho] add helpers for server communication
//                     x
//                 }

//                 return result;
//             }


//             if(node.Kind == SemanticKind.Component)
//             {
//                 var result = new Result<Node>();

    
//                 var component = ASTNodeFactory.Component(context.AST, (DataNode<string>)node);

//                 var c = ContextHelpers.Clone(context);
//                 c.Component = component;

//                 RequiresVolleyImport = false;
//                 HasVolleyImportAlready = false;

//                 result.AddMessages(TransformChildren(component, c, token));

//                 if(RequiresVolleyImport && !HasVolleyImportAlready)
//                 {
//                     // add an edge here to import Volley
//                 }

//                 return result;
//             }

//             if(node.Kind == SemanticKind.ImportDeclaration)
//             {
//                 var importDecl = ASTNodeFactory.ImportDeclaration(context.AST, node);

//                 if(!HasVolleyImportAlready)
//                 {
//                     HasVolleyImportAlready = importDecl.IsVolleyImport(); // somehow
//                 }

//                 return TransformChildren(node, context, token);
//             }

//             if(node.Kind == SemanticKind.Directive)
//             {
//                 var directive = ASTNodeFactory.Directive(context.AST, (DataNode<string>)node);

//                 var directiveName = directive.Name;

//                 if(context.Session.Artifacts.ContainsKey(directiveName))
//                 {
//                     var targetArtifact = context.Session.Artifacts[directiveName];

//                     if(targetArtifact.Role == ArtifactRole.Server)
//                     {
//                         RequiresVolleyImport = true;
//                         ImportServerLib = true;

//                         /*
//                             var y = await #server someFunction("hello", 123);





//                             INPUT SOURCE:
//                             var x = await #server #db User.some(x.id = "foo")
                        
                            
//                             TRANSFORMED SOURCE:
//                             // need to know the url to send the server request to.. build arg?
//                             var x : User = makeRequest("nodeIDHere").get();



//                             // - create server makeRequest lib in client
//                             // - replace call site node with invocation node of server lib function 
//                             // - generate server side lambda code
//                             // - add component to server artifact
//                             // - need to know URLs/credentials for artifact being bridged to etc.
//                             // - server bundler (now)
//                             // - client bundler (android)


//                             ANDROID SPECIFIC STEPS:
//                             // * - inject volley in client
//                             // * - add INTERNET permission in manifest (subsequent separate step when inferring Manifest?)


//                             OTHER THOUGHTS:
//                             // - NetworkRequest primitive ... () instead of literal code?
//                             // - pros / cons?
//                             // - + when transpiling intent from TypeScript x-platform this type would be helpful


//                             // .. how does this all factor into compiling an app into a single class? do we do that too?





//                             // set url
//                             module.exports = (req, res) => {
//                                 try
//                                 {
//                                     res.end(handler())
//                                 }
//                                 catch(err)
//                                 {
//                                     res.status = ..;
//                                 }
//                             }
//                             function handler()
//                             {
//                                 // some database connection here - requires credentials/host etc
//                             }

//                         */


//                         // Needs Volley import
//                         // Might involve multiple statements, not just inline
//                         // And we need to interfere with the other AST to expose the route
//                         s
//                     }
//                     else
//                     {
//                         // error
//                         x
//                     }

//                 }
//             }

//             return TransformChildren(node, context, token);
//         }




//         protected Result<Node> P(Node node, CancellationToken token)
//         {

//         }

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         protected Result<Node> TransformChildren(ASTNode nodeWrapper, Context context, CancellationToken token) => TransformChildren(nodeWrapper.Node, context, token);

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         protected Result<Node> TransformChildren(Node node, Context context, CancellationToken token)
//         {
//             var result = new Result<Node>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             var newChildren = default(Node[]);

//             if(node.Children != null)
//             {
//                 ASTHelpers.ReplaceNode(ast, outgoingID, newNode);

//                 newChildren = new Node[node.Children.Length];

//                 for(int i = 0; i < node.Children?.Length; ++i)
//                 {
//                     var currentChild = node.Children[i];

//                     // [dho] child might be null if a type has a field that is optional,
//                     // but stores the value for that field as an Node - 27/09/18
//                     if(currentChild != null)
//                     {
//                         var r = TransformNode(currentChild, childContext, token);

//                         result.AddMessages(r.Messages);

//                         // [dho] I'm nervous about only doing this if we detect the child
//                         // has actually transformed (been replaced) in case `Transform___` functions
//                         // herein only return a property of the Node, and return a Node that ostensibly
//                         // looks unchanged because the ID is the same - 24/09/18
//                         newChildren[i] = r.Value;
//                     }
//                     else
//                     {
//                         newChildren[i] = null;
//                     }


                    
//                     if(token.IsCancellationRequested)
//                     {
//                         break;
//                     }
//                 }

//             }


//             if(!HasErrors(result))
//             {
//                 result.Value = new Node(newID, node.Kind, node.Origin, node.Meta.Clone(), newChildren);
//             }

//             return result;
//         }
    
//     }
// }