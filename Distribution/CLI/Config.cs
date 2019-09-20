using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Sempiler;
using Sempiler.AST;
using Sempiler.Consumption;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Emission;
using Sempiler.Transformation;
using Sempiler.Parsing;
using Newtonsoft.Json;

namespace Sempiler.Distribution.CLI
{
    public class Config
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name;

        [JsonProperty("compilation", Required = Required.Always)]
        public Dictionary<string, CompilationConfig> Compilation;

        [JsonProperty("parsing", Required = Required.Always)]
        public Dictionary<string, ParsingConfig> Parsing;

        [JsonProperty("transformation")]
        public Dictionary<string, TransformationConfig> Transformation;

        [JsonProperty("emission")]
        public Dictionary<string, EmissionConfig> Emission;

        [JsonProperty("consumption")]
        public Dictionary<string, ConsumptionConfig> Consumption;

        [JsonProperty("source", Required = Required.Always)]
        public Dictionary<string, SourceConfig> Source;

        [JsonProperty("destination")]
        public Dictionary<string, DestinationConfig> Destination;
    }

    public class CompilationConfig
    {
        // [JsonProperty("options")]
        // public Dictionary<string, object> Options;

        [JsonProperty("compiler", Required = Required.Always)]
        public object Compiler; // string || InlineCompilerConfig

        [JsonProperty("source", Required = Required.Always)]
        public string Source;

        [JsonProperty("destination")]
        public string Destination;
    }

    public class InlineCompilerConfig
    {
        [JsonProperty("parsing", Required = Required.Always)]
        public string Parsing;

        [JsonProperty("transformation")]
        public string[] Transformation;

        [JsonProperty("emission")]
        public string Emission;

        [JsonProperty("consumption")]
        public string[] Consumption;
    }

    public class ParsingConfig
    {
        [JsonProperty("parser", Required = Required.Always)]
        public string Parser;
    }

    public class TransformationConfig
    {
        [JsonProperty("transformer", Required = Required.Always)]
        public string Transformer;
    }

    public class EmissionConfig
    {
        [JsonProperty("emitter", Required = Required.Always)]
        public string Emitter;

        // [JsonProperty("target")]
        // public string Target;
    }

    public class ConsumptionConfig
    {
        [JsonProperty("consumer", Required = Required.Always)]
        public string Consumer;

        [JsonProperty("args", Required = Required.Always)]
        public Dictionary<string, object> Arguments;
    }

    public class SourceConfig
    {
        [JsonProperty("path", Required = Required.Always)]
        public string Path;

        [JsonProperty("dirs")]
        public SourceDirectoryConfig[] Directories;

        [JsonProperty("files")]
        public SourceFileConfig[] Files;

        [JsonProperty("literals")]
        public SourceLiteralConfig[] Literals;
    }

    public class RelativeMainConfig
    {
        [JsonProperty("path", Required = Required.Always)]
        public string Path;
    }


    public class SourceDirectoryConfig
    {
        [JsonProperty("path", Required = Required.Always)]
        public string Path;
    }

    public class SourceFileConfig
    {
        [JsonProperty("path", Required = Required.Always)]
        public string Path;
    }

    public class SourceLiteralConfig
    {
        [JsonProperty("path")]
        public string Path;

        [JsonProperty("text", Required = Required.Always)]
        public string Text;
    }

    public class DestinationConfig
    {
        [JsonProperty("path", Required = Required.Always)]
        public string Path;
    }

    // public interface IResolvedSourceCollection : IEnumerable<Result<ISource>>
    // {
    //     IDirectoryLocation Root { get; }

    // }

    // public class ResolvedSourceCollection : IResolvedSourceCollection
    // {
    //     private ResolvedSourceCollection(IDirectoryLocation root, IEnumerable<Result<ISource>> sourceIterator)
    //     {
    //         Root = root;
    //         Source = sourceIterator;
    //     }

    //     public IDirectoryLocation Root { get; }

    //     IEnumerable<Result<ISource>> Source { get; }

    //     public IEnumerator<Result<ISource>> GetEnumerator()
    //     {
    //         return Source.GetEnumerator();
    //     }

    //     System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    //     {
    //         return GetEnumerator();
    //     }

    //     public static Result<ResolvedSourceCollection> Create(string absBasePath, string compilerID, Config config)
    //     {
    //         var result = new Result<ResolvedSourceCollection>();

    //         if(config.Compilation.ContainsKey(compilerID))
    //         {
    //             var c = config.Compilation[compilerID];

    //             var ixx = new List<IEnumerable<Result<ISource>>>();

    //             // [dho] raw source files provided in the config - 05/04/19
    //             {
    //                 var sourceID = c.Source;

    //                 if (sourceID != null)
    //                 {
    //                     if (config.Source != null && config.Source.ContainsKey(sourceID))
    //                     {
    //                         var source = config.Source[sourceID];

    //                         var absRootPath = FileSystem.Resolve(absBasePath, source.Path);

    //                         // var root = FileSystem.ParseDirectoryLocation(absRootPath);

    //                         var iterator = result.AddMessages(ConfigHelpers.EnumerateSources(absRootPath, compilerID, config));

    //                         ixx.Add(iterator);
    //                     }
    //                     else
    //                     {
    //                         result.AddMessages(
    //                             new Message(MessageKind.Error, $"Source config for compiler '{compilerID}' contains unknown source ID '{sourceID}'")
    //                         );
    //                     }
    //                 }
    //             }

    //         }


    //         /*
    //             Here we want to add source files with SourceIntent.Plugin to the Sources Collection

    //             will that tie us in to using the same parser for plugins? But even if it does, you can just
    //             compile a new plugin that selects the parser based on the input file        

            
    //             if the file already exists in the sources, then add the correct intent flag SourceIntent.RunTime | SourceIntent.CompileTime




    //          */

    //         return result;
    //     }
    // }


