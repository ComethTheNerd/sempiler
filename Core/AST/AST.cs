using Sempiler.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;

// Abstract Semantic Tree!! NOT Syntax
namespace Sempiler.AST
{
    using NodeID = System.String;

    ///<summary>
    /// Serialized between runs so should NOT contain any data that is not 
    /// portable. For example, file paths should not be stored as strings with hardcoded
    /// path separators but as string arrays representating the relative path from the base directory
    /// for the ISession
    ///</summary>
    public class RawAST
    {
        public readonly string ID;
        public readonly Dictionary<NodeID, Node> Nodes;
        public readonly Dictionary<SemanticKind, List<Node>> NodesByKind;
        public readonly Dictionary<NodeID, Edge[]> Edges;
        public readonly Dictionary<NodeID, bool> Disabled;
    
        public RawAST() : this(
            new Dictionary<NodeID, Node>(), 
            new Dictionary<SemanticKind, List<Node>>(),
            new Dictionary<NodeID, Edge[]>(),
            new Dictionary<NodeID, bool>()
        )
        {
        }

        public RawAST(
            Dictionary<NodeID, Node> nodes, 
            Dictionary<SemanticKind, List<Node>> nodesByKind,
            Dictionary<NodeID, Edge[]> edges, 
            Dictionary<NodeID, bool> removed
        )
        {
            ID = CompilerHelpers.NextInternalGUID();
            Nodes = nodes;
            NodesByKind = nodesByKind;
            Edges = edges;
            Disabled = removed;
        }

        public RawAST Clone()
        {
            return new RawAST(
                new Dictionary<NodeID, Node>(Nodes),
                new Dictionary<SemanticKind, List<Node>>(NodesByKind),
                new Dictionary<NodeID, Edge[]>(Edges),
                new Dictionary<NodeID, bool>(Disabled)
            );
        }
    }

    public static class ASTHelpers
    {
        private static int IDSeed = 0;

        public static NodeID NextID()
        {
            int step = Interlocked.Increment(ref IDSeed);

            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            NodeID id = $"node_{milliseconds}_${step}";

            return id;
        }

        public static Node GetRoot(RawAST ast)
        {
            lock(ast)
            {
                // [dho] NOTE assumes the invariant that the tree will only ever
                // have 1 `Domain` and it will be found at the root - 13/05/19
                foreach(var kv in ast.Nodes)
                {
                    if(kv.Value.Kind == SemanticKind.Domain)
                    {
                        return kv.Value;
                    }
                }
            }

            return null;
        }


        public static void Register(RawAST ast, Node node)
        {
            lock(ast)
            {
                ast.Nodes[node.ID] = node;
                
                var kind = node.Kind;

                if(!ast.NodesByKind.ContainsKey(kind))
                {
                    ast.NodesByKind[kind] = new List<Node>();
                }
                else if(ast.NodesByKind[kind].IndexOf(node) > - 1)
                {
                    int i = 0;
                }

                ast.NodesByKind[kind].Add(node);
            }
        }

        public static IEnumerable<Edge> GetLiveEdges(RawAST ast, NodeID nodeID)
        {
            List<Edge> edges = new List<Edge>();

            lock(ast)
            {
                if(IsLive(ast, nodeID) && ast.Edges.ContainsKey(nodeID))
                {
                    foreach(var edge in ast.Edges[nodeID])
                    {
                        if(IsLive(ast, edge.NodeID))
                        {
                            edges.Add(edge);
                        }
                    }
                }
            }

            return edges;
        }

        public static bool IsLive(RawAST ast, NodeID id) => ast.Nodes.ContainsKey(id) && !ast.Disabled.ContainsKey(id);

        public static bool IsChildEdge(Edge edge) => edge.Role != SemanticRole.Meta && edge.Role != SemanticRole.Parent;


        public struct Position
        {
            public readonly int Index;
            public readonly SemanticRole Role;
            public readonly Node Node;
            public readonly Node Parent;

            public readonly bool Alive;

            public Position(int index, SemanticRole role, Node node, Node parent, bool alive)
            {
                Index = index;
                Role = role;
                Node = node;
                Parent = parent;
                Alive = alive;
            }
        }

