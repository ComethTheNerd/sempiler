
namespace Sempiler.Core
{
    using Sempiler.AST;
    using Sempiler.Core.Directives;
    using Sempiler.Diagnostics;
    using Sempiler.Emission;
    using Sempiler.Bundler;
    using AST.Diagnostics;
    using static AST.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.SourceHelpers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Main
    {
        public static class InferredConfig
        {
            public const string ResDirName = "res";
            public const string SourceDirName = "src";
            public const string OutDirName = "out";
            public const string PostBuildDirName = "post";

            public const string EntrypointFileName = "index";
        }

        public static Result<Session> Compile(IDirectoryLocation baseDirectory, IEnumerable<string> inputPaths, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<Session>();

            var session = new Session
            {
                Start = DateTime.UtcNow,
                BaseDirectory = baseDirectory,
                InputPaths = inputPaths,
                Server = server
            };

            var parser = new Sempiler.Parsing.PolyParser();

            var ast = new RawAST();

            var initiallyParsedComponents = result.AddMessages(
                Sempiler.CompilerHelpers.Parse(parser, session, ast, sourceProvider, inputPaths, token)
            );

            if (HasErrors(result) || token.IsCancellationRequested) return result;


            var artifacts = session.Artifacts = result.AddMessages(ParseArtifacts(ast, token));


            if (HasErrors(result) || token.IsCancellationRequested) return result;


            session.Ancillaries = new Dictionary<string, List<Ancillary>>(artifacts.Count);

            {
                foreach (var kv in artifacts)
                {
                    var artifactName = kv.Key;

                    session.Ancillaries[artifactName] = new List<Ancillary>();
                }
            }

            {
                session.FilesWritten = new Dictionary<string, Dictionary<string, OutFile>>();
            }

            // [dho] disabling inferred folders - 20/10/19
            /* 
            var baseDirPath = baseDirectory.ToPathString();

            var srcDirPath = (baseDirPath + "/" + InferredConfig.SourceDirName);
            var inferSources = Directory.Exists(srcDirPath);

            if(!inferSources)
            {
                result.AddMessages(new Message(MessageKind.Info, 
                    $"Will not infer sources because expected source path does not exist : '{srcDirPath}'"));
            }
         

            var resDirPath = (baseDirPath + "/" + InferredConfig.ResDirName);
            var inferRes = Directory.Exists(resDirPath);

            if(!inferRes)
            {
                result.AddMessages(new Message(MessageKind.Info, 
                    $"Will not infer resources because expected resource path does not exist : '{resDirPath}'"));
            }
            */


            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            {
                if (HasErrors(result) || token.IsCancellationRequested) return result;

                var l = new object();

                Parallel.ForEach(artifacts, (kv, state) =>
                {

                    var artifact = kv.Value;
                    var ancillary = new Ancillary(AncillaryRole.MainApp, ast.Clone());

                    session.Ancillaries[artifact.Name].Add(ancillary);


                    // var artifactAST = session.ASTs[artifact.Name].Clone();

                    var artifactSeedComponents = new List<Component>(initiallyParsedComponents);

                    try
                    {
                        // [dho] disabling inferred folders - 20/10/19
                        /*
                        if(inferSources)
                        {
                            var inferredArtifactSrcDirPath = srcDirPath + $"/{artifact.Name}";

                            if(Directory.Exists(inferredArtifactSrcDirPath))
                            {
                                // [dho] automatically load sources from conventional source path, that we expect to have the 
                                // same name as the artifact - 21/05/19
                                var inputs = new [] { 
                                    // new SourceFilePatternMatchInput(srcDirPath, $"./{artifact.Name}.*"),
                                    new SourceFilePatternMatchInput(srcDirPath, $"./{artifact.Name}/*"),
                                };

                                var inferredComponentsFromSourcePaths = result.AddMessages(
                                    ParseNewSources(inputs, session, artifact, ancillary.AST, sourceProvider, server, token)
                                );

                                if(HasErrors(result) || token.IsCancellationRequested) return;

                                artifactSeedComponents.AddRange(inferredComponentsFromSourcePaths.NewComponents);
                            }
                            else
                            {
                                result.AddMessages(new Message(MessageKind.Info, 
                                    $"Will not infer sources for '{artifact.Name}' because expected source path does not exist : '{inferredArtifactSrcDirPath}'"));
                            }
                        }

                        // var artifactResources = new List<ISource>();
                        
                        if(inferRes)
                        {
                            var inferredArtifactResDirPath = resDirPath + $"/{artifact.Name}";

                            if(Directory.Exists(inferredArtifactResDirPath))
                            {
                                // [dho] automatically load resources from conventional resource path, that we expect to have the 
                                // same name as the artifact - 19/07/19
                                var inputs = new [] { 
                                    // new SourceFilePatternMatchInput(resDirPath, $"./{artifact.Name}.*"),
                                    new SourceFilePatternMatchInput(resDirPath, $"./{artifact.Name}/*"),
                                };

                                var inferredResources = result.AddMessages(SourceHelpers.EnumerateSourceFilePatternMatches(inputs));
                                
                                if(inferredResources != null)
                                {
                                    lock(l)
                                    {
                                        ancillary.Resources.AddRange(inferredResources);
                                    }
                                }

                                if(HasErrors(result) || token.IsCancellationRequested) return;
                            }
                            else
                            {
                                result.AddMessages(new Message(MessageKind.Info, 
                                    $"Will not infer resources for '{artifact.Name}' because expected resource path does not exist : '{inferredArtifactResDirPath}'"));
                            }
                        }*/


                        // [dho] processing artifact and any compile time directives - 21/05/19
                        {
                            var task = ProcessArtifact(session, artifact, ancillary, artifactSeedComponents, sourceProvider, server, token);

                            task.Wait();

                            lock (l)
                            {
                                ancillary.AST = result.AddMessages(task.Result);
                            }

                            if (HasErrors(task.Result)) return;
                        }
                    }
                    catch (Exception e)
                    {
                        lock (l)
                        {
                            result.AddMessages(CreateErrorFromException(e));
                        }
                    }
                });
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // {
            //     if(HasErrors(result) || token.IsCancellationRequested) return result;

            //     foreach(var kv in session.BridgeIntents)
            //     {
            //         var sourceArtifactName = kv.Key;

            //         foreach(var bridgeIntent in kv.Value)
            //         {
            //             result.AddMessages(
            //                 Bridging.ProcessBridgeIntent(session, sourceArtifactName, bridgeIntent, token)
            //             );

            //             if(HasErrors(result) || token.IsCancellationRequested) return result;
            //         }
            //     }
            // }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            {
                if (HasErrors(result) || token.IsCancellationRequested) return result;

                var l = new object();

                Parallel.ForEach(artifacts, (kv, state) =>
                {

                    var artifact = kv.Value;

                    try
                    {
                        var task = BundleAndWriteArtifact(session, artifact, token);

                        task.Wait();

                        lock (l)
                        {
                            session.FilesWritten[artifact.Name] = result.AddMessages(task.Result);
                        }

                        if (HasErrors(task.Result)) return;

                    }
                    catch (Exception e)
                    {
                        lock (l)
                        {
                            result.AddMessages(CreateErrorFromException(e));
                        }
                    }
                });
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




            // [dho] TODO Post Scripts - 30/05/19




            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            session.End = DateTime.UtcNow;

            result.Value = session;

            return result;
        }

        private static Result<Dictionary<string, Artifact>> ParseArtifacts(AST.RawAST ast, CancellationToken token)
        {
            var result = new Result<Dictionary<string, Artifact>>();

            var artifacts = result.Value = new Dictionary<string, Artifact>();

            var root = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

            var domain = ASTNodeFactory.Domain(ast, root);

            foreach (var component in domain.Components)
            {
                foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(domain.AST, component.ID))
                {
                    var r = MaybeParseArtifactDeclaration(domain.AST, child);

                    result.AddMessages(r);

                    if (!HasErrors(r) && r.Value.Item1 /* is artifact decl */)
                    {
                        var artifact = r.Value.Item2;

                        if (artifacts.ContainsKey(artifact.Name))
                        {
                            result.AddMessages(new NodeMessage(MessageKind.Error, $"Duplicate artifact declarations with name : '{artifact.Name}'", child)
                            {
                                Hint = GetHint(child.Origin),
                                // Tags = DiagnosticTags
                            });
                        }
                        else
                        {
                            artifacts[artifact.Name] = artifact;
                        }
                    }
                }
            }

            return result;
        }

        // [dho] this is a bit painful!! Statically parsing out artifact declarations - 06/05/19
        private static Result<(bool, Artifact)> MaybeParseArtifactDeclaration(RawAST ast, Node node)
        {
            var result = new Result<(bool, Artifact)>()
            {
                Value = (false, default(Artifact))
            };

            if (node.Kind == SemanticKind.Directive)
            {
                var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

                if (directive.Name == CTDirective.CodeExec &&
                    directive.Subject?.Kind == SemanticKind.Invocation)
                {
                    var invocation = ASTNodeFactory.Invocation(directive.AST, directive.Subject);

                    if (invocation.Subject?.Kind == SemanticKind.Identifier)
                    {
                        var name = ASTNodeFactory.Identifier(invocation.AST, (DataNode<string>)invocation.Subject);

                        if (ASTNodeHelpers.GetLexeme(name) == CTAPISymbols.Build)
                        {
                            var artifactName = default(string);
                            var artifactRole = default(ArtifactRole);
                            var artifactTargetLang = default(string);
                            var artifactTargetPlatform = default(string);

                            var template = invocation.Template;

                            if (template.Length > 0)
                            {
                                var templateMarker = template[0];

                                result.AddMessages(new NodeMessage(MessageKind.Error, $"Artifact declaration should not be templated", templateMarker)
                                {
                                    Hint = GetHint(templateMarker.Origin),
                                    // Tags = DiagnosticTags
                                });
                            }

                            var args = invocation.Arguments;

                            if (args.Length != 3)
                            {
                                result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected 3 arguments", invocation)
                                {
                                    Hint = GetHint(invocation.Origin),
                                    // Tags = DiagnosticTags
                                });
                            }

                            for (int i = 0; i < args.Length; ++i)
                            {
                                var arg = args[i];

                                if (arg.Kind == SemanticKind.StringConstant)
                                {
                                    var str = ASTNodeFactory.StringConstant(invocation.AST, (DataNode<string>)arg);

                                    if (String.IsNullOrEmpty(str.Value))
                                    {
                                        result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected non empty string constant", arg)
                                        {
                                            Hint = GetHint(arg.Origin),
                                            // Tags = DiagnosticTags
                                        });

                                        continue;
                                    }

                                    switch (i)
                                    {
                                        case 0:
                                            artifactName = str.Value;
                                            break;

                                        case 1:
                                            artifactTargetLang = str.Value;
                                            break;

                                        case 2:
                                            artifactTargetPlatform = str.Value;
                                            break;
                                    }
                                }
                                else
                                {
                                    result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected non empty string constant", arg)
                                    {
                                        Hint = GetHint(arg.Origin),
                                        // Tags = DiagnosticTags
                                    });
                                }
                            }

                            // [dho] infer artifact role from target platform - 21/05/19
                            {
                                switch (artifactTargetPlatform)
                                {
                                    case ArtifactTargetPlatform.Node:
                                    case ArtifactTargetPlatform.FirebaseFunctions:
                                    case ArtifactTargetPlatform.AWSLambda:
                                    case ArtifactTargetPlatform.ZeitNow:
                                        artifactRole = ArtifactRole.Server;
                                        break;

                                    case ArtifactTargetPlatform.Android:
                                    case ArtifactTargetPlatform.IOS:
                                    case ArtifactTargetPlatform.SwiftUI:
                                    case ArtifactTargetPlatform.WebBrowser:
                                        artifactRole = ArtifactRole.Client;
                                        break;

                                    default:
                                        if (artifactTargetLang == ArtifactTargetLang.SQL)
                                        {
                                            artifactRole = ArtifactRole.Database;
                                        }
                                        else
                                        {
                                            result.AddMessages(
                                                new Message(MessageKind.Error, $"Could not infer artifact role for '{artifactName}' from target language '{artifactTargetLang}' and target platform '{artifactTargetPlatform}'")
                                            );
                                        }
                                        break;
                                }
                            }


                            if (!HasErrors(result))
                            {
                                ASTHelpers.RemoveNodes(directive.AST, directive.ID);
                                // if(!ASTHelpers.DeregisterNode(runDirective.AST, runDirective.Node))
                                // {
                                //     result.AddMessages(new NodeMessage(MessageKind.Error, $"Could not remove artifact declaration from AST", runDirective)
                                //     {
                                //         Hint = GetHint(runDirective.Origin),
                                //         // Tags = DiagnosticTags
                                //     });
                                // }
                                var artifact = new Artifact(artifactRole, artifactName, artifactTargetLang, artifactTargetPlatform);

                                result.Value = (true, artifact);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static async Task<Result<RawAST>> ProcessArtifact(Session session, Artifact artifact, Ancillary ancillary, List<Component> newlyAddedComponents, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<RawAST>();


            // [dho] TODO CHECK it is OK to put this transformer here.. if we ever have a different input language other
            // than TypeScript/JavaScript then we probably do not want to always run this transformer unless the source from which
            // the artifact originated was a typescript file
            // (could we just check the origin is a file with a ts/tsx/js/jsx extension?) - 04/08/19
            {
                var tsSyntaxPolyfillTransformer = new Sempiler.Transformation.TypeScriptSyntaxPolyfillTransformer();

                /* ast = */
                result.AddMessages(await tsSyntaxPolyfillTransformer.Transform(session, artifact, ancillary.AST, token));

                if (HasErrors(result) || token.IsCancellationRequested) return result;
            }

            // [dho] find and replace any nodes that are intent to create a `ViewDeclaration`. For example, a function that returns
            // a view chunk may be detected to be meant as a `ViewDeclaration`, ie. a factory for producing a view - 14/06/19
            {
                var viewDeclIntentTransformer = new Sempiler.Transformation.ViewDeclarationIntentTransformer();

                /* ast = */
                result.AddMessages(await viewDeclIntentTransformer.Transform(session, artifact, ancillary.AST, token));

                if (HasErrors(result) || token.IsCancellationRequested) return result;
            }

            result.Value = ancillary.AST;


            // [dho] NOTE we look for MPInfo on *only* the newly added components, not the entire AST as we may
            // be subcompiling a subset of the tree because of injecting new sources etc. - 30/05/19
            var mpInfo = MetaProgramming.GetMetaProgrammingInfo(session, ancillary.AST, newlyAddedComponents, token);

            // [dho] the source contains expressions that require compile time code execution to evaluate - 30/05/19
            var requiresCTExec = mpInfo.CTDirectives.Count > 0;

            if (requiresCTExec)
            {
                // [dho] NOTE no lock on ast because the caller provides a fresh clone to manipulate - 14/05/19
                result.AddMessages(await CTExec(session, artifact, ancillary, newlyAddedComponents, sourceProvider, server, token));

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                // [dho] TODO OPTIMIZE we naively reload the meta programming info after CTExec in case the 
                // AST has been modified and includes new bridge intents etc as a result of the evaluation - 30/05/19
                mpInfo = MetaProgramming.GetMetaProgrammingInfo(session, ancillary.AST, newlyAddedComponents, token);
            }

            // [dho] only acquire lock if we found bridge intent directives - 30/05/19
            if (mpInfo.BridgeIntentDirectives.Count > 0)
            {
                lock (ancillary.BridgeIntents)
                {
                    // [dho] NOTE we use `AddRange` because the `ProcessArtifact` function may get called multiple
                    // times during the compilation of an artifact, so we do not want to squash anything by wiping out
                    // the existing data in the map each time - 30/05/19 
                    ancillary.BridgeIntents.AddRange(mpInfo.BridgeIntentDirectives);
                }
            }

            return result;
        }

        private static string GenerateCTID()
        {
            return "_" + System.Guid.NewGuid().ToString().Replace('-', '_');
        }


        static object ctExecLock = new object();
        static Dictionary<string, CancellationTokenSource> CTExecServerHandlerCTSCache = new Dictionary<string, CancellationTokenSource>();

        private static async Task<Result<object>> CTExec(Session session, Artifact artifact, Ancillary ancillary, List<Component> newlyAddedComponents, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<object>();

            var consumer = default(Sempiler.Consumption.IConsumer);
            var filesWritten = default(Dictionary<string, OutFile>);

            var ctid = GenerateCTID();

            var absCTIDDirPath = FileSystem.Resolve(/* session.BaseDirectory.ToPathString() */"./compiler", $"./{ctid}/");

            var outDirLocation = FileSystem.ParseDirectoryLocation(absCTIDDirPath);


            // if(artifact.TargetLang == ArtifactTargetLang.Java)
            // {
            //     // [dho] use our special compile time emitter - 12/04/19
            //     var emitter = new CTJavaEmitter(ctid, server.IPAddress, server.Port, outDirLocation);

            //     {
            //         var filePathsToInit = new string[newlyAddedComponents.Count];

            //         for(int i = 0; i < filePathsToInit.Length; ++i) filePathsToInit[i] = newlyAddedComponents[i].Name;

            //         // [dho] make sure we only evaluate directives in these specific files - 06/05/19
            //         emitter.FilePathsToInit = filePathsToInit;
            //     }

            //     var outFileCollection = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ast, token));

            //     if(HasErrors(result) || token.IsCancellationRequested) return result;

            //     filesWritten = result.AddMessages(await FileSystem.Write(absCTIDDirPath, outFileCollection, token));

            //     consumer = new Sempiler.Consumption.CommandLineConsumer
            //     {
            //         Name = "CTJava",
            //         // [dho] NOT using `ArtifactTargetLang` constant because this is a filename,
            //         // not an internal identifier representing the target platform, and would
            //         // break unnecessarily if we change the value of `ArtifactTargetLang.Java` - 21/05/19
            //         FileName = "java",
            //         ParseDiagnostics = true,
            //         DiagnosticsParser = CommandLineDiagnosticsParser.JavaC, 
            //         InjectVariables = false,
            //         Arguments = filesWritten.Keys
            //     };
            // }
            // else
            {


                var task = new CTExecTypeScriptBundler().Bundle(session, artifact, new List<Ancillary>() { ancillary }, token);

                task.Wait();

                var outFileCollection = result.AddMessages(task.Result);

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                filesWritten = result.AddMessages(await FileSystem.Write(absCTIDDirPath, outFileCollection, token));

                // consumer = new Sempiler.Consumption.CommandLineConsumer
                // {
                //     Name = "CTTypeScript",
                //     FileName = "ruby",
                //     ParseDiagnostics = true,
                //     // [dho] TODO parse error messages!! Must. Simplify. Toolchain. - 11/07/19
                //     DiagnosticsParser = CommandLineDiagnosticsParser.GCC, 
                //     InjectVariables = false,
                //     Arguments = new [] { 
                //         "-e",
                //         $"\"Dir.chdir('{absCTIDDirPath}') do `node *.js` end\""
                //     }
                // };
                consumer = new Sempiler.Consumption.CommandLineConsumer
                {
                    Name = "CTTypeScript",
                    FileName = "node",
                    ParseDiagnostics = true,
                    // [dho] TODO parse error messages!! Must. Simplify. Toolchain. - 11/07/19
                    DiagnosticsParser = CommandLineDiagnosticsParser.GCC,
                    InjectVariables = false,
                    Arguments = new[] {
                        $"\"{absCTIDDirPath}/{CTExecTypeScriptBundler.EntrypointFileName}.js\""
                    }
                };
            }


            if (HasErrors(result)) return result;

            if (!token.IsCancellationRequested)
            {
                CancellationTokenSource consumptionCTS;
                DuplexSocketServer.OnMessageDelegate messageHandler = null;

                lock (ctExecLock)
                {
                    if (CTExecServerHandlerCTSCache.ContainsKey(artifact.Name))
                    {
                        consumptionCTS = CTExecServerHandlerCTSCache[artifact.Name];
                    }
                    else
                    {
                        consumptionCTS = CTExecServerHandlerCTSCache[artifact.Name] = new CancellationTokenSource();

                        using (CancellationTokenRegistration ctr = token.Register(() => consumptionCTS.Cancel()))
                        {
                            var messageHistoryLock = new object();
                            var messageHistory = new Dictionary<string, bool>();

                            messageHandler = CreateOnMessageDelegate(session, artifact, sourceProvider, server, consumptionCTS, result);

                            server.OnMessage += messageHandler;
                        }
                    }
                }


                result.AddMessages(await consumer.Consume(session, artifact, ancillary.AST, filesWritten, consumptionCTS.Token));

                if (messageHandler != null)
                {
                    lock (ctExecLock)
                    {
                        System.Diagnostics.Debug.Assert(CTExecServerHandlerCTSCache.Remove(artifact.Name));
                        server.OnMessage -= messageHandler;
                    }
                }
            }

            // [dho] delete the build files we created - 06/05/19
            {
                var deleteFilesWrittenTask = FileSystem.Delete(filesWritten.Keys);

                var deleteCTIDDirTask = FileSystem.Delete(new string[] { absCTIDDirPath });

                result.AddMessages(await deleteFilesWrittenTask);
                result.AddMessages(await deleteCTIDDirTask);
            }

            return result;



        }


        private static DuplexSocketServer.OnMessageDelegate CreateOnMessageDelegate(Session session, Artifact artifact, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationTokenSource cts, Result<object> result)
        {
            var messageHistoryLock = new object();
            var messageHistory = new Dictionary<string, bool>();

            return (socket, message) =>
            {
                var command = CTProtocolHelpers.ParseCommand(message);

                // [dho] this message was meant for a different subscriber - 06/05/19
                if (command.ArtifactName != artifact.Name) return;

                // System.Console.WriteLine("THREAD #" + System.Threading.Thread.CurrentThread.ManagedThreadId + " -> " +  command.MessageID + " COMMAND ARTIFACT NAME : " + command.ArtifactName);
                // System.Console.WriteLine("THREAD #" + System.Threading.Thread.CurrentThread.ManagedThreadId + " -> " + command.MessageID + " COMMAND TARGET X INDEX : " + command.AncillaryIndex);
                // System.Console.WriteLine("THREAD #" + System.Threading.Thread.CurrentThread.ManagedThreadId + " -> " + command.MessageID + " COMMAND KIND : " + command.Kind);
                // System.Console.WriteLine("THREAD #" + System.Threading.Thread.CurrentThread.ManagedThreadId + " -> " + command.MessageID + " COMMAND ARGUMENTS : " + string.Join(",", command.Arguments));


                var isNewMessage = true;

                lock (messageHistoryLock)
                {
                    isNewMessage = !messageHistory.ContainsKey(command.MessageID);

                    messageHistory[command.MessageID] = true;
                }


                string response = default(string);

                if (isNewMessage)
                {
                    Result<string> r;

                    var ancillary = session.Ancillaries[artifact.Name][command.AncillaryIndex];

                    // [dho] NOTE no lock on ast because the caller provides a fresh clone to manipulate - 14/05/19
                    r = HandleServerMessage(session, command, artifact, ancillary, sourceProvider, server, cts.Token);

                    result.AddMessages(r);

                    if (HasErrors(r))
                    {
                        // [dho] kill the consumer if we get any errors - 06/05/19
                        // [dho] NOTE because we have not called `Send`, the socket inside the CT program will be waiting,
                        // and so is effectively paused, which is what we want - not for it to continue executing in an unknown
                        // state - 20/04/19
                        cts.Cancel();
                        return;
                    }
                    else
                    {
                        response = r.Value;
                    }
                }


                server.Send(socket, response);
            };
        }

        private static Result<string> HandleServerMessage(Session session, CTProtocolCommand command, Artifact artifact, Ancillary ancillary, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<string>();

            switch (command.Kind)
            {
                case CTProtocolCommandKind.AddCapability:
                    {
                        var name = command.Arguments[CTProtocolAddCapabilityCommand.NameIndex];
                        var type = (ConfigurationPrimitive)Enum.Parse(typeof(ConfigurationPrimitive), command.Arguments[CTProtocolAddEntitlementCommand.TypeIndex]);
                        var values = new string[command.Arguments.Length - 2];
                        Array.Copy(command.Arguments, 2, values, 0, values.Length);

                        ancillary.Capabilities.Add(
                            new Capability
                            {
                                Name = name,
                                Type = type,
                                Values = values
                            }
                        );

                        result.Value = ("Added capability " + name);
                    }
                    break;

                case CTProtocolCommandKind.AddDependency:
                    {
                        var name = command.Arguments[CTProtocolAddDependencyCommand.NameIndex];

                        if (command.Arguments.Length > 1)
                        {
                            var version = command.Arguments[CTProtocolAddDependencyCommand.VersionIndex];

                            ancillary.Dependencies.Add(
                                new Dependency
                                {
                                    Name = name,
                                    Version = version
                                }
                            );

                            result.Value = ("Added dependency " + name + " of version " + version);
                        }
                        else
                        {
                            ancillary.Dependencies.Add(
                                new Dependency
                                {
                                    Name = name
                                }
                            );

                            result.Value = ("Added dependency " + name);
                        }
                    }
                    break;

                case CTProtocolCommandKind.AddEntitlement:
                    {
                        var name = command.Arguments[CTProtocolAddEntitlementCommand.NameIndex];
                        var type = (ConfigurationPrimitive)Enum.Parse(typeof(ConfigurationPrimitive), command.Arguments[CTProtocolAddEntitlementCommand.TypeIndex]);
                        var values = new string[command.Arguments.Length - 2];
                        Array.Copy(command.Arguments, 2, values, 0, values.Length);

                        ancillary.Entitlements.Add(
                            new Entitlement
                            {
                                Name = name,
                                Type = type,
                                Values = values
                            }
                        );

                        result.Value = ("Added entitlement " + name);
                    }
                    break;

                case CTProtocolCommandKind.AddPermission:
                    {
                        var name = command.Arguments[CTProtocolAddPermissionCommand.NameIndex];
                        var description = command.Arguments[CTProtocolAddPermissionCommand.DescriptionIndex];

                        ancillary.Permissions.Add(
                            new Permission
                            {
                                Name = name,
                                Description = description
                            }
                        );

                        result.Value = ("Added permission " + name);
                    }
                    break;

                case CTProtocolCommandKind.DeleteNode:
                    {
                        var nodeID = command.Arguments[CTProtocolDeleteNodeCommand.NodeIDIndex];
                        var node = ASTHelpers.GetNode(ancillary.AST, nodeID);

                        if (node != null)
                        {
                            ASTHelpers.RemoveNodes(ancillary.AST, node.ID);
                            result.Value = ("Deleted node " + node.ID);
                            break;
                            // if (ASTHelpers.DeregisterNode(ast, node))
                            // {
                            //     result.Value = ("Deleted node " + node.ID);
                            //     break;
                            // }
                        }
                        // [dho] NOTE no longer treating the inability to find a node as an error because of 
                        // the case where you have a #run inside an #if, which would mean the #if is deleted 
                        // before the #run asks to be deleted - 03/05/19
                    }
                    break;

                // case CTProtocolCommandKind.ReplaceNodeWithValue:
                //     {
                //         var nodeID = command.Arguments[CTProtocolReplaceNodeWithValueCommand.NodeIDIndex];

                //         var p = ASTHelpers.GetPosition(ast, nodeID);

                //         if (p.Index > -1) // [dho] NOTE this implies that it is not possible to replace the root - 14/05/19
                //         {
                //             var type = command.Arguments[CTProtocolReplaceNodeWithValueCommand.TypeIndex];
                //             var value = command.Arguments[CTProtocolReplaceNodeWithValueCommand.ValueIndex];

                //             if (type == "java.lang.Integer") // [dho] TODO make this language agnostic!!! - 14/05/19
                //             {
                //                 // [dho] TODO origin! - 23/04/19
                //                 var replacement = NodeFactory.NumericConstant(ast, p.Node.Origin, value);

                //                 ASTHelpers.InsertNodes(ast, p.Parent.ID, new [] { replacement.Node }, p.Role, p.Index);
                //                 ASTHelpers.RemoveNodes(ast, nodeID);

                //                 result.Value = ("Replaced node " + nodeID + " with new node " + replacement.ID);
                //             }
                //             else
                //             {
                //                 result.AddMessages(
                //                     new Message(MessageKind.Error, $"Failed to replace node because type is not supported : '{type}'")
                //                 );
                //             }
                //             break;
                //         }

                //         // [dho] NOTE no longer treating the inability to find a node as an error because of 
                //         // the case where you have a #run inside an #if, which would mean the #if is deleted 
                //         // before the #run asks to be deleted - 03/05/19
                //     }
                //     break;

                // case CTProtocolCommandKind.ReplaceNodeWithNodes:
                //     {
                //         var nodeID = command.Arguments[CTProtocolReplaceNodeWithValueCommand.NodeIDIndex];
                //         var pos = ASTHelpers.GetPosition(ast, nodeID);

                //         if (pos.Index > -1) // [dho] NOTE this implies that it is not possible to replace the root - 14/05/19
                //         {
                //             // [dho] because original nodeID is one of the arguments - 23/04/19
                //             var replacementCount = command.Arguments.Length - 1;

                //             if (replacementCount == 0)
                //             {
                //                 result.AddMessages(
                //                     new Message(MessageKind.Error, $"Failed to replace node because no replacement nodes were provided")
                //                 );
                //             }
                //             else if (replacementCount == 1)
                //             {
                //                 var replacementNodeID = command.Arguments[CTProtocolReplaceNodeWithValueCommand.NodeIDIndex + 1];
                //                 var replacement = ASTHelpers.GetNode(ast, replacementNodeID);

                //                 if (replacement != null)
                //                 {
                //                     ASTHelpers.InsertNodes(ast, pos.Parent.ID, new [] { replacement }, pos.Role, pos.Index);
                //                     ASTHelpers.RemoveNodes(ast, nodeID);

                //                     result.Value = ("Replaced node " + nodeID + " with node " + replacementNodeID);
                //                 }
                //                 else
                //                 {
                //                     result.AddMessages(
                //                         new Message(MessageKind.Error, $"Failed to move node because it does not exist : '{replacementNodeID}'")
                //                     );
                //                     break;
                //                 }
                //             }
                //             else
                //             {
                //                 var success = true;

                //                 var replacements = new Node[replacementCount];

                //                 var indexOffset = CTProtocolReplaceNodeWithValueCommand.NodeIDIndex + 1;

                //                 for (int i = indexOffset; i < command.Arguments.Length; ++i)
                //                 {
                //                     var replacementNodeID = command.Arguments[i];

                //                     var replacement = ASTHelpers.GetNode(ast, replacementNodeID);

                //                     if (replacement != null)
                //                     {
                //                         replacements[i - indexOffset] = replacement;
                //                     }
                //                     else
                //                     {
                //                         result.AddMessages(
                //                             new Message(MessageKind.Error, $"Failed to move node because it does not exist : '{replacementNodeID}'")
                //                         );
                //                         success = false;
                //                         break;
                //                     }
                //                 }

                //                 if (success)
                //                 {
                //                     ASTHelpers.InsertNodes(ast, pos.Parent.ID, replacements, pos.Role, pos.Index);
                //                     ASTHelpers.RemoveNodes(ast, nodeID);

                //                     result.Value = ("Replaced node " + nodeID + " with nodes " + string.Join(",", command.Arguments[CTProtocolReplaceNodeWithNodesCommand.NodeIDIndex + 1]));
                //                 }
                //             }
                //         }
                //         // [dho] NOTE no longer treating the inability to find a node as an error because of 
                //         // the case where you have a #run inside an #if, which would mean the #if is deleted 
                //         // before the #run asks to be deleted - 03/05/19
                //     }
                //     break;

                case CTProtocolCommandKind.AddAncillary: // #run add_sources("foo.ts", "hello-world.ts") 
                    {
                        var role = command.Arguments[CTProtocolAddAncillaryCommand.RoleIndex];

                        var newAncillary = default(Ancillary);

                        if (role == "share-extension")
                        {
                            var newAST = new RawAST();
                            var newDomain = NodeFactory.Domain(newAST, new PhaseNodeOrigin(PhaseKind.Transformation));

                            ASTHelpers.Register(newAST, newDomain.Node);

                            // [dho] TODO CHECK do we want to clone `ancillary.AST` here, or use a fresh AST? - 16/10/19
                            newAncillary = new Ancillary(AncillaryRole.ShareExtension, newAST);

                            session.Ancillaries[artifact.Name].Add(newAncillary);
                        }
                        else
                        {
                            result.AddMessages(
                                new Message(MessageKind.Error, $"Unsupported ancillary role '{role}'")
                            );
                        }

                        if (!HasErrors(result) && !token.IsCancellationRequested)
                        {
                            var baseDirPath = command.Arguments[CTProtocolAddAncillaryCommand.BaseDirPathIndex];

                            var sourcePath = command.Arguments[CTProtocolAddAncillaryCommand.SourcePathIndex];

                            var patterns = new string[] { sourcePath };

                            result.AddMessages(
                                // [dho] NOTE passing in new target! - 16/10/19
                                AddAncillarySources(session, artifact, newAncillary, sourceProvider, server, baseDirPath, patterns, token)
                            );
                        }
                    }
                    break;

                case CTProtocolCommandKind.AddSources: // #run add_sources("foo.ts", "hello-world.ts") 
                    {
                        var baseDirPath = command.Arguments[CTProtocolAddSourcesCommand.BaseDirPathIndex];

                        var patternsStartIndex = CTProtocolAddSourcesCommand.BaseDirPathIndex + 1;
                        var patterns = new string[command.Arguments.Length - patternsStartIndex];

                        Array.Copy(command.Arguments, patternsStartIndex, patterns, 0, patterns.Length);

                        result.AddMessages(
                            AddAncillarySources(session, artifact, ancillary, sourceProvider, server, baseDirPath, patterns, token)
                        );
                    }
                    break;

                case CTProtocolCommandKind.AddAsset:
                    {
                        var baseDirPath = command.Arguments[CTProtocolAddAssetCommand.BaseDirPathIndex];

                        var rawRole = command.Arguments[CTProtocolAddAssetCommand.RoleIndex];
                        AssetRole role = AssetRole.None;

                        if(rawRole == "image")
                        {
                            role = AssetRole.Image;
                        }
                        else if(rawRole == "app-icon")
                        {
                            role = AssetRole.AppIcon;
                        }
                        else
                        {
                            result.AddMessages(
                                new Message(MessageKind.Error, $"Unsupported asset role '{rawRole}'")
                            );
                        }


                        if (!HasErrors(result) && !token.IsCancellationRequested)
                        {
                            var sourcePath = command.Arguments[CTProtocolAddAssetCommand.SourcePathIndex];

                            List<SourceFilePatternMatchInput> inputs = new List<SourceFilePatternMatchInput>()
                            {
                                new SourceFilePatternMatchInput(baseDirPath, sourcePath)
                            };

                            // [dho] TODO filter - 08/11/19
                            SourceFileFilterDelegate filter = path => true;

                            var parsedPaths = result.AddMessages(
                                FilterNewSourceFilePaths(session, ancillary.AST, inputs, filter, token)
                            );

                            if (HasErrors(result) || token.IsCancellationRequested) return result;

                            foreach (var sf in parsedPaths.NewSourceFiles)
                            {
                                string name = Path.GetFileNameWithoutExtension(sf.GetPathString());
                                string size = null;
                                string scale = null;

                                // [dho] NOTE does not support subpixels - 09/11/19
                                var rx = new System.Text.RegularExpressions.Regex(@"(-[0-9]+x[0-9]+)?(@[0-9]+x)?$", System.Text.RegularExpressions.RegexOptions.Compiled);

                                var matches = rx.Matches(name);

                                if (matches.Count > 0)
                                {
                                    var match = matches[0];
                                    var groups = match.Groups;

                                    var suffix = groups[0].Value;
                                    name = name.Substring(0, name.Length - suffix.Length);
                                    size = String.IsNullOrEmpty(groups[1].Value) ? null : groups[1].Value.Substring(1); // [dho] remove trailing '-' - 09/11/19
                                    scale = String.IsNullOrEmpty(groups[2].Value) ? null : groups[2].Value.Substring(1); // [dho] remove trailing '@' - 09/11/19
                                }
if(name == "AppIcon")
{
    int i = 0;
}
                                var existingImgIndex = IndexOfImageAsset(ancillary.Assets, role, name);

                                if (existingImgIndex > -1)
                                {
                                    var imgAssetSet = (ImageAssetSet)ancillary.Assets[existingImgIndex];

                                    imgAssetSet.Images.Add(new ImageAssetMember
                                    {
                                        Size = size,
                                        Scale = scale,
                                        Source = sf
                                    });
                                }
                                else
                                {
                                    var imgAssetSet = new ImageAssetSet
                                    {
                                        Name = name,
                                        Role = role,
                                        Images = new List<ImageAssetMember>()
                                    };

                                    imgAssetSet.Images.Add(new ImageAssetMember
                                    {
                                        Size = size,
                                        Scale = scale,
                                        Source = sf
                                    });

                                    ancillary.Assets.Add(imgAssetSet);
                                }
                            }
                        }
                    }
                    break;

                case CTProtocolCommandKind.AddRes:
                    {
                        var baseDirPath = command.Arguments[CTProtocolAddResCommand.BaseDirPathIndex];

                        var sourcePath = command.Arguments[CTProtocolAddResCommand.SourcePathIndex];

                        List<SourceFilePatternMatchInput> inputs = new List<SourceFilePatternMatchInput>()
                    {
                        new SourceFilePatternMatchInput(baseDirPath, sourcePath)
                    };

                        // [dho] TODO filter - 08/11/19
                        SourceFileFilterDelegate filter = path => true;

                        var parsedPaths = result.AddMessages(
                            FilterNewSourceFilePaths(session, ancillary.AST, inputs, filter, token)
                        );

                        if (HasErrors(result) || token.IsCancellationRequested) return result;

                        var sourceFiles = parsedPaths.NewSourceFiles;

                        ancillary.Resources.AddRange(sourceFiles);
                    }
                    break;

                // [dho] NOT parsed! - 08/07/19
                case CTProtocolCommandKind.AddRawSources: // #compiler add_raw_sources("foo.ts", "hello-world.ts") 
                    {
                        var baseDirPath = command.Arguments[CTProtocolAddRawSourcesCommand.BaseDirPathIndex];

                        List<SourceFilePatternMatchInput> inputs = new List<SourceFilePatternMatchInput>();

                        // [dho] should be resillient to the case where the input path is a directory or regex - 06/05/19
                        for (int i = CTProtocolAddRawSourcesCommand.BaseDirPathIndex + 1; i < command.Arguments.Length; ++i)
                        {
                            var pattern = command.Arguments[i];
                            var @base = pattern.StartsWith(".") ? baseDirPath : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                            inputs.Add(new SourceFilePatternMatchInput(@base, pattern));
                        }

                        if (HasErrors(result) || token.IsCancellationRequested) return result;

                        var componentNames = GetComponentNames(ancillary.AST);
                        SourceFileFilterDelegate filter = path => System.Array.IndexOf(componentNames, path) == -1;

                        var parsedPaths = result.AddMessages(
                            FilterNewSourceFilePaths(session, ancillary.AST, inputs, filter, token)
                        );

                        var newPaths = parsedPaths.NewPaths;

                        if (newPaths.Count > 0)
                        {
                            var newComponents = new Node[newPaths.Count];

                            for (int i = 0; i < newPaths.Count; ++i)
                            {
                                var path = newPaths[i];
                                var text = default(string);

                                try
                                {
                                    // [dho] TODO non blocking I/O - 13/08/18
                                    text = File.ReadAllText(path);
                                }
                                catch (Exception e)
                                {
                                    result.AddMessages(CreateErrorFromException(e));

                                    continue;
                                }

                                var newComponent = NodeFactory.Component(ancillary.AST, new PhaseNodeOrigin(PhaseKind.Parsing), path);
                                {
                                    ASTHelpers.Connect(ancillary.AST, newComponent.ID, new[] {
                                    NodeFactory.CodeConstant(ancillary.AST, new PhaseNodeOrigin(PhaseKind.Parsing), text).Node
                                }, SemanticRole.None);
                                }

                                newComponents[i] = newComponent.Node;
                            }

                            if (HasErrors(result) || token.IsCancellationRequested) return result;


                            var root = ASTHelpers.GetRoot(ancillary.AST);

                            System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

                            ASTHelpers.Connect(ancillary.AST, root.ID, newComponents, SemanticRole.Component);
                        }
                        else if (parsedPaths.TotalPaths.Count == 0)
                        {
                            var pathList = new List<string>();

                            foreach (var input in inputs)
                            {
                                pathList.Add($"\"{input.BaseDirPath}/{input.SearchPattern}\"");
                            }

                            result.AddMessages(
                                new Message(MessageKind.Error, $"Could not find file(s) at specified path(s) : {string.Join(",", pathList)}")
                            );
                        }
                    }
                    break;

                case CTProtocolCommandKind.ReplaceNodeByCodeConstant:
                    {
                        var removeeID = command.Arguments[CTReplaceNodeByCodeConstantCommand.RemoveeIDIndex];

                        var pos = ASTHelpers.GetPosition(ancillary.AST, removeeID);

                        if (pos.Index > -1) // [dho] NOTE this implies that it is not possible to replace the root - 14/07/19
                        {
                            // [dho] insertion - 14/07/19
                            {
                                var codeConstant = command.Arguments[CTReplaceNodeByCodeConstantCommand.CodeConstantIndex];

                                // [dho] TODO origin! - 14/07/19
                                ASTHelpers.Replace(ancillary.AST, removeeID, new[] {
                                NodeFactory.CodeConstant(ancillary.AST, pos.Node.Origin, codeConstant).Node
                            });
                            }

                            // // [dho] removal - 14/07/19
                            // {
                            //     var removeeID = command.Arguments[CTReplaceNodeByCodeConstantCommand.RemoveeIDIndex];

                            //     ASTHelpers.RemoveNodes(ast, new string[] { removeeID });
                            // }

                            break;
                        }

                        // [dho] NOTE no longer treating the inability to find a node as an error because of 
                        // the case where you have a #run inside an #if, which would mean the #if is deleted 
                        // before the #run asks to be deleted - 03/05/19
                    }
                    break;


                case CTProtocolCommandKind.InsertImmediateSiblingAndFromValueAndDeleteNode:
                    {
                        var insertionPointID = command.Arguments[CTInsertImmediateSiblingFromValueAndDeleteNodeCommand.InsertionPointIDIndex];

                        var pos = ASTHelpers.GetPosition(ancillary.AST, insertionPointID);

                        if (pos.Index > -1) // [dho] NOTE this implies that it is not possible to replace the root - 14/05/19
                        {
                            // [dho] insertion - 18/05/19
                            {
                                var type = command.Arguments[CTInsertImmediateSiblingFromValueAndDeleteNodeCommand.TypeIndex];
                                var value = command.Arguments[CTInsertImmediateSiblingFromValueAndDeleteNodeCommand.ValueIndex];

                                if (type == "java.lang.Integer") // [dho] TODO make this language agnostic!!! - 14/05/19
                                {
                                    // [dho] TODO origin! - 23/04/19
                                    var replacement = NodeFactory.NumericConstant(ancillary.AST, pos.Node.Origin, value);

                                    ASTHelpers.Connect(ancillary.AST, pos.Parent.ID, new[] { replacement.Node }, pos.Role, pos.Index);
                                }
                                else
                                {
                                    result.AddMessages(
                                        new Message(MessageKind.Error, $"Failed to replace node because type is not supported : '{type}'")
                                    );
                                }
                            }

                            // [dho] removal - 18/05/19
                            {
                                var removeeID = command.Arguments[CTInsertImmediateSiblingFromValueAndDeleteNodeCommand.RemoveeIDIndex];

                                ASTHelpers.RemoveNodes(ancillary.AST, new string[] { removeeID });
                            }

                            break;
                        }

                        // [dho] NOTE no longer treating the inability to find a node as an error because of 
                        // the case where you have a #run inside an #if, which would mean the #if is deleted 
                        // before the #run asks to be deleted - 03/05/19
                    }
                    break;

                case CTProtocolCommandKind.InsertImmediateSiblingAndDeleteNode:
                    {
                        var insertionPoint = command.Arguments[CTInsertImmediateSiblingAndDeleteNodeCommand.InsertionPointIDIndex];

                        var pos = ASTHelpers.GetPosition(ancillary.AST, insertionPoint);

                        if (pos.Node != null)
                        {
                            System.Diagnostics.Debug.Assert(pos.Parent != null);

                            // [dho] insertion - 18/05/19
                            {
                                var insertee = ASTHelpers.GetNode(ancillary.AST, command.Arguments[CTInsertImmediateSiblingAndDeleteNodeCommand.InserteeIDIndex]);

                                if (insertee != null)
                                {
                                    ASTHelpers.Connect(ancillary.AST, pos.Parent.ID, new[] { insertee }, pos.Role, pos.Index);


                                }
                            }

                            // [dho] removal - 18/05/19
                            {
                                var removeeID = command.Arguments[CTInsertImmediateSiblingAndDeleteNodeCommand.RemoveeIDIndex];

                                ASTHelpers.RemoveNodes(ancillary.AST, new string[] { removeeID });
                            }
                        }
                    }
                    break;

                case CTProtocolCommandKind.IllegalBridgeDirectiveNode:
                    {
                        var nodeID = command.Arguments[CTProtocolIllegalBridgeDirectiveNodeCommand.NodeIDIndex];
                        var node = ASTHelpers.GetNode(ancillary.AST, nodeID);

                        if (node != null)
                        {
                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"Cannot bridge call to artifact at compile time, because it has not been compiled yet!", node)
                                {
                                    Hint = GetHint(node.Origin),
                                // Tags = DiagnosticTags
                            }
                            );
                        }
                    }
                    break;

                default:
                    result.AddMessages(
                        new Message(MessageKind.Error, $"Could not parse compile time command from message")
                    );
                    break;
            }

            return result;
        }

        static Result<object> AddAncillarySources(Session session, Artifact artifact, Ancillary ancillary, ISourceProvider sourceProvider, DuplexSocketServer server, string baseDirPath, string[] patterns, CancellationToken token)
        {
            var result = new Result<object>();

            List<SourceFilePatternMatchInput> inputs = new List<SourceFilePatternMatchInput>();

            // [dho] should be resillient to the case where the input path is a directory or regex - 06/05/19
            for (int i = 0; i < patterns.Length; ++i)
            {
                var pattern = patterns[i];
                var @base = pattern.StartsWith(".") ? baseDirPath : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                inputs.Add(new SourceFilePatternMatchInput(@base, pattern));
            }

            if (HasErrors(result) || token.IsCancellationRequested) return result;

            var parsedSources = result.AddMessages(
                ParseNewSources(inputs, session, artifact, ancillary.AST, sourceProvider, server, token)
            );

            if (parsedSources.TotalPaths?.Count == 0)
            {
                var pathList = new List<string>();

                foreach (var input in inputs)
                {
                    pathList.Add($"\"{input.BaseDirPath}/{input.SearchPattern}\"");
                }

                result.AddMessages(
                    new Message(MessageKind.Error, $"Could not find file(s) at specified path(s) : {string.Join(",", pathList)}")
                );
            }

            if (HasErrors(result) || token.IsCancellationRequested) return result;


            try
            {
                var task = ProcessArtifact(session, artifact, ancillary, parsedSources.NewComponents, sourceProvider, server, token);

                task.Wait();

                result.AddMessages(task.Result);
            }
            catch (Exception e)
            {
                result.AddMessages(CreateErrorFromException(e));
            }

            return result;
        }

        static int IndexOfImageAsset(IList<Asset> assets, AssetRole role, string needleName)
        {
            for (int i = 0; i < assets.Count; ++i)
            {
                var asset = assets[i];

                if(asset.Role == role && (asset as ImageAssetSet).Name == needleName)
                {
                    return i;
                }
            }

            return -1;
        }

        struct ParseNewSourceFilesResult
        {
            public List<string> TotalPaths;
            public List<Component> NewComponents;
        }

        private static Result<ParseNewSourceFilesResult> ParseNewSources(IEnumerable<SourceFilePatternMatchInput> inputs, Session session, Artifact artifact, RawAST ast, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<ParseNewSourceFilesResult>();

            var componentNames = GetComponentNames(ast);
            SourceFileFilterDelegate filter = path => System.Array.IndexOf(componentNames, path) == -1;

            var parsedPaths = result.AddMessages(FilterNewSourceFilePaths(session, ast, inputs, filter, token));

            var totalPaths = parsedPaths.TotalPaths;
            var newPaths = parsedPaths.NewPaths;
            var newComponents = default(List<Component>);

            // [dho] parse components and add them to the tree - 14/05/19
            if (newPaths?.Count > 0)
            {
                var parser = new Sempiler.Parsing.PolyParser();

                newComponents = result.AddMessages(
                    Sempiler.CompilerHelpers.Parse(parser, session, ast, sourceProvider, newPaths, token)
                );
            }
            else
            {
                newComponents = new List<Component>();
            }

            result.Value = new ParseNewSourceFilesResult
            {
                TotalPaths = totalPaths,
                NewComponents = newComponents
            };

            return result;
        }

        struct FilterNewSourceFilePathsResult
        {
            public List<string> TotalPaths;
            public List<string> NewPaths;
            public List<ISourceFile> NewSourceFiles;
        }


        private static string[] GetComponentNames(RawAST ast)
        {
            var root = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

            string[] componentNames = default(string[]);
            {
                var domain = ASTNodeFactory.Domain(ast, root);
                var componentNodes = domain.Components;

                componentNames = new string[componentNodes.Length];

                for (int i = 0; i < componentNames.Length; ++i)
                {
                    componentNames[i] = ASTNodeFactory.Component(ast, (DataNode<string>)componentNodes[i]).Name;
                }
            }

            return componentNames;
        }

        delegate bool SourceFileFilterDelegate(string path);

        private static Result<FilterNewSourceFilePathsResult> FilterNewSourceFilePaths(Session session, RawAST ast, IEnumerable<SourceFilePatternMatchInput> inputs, SourceFileFilterDelegate filter, CancellationToken token)
        {
            var result = new Result<FilterNewSourceFilePathsResult>();


            var totalPaths = new List<string>();
            var newPaths = new List<string>();
            var newSourceFiles = new List<ISourceFile>();

            // [dho] set this in any case to protect calling code from `NullPointerException` - 29/09/19
            result.Value = new FilterNewSourceFilePathsResult
            {
                TotalPaths = totalPaths,
                NewPaths = newPaths,
                NewSourceFiles = newSourceFiles
            };


            // [dho] dedup the components based on ones already in the tree 
            // (in the case where the code has tried to add the same source multiple times) - 14/05/19
            {
                var sourceFiles = result.AddMessages(SourceHelpers.EnumerateSourceFilePatternMatches(inputs));

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                // [dho] filter out any paths we have already parsed before - 06/05/19
                foreach (var sourceFile in sourceFiles)
                {
                    // [dho] assumes we always name the component with the abs path to the file - 06/05/19
                    var sourceFilePath = sourceFile.GetPathString();

                    // [dho] ignore hidden crappy files on macOS - 28/07/19
                    if (Path.GetExtension(sourceFilePath) == ".ds_store")
                    {
                        continue;
                    }

                    if (filter(sourceFilePath))
                    {
                        newPaths.Add(sourceFilePath);
                        newSourceFiles.Add(sourceFile);
                    }

                    totalPaths.Add(sourceFilePath);
                }
            }


            return result;
        }

        private static async Task<Result<Dictionary<string, OutFile>>> BundleAndWriteArtifact(Session session, Artifact artifact, CancellationToken token)
        {
            var result = new Result<Dictionary<string, OutFile>>();

            var bundler = default(IBundler);

            // [dho] instantiate the appropriate packager implementation - 21/05/19
            {
                if (artifact.TargetPlatform == ArtifactTargetPlatform.Android)
                {
                    bundler = new AndroidBundler();
                }
                else if (artifact.TargetPlatform == ArtifactTargetPlatform.IOS ||
                    artifact.TargetPlatform == ArtifactTargetPlatform.SwiftUI)
                {
                    bundler = new IOSBundler();
                }
                else if (artifact.TargetPlatform == ArtifactTargetPlatform.ZeitNow)
                {
                    bundler = new ZeitNowBundler();
                }
                else if (artifact.TargetPlatform == ArtifactTargetPlatform.FirebaseFunctions)
                {
                    bundler = new FirebaseFunctionsBundler();
                }
                else
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"No bundler implementation exists for target language '{artifact.TargetLang}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                    );

                    return result;
                }
            }

            var outFileCollection = result.AddMessages(CompilerHelpers.Bundle(bundler, session, artifact, token));

            if (HasErrors(result) || token.IsCancellationRequested) return result;


            // [dho] write the package to disk - 21/05/19
            {
                var absOutDirPath = FileSystem.Resolve(session.BaseDirectory.ToPathString(), $"{InferredConfig.OutDirName}/{artifact.Name}/");

                // [dho] first implementation of being able to keep some directories between builds to save time,
                // such as the Pods that get downloaded and installed for an iOS project - these can be large dependencies
                // so nuking that folder and reinstalling them for every build is time consuming and frustrating - 04/11/19
                // [dho] TODO optimize deletions - 04/10/19
                var preservedRelPaths = bundler.GetPreservedDebugEmissionRelPaths();

                if (preservedRelPaths.Count > 0)
                {
                    // [dho] subdirectories - 04/10/19
                    if (Directory.Exists(absOutDirPath))
                    {
                        foreach (var absItemPath in Directory.EnumerateDirectories(absOutDirPath))
                        {
                            result.AddMessages(await DeletePathIfNotPreserved(artifact, preservedRelPaths, absOutDirPath, absItemPath));
                        }

                        // [dho] files - 04/10/19                    
                        foreach (var absItemPath in Directory.GetFiles(absOutDirPath, "*.*", SearchOption.AllDirectories))
                        {
                            result.AddMessages(await DeletePathIfNotPreserved(artifact, preservedRelPaths, absOutDirPath, absItemPath));
                        }
                    }

                }
                else
                {
                    result.AddMessages(await FileSystem.Delete(new string[] { absOutDirPath }));
                }

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                var filesWritten = result.AddMessages(await FileSystem.Write(absOutDirPath, outFileCollection, token));

                result.Value = filesWritten;
            }


            return result;
        }
        private static async Task<Result<object>> DeletePathIfNotPreserved(Artifact artifact, IList<string> preservedRelPaths, string absBaseDirPath, string absItemPath)
        {
            var result = new Result<object>();

            var relItemPath = absItemPath.Substring(absBaseDirPath.Length + 1);

            foreach (var preservedRelPath in preservedRelPaths)
            {
                if (relItemPath == preservedRelPath)
                {
                    result.AddMessages(new Message(MessageKind.Info, $"Compiler is preserving '{artifact.Name}' item '{relItemPath}' from previous build"));
                    return result;
                }
                else if (relItemPath.IndexOf(preservedRelPath) == 0) // [dho] child path - 04/11/19
                {
                    return result;
                }
            }

            result.AddMessages(await FileSystem.Delete(new string[] { absItemPath }));

            return result;
        }
    }


