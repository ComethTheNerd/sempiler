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
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

    public class SwiftEnforceMutatingMethodsTransformer : ITransformer
    {
        protected readonly string[] DiagnosticTags;

        public SwiftEnforceMutatingMethodsTransformer()
        {
            DiagnosticTags = new[] { "transformer", "swift-enforce-mutating-methods-transformer" };
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

        private Result<object> TransformNode(Session session, Node start, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var ast = context.AST;

            ASTHelpers.PreOrderTraversal(session, ast, start, node =>
            {
                if(node.Kind == SemanticKind.MethodDeclaration)
                {   
                    var methodDecl = ASTNodeFactory.MethodDeclaration(ast, node);

                    var parentIsStruct = (MetaHelpers.ReduceFlags(ASTNodeFactory.ObjectTypeDeclaration(ast, methodDecl.Parent)) & MetaFlag.ValueType) > 0;

                    if(parentIsStruct)
                    {
                        var flags = MetaHelpers.ReduceFlags(methodDecl);

                        if((flags & (MetaFlag.Static /* | MetaFlag.Mutation */)) == 0)
                        {
                            var modifier = NodeFactory.Modifier(ast, methodDecl.Origin, "mutating");

                            ASTHelpers.Connect(ast, methodDecl.ID, new [] { modifier.Node }, SemanticRole.Modifier);
                        }
                    }

                }

                return true;
            }, token);


            return result;
        }
    }
}