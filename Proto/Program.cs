using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sempiler.AST;
using Sempiler.Diagnostics;
using System.Collections.Generic;

namespace Proto
{
    class Program
    {
        const string PortSwitch = "--p";
        const int DefaultPort = 8189;

        const string InfoIcon = "📘";
        const string SuccessIcon = "📗";
        const string ErrorIcon = "📕";
        const string WarningIcon = "📙";

        static int Main(string[] args)
        {
            var timer = Sempiler.CompilerHelpers.StartTimer("SESSION");

            var result = new Result<object>();

            try
            {
                var inputPaths = new List<string>();
                var port = DefaultPort;

                for(int i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];

                    if(arg == PortSwitch)
                    {
                        try
                        {
                            port = System.Int32.Parse(args[++i]);
                        }
                        catch(Exception e)
                        {
                            throw new System.Exception($"Expected integer to follow port switch '{PortSwitch}'");
                        }
                    }
                    else
                    {
                        inputPaths.Add(arg);
                    }
                }


                var task = Compile(port, inputPaths.ToArray());

                task.Wait();

                result.AddMessages(task.Result);
            }
            catch(Exception e)
            {
                result.AddMessages(DiagnosticsHelpers.CreateErrorFromException(e));
            }

            Sempiler.CompilerHelpers.StopTimer(timer);

            if(result.Messages != null)
            {
                var infos = result.Messages.Infos;

                if(infos != null)
                {
                    foreach(var m in infos)
                    {
                        PrintMessage(InfoIcon, m);
                    }
                }

                var warnings = result.Messages.Warnings;

                if(warnings != null)
                {
                    foreach(var m in warnings)
                    {
                        PrintMessage(WarningIcon, m);
                    }
                }

                var errors = result.Messages.Errors;

                if(errors != null)
                {
                    foreach(var m in errors)
                    {
                        PrintMessage(ErrorIcon, m);
                    }
                }
            }

            var infoCount = 0;
            var warningCount = 0;
            var errorCount = 0;
            
            if(result.Messages?.Infos != null)
            {
                infoCount = result.Messages.Infos.Count;
            }

            if(result.Messages?.Warnings != null)
            {
                warningCount = result.Messages.Warnings.Count;
            }

            var exitCode = 0;

            if(Sempiler.Diagnostics.DiagnosticsHelpers.HasErrors(result))
            {
                errorCount = result.Messages.Errors.Count;
                exitCode = 1;
            }

            var statusIcon = errorCount == 0 ? SuccessIcon : ErrorIcon;
            var statusMessage = errorCount == 0 ? "SUCCESS!" : "FAILED!";

            Console.WriteLine($"\n{InfoIcon} {infoCount} INFO(S)");
            Console.WriteLine($"{WarningIcon} {warningCount} WARNING(S)");
            Console.WriteLine($"{ErrorIcon} {errorCount} ERROR(S)\n");

            Console.WriteLine($"{statusIcon} {statusMessage}");

            Sempiler.CompilerHelpers.PrintElapsed(timer);

            return exitCode;
        }


        private static void PrintMessage(string icon, Message m)
        {
            FileMarker hint = null;
            SourceNodeOrigin origin = null;

            string kind = icon + " " + m.Kind.ToString().ToUpper();
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

        async static Task<Result<object>> Compile(int port, string[] inputPaths)
        {
            var result = new Result<object>();

            var server = new Sempiler.Core.DuplexSocketServer();

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
