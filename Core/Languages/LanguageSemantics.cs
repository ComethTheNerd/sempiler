
namespace Sempiler.Languages
{
    using Sempiler.AST;
    using Sempiler.AST.Diagnostics;
    using System.Collections.Generic;
    using System.Threading;

    public class Scope 
    {
        public Node Start;
        public Dictionary<string, Node> Declarations;
        // public Dictionary<string, List<Node>> References;
        // public List<Scope> Children;

        // public Scope Parent { get => null; }
    
        public Scope(Node start)
        {
            Start = start;
            Declarations = new Dictionary<string, Node>();
            // References = new Dictionary<string, List<Node>>();
        }
    }

    public abstract class BaseLanguageSemantics
    {
        protected readonly Dictionary<SemanticKind, object> ScopeBoundaries;
        public BaseLanguageSemantics(Dictionary<SemanticKind, object> scopeBoundaries)
        {
            ScopeBoundaries = scopeBoundaries;
        }

        public Scope GetEnclosingScope(Session session, RawAST ast, Node node, Scope startScope, System.Threading.CancellationToken token)
        {
            var scope = default(Scope);

            var encScopeStartNode = GetEnclosingScopeStart(ast, node, token);

            if(encScopeStartNode != null)
            {
                scope = new Scope(encScopeStartNode);

                foreach(var kv in startScope.Declarations)
                {
                    scope.Declarations.Add(kv.Key, kv.Value);
                }

                var d = GetEnclosingScope(session, ast, encScopeStartNode, token)?.Declarations;

                if(d != null)
                {
                    foreach(var kv in GetEnclosingScope(session, ast, encScopeStartNode, token)?.Declarations)
                    {
                        if(!scope.Declarations.ContainsKey(kv.Key))
                        {
                            scope.Declarations.Add(kv.Key, kv.Value);
                        }
                    }
                }

            }

            return scope;
        }

        public Scope GetEnclosingScope(Session session, RawAST ast, Node node, System.Threading.CancellationToken token)
        {
            var scope = default(Scope);

            var encScopeStartNode = GetEnclosingScopeStart(ast, node, token);

            if(encScopeStartNode != null)
            {
                scope = new Scope(encScopeStartNode);

                // [dho] get ancestor declarations - 22/06/19
                var parentScope = GetEnclosingScope(session, ast, encScopeStartNode, token);

                if(parentScope != null)
                {
                    foreach(var kv in parentScope.Declarations)
                    {
                        scope.Declarations.Add(kv.Key, kv.Value);
                    }
                }

                ASTHelpers.PreOrderTraversal(session, ast, node, focus => {

                    if(IsDeclarationStatement(ast, focus))
                    {
                        var name = ASTHelpers.GetSingleMatch(ast, focus.ID, SemanticRole.Name);

                        if(name != null)
                        {
                            System.Diagnostics.Debug.Assert(name.Kind == SemanticKind.Identifier);

                            var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;

                            scope.Declarations[lexeme] = focus;
                        }

                    }

                    // [dho] only explore subtree if not a new scope - 22/06/19
                    return focus == node || !ScopeBoundaries.ContainsKey(focus.Kind);
                }, token);
            }
            
            return scope;
        }


        public Node GetEnclosingScopeStart(RawAST ast, Node node, System.Threading.CancellationToken token)
        {
            var parent = ASTHelpers.GetParent(ast, node.ID);

            while(parent != null)
            {
                if(ScopeBoundaries.ContainsKey(parent.Kind))
                {
                    return parent;
                }
                else
                {
                    parent = ASTHelpers.GetParent(ast, parent.ID);
                }
            }

            return null;
        }

        public List<Node> GetUnqualifiedReferenceMatches(Session session, RawAST ast, Node start, Scope startScope, string identifierLexeme, System.Threading.CancellationToken token)
        {
            var references = new List<Node>();

            if(!startScope.Declarations.ContainsKey(identifierLexeme))
            {
                return references;
            }

            ASTHelpers.PreOrderTraversal(session, ast, start, node => {
                
                var shouldExploreChildren = true;

                // if(node.Kind == SemanticKind.QualifiedAccess)
                // {
                //     // [dho] in the case of a qualified access we will just check the Incident (left side of dot)
                //     // because we are not interested in qualified names - 23/06/19
                //     var qaIncident = ASTNodeFactory.QualifiedAccess(ast, node).Incident;

                //     var isReferenceMatch = IsReferenceMatchAndShouldExploreChildren(session, ast, qaIncident, startScope, identifierLexeme, token).Item1;
                    
                //     if(isReferenceMatch)
                //     {
                //         // [dho] NOTE add the incident object, NOT the node - 23/06/19
                //         references.Add(qaIncident);
                //     }
                    
                //     shouldExploreChildren = false; // [dho] do NOT explore the qualified access any further - 23/06/19
                // }
                // else// if(!JavaTreatsAsDeclaration(ast, node))
                // {
                    var _ = IsReferenceMatchAndShouldExploreChildren(session, ast, node, startScope, identifierLexeme, token);

                    var isReferenceMatch = _.Item1;

                    if(isReferenceMatch)
                    {
                        references.Add(node);
                    }

                    shouldExploreChildren = _.Item2;
                // }

                // System.Console.WriteLine("TRAVERSAL " + node.Kind + " :: " + ASTHelpers.GetPosition(ast, node.ID).Role + " :: " + shouldExploreChildren);

                return shouldExploreChildren;

            }, token);


            return references;
        }