//     public static class ConfigHelpers
//     {
//         /// <summary>
//         /// </summary>
//         /// <param name="absBasePath">The absolute path to resolve relative paths in the config against. This would usually be the location of the config file on disk if the config was loaded from file.</param>
//         public static Result<Dictionary<string, ICompiler>> CreateCompilers(string absBasePath, Config config/*, CancellationToken token*/)
//         {
//             var result = new Result<Dictionary<string, ICompiler>>();

//             // var bResult = ParseBaseDirectory(absBasePath);

//             // result.AddMessages(bResult.Messages);

//             // var baseDirectory = bResult.Value;

//             var compilers = new Dictionary<string, ICompiler>();

//             // caches to avoid parsing same config twice
//             var parsersCache = new Dictionary<string, IParser>();
//             var transformersCache = new Dictionary<string, ITransformer>();
//             var emittersCache = new Dictionary<string, IEmitter>();
//             var consumersCache = new Dictionary<string, IConsumer>();

//             // [dho] TODO optimize? parallelize? - 26/08/18
//             foreach (var kv in config.Compilation)
//             {
//                 var compilerID = kv.Key;

//                 if(kv.Value.Compiler is string)
//                 {
//                     var (messages, compiler) = InstantiateCompilerFromPath(absBasePath, (string)kv.Value.Compiler);

//                     result.AddMessages(messages);

//                     if(compiler != null)
//                     {
//                         compilers[compilerID] = compiler;
//                     }
//                 }
//                 else
//                 {
//                     InlineCompilerConfig c = null;

//                     try
//                     {
//                         c = ((Newtonsoft.Json.Linq.JObject)kv.Value.Compiler).ToObject<InlineCompilerConfig>();
//                     }
//                     catch(Exception e)
//                     {
//                         result.AddMessages(CreateErrorFromException(e));
//                         continue;
//                     }

//                     IParser parser = null;
//                     var transformers = new List<ITransformer>();
//                     IEmitter emitter = null;
//                     var consumers = new List<IConsumer>();

//                     var parserID = c.Parsing;

//                     if (parsersCache.ContainsKey(parserID))
//                     {
//                         parser = parsersCache[parserID];
//                     }
//                     else if (config.Parsing.ContainsKey(parserID))
//                     {
//                         // create and cache

//                         var pResult = CreateParser(absBasePath, config.Parsing[parserID]);

//                         result.AddMessages(pResult.Messages);

//                         parser = parsersCache[parserID] = pResult.Value;
//                     }
//                     else
//                     {
//                         result.AddMessages(
//                             new Message(MessageKind.Error, $"Compiler config for compiler '{compilerID}' contains unknown parser ID '{parserID}'")
//                         );
//                     }

//                     foreach (var transformerID in c.Transformation)
//                     {
//                         if (transformersCache.ContainsKey(transformerID))
//                         {
//                             transformers.Add(transformersCache[transformerID]);
//                         }
//                         else if (config.Transformation.ContainsKey(transformerID))
//                         {
//                             // create and cache

