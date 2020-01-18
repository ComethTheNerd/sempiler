
using Sempiler.AST;
using Sempiler.CTExec;
using System.Collections.Generic;
using System.Threading;

namespace Sempiler.Core 
{
    public static class MetaProgramming
    {
        public struct MetaProgrammingInfo
        {
            public List<Directive> BridgeIntentDirectives;
            public List<Directive> CTDirectives;
        }

        public static MetaProgrammingInfo GetMetaProgrammingInfo(Session session, RawAST ast, Node node, CancellationToken token)
        {
            var mpInfo = new MetaProgrammingInfo
            {
                BridgeIntentDirectives = new List<Directive>(),
                CTDirectives = new List<Directive>()
            };

            PopulateMetaProgrammingInfo(session, ast, node, token, ref mpInfo);

            return mpInfo;
        }

        public static MetaProgrammingInfo GetMetaProgrammingInfo(Session session, RawAST ast, IEnumerable<ASTNode> roots, CancellationToken token)
        {
            var mpInfo = new MetaProgrammingInfo
            {
                BridgeIntentDirectives = new List<Directive>(),
                CTDirectives = new List<Directive>()
            };

            foreach (var nodeWrapper in roots)
            {
                if (token.IsCancellationRequested) break;

                PopulateMetaProgrammingInfo(session, ast, nodeWrapper.Node, token, ref mpInfo);
            }

            return mpInfo;
        }

        private static void PopulateMetaProgrammingInfo(Session session, RawAST ast, Node node, CancellationToken token, ref MetaProgrammingInfo mpInfo)
        {
            if (node.Kind == SemanticKind.Directive)
            {
                var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

                var name = directive.Name;

                // [dho] TODO store directive names centrally somewhere!! -15/05/19
                switch (name)
                {
                    case CTDirective.Emit: // [dho] compile time code generation - 15/05/19
                    case CTDirective.CodeExec: // [dho] compile time execution - 15/05/19
                        {
                            mpInfo.CTDirectives.Add(directive);
                        }
                        break;

                    default:
                        {
                            // [dho] bridged usage of another artifact in the session - 15/05/19
                            if (session.Artifacts.ContainsKey(name))
                            {
                                mpInfo.BridgeIntentDirectives.Add(directive);
                                // [dho] we don't need to examine the children because they are descendants
                                // of an artifact reference, which will be moved/transformed and then recursively
                                // processed in the context of the artifact it will reside in - 29/05/19
                                return;
                            }
                        }
                        break;
                }
            }

            foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, node.ID))
            {
                if (token.IsCancellationRequested) break;

                PopulateMetaProgrammingInfo(session, ast, child, token, ref mpInfo);
            }
        }
    }
}