        private (bool, bool) IsReferenceMatchAndShouldExploreChildren(Session session, RawAST ast, Node node, Scope startScope, string identifierLexeme, System.Threading.CancellationToken token)
        {
            var isReferenceMatch = false;
            var shouldExploreChildren = true;

            if(ASTNodeHelpers.IsIdentifierWithName(ast, node, identifierLexeme) &&
                IsEligibleForReferenceMatch(ast, node))
            {
                // [dho] guard against us finding the original declaration in the tree 
                // and thinking that its a match - 23/06/19
                if(startScope.Declarations[identifierLexeme].ID != node.ID)
                {
                    var nestedScope = GetEnclosingScope(session, ast, node, startScope, token);

                    // [dho] is this a reference to the same declaration - 23/06/19
                    if(nestedScope.Declarations[identifierLexeme].ID == startScope.Declarations[identifierLexeme].ID)
                    {
                        isReferenceMatch = true;
                        shouldExploreChildren = false;
                    }
                    else
                    {
                        // [dho] no point exploring children because the declaration has been
                        // shadowed - 23/06/19
                        isReferenceMatch = false;
                        shouldExploreChildren = false;
                    }
                }
            }

            return (isReferenceMatch, shouldExploreChildren);
        }

        private bool IsEligibleForReferenceMatch(RawAST ast, Node node)
        {
            var pos = ASTHelpers.GetPosition(ast, node.ID);

            switch(pos.Role)
            {
                case SemanticRole.Component:
                case SemanticRole.Body:
                case SemanticRole.Scope:
                case SemanticRole.From:
                case SemanticRole.Label:
                case SemanticRole.Name:
                case SemanticRole.ParserName:
                case SemanticRole.Literal:
                    return false;

                default:{
                    // [dho] if this node is inside a (potentially nested) qualified access,
                    // ensure it is the leftmost symbol (incident) - 05/07/19
                    while(pos.Parent?.Kind == SemanticKind.QualifiedAccess)
                    {
                        if(pos.Role != SemanticRole.Incident)
                        {
                            return false;
                        }

                        pos = ASTHelpers.GetPosition(ast, pos.Parent.ID);
                    }

                    return true;
                }
            }
        }


        public class SymbolicDependency 
        {
            public Node Declaration;
            public Dictionary<string, List<Node>> References = new Dictionary<string, List<Node>>();
        }

  
        ///<summary>
        /// For a given node, what symbols are referenced inside it that come from an enclosing
        // scope
        ///</summary>
        public List<SymbolicDependency> GetSymbolicDependencies(Session session, Artifact artifact, RawAST ast, Node start, CancellationToken token)
        {
            var symbolicDependencyList = new List<SymbolicDependency>();

            var startScope = new Scope(start);

            var scope = GetEnclosingScope(session, ast, start, startScope, token);

            ASTHelpers.PreOrderTraversal(session, ast, start, node => {

                if(start != node)
                {
                    if(node.Kind == SemanticKind.Identifier && IsEligibleForReferenceMatch(ast, node))
                    {
                        var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)node).Lexeme;

                        var symbolicDependency = default(SymbolicDependency);

                        // [dho] do we know where this symbol was declared - 11/07/19
                        if(scope.Declarations.ContainsKey(lexeme))
                        {
                            var decl = scope.Declarations[lexeme];

                            // [dho] if the symbol declaration is not a child of the start node, then 
                            // consider the start node to be dependent on it - 11/07/19
                            if(!ASTHelpers.Contains(ast, start, decl.ID, token))
                            {
                                foreach(var sd in symbolicDependencyList)
                                {
                                    if(sd.Declaration?.ID == decl.ID)
                                    {
                                        symbolicDependency = sd;
                                        break;
                                    }
                                }

                                if(symbolicDependency == null)
                                {
                                    symbolicDependency = new SymbolicDependency()
                                    {
                                        Declaration = decl
                                    };

                                    symbolicDependencyList.Add(symbolicDependency);
                                }                            
                            }
                        }
                        else
                        {
                            // [dho] cannot find the declaration, so may be a global or implicity symbol that start node depends on - 11/07/19
                        }         

                        if(symbolicDependency != null)
                        {
                            if(!symbolicDependency.References.ContainsKey(lexeme))
                            {
                                var root = ASTHelpers.GetRoot(ast);
                                var sc = new Scope(root);
                                sc.Declarations[lexeme] = symbolicDependency.Declaration;

                                symbolicDependency.References[lexeme] = 
                                    GetUnqualifiedReferenceMatches(session, ast, root, sc, lexeme, token);//new List<Node>();
                            }

                            // symbolicDependency.References[lexeme].Add(node);        
                        }
                    }
                }

                return true;
            }, token);

