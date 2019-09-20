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

//     public class SwiftSymbolVisibilityTransformer : ITransformer
//     {
//         protected readonly string[] DiagnosticTags;

//         public SwiftSymbolVisibilityTransformer()
//         {
//             DiagnosticTags = new[] { "transformer", "swift-symbol-visibility" };
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

//             System.Diagnostics.Debug.Assert(start.Kind == SemanticKind.Domain);

//             var ast = context.AST;

//             var domain = ASTNodeFactory.Domain(ast, start);

//             foreach(var cNode in domain.Components)
//             {
//                 // [dho] top level of each component - 28/06/19
//                 foreach(var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, cNode.ID))
//                 {
//                     if(child.Kind == SemanticKind.ExportDeclaration)
//                     {
//                         var exportDecl = ASTNodeFactory.ExportDeclaration(ast, child);

//                         // just extract the symbols and make them public


//                     }
//                     else if(LanguageSemantics.Swift.IsDeclaration(ast, child)) 
//                     {

//                     }
//                 }
//             }


//             return result;
//         }
//     }
// }