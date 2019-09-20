
namespace Sempiler.Languages
{
    using Sempiler.AST;
    using System.Collections.Generic;

    public class JavaLanguageSemantics : BaseLanguageSemantics
    {
        public JavaLanguageSemantics() : base(new Dictionary<SemanticKind, object>()
        {
            { SemanticKind.Domain, true },
            { SemanticKind.Component, true },
            { SemanticKind.ObjectTypeDeclaration, true },
            { SemanticKind.InterfaceDeclaration, true },
            { SemanticKind.NamespaceDeclaration, true },
            { SemanticKind.EnumerationTypeDeclaration, true },
            { SemanticKind.PredicateJunction, true },
            { SemanticKind.WhilePredicateLoop, true },
            { SemanticKind.DoWhilePredicateLoop, true },
            { SemanticKind.ForMembersLoop, true },
            { SemanticKind.ForKeysLoop, true },
            { SemanticKind.ForPredicateLoop, true },
            { SemanticKind.ErrorTrapJunction, true },
            { SemanticKind.ErrorHandlerClause, true },
            { SemanticKind.ErrorFinallyClause, true },
            { SemanticKind.Block, true },
            { SemanticKind.ConstructorDeclaration, true },
            { SemanticKind.MethodDeclaration, true },
            { SemanticKind.FunctionDeclaration, true },
            { SemanticKind.LambdaDeclaration, true },
        })
        {

        }


        public override bool IsDeclarationStatement(RawAST ast, Node node)
        {
            // [dho] TODO CLEANUP HACK!! - 16/05/19
            return node.Kind.ToString().EndsWith("Declaration");
        }

        public override bool IsValueExpression(RawAST ast, Node node)
        {
            var pos = ASTHelpers.GetPosition(ast, node.ID);

            if(pos.Node != null)
            {
                switch(pos.Role)
                {
                    case SemanticRole.Initializer:
                    case SemanticRole.Condition:
                    case SemanticRole.Predicate:
                    case SemanticRole.Default:
                    case SemanticRole.Value:
                    case SemanticRole.Operand:
                    case SemanticRole.Size:
                    case SemanticRole.Argument:
                    case SemanticRole.Offset:
                        return true;
                }
            }

            return false;
        }

        public override bool IsFunctionLikeDeclarationStatement(RawAST ast, Node node)
        {
            switch(node.Kind)
            {
                case SemanticKind.AccessorDeclaration:
                case SemanticKind.MutatorDeclaration:
                case SemanticKind.ConstructorDeclaration:
                case SemanticKind.DestructorDeclaration:
                case SemanticKind.MethodDeclaration:
                case SemanticKind.FunctionDeclaration:
                case SemanticKind.LambdaDeclaration:
                    return true;
                
                default:
                    return false;
            }
        }

        public override bool IsInvocationLikeExpression(RawAST ast, Node node)
        {
            switch(node.Kind)
            {
                case SemanticKind.Invocation:
                case SemanticKind.NamedTypeConstruction:
                    return true;
                
                default:
                    return false;
            }
        }
    }
}