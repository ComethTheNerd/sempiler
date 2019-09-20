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
        public readonly Dictionary<NodeID, Node> Nodes;
        public readonly Dictionary<NodeID, Edge[]> Edges;
        public readonly Dictionary<NodeID, bool> Removed;
    
        public RawAST() : this(
            new Dictionary<NodeID, Node>(), 
            new Dictionary<NodeID, Edge[]>(),
            new Dictionary<NodeID, bool>()
        )
        {
        }

        public RawAST(Dictionary<NodeID, Node> nodes, Dictionary<NodeID, Edge[]> edges, Dictionary<NodeID, bool> removed)
        {
            Nodes = nodes;
            Edges = edges;
            Removed = removed;
        }

        public RawAST Clone()
        {
            return new RawAST(
                new Dictionary<NodeID, Node>(Nodes), 
                new Dictionary<NodeID, Edge[]>(Edges),
                new Dictionary<NodeID, bool>(Removed)
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

        public static void RegisterNode(RawAST ast, Node node)
        {
            lock(ast)
            {
                ast.Nodes[node.ID] = node;
            }
        }

        public static IEnumerable<Edge> GetEdges(RawAST ast, NodeID nodeID)
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

        public static bool IsLive(RawAST ast, NodeID id) => ast.Nodes.ContainsKey(id) && !ast.Removed.ContainsKey(id);

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

        public static void RemoveNodes(RawAST ast,  params NodeID[] nodeIDs)
        {
            lock(ast)
            {
                foreach(var nodeID in nodeIDs)
                {
                    ast.Removed[nodeID] = true;
                }
            }
        }

        public static void Connect(RawAST ast, NodeID parentID, Node[] newNodes, SemanticRole newRole, int index = -1)
        {
            lock(ast)
            {
                var combinedEdges = new List<Edge>(GetEdges(ast, parentID));

                var insertedEdges = new Edge[newNodes.Length];

                for(int i = 0; i < newNodes.Length; ++i)
                {
                    var newNode = newNodes[i];
                    
                    if(newNode.ID == parentID)
                    {
                        throw new System.ArgumentException("Cannot connect node to itself");
                    }

                    // [dho] if the new node was previously deleted, now undelete it! - 13/05/18
                    ast.Removed.Remove(newNode.ID);

                    // [dho] remove existing parent - 13/05/19
                    {
                        var oldParent = GetParent(ast, newNode.ID);

                        if(oldParent != null)
                        {
                            RemoveEdge(ast, oldParent.ID, e => e.NodeID == newNode.ID);
                        }
                        
                        // [dho] add edge from new node to it's new parent - 13/05/19
                        AddEdge(ast, newNode.ID, parentID, SemanticRole.Parent, e => e.Role == SemanticRole.Parent);
                    }

                    insertedEdges[i] = new Edge(newNode.ID, newRole);
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
                
                ASTHelpers.RemoveNodes(ast, outgoingID);
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


        public static Node GetParent(RawAST ast, NodeID id) => GetSingleMatch(ast, id, SemanticRole.Parent);

        public static Node GetSingleMatch(RawAST ast, NodeID id, SemanticRole role)
        {
            var matches = QueryEdges(ast, id, role);

            if(matches.Length == 1)
            {
                return GetNode(ast, matches[0]);
            }
            else
            {
                return null;
            }
        }

        public static Node[] QueryEdgeNodes(RawAST ast, NodeID id, SemanticRole role)
        {
            var edges = QueryEdges(ast, id, role);

            var nodes = new Node[edges.Length];

            for(int i = 0; i < edges.Length; ++i)
            {
                nodes[i] = GetNode(ast, edges[i]);
            }

            return nodes;
        }

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

                    if(IsLive(ast, edge.NodeID) && predicate(edge))
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

        public static void PreOrderTraversal(Session session, RawAST ast, Node start, TraversalDelegate del, CancellationToken token)
        {
            var queue = new Queue<Node>();

            queue.Enqueue(start);

            while(queue.Count > 0)
            {
                var focus = queue.Dequeue();

                var shouldExploreChildren = del(focus);

                // Console.WriteLine("Traversal : " + focus.Kind + " (" + ASTHelpers.GetPosition(ast, focus.ID).Role + ", " +  focus.ID + ")\n" + ASTNodeHelpers.GetLexeme(focus) + "\n should look at children? " + shouldExploreChildren);

                if(token.IsCancellationRequested)
                {
                    break;
                }

                if(shouldExploreChildren)
                {
                    foreach(var (child, _) in ASTNodeHelpers.IterateChildren(ast, focus.ID))
                    {
                        // Console.WriteLine(focus.Kind + " (" +  focus.ID + ") is adding " + child.Kind + " (" + child.ID + ")");
                        queue.Enqueue(child);
                    }
                }
            }
        }

        public static bool Contains(RawAST ast, Node start, string needleID, CancellationToken token)
        {
            foreach(var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, start.ID))
            {
                if(child.ID == needleID || Contains(ast, child, needleID, token)) 
                {
                    return true;
                }
            }

            return false;
        }
    }

}