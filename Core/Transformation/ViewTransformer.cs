using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using Sempiler.Diagnostics;
using Sempiler.Languages;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sempiler.Transformation
{
    public abstract class ViewTransformer : ITransformer
    {
        protected readonly string[] DiagnosticTags;

        public ViewTransformer(string[] diagnosticTags)
        {
            DiagnosticTags = diagnosticTags;
        }

        public Task<Diagnostics.Result<RawAST>> Transform(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<RawAST>();

            // var clonedAST = ast.Clone();

            var context = new Context
            {
                Artifact = artifact,
                Session = session,
                AST = ast//clonedAST
            };

            var root = ASTHelpers.GetRoot(/* clonedAST */ ast);

            result.AddMessages(TransformNode(session, root, context, token));

            // if (!HasErrors(result))
            // {
            //     result.Value = clonedAST;
            // }

            result.Value = ast;

            return Task.FromResult(result);
        }

        protected Result<object> TransformNode(Session session, Node start, Context context, CancellationToken token)
        {
            var result = new Result<object>() { };

            var viewConstructions = new List<ViewConstruction>();
            var viewDeclarations = new List<ViewDeclaration>();
            var childContext = ContextHelpers.Clone(context);

            var ast = context.AST;

            ASTHelpers.PreOrderTraversal(session, ast, start, node =>
            {
                // if (node != null)
                // {
                    if (node.Kind == SemanticKind.ViewConstruction)
                    {
                        viewConstructions.Add(ASTNodeFactory.ViewConstruction(ast, node));

                        // return false; // [dho] do not explore this node any further - 17/06/19
                    }
                    else if (node.Kind == SemanticKind.ViewDeclaration)
                    {
                        viewDeclarations.Add(ASTNodeFactory.ViewDeclaration(ast, node));

                        // result.Value = false; // [dho] do not explore this node any further - 17/06/19
                    }

                    return true; // explore subtree
                // }


                // return false; // do not explore subtree


            }, token);

            foreach (var viewConstruction in viewConstructions)
            {
                result.AddMessages(
                    TransformViewConstruction(session, ast, viewConstruction, childContext, token)
                );
            }

            foreach (var viewDecl in viewDeclarations)
            {
                result.AddMessages(
                    TransformViewDeclaration(session, ast, viewDecl, childContext, token)
                );
            }


            return result;
        }
   
        protected abstract Result<object> TransformViewDeclaration(Session session, RawAST ast, ViewDeclaration node, Context context, CancellationToken token);
        protected abstract Result<object> TransformViewConstruction(Session session, RawAST ast, ViewConstruction node, Context context, CancellationToken token);
    }
}