    public static class MetaProgramming
    {
        public struct MetaProgrammingInfo
        {
            public List<Directive> BridgeIntentDirectives;
            public List<Directive> CTDirectives;
        }

        public static MetaProgrammingInfo GetMetaProgrammingInfo(Session session, RawAST ast, Node node, CancellationToken token)
        {
            var mpInfo = new MetaProgrammingInfo
            {
                BridgeIntentDirectives = new List<Directive>(),
                CTDirectives = new List<Directive>()
            };

            PopulateMetaProgrammingInfo(session, ast, node, token, ref mpInfo);

            return mpInfo;
        }

        public static MetaProgrammingInfo GetMetaProgrammingInfo(Session session, RawAST ast, IEnumerable<ASTNode> roots, CancellationToken token)
        {
            var mpInfo = new MetaProgrammingInfo
            {
                BridgeIntentDirectives = new List<Directive>(),
                CTDirectives = new List<Directive>()
            };

            foreach (var nodeWrapper in roots)
            {
                if (token.IsCancellationRequested) break;

                PopulateMetaProgrammingInfo(session, ast, nodeWrapper.Node, token, ref mpInfo);
            }

            return mpInfo;
        }

        private static void PopulateMetaProgrammingInfo(Session session, RawAST ast, Node node, CancellationToken token, ref MetaProgrammingInfo mpInfo)
        {
            if (node.Kind == SemanticKind.Directive)
            {
                var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

                var name = directive.Name;

                // [dho] TODO store directive names centrally somewhere!! -15/05/19
                switch (name)
                {
                    case CTDirective.CodeGen: // [dho] compile time code generation - 15/05/19
                    case CTDirective.CodeExec: // [dho] compile time execution - 15/05/19
                        {
                            mpInfo.CTDirectives.Add(directive);
                        }
                        break;

                    default:
                        {
                            // [dho] bridged usage of another artifact in the session - 15/05/19
                            if (session.Artifacts.ContainsKey(name))
                            {
                                mpInfo.BridgeIntentDirectives.Add(directive);
                                // [dho] we don't need to examine the children because they are descendants
                                // of an artifact reference, which will be moved/transformed and then recursively
                                // processed in the context of the artifact it will reside in - 29/05/19
                                return;
                            }
                        }
                        break;
                }
            }

            foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, node.ID))
            {
                if (token.IsCancellationRequested) break;

                PopulateMetaProgrammingInfo(session, ast, child, token, ref mpInfo);
            }
        }
    }

    public static class Bridging
    {
        ///<param name="sourceArtifactName">The artifact in which the bridge intent was discovered</param>
        public static Result<object> ProcessBridgeIntent(Session session, string sourceArtifactName, ASTNode bridgeIntent, CancellationToken token)
        {
            var result = new Result<object>();

            var directive = bridgeIntent as Directive;

            // [dho] TODO CLEANUP restriction for now, though we may want to be agnostic of a particular construct - 30/05/19
            if (directive == null)
            {
                result.AddMessages(new Message(MessageKind.Error, $"Expected a Directive representing Bridge Intent, but given {bridgeIntent.Kind}"));
                return result;
            }

            // [dho] the artifact we need to move the bridge subject to - 30/05/19
            var targetArtifactName = directive.Name; // [dho] eg. 'server' in `#server` - 30/05/19

            // [dho] the expression we need to transfer over to the target artifact AST - 30/05/19
            var targetExp = directive.Subject;


            {
                var sourceArtifact = session.Artifacts[sourceArtifactName];

                switch (sourceArtifact.TargetPlatform)
                {
                    // Android, Java


                }


                var targetArtifact = session.Artifacts[targetArtifactName];

                switch (targetArtifact.TargetPlatform)
                {
                    // Now, TypeScript


                    // after we move directive.Subject
                    // call DiscoverAndProcessBridgeIntents(movedNode)

                    /*
                        module.exports = (req, res) => {
                            res.end('Hello, World');
                        }
                    
                     */



                    // THINK ABOUT THIS... HOW DO WE RESOLVE THE FUNCTIONS THAT ARE BEING REFERENCED... WE ALSO NEED
                    // TO PARSE OUT THE ARGUMENTS:


                    // var x = #server foo(myVariable);

                    // we need to serialize myVariable and parse it back

                    // payload : [myVariable]

                    // no arguments yet

                    // #server hello();

                    /*
                        1. we need to inject hooks that will allow us to execute some code without needing to resolve
                        the paths of identifiers







                        Inside the target artifact, add a global thing called :

                            Dictionary<string, Lambda>() Hooks;


                        to simulate being able to call that code from that place


                            Hooks[nodeID] = () => expression;


                        then the exposed lambda will be:

                            module.exports = (req, res) => Hooks["somehardcodedvalue"](...)







                        if(LanguageHelpers.JavaScriptTreatsAsValue(sourceNode))
                        {
                            // invocation with a result
                            module.exports = (req, res) => res.json(some.path.to.hello())
                        }
                        else
                        {
                            // invocation without a result
                            module.exports = (req, res) => { some.path.to.hello(); }
                        }

                    
                    
                     */

                }


                // NodeFactory.BridgeFunctionDeclaration(name, parameters, type, body)

                // NodeFactory.BridgeInvocation(name, arguments)

                // replace the source with a bridgecall

                // NOTE DO WE NEED TO CALL REGISTER NODE RECURSIVELY ON THE SUBTREE......
                // should have a move function that does that implicitly really



            }


            /*
                move directive.Subject to target AST (eg. new component)?

                        replace the call site with a BridgeCall(target, newNodeID)

                        zz(newNodeID) // recurse in context of new home
            
             */

            return result;
        }

        private static Result<object> DiscoverAndProcessBridgeIntents(Session session, string artifactName, ASTNode start, CancellationToken token)
        {
            var result = new Result<object>();

            var mpInfo = MetaProgramming.GetMetaProgrammingInfo(session, start.AST, start.Node, token);

            // [dho] NOTE assumption is that bridging happens later in the pipeline than CTExec - 30/05/19
            if (mpInfo.CTDirectives.Count > 0)
            {
                result.AddMessages(
                    new Message(MessageKind.Error, $"Expected all CTExec code to have been processed before bridging, but found {mpInfo.CTDirectives.Count}")
                );

                return result;
            }

            foreach (var bridgeIntent in mpInfo.BridgeIntentDirectives)
            {
                result.AddMessages(ProcessBridgeIntent(session, artifactName, bridgeIntent, token));
            }

            return result;
        }
    }
}