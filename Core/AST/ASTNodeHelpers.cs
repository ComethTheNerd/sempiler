﻿using System.Collections.Generic;
using System.Threading;
using Sempiler.Languages;
using Sempiler.Diagnostics;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.AST.Diagnostics;

namespace Sempiler.AST
{
    using NodeID = System.String;
    public static class ASTNodeHelpers
    {
        public static string GetLexeme(ASTNode nodeWrapper) => GetLexeme(nodeWrapper.Node);
        public static string GetLexeme(Node node)
        {
            return node?.Origin?.Lexeme;
        }

        public static Node[] GetMeta(RawAST ast, NodeID nodeID)
        {
            return ASTHelpers.QueryEdgeNodes(ast, nodeID, SemanticRole.Meta);
        }

        public static MetaFlag GetMetaFlags(RawAST ast, NodeID nodeID)
        {
            MetaFlag flags = 0x0;

            var meta = GetMeta(ast, nodeID);

            for (int i = 0; i < meta.Length; ++i)
            {
                flags |= ASTNodeFactory.Meta(ast, (DataNode<MetaFlag>)meta[i]).Flags;
            }

            return flags;
        }


        public static IEnumerable<(AST.Node, bool)> IterateChildren(RawAST ast, AST.ASTNode nodeWrapper) => IterateChildren(ast, nodeWrapper.ID);

        public static IEnumerable<(AST.Node, bool)> IterateChildren(RawAST ast, NodeID nodeID)
        {
            var edges = new List<Edge>();

            // System.Console.WriteLine("\n\nEDGES for " + nodeID);

            foreach (var edge in ASTHelpers.GetEdges(ast, nodeID))
            {
                if (ASTHelpers.IsChildEdge(edge) && ASTHelpers.IsLive(ast, nodeID))
                {
                    // System.Console.WriteLine("The Edge from " + nodeID + " to " + edge.NodeID + " : " + edge.Role);

                    edges.Add(edge);
                }
            }

            if (edges.Count > 0)
            {
                for (int i = 0; i < edges.Count - 1; ++i)
                {
                    var node = ASTHelpers.GetNode(ast, edges[i].NodeID);

                    yield return (node, true);
                }

                {
                    var node = ASTHelpers.GetNode(ast, edges[edges.Count - 1].NodeID);

                    yield return (node, false);
                }
            }

        }

        public static IEnumerable<(AST.Node, bool)> IterateMembers(AST.Node[] members)
        {
            if (members.Length > 0)
            {
                for (int m = 0; m < members.Length - 1; ++m)
                {
                    yield return (members[m], true);
                }

                yield return (members[members.Length - 1], false);
            }
            // if(node != null)
            // {
            //     switch(node.Kind)
            //     {
            //         case SemanticKind.OrderedGroup:{
            //             var members = OrderedGroup(node).Members;

            //             if(filterNulls)
            //             {
            //                 for(int m = 0; m < members.Length - 1; ++m)
            //                 {
            //                     var member = members[m];

            //                     if(member != null)
            //                     {
            //                         yield return (members[m], true);
            //                     }
            //                 }

            //                 var lastMember = members[members.Length - 1];

            //                 if(lastMember != null)
            //                 {
            //                     yield return (lastMember, false);
            //                 }
            //             }
            //             else
            //             {
            //                 for(int m = 0; m < members.Length - 1; ++m)
            //                 {
            //                     yield return (members[m], true);
            //                 }

            //                 yield return (members[members.Length - 1], false);
            //             }
            //         }
            //         break;

            //         default:{
            //             yield return (node, false);
            //         }
            //         break;
            //     }
            // }
        }

        public static Node RHS(RawAST ast, Node node)
        {
            if(node.Kind == SemanticKind.QualifiedAccess)
            {
                var qa = ASTNodeFactory.QualifiedAccess(ast, node);

                return RHS(ast, qa.Member);
            }
            else
            {
                return node;
            }
        }

        public static IEnumerable<(AST.Node, bool)> IterateQualifiedAccessLTR(AST.QualifiedAccess qualifiedAccess)
        {
            var incident = qualifiedAccess.Incident;
            var member = qualifiedAccess.Member;

            var items = new List<Node>();

            if(incident.Kind == SemanticKind.QualifiedAccess)
            {   
                foreach(var (item, _) in IterateQualifiedAccessLTR(ASTNodeFactory.QualifiedAccess(qualifiedAccess.AST, incident)))
                {
                    items.Add(item);
                }
            }
            else
            {
                items.Add(incident);
            }


            items.Add(member);

            return IterateMembers(items.ToArray());
        }