        public static Position GetPosition(RawAST ast, NodeID id)
        {
            int index = -1;
            var role = SemanticRole.None;
            var node = GetNode(ast, id);
            Node parent = default(Node);
            var alive = IsLive(ast, id);

            if(node != null)
            {
                parent = GetParent(ast, id);

                if(parent != null)
                {
                    var edges = ast.Edges[parent.ID];

                    for(int i = 0; i < edges.Length; ++i)
                    {
                        var edge = edges[i];

                        if(edge.NodeID == id)
                        {
                            index = i;
                            role = edge.Role;
                            break;
                        }
                    }
                }
            }

            return new Position(index, role, node, parent, alive);
        }

        public static void DisableNodes(RawAST ast,  params NodeID[] nodeIDs)
        {
            lock(ast)
            {
                foreach(var nodeID in nodeIDs)
                {
                    ast.Disabled[nodeID] = true;
                }
                // [dho] TODO FIX if we disable/enable every node in the subtree
                // we hit an exception in the iOS bundling - 14/12/19
                // __PreOrderTraversal_NoLiveCheck(ast, nodeID => ast.Disabled[nodeID] = true, nodeIDs);
            }
        }

        public static void EnableNodes(RawAST ast,  params NodeID[] nodeIDs)
        {
            lock(ast)
            {
                foreach(var nodeID in nodeIDs)
                {
                    ast.Disabled.Remove(nodeID);
                }
                // [dho] TODO FIX if we disable/enable every node in the subtree
                // we hit an exception in the iOS bundling - 14/12/19
                // __PreOrderTraversal_NoLiveCheck(ast, nodeID => ast.Disabled.Remove(nodeID), nodeIDs);
            }
        }

        private delegate void TraversalNodeIDDelegate(NodeID nodeID);

        private static void __PreOrderTraversal_NoLiveCheck(RawAST ast, TraversalNodeIDDelegate del, params NodeID[] nodeIDs)
        {
            var toProcess = new Queue<NodeID>(nodeIDs);

            while(toProcess.Count > 0)
            {
                var nodeID = toProcess.Dequeue();

                del(nodeID);

                var childEdges = QueryEdges(ast, nodeID, e => e.Role != SemanticRole.Parent);

                foreach(var childID in childEdges)
                {
                    toProcess.Enqueue(childID);
                }
            }
        }
        
        public static void Connect(RawAST ast, NodeID parentID, Node[] newNodes, SemanticRole newRole, int index = -1)
        {
            lock(ast)
            {
                // [dho] NOTE use of **all** edges, not just _live_ edges, because
                // the indexes need to account for all connected edges from a node - 06/10/19
                var combinedEdges = ast.Edges.ContainsKey(parentID) ? new List<Edge>(ast.Edges[parentID]) : new List<Edge>();

                var insertedEdges = new List<Edge>();

                for(int i = 0; i < newNodes.Length; ++i)
                {
                    var newNode = newNodes[i];
                    
                    if(newNode.ID == parentID)
                    {
                        throw new System.ArgumentException("Cannot connect node to itself");
                    }

                    // foreach(var e in QueryEdges(ast, newNode.ID, x => x.Role != SemanticRole.Parent))
                    // {
                    //     if(e == parentID)
                    //     {
                    //         throw new System.ArgumentException("Circular dependency!!");
                    //     }
                    // }
                
                    // [dho] if the new node was previously deleted, now undelete it! - 13/05/18
                    if(!IsLive(ast, newNode.ID)) 
                    {
                        EnableNodes(ast, new [] { newNode.ID });
                    }

                    // [dho] remove existing parent - 13/05/19
                    {
                        var oldParent = GetParent(ast, newNode.ID);

                        if(oldParent != null)
                        {   
                            // [dho] we are trying to connect the node to the same parent - 05/11/19
                            if(oldParent.ID == parentID)
                            {
                                var didRemoveExistingEdge = false;

                                for(int ci = 0; ci < combinedEdges.Count; ++ci)
                                {
                                    var existingEdge = combinedEdges[ci];

                                    if(existingEdge.NodeID == newNode.ID)
                                    {
                                        // [dho] if the caller cares about the insertion index - 05/11/19
                                        if(index > -1 || existingEdge.Role != newRole)
                                        {
                                            combinedEdges.RemoveAt(ci);
                                            didRemoveExistingEdge = true;

                                            if(index > 0) 
                                            {
                                                // [dho] we have removed an element from the existing combined edges,
                                                // so when we come to insert at a specific index we need to take that 
                                                // removal into account and shift back a place - 05/12/19
                                                --index;
                                            }

                                            break;
                                        }
                                    }
                                }

                                if(!didRemoveExistingEdge)
                                {
                                    // [dho] edge exists already and is satisfactory - 05/11/19
                                    continue; 
                                }
                            }
                            else
                            {
                                // [dho] remove edge from old parent to node - 05/12/19
                                RemoveEdge(ast, oldParent.ID, e => e.NodeID == newNode.ID);
                                
                                // [dho] add edge from new node to it's new parent - 13/05/19
                                AddEdge(ast, newNode.ID, parentID, SemanticRole.Parent, e => e.Role == SemanticRole.Parent);
                            }
                        }
                        else
                        {
                            // [dho] add edge from new node to it's new parent - 13/05/19
                            AddEdge(ast, newNode.ID, parentID, SemanticRole.Parent, e => e.Role == SemanticRole.Parent);
                        }
                    }

                    insertedEdges.Add(new Edge(newNode.ID, newRole));
                }


                if(index > -1)
                {
                    combinedEdges.InsertRange(index, insertedEdges);
                }
                else
                {
                    combinedEdges.AddRange(insertedEdges);
                }

                ast.Edges[parentID] = combinedEdges.ToArray();
            }
        }

