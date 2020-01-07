
namespace Sempiler.Languages
{
    using Sempiler.AST;
    using Sempiler.AST.Diagnostics;
    using System.Collections.Generic;
    using System.Threading;

    public class Scope 
    {
        /// <summary>The Node that the symbols/declarations are relative to</summary>
        public Node Subject;
        public Dictionary<string, Node> Declarations;
        // public Dictionary<string, List<Node>> References;
        // public List<Scope> Children;

        // public Scope Parent { get => null; }
    
        public Scope(Node subject)
        {
            Subject = subject;
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

                ASTHelpers.PreOrderLiveTraversal(ast, node, focus => {

                    if(IsEligibleForSymbolResolutionTarget(ast, focus))
                    {
                        var name = ASTHelpers.GetSingleLiveMatch(ast, focus.ID, SemanticRole.Name);

                        if(name != null)
                        {
                            if(name.Kind == SemanticKind.Identifier)
                            {
                                AddSymbolToScope(ast, scope, name, focus);
                            }
                            else if(name.Kind == SemanticKind.EntityDestructuring)
                            {
                                var entityDestructuring = ASTNodeFactory.EntityDestructuring(ast, name);

                                AddSubsetDestructuringSymbolsToScope(ast, scope, entityDestructuring, token);
                            }
                            else if(name.Kind == SemanticKind.CollectionDestructuring)
                            {
                                var collectionDestructuring = ASTNodeFactory.CollectionDestructuring(ast, name);

                                AddSubsetDestructuringSymbolsToScope(ast, scope, collectionDestructuring, token);
                            }
                            // else
                            // {
                            //     int i = 0;
                            // }
                        }
                    }

                    // [dho] only explore subtree if not a new scope - 22/06/19
                    return focus == node || !ScopeBoundaries.ContainsKey(focus.Kind);
                }, token);
            }
            
            return scope;
        }

