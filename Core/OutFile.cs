namespace Sempiler.Emission
{
    using Sempiler.Diagnostics;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using System;

    public interface IOutFileCollection : IEnumerable<OutFile>
    {
        bool Contains(ILocation location);

        int Count { get; }

        IOutFileContent this[ILocation location] { get; set; }
    }

    public class OutFileCollection : IOutFileCollection 
    {
        protected Dictionary<string, IOutFileContent> outFiles;

        public OutFileCollection()
        {
            outFiles = new Dictionary<string, IOutFileContent>();
        }

        public int Count { get => outFiles.Count; }

        public bool Contains(ILocation location)
        {
            return outFiles.ContainsKey(location.ToPathString());
        }

        // caller responsible for not asking for same file path in different ways :)
        public IOutFileContent this[ILocation location]
        {
            get => outFiles[location.ToPathString()];

            set => outFiles[location.ToPathString()] = value;
        }

        public void AddAll(OutFileCollection other)
        {
            foreach(var kv in other.outFiles)
            {
                System.Diagnostics.Debug.Assert(!this.outFiles.ContainsKey(kv.Key));

                this.outFiles[kv.Key] = kv.Value;
            }
        }

        public IEnumerator<OutFile> GetEnumerator()
        {
            foreach(var item in outFiles)
            {
                yield return new OutFile(item.Key, item.Value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public interface IOutFileContent
    {
        byte[] Serialize();
    }

    public struct OutFile
    {
        public readonly string Path;
        public readonly IOutFileContent Emission;

        public OutFile(string path, IOutFileContent emission)
        {
            Path = path;
            Emission = emission;
        }
    }

    public struct EmissionMarker
    {
        public readonly AST.Node Node;
        public readonly int LineNumber;
        public readonly int ColumnIndex;
        public readonly int StartPos;
        public readonly int EndPos;

        public EmissionMarker(AST.Node node, int lineNumber, int columnIndex, int startPos, int endPos)
        {
            Node = node;
            LineNumber = lineNumber;
            ColumnIndex = columnIndex;
            StartPos = startPos;
            EndPos = endPos;
        }
    }

    ///<summary>
    /// Do not insert new line characters literally! Use the instance method `AppendBelow` to avoid
    /// positions being wrong
    ///</summary>
    public interface IEmission : IOutFileContent
    {
        EmissionKind Kind { get; set; }

        void Indent();

        void Outdent();

        EmissionMarker Append(AST.ASTNode nodeWrapper, params string[] tex);

        EmissionMarker AppendBelow(AST.ASTNode nodeWrapper, params string[] text);

        EmissionMarker Append(AST.Node node, params string[] tex);

        EmissionMarker AppendBelow(AST.Node node, params string[] text);

        EmissionMarker Marker(AST.Node node);

        int GetPositionFromLineAndColumn(int lineNumber, int column);

        ///<summary>Returns markers for the nodes that start at the given position in the emission</summary>
        IList<EmissionMarker> GetMarkersAtPosition(int position);

        IList<EmissionMarker> GetClosestMarkersAtOrBeforePosition(int position);

        int Length { get; }


        // string ToString();
    }

    public enum EmissionKind
    {
        File,
        Literal
    }

    ///<summary>
    /// Do not insert new line characters literally! Use the instance method `AppendBelow` to avoid
    /// positions being wrong
    ///</summary>
    public abstract class Emission : IEmission
    {
        const string IndendationSequence = "    ";

        protected int mIndendation;

        protected int mLineNumber;
        
        protected StringBuilder mBuilder;

        protected Dictionary<string, EmissionMarker> mMarkers;

        // [dho] dictionary because it is not 0-based, line number starts at 1 so to avoid
        // confusion with index 0 in a list we use a mapping - 09/08/18
        protected Dictionary<int, int> mLineStartPositions;

        public EmissionKind Kind { get ; set; }

        public int Length { get => mBuilder.Length; }

        protected Emission(EmissionKind kind)
        {
            Kind = kind;
            mIndendation = 0;
            mLineNumber = 1;
            mLineStartPositions = new Dictionary<int, int>
            {
                [mLineNumber] = 0
            };

            mBuilder = new StringBuilder();
            mMarkers = new Dictionary<string, EmissionMarker>();
        }
        
        public void Indent()
        {
            ++mIndendation; 
        }

        public void Outdent()
        {
            if(mIndendation > 0)
            {
                --mIndendation;
            }
        }


        public EmissionMarker Append(AST.ASTNode nodeWrapper, params string[] text) => Append(nodeWrapper.Node, text);

        public EmissionMarker AppendBelow(AST.ASTNode nodeWrapper, params string[] text) => AppendBelow(nodeWrapper.Node, text);

        public EmissionMarker Append(AST.Node node, params string[] text)
        {
            if(mMarkers.ContainsKey(node.ID))
            {
                for(int i = 0; i < text.Length; ++i) mBuilder.Append(text[i]);

                var currentPos = mMarkers[node.ID];

                var newPos = new EmissionMarker(
                    node,
                    currentPos.LineNumber, 
                    currentPos.ColumnIndex, 
                    currentPos.StartPos,  
                    mBuilder.Length - 1
                );

                return mMarkers[node.ID] = newPos;
            }   
            else
            {
                var start = mBuilder.Length;

                for(int i = 0; i < text.Length; ++i) mBuilder.Append(text[i]);

                var columnIndex = start - mLineStartPositions[mLineNumber];

                var end = mBuilder.Length - 1;

                return mMarkers[node.ID] = new EmissionMarker(
                    node,
                    mLineNumber, 
                    columnIndex, 
                    start,  
                    end
                );
            }         
        }

        public EmissionMarker AppendBelow(AST.Node node, params string[] text)
        {
            EmissionMarker marker = Marker(node);

            foreach(var line in text)
            {
                mBuilder.AppendLine();

                // [dho] move to next line. NOTE we do not assume character width of the
                // new line, we just take use the current string builder length after
                // insertion.. so the carriage return sequence could be 100 chars long and we
                // wouldn't need to care - 09/08/18
                mLineStartPositions[++mLineNumber] = mBuilder.Length - 1;

                for(int i = 0; i < mIndendation; ++i)
                {
                    mBuilder.Append(IndendationSequence);
                }

                marker = Append(node, line);
            }

            return marker;
        }

        public EmissionMarker Marker(AST.Node node)
        {
            if(mMarkers.ContainsKey(node.ID))
            {
                return mMarkers[node.ID];
            }
            else
            {
                return new EmissionMarker(node, -1, -1, -1, -1);
            }
        }

        public int GetPositionFromLineAndColumn(int lineNumber, int column)
        {
            if(mLineStartPositions.ContainsKey(lineNumber) && column >= 0)
            {
                return mLineStartPositions[lineNumber] + column;
            }
            else
            {
                return -1;
            }
        }

        public IList<EmissionMarker> GetMarkersAtPosition(int position)
        {
            var markers = new List<EmissionMarker>();

            foreach(var kv in mMarkers)
            {
                var marker = kv.Value;

                if(marker.StartPos == position)
                {
                    markers.Add(marker);
                }
            }

            return markers;
        }

        public IList<EmissionMarker> GetClosestMarkersAtOrBeforePosition(int position)
        {
            List<EmissionMarker> markers = null;
            var bestDistance = int.MaxValue;

            foreach(var kv in mMarkers)
            {
                var marker = kv.Value;
                
                var distance = position - marker.StartPos;

                if(distance < 0)
                {
                    // [dho] we only care about markers that start BEFORE
                    // the given position - 29/08/18
                    continue;
                }
                else if(distance < bestDistance)
                {
                    markers = new List<EmissionMarker>();

                    markers.Add(marker);

                    bestDistance = distance;
                }
                else if(distance == bestDistance)
                {
                    markers.Add(marker);
                }
            }

            return markers ?? (new List<EmissionMarker>());
        }

        public byte[] Serialize()
        {
            return System.Text.Encoding.UTF8.GetBytes(mBuilder.ToString());
        }
    }

    public interface IFileEmission : IEmission
    {
        IFileLocation Destination { get; set; }
    }

    public class FileEmission : Emission, IFileEmission
    {
        public IFileLocation Destination { get; set; }

        public FileEmission(IFileLocation destination) : base(EmissionKind.File)
        {
            Destination = destination;
        }
    }

    // public interface ILiteralEmission : IEmission
    // {
    // }

    // public class LiteralEmission : Emission, ILiteralEmission
    // {
    //     public LiteralEmission() : base(EmissionKind.Literal)
    //     {
    //     }
    // }

    public class RawOutFileContent : IOutFileContent
    {
        public readonly byte[] Content;

        public RawOutFileContent(byte[] content)
        {
            Content = content;
        }

        public byte[] Serialize() 
        {
            return Content;
        }
    } 


    public static class OutFileHelpers
    {
        // public static Result<object> Merge(IOutFileCollection target, params IOutFileCollection[] sources)
        // {
        //     var result = new Result<object>();

        //     foreach(var source in sources)
        //     {
        //         foreach (var item in source)
        //         {
        //             switch (item.Emission.Kind)
        //             {
        //                 case EmissionKind.File:
        //                     {
        //                         var location = ((IFileEmission)item.Emission).Destination;

        //                         if(target.Contains(location))
        //                         {
        //                             result.AddMessages(
        //                                 new Message(MessageKind.Warning, $"Artifact merge overwriting existing emission at location : '{location.ToPathString()}'")
        //                             );
        //                         }

        //                         target[location] = item.Emission;
        //                     }
        //                     break;

        //                 default:
        //                     {
        //                         result.AddMessages(
        //                             new Message(MessageKind.Error, $"Emission has unsupported kind '{item.Emission.Kind}'")
        //                         );
        //                     }
        //                     break;
        //             }
        //         }
        //     }

        //     return result;
        // }
    }
}