        // [dho] ensures that the whole subtree for each of the provided nodes is registered in the destination AST,
        // and connects each to the destination parent - 05/12/19
        public static void DeepRegister(RawAST sourceAST, RawAST destAST, NodeID destParentID, Node[] roots, CancellationToken token)
        {
            lock(sourceAST)
            {
                lock(destAST)
                {
                    System.Diagnostics.Debug.Assert(destAST.Nodes.ContainsKey(destParentID));

                    foreach(var root in roots)
                    {
                        Register(destAST, root);

                        Connect(destAST, destParentID, new [] { root }, GetPosition(sourceAST, root.ID).Role);

                        var childEdges = QueryEdges(sourceAST, root.ID, e => e.Role != SemanticRole.Parent);

                        __PreOrderTraversal_NoLiveCheck(sourceAST, nodeID => { 
                            var node = GetNode(sourceAST, nodeID);

                            Register(destAST, node);

                            var pos = GetPosition(sourceAST, nodeID);

                            Connect(destAST, pos.Parent.ID, new [] { node }, pos.Role);

                        }, childEdges);
                    }
                }
            }
        }

        public static void DeepRegister(RawAST sourceAST, RawAST destAST, Node[] nodes, CancellationToken token)
        {            
            foreach(var node in nodes)
            {
                Register(destAST, node);

                var sourceASTEdgeNodes = ASTHelpers.QueryLiveEdgeNodes(sourceAST, node.ID, x => x.Role != SemanticRole.Parent);

                DeepRegister(sourceAST, destAST, node.ID, sourceASTEdgeNodes, token);
            }
        }

        // // [dho] registers a node and all of it's children in the destination AST. NOTE this does not create any connections
        // // from the `node` to the rest of the destination AST, just ensures that the whole subtree is known to the destination AST - 05/12/19
        // public static void DeepRegister(RawAST sourceAST, RawAST destAST, Node node)
        // {
        //     Register(destAST, node);

        //     var sourceASTEdgeNodes = ASTHelpers.QueryEdgeNodes(sourceAST, node.ID, x => x.Role != SemanticRole.Parent);

        //     foreach(var child in sourceASTEdgeNodes)
        //     {
        //         DeepRegister(sourceAST, destAST, child);
        //     }
        // }

        public static void InsertBefore(RawAST ast, NodeID node, Node[] newNodes, SemanticRole role)
        {
            lock(ast)
            {
                var pos = ASTHelpers.GetPosition(ast, node);
                        
                ASTHelpers.Connect(ast, pos.Parent.ID, newNodes, role, pos.Index);
            }
        }

