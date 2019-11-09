namespace Sempiler
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Sempiler.Diagnostics;
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using Sempiler.Emission;
    using System.Runtime.Loader;

    public interface ILocation 
    {
        string ToPathString();
    }

    public interface IDirectoryLocation : ILocation
    {
        string[] Path { get; set; }
    }

    public class DirectoryLocation : IDirectoryLocation
    {
        public string[] Path { get; set; }
        public DirectoryLocation(string[] path)
        {
            Path = path;
        }

        public string ToPathString()
        {
            return String.Join(System.IO.Path.DirectorySeparatorChar + "", Path);
        }
    }

    public interface IFileLocation : ILocation
    {
        IDirectoryLocation ParentDir { get; set; }

        string Name { get; set; }

        string Extension { get; set; }
    }

    public class FileLocation : IFileLocation 
    {
        public IDirectoryLocation ParentDir { get; set; }
        public string Name { get; set; }

        public string Extension { get; set; }

        public FileLocation(IDirectoryLocation parentDir, string name, string extension)
        {
            ParentDir = parentDir;
            Name = name;
            Extension = extension;
        }

        public string ToPathString()
        {
            var sb = new System.Text.StringBuilder();

            var parentDirPath = ParentDir.ToPathString();

            if(parentDirPath.Length > 0)
            {
                sb.Append($"{parentDirPath}{System.IO.Path.DirectorySeparatorChar}");
            }
            
            sb.Append(Name);

            if(!String.IsNullOrEmpty(Extension))
            {
                sb.Append($".{Extension}");
            
            }
            
            return sb.ToString();
        }
    }

    public static class FileSystem
    {
        public static bool IsDLLPath(string path)
        {
            return Path.GetExtension(path)?.ToLower() == "dll";
        }

        public static bool IsChildPath(string basePath, string childPath)
        {
            var cwd = Directory.GetCurrentDirectory();

            return Resolve(cwd, childPath).IndexOf(Resolve(cwd, basePath)) == 0;
        }

        public static Result<Type> GetTypeFromPath(string asmPath, string typeName)
        {   
            var result = new Result<Type>();

            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(asmPath);

                // [dho] NOT using `assembly.GetType()` because it requires a fully qualified type
                // name (including the enclosing namespaces), and right now we look for a name
                // that matches the DLL file name for convenience, so instead we just iterate the 
                // exported types from the assembly and use the first that matches the name - 28/11/18
                foreach(var exportedType in assembly.GetExportedTypes())
                {
                    // [dho] the `typeName` could be fully qualified if the caller wants to
                    // specify it - 28/11/18
                    if(exportedType.FullName == typeName || exportedType.Name == typeName)
                    {
                        result.Value = exportedType;
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                result.AddMessages(
                    CreateErrorFromException(e)
                );
            }

            return result;
        }
        public static string Resolve(string absBasePath, string relativePath)
        {
            // [dho] `GetFullPath` will remove relative symbols like '../../' etc. - 25/08/18
            return Path.GetFullPath(
                Path.Combine(
                    absBasePath,
                    relativePath
                )
            ).TrimEnd(Path.DirectorySeparatorChar);
        }

        public static Result<IEnumerable<string>> EnumerateFiles(string basePath, string searchPattern)
        {
            var result = new Result<IEnumerable<string>>();

            var resolvedPath = Resolve(basePath, searchPattern);

            var attrs = result.AddMessages(GetAttributes(resolvedPath));

            if((attrs & FileAttributes.Directory) == FileAttributes.Directory)
            {
                try
                {   
                    result.Value = Directory.EnumerateFiles(resolvedPath, "*.*", System.IO.SearchOption.AllDirectories);
                }
                catch(Exception exception)
                {
                    result.AddMessages(
                        CreateErrorFromException(exception)
                    );
                }
            }
            else if(attrs > 0)
            {
                result.Value = new [] {
                    resolvedPath
                };
            }

            return result;
        }

        public static Result<FileAttributes> GetAttributes(string path)
        {
            var result = new Result<FileAttributes>();

            try
            {
                result.Value = System.IO.File.GetAttributes(path);
            }
            catch(Exception exception)
            {
                result.AddMessages(
                    CreateErrorFromException(exception)
                );

                result.Value = 0;
            }

            return result;
        }

        public static Result<object> SetAttributes(string path, FileAttributes attributes)
        {
            var result = new Result<object>();

            try
            {
                System.IO.File.SetAttributes(path, attributes);
            }
            catch(Exception exception)
            {
                result.AddMessages(
                    CreateErrorFromException(exception)
                );
            }

            return result;
        }

        public static IDirectoryLocation ParseDirectoryLocation(string fullPath)
        {
            return CreateDirectoryLocation(
                fullPath.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar)
            );
        }

        public static IFileLocation ParseFileLocation(string fullFilePath)
        {
            var pBits = fullFilePath.Split(Path.DirectorySeparatorChar);

            var parentDirPath = new string[pBits.Length - 1];
            
            Array.Copy(pBits, parentDirPath, parentDirPath.Length);

            var namePart = pBits[pBits.Length - 1];

            var extension = Path.GetExtension(namePart).ToLower();

            var name = namePart.Substring(0, namePart.Length - extension.Length);

            extension = extension.TrimStart('.');

            var location = CreateFileLocation(parentDirPath, name, extension);

            return location;
        }

        public static IDirectoryLocation CreateDirectoryLocation(string[] dirPath)
        {
            return new DirectoryLocation(dirPath);
        }

        public static IFileLocation CreateFileLocation(string[] dirPath, string name, string extension)
        {            
            return new FileLocation(CreateDirectoryLocation(dirPath), name, extension);
        }

        public static IFileLocation CreateFileLocation(IDirectoryLocation parentDir, string name, string extension)
        {            
            return new FileLocation(parentDir, name, extension);
        }

        public static Task<Result<Dictionary<string, OutFile>>> Write(string absOutDirPath, OutFileCollection outFileCollection, CancellationToken token)
        {
            // [dho] TODO optimize, and also use CancellationToken - 26/08/18

            var result = new Result<Dictionary<string, OutFile>>();

            if(File.Exists(absOutDirPath))
            {
                result.AddMessages(
                    new Message(MessageKind.Error, $"File writer out directory points to a file : '{absOutDirPath}'")
                );
            }
            else
            {
                var written = new Dictionary<string, OutFile>();

                object l = new object();

                Parallel.ForEach(outFileCollection, item => {

                    var absPath = FileSystem.Resolve(absOutDirPath, item.Path);

                    try
                    {
                        Directory.CreateDirectory(
                            Directory.GetParent(absPath).ToString()
                        );

                        File.WriteAllBytes(absPath, item.Emission.Serialize());

                        lock(l)
                        {
                            written[absPath] = item;
                        }
                    }
                    catch(Exception exception)
                    {
                        lock(l)
                        {
                            result.AddMessages(
                                CreateErrorFromException(exception, $"Failed to write to '{absPath}'")
                            );
                        }
                    }
                });

                result.Value = written;
            }

            return Task.FromResult(result);
        }

        public static Task<Result<Dictionary<string, bool>>> Delete(IEnumerable<string> absPaths)
        {
            var result = new Result<Dictionary<string, bool>>();

            Dictionary<string, bool> deleted = new Dictionary<string, bool>();

            object l = new object();

            Parallel.ForEach(absPaths, absPath => {
                try
                {
                    var didDelete = false;

                    if(File.Exists(absPath))
                    {
                        System.IO.File.Delete(absPath);
                        didDelete = true;
                    }
                    else if(Directory.Exists(absPath))
                    {
                        DeleteDirectory(absPath);
                        didDelete = true;
                    }   

                    lock(l)
                    {
                        deleted[absPath] = didDelete;
                    }
                }
                catch(Exception exception)
                {
                    lock(l)
                    {
                        result.AddMessages(
                            CreateErrorFromException(exception, $"Failed to delete '{absPath}'")
                        );

                        deleted[absPath] = false;
                    }
                }
            });

            result.Value = deleted;

            return Task.FromResult(result);
        }

        // [dho] adapted from : https://stackoverflow.com/a/1703799 - 19/10/19
        private static void DeleteDirectory(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException) 
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }
    }
}