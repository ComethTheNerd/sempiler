
namespace Sempiler.Languages
{
    using Sempiler.AST;
    using System.Collections.Generic;

    public class TypeScriptLanguageSemantics : BaseLanguageSemantics
    {
        public TypeScriptLanguageSemantics() : base(new Dictionary<SemanticKind, object>()
        {
            { SemanticKind.Block, true },
            { SemanticKind.Component, true },
            { SemanticKind.ConstructorDeclaration, true },
            { SemanticKind.Domain, true },
            { SemanticKind.DoWhilePredicateLoop, true },
            { SemanticKind.DynamicTypeConstruction, true },
            { SemanticKind.DynamicTypeReference, true },
            { SemanticKind.EnumerationTypeDeclaration, true },
            { SemanticKind.ErrorTrapJunction, true },
            { SemanticKind.ErrorHandlerClause, true },
            { SemanticKind.ErrorFinallyClause, true },
            { SemanticKind.ForKeysLoop, true },
            { SemanticKind.ForMembersLoop, true },
            { SemanticKind.ForPredicateLoop, true },
            { SemanticKind.FunctionDeclaration, true },
            { SemanticKind.InterfaceDeclaration, true },
            { SemanticKind.LambdaDeclaration, true },
            { SemanticKind.MethodDeclaration, true },
            { SemanticKind.NamespaceDeclaration, true },
            { SemanticKind.ObjectTypeDeclaration, true },
            { SemanticKind.PredicateJunction, true },
            { SemanticKind.WhilePredicateLoop, true },
        })
        {

        }

        public override bool IsEligibleForSymbolResolutionTarget(RawAST ast, Node node)
        {
            if(node.Kind == SemanticKind.FieldDeclaration)
            {
                // [dho] fix for the situation where:
                // ```
                // const z = { x, y : () => { x } }
                // ```
                //
                // The use of `x` inside the dynamic type construction should
                // not be added to symbols in the nested scope... I'm pretty sure that's right? TODO CHECK! 
                // - 23/09/19
                return ASTHelpers.GetParent(ast, node.ID).Kind != SemanticKind.DynamicTypeConstruction;
            }

            return base.IsEligibleForSymbolResolutionTarget(ast, node);
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

        // public override bool IsFunctionLikeDeclarationStatement(RawAST ast, Node node)
        // {
        //     switch(node.Kind)
        //     {
        //         case SemanticKind.AccessorDeclaration:
        //         case SemanticKind.MutatorDeclaration:
        //         case SemanticKind.ConstructorDeclaration:
        //         case SemanticKind.DestructorDeclaration:
        //         case SemanticKind.MethodDeclaration:
        //         case SemanticKind.FunctionDeclaration:
        //         case SemanticKind.LambdaDeclaration:
        //             return true;
                
        //         default:
        //             return false;
        //     }
        // }

        // public override bool IsInvocationLikeExpression(RawAST ast, Node node)
        // {
        //     switch(node.Kind)
        //     {
        //         case SemanticKind.Invocation:
        //         case SemanticKind.NamedTypeConstruction:
        //             return true;
                
        //         default:
        //             return false;
        //     }
        // }
    }
}