//                             var (tMessages, transformer) = CreateTransformer(absBasePath, config.Transformation[transformerID]);

//                             result.AddMessages(tMessages);

//                             transformers.Add(transformersCache[transformerID] = transformer);
//                         }
//                         else
//                         {
//                             result.AddMessages(
//                                 new Message(MessageKind.Error, $"Compiler config for compiler '{compilerID}' contains unknown transformer ID '{transformerID}'")
//                             );
//                         }
//                     }

//                     var emitterID = c.Emission;

//                     if(emitterID != null) // [dho] emission is optional - 21/03/19
//                     {
//                         if (emittersCache.ContainsKey(emitterID))
//                         {
//                             emitter = emittersCache[emitterID];
//                         }
//                         else if (config.Emission.ContainsKey(emitterID))
//                         {
//                             // create and cache

//                             var eResult = CreateEmitter(absBasePath, config.Emission[emitterID]);

//                             result.AddMessages(eResult.Messages);

//                             emitter = emittersCache[emitterID] = eResult.Value;
//                         }
//                         else
//                         {
//                             result.AddMessages(
//                                 new Message(MessageKind.Error, $"Compiler config for compiler '{compilerID}' contains unknown emitter ID '{emitterID}'")
//                             );
//                         }
//                     }

//                     if(c.Consumption != null) // [dho] consumption is optional - 21/03/19
//                     {
//                         foreach (var consumerID in c.Consumption)
//                         {
//                             if (consumersCache.ContainsKey(consumerID))
//                             {
//                                 consumers.Add(consumersCache[consumerID]);
//                             }
//                             else if (config.Consumption.ContainsKey(consumerID))
//                             {
//                                 // create and cache

//                                 var (cMessages, consumer) = CreateConsumer(absBasePath, config.Consumption[consumerID]);

//                                 result.AddMessages(cMessages);

//                                 consumers.Add(consumersCache[consumerID] = consumer);
//                             }
//                             else
//                             {
//                                 result.AddMessages(
//                                     new Message(MessageKind.Error, $"Compiler config for compiler '{compilerID}' contains unknown consumer ID '{consumerID}'")
//                                 );
//                             }
//                         }
//                     }


//                     var compiler = new Compiler(/*baseDirectory, */parser, transformers, emitter, consumers);

//                     compilers[compilerID] = compiler;
//                 }
//             }

//             result.Value = compilers;

//             return result;
//         }


//         public static Result<IParser> CreateParser(string absBasePath, ParsingConfig config)
//         {
//             if (config.Parser.StartsWith("$"))
//             {
//                 var result = new Result<IParser>();

//                 switch (config.Parser)
//                 {
//                     case "$poly":
//                         result.Value = new PolyParser();
//                         break;

//                     case "$typescript":
//                         result.Value = new RelaxedParser();//new TypeScriptParser();
//                         break;

//                     default:
//                         result.AddMessages(
//                             new Message(MessageKind.Error, $"Parsing config contains unsupported parser value '{config.Parser}'")
//                         );
//                         break;
//                 }

//                 return result;
//             }
//             else
//             {
//                 return InstantiateParserFromPath(absBasePath, config.Parser);
//             }
//         }

//         public static Result<ITransformer> CreateTransformer(string absBasePath, TransformationConfig config)
//         {
//             if (config.Transformer.StartsWith("$"))
//             {
//                 var result = new Result<ITransformer>();

//                 switch (config.Transformer)
//                 {
//                     // [dho] add cases here for recognized middlewares - 25/08/18

//                     // case "$intrinsics":
//                     //     result.Value = new IntrinsicTypeTransformer();
//                     // break;

//                     default:
//                         result.AddMessages(
//                             new Message(MessageKind.Error, $"Transformation config contains unsupported transformer value '{config.Transformer}'")
//                         );
//                         break;
//                 }

//                 return result;
//             }
//             else
//             {
//                 return InstantiateTransformerFromPath(absBasePath, config.Transformer);
//             }
//         }

//         public static Result<IEmitter> CreateEmitter(string absBasePath, EmissionConfig config)
//         {
//             if (config.Emitter.StartsWith("$"))
//             {
//                 var result = new Result<IEmitter>();