        public static void Replace(RawAST ast, NodeID outgoingID, Node[] newNodes)
        {
            lock(ast)
            {
                var pos = ASTHelpers.GetPosition(ast, outgoingID);
                        
                ASTHelpers.Connect(ast, pos.Parent.ID, newNodes, pos.Role, pos.Index);
                
                ASTHelpers.DisableNodes(ast, outgoingID);
            }
        }
        


        public static Node GetFirstAncestorOfKind(RawAST ast, NodeID start, SemanticKind kind)
        {
            var parent = GetParent(ast, start);

            while(parent != null)
            {   
                if(parent.Kind == kind)
                {
                    return parent;
                }

                parent = GetParent(ast, parent.ID);
            }

            return null;
        }



        // public static /* bool */ void DeregisterNode(RawAST ast, NodeID nodeID)
        // {
        //     lock(ast)
        //     {
        //         if(ast.Nodes.ContainsKey(nodeID))
        //         {
        //             // [dho] NOTE makes the assumption that the only other node linked
        //             // to this one would be it's parent - 11/05/19
        //             var parent = GetParent(ast, nodeID);

        //             if(parent != null)
        //             {
        //                 RemoveEdge(ast, parent.ID, e => e.NodeID == nodeID);
        //             }
                    
        //             // [dho] prune any nodes that this node is the parent of - 11/05/19
        //             {
        //                 var edges = ast.Edges[nodeID];

        //                 for(int i = 0; i < edges.Length; ++i)
        //                 {
        //                     var edge = edges[i];

        //                     // [dho] if the node we are deregistering is the parent
        //                     // of the node linked to by the edge - 11/05/19
        //                     if(GetParent(ast, edge.NodeID)?.ID == nodeID)
        //                     {
        //                         DeregisterNode(ast, edge.NodeID);
        //                     }
        //                 }
        //             }

        //             ast.Edges.Remove(nodeID);

        //             ast.Nodes.Remove(nodeID);

        //             // return true;
        //         }
        //     }  

        //     // return false;
        // }


        // public static void ReplaceNode(RawAST ast, NodeID outgoing, Node incoming)
        // {
        //     lock(ast)
        //     {
        //         var parent = GetParent(ast, outgoing);
        //         Edge edgeFromParentToOutgoing = null;

        //         if(parent != null)
        //         {
        //             foreach(var (edge, hasNext) in IterateEdges(ast, parent.ID))
        //             {
        //                 if(edge.NodeID == outgoing)
        //                 {
        //                     edgeFromParentToOutgoing = edge;
        //                     break;
        //                 }
        //             }
        //         }

        //         System.Diagnostics.Debug.Assert(edgeFromParentToOutgoing != null);

        //         ReplaceNode(ast, outgoing, incoming, edgeFromParentToOutgoing.Role);
        //     }
        // }

        // public static void ReplaceNode(RawAST ast, NodeID outgoing, Node[] incoming, SemanticRole newRole)
        // {
        //     /*
        //         TODO actually we need to support replacing the existing node
        //         with many new nodes AT THE SAME INDEX..

        //         because when we code gen declarations we want them to be emitted
        //         in the same place as they were written in the code
        //     */
            
        //     lock(ast)
        //     {
        //         var parent = GetParent(ast, outgoing);




        //         DeregisterNode(ast, outgoing);

        //         RegisterNode(ast, incoming);

        //         // [dho] ie. is not the root - 11/05/19
        //         if(parent != null)
        //         {
        //             // [dho] use the same role as the child we are replacing - 11/05/19
        //             AssignParent(ast, incoming.ID, newRole, parent.ID);
        //         }
        //     }
        // }

        public static Node GetNode(RawAST ast, NodeID id)
        {
            lock(ast)
            {
                return ast.Nodes.ContainsKey(id) /* && !ast.Removed.ContainsKey(id) */ ? ast.Nodes[id] : null;
            }
        }

        // private static void RemoveParentalLink(RawAST ast, NodeID childID)
        // {
        //     lock(ast)
        //     {
        //         var parent = GetParent(ast, childID);

        //         if(parent != null)
        //         {
        //             RemoveEdge(ast, parent.ID, e => e.NodeID == childID);
        //             RemoveEdge(ast, childID,  e => e.NodeID == parent.ID);
        //             AddEdge(ast,)
        //         }
        //     }
        // }

