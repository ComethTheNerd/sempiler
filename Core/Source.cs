namespace Sempiler
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Sempiler.Diagnostics;
    using static Sempiler.Diagnostics.DiagnosticsHelpers;

    public enum SourceKind
    {
        Directory,
        File,
        Literal,
        //Remote
    }

    // [dho] 05/04/19
    ///<summary>
    /// Flags that indicate when the source should be evaluated
    ///</summary>
    [Flags]
    public enum SourceIntent 
    {
        // [dho] 05/04/19
        ///<summary>
        /// The source should be evaluated at compile time (eg. plugin code)
        ///</summary>
        CompileTime = 0x2,
        // [dho] 05/04/19
        ///<summary>
        /// The source should be evaluated at run time (eg. app code)
        ///</summary>
        RunTime = 0x2,
    }

    public interface ISource
    {
        SourceKind Kind { get; }

        // string URI { get; set; }
        // string Text { get; set; }


        // int Start { get; set; }

        // int End { get; set; }
    }

    public interface ISourceWithLocation<T> : ISource where T : ILocation
    {
        T Location { get; }

        string GetPathString();
    }

    public abstract class Source<T> : ISourceWithLocation<T> where T : ILocation
    {
        public SourceKind Kind { get; }

        public T Location { get; }

        // public string Text { get; set; }

        // public string URI { get; set; }

        // public int Start { get; set; }

        // public int End { get; set; }


        public string GetPathString()
        {
            return Location?.ToPathString();
        }

        public Source(SourceKind kind, T location) 
        {
            Kind = kind;
            Location = location;
        }
    }

    public interface ISourceDirectory : ISourceWithLocation<IDirectoryLocation>
    {
    }

    public class SourceDirectory : Source<IDirectoryLocation>, ISourceDirectory
    {
        public SourceDirectory(IDirectoryLocation location) : base(SourceKind.Directory, location)
        {
        }
    }



    public interface ISourceFile : ISourceWithLocation<IFileLocation>
    {
    }

    public class SourceFile : Source<IFileLocation>, ISourceFile
    {
        public SourceFile(IFileLocation location) : base(SourceKind.File, location)
        {
        } 
    }

    public interface ISourceLiteral : ISourceWithLocation<IFileLocation>
    {
        string Text { get; set; }
    }

    public class SourceLiteral : Source<IFileLocation>, ISourceLiteral
    {
        public string Text { get; set; }

        public SourceLiteral(string text, IFileLocation location = null) : base(SourceKind.Literal, location)
        {
            Text = text;
        }
    }

    public interface ISourceProvider
    {
        ISource ProvideFromPath(string absPath);
    }

    public class DefaultSourceProvider : ISourceProvider
    {
        public virtual ISource ProvideFromPath(string absPath)
        {
            return SourceHelpers.CreateFile(FileSystem.ParseFileLocation(absPath));
        }
    }

    public static class SourceHelpers
    {
        public struct SourceFilePatternMatchInput 
        {
            public readonly string BaseDirPath;
            public readonly string SearchPattern;

            public SourceFilePatternMatchInput(string baseDirPath, string searchPattern)
            {
                BaseDirPath = baseDirPath;
                SearchPattern = searchPattern;
            }
        }

        public struct SourceFilePatternMatchOutput 
        {
            public readonly string BaseDirPath;
            public readonly IEnumerable<string> RelativeFilePaths;

            public SourceFilePatternMatchOutput(string baseDirPath, IEnumerable<string> relativeFilePaths)
            {
                BaseDirPath = baseDirPath;
                RelativeFilePaths = relativeFilePaths;
            }
        }

        public static Result<IEnumerable<ISourceFile>> EnumerateSourceFilePatternMatches(IEnumerable<SourceFilePatternMatchInput> searchInputs)
        {
            var result = new Result<IEnumerable<ISourceFile>>();

            List<SourceFilePatternMatchOutput> searchOutputs = new List<SourceFilePatternMatchOutput>();

            foreach(var input in searchInputs)
            {
                var baseDirPath = input.BaseDirPath;
                var searchPattern = input.SearchPattern;

                // [dho] first we ask the file system for all file paths that match the search pattern - 13/04/19
                var r = FileSystem.EnumerateFiles(baseDirPath, searchPattern);

                result.AddMessages(r);

                if(!HasErrors(r))
                {
                    searchOutputs.Add(new SourceFilePatternMatchOutput(baseDirPath, r.Value));
                }
            }

            // [dho] now we take care to dedup the results and create a lazy enumerator - 13/04/19
            result.Value = EnumerateDedupedSourceFiles(searchOutputs);

            return result;
        }

        public static IEnumerable<ISourceFile> EnumerateDedupedSourceFiles(IEnumerable<SourceFilePatternMatchOutput> searchOutputs)
        {
            var result = new Result<IEnumerable<ISourceFile>>();

            var filesSeen = new Dictionary<string, ISourceFile>();

            foreach(var output in searchOutputs)
            {
                var baseDirPath = output.BaseDirPath;
                var relativeFilePaths = output.RelativeFilePaths;

                if(relativeFilePaths != null)
                {
                    foreach(var filePath in relativeFilePaths)
                    {
                        var fullFilePath = Path.GetFullPath(filePath); // otherwise it might contain '../../' etc.

                        if(filesSeen.ContainsKey(fullFilePath))
                        {
                            // var f = filesSeen[fullFilePath];

                            // // [dho] combine intent flags - 05/04/19
                            // filesSeen[fullFilePath] = SourceHelpers.CreateFile(f.Intent | intent, f.Location);
                            continue;
                        }
                        else
                        {
                            // // [dho] if the file will be emitted somewhere, then we resolve the file location
                            // // relative to the `baseDirPath`, because that will be the relative path appended
                            // // to the output dir - 05/04/19
                            // // [dho] NOTE will be null if `fullFilePath` is not a child of `baseSourceDirPath` - 05/04/19
                            // var location = ParseChildSourceFileLocation(baseDirPath, fullFilePath);

                            // if(location == null)
                            // {
                            //     result.AddMessages(
                            //         new Message(MessageKind.Warning, $"File '{fullFilePath}' is not a descendant of base path '{baseDirPath}'")
                            //     );

                            //     // [dho] fallback to just using the path as is - 05/04/19
                            //     location = FileSystem.ParseFileLocation(fullFilePath);
                            // }
                            
                            // [dho] NOTE have commented out the child location stuff because I think all that relative
                            // emission path logic was based on the old compiler way of source and out dir for a single artifact..
                            // if that assumption is not correct then we will need to revise how these paths are handled, because `add_sources`
                            // expects an absolute file path - 08/05/19
                            var location = FileSystem.ParseFileLocation(fullFilePath);

                            yield return filesSeen[fullFilePath] = CreateFile(location);
                        }
                    }
                }
            }
        }

        // private static IEnumerable<Result<ISourceFile>> CreateChildSourceFileEnumerator(string baseDirPath, IEnumerable<IEnumerable<string>> filePathSetsCollection, SourceIntent intent)
        // {
        //     var filesSeen = new Dictionary<string, bool>();

        //     foreach(var filePathSet in filePathSetsCollection)
        //     {
        //         foreach(var filePath in filePathSet)
        //         {
        //             var fullFilePath = Path.GetFullPath(filePath); // otherwise it might contain '../../' etc.

        //             if(filesSeen.ContainsKey(fullFilePath))
        //             {
        //                 continue;
        //             }

        //             filesSeen[fullFilePath] = true;

        //             var fResult = new Result<ISourceFile>();

        //             var location = ParseSourceFileLocation(baseDirPath, fullFilePath);

        //             if(location != null)
        //             {
        //                 fResult.Value = CreateFile(intent, location);
        //             }
        //             else
        //             {
        //                 fResult.AddMessages(
        //                     new Message(MessageKind.Warning, $"Ignoring file '{fullFilePath}' because it is not a descendant of base path '{baseDirPath}'")
        //                 );
        //             }

        //             yield return fResult;
        //         }
        //     }
        // }





        // public static ISource CreateFromPath(string filePath)
        // {
        //     var parts = filePath.Split(Path.DirectorySeparatorChar);

        //     var last = parts[parts.Length - 1];

        //     var extension = Path.GetExtension(last);

        //     /* 
        //         try
        //         {
        //             var attrs = FileAttributes attributes = File.GetAttributes(path);

        //             if(attrs & FileAttributes.Directory)
        //             {

        //             }
        //             else
        //             {

        //             }

        //         }
        //         catch(Exception)
        //         {

        //         }
            
        //      */


        //     // [dho] looking forward to this breaking in some situation I 
        //     // haven't yet thought of - 07/08/18
        //     if(String.IsNullOrEmpty(extension))
        //     {
        //         return Directory(parts);
        //     }
        //     else
        //     {
        //         var dirPath = new string[parts.Length - 1];

        //         Array.Copy(parts, dirPath, parts.Length - 1);

        //         var name = last.Substring(0, last.Length - extension.Length);
                
        //         extension = extension.Substring(1); // strip the dot

        //         return File(dirPath, name, extension);
        //     }
        // }

        /// <returns>Returns null if `filePath` is not a child of `baseDirPath`</returns>
        public static IFileLocation ParseChildSourceFileLocation(string baseDirPath, string filePath)
        {
            if(filePath.IndexOf(baseDirPath) == 0)
            {
                return Sempiler.FileSystem.ParseFileLocation(
                    filePath.Substring(baseDirPath.Length)
                );
            }

            return null;
        }

        public static ISourceFile CreateFile(IFileLocation location)
        {            
            return new SourceFile(location);
        }
    
        public static ISourceDirectory CreateDirectory(IDirectoryLocation location)
        {
            return new SourceDirectory(location);
        }

        public static ISourceLiteral CreateLiteral(string text, IFileLocation location = null)
        {
            return new SourceLiteral(text, location);
        }

        //public static Source Remote(string uri)
        //{
        //    return new Source(SourceKind.Remote)
        //    {
        //        URI = uri
        //    };
        //}


        // public static Result<object> Write(ISource source)
        // {
        //     throw new NotImplementedException();
        // //     var result = new Result<object>();

        // //     if(string.IsNullOrEmpty(source.URI))
        // //     {
        // //         // add error
        // //         x
        // //     }
        // //     else if(source.Text == null)
        // //     {
        // //         x
        // //     }
        // //     else
        // //     {
        // //         try
        // //         {
        // //             var path = Path.Combine(
        // //                 Directory.GetCurrentDirectory(), source.URI
        // //             );

        // //             System.IO.File.WriteAllText(path, source.Text);
        // //         }
        // //         catch(ArgumentException)
        // //         {
                    
        // //         }
        // //         catch(PathTooLongException)
        // //         {
                    
        // //         }
        // //         catch(DirectoryNotFoundException)
        // //         {

        // //         }
        // //         catch(IOException)
        // //         {

        // //         }
        // //         catch(UnauthorizedAccessException)
        // //         {

        // //         }
        // //     }

        // //     return result;

        // }

        // private static Result<string[]> DirectoryPath(ISource source)
        // {
        //     var result = new Result<string[]>();

        //     switch(source.Kind)
        //     {
        //         case SourceKind.File:
        //             result.Value = ((ISourceFile)source).ParentDirPath;
        //         break;

        //         case SourceKind.Directory:
        //             result.Value = ((ISourceDirectory)source).Path;
        //         break;

        //         default:{
        //             result.AddError(new Sempiler.Diagnostics.Error($"Unsupported source kind")
        //             {
        //                 Data = new { source.Kind }
        //             });
        //         }
        //         break;
        //     }

        //     return result;
        // }

        // static Result<List<string>> LongestPrefix(List<ISource> sources)
        // {
        //     var result = new Result<List<string>>();

        //     var prefix = new List<string>();

        //     if(sources.Count > 0)
        //     {
        //         var paths = new string[sources.Count][];

        //         for(var i = 0; i < sources.Length)


        //         var first = sources[0];
        //         var (bais)

        //         for(var i = 0; i < basisPath.Length; ++i)
        //         {
        //             var pathPart = basisPath[i];

        //             for(var f = 1; f < sources.Count; ++f)
        //             {
        //                 var absPathParts = absSourceFilePathParts[f];

        //                 if(i >= absPathParts.Length || absPathParts[i] != pathPart)
        //                 {
        //                     return prefix;
        //                 }
        //             }

        //             prefix.Add(pathPart);
        //         }
        //     }

        //     return prefix;
        // }

    }
}