        public static Node ConvertToQualifiedAccessIfRequiredLTR(RawAST ast, INodeOrigin origin, Node[] nodes)
        {
            System.Diagnostics.Debug.Assert(nodes.Length > 0);

            if(nodes.Length == 1)
            {
                return nodes[0];
            }
            else
            {
                var focus = NodeFactory.QualifiedAccess(ast, origin).Node;

                ASTHelpers.Connect(ast, focus.ID, new [] { nodes[0] }, SemanticRole.Incident);
                ASTHelpers.Connect(ast, focus.ID, new [] { nodes[1] }, SemanticRole.Member);

                for(int i = 2; i < nodes.Length; ++i)
                {
                    var incident = focus;
                    var member = nodes[i];
                    
                    focus = NodeFactory.QualifiedAccess(ast, origin).Node;
                    
                    ASTHelpers.Connect(ast, focus.ID, new [] { incident }, SemanticRole.Incident);
                    ASTHelpers.Connect(ast, focus.ID, new [] { member }, SemanticRole.Member);
                }

                return focus;
            }
        }

        // public static int MemberCount(AST.Node node)
        // {
        //     if(node != null)
        //     {
        //         switch(node.Kind)
        //         {
        //             case SemanticKind.OrderedGroup:{
        //                 var members = OrderedGroup(node).Members;

        //                 if(members == null)
        //                 {
        //                     return 0;
        //                 }
        //                 else
        //                 {
        //                     return members.Length;
        //                 }
        //             }

        //             default:
        //                 return 1;
        //         }
        //     }

        //     return 0;
        // }

        // public static IEnumerable<(AST.Node, bool, AST.Node)> IterateMembers(params AST.Node[] nodes)
        // {
        //     for(var i = 0; i < nodes.Length; ++i)
        //     {
        //         var node = nodes[i];

        //         if(node != null)
        //         {
        //             switch(node.Kind)
        //             {
        //                 case SemanticKind.OrderedGroup:{
        //                     var members = ((OrderedGroup)node).Members;

        //                     for(int m = 0; m < members.Count - 1; ++m)
        //                     {
        //                         yield return (members[m], true, node);
        //                     }

        //                     yield return (members[members.Count - 1], _HasNext(nodes, i), node);
        //                 }
        //                 break;

        //                 default:{
        //                     yield return (node, _HasNext(nodes, i), node);
        //                 }
        //                 break;
        //             }
        //         }
        //     }
        // }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // static bool _HasNext(AST.Node[] nodes, int index)
        // {
        //     if(index < nodes.Length - 2)
        //     {
        //         var node = nodes[index + 1];

        //         if(node != null)
        //         {
        //             // [dho] the node is defined, but just not a group
        //             return node.Kind != SemanticKind.OrderedGroup || 
        //                         (node as OrderedGroup).Members?.Count > 0;
        //         }
        //     }

        //     return false;
        // }

        // public static int IndexOfChild(Node node, NodeID id)
        // {
        //     if(node != null && id != null)
        //     {
        //         for(int i = 0; i < node.Children?.Length; ++i)
        //         {
        //             if(node.Children[i]?.ID == id)
        //             {
        //                 return i;
        //             }
        //         }
        //     }

        //     return -1;
        // }

        public static bool IsIdentifierWithName(RawAST ast, Node node, string name)
        {
            return node?.Kind == SemanticKind.Identifier && 
                ASTNodeFactory.Identifier(ast, (DataNode<string>)node).Lexeme == name;
        }

        // public struct InvocationMatch
        // {
        //     public readonly bool IsKindMatch;
        //     public readonly bool IsNameMatch;
        //     public readonly bool IsArgumentsMatch;

        //     public bool IsMatch { get => IsKindMatch && IsNameMatch && IsArgumentsMatch; }

        //     public InvocationMatch(bool kind, bool name, bool arguments)
        //     {
        //         IsKindMatch = kind;
        //         IsNameMatch = name;
        //         IsArgumentsMatch = arguments;
        //     }
        // }

        // public static InvocationMatch IsInvocationOfForm(Node node, string functionName, SemanticKind[] argKinds)
        // {
        //     var invocation = (node as Invocation);

        //     if(invocation != null)
        //     {
        //         return IsInvocationOfForm(invocation, functionName, argKinds);
        //     }

        //     return new InvocationMatch(false, false, false);
        // }

        // public static InvocationMatch IsInvocationOfForm(Invocation invocation, string functionName, SemanticKind[] argKinds)
        // {
        //     if(IsIdentifierWithName(invocation.Subject, functionName))
        //     {
        //         if(invocation.Arguments != null)
        //         {
        //             var args = invocation.Arguments;

        //             if(args.Kind == SemanticKind.OrderedGroup)
        //             {
        //                 var og = (OrderedGroup)args;

