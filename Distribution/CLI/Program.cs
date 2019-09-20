using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sempiler;
using Sempiler.Diagnostics;
using Sempiler.Emission;
using Sempiler.Transformation;
using Sempiler.Parsing;
using Newtonsoft.Json;

namespace Sempiler.Distribution.CLI
{
    using static Protocol.ProtocolHelpers;

    // public class CLISession // [dho] support for this
    // {
    //     // public SourceConfig OverrideSources;

    //     // public IDirectoryLocation BaseDirectory;

    //     // public string AbsMainPath;

    //     // public CancellationTokenSource CurrentCompilation;
    // }

    public class OverrideSourceProvider : DefaultSourceProvider
    {
        public SourceConfig OverrideSources;

        public override ISource ProvideFromPath(string absPath)
        {
            var result = new Result<ISource>();

            // [dho] see if we have an override source literal for the same path first,
            // and use that if we do. This allows us to support 'dirty' files that are being
            // edited in an IDE - 05/05/19
            if(OverrideSources?.Literals?.Length > 0)
            {
                foreach(var literal in OverrideSources.Literals)
                {
                    if(!string.IsNullOrEmpty(literal.Path))
                    {
                        var absLiteralPath = FileSystem.Resolve(OverrideSources.Path, literal.Path);

                        if(absPath == absLiteralPath)
                        {
                            var fileLocation = FileSystem.ParseFileLocation(absLiteralPath);

                            return SourceHelpers.CreateLiteral(literal.Text, fileLocation);
                        }
                    }
                }
            }

            return base.ProvideFromPath(absPath);
        }
    }

    public static class CLIHelpers
    {
        static void Main(string[] args)
        {
            var result = new Result<Protocol.IProtocol_Serializable>();

            try
            {
                var task = Compile();

                task.Wait();

                result.AddMessages(task.Result);
            }
            catch(Exception e)
            {
                result.AddMessages(CreateErrorFromException(e));
            }

            Print(new Protocol.Response
            {
                // [dho] indicates that the packet was processed
                // OK, not that the Data contains no Error Messages - 07/09/18
                OK = true,
                Data = Convert(result)
            });

            if(DiagnosticsHelpers.HasErrors(result))
            {
                Environment.Exit(1);
            }

        }

        
        async static Task<Result<object>> Compile()
        {
            var result = new Result<object>();

            var server = new Sempiler.Core.DuplexSocketServer();

            var port = 8183;

            result.AddMessages(server.BindPort(port));

            // [dho] could not bind server to port - 05/05/19
            if (DiagnosticsHelpers.HasErrors(result)) return result;

            var cts = new CancellationTokenSource();

            var serverTask = server.StartAcceptingRequests(cts.Token);

            try
            {
                var userInputTask = Task.Run(() => ReadUserInput(cts), cts.Token);

                // [dho] blocks this thread - 07/05/19
                ProcessCommandLineRequests(server, cts.Token);
            }
            catch (Exception e)
            {
                cts.Cancel();

                result.AddMessages(CreateErrorFromException(e));

                // Print(new Protocol.Response
                // {
                //     // [dho] indicates that the packet was processed
                //     // OK, not that the Data contains no Error Messages - 07/09/18
                //     OK = true,
                //     Data = Convert(result)
                // });

                // Environment.Exit(1);
            }

            server.Stop();

            result.AddMessages(await serverTask);

            return result;
        }

        public static Result<IEnumerable<ISource>> ParseSourcesFromArgs(string[] args)
        {
            var result = new Result<IEnumerable<ISource>>();

            IEnumerable<ISource> sources = default(IEnumerable<ISource>);

            if (args.Length > 0)
            {
                var inputPatterns = new List<SourceHelpers.SourceFilePatternMatchInput>();

                var baseDirPath = Directory.GetCurrentDirectory();

                for (int i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];

                    if(!arg.StartsWith("--")) // [dho] args that do not start with "--" are treated as input file patterns - 30/04/19
                    {
                        var searchPattern = arg;

                        inputPatterns.Add(new SourceHelpers.SourceFilePatternMatchInput(baseDirPath, searchPattern));
                    }
                }

                if(inputPatterns.Count > 0)
                {
                    sources = result.AddMessages(SourceHelpers.EnumerateSourceFilePatternMatches(inputPatterns));
                }
            }

            result.Value = sources;

            return result;
        }



        static ConcurrentQueue<Protocol.Request> RequestBuffer = new ConcurrentQueue<Protocol.Request>();

        static Thread UserInputThread;

    
        private static void ReadUserInput(CancellationTokenSource cts)
        {
            Console.WriteLine("Awaiting input commands from prompt...");

            string cmd;

            while ((cmd = Console.ReadLine()) != null)
            {
                cmd = cmd.Trim();

                if (cmd.Length == 0)
                {
                    continue;
                }

                if (cmd == "exit")
                {
                    break;
                }

                Protocol.Packet packet = null;

                try
                {
                    packet = JsonConvert.DeserializeObject<Protocol.Packet>(cmd);
                }
                catch (Exception)
                {
                    var r = new Result<Protocol.IProtocol_Serializable>();
                    
                    r.AddMessages(new Message(
                        MessageKind.Error,
                        "Could not parse packet from input"
                    ));
                    
                    Print(new Protocol.Response
                    {
                        OK = true,
                        Data = Convert(r)
                    });

                    continue;
                }

                switch (packet.Type)
                {
                    case "request":
                        {
                            Protocol.Request req = null;

                            try
                            {
                                req = JsonConvert.DeserializeObject<Protocol.Request>(cmd);
                            }
                            catch (Exception)
                            {
                                var r = new Result<Protocol.IProtocol_Serializable>();
                    
                                r.AddMessages(new Message(
                                    MessageKind.Error,
                                    "Could not parse request packet from input"
                                ));
                                
                                Print(new Protocol.Response
                                {
                                    OK = true,
                                    Data = Convert(r)
                                });

                                break;
                            }

                            RequestBuffer.Enqueue(req);
                        }
                        break;

                    default:
                        {
                            var r = new Result<Protocol.IProtocol_Serializable>();
                    
                            r.AddMessages(new Message(
                                MessageKind.Error,
                                $"Unsupported packet type : '{packet.Type}"
                            ));
                            
                            Print(new Protocol.Response
                            {
                                OK = true,
                                Data = Convert(r)
                            });
                        }
                        break;
                }
            }

            cts.Cancel();
        }


