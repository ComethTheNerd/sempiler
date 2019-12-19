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

            result.AddMessages(TransformNode(session, artifact, root, context, token));

            // if (!HasErrors(result))
            // {
            //     result.Value = clonedAST;
            // }

            result.Value = ast;

            return Task.FromResult(result);
        }

        protected Result<object> TransformNode(Session session, Artifact artifact, Node start, Context context, CancellationToken token)
        {
            var result = new Result<object>() { };

            var ast = context.AST;
            var childContext = ContextHelpers.Clone(context);

            foreach (var node in ASTHelpers.QueryByKind(ast, SemanticKind.ViewConstruction))
            {
                if(!ASTHelpers.IsLive(ast, node.ID)) continue;

                var viewConstruction = ASTNodeFactory.ViewConstruction(ast, node);

                result.AddMessages(
                    TransformViewConstruction(session, artifact, ast, viewConstruction, childContext, token)
                );
            }

            foreach (var node in ASTHelpers.QueryByKind(ast, SemanticKind.ViewDeclaration))
            {
                if(!ASTHelpers.IsLive(ast, node.ID)) continue;

                var viewDecl = ASTNodeFactory.ViewDeclaration(ast, node);

                result.AddMessages(
                    TransformViewDeclaration(session, artifact, ast, viewDecl, childContext, token)
                );
            }


            return result;
        }
   
        protected abstract Result<object> TransformViewDeclaration(Session session, Artifact artifact, RawAST ast, ViewDeclaration node, Context context, CancellationToken token);
        protected abstract Result<object> TransformViewConstruction(Session session, Artifact artifact, RawAST ast, ViewConstruction node, Context context, CancellationToken token);
    }
}