        // public static bool AssignParent(RawAST ast, NodeID childID, SemanticRole childRole, NodeID parentID)
        // {
        //     lock(ast)
        //     {
        //         RemoveParentalLink(ast, childID);

        //         var parentIndex = AddEdge(ast, from : childID, to : parentID, role : SemanticRole.Parent, shouldReplace : e => e.NodeID == parentID /* && e.Role == SemanticRole.Parent */);
                
        //         if(parentIndex == -1) return false;

        //         var childIndex = AddEdge(ast, from : parentID, to : childID, role : childRole, shouldReplace : e => e.NodeID == childID);
            
        //         return childIndex > -1;
        //     }
        // }


        public delegate bool EdgePredicate(Edge edge);

        // private static int AddEdge(RawAST ast, NodeID from, NodeID to, SemanticRole role) => AddEdge(ast, from, to, role, e => false);
        
        private static int AddEdge(RawAST ast, NodeID from, NodeID to, SemanticRole role, EdgePredicate shouldReplace)
        {
            System.Diagnostics.Debug.Assert(from != to);

            int resultIndex = -1;
            
            var newEdge = new Edge(to, role);
            
            lock(ast)
            {
                if(ast.Edges.ContainsKey(from))
                {
                    var edgeIndexToReplace = -1;

                    var existingEdges = ast.Edges[from];

                    for(int i = 0; i < existingEdges.Length; ++i)
                    {
                        var existingEdge = existingEdges[i];

                        if(shouldReplace(existingEdge))
                        {
                            edgeIndexToReplace = i;
                            break;
                        }
                    }

                    Edge[] newEdges;

                    if(edgeIndexToReplace > -1)
                    {
                        newEdges = new Edge[existingEdges.Length];

                        for(int i = 0; i < existingEdges.Length; ++i)
                        {
                            if(i == edgeIndexToReplace)
                            {
                                newEdges[resultIndex = i] = newEdge;
                            }
                            else
                            {
                                newEdges[i] = existingEdges[i];
                            }
                        }
                    }
                    else
                    {
                        newEdges = new Edge[existingEdges.Length + 1];

                        System.Array.Copy(existingEdges, newEdges, existingEdges.Length);

                        newEdges[resultIndex = newEdges.Length - 1] = newEdge;
                    }

                    ast.Edges[from] = newEdges;
                }
                else
                {
                    ast.Edges[from] = new [] { newEdge };
                    resultIndex = 0;
                }
            }

            return resultIndex;
        }

        private static void RemoveEdge(RawAST ast, NodeID from, EdgePredicate shouldRemove)
        {
            lock(ast)
            {
                if(ast.Edges.ContainsKey(from))
                {
                    var existingEdges = ast.Edges[from];

                    List<Edge> newEdges = new List<Edge>(existingEdges.Length);

                    for(int i = 0; i < existingEdges.Length; ++i)
                    {
                        var existingEdge = existingEdges[i];

                        if(!shouldRemove(existingEdge))
                        {
                            newEdges.Add(existingEdge);
                        }
                    }

                    ast.Edges[from] = newEdges.ToArray();
                }
            }
        }


        public static Node GetParent(RawAST ast, NodeID id) => GetSingleLiveMatch(ast, id, SemanticRole.Parent);

        public static Node GetSingleLiveMatch(RawAST ast, NodeID id, SemanticRole role)
        {
            var matches = QueryEdges(ast, id, e => 
                e.Role == role && IsLive(ast, e.NodeID)
            );

            if(matches.Length == 1)
            {
                return GetNode(ast, matches[0]);
            }
            else
            {
                return null;
            }
        }

        public static Node[] QueryLiveEdgeNodes(RawAST ast, NodeID id, SemanticRole role)
        {
            var liveEdges = QueryEdges(ast, id, e => e.Role == role && IsLive(ast, e.NodeID));

            var nodes = new Node[liveEdges.Length];

            for(int i = 0; i < liveEdges.Length; ++i)
            {
                nodes[i] = GetNode(ast, liveEdges[i]);
            }

            return nodes;
        }

        public static IEnumerable<Node> QueryByKind(RawAST ast, SemanticKind kind)
        {
            return ast.NodesByKind.ContainsKey(kind) ? (
                ast.NodesByKind[kind] 
            ) : (
                new List<Node>()
            );
        }

