using System;
using Sempiler.AST;
using Sempiler.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Sempiler.AST.Diagnostics
{
    public class NodeMessage : Message
    {
        public readonly Node Node;

        public NodeMessage(MessageKind kind, string description, ASTNode nodeWrapper) : this(kind, description, nodeWrapper.Node)
        {}

        public NodeMessage(MessageKind kind, string description, Node node) : base(kind, description)
        {
            Node = node;
        }
    }

    public static class DiagnosticsHelpers
    {
        public const string SplitOnUpperCaseLettersRegexPattern = @"(?<!^)(?=[A-Z])";

        public static FileMarker GetHint(INodeOrigin origin)
        {
            switch(origin?.Kind)
            {
                case NodeOriginKind.Source:{
                    var sourceOrigin = (SourceNodeOrigin)origin;

                    return new FileMarker
                    {
                        File = (sourceOrigin.Source as ISourceWithLocation<IFileLocation>).Location,
                        LineNumber = RangeHelpers.Clone(sourceOrigin.LineNumber),
                        ColumnIndex = RangeHelpers.Clone(sourceOrigin.ColumnIndex),
                        Pos = RangeHelpers.Clone(sourceOrigin.Pos)
                    };
                };

                default:
                    return null;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<object> CreateUnsupportedFeatureResult(ASTNode nodeWrapper) => CreateUnsupportedFeatureResult(nodeWrapper.Node);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<object> CreateUnsupportedFeatureResult(Node node)
        {
            var featureName = System.String.Join(" ", 
                System.Text.RegularExpressions.Regex.Split(node.Kind.ToString(), SplitOnUpperCaseLettersRegexPattern)
            ).ToLower();

            return CreateUnsupportedFeatureResult(node, featureName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<object> CreateUnsupportedFeatureResult(IEnumerable<Node> nodes, string featureName)
        {
            var result = new Result<object>();

            foreach(var node in nodes) 
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node, featureName));
            }

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<object> CreateUnsupportedFeatureResult(ASTNode nodeWrapper, string featureName) => CreateUnsupportedFeatureResult(nodeWrapper.Node, featureName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<object> CreateUnsupportedFeatureResult(Node node, string featureName)
        {
            var result = new Result<object>();

            result.AddMessages(new NodeMessage(MessageKind.Error, $"The use of {featureName} is not currently supported ({node.ID})", node)
            {
                Hint = GetHint(node.Origin),
                // Tags = DiagnosticTags
            });

            return result;
        }
    }
}