            return symbolicDependencyList;
        }

        public bool IsStaticallyComputable(Session session, Artifact artifact, RawAST ast, Node node, CancellationToken token)
        {
            if(node.Kind == SemanticKind.Invocation || 
                node.Kind == SemanticKind.ParameterDeclaration || 
                node.Kind == SemanticKind.ViewDeclaration || 
                IsConstruction(ast, node))
            {
                return false;
            }

            var dependencies = GetSymbolicDependencies(session, artifact, ast, node, token);

            foreach(var dependency in dependencies)
            {
                if(dependency.Declaration != null &&
                    !IsStaticallyComputable(session, artifact, ast, dependency.Declaration, token))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsConstruction(RawAST ast, Node node)
        {
            // [dho] TODO CLEANUP HACK!! - 11/07/19
            return node.Kind.ToString().IndexOf("Construction") > -1;
        }

        ///<summary>Do the language semantics dictate that the given node is in a position
        /// that will mean it is treated as a declaration (eg. class, interface etc.)</summary>
        public abstract bool IsDeclarationStatement(RawAST ast, Node node);

        ///<summary>Do the language semantics dictate that the given node is in a position
        /// that will mean it is treated as a value (eg. argument, rval etc.)</summary>
        public abstract bool IsValueExpression(RawAST ast, Node node);

        public abstract bool IsFunctionLikeDeclarationStatement(RawAST ast, Node node);

        public abstract bool IsInvocationLikeExpression(RawAST ast, Node node);


        ///<summary>
        /// Produces a list of all the explicitly provided exit points (return statements, single statement lambda bodies) 
        /// in the scope of start (ie. not including nested scopes)
        ///</summary>
        public List<Node> GetExplicitExits(Session session, RawAST ast, Node start, CancellationToken token)
        {
            var output = new List<Node>();

            // [dho] deal with the case where it is `() => x` and we want to report
            // `x` as the exit - 21/06/19
            if(start.Kind != SemanticKind.Block && ASTHelpers.GetParent(ast, start.ID)?.Kind == SemanticKind.LambdaDeclaration)
            {
                output.Add(start);
            }
            else
            {
                var nodesToSearch = new Stack<Node>();

                nodesToSearch.Push(start);

                while(nodesToSearch.Count > 0)
                {
                    if(token.IsCancellationRequested) return output;

                    var node = nodesToSearch.Pop();

                    if(node == null) continue;

                    if(node.Kind == SemanticKind.FunctionTermination)
                    {
                        output.Add(node);
                    }
                    else if(node.Kind == SemanticKind.Block)
                    {
                        // [dho] search all nodes inside the block - 14/06/19
                        foreach(var n in ASTNodeFactory.Block(ast, node).Content) nodesToSearch.Push(n);
                    }
                    // [dho] we don't care about nested functions as they are a different `return` scope - 14/06/19
                    else if(!IsFunctionLikeDeclarationStatement(ast, node))
                    {
                        var b = ASTHelpers.GetSingleMatch(ast, node.ID, SemanticRole.Body);
                    
                        if(b != null) // [dho] loops and clauses may have a body, so this captures them - 14/06/19
                        {
                            nodesToSearch.Push(b);
                        }
                        else if(node.Kind == SemanticKind.PredicateJunction)
                        {
                            var pred = ASTNodeFactory.PredicateJunction(ast, node);

                            nodesToSearch.Push(pred.TrueBranch);
                            nodesToSearch.Push(pred.FalseBranch);
                        }
                        else if(node.Kind == SemanticKind.MatchJunction)
                        {
                            // [dho] search the clauses in the `switch` statement - 14/06/19
                            foreach(var n in ASTNodeFactory.MatchJunction(ast, node).Clauses) nodesToSearch.Push(n);
                        }
                    }
                }
            }

            return output;
        }
    }
}