//                 switch (config.Emitter)
//                 {
//                     // [dho] add cases here for recognized middlewares - 25/08/18

//                     // case "$cs":
//                     //     result.Value = new CSEmitter();
//                     //     break;

//                     // case "$python":
//                     //     result.Value = new PythonEmitter();
//                     //     break;

//                     case "$swift":
//                         result.Value = new SwiftEmitter();
//                         break;

//                     default:
//                         result.AddMessages(
//                             new Message(MessageKind.Error, $"Emission config contains unsupported emitter value '{config.Emitter}'")
//                         );
//                         break;
//                 }

//                 return result;
//             }
//             else
//             {
//                 return InstantiateEmitterFromPath(absBasePath, config.Emitter);
//             }
//             /*
//             var result = new Result<IEmitter>();

//             if(String.IsNullOrEmpty(config.Path))
//             {
//                 result.AddError(new Error("Emission config must specify a valid output path")
//                 {
//                     Data = new { Config = config }
//                 });
//             }
//             else
//             {
//                 var outputDir = Sempiler.SourceHelpers.CreateDirectory(
//                     config.Path.Split(Path.DirectorySeparatorChar)
//                 );

//                 if(config.Emitter.StartsWith("$"))
//                 {
//                     switch(config.Emitter)
//                     {
//                         // [dho] add cases here for recognized middlewares - 25/08/18

//                         default:
//                             result.AddError(
//                                 new Error($"Emission config contains unsupported emitter value '{config.Emitter}'")
//                                 {
//                                     Data = new { Config = config }
//                                 }
//                             );
//                         break;
//                     }

//                     return result;
//                 }
//                 else
//                 {
//                     var (messages, emitter) = CreateDynamicallyLinkedEmitter(absBasePath, config.Emitter);

//                     result.AddMessages(messages);

//                     result.Value = emitter;
//                 }
//             }

//             return result;*/
//         }

//         public static Result<IConsumer> CreateConsumer(string absBasePath, ConsumptionConfig config)
//         {
//             var result = new Result<IConsumer>();

//             if (config.Consumer.StartsWith("$"))
//             {
//                 switch (config.Consumer)
//                 {
//                     // [dho] add cases here for recognized middlewares - 25/08/18

//                     case "$cmd":
//                         {
//                             var cmd = new Sempiler.Consumption.CommandLine();

//                             result.Value = cmd;

//                             foreach (var kv in config.Arguments)
//                             {
//                                 switch (kv.Key)
//                                 {
//                                     // [dho] TODO more properties - 28/08/18

//                                     case "args":
//                                         {
//                                             if(kv.Value is Newtonsoft.Json.Linq.JArray)
//                                             {
//                                                 try
//                                                 {
//                                                     cmd.Arguments = ((Newtonsoft.Json.Linq.JArray)kv.Value).ToObject<string[]>();
//                                                     continue;
//                                                 }
//                                                 catch(Exception)
//                                                 {
//                                                 }
//                                             }

//                                             // [dho] if we drop to here then the value was invalid - 28/08/18
//                                             result.AddMessages(
//                                                 new Message(MessageKind.Error, $"Consumption config '{config.Consumer}' contains unsupported '{kv.Key}' argument value '{kv.Value}'")
//                                             );
                                        
//                                         }
//                                         break;

//                                     case "name":
//                                         {
//                                             var name = kv.Value as string;

//                                             if (name != null && Regex.IsMatch(name, @"^[a-zA-Z]+$"))
//                                             {
//                                                 cmd.Name = name;
//                                             }
//                                             else
//                                             {
//                                                 result.AddMessages(
//                                                     new Message(MessageKind.Error, $"Consumption config '{config.Consumer}' contains unsupported '{kv.Key}' argument value '{kv.Value}'")
//                                                 );
//                                             }
//                                         }
//                                         break;

//                                     case "fileName":
//                                         {
//                                             var fileName = kv.Value as string;

//                                             if (fileName != null)
//                                             {
//                                                 cmd.FileName = fileName;
//                                             }
//                                             else
//                                             {
//                                                 result.AddMessages(
//                                                     new Message(MessageKind.Error, $"Consumption config '{config.Consumer}' contains unsupported '{kv.Key}' argument value '{kv.Value}'")
//                                                 );
//                                             }
//                                         }
//                                         break;

