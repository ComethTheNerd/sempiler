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

    public class SwiftNullabilityTransformer : ITransformer
    {
        protected readonly string[] DiagnosticTags;

        public SwiftNullabilityTransformer()
        {
            DiagnosticTags = new[] { "transformer", "swift-nullability-transformer" };
        }

        struct NullabilityAssertions 
        {
            public List<Node> DefinitelyNull;
            public List<Node> DefinitelyNotNull;
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

            result.AddMessages(TransformNullability(session, artifact, context, token));

            result.Value = ast;

            return Task.FromResult(result);
        }

        private Result<object> TransformNullability(Session session, Artifact artifact, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var ast = context.AST;

            var languageSemantics = LanguageSemantics.Of(artifact.TargetLang);

            if(languageSemantics == null)
            {
                result.AddMessages(new Message(MessageKind.Warning, $"Language semantics not configured for {artifact.TargetLang}, using TypeScript semantics as a fallback"));

                languageSemantics = LanguageSemantics.TypeScript;
            }

            foreach(var n in ASTHelpers.QueryByKind(ast, SemanticKind.PredicateJunction))
            {
                if(!ASTHelpers.IsLive(ast, n.ID)) continue;

                var ifStatement = ASTNodeFactory.PredicateJunction(ast, n);
                
                var predicate = ifStatement.Predicate;
                var trueBranch = ifStatement.TrueBranch;
                var falseBranch = ifStatement.FalseBranch;

                var r = InferNullabilityAssertions(session, predicate, languageSemantics, context, token);

                if(HasErrors(r))
                {
                    continue;
                }

                var na = result.AddMessages(r);

                // [dho] For if statements of the form `if(x === null){ ... } else { ... }`, 
                // we want to force unwrap any instances of `x` in the FALSE branch to `x!.` 
                // (where they are not already unwrapped with `x!` or `x?`) - 15/03/20
                result.AddMessages(
                    ForceUnwrap(session, na.DefinitelyNotNull, trueBranch, languageSemantics, context, token)
                );

                // [dho] NOTE this FALSE branch could also be an if statement, but if we have
                // `if(x === null){ ... } else if(y !== null){ ...}` then it is still correct
                // to force unwrap usages of symbols that are null in the true branch - 15/03/20
                // [dho] NOTE the additional check to ensure the predicate was not conditional, 
                // otherwise if it is we can not reliable assert the things that were definitely
                // null in the predicate are safe to unwrap, eg `if(x !== null && y !== null)`..
                // if only `x !== null`, then we can be sure that `y` isn't etc. - 15/03/20
                if(predicate.Kind != SemanticKind.LogicalAnd && predicate.Kind != SemanticKind.LogicalOr)
                {
                    result.AddMessages(
                        ForceUnwrap(session, na.DefinitelyNull, falseBranch, languageSemantics, context, token)
                    );
                }
            }

            foreach(var n in ASTHelpers.QueryByKind(ast, SemanticKind.PredicateFlat))
            {
                if(!ASTHelpers.IsLive(ast, n.ID)) continue;

                var ternaryExp = ASTNodeFactory.PredicateFlat(ast, n);
                
                var predicate = ternaryExp.Predicate;
                var trueValue = ternaryExp.TrueValue;
                var falseValue = ternaryExp.FalseValue;

                var r = InferNullabilityAssertions(session, predicate, languageSemantics, context, token);

                if(HasErrors(r))
                {
                    continue;
                }

                var na = result.AddMessages(r);
                  
                result.AddMessages(
                    ForceUnwrap(session, na.DefinitelyNotNull, trueValue, languageSemantics, context, token)
                );
            
                // [dho] NOTE the additional check to ensure the predicate was not conditional, 
                // otherwise if it is we can not reliable assert the things that were definitely
                // null in the predicate are safe to unwrap, eg `if(x !== null && y !== null)`..
                // if only `x !== null`, then we can be sure that `y` isn't etc. - 15/03/20
                if(predicate.Kind != SemanticKind.LogicalAnd && predicate.Kind != SemanticKind.LogicalOr)
                {
                    result.AddMessages(
                        ForceUnwrap(session, na.DefinitelyNull, falseValue, languageSemantics, context, token)
                    );
                }
            }


            return result;
        }

        /** [dho] infers from the given Node which symbols are asserted to be explicitly null or not null - 15/03/20 */
        private Result<NullabilityAssertions> InferNullabilityAssertions(Session session, Node node, BaseLanguageSemantics languageSemantics,  Context context, CancellationToken token)
        {
            var result = new Result<NullabilityAssertions>();

            var ast = context.AST;

            if(node.Kind == SemanticKind.LogicalAnd)
            {
                var logicalAnd = ASTNodeFactory.LogicalAnd(ast, node);
                var operands = logicalAnd.Operands;

                var na = new NullabilityAssertions
                {
                    DefinitelyNull = new List<Node>(),
                    DefinitelyNotNull = new List<Node>()
                };

                for(int i = 0; i < operands.Length; ++i)
                {
                    var operand = operands[i];
                    var r = InferNullabilityAssertions(session, operand, languageSemantics, context, token);

                    if(HasErrors(r))
                    {
                        continue;
                    }

                    var operandNA = result.AddMessages(r);

                    for(int j = i + 1; j < operands.Length; ++j)
                    {
                        result.AddMessages(
                            ForceUnwrap(session, operandNA.DefinitelyNotNull, operands[j], languageSemantics, context, token)
                        );
                    }

                    na.DefinitelyNull.AddRange(operandNA.DefinitelyNull);
                    na.DefinitelyNotNull.AddRange(operandNA.DefinitelyNotNull);
                }

                result.Value = na;
                return result;
            }
            else if(node.Kind == SemanticKind.StrictEquivalent)
            {
                var seq = ASTNodeFactory.StrictEquivalent(ast, node);
                var operands = seq.Operands;

                var nonNulls = ASTNodeHelpers.FilterNonKindMatches(operands, SemanticKind.Null);

                // [dho] there was a mix of non null and `null` expressions in the operands - 15/03/20
                if(nonNulls.Count > 0 && nonNulls.Count < operands.Length)
                {
                    result.Value = new NullabilityAssertions 
                    {
                        DefinitelyNull = nonNulls,
                        DefinitelyNotNull = new List<Node>()
                    };

                    return result;
                }
            }
            else if(node.Kind == SemanticKind.StrictNonEquivalent)
            {
                var sneq = ASTNodeFactory.StrictNonEquivalent(ast, node);
                var operands = sneq.Operands;

                var nonNulls = ASTNodeHelpers.FilterNonKindMatches(operands, SemanticKind.Null);

                // [dho] there was a mix of non null and `null` expressions in the operands - 15/03/20
                if(nonNulls.Count > 0 && nonNulls.Count < operands.Length)
                {
                    result.Value = new NullabilityAssertions 
                    {
                        DefinitelyNull = new List<Node>(),
                        DefinitelyNotNull = nonNulls
                    };

                    return result;
                }
            }

            result.Value = new NullabilityAssertions 
            {
                DefinitelyNull = new List<Node>(),
                DefinitelyNotNull = new List<Node>()
            };

            return result;
        }

        private Result<object> ForceUnwrap(Session session, List<Node> nodes, Node inScope, BaseLanguageSemantics languageSemantics, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if(inScope != null)
            {
                var ast = context.AST;

                var scope = new Scope(inScope);

                foreach(var node in nodes)
                {
                    if(node.Kind == SemanticKind.QualifiedAccess)
                    {
                        List<string> symbols = new List<string>();

                        var qa = ASTNodeFactory.QualifiedAccess(ast, node);

                        foreach(var (part, _) in ASTNodeHelpers.IterateQualifiedAccessLTR(qa))
                        {
                            if(part.Kind == SemanticKind.Identifier)
                            {
                                symbols.Add(ASTNodeFactory.Identifier(ast, (DataNode<string>)part).Lexeme);
                            }
                            else
                            {
                                return result;
                            }
                        }

                        scope.Declarations[symbols[symbols.Count - 1]] = ASTNodeHelpers.RHS(ast, qa.Node);
                    
                        var references = languageSemantics.GetUnqualifiedReferenceMatches(session, ast, scope.Subject, scope, symbols, token);

                        EnforceExplicitNullabilityUnwrap(session, references, languageSemantics, context, token);
                    }
                    else if(node.Kind == SemanticKind.Identifier)
                    {
                        var symbol = ASTNodeFactory.Identifier(ast, (DataNode<string>)node).Lexeme;

                        scope.Declarations[symbol] = node;

                        var references = languageSemantics.GetUnqualifiedReferenceMatches(session, ast, scope.Subject, scope, symbol, token);

                        EnforceExplicitNullabilityUnwrap(session, references, languageSemantics, context, token);
                    }
                }
            }

            return result;
        }

        private void EnforceExplicitNullabilityUnwrap(Session session, List<Node> references, BaseLanguageSemantics languageSemantics,  Context context, CancellationToken token)
        {
            var ast = context.AST;

            foreach (var reference in references)
            {
                if(IsEligibleToForceUnwrap(ast, reference))
                {
                    InsertNotNullWrapper(ast, reference);
                }
            }
        }

        private bool IsEligibleToForceUnwrap(RawAST ast, Node node)
        {
            var parent = ASTHelpers.GetParent(ast, node.ID);

            if(parent is null)
            {
                return false;
            }

            switch(parent.Kind)
            {
                case SemanticKind.NotNull:
                case SemanticKind.MaybeNull:
                    return false;

                case SemanticKind.StrictNonEquivalent:{
                    var sneq = ASTNodeFactory.StrictNonEquivalent(ast, node);
                    var operands = sneq.Operands;

                    var nonNulls = ASTNodeHelpers.FilterNonKindMatches(operands, SemanticKind.Null);

                    // [dho] check this is not a null test - 15/03/20
                    return nonNulls.Count == operands.Length;
                }

                case SemanticKind.StrictEquivalent:{
                    var seq = ASTNodeFactory.StrictEquivalent(ast, node);
                    var operands = seq.Operands;

                    var nonNulls = ASTNodeHelpers.FilterNonKindMatches(operands, SemanticKind.Null);

                    // [dho] check this is not a null test - 15/03/20
                    return nonNulls.Count == operands.Length;
                }

                default:
                    return true;
            }
        }

        private Node InsertNotNullWrapper(RawAST ast, Node node)
        {
            var notNull = NodeFactory.NotNull(ast, node.Origin);

            ASTHelpers.Replace(ast, node.ID, new [] { notNull.Node });

            ASTHelpers.Connect(ast, notNull.ID, new [] { node }, SemanticRole.Subject);

            return notNull.Node;
        }
    }
}