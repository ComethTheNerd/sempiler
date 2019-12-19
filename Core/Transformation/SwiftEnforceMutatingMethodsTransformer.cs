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

            foreach(var mNode in ASTHelpers.QueryByKind(ast, SemanticKind.MethodDeclaration))
            {
                if(!ASTHelpers.IsLive(ast, mNode.ID)) continue;

                var methodDecl = ASTNodeFactory.MethodDeclaration(ast, mNode);
                var mNodeParent = methodDecl.Parent;

                System.Diagnostics.Debug.Assert(mNodeParent.Kind == SemanticKind.ObjectTypeDeclaration);

                var parent = ASTNodeFactory.ObjectTypeDeclaration(ast, mNodeParent);

                var parentIsStruct = (MetaHelpers.ReduceFlags(parent) & MetaFlag.ValueType) > 0;

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

            // if (!HasErrors(result))
            // {
            //     result.Value = clonedAST;
            // }

            result.Value = ast;

            return Task.FromResult(result);
        }
    }
}