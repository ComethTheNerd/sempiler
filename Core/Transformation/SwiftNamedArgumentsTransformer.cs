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

    public class SwiftNamedArgumentsTransformer : ITransformer
    {
        protected readonly string[] DiagnosticTags;

        public SwiftNamedArgumentsTransformer()
        {
            DiagnosticTags = new[] { "transformer", "swift-named-arguments-transformer" };
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

            foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.SpreadDestructuring))
            {
                if(!ASTHelpers.IsLive(ast, node.ID)) continue;
                
                result.AddMessages(
                    TransformSpreadDestructuring(session, node, context, token)
                );
            }

            // if (!HasErrors(result))
            // {
            //     result.Value = clonedAST;
            // }

            result.Value = ast;

            return Task.FromResult(result);
        }

        
        private Result<object> TransformSpreadDestructuring(Session session, Node node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var ast = context.AST;
            var spread = ASTNodeFactory.SpreadDestructuring(ast, node);

            // var replacementPoint = spread.Node;
            // var parent = spread.Parent;
            // {
            //     while(parent?.Kind == SemanticKind.InvocationArgument || parent?.Kind == SemanticKind.Association) 
            //     {   
            //         replacementPoint = parent;
            //         parent = ASTHelpers.GetParent(ast, parent.ID);
            //     }
            // }

            var parent = spread.Parent;

            if (parent.Kind == SemanticKind.InvocationArgument)
            {
                var arguments = result.AddMessages(AsNamedArguments(session, ast, node, context, token));

                // [dho] NOTE replace the spread parent Node if it is an invocation argument - 04/07/19
                ASTHelpers.Replace(ast, parent.ID, arguments.ToArray());
            }
            else if(LanguageSemantics.Swift.IsInvocationLikeExpression(ast, parent))
            {
                var arguments = result.AddMessages(AsNamedArguments(session, ast, node, context, token));

                // [dho] NOTE replace the spread Node itself if it is in an invocation like expression - 04/07/19
                ASTHelpers.Replace(ast, spread.ID, arguments.ToArray());
            }

            return result;
        }

        public Result<List<Node>> AsNamedArguments(Session session, RawAST ast, Node node, Context context, CancellationToken token)
        {
            var result = new Result<List<Node>>();

            var arguments = result.Value = new List<Node>();

            if(node.Kind != SemanticKind.SpreadDestructuring)
            {
                return result;
            }

            var spread = ASTNodeFactory.SpreadDestructuring(ast, node);
            var subject = spread.Subject;

            if (subject.Kind == SemanticKind.DynamicTypeConstruction)
            {
                var dtc = ASTNodeFactory.DynamicTypeConstruction(ast, subject);

                var members = dtc.Members;

                foreach(var member in members)
                {
                    if (member.Kind == SemanticKind.FieldDeclaration)
                    {
                        var fieldDecl = ASTNodeFactory.FieldDeclaration(ast, member);
                        var name = fieldDecl.Name;
                        var init = fieldDecl.Initializer;

                        if(init?.Kind == SemanticKind.SpreadDestructuring)
                        {
                            result.AddMessages(
                                TransformSpreadDestructuring(session, init, context, token)
                            );

                            init = fieldDecl.Initializer;
                        }

                        var argument = NodeFactory.InvocationArgument(ast, member.Origin);

                        ASTHelpers.Connect(ast, argument.ID, new[] { name }, SemanticRole.Label);

                        ASTHelpers.Connect(ast, argument.ID, new[] { init }, SemanticRole.Value);

                        arguments.Add(argument.Node);
                    }
                    else
                    {
                        result.AddMessages(new NodeMessage(MessageKind.Error, $"Cannot convert '{member.Kind}' to named argument", member)
                        {
                            Hint = GetHint(member.Origin)
                        });
                    }

                }
            }


            return result;
        }
    }
}