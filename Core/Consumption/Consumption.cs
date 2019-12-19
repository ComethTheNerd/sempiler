using Sempiler.AST;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Emission;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sempiler.Consumption
{
    public interface IConsumer
    {
        Task<Result<object>> Consume(Session session, Artifact artifact, RawAST ast, Dictionary<string, OutFile> filesWritten, CancellationToken token);
    }

    public interface ICommandLineConsumer : IConsumer
    {
        string Name { get; set; }

        string FileName { get; set; }
        IEnumerable<string> Arguments { get; set; }

        bool InjectVariables { get; set; }

        bool ParseDiagnostics { get; set; }
        CommandLineDiagnosticsParser DiagnosticsParser { get; set; }
    }

    public class CommandLineConsumer : ICommandLineConsumer
    {

        public CommandLineConsumer()
        {
            InjectVariables = true;
            ParseDiagnostics = true;
        }

        public string Name { get; set; }

        public string FileName { get; set; }
        public IEnumerable<string> Arguments { get; set; }

        public bool InjectVariables { get; set; }

        public bool ParseDiagnostics { get; set; }

        public CommandLineDiagnosticsParser DiagnosticsParser { get; set; }


        public async Task<Result<object>> Consume(Session session, Artifact artifact, RawAST ast, Dictionary<string, OutFile> filesWritten, CancellationToken token)
        {
            var result = new Result<object>();

            var phase = PhaseKind.Consumption.ToString("g").ToLower();

            string[] tags = Name != null ? new string[]{ Name.ToLower(), phase } : new string[] { phase };

            if(String.IsNullOrEmpty(FileName))
            {
                result.AddMessages(
                    new Message(MessageKind.Error, "File name must be set to use command line")
                    {
                        Tags = tags
                    }
                );

                return result;
            }

            var (argumentsMessages, arguments) = ArgumentsToString(session, artifact, filesWritten, tags);

            result.AddMessages(argumentsMessages);

            if(HasErrors(result) || token.IsCancellationRequested)
            {
                return result;
            }

            int exitCode;
            IEnumerable<string> outLines;
            MessageCollection execMessages = null;

            try
            {
                var execResult = await Sempiler.CommandLine.Exec(FileName, arguments, token);
          
                // [dho] NOTE we are not just adding the execMessages straight to the result
                // because we may want to parse diagnostics out of them first (see below) - 28/08/18
                execMessages = execResult.Messages;

                exitCode = execResult.Value.Item1;

                if(exitCode != 0)
                {
                    result.AddMessages(
                        new Message(MessageKind.Error, $"{FileName} {arguments} exited with status code {exitCode}")
                        {
                            Tags = tags
                        }
                    );
                }

                outLines = execResult.Value.Item2;
            }
            catch(Exception e)
            {
                result.AddMessages(CreateErrorFromException(e));

                return result;
            }

            if(execMessages != null)
            {
                if(ParseDiagnostics)
                {
                    var parser = DiagnosticsParser ?? CommandLineDiagnosticsParser.GCC;


                    // [dho] TODO parse output and errors
                
                    if (execMessages?.Errors?.Count > 0)
                    {
                        foreach (var error in execMessages.Errors)
                        {
                            var diagnosticsParsed = 0;

                            var lines = (error as CommandLineMessage)?.Data?.Lines;

                            if(lines != null)
                            {
                                foreach (var line in lines)
                                {
                                    var diagnostics = parser.Parse(line);

                                    diagnosticsParsed += diagnostics.Count;

                                    foreach (var message in diagnostics)
                                    {
                                        var description = $"{FileName} : {message.Description}";

                                        var hint = message.Hint;

                                        if(hint != null && hint.File != null)
                                        {
                                            var filePath = hint.File.ToPathString();
                                            var lineNumberStart = hint.LineNumber.Start;
                                            var columnIndexStart = hint.ColumnIndex.Start;

                                            var closestNode = CompilerHelpers.GetClosestNode(filePath, lineNumberStart, columnIndexStart, filesWritten);

                                            if(closestNode != null)
                                            {
                                                result.AddMessages(
                                                    new Message(message.Kind, description)
                                                    {
                                                        // [dho] this hint will let us display the cause of the problem in the original
                                                        // source file before it was sempiled, eg. a statement in the TypeScript file that was
                                                        // then sempiled to Swift and triggered an error from the Swift compiler consuming it - 09/09/18
                                                        Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(closestNode.Origin),
                                                        Tags = tags
                                                    }
                                                );

                                                continue;
                                            }                
                                        }

                                        // [dho] fall through to here if we were unable to 
                                        // parse the node for any reason (see above) - 09/08/18
                                        result.AddMessages(
                                            new Message(message.Kind, description)
                                            {
                                                Tags = tags
                                            }
                                        );
                                    }
                                }
                            }

                            // [dho] we did not manage to parse any specific error information
                            // so just preserve the original error - 16/08/18
                            if(diagnosticsParsed == 0)
                            {
                                result.AddMessages(
                                    new Message(MessageKind.Warning, "Unable to parse specific diagnostic information - this could be due to an inaccurate diagnostics pattern")
                                    {
                                        Tags = tags
                                    },
                                    new Message(MessageKind.Error, lines != null ? String.Join("\n", lines) : error.Description)
                                    {
                                        Tags = tags
                                    }
                                );
                                
                            }
                        }
                    }
                }
                else
                {
                    // [dho] just add the exec messages as they are, no parsing first - 28/08/18
                    result.AddMessages(execMessages);
                }
            }

            return result;
        }

        private Result<string> ArgumentsToString(Session session, Artifact artifact, Dictionary<string, OutFile> filesWritten, string[] tags)
        {
            var result = new Result<string>();

            if(Arguments != null)
            {
                if(InjectVariables)
                {
                    var sb = new StringBuilder();
                    
                    foreach(var arg in Arguments)
                    {
                        if(arg.StartsWith("$"))
                        {
                            switch(arg.Substring(1))
                            {
                                // [dho] @TODO other variables - 28/08/18

                                case CommandLineVariables.SessionFilesWritten:{
                                    if(filesWritten.Count > 0)
                                    {
                                        sb.Append("\"" + String.Join("\" \"", filesWritten.Keys) + "\"");
                                        sb.Append(" ");
                                    }
                                }
                                break;

                                default:{
                                    result.AddMessages(
                                        new Message(MessageKind.Error, $"Unsupported command line variable '{arg}'")
                                        {
                                            Tags = tags
                                        }
                                    );
                                }
                                break;
                            }
                        }
                        else
                        {
                            sb.Append(arg);
                            sb.Append(" ");
                        }
                    }

                    result.Value = sb.ToString().Trim();
                }
                else
                {
                    result.Value = String.Join(" ", Arguments);
                }
            }
            else
            {
                result.Value = String.Empty;
            }

            return result;
        }
    }
}