        public static Node[] QueryLiveEdgeNodes(RawAST ast, NodeID id, EdgePredicate predicate)
        {
            var liveEdges = QueryEdges(ast, id, e => predicate(e) && IsLive(ast, e.NodeID));

            var nodes = new Node[liveEdges.Length];

            for(int i = 0; i < liveEdges.Length; ++i)
            {
                nodes[i] = GetNode(ast, liveEdges[i]);
            }

            return nodes;
        }

        public static NodeID[] QueryLiveChildEdges(RawAST ast, NodeID id)
        {
            return QueryEdges(ast, id, e => 
                e.Role != SemanticRole.Parent && IsLive(ast, e.NodeID)
            );
        }

        public static NodeID[] GetAllEdges(RawAST ast, NodeID id) => QueryEdges(ast, id, e => true);

        public static NodeID[] QueryEdges(RawAST ast, NodeID id, SemanticRole role) => QueryEdges(ast, id, e => e.Role == role);

        public static NodeID[] QueryEdges(RawAST ast, NodeID id, EdgePredicate predicate)
        {
            if(ast.Edges.ContainsKey(id))
            {
                var edges = ast.Edges[id];

                List<NodeID> matches = new List<NodeID>(edges.Length);

                for(int i = 0; i < edges.Length; ++i)
                {
                    var edge = edges[i];

                    if(predicate(edge))
                    {
                        matches.Add(edge.NodeID);
                    }
                }
                
                return matches.ToArray();
            }
            else
            {
                return new NodeID[]{};
            }
        }

        // public static IEnumerable<(Edge, bool)> IterateEdges(RawAST ast, NodeID id)
        // {
        //     if(ast.Edges.ContainsKey(id))
        //     {
        //         var edges = ast.Edges[id];

        //         for(int m = 0; m < edges.Length - 1; ++m)
        //         {
        //             yield return (edges[m], true);
        //         }

        //         yield return (edges[edges.Length - 1], false);
        //     }
        // }

        // public static Node[] GetEdgeNodes(RawAST ast, NodeID id)
        // {
        //     if(ast.Edges.ContainsKey(id))
        //     {
        //         var edges = ast.Edges[id];

        //         List<Node> matches = new List<Node>(edges.Length);

        //         for(int i = 0; i < edges.Length; ++i)
        //         {
        //             matches.Add(GetNode(ast, edges[i].NodeID));
        //         }
                
        //         return matches.ToArray();
        //     }
        //     else
        //     {
        //         return new Node[]{};
        //     }
        // }

        // public static IEnumerable<Component> EnumerateTopLevelComponents(RawAST ast)
        // {
        //     if(ast.Root?.Kind == SemanticKind.Domain)
        //     {
        //         foreach(var (child, hasNext) in NodeInterrogation.IterateChildren(ast.Root))
        //         {
        //             if(child.Kind == SemanticKind.Component)
        //             {
        //                 yield return NodeInterrogation.Component(child);
        //             }
        //         }
        //     }
        // }

        // public static bool HasTopLevelComponent(RawAST ast, string name)
        // {   
        //     foreach(var component in EnumerateTopLevelComponents(ast))
        //     {
        //         if(component.Name == name)
        //         {
        //             return true;
        //         }
        //     }

        //     return false;
        // }

        // public static Result<object> TransferTopLevelComponents(RawAST source, RawAST destination)
        // {
        //     x;
        //     // var result = new Result<object>();

        //     // if(destination.Root?.Kind == SemanticKind.Domain)
        //     // {
        //     //     var destinationDomain = NodeInterrogation.Domain(destination.Root);

        //     //     foreach(var component in EnumerateTopLevelComponents(source))
        //     //     {
        //     //         destinationDomain.AddChild(component);
        //     //     }
        //     // }
        //     // else
        //     // {
        //     //     result.AddMessages(new Message(MessageKind.Error, "Expected root component of AST to be Domain but found " + destination.Root?.Kind.ToString()));
        //     // }

        //     // return result;
        // }


        // public static Result<bool> LookUpMembersInTypeReference(RawAST ast, Node typeReference, Dictionary<string, Node> needles)
        // {
        //     var result = new Result<bool>();