//                                     default:
//                                         {
//                                             result.AddMessages(
//                                                 new Message(MessageKind.Error, $"Consumption config '{config.Consumer}' contains unsupported '{kv.Key}' property")
//                                             );
//                                         }
//                                         break;
//                                 }
//                             }

//                         }
//                         break;

//                     case "$gcc":
//                     {
//                         var (messages, cmd) = CreateCommandLine("GCC", "gcc", config);
                            
//                         result.AddMessages(messages);
//                         result.Value = cmd;
//                     }
//                     break;

//                     case "$javac":
//                     {
//                         var (messages, cmd) = CreateCommandLine("Java", "javac", config);
                            
//                         result.AddMessages(messages);
//                         result.Value = cmd;
//                     }
//                     break;

//                     case "$mcs":
//                     {
//                         var (messages, cmd) = CreateCommandLine("Mono Compiler Suite", "mcs", config);
                        
//                         result.AddMessages(messages);

//                         if(cmd != null)
//                         {
//                             cmd.DiagnosticsParser = CommandLineDiagnosticsParser.MCS;
//                         }

//                         result.Value = cmd;
//                     }
//                     break;

//                     case "$mono":
//                     {
//                         var (messages, cmd) = CreateCommandLine("Mono", "mono", config);
                            
//                         result.AddMessages(messages);
//                         result.Value = cmd;
//                     }
//                     break;

//                     case "$swiftc":
//                     {
//                         var (messages, cmd) = CreateCommandLine("Swift", "swiftc", config);
                        
//                         result.AddMessages(messages);
//                         result.Value = cmd;
//                     }
//                     break;
                   
//                     default:
//                     {
//                         result.AddMessages(
//                             new Message(MessageKind.Error, $"Consumption config contains unsupported consumer value '{config.Consumer}'")
//                         );
//                     }
//                     break;
//                 }

//             }
//             else
//             {
//                 var (messages, consumer) = InstantiateConsumerFromPath(absBasePath, config.Consumer, config.Arguments);

//                 result.AddMessages(messages);
//                 result.Value = consumer;
//             }

//             return result;
//         }

//         // static Result<IDirectoryLocation> ParseBaseDirectory(string path)
//         // {
//         //     var result = new Result<IDirectoryLocation>();

//         //     if (String.IsNullOrEmpty(path))
//         //     {
//         //         result.AddError(new Sempiler.Diagnostics.Error("Missing base path argument")
//         //         {
//         //             Code = (int)ErrorCode.IllegalArgument
//         //         });
//         //     }
//         //     else
//         //     {
//         //         var absBasePath = FileSystem.Resolve(Directory.GetCurrentDirectory(), path);

//         //         var (attrsMessages, attrs) = FileSystem.GetAttributes(absBasePath);

//         //         result.AddMessages(attrsMessages);

//         //         if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
//         //         {
//         //             var dir = FileSystem.ParseDirectoryLocation(absBasePath);

//         //             result.Value = dir;
//         //         }
//         //         else
//         //         {
//         //             result.AddError(new Sempiler.Diagnostics.Error("Base directory must be a valid directory path"));
//         //         }
//         //     }

//         //     return result;
//         // }

//         private static Result<Sempiler.Consumption.CommandLine> CreateCommandLine(string name, string fileName, ConsumptionConfig config)
//         {
//             var result = new Result<Sempiler.Consumption.CommandLine> ();

//             var cmd = new Sempiler.Consumption.CommandLine
//             {
//                 Name = name,
//                 FileName = fileName,
//                 ParseDiagnostics = true,
//                 InjectVariables = true
//             };

//             foreach (var kv in config.Arguments)
//             {
//                 switch (kv.Key)
//                 {
//                     case "args":{
//                         if(kv.Value is Newtonsoft.Json.Linq.JArray)
//                         {
//                             try
//                             {
//                                 cmd.Arguments = ((Newtonsoft.Json.Linq.JArray)kv.Value).ToObject<string[]>();
//                                 continue;
//                             }
//                             catch(Exception)
//                             {
//                             }
//                         }

//                         // [dho] if we drop to here then the value was invalid - 28/08/18
//                         result.AddMessages(
//                             new Message(MessageKind.Error, $"Consumption config '{config.Consumer}' contains unsupported '{kv.Key}' argument value '{kv.Value}'")
//                         );
                    
//                     }
//                     break;

//                     default:
//                     {
//                         result.AddMessages(
//                             new Message(MessageKind.Error, $"Consumption config '{config.Consumer}' contains unsupported '{kv.Key}' property")
//                         );
//                     }
//                     break;
//                 }
//             }

