// using Sempiler.AST;
// using Sempiler.AST.Diagnostics;
// using Sempiler.Diagnostics;
// using Sempiler.Languages;
// using System.Runtime.CompilerServices;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;

// namespace Sempiler.Transformation
// {
//     using static Sempiler.Diagnostics.DiagnosticsHelpers;
//     using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

//     public class SwiftParameterTransformer : ITransformer
//     {
//         protected readonly string[] DiagnosticTags;

//         public SwiftParameterTransformer()
//         {
//             DiagnosticTags = new[] { "transformer", "swift-parameter-transformer" };
//         }

//         public Task<Diagnostics.Result<RawAST>> Transform(Session session, Artifact artifact, RawAST ast, CancellationToken token)
//         {
//             var result = new Result<RawAST>();

//             // var clonedAST = ast.Clone();

//             var context = new Context
//             {
//                 Artifact = artifact,
//                 Session = session,
//                 AST = ast//clonedAST
//             };

//             var root = ASTHelpers.GetRoot(/* clonedAST */ ast);

//             result.AddMessages(TransformNode(session, root, context, token));

//             // if (!HasErrors(result))
//             // {
//             //     result.Value = clonedAST;
//             // }

//             result.Value = ast;

//             return Task.FromResult(result);
//         }

//         private Result<object> TransformNode(Session session, Node start, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var ast = context.AST;

//             ASTHelpers.PreOrderTraversal(session, ast, start, node =>
//             {
//                 if(node.Kind == SemanticKind.Meta)
//                 {
//                     var meta = ASTNodeFactory.Meta(ast, (DataNode<MetaFlag>)node);

//                     if(meta.Flags == MetaFlag.Optional && meta.Parent.Kind == SemanticKind.ParameterDeclaration)
//                     {
//                         var paramDecl = ASTNodeFactory.ParameterDeclaration(ast, meta.Parent);


//                     }
//                 }

//                 if(node.Kind == SemanticKind.ParameterDeclaration)
//                 {
//                     var paramDecl = ASTNodeFactory.ParameterDeclaration(ast, node);

//                     foreach(var m in paramDecl.Meta)
//                     {
//                         if(m.Kind == SemanticKind.Meta)
//                         {
//                             var meta = ASTNodeFactory.Meta(ast, (DataNode<MetaFlag>)m);

//                             if(meta.Flags == MetaFlag.Optional)
//                             {
                                
//                             }
//                         }
//                     }
//                 }

//                 return true;
//             }, token);


//             return result;
//         }
//     }
// }