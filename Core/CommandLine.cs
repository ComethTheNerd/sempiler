namespace Sempiler
{
    using Sempiler.Diagnostics;
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System;
    using System.Threading;


    public class CommandLineErrorData
    {
        public List<string> Lines { get; }

        public CommandLineErrorData(List<string> lines)
        {
            Lines = lines;
        }
    }

    public class CommandLineMessage : Message
    {
        public readonly CommandLineErrorData Data;

        public CommandLineMessage(MessageKind kind, string description, CommandLineErrorData data) : base(kind, description)
        {
            Data = data;
        }
    }

    // // [dho] similar types to consolidate? FileMarker, EmissionMarker - 08/09/18
    // public struct CommandLineCompilerDiagnostic
    // {
    //     public string FilePath { get; set; }
    //     public int LineNumber { get; set; }
    //     public int ColumnIndex { get; set; }
    //     public string Description { get; set; }
    // }

    public static class CommandLineVariables
    {
        public const string SessionFilesWritten = "session.filesWritten";
    
        public const string DiagnosticFilePath = "diagnostic.filePath";
        public const string DiagnosticLineNumber = "diagnostic.lineNumber";
        public const string DiagnosticColumnIndex = "diagnostic.columnIndex";
        public const string DiagnosticSeverity = "diagnostic.severity";
        public const string DiagnosticDescription = "diagnostics.description";
    }

    public class CommandLineDiagnosticsParser
    {
        public string RawPattern { get; set; }
        
        public string CompiledPattern { get; private set; }

        private int mFilePathDiagnosticIndex = -1;
        private int mLineNumberDiagnosticIndex = -1;
        private int mColumnIndexDiagnosticIndex = -1;
        private int mSeverityDiagnosticIndex = -1;

        private int mDescriptionDiagnosticIndex = -1;

        private CommandLineDiagnosticsParser()
        {
        }

        public string Serialize(Message message)
        {
            // [dho] TODO CLEANUP `message.Hint != null`... unless the compiler will for me *angel emoji* - 12/04/19
            var filePath = message.Hint != null ? message.Hint.File.ToPathString() : "";
            var lineNumber = message.Hint != null ? message.Hint.LineNumber.Start : -1;
            var columnIndex = message.Hint != null ? message.Hint.ColumnIndex.Start : -1;
            var severity = message.Kind.ToString().ToLower();
            var description = message.Description;

            return RawPattern
                    .Replace('$' + CommandLineVariables.DiagnosticFilePath, filePath)
                    .Replace('$' + CommandLineVariables.DiagnosticLineNumber, lineNumber + "")
                    .Replace('$' + CommandLineVariables.DiagnosticColumnIndex, columnIndex + "")
                    .Replace('$' + CommandLineVariables.DiagnosticSeverity, severity)
                    .Replace('$' + CommandLineVariables.DiagnosticDescription, description);
        }
        public List<Message> Parse(string text)
        {
            Regex regex = new Regex(CompiledPattern);

            MatchCollection matches = regex.Matches(text);

            var messages = new List<Message>();

            for (int i = 0; i < matches?.Count; ++i)
            {
                try
                {
                    var groups = matches[i].Groups;

                    var lineNumber = mLineNumberDiagnosticIndex > -1 ? int.Parse(groups[mLineNumberDiagnosticIndex].Value) : -1;
                    var columnIndex = mColumnIndexDiagnosticIndex > -1 ? 
                        String.IsNullOrEmpty(groups[mColumnIndexDiagnosticIndex].Value) ? 0 : int.Parse(groups[mColumnIndexDiagnosticIndex].Value)
                    : 0;

                    var hint = mFilePathDiagnosticIndex > -1 ? new FileMarker 
                    {
                        File = FileSystem.ParseFileLocation(groups[mFilePathDiagnosticIndex].Value),
                        // [dho] we do not get told enough information to know the precise
                        // end line and column - 16/0918
                        LineNumber = new Range(lineNumber, lineNumber),
                        ColumnIndex = new Range(columnIndex, columnIndex)
                    } : null;

                    var description = mDescriptionDiagnosticIndex > -1 ? groups[mDescriptionDiagnosticIndex].Value : "";

                    var severity = mSeverityDiagnosticIndex > -1 ? groups[mSeverityDiagnosticIndex].Value.ToLower() : null;

                    switch (severity)
                    {
                        case "error":
                            messages.Add(new Message(MessageKind.Error, description)
                            {
                                Hint = hint
                            });
                            break;

                        case "warning":
                            messages.Add(new Message(MessageKind.Warning, description)
                            {
                                Hint = hint
                            });
                            break;

                        case "info":
                        default:
                            messages.Add(new Message(MessageKind.Info, description)
                            {
                                Hint = hint
                            });
                            break;

                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return messages;
        }

        public static CommandLineDiagnosticsParser Create(string pattern)
        {
            var cldp = new CommandLineDiagnosticsParser
            {
                RawPattern = pattern,
                CompiledPattern = pattern
            };

            var varNames = new string[] 
            {
                CommandLineVariables.DiagnosticFilePath,
                CommandLineVariables.DiagnosticLineNumber,
                CommandLineVariables.DiagnosticColumnIndex,
                CommandLineVariables.DiagnosticSeverity,
                CommandLineVariables.DiagnosticDescription
            };

            var regex = new Regex(@"\$(" + String.Join("|", varNames).Replace(".", @"\.") + ")");
        
            MatchCollection matches = regex.Matches(pattern);

            for (int i = 0; i < matches?.Count; ++i)
            {
                var groups = matches[i].Groups;

                // [dho] group at index [0] will have the $ prefix present - 28/08/18
                var varName = groups[0].Value;

                // var start = groups[0].Index;
                // var length = groups[0].Length;

                // [dho] group at index [1] will have the $ prefix removed - 28/08/18
                var name = groups[1].Value;

                // [dho] +1 because the [0] will be the whole string that contained matches during parsing - 28/08/18
                var diagnosticIndex = i + 1;

                // [dho] NOTE we cannot use .Remove(start, length).Insert(start, value) because the value's we are replacing
                // with are not the same length as the matched varName so subsequent indices would be incorrect - 28/08/18
                switch(name)
                {
                    case CommandLineVariables.DiagnosticFilePath:{
                        cldp.CompiledPattern = cldp.CompiledPattern.Replace(varName, @"(.*)");
                        cldp.mFilePathDiagnosticIndex = diagnosticIndex;
                    }
                    break;

                    case CommandLineVariables.DiagnosticLineNumber:{
                        cldp.CompiledPattern = cldp.CompiledPattern.Replace(varName, @"(\d+)");
                        cldp.mLineNumberDiagnosticIndex = diagnosticIndex;
                    }
                    break;
                    case CommandLineVariables.DiagnosticColumnIndex:{
                        cldp.CompiledPattern = cldp.CompiledPattern.Replace(varName, @"(\d+)");
                        cldp.mColumnIndexDiagnosticIndex = diagnosticIndex;
                    }
                    break;

                    case CommandLineVariables.DiagnosticSeverity:{
                        cldp.CompiledPattern = cldp.CompiledPattern.Replace(varName, @"(info|warning|error)");
                        cldp.mSeverityDiagnosticIndex = diagnosticIndex;
                    }
                    break;

                    case CommandLineVariables.DiagnosticDescription:{
                        cldp.CompiledPattern = cldp.CompiledPattern.Replace(varName, @"(.*)");
                        cldp.mDescriptionDiagnosticIndex = diagnosticIndex;
                    }
                    break;
                }
            }

            return cldp;
        }

        private static CommandLineDiagnosticsParser _gcc;
        public static CommandLineDiagnosticsParser GCC {
            get {
                if(_gcc == null)
                {
                    _gcc = Create(@"^$" + CommandLineVariables.DiagnosticFilePath + ":$" + CommandLineVariables.DiagnosticLineNumber + 
                                ":$" + CommandLineVariables.DiagnosticColumnIndex + @":\s+$" + CommandLineVariables.DiagnosticSeverity + 
                                    @":\s+$" + CommandLineVariables.DiagnosticDescription + "$");
                }

                return _gcc;
            }
        }


        private static CommandLineDiagnosticsParser _mcs;
        public static CommandLineDiagnosticsParser MCS {
            get {
                if(_mcs == null)
                {
                    _mcs = Create(@"^$" + CommandLineVariables.DiagnosticSeverity + 
                                    @"\s+CS[0-9]+:\s+$" + CommandLineVariables.DiagnosticDescription + "$");
                }

                return _mcs;
            }
        }

        private static CommandLineDiagnosticsParser _javaC;
        public static CommandLineDiagnosticsParser JavaC {
            get {
                if(_javaC == null)
                {
                    _javaC = Create(@"^$" + CommandLineVariables.DiagnosticFilePath + ":$" + CommandLineVariables.DiagnosticLineNumber + 
                                @":\s+$" + CommandLineVariables.DiagnosticSeverity + @":\s+$" + CommandLineVariables.DiagnosticDescription + "$");
                }

                return _javaC;
            }
        }

        // private static CommandLineDiagnosticsParser _nodeStack;
        // public static CommandLineDiagnosticsParser NodeStack {
        //     get {
        //         if(_nodeStack == null)
        //         {
        //             _nodeStack = Create(@"^$" + CommandLineVariables.DiagnosticFilePath + ":$" + CommandLineVariables.DiagnosticLineNumber + 
        //                         @"\n\s+$" + CommandLineVariables.DiagnosticDescription);
        //         }

        //         return _nodeStack;
        //     }
        // }
    }

    public static class CommandLine
    {
        // static string CMDFileName()
        // {
        //     if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //     {
        //         return "cmd.exe";
        //     }
        //     else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //     {
        //         return "/bin/bash";
        //     }
        //     else
        //     {
        //         return null;
        //     }
        // }

        // public static Result<IEnumerable<KeyValuePair<string, string>>> IterateArguments(string[] args)
        // {
        //     foreach (var arg in args)
        //     {
        //         if (arg.Length > 2)
        //         {
        //             if (arg[0] == '-' && arg[1] == '-')
        //             {
        //                 var x = arg.Substring(2).Split('=');

        //                 string name = null, value = null;

        //                 switch (x.Length)
        //                 {
        //                     case 1:
        //                         {
        //                             name = x[0];
        //                         };
        //                         break;

        //                     case 2:
        //                         {
        //                             name = x[0];
        //                             value = x[1];
        //                         }
        //                         break;

        //                     default:
        //                         {
        //                             // err
        //                         }
        //                         break;
        //                 }

        //                 switch (name)
        //                 {
        //                     case "config":
        //                         {
        //                             if (value != null)
        //                             {
        //                                 c.ConfigPath = value;
        //                             }
        //                             else
        //                             {
        //                                 // error
        //                             }
        //                         }
        //                         break;

        //                     default:
        //                         {
        //                             // error
        //                         }
        //                         break;
        //                 }
        //             }
        //             else
        //             {
        //                 // error malformed
        //             }
        //         }
        //     }
        // }


        // public static string InjectVariableValue(string input, string variableName, string value)
        // {
        //     return input.Replace("$" + variableName + "", value);
        // }

        public static Task<Result<(int, List<string>)>> Exec(string fileName, string arguments, CancellationToken token)
        {
            // Console.WriteLine($"SETUP COMMAND LINE EXEC {fileName} {string.Join("", arguments)}");

            var tcs = new TaskCompletionSource<Result<(int, List<string>)>>();
            var result = new Result<(int, List<string>)>();

            var proc = new Process()
            {
                StartInfo = {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    // RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            CancellationTokenRegistration ctr = token.Register(() => proc.Kill());

            proc.Exited += (sender, args) =>
            {
                Console.WriteLine($"PROCESS EXITED COMMAND LINE EXEC {fileName} {string.Join("", arguments)}");

                ctr.Dispose();

                // while(proc.StandardOutput.Peek() >= 0)
                // {
                //     var outLine = proc.StandardOutput.ReadLine();

                //     result.AddInfo(new Sempiler.Diagnostics.Info(outLine));
                // }

                List<string> errLines = new List<string>();

                while (proc.StandardError.Peek() >= 0)
                {
                    var errLine = proc.StandardError.ReadLine();

                    errLines.Add(errLine);
                }

                if (errLines.Count > 0)
                {
                    var data = new CommandLineErrorData(errLines);

                    result.AddMessages(
                        new CommandLineMessage(MessageKind.Error, $"Running {fileName} {arguments} resulted in error output", data)
                    );
                }


                List<string> outLines = new List<string>();

                // while (proc.StandardOutput.Peek() >= 0)
                // {
                //     var outLine = proc.StandardOutput.ReadLine();

                //     outLines.Add(outLine);
                // }

                result.Value = (proc.ExitCode, outLines);

                Console.WriteLine($"SET RESULT COMMAND LINE EXEC {fileName} {string.Join("", arguments)}");
                tcs.SetResult(result);
            };

            try
            {
                Console.WriteLine($"START COMMAND LINE EXEC {fileName} {string.Join("", arguments)}");
                if(!proc.Start())
                {
                    result.AddMessages(new Message(MessageKind.Error, $"Could not start process for : {fileName} {String.Join(" ", arguments)}"));
                    
                    tcs.SetResult(result);
                    ctr.Dispose();
                }
            }
            catch(Exception e) // [dho] eg. throws if fileName is not known - 28/08/18
            {
                Console.WriteLine($"ERROR COMMAND LINE EXEC {fileName} {string.Join("", arguments)}");
                result.AddMessages(CreateErrorFromException(e));

                tcs.SetResult(result);
                ctr.Dispose();
            }
        

            return tcs.Task;

            // proc.BeginOutputReadLine();
            // proc.BeginErrorReadLine();

            // cmd.StandardInput.WriteLine("echo Oscar");
            // cmd.StandardInput.Flush();
            // cmd.StandardInput.Close();
        }
    }
}