        private static void ProcessCommandLineRequests(Sempiler.Core.DuplexSocketServer server, CancellationToken token)
        {
            ISourceProvider sourceProvider = new DefaultSourceProvider();

            var inputPaths = new string[] {};

            var baseDirectory = FileSystem.ParseDirectoryLocation(Directory.GetCurrentDirectory());

            // string absMainPath = default(string);

            CancellationTokenSource currentCompilation = default(CancellationTokenSource);


            while (!token.IsCancellationRequested)
            {
                Protocol.Request req;

                if (RequestBuffer.TryDequeue(out req))
                {
                    var result = new Result<Protocol.IProtocol_Serializable>();

                    try
                    {
                        switch (req.Command)
                        {
                            case "set_main":{
                                RelativeMainConfig relMainConfig = null;

                                try
                                {
                                    relMainConfig = ((Newtonsoft.Json.Linq.JObject)req.Data).ToObject<RelativeMainConfig>();
                                    inputPaths = new [] { relMainConfig.Path };
                                    baseDirectory = FileSystem.ParseDirectoryLocation(System.IO.Directory.GetParent(relMainConfig.Path).ToString());
                                }
                                catch
                                {
                                    result.AddMessages(
                                        new Message(MessageKind.Error, $"Could not parse data for '{req.Command}'")
                                    );
                                    break;
                                }

                                
                            }
                            break;

                            case "compile":{
                                if(token.IsCancellationRequested) break;

                                if(currentCompilation != null && !currentCompilation.Token.IsCancellationRequested) 
                                {
                                    currentCompilation.Cancel();
                                    currentCompilation.Dispose();
                                }

                                currentCompilation = new CancellationTokenSource();

                                using(CancellationTokenRegistration ctr = token.Register(() => currentCompilation.Cancel()))
                                {
                                    var session = result.AddMessages(
                                        Sempiler.Core.Main.Compile(baseDirectory, inputPaths, sourceProvider, server, currentCompilation.Token)
                                    );

                                    if(!Diagnostics.DiagnosticsHelpers.HasErrors(result))
                                    {
                                        result.Value = Protocol.ProtocolHelpers.Convert(session);
                                    }
                                }
                            }
                            break;


                            case "set_override_sources":{
                                try
                                {
                                    sourceProvider = new OverrideSourceProvider
                                    {
                                        OverrideSources = ((Newtonsoft.Json.Linq.JObject)req.Data).ToObject<SourceConfig>()
                                    };
                                }
                                catch
                                {
                                    result.AddMessages(
                                        new Message(MessageKind.Error, $"Could not parse data for '{req.Command}'")
                                    );
                                    break;
                                }
                            }
                            break;

                            default:
                                {
                                    result.AddMessages(
                                        new Message(MessageKind.Error, $"Unsupported request command : '{req.Command}")
                                    );
                                }
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        result.AddMessages(CreateErrorFromException(e));
                    }

                    


                    Print(new Protocol.Response
                    {
                        RequestID = req.ID,
                        // [dho] indicates that the packet was processed
                        // OK, not that the Data contains no Error Messages - 07/09/18
                        OK = true,//!IsTerminal(result),
                        Data = Convert(result)
                    });
                }
            }
        }

        static Result<object> CreateErrorFromException(Exception e)
        {
            var result = new Result<object>();

            result.AddMessages(DiagnosticsHelpers.CreateErrorFromException(e));
            result.AddMessages(new Message(MessageKind.Error, e.StackTrace));

            return result;
        }


        
        private static void PrintMessages(MessageCollection messages)
        {
            if (messages != null)
            {
                if (messages.Infos?.Count > 0)
                {
                    foreach (var info in messages.Infos)
                    {
                        Console.Out.WriteLine($"INFO : {info.Description}");
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }

                if (messages.Warnings?.Count > 0)
                {
                    Console.WriteLine($"{messages.Warnings.Count} warnings encountered:");

                    foreach (var warning in messages.Warnings)
                    {
                        Console.Out.WriteLine($"WARN : {warning.Description}");
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }

                if (messages.Errors?.Count > 0)
                {
                    Console.WriteLine($"{messages.Errors.Count} errors encountered:");

                    foreach (var error in messages.Errors)
                    {
                        Console.Error.WriteLine($"ERROR : {error.Description}");
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }

        


        /// <summary>
        /// The result `Value` will contain the absolute path and the deserialized object
        /// </summary>
        static Result<(string, T)> ParseJSONFromPath<T>(string configPath)
        {
            var result = new Result<(string, T)>();

            if (String.IsNullOrEmpty(configPath))
            {
                result.AddMessages(
                    new Message(MessageKind.Error, "Missing path")
                );
            }
            else
            {
                var absPath = FileSystem.Resolve(
                    Directory.GetCurrentDirectory(),
                    configPath
                );

                try
                {
                    var contents = File.ReadAllText(absPath);

                    result.Value = (absPath, JsonConvert.DeserializeObject<T>(contents));
                }
                catch (Exception e)
                {
                    result.AddMessages(CreateErrorFromException(e));
                }
            }

            return result;
        }
    }

}