        //     var membersFound = 0;

        //     if(typeReference?.Kind == SemanticKind.DynamicTypeReference)
        //     {
        //         var dynamicTypeRef = ASTNodeFactory.DynamicTypeReference(ast, typeReference);

        //         foreach(var sig in dynamicTypeRef.Signatures)
        //         {
        //             var name = ASTHelpers.GetSingleMatch(ast, sig.ID, SemanticRole.Name);

        //             if(name?.Kind == SemanticKind.Identifier)
        //             {
        //                 var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;

        //                 // [dho] if this is the name of a member the caller was interesting in
        //                 // we update the dictionary entry - 14/06/19
        //                 if(needles.ContainsKey(lexeme))
        //                 {
        //                     needles[lexeme] = sig;
                            
        //                     ++membersFound;
        //                 }
        //             }
        //         }
        //     }
        //     else
        //     {
        //         error();
        //     }

        //     result.Value = membersFound == needles.Count;
        
        //     return result;
        // }

        public delegate bool TraversalDelegate(Node node);

        public static void PreOrderLiveTraversal(RawAST ast, Node start, TraversalDelegate del, CancellationToken token)
        {
            var queue = new Queue<Node>();

            queue.Enqueue(start);

            // var seen = new System.Collections.Generic.Dictionary<string, bool>();

            while(queue.Count > 0)
            {
                var focus = queue.Dequeue();

                // if(seen.ContainsKey(focus.ID))
                // {
                //     int i =0;
                // }
                // seen[focus.ID] = true;

                var shouldExploreChildren = del(focus);

                // Console.WriteLine("Traversal : " + focus.Kind + " (" + ASTHelpers.GetPosition(ast, focus.ID).Role + ", " +  focus.ID + ")\n" + ASTNodeHelpers.GetLexeme(focus) + "\n should look at children? " + shouldExploreChildren);

                if(token.IsCancellationRequested)
                {
                    break;
                }

                if(shouldExploreChildren)
                {
                    foreach(var (child, _) in ASTNodeHelpers.IterateLiveChildren(ast, focus.ID))
                    {
                        // if(focus.ID.EndsWith("_$1007")/*  || focus.ID.EndsWith("_$858") || focus.ID.EndsWith("_$859")*/)
                        // {
                        //     Console.WriteLine(focus.Kind + " (" +  focus.ID + ") is adding " + child.Kind + " (" + child.ID + ")");

                        // }
                        queue.Enqueue(child);
                    }
                }
            }
        }

        public static bool Contains(RawAST ast, string needleID, CancellationToken token)
        {
            return Contains(ast, GetRoot(ast), needleID, token);
        }

        public static bool Contains(RawAST ast, Node start, string needleID, CancellationToken token)
        {
            foreach(var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, start.ID))
            {
                if(child.ID == needleID || Contains(ast, child, needleID, token)) 
                {
                    return true;
                }
            }

            return false;
        }


        public delegate bool FindPredicate(RawAST ast, Node node);

        public static IEnumerable<Node> FindAll(RawAST ast, Node start, FindPredicate predicate, CancellationToken token)
        {
            foreach(var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, start.ID))
            {
                if(predicate(ast, child)) 
                {
                    yield return child;
                }

                foreach(var match in FindAll(ast, child, predicate, token))
                {
                    yield return match;
                }
            }
        }

        public static Component GetComponentByName(RawAST ast, string name)
        {
            foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.Component))
            {
                var component = ASTNodeFactory.Component(ast, (DataNode<string>)node);

                if(component.Name == name)
                {
                    return component;
                }
            }

            return null;
        }

        public static string[] GetComponentNames(RawAST ast, bool liveOnly = false)
        {
            var componentNames = new List<string>();

            if(liveOnly)
            {
                foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.Component))
                {
                    if(!IsLive(ast, node.ID)) continue;

                    var component = ASTNodeFactory.Component(ast, (DataNode<string>)node);

                    componentNames.Add(component.Name);
                }
            }
            else
            {
                foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.Component))
                {
                    var component = ASTNodeFactory.Component(ast, (DataNode<string>)node);

                    componentNames.Add(component.Name);
                }
            }

            return componentNames.ToArray();
        }
    }

}