//             if(!HasErrors(result))
//             {
//                 result.Value = cmd;
//             }

//             return result;
//         }

//         // public static Result<Dictionary<string, IResolvedSourceCollection>> CreateSources(string absBasePath, Config config/*, CancellationToken token*/)
//         // {
//         //     var result = new Result<Dictionary<string, IResolvedSourceCollection>>();

//         //     // keyed by compiler ID
//         //     var sources = new Dictionary<string, IResolvedSourceCollection>();

//         //     // keyed by source ID
//         //     // var sourceCache = new Dictionary<string, IResolvedSourceCollection>();

//         //     foreach (var kv in config.Compilation)
//         //     {
//         //         var compilerID = kv.Key;
//         //         // var sourceID = kv.Value.Source;

//         //         // if (sourceID != null)
//         //         // {
//         //         //     if (sourceCache.ContainsKey(sourceID))
//         //         //     {
//         //         //         sources[compilerID] = sourceCache[sourceID];
//         //         //     }
//         //         //     else if (config.Source != null && config.Source.ContainsKey(sourceID))
//         //         //     {
//         //         //         var rResult = ResolvedSourceCollection.Create(absBasePath, compilerID, config);

//         //         //         result.AddMessages(rResult.Messages);

//         //         //         sources[compilerID] = sourceCache[sourceID] = rResult.Value;
//         //         //     }
//         //         //     else
//         //         //     {
//         //         //         result.AddMessages(
//         //         //             new Message(MessageKind.Error, $"Source config for compiler '{compilerID}' contains unknown source ID '{sourceID}'")
//         //         //         );
//         //         //     }
//         //         // }
//         //          var rResult = ResolvedSourceCollection.Create(absBasePath, compilerID, config);

//         //         result.AddMessages(rResult.Messages);

//         //         sources[compilerID] = rResult.Value;
//         //     }

//         //     result.Value = sources;

//         //     return result;
//         // }

//         public static Result<IEnumerable<ISource>> EnumerateSources(string baseDirPath, string compilerID, Config config)
//         {
//             var result = new Result<IEnumerable<ISource>>();

//             if(!config.Compilation.ContainsKey(compilerID))
//             {
//                 result.AddMessages(
//                     new Message(MessageKind.Error, $"Compiler ID '{compilerID}' not found in config")
//                 );

//                 return result;
//             }

//             var compilation = config.Compilation[compilerID];

//             // compilation.Config is either an InlineCompilerConfig or its a path to a compiler dll

//             var patterns = new List<SourceHelpers.SourceFilePatternMatchInput>();

//             // [dho] if this is a file path.. what do we do?
//             // if its to a `cs` file do we just wrap and inject it as the compiler
//             var compiler = compilation.Compiler as InlineCompilerConfig;

//             // if(compiler != null)
//             // {   
//             //     // ADD THE FILES.... but what if its a `cs` file? I guess in your parser you would just
//             //     // create a Node that is like LiteralCodeDump and just put it all in there as a passthrough
//             //     // TODO add LiteralCodeDump Node for pass through - 05/04/19

//             //     // compiler.Transformation

//             //     // compiler.Emitter

//             //     // compiler.Consumer

//             //     // compiler.Parser
//             //     x
//             // }

//             if(compilation.Source != null)
//             {
//                 var sourceID = compilation.Source;

//                 if(config.Source == null || !config.Source.ContainsKey(sourceID))
//                 {
//                     result.AddMessages(
//                         new Message(MessageKind.Error, $"Source ID '{sourceID}' not found in config for compiler ID '{compilerID}'")
//                     );
//                 }
//                 else // [dho] add the sources that will most likely just contain run time code (eg. app code) - 05/04/19
//                 {
//                     var sources = config.Source[compilation.Source];   

//                     var dirCount = sources.Directories != null ? sources.Directories.Length : 0;
//                     var fileCount = sources.Files != null ? sources.Files.Length : 0;

//                     for (var dirIndex = 0; dirIndex < dirCount; ++dirIndex)
//                     {
//                         var dirPath = sources.Directories[dirIndex].Path;

//                         if(!dirPath.EndsWith('/'))
//                         {
//                             dirPath += '/';
//                         }
//                         patterns.Add(
//                             // [dho] NOTE the dir path should be absolute - 05/04/19
//                             new SourceHelpers.SourceFilePatternMatchInput(baseDirPath, dirPath)
//                         );
//                     }

