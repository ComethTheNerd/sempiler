using System;
using Sempiler.AST;
using Sempiler.Diagnostics;

namespace Sempiler.Parsing.Diagnostics
{
    public class ParsingMessage : Message
    {
        public readonly Sempiler.Range Pos;

        public ParsingMessage(MessageKind kind, string description, Sempiler.Range pos) : base(kind, description)
        {
            Pos = pos;
        }
    }
}