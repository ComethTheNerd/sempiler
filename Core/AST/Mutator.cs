// namespace Sempiler.AST
// {
//     using System.Collections.Generic;
//     // public interface IMutator
//     // {
//     //     // parentID should not be null
//     //     // newChildID may be null, and this case we just remove the existing child at the 
//     //     // supplied index.. if that is missing do nothing
//     //     // if newChildID is null then this just detaches that was at index before
//     //     // if newChildID is not null, but index is null, then we just append the child
//     //     // Diagnostics.Result<void> Update(NodeID parentID, NodeID newChildID, int? newChildIndex = null);
    
        
//     // }

//     // // light wrapper around static APIs
//     // internal class Mutator : IMutator
//     // {
//     //     private RawAST AST;

//     //     public Mutator(RawAST ast)
//     //     {
//     //         this.AST = ast;
//     //     }

//     //     public Diagnostics.Result<void> Update(NodeID parentID, NodeID newChildID, int? newChildIndex = null)
//     //     {
//     //         return MutationHelpers.Update(AST, parentID, newChildID, newChildIndex);
//     //     }
//     // }

//     public static class MutationHelpers
//     {
//         // public static void Update(Node node, Node newValue, int index)
//         // {
//         //     lock(node)
//         //         (node.Children ?? (node.Children = new NodeChildren(node)))[index] = newValue;
//         // }

//         // public static bool Delete(Node node)
//         // {

//         // }

//         // public static bool Delete(Node node)
//         // {
//         //     lock(astlock)
//         //     {
//         //         var index = NodeInterrogation.IndexOfChild(node.Parent, node.ID);

//         //         if (index > -1)
//         //         {
//         //             node.Parent.Children[index] = null;

//         //             return true;
//         //         }

//         //         return false;
//         //     }
//         // }

//         // public static void Detach(Node node)
//         // {
//         //     Node parent = node.Parent;

//         //     // if parent is set then by implication the children
//         //     // should also be initialized on the parent, otherwise
//         //     // there's a bug somewhere
//         //     if(parent != null)
//         //     {
//         //         lock(astlock)
//         //         {
//         //             for(int i = 0; i < parent.Children.Count; ++i)
//         //             {
//         //                 if(parent.Children[i]?.ID == node.ID)
//         //                 {
//         //                     parent.Children[i] = null;
//         //                     break;
//         //                 }
//         //             }

//         //             node.Parent = null;
//         //         }
//         //     }
//         // }

//         // public static void Attach(Node node, Node newParent, int? index = null)
//         // {
//         //     lock(astlock)
//         //     {
//         //         NodeChildren target = (newParent.Children ?? (newParent.Children = new NodeChildren(newParent)));

//         //         if(index.HasValue)
//         //         {
//         //             target[index.Value] = node;
//         //         }
//         //         else
//         //         {
//         //             target.Add(node);
//         //         }
//         //     }
//         // }

//         // public static Node ContextualizeIdentifiers(RawAST ast, Node haystack, ContextualHandleType type, Node needle)
//         // {
//         //     if(needle != null)
//         //     {
//         //         switch(needle.Kind)
//         //         {
//         //             case SemanticKind.Identifier:
//         //                 haystack = ContextualizeIdentifiersWithName(ast, haystack, type, ((Identifier)needle).Text);
//         //             break;

//         //             case SemanticKind.OrderedGroup:
//         //             {
//         //                 var group = (OrderedGroup)needle;

//         //                 for(int i = 0; i < group.Members?.Count; ++i)
//         //                 {
//         //                     haystack = ContextualizeIdentifiers(ast, haystack, type, group.Members[i]);
//         //                 }
//         //             }
//         //             break;
//         //         }
//         //     }
           
//         //     return haystack;
//         // }

//         /// Finds all unqualified identifiers and replaces them with a ContextualHandle 
//         /// with the given name
//         // public static Node ContextualizeIdentifiersWithName(RawAST ast, Node haystack, ContextualHandleType type, string name)
//         // {
//         //     if(haystack != null)
//         //     {
//         //         if(NodeInterrogation.IsIdentifierWithName(haystack, name))
//         //         {
//         //             var handle = NodeFactoryHelpers.CreateContextualHandle(ast, type);

//         //             handle.Name = NodeFactoryHelpers.CreateIdentifier(ast, name);

//         //             return handle;
//         //         }
//         //         else
//         //         {
//         //             var queue = new Queue<(int,AST.Node)>();

//         //             for(int i = 0; i < haystack.Children?.Count; ++i)
//         //             {
//         //                 queue.Enqueue((i, haystack.Children[i]));
//         //             }