//                     for (var fileIndex = 0; fileIndex < fileCount; ++fileIndex)
//                     {
//                         var filePath = sources.Files[fileIndex].Path;

//                         patterns.Add(
//                             new SourceHelpers.SourceFilePatternMatchInput(baseDirPath, filePath)
//                         );
//                     }
//                 }
//             }

//             result.Value = result.AddMessages(SourceHelpers.EnumerateSourceFilePatternMatches(patterns));

//             return result;
//         }
//         // private static IEnumerable<Result<ISource>> CreateSourcesEnumerator(string baseDirPath, IEnumerable<SourceLiteralConfig> literals, IEnumerable<Result<ISourceFile>> files, SourceIntent intent)
//         // {
//         //     var literalPaths = new Dictionary<string, object>();

//         //     if (literals != null)
//         //     {
//         //         foreach (var l in literals)
//         //         {
//         //             var intermediate = new Result<ISource>();

//         //             IFileLocation location = null;

//         //             if (!String.IsNullOrEmpty(l.Path))
//         //             {
//         //                 var fullFilePath = FileSystem.Resolve(baseDirPath, l.Path);

//         //                 location = SourceHelpers.ParseSourceFileLocation(baseDirPath, fullFilePath);

//         //                 if (location != null)
//         //                 {
//         //                     // [dho] just mark that we've seen this literal so we do not inadvertently
//         //                     // squash it with a file from disk later in the enumerator - 24/08/18
//         //                     literalPaths[location.ToPathString()] = default(object);
//         //                 }
//         //                 else
//         //                 {
//         //                     intermediate.AddMessages(
//         //                         new Message(MessageKind.Warning, $"Skipping file '{fullFilePath}' because it is not a descendant of base path '{baseDirPath}'")
//         //                     );
//         //                 }
//         //             }

//         //             intermediate.Value = SourceHelpers.CreateLiteral(intent, l.Text, location);

//         //             yield return intermediate;
//         //         }
//         //     }

//         //     if (files != null)
//         //     {
//         //         foreach (var intermediate in files)
//         //         {
//         //             if (intermediate.Value != null)
//         //             {
//         //                 var path = intermediate.Value.GetPathString();

//         //                 // [dho] guard against squashing literals 
//         //                 // (they take precendence over source files) - 24/08/18
//         //                 if (literalPaths.ContainsKey(path))
//         //                 {
//         //                     intermediate.AddMessages(
//         //                         new Message(MessageKind.Info, $"Skipping source file '{path}' because a source literal was provided for the same location")
//         //                     );

//         //                     continue;
//         //                 }
//         //             }

//         //             yield return new Result<ISource>
//         //             {
//         //                 Value = intermediate.Value,
//         //                 Messages = intermediate.Messages
//         //             };
//         //         }
//         //     }
//         // }

//         public static Result<Dictionary<string, IDirectoryLocation>> CreateDestinations(string absBasePath, Config config/*, CancellationToken token*/)
//         {
//             var result = new Result<Dictionary<string, IDirectoryLocation>>();

//             // keyed by compiler id
//             var destinations = new Dictionary<string, IDirectoryLocation>();

//             // keyed by destination id
//             var destinationCache = new Dictionary<string, IDirectoryLocation>();

//             foreach (var kv in config.Compilation)
//             {
//                 var compilerID = kv.Key;
//                 var destinationID = kv.Value.Destination;

//                 if(destinationID != null) // [dho] optional destination - 21/03/19
//                 {
//                     if (destinationCache.ContainsKey(destinationID))
//                     {
//                         destinations[compilerID] = destinationCache[destinationID];
//                     }
//                     else if (config.Destination.ContainsKey(destinationID))
//                     {
//                         var absDestinationPath = FileSystem.Resolve(absBasePath, config.Destination[destinationID].Path);

//                         destinations[compilerID] = destinationCache[destinationID] = FileSystem.ParseDirectoryLocation(absDestinationPath);
//                     }
//                     else
//                     {
//                         result.AddMessages(
//                             new Message(MessageKind.Error, $"Destination config for compiler '{compilerID}' contains unknown destination ID '{destinationID}'")
//                         );
//                     }
//                 }

//             }

//             result.Value = destinations;

//             return result;
//         }
//     }
}
