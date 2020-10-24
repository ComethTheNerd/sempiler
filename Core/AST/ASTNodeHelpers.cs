using System.Collections.Generic;
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
        public static void PrintNode(Node node)
        {
            System.Console.WriteLine($"PRINT NODE {node.ID} : {node.Kind}\n{GetLexeme(node)}");
        }

        public static string GetLexeme(ASTNode nodeWrapper) => GetLexeme(nodeWrapper.Node);
        public static string GetLexeme(Node node)
        {
            return node?.Origin?.Lexeme;
        }

        public static Node[] GetMeta(RawAST ast, NodeID nodeID)
        {
            return ASTHelpers.QueryLiveEdgeNodes(ast, nodeID, SemanticRole.Meta);
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

        public static (Association, InterimSuspension) CreateAwait(RawAST ast, Node operand)
        {
            var awaitExp = NodeFactory.InterimSuspension(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, awaitExp.ID, new[] { operand }, SemanticRole.Operand);
            }

            var parentheses = NodeFactory.Association(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, parentheses.ID, new[] { awaitExp.Node }, SemanticRole.Subject);
            }

            return (parentheses, awaitExp);
        }


        ///<summary>Removes any outer parentheses around a node. Returns null if the input node is null, or the nested subject of an association is null</summary>
        public static Node UnwrapAssociations(RawAST ast, Node node)
        {
            var focus = node;

            // System.Console.WriteLine("UnwrapAssociations : " + focus?.Kind);

            while(focus?.Kind == SemanticKind.Association)
            {
                focus = ASTNodeFactory.Association(ast, focus).Subject;
            }

            return focus;
        }

        public static Node UnwrapInvocationArgument(RawAST ast, Node node)
        {
            if(node.Kind == SemanticKind.InvocationArgument)
            {
                return ASTNodeFactory.InvocationArgument(ast, node).Value;
            }

            return node;
        }

        public static IEnumerable<(AST.Node, bool)> IterateLiveChildren(RawAST ast, AST.ASTNode nodeWrapper) => IterateLiveChildren(ast, nodeWrapper.ID);

        public static IEnumerable<(AST.Node, bool)> IterateLiveChildren(RawAST ast, NodeID nodeID)
        {
            var edges = new List<Edge>();

            // System.Console.WriteLine("\n\nEDGES for " + nodeID);

            foreach (var edge in ASTHelpers.GetLiveEdges(ast, nodeID))
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

        public static Node LHS(RawAST ast, Node node)
        {
            if(node.Kind == SemanticKind.QualifiedAccess)
            {
                var incident = ASTNodeFactory.QualifiedAccess(ast, node).Incident;
                
                return LHS(ast, incident);
            }
            else
            {
                return node;
            }
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

        public static bool IsQualifiedAccessOfLexemesLTR(QualifiedAccess qa, List<string> lexemes, int startIndex = 0)
        {
            var ast = qa.AST;
            var index = startIndex;

            foreach(var (member, hasNext) in ASTNodeHelpers.IterateQualifiedAccessLTR(qa))
            {
                if(!ASTNodeHelpers.IsIdentifierWithName(ast, member, lexemes[index++]))
                {
                    return false;
                }
            }

            return true;
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

        public static Assignment CreateAssignment(RawAST ast, Node storage, Node value)
        {
            var assignment = NodeFactory.Assignment(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, assignment.ID, new [] { storage }, SemanticRole.Storage);

                ASTHelpers.Connect(ast, assignment.ID, new [] { value }, SemanticRole.Value);
            }

            return assignment;
        }

        public static (Invocation, LambdaDeclaration) CreateIIFE(RawAST ast, List<Node> content)
        {
            var iife = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

            var lambdaDecl = CreateLambda(ast, content);

            var parentheses = NodeFactory.Association(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, parentheses.ID, new [] { lambdaDecl.Node }, SemanticRole.Subject);
            }

            ASTHelpers.Connect(ast, iife.ID, new [] { parentheses.Node }, SemanticRole.Subject);
        

            return (iife, lambdaDecl);
        }

        public static LambdaDeclaration CreateLambda(RawAST ast, List<Node> content)
        {
            var lambdaDecl = NodeFactory.LambdaDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                // var asyncFlag = NodeFactory.Meta(
                //     ast,
                //     new PhaseNodeOrigin(PhaseKind.Transformation),
                //     MetaFlag.Asynchronous
                // );

                // ASTHelpers.Connect(ast, lambdaDecl.ID, new [] { asyncFlag.Node }, SemanticRole.Meta);

                var body = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                {
                    ASTHelpers.Connect(ast, body.ID, content.ToArray(), SemanticRole.Content);
                }

                ASTHelpers.Connect(ast, lambdaDecl.ID, new [] { body.Node }, SemanticRole.Body);
            }

            return lambdaDecl;
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
                                new NodeMessage(MessageKind.Error, $"Member has unsupported kind '{item.Kind}' for identifier list", item)
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
                        new NodeMessage(MessageKind.Error, $"Node has unsupported kind '{node.Kind}' for identifier list", node)
                        {
                            Hint = GetHint(node.Origin)
                        }
                    );
                }
            }

            result.Value = items;

            return result;
        }

        public static int IndexOfArgument(string[] parameterNames, Identifier argName)
        {
            int index = -1;

            if(argName != null && parameterNames != null)
            {
                var argNameLexeme = argName.Lexeme;

                for(int i = 0; i < parameterNames.Length; ++i)
                {
                    if(parameterNames[i] == argNameLexeme)
                    {
                        index = i;
                        break;
                    }
                }
            }

            return index;
        }

        ///<summary>[dho] Returns a string array of the raw parameter name lexemes, and `null` at any
        /// index that corresponds to a parameter without a name that is an `Identifier` (eg. a destructuring) - 23/09/19</summary>
        public static string[] ExtractParameterNameLexemes(RawAST ast, Node[] parameters)
        {
            var paramNameLexemes = new string[parameters.Length];

            for(int i = 0; i < parameters.Length; ++i)
            {
                var p = parameters[i];
                System.Diagnostics.Debug.Assert(p.Kind == SemanticKind.ParameterDeclaration);

                var parameter = ASTNodeFactory.ParameterDeclaration(ast, p);
                var symbol = parameter.Label ?? parameter.Name;

                if(symbol?.Kind == SemanticKind.Identifier)
                {
                    paramNameLexemes[i] = ASTNodeFactory.Identifier(ast, (DataNode<string>)symbol).Lexeme;
                }
            }

            return paramNameLexemes;
        }

        public static List<Node> FilterNonKindMatches(IEnumerable<Node> nodes, SemanticKind kind)
        {
            List<Node> result = new List<Node>();

            foreach(var node in nodes)
            {
                if(node.Kind != kind)
                {
                    result.Add(node);
                }
            }

            return result;
        }
    }
}