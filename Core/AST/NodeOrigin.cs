
namespace Sempiler.AST
{
    public interface INodeOrigin
    {
        NodeOriginKind Kind { get; }
        string Lexeme { get; }
    }

    public enum NodeOriginKind
    {
        Source,
        Phase
    }

    public abstract class NodeOrigin : INodeOrigin
    {
        public NodeOriginKind Kind { get; }
        public string Lexeme { get; }

        public NodeOrigin(NodeOriginKind kind, string lexeme)
        {
            Kind = kind;
            Lexeme = lexeme;
        }
    }

    public class PhaseNodeOrigin : NodeOrigin
    {
        public readonly PhaseKind Phase;

        public readonly string Description;

        public PhaseNodeOrigin(PhaseKind phase, string lexeme = null, string description = null) : base(NodeOriginKind.Phase, lexeme)
        {
            Phase = phase;
            Description = description;
        }
    }


    public class SourceNodeOrigin : NodeOrigin
    {
        public readonly ISource Source;
        public readonly Sempiler.Range LineNumber;
        public readonly Sempiler.Range ColumnIndex;
        public readonly Sempiler.Range Pos;

        public SourceNodeOrigin(ISource source, Sempiler.Range lineNumber, Sempiler.Range columnIndex, Sempiler.Range pos, string lexeme) : base(NodeOriginKind.Source, lexeme)
        {
            Source = source;
            LineNumber = lineNumber;
            ColumnIndex = columnIndex;
            Pos = pos;
        }
    }

}