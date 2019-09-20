using System.Collections.Generic;
using System.Diagnostics;

namespace Sempiler.AST
{
    using NodeID = System.String;

    public class Node
    {
        public readonly NodeID ID;
        public readonly SemanticKind Kind;
        public readonly INodeOrigin Origin;

        public Node(NodeID id, SemanticKind kind, INodeOrigin origin)
        {
            ID = id;
            Kind = kind;
            Origin = origin;
        }
    }

    public class DataNode<T> : Node 
    {
        public readonly T Data;

        public DataNode(NodeID id, SemanticKind kind, INodeOrigin origin, T data) : base(id, kind, origin)
        {
            Data = data;
        }
    }
}