        //                 if(argKinds.Length == og.Members?.Count)
        //                 {
        //                     // [dho] check the types match for each argument - 08/12/18
        //                     for(var i = 0; i < argKinds.Length; ++i)
        //                     {
        //                         if(argKinds[i] != ((InvocationArgument)og.Members[i]).Value.Kind)
        //                         {
        //                             return new InvocationMatch(true, true, false);
        //                         }
        //                     }
        //                 }
        //             }
        //             else if(argKinds.Length == 1)
        //             {
        //                 // [dho] single argument, make sure its expected kind - 08/12/18
        //                 return new InvocationMatch(true, true, ((InvocationArgument)args).Value.Kind == argKinds[0]);
        //             }
        //         }
        //         else
        //         {
        //             // [dho] parameterless - 08/12/18
        //             return new InvocationMatch(true, true, argKinds.Length == 0);
        //         }

        //     }

        //     return new InvocationMatch(false, false, false);
        // }

        public static bool IsMultilineString(InterpolatedString node)
        {
            // [dho] in languages like Swift there is no difference in syntax delimiters
            // between an interpolated string and a normal string literal. In order to determine
            // whether an interpolated string is multiline we look for all the constant string bits inside it
            // and see if any span more than one line - 28/10/18
            foreach (var (member, hasNext) in IterateMembers(node.Members))
            {
                if (member.Kind == SemanticKind.InterpolatedStringConstant)
                {
                    if (IsMultilineString(ASTNodeFactory.InterpolatedStringConstant(node.AST, (DataNode<string>)member)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsMultilineString(Constant<string> node)
        {
            var value = node.Value;

            // [dho] adapted from : https://stackoverflow.com/a/34903526 - 30/06/19
            int newLineLen = System.Environment.NewLine.Length;
            int lineCount = value.Length - value.Replace(System.Environment.NewLine, string.Empty).Length;
            
            if (newLineLen != 0)
            {
                lineCount /= newLineLen;
                lineCount++;
            }

            return lineCount > 1;

            // if (node.Origin?.Kind == NodeOriginKind.Source)
            // {
            //     var lines = ((SourceNodeOrigin)node.Origin).LineNumber;

            //     return lines.Start < lines.End;
            // }
            // // [dho] TODO we could look at the `Value` but that has issues because if we just
            // // count `\n` but this has two issues:
            // // - the actual match could be an escaped new line character
            // // - escape syntax may not be same across source languages
            // //
            // // Until we parse out escape sequences in the source parser, relying on interrogating
            // // the value will be buggy - 28/10/18

            // // else if(node.Value != null)
            // // {

            // // }
            // else
            // {
            //     return false;
            // }
        }

        // [dho] for a given node, eg `x`, replace with a qualified access of the form `y.x` - 23/06/19
        public static QualifiedAccess ConvertToPrefixedQualifiedAccess(RawAST ast, Node node, string prefix)
        {
            var qa = NodeFactory.QualifiedAccess(ast, node.Origin);

            ASTHelpers.Connect(ast, qa.ID, new[] {
                NodeFactory.Identifier(ast, node.Origin, prefix).Node
            }, SemanticRole.Incident);

            ASTHelpers.Replace(ast, node.ID, new[] { qa.Node });

            ASTHelpers.Connect(ast, qa.ID, new[] { node }, SemanticRole.Member);

            return qa;
        }

        public static Identifier RefactorName(RawAST ast, Node node, string lexeme)
        {
            var newName = NodeFactory.Identifier(ast, node.Origin, lexeme);

            ASTHelpers.Replace(ast, node.ID, new[] { newName.Node });

            return newName;
        }

        public static Result<List<Identifier>> ToIdentifierList(RawAST ast, Node node)
        {
            var result = new Result<List<Identifier>>();

            var items = new List<Identifier>();
            {
                if (node.Kind == SemanticKind.Identifier)
                {
                    items.Add(ASTNodeFactory.Identifier(ast, (DataNode<string>)node));
                }
                else if (node.Kind == SemanticKind.QualifiedAccess)
                {
                    var qa = ASTNodeFactory.QualifiedAccess(ast, node);

                    foreach (var (item, hasNext) in ASTNodeHelpers.IterateQualifiedAccessLTR(qa))
                    {
                        if (item.Kind == SemanticKind.Identifier)
                        {
                            items.Add(ASTNodeFactory.Identifier(ast, (DataNode<string>)item));
                        }
                        else
                        {
                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"Unsupported member kind '{item.Kind}' in view construction name", item)
                                {
                                    Hint = GetHint(item.Origin),
                                }
                            );
                        }
                    }
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Unsupported view construction name kind '{node.Kind}'", node)
                        {
                            Hint = GetHint(node.Origin)
                        }
                    );
                }
            }

            result.Value = items;

            return result;
        }
    }
}