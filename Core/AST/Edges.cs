using System.Collections.Generic;
using System.Diagnostics;

namespace Sempiler.AST
{
    using NodeID = System.String;

    public class Edge 
    {
        public readonly NodeID NodeID;
        public readonly SemanticRole Role;

        public Edge(NodeID nodeID, SemanticRole role)
        {
            NodeID = nodeID;
            Role = role;
        }
    }
}