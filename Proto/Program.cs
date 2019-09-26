using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sempiler.AST;
using Sempiler.Diagnostics;

namespace Proto
{
    class Program
    {
        static int Main(string[] args)
        {
            var result = new Result<object>();

            try
            {
                var task = Compile(args);

                task.Wait();

                result.AddMessages(task.Result);
            }
            catch(Exception e)
            {
                result.AddMessages(DiagnosticsHelpers.CreateErrorFromException(e));
            }

            if(result.Messages != null)
            {
                var infos = result.Messages.Infos;

                if(infos != null)
                {
                    foreach(var m in infos)
                    {
                        PrintMessage(m);
                    }
                }

                var warnings = result.Messages.Warnings;

                if(warnings != null)
                {
                    foreach(var m in warnings)
                    {
                        PrintMessage(m);
                    }
                }

                var errors = result.Messages.Errors;

                if(errors != null)
                {
                    foreach(var m in errors)
                    {
                        PrintMessage(m);
                    }
                }
            }

            var errorCount = 0;

            var exitCode = 0;

            if(Sempiler.Diagnostics.DiagnosticsHelpers.HasErrors(result))
            {
                errorCount = result.Messages.Errors.Count;
                exitCode = 1;
            }

            Console.WriteLine($"\n***** FINISHED WITH {errorCount} ERROR(S) *****");
            
            return exitCode;
        }


        private static void PrintMessage(Message m)
        {
            FileMarker hint = null;
            SourceNodeOrigin origin = null;

            MessageKind kind = m.Kind;
            string tags = "",
                    filePath = "<unknown>", 
                    lineNumber = "-1", 
                    columnIndex = "-1", 
                    description = m.Description ?? "<empty>";


            if(m.Tags != null)
            {
                tags = $"[{string.Join('|', m.Tags)}]";
            }

            if(m is Sempiler.Diagnostics.Message)
            {
                var message = ((Sempiler.Diagnostics.Message)m);

                hint = message.Hint;
            }

            if(m is Sempiler.AST.Diagnostics.NodeMessage)
            {
                var nodeMessage = ((Sempiler.AST.Diagnostics.NodeMessage)m);

                var node = nodeMessage.Node;

                origin = node.Origin as SourceNodeOrigin;
            }

            if(hint != null)
            {
                filePath = hint.File.ToPathString();
                lineNumber = hint.LineNumber.Start + "";
                columnIndex = hint.ColumnIndex.Start + "";
            }
            else if(origin != null)
            {
                var sourceFile = origin.Source as Sempiler.ISourceFile;

                if(sourceFile != null)
                {
                    filePath = sourceFile.Location.ToPathString();
                }
            }

            Console.WriteLine(
                String.Format(@"{0}:{1}:{2}: {3}: {4}", filePath, lineNumber, columnIndex, kind, $"{tags} {description}")
            );
        }

        async static Task<Result<object>> Compile(string[] inputPaths)
        {
            var result = new Result<object>();

            var server = new Sempiler.Core.DuplexSocketServer();

            var port = 8189;

            result.AddMessages(server.BindPort(port));

            // [dho] could not bind server to port - 05/05/19
            if (DiagnosticsHelpers.HasErrors(result)) return result;

            var cts = new CancellationTokenSource();

            var serverTask = server.StartAcceptingRequests(cts.Token);

            var baseDirectory = Sempiler.FileSystem.ParseDirectoryLocation(
                inputPaths.Length > 0 ? Directory.GetParent(inputPaths[0]).ToString() : Directory.GetCurrentDirectory()
            );

            var sourceProvider = new Sempiler.DefaultSourceProvider();

            try
            {
                result.Value = result.AddMessages(
                    Sempiler.Core.Main.Compile(baseDirectory, inputPaths, sourceProvider, server, cts.Token)
                );
            }
            catch (Exception e)
            {
                cts.Cancel();

                result.AddMessages(DiagnosticsHelpers.CreateErrorFromException(e));

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
    }
}