//         //             while(queue.Count > 0)
//         //             {
//         //                 var (index, focus) = queue.Dequeue();

//         //                 if(focus == null)
//         //                 {
//         //                     continue;
//         //                 }
//         //                 else if(NodeInterrogation.IsIdentifierWithName(focus, name))
//         //                 {
//         //                     var handle = NodeFactoryHelpers.CreateContextualHandle(ast, type);

//         //                     handle.Name = NodeFactoryHelpers.CreateIdentifier(ast, name);

//         //                     Update(focus.Parent, handle, index);
//         //                 }   
//         //                 else if(focus is QualifiedAccess)
//         //                 {
//         //                     // is left most member of qualified access, eg. `x` in `x.y.z` 
//         //                     if(focus.Parent?.Kind != SemanticKind.QualifiedAccess)
//         //                     {
//         //                         var incident = ((QualifiedAccess)focus).Incident;

//         //                         // we only care about checking the `Incident`, ie. the `x` in `x.y.z`.. we don't care
//         //                         // about what comes after that
//         //                         queue.Enqueue((NodeInterrogation.IndexOfChild(focus, incident?.ID), incident));
//         //                     }
//         //                 }
//         //                 else if(focus is DataValueDeclaration)
//         //                 {
//         //                     var initializer = ((DataValueDeclaration)focus).Initializer;

//         //                     queue.Enqueue((NodeInterrogation.IndexOfChild(focus, initializer?.ID), initializer));
//         //                 }
//         //                 else if(focus is FunctionDeclaration)
//         //                 {
//         //                     var body = ((FunctionDeclaration)focus).Body;

//         //                     queue.Enqueue((NodeInterrogation.IndexOfChild(focus, body?.ID), body));
//         //                 }
//         //                 else
//         //                 {
//         //                     for(int i = 0; i < haystack.Children?.Count; ++i)
//         //                     {
//         //                         queue.Enqueue((i, haystack.Children[i]));
//         //                     }
//         //                 }
//         //             }

//         //         }
//         //     }
   
//         //     return haystack;
//         // }




//         // public static Diagnostics.Result<void> Update(RawAST ast, NodeID parentID, NodeID newChildID, int? newChildIndex = null)
//         // {
//         //     const Node parent = ast.Nodes[parentID];

//         //     if(parent == null)
//         //     {
//         //         // return error not found parent
//         //         return Diagnostics.CreateResultWithErrors(
//         //             //...
//         //         );
//         //     }

//         //     if(newChildIndex.HasValue)
//         //     {
//         //         // remove existing child at that index
//         //         const Node existingChild = this.ChildAt(ast, parentID, newChildIndex.Value);

//         //         if(existingChild != null)
//         //         {
//         //             // detach existing child from parent
//         //             ast.Parents.Remove(existingChild.ID);
//         //             ast.Children[parentID]?[newChildIndex.Value] = null;
//         //         }

//         //         if(newChildID != null)
//         //         {
//         //             this.Move(ast, parentID, newChildID, newChildIndex);
//         //         }
//         //     }
//         //     else if(newChildID != null)
//         //     {
//         //         this.Move(ast, parentID, newChildID);
//         //     }

//         //     return Diagnostics.CreateResult(/**/);       
//         // }

//         // private static void Move(RawAST ast, NodeID newParentID, NodeID childID, int? newIndex = null)
//         // {
//         //     // if this is a move operation from an old parent to our new parent
//         //     // then we need to clean up the existing references in the old parent
//         //     const Node oldParent = this.Parent(ast, childID);

//         //     if(oldParent != null)
//         //     {
//         //         const List<NodeID> oldParentChildren = ast.Children[oldParent.ID];

//         //         // remove the reference in the old parent that points to the child we are moving
//         //         oldParentChildren[oldParentChildren.IndexOf(childID)] = null;
//         //     }

//         //     // update the parent value for the child we are moving
//         //     ast.Parents[childID] = newParentID;

//         //     if(newIndex.HasValue)
//         //     {
//         //         // store the new child at the specified index in the new parent
//         //         (ast.Children[newParentID] ?? ast.Children[newParentID] = new List<NodeID>(newChildIndex.Value + 1))[newChildIndex.Value] = childID;
//         //     }
//         //     else
//         //     {
//         //         // append the new child to the parent
//         //         (ast.Children[newParentID] ?? ast.Children[newParentID] = new List<NodeID>()).Add(newChildID);
//         //     }
//         // }
//     }

    
// }