        private void AddSymbolToScope(RawAST ast, Scope scope, Node symbol, Node decl)
        {
            var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)symbol).Lexeme;

            scope.Declarations[lexeme] = decl;
        }

        private void AddSubsetDestructuringSymbolsToScope(RawAST ast, Scope scope, SubsetDestructuring subsetDestructuring, CancellationToken token)
        {
            foreach(var member in subsetDestructuring.Members)
            {
                if(member != null)
                {
                    System.Diagnostics.Debug.Assert(member.Kind == SemanticKind.DestructuredMember);

                    var name = ASTNodeFactory.DestructuredMember(ast, member).Name;

                    if(name.Kind == SemanticKind.Identifier)
                    {
                        // [dho] TODO CHECK `{ x } = ...;` we are saying `x` is the symbol AND declaration? - 23/09/19
                        AddSymbolToScope(ast, scope, name, name);
                    }
                    else if(name.Kind == SemanticKind.ReferenceAliasDeclaration)
                    {
                        var refAliasDeclName = ASTNodeFactory.ReferenceAliasDeclaration(ast, name).Name;

                        if(refAliasDeclName.Kind == SemanticKind.Identifier)
                        {
                            // [dho] TODO CHECK `{ x } = ...;` we are saying `x` is the symbol AND declaration? - 23/09/19
                            AddSymbolToScope(ast, scope, refAliasDeclName, refAliasDeclName);
                        }
                        else if(refAliasDeclName.Kind == SemanticKind.EntityDestructuring)
                        {
                            var entityDestructuring = ASTNodeFactory.EntityDestructuring(ast, refAliasDeclName);

                            AddSubsetDestructuringSymbolsToScope(ast, scope, entityDestructuring, token);
                        }
                        else if(refAliasDeclName.Kind == SemanticKind.CollectionDestructuring)
                        {
                            var collectionDestructuring = ASTNodeFactory.EntityDestructuring(ast, refAliasDeclName);

                            AddSubsetDestructuringSymbolsToScope(ast, scope, collectionDestructuring, token);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, 
                                $"Unhandled reference alias declaration name kind '{refAliasDeclName.Kind}' found whilst adding symbols to scope from subset destructuring");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, 
                                $"Unhandled destructured member name kind '{name.Kind}' found whilst adding symbols to scope from subset destructuring");
                    }
                }
            }
        }

        public Node GetEnclosingScopeStart(RawAST ast, Node node, CancellationToken token)
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

    
        ///<summary>[dho] Finds all occurrences in scope whereby the given symbols are referenced in a left to right sequence (ie. qualified access pattern) - 02/02/19</summary>
        public List<Node> GetUnqualifiedReferenceMatches(Session session, RawAST ast, Node start, Scope startScope, List<string> ltrSymbols, System.Threading.CancellationToken token)
        {
            if(ltrSymbols.Count == 0)
            {
                return new List<Node>();
            }
            else 
            {
                var first = ltrSymbols[0];
                var references = GetUnqualifiedReferenceMatches(session, ast, start, startScope, first, token);

                if(ltrSymbols.Count == 1)
                {
                    return references;
                }

                var matches = new List<Node>(references.Count);

                // [dho] we have to now detect whether the matches on the leftmost symbol were part of a qualified access
                // pattern matching the rest of the symbols in the given sequence - 02/02/19
                foreach(var reference in references)
                {
                    var pos = ASTHelpers.GetPosition(ast, reference.ID);
                    var parent = pos.Parent;

                    if(parent.Kind == SemanticKind.QualifiedAccess)
                    {
                        System.Diagnostics.Debug.Assert(pos.Role == SemanticRole.Subject);

                        // [dho] get outermost QA because matching on the first lexeme we care about will have given us
                        // a handle to the innermost QA, eg `x.y` in `x.y.z.a.b.c` (becuase it will be parsed as `((((x.y).z).a).b).c`- 01/01/20
                        while(parent.Kind == SemanticKind.QualifiedAccess)
                        {
                            var p = ASTHelpers.GetParent(ast, parent.ID);

                            if(p.Kind == SemanticKind.QualifiedAccess)
                            {
                                parent = p;
                            }
                        }

                        if(ASTNodeHelpers.IsQualifiedAccessOfLexemesLTR(
                            ASTNodeFactory.QualifiedAccess(ast, parent), 
                            ltrSymbols, 
                            1 /* start index because we have accounted for the 0th! */)
                        )
                        {
                            matches.Add(parent);
                        }
                    }
                }

                return matches;
            }
        }

        ///<summary>[dho] Finds all occurrences in scope whereby the given symbol is referenced - 02/02/19</summary>
        public List<Node> GetUnqualifiedReferenceMatches(Session session, RawAST ast, Node start, Scope startScope, string symbol, System.Threading.CancellationToken token)
        {
            var references = new List<Node>();

            if(!startScope.Declarations.ContainsKey(symbol))
            {
                return references;
            }

            ASTHelpers.PreOrderLiveTraversal(ast, start, node => {
                
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
                    var _ = IsReferenceMatchAndShouldExploreChildren(session, ast, node, startScope, symbol, token);

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
                var decl = startScope.Declarations[identifierLexeme];

                // [dho] guard against us finding the original declaration in the tree 
                // and thinking that its a match - 23/06/19
                if(decl != null && decl.ID != node.ID)
                {
                    var nestedScope = GetEnclosingScope(session, ast, node, startScope, token);

                    // [dho] is this a reference to the same declaration - 23/06/19
                    if(nestedScope.Declarations[identifierLexeme]?.ID == decl.ID)
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
            ///<summary>The declaration of the symbol, or null if the declaration for the symbol was not found in the AST - 23/09/19</summary>
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

            // var startScope = new Scope(start);
            // if(start.ID.EndsWith("$1007"))
            // {
            //     int i = 0;
            // }

            // var scope = GetEnclosingScope(session, ast, start, startScope, token);
            
            // if(start.ID.EndsWith("_$857") || start.ID.EndsWith("_$858") || start.ID.EndsWith("_$859"))
            // {
                // System.Console.WriteLine("GET SYM DEPS " + start.ID + "   " + start.Kind);
            // }
            ASTHelpers.PreOrderLiveTraversal(ast, start, node => {
                // System.Console.WriteLine("NODE ID " + node.ID);
                // if(node.ID.EndsWith("_$1006") || node.ID.EndsWith("_$1007") || node.ID.EndsWith("_$1008"))
                // {
                //     System.Console.WriteLine("CALLBACK " + node.ID + "   " + node.Kind + ASTHelpers.QueryEdges(ast, node.ID, x=>true).Length);
                //     int i = 0;
                // }

                // _scope_boundaries();



                // if(node.ID.EndsWith("_$858"))
                // {
                //     int i = 0;
                // }
                if(start != node)
                {
                    if(node.Kind == SemanticKind.Identifier && IsEligibleForReferenceMatch(ast, node))
                    {
                        var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)node).Lexeme;

                        var symbolicDependency = default(SymbolicDependency);
                        
                        // [dho] TODO OPTIMIZE originally we were getting the scope from the start node
                        // of `GetSymbolicDependencies`, but this means any symbols between that start point,
                        // and some arbitrarily nested identifier will not be added to the Scope, and not be resolvable.
                        // However, I think we can do better than this fix of getting the scope every time..
                        // maybe incrementally adding to the scope as we traverse? - 23/09/19
                        var scope = GetEnclosingScope(session, ast, node, new Scope(node), token);

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

                                // [dho] check we have not accounted for this dependency already - 23/09/19
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
                            // System.Console.WriteLine("UNRESOLVED " + lexeme);
                            // [dho] TODO should we use `Unknown` as a new Node type for cases where the declaration
                            // was not found, instead of `null`? - 23/09/19
                            symbolicDependency = new SymbolicDependency()
                            {
                                // [dho] symbol declaration not found in AST - 23/09/19
                                Declaration = null
                            };

                            symbolicDependencyList.Add(symbolicDependency);
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
            // System.Console.WriteLine("DONE SYM DEPS " + start.ID);
            return symbolicDependencyList;
        }

        // [dho] can this node be used entirely at compile time (statically), ie. doesn't
        // require any dynamism - 27/11/19
        // public bool IsCTComputable(Session session, Artifact artifact, RawAST ast, Node node, CancellationToken token)
        // {
        //     if(MetaHelpers.HasFlags(ast, node, MetaFlag.CTExec))
        //     {
        //         return true;
        //     }
            
        //     // if(node.Kind == SemanticKind.Identifier || 
        //     //     node.Kind == SemanticKind.StringConstant ||
        //     //     node.Kind == SemanticKind.NumericConstant || 
        //     //     node.Kind == SemanticKind.Null || 
        //     //     node.Kind == SemanticKind.BooleanConstant
        //     // )
        //     // {
        //     //     return true;
        //     // }

        //     // if(node.Kind == SemanticKind.InterimSuspension)
        //     // {
        //     //     var operand = ASTNodeFactory.InterimSuspension(ast, node).Operand;

        //     //     return IsCTComputable(
        //     //         session, artifact, ast, operand, token
        //     //     );
        //     // }

        //     // if(node.Kind == SemanticKind.QualifiedAccess)
        //     // {
        //     //     var qa = ASTNodeFactory.QualifiedAccess(ast, node);

        //     //     // [dho] just need to check if the leftmost identifier
        //     //     // is a CT computable 
        //     //     foreach(var (child, hasNext) in ASTNodeHelpers.IterateQualifiedAccessLTR(qa))
        //     //     {
        //     //         if(!IsCTComputable(session, artifact, ast, child, token))
        //     //         {
        //     //             return false;
        //     //         }
        //     //         break;
        //     //     }

        //     //     return true;
        //     // }


        //     if(node.Kind == SemanticKind.ParameterDeclaration || 
        //         node.Kind == SemanticKind.ViewDeclaration || 
        //         IsConstruction(ast, node))
        //     {
        //         return false;
        //     }

        //     // if(ASTNodeHelpers.GetLexeme(node) == "admin")
        //     // {
        //     //     int i = 0;
        //     // }


        //     // var pos = ASTHelpers.GetPosition(ast, node.ID);

        //     // System.Diagnostics.Debug.Assert(pos.Index > -1);

        //     // if(pos.Role == SemanticRole.Clause && pos.Parent.Kind == SemanticKind.ImportDeclaration)
        //     // {
        //     //     return false;
        //     // }


        //     var dependencies = GetSymbolicDependencies(session, artifact, ast, node, token);


        //     System.Console.WriteLine("\n\n🍀 🍀 LISTING DEPENDENCIES FOR " + node.ID + " : " + node.Kind);
        //     System.Console.WriteLine(ASTNodeHelpers.GetLexeme(node));

        //     System.Console.WriteLine("\nSTART:");

        //     // if(node.Kind == SemanticKind.QualifiedAccess)
        //     // {
        //     //     var qa = ASTNodeFactory.QualifiedAccess(ast, node);

        //     //     foreach(var item in ASTNodeHelpers.IterateQualifiedAccessLTR(qa))
        //     //     {
        //     //         int ii = 0;
        //     //     }

        //     //     int i = 0;
        //     // }
            
        //     foreach(var dependency in dependencies)
        //     {
        //         if(dependency.Declaration == null)
        //         {
        //             continue;
        //         }
        //         System.Console.Write("🔥");
        //         ASTNodeHelpers.PrintNode(dependency.Declaration);
        //     }
        //     System.Console.WriteLine("\nEND!🌕\n\n");

        //     foreach(var dependency in dependencies)
        //     {

        //         var decl = dependency.Declaration;

        //         // [dho] we could not resolve the declaration for the symbol - 23/09/19
        //         if(decl == null)
        //         {
        //             // [dho] maybe we did not find the declaration because this is a compiler
        //             // API, so let's check that case - 27/11/19
        //             foreach(var symbolName in dependency.References.Keys)
        //             {
        //                 if(!Sempiler.CTExec.CTAPISymbols.IsCTAPISymbolName(symbolName))
        //                 {
        //                     return false;
        //                 }
        //             }
        //         }
        //         else 
        //         {
        //             System.Console.WriteLine("🎃 RECURSING WITH dependency decl " + decl.ID);
        //             if(!IsCTComputable(session, artifact, ast, decl, token))
        //             {
        //                 return false;
        //             }
        //         }
        //     }

        //     return true;
        // }

        public bool IsConstruction(RawAST ast, Node node)
        {
            // [dho] TODO CLEANUP HACK!! - 11/07/19
            return node.Kind.ToString().IndexOf("Construction") > -1;
        }

        ///<summary>[dho] When resolving a symbol, should we consider the given `node`
        /// eligible as the target of that resolution (ie. is it the definition of a symbol) - 23/09/19</summary>
        public virtual bool IsEligibleForSymbolResolutionTarget(RawAST ast, Node node)
        {
            return IsDeclarationStatement(ast, node);
        }

        ///<summary>Do the language semantics dictate that the given node is in a position
        /// that will mean it is treated as a declaration (eg. class, interface etc.)</summary>
        public virtual bool IsDeclarationStatement(RawAST ast, Node node)
        {
            switch(node.Kind)
            {
                case SemanticKind.AccessorDeclaration:
                case SemanticKind.ConstructorDeclaration:
                case SemanticKind.DataValueDeclaration:
                case SemanticKind.DestructorDeclaration:
                case SemanticKind.EnumerationMemberDeclaration:
                case SemanticKind.EnumerationTypeDeclaration:
                case SemanticKind.GlobalDeclaration:
                case SemanticKind.ExportDeclaration:
                case SemanticKind.FieldDeclaration:
                case SemanticKind.FunctionDeclaration:
                case SemanticKind.ImportDeclaration:
                case SemanticKind.InterfaceDeclaration:
                case SemanticKind.LambdaDeclaration:
                case SemanticKind.MethodDeclaration:
                case SemanticKind.MutatorDeclaration:
                case SemanticKind.NamespaceDeclaration:
                case SemanticKind.ObjectTypeDeclaration:
                case SemanticKind.ParameterDeclaration:
                case SemanticKind.PropertyDeclaration:
                case SemanticKind.ReferenceAliasDeclaration:
                case SemanticKind.TypeAliasDeclaration:
                case SemanticKind.TypeParameterDeclaration:
                case SemanticKind.ViewDeclaration:
                    return true;

                default:
                    return false;
            }
        }

        ///<summary>Do the language semantics dictate that the given node is in a position
        /// that will mean it is treated as a value (eg. argument, rval etc.)</summary>
        public abstract bool IsValueExpression(RawAST ast, Node node);

        public virtual bool IsFunctionLikeDeclarationStatement(RawAST ast, Node node)
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

        public virtual bool IsInvocationLikeExpression(RawAST ast, Node node)
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
                        var b = ASTHelpers.GetSingleLiveMatch(ast, node.ID, SemanticRole.Body);
                    
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