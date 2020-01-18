
namespace Sempiler.Core
{
    using Sempiler.AST;
    using Sempiler.Diagnostics;
    using Sempiler.Emission;
    using Sempiler.Bundling;
    using Sempiler.CTExec;
    using AST.Diagnostics;
    using static AST.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
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

            var ctExecID = CompilerHelpers.NextInternalGUID();

            var ctExecInfo = new CTExecInfo
            {
                ID = ctExecID,
                OutDirectory = FileSystem.ParseDirectoryLocation(
                    FileSystem.Resolve(/* session.BaseDirectory.ToPathString() */"./compiler", $"./{ctExecID}/")
                ),
                FilesWritten = new Dictionary<string, OutFile>(),
                ComponentIDsEmitted = new Dictionary<string, bool>(),
            };

            var session = new Session
            {
                Start = DateTime.UtcNow,
                BaseDirectory = baseDirectory,
                InputPaths = inputPaths,
                Server = server,
                ComponentCache = new Dictionary<string, Component>(),
                Artifacts = new Dictionary<string, Artifact>(),
                Shards = new Dictionary<string, List<Shard>>(),
                CTExecInfo = ctExecInfo,
                FilesWritten = new Dictionary<string, Dictionary<string, OutFile>>()
            };


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
            // FRONTEND
            if (!HasErrors(result) && !token.IsCancellationRequested)
            {
                var task = __FrontEnd(session, inputPaths, sourceProvider, server, token);

                try
                {
                    task.Wait();

                    result.AddMessages(task.Result);
                }
                catch(Exception e)
                {
                    result.AddMessages(CreateErrorFromException(e));
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // BACKEND
            if (!HasErrors(result) && !token.IsCancellationRequested)
            {
                result.AddMessages(
                    __BackEnd(session, server, token)
                );
            }


            session.End = DateTime.UtcNow;

            result.Value = session;

            return result;
        }




        private static async Task<Result<object>> __FrontEnd(Session session, IEnumerable<string> inputPaths, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<object>();

            var parseTimer = CompilerHelpers.StartTimer("PARSING PHASE");

            // [dho] NOTE CT exec writes diagnostics directly into `result.Messages` asynchronously as it
            // processes commands, so we need to ensure the collection is initialized - 25/11/19
            result.Messages = result.Messages ?? new MessageCollection();

            result.AddMessages(
                CTExecHelpers.InitializeRunTime(session, sourceProvider, server, result.Messages, token)
            );

            if (!HasErrors(result) && !token.IsCancellationRequested)
            {
                var (compilerArtifact, compilerShard, _) = CompilerHelpers.CreateArtifact(
                    session, ArtifactRole.Compiler, string.Empty, ArtifactTargetLang.TypeScript, string.Empty
                );

                var parser = new Sempiler.Parsing.PolyParser();

                var seedComponents = result.AddMessages(
                    Sempiler.CompilerHelpers.Parse(parser, session, compilerShard.AST, sourceProvider, inputPaths, token)
                );

                SessionHelpers.CacheComponents(session, seedComponents, token);

                // [dho] processing artifact and any compile time directives - 21/05/19
                {
                    // [dho] TODO CLEANUP this transformation logic should be called by source files, not assumed by the compiler!! - 25/11/19
                    var resultAST = result.AddMessages(
                        await CompilerHelpers.PerformLegacyTransformations(session, compilerArtifact, compilerShard, seedComponents, sourceProvider, server, token)
                    );


                    // lock(result)
                    // {

                    System.Diagnostics.Debug.Assert(resultAST == null || resultAST == compilerShard.AST, "AST has unexpectedly changed");
                    // }

                    // [dho] NOTE `break` because we have some clean up of CT exec below - 26/11/19
                }

                CompilerHelpers.StopTimer(parseTimer);
                CompilerHelpers.PrintElapsed(parseTimer);

                if (!HasErrors(result) && !token.IsCancellationRequested)
                {
                    result.AddMessages(
                        await __Middle(session, compilerArtifact, compilerShard, seedComponents, sourceProvider, server, token)
                    );
                }
            }
            else
            {
                CompilerHelpers.StopTimer(parseTimer);
                CompilerHelpers.PrintElapsed(parseTimer);
            }

            result.AddMessages(
                CTExecHelpers.DestroyRunTime(session, server, token)
            );

            return result;
        }

        private async static Task<Result<object>> __Middle(Session session, Artifact artifact, Shard mainAppShard, List<Component> seedComponents, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<object>();

            var ctExecTimer = CompilerHelpers.StartTimer("CT EXEC PHASE");
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
                            ParseNewSources(inputs, session, artifact, shard.AST, sourceProvider, server, token)
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
                                shard.Resources.AddRange(inferredResources);
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


                var absOutDirPath = session.CTExecInfo.OutDirectory.ToPathString();

                {
                    var ofc = new OutFileCollection();

                    var initFilePaths = result.AddMessages(
                        CTExecHelpers.AddSessionCTExecSourceFiles(session, artifact, mainAppShard, seedComponents, ofc, token)
                    );

                    if (HasErrors(result) || token.IsCancellationRequested)
                    {
                        CompilerHelpers.StopTimer(ctExecTimer);
                        CompilerHelpers.PrintElapsed(ctExecTimer);
                        return result;
                    }

                    var filesWritten = result.AddMessages(
                        await FileSystem.Write(absOutDirPath, ofc, token)
                    );
                
                    if (HasErrors(result) || token.IsCancellationRequested)
                    {
                        CompilerHelpers.StopTimer(ctExecTimer);
                        CompilerHelpers.PrintElapsed(ctExecTimer);
                        return result;
                    }


                    {
                        foreach(var kv in filesWritten)
                        {
                            session.CTExecInfo.FilesWritten.Add(kv.Key, kv.Value);
                        }
                    }


                    var arguments = $"\"{absOutDirPath}/" + String.Join($"\" \"{absOutDirPath}/", initFilePaths) + "\"";

                    var proc = session.CTExecInfo.Process = new System.Diagnostics.Process()
                    {
                        StartInfo = {
                            FileName = "node",
                            Arguments = arguments,
                            RedirectStandardInput = true,
                            // RedirectStandardError = true,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        },
                        EnableRaisingEvents = false
                    };

                    try
                    {
                        if (proc.Start())
                        {
                            // [dho] NOTE CTExecHandler will kill process once it receives a terminate
                            // message - 25/11/19
                            proc.WaitForExit();
                        }
                        else
                        {
                            result.AddMessages(new Message(MessageKind.Error, $"Could not start CT Exec process for file '{arguments}'"));
                        }
                    }
                    catch (Exception e) // [dho] eg. throws if fileName is not known - 21/11/19
                    {
                        try
                        {
                            proc.Kill();
                        }
                        catch
                        {
                        }

                        result.AddMessages(CreateErrorFromException(e));
                    }

                    // lock (session.CTExecInfo.Processes)
                    // {
                        // session.CTExecInfo.Processes.Remove(artifact.Name);
                    // }

                    session.CTExecInfo.Process = null;
                }

            }
            catch (Exception e)
            {
                // lock (result)
                // {
                result.AddMessages(CreateErrorFromException(e));
                // }

                // [dho] NOTE `break` because we have some clean up of CT exec below - 26/11/19

            }

            CompilerHelpers.StopTimer(ctExecTimer);
            CompilerHelpers.PrintElapsed(ctExecTimer);

            return result;
        }

        private static Result<object> __BackEnd(Session session, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<object>();

            var backEndTimer = CompilerHelpers.StartTimer("BACK END PHASE");

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
                if (HasErrors(result) || token.IsCancellationRequested)
                {
                    CompilerHelpers.StopTimer(backEndTimer);
                    CompilerHelpers.PrintElapsed(backEndTimer);
                    return result;
                }

                // var l = new object();

                foreach (var kv in session.Artifacts)
                {
                    var artifact = kv.Value;

                    if(artifact.Role == ArtifactRole.Compiler) continue;

                    try
                    {
                        var task = BundleAndWriteArtifact(session, artifact, token);

                        task.Wait();

                        // lock (session.FilesWritten)
                        // {
                        //     lock(result)
                        //     {
                        session.FilesWritten[artifact.Name] = result.AddMessages(task.Result);
                        //      }
                        // }

                        if (HasErrors(result) || token.IsCancellationRequested) break;
                    }
                    catch (Exception e)
                    {
                        // lock (result)
                        // {
                        result.AddMessages(CreateErrorFromException(e));
                        // }
                        break;
                    }
                }


                // Parallel.ForEach(session.Artifacts, (kv, state) =>
                // {

                //     var artifact = kv.Value;

                //     try
                //     {
                //         var task = BundleAndWriteArtifact(session, artifact, token);

                //         task.Wait();

                //         lock (session.FilesWritten)
                //         {
                //             lock(result)
                //             {
                //                 session.FilesWritten[artifact.Name] = result.AddMessages(task.Result);
                //             }
                //         }

                //         if (HasErrors(result) || token.IsCancellationRequested) return;
                //     }
                //     catch (Exception e)
                //     {
                //         lock (result)
                //         {
                //             result.AddMessages(CreateErrorFromException(e));
                //         }
                //     }
                // });
            }

            CompilerHelpers.StopTimer(backEndTimer);
            CompilerHelpers.PrintElapsed(backEndTimer);

            return result;
        }

        // private static Result<Dictionary<string, Artifact>> ParseArtifactsX(AST.RawAST ast, CancellationToken token)
        // {
        //     var result = new Result<Dictionary<string, Artifact>>();

        //     var artifacts = result.Value = new Dictionary<string, Artifact>();

        //     var root = ASTHelpers.GetRoot(ast);

        //     System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

        //     var domain = ASTNodeFactory.Domain(ast, root);

        //     foreach (var component in domain.Components)
        //     {
        //         foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(domain.AST, component.ID))
        //         {
        //             var r = MaybeParseArtifactDeclaration(domain.AST, child);

        //             result.AddMessages(r);

        //             if (!HasErrors(r) && r.Value.Item1 /* is artifact decl */)
        //             {
        //                 var artifact = r.Value.Item2;

        //                 if (artifacts.ContainsKey(artifact.Name))
        //                 {
        //                     result.AddMessages(new NodeMessage(MessageKind.Error, $"Duplicate artifact declarations with name : '{artifact.Name}'", child)
        //                     {
        //                         Hint = GetHint(child.Origin),
        //                         // Tags = DiagnosticTags
        //                     });
        //                 }
        //                 else
        //                 {
        //                     artifacts[artifact.Name] = artifact;
        //                 }
        //             }
        //         }
        //     }

        //     return result;
        // }

        // // [dho] this is a bit painful!! Statically parsing out artifact declarations - 06/05/19
        // private static Result<(bool, Artifact)> MaybeParseArtifactDeclarationX(RawAST ast, Node node)
        // {
        //     var result = new Result<(bool, Artifact)>()
        //     {
        //         Value = (false, default(Artifact))
        //     };

        //     if (node.Kind == SemanticKind.Directive)
        //     {
        //         var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

        //         if (directive.Name == CTDirective.CodeExec &&
        //             directive.Subject?.Kind == SemanticKind.Invocation)
        //         {
        //             var invocation = ASTNodeFactory.Invocation(directive.AST, directive.Subject);

        //             if (invocation.Subject?.Kind == SemanticKind.Identifier)
        //             {
        //                 var name = ASTNodeFactory.Identifier(invocation.AST, (DataNode<string>)invocation.Subject);

        //                 if (ASTNodeHelpers.GetLexeme(name) == CTAPISymbols.Build)
        //                 {
        //                     var artifactName = default(string);
        //                     var artifactRole = default(ArtifactRole);
        //                     var artifactTargetLang = default(string);
        //                     var artifactTargetPlatform = default(string);

        //                     var template = invocation.Template;

        //                     if (template.Length > 0)
        //                     {
        //                         var templateMarker = template[0];

        //                         result.AddMessages(new NodeMessage(MessageKind.Error, $"Artifact declaration should not be templated", templateMarker)
        //                         {
        //                             Hint = GetHint(templateMarker.Origin),
        //                             // Tags = DiagnosticTags
        //                         });
        //                     }

        //                     var args = invocation.Arguments;

        //                     if (args.Length != 3)
        //                     {
        //                         result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected 3 arguments", invocation)
        //                         {
        //                             Hint = GetHint(invocation.Origin),
        //                             // Tags = DiagnosticTags
        //                         });
        //                     }

        //                     for (int i = 0; i < args.Length; ++i)
        //                     {
        //                         var arg = args[i];

        //                         if (arg.Kind == SemanticKind.StringConstant)
        //                         {
        //                             var str = ASTNodeFactory.StringConstant(invocation.AST, (DataNode<string>)arg);

        //                             if (String.IsNullOrEmpty(str.Value))
        //                             {
        //                                 result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected non empty string constant", arg)
        //                                 {
        //                                     Hint = GetHint(arg.Origin),
        //                                     // Tags = DiagnosticTags
        //                                 });

        //                                 continue;
        //                             }

        //                             switch (i)
        //                             {
        //                                 case 0:
        //                                     artifactName = str.Value;
        //                                     break;

        //                                 case 1:
        //                                     artifactTargetLang = str.Value;
        //                                     break;

        //                                 case 2:
        //                                     artifactTargetPlatform = str.Value;
        //                                     break;
        //                             }
        //                         }
        //                         else
        //                         {
        //                             result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected non empty string constant", arg)
        //                             {
        //                                 Hint = GetHint(arg.Origin),
        //                                 // Tags = DiagnosticTags
        //                             });
        //                         }
        //                     }

        //                     // [dho] infer artifact role from target platform - 21/05/19
        //                     {
        //                         switch (artifactTargetPlatform)
        //                         {
        //                             case ArtifactTargetPlatform.Node:
        //                             case ArtifactTargetPlatform.FirebaseFunctions:
        //                             case ArtifactTargetPlatform.AWSLambda:
        //                             case ArtifactTargetPlatform.ZeitNow:
        //                                 artifactRole = ArtifactRole.Server;
        //                                 break;

        //                             case ArtifactTargetPlatform.Android:
        //                             case ArtifactTargetPlatform.IOS:
        //                             case ArtifactTargetPlatform.SwiftUI:
        //                             case ArtifactTargetPlatform.WebBrowser:
        //                                 artifactRole = ArtifactRole.Client;
        //                                 break;

        //                             default:
        //                                 if (artifactTargetLang == ArtifactTargetLang.SQL)
        //                                 {
        //                                     artifactRole = ArtifactRole.Database;
        //                                 }
        //                                 else
        //                                 {
        //                                     result.AddMessages(
        //                                         new Message(MessageKind.Error, $"Could not infer artifact role for '{artifactName}' from target language '{artifactTargetLang}' and target platform '{artifactTargetPlatform}'")
        //                                     );
        //                                 }
        //                                 break;
        //                         }
        //                     }


        //                     if (!HasErrors(result))
        //                     {
        //                         ASTHelpers.DisableNodes(directive.AST, directive.ID);
        //                         // if(!ASTHelpers.DeregisterNode(runDirective.AST, runDirective.Node))
        //                         // {
        //                         //     result.AddMessages(new NodeMessage(MessageKind.Error, $"Could not remove artifact declaration from AST", runDirective)
        //                         //     {
        //                         //         Hint = GetHint(runDirective.Origin),
        //                         //         // Tags = DiagnosticTags
        //                         //     });
        //                         // }
        //                         var artifact = new Artifact(artifactRole, artifactName, artifactTargetLang, artifactTargetPlatform);

        //                         result.Value = (true, artifact);
        //                     }
        //                 }
        //             }
        //         }
        //     }

        //     return result;
        // }

        // private static async Task<Result<object>> RunArtifactCTExecX(Session session, Artifact artifact, Shard mainAppShard, List<Component> seedComponents,/*,  ISourceProvider sourceProvider, DuplexSocketServer server,*/ CancellationToken token)
        // {
        //     var result = new Result<object>();

        //     var ofc = new OutFileCollection();

        //     var initFilePaths = result.AddMessages(
        //         CTExecHelpers.AddShardCTExecSourceFiles(session, artifact, mainAppShard, seedComponents, ofc, token)
        //     );

        //     if (HasErrors(result) || token.IsCancellationRequested) return result;


        //     var absOutDirPath = session.CTExecInfo.OutDirectory.ToPathString();

        //     result.AddMessages(
        //         await FileSystem.Write(absOutDirPath, ofc, token)
        //     );


        //     if (HasErrors(result) || token.IsCancellationRequested) return result;


        //     var arguments = $"\"{absOutDirPath}/" + String.Join($"\" \"{absOutDirPath}/", initFilePaths) + "\"";

        //     var proc = new System.Diagnostics.Process()
        //     {
        //         StartInfo = {
        //             FileName = "node",
        //             Arguments = arguments,
        //             RedirectStandardInput = true,
        //             // RedirectStandardError = true,
        //             CreateNoWindow = true,
        //             UseShellExecute = false
        //         },
        //         EnableRaisingEvents = false
        //     };

        //     lock (session.CTExecInfo.Processes)
        //     {
        //         session.CTExecInfo.Processes[artifact.Name] = proc;
        //     }

        //     try
        //     {
        //         if (proc.Start())
        //         {
        //             // [dho] NOTE CTExecHandler will kill process once it receives a terminate
        //             // message - 25/11/19
        //             proc.WaitForExit();
        //         }
        //         else
        //         {
        //             result.AddMessages(new Message(MessageKind.Error, $"Could not start CT Exec process for file '{arguments}'"));
        //         }
        //     }
        //     catch (Exception e) // [dho] eg. throws if fileName is not known - 21/11/19
        //     {
        //         try
        //         {
        //             proc.Kill();
        //         }
        //         catch
        //         {
        //         }

        //         result.AddMessages(CreateErrorFromException(e));
        //     }

        //     lock (session.CTExecInfo.Processes)
        //     {
        //         session.CTExecInfo.Processes.Remove(artifact.Name);
        //     }

        //     return result;
        // }




        // static object ctExecLock = new object();
        // static Dictionary<string, CancellationTokenSource> CTExecServerHandlerCTSCache = new Dictionary<string, CancellationTokenSource>();
        // static object MessageHistoryLock = new object();
        // static Dictionary<string, bool> MessageHistoryCache = new Dictionary<string, bool>();
        // private static async Task<Result<object>> CTExec_____REFACTOR_AND_DELETE(Session session, Artifact artifact, Shard shard, List<Component> newlyAddedComponents, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        // {
        //     var result = new Result<object>();

        //     var filesWritten = default(Dictionary<string, OutFile>);

        //     var ctid = GenerateCTID();

        //     var absCTIDDirPath = FileSystem.Resolve(/* session.BaseDirectory.ToPathString() */"./compiler", $"./{ctid}/");

        //     var outDirLocation = FileSystem.ParseDirectoryLocation(absCTIDDirPath);


        //     // if(artifact.TargetLang == ArtifactTargetLang.Java)
        //     // {
        //     //     // [dho] use our special compile time emitter - 12/04/19
        //     //     var emitter = new CTJavaEmitter(ctid, server.IPAddress, server.Port, outDirLocation);

        //     //     {
        //     //         var filePathsToInit = new string[newlyAddedComponents.Count];

        //     //         for(int i = 0; i < filePathsToInit.Length; ++i) filePathsToInit[i] = newlyAddedComponents[i].Name;

        //     //         // [dho] make sure we only evaluate directives in these specific files - 06/05/19
        //     //         emitter.FilePathsToInit = filePathsToInit;
        //     //     }

        //     //     var outFileCollection = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ast, token));

        //     //     if(HasErrors(result) || token.IsCancellationRequested) return result;

        //     //     filesWritten = result.AddMessages(await FileSystem.Write(absCTIDDirPath, outFileCollection, token));

        //     //     consumer = new Sempiler.Consumption.CommandLineConsumer
        //     //     {
        //     //         Name = "CTJava",
        //     //         // [dho] NOT using `ArtifactTargetLang` constant because this is a filename,
        //     //         // not an internal identifier representing the target platform, and would
        //     //         // break unnecessarily if we change the value of `ArtifactTargetLang.Java` - 21/05/19
        //     //         FileName = "java",
        //     //         ParseDiagnostics = true,
        //     //         DiagnosticsParser = CommandLineDiagnosticsParser.JavaC, 
        //     //         InjectVariables = false,
        //     //         Arguments = filesWritten.Keys
        //     //     };
        //     // }
        //     // else
        //     {


        //         var task = new CTExecTypeScriptBundler().Bundle(session, artifact, new List<Shard>() { shard }, token);

        //         task.Wait();

        //         var outFileCollection = result.AddMessages(task.Result);

        //         if (HasErrors(result) || token.IsCancellationRequested) return result;

        //         filesWritten = result.AddMessages(await FileSystem.Write(absCTIDDirPath, outFileCollection, token));
        //     }


        //     if (HasErrors(result)) return result;

        //     if (!token.IsCancellationRequested)
        //     {
        //         CancellationTokenSource consumptionCTS;
        //         CancellationTokenRegistration ctr = default(CancellationTokenRegistration);
        //         bool ownsCTS;

        //         lock (ctExecLock)
        //         {
        //             if (CTExecServerHandlerCTSCache.ContainsKey(artifact.Name))
        //             {
        //                 consumptionCTS = CTExecServerHandlerCTSCache[artifact.Name];
        //                 ownsCTS = false;
        //             }
        //             else
        //             {
        //                 consumptionCTS = CTExecServerHandlerCTSCache[artifact.Name] = new CancellationTokenSource();
        //                 ctr = token.Register(() => consumptionCTS.Cancel());
        //                 ownsCTS = true;
        //             }
        //         }

        //         result.AddMessages(
        //             ZXZZXZZ(session, artifact, sourceProvider, server, filesWritten, consumptionCTS.Token)
        //         );

        //         if(ownsCTS)
        //         {
        //             lock (ctExecLock)
        //             {
        //                 ctr.Dispose();
        //                 consumptionCTS.Dispose();
        //                 System.Diagnostics.Debug.Assert(CTExecServerHandlerCTSCache.Remove(artifact.Name));
        //             }
        //         }
        //     }

        //     // [dho] delete the build files we created - 06/05/19
        //     {
        //         var deleteFilesWrittenTask = FileSystem.Delete(filesWritten.Keys);

        //         var deleteCTIDDirTask = FileSystem.Delete(new string[] { absCTIDDirPath });

        //         result.AddMessages(await deleteFilesWrittenTask);
        //         result.AddMessages(await deleteCTIDDirTask);
        //     }

        //     return result;



        // }







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

            var bundleTimer = CompilerHelpers.StartTimer("BUNDLING");
            var outFileCollection = result.AddMessages(CompilerHelpers.Bundle(bundler, session, artifact, token));

            CompilerHelpers.StopTimer(bundleTimer);
            CompilerHelpers.PrintElapsed(bundleTimer);

            if (HasErrors(result) || token.IsCancellationRequested) return result;

            var writeTimer = CompilerHelpers.StartTimer("WRITING");
            // [dho] write the package to disk - 21/05/19
            {
                var absOutDirPath = FileSystem.Resolve(session.BaseDirectory.ToPathString(), $"{InferredConfig.OutDirName}/{artifact.Name}/");

                // [dho] first implementation of being able to keep some directories between builds to save time,
                // such as the Pods that get downloaded and installed for an iOS project - these can be large dependencies
                // so nuking that folder and reinstalling them for every build is time consuming and frustrating - 04/11/19
                // [dho] TODO optimize deletions - 04/10/19
                var preservedRelPaths = bundler.GetPreservedDebugEmissionRelPaths(session, artifact, token);

                if (preservedRelPaths.Count > 0)
                {
                    // [dho] subdirectories - 04/10/19
                    if (Directory.Exists(absOutDirPath))
                    {
                        {
                            var absPathsToDelete = new List<string>();

                            foreach (var absItemPath in Directory.GetDirectories(absOutDirPath, "*.*", SearchOption.AllDirectories))
                            {
                                var shouldDelete = result.AddMessages(ShouldDeleteDirectory(artifact, preservedRelPaths, absOutDirPath, absItemPath));

                                if(shouldDelete)
                                {
                                    absPathsToDelete.Add(absItemPath);
                                }
                            }

                            // [dho] delete directories before looping for files to save us checking if 
                            // we need to preserve file paths unnecessarily below - 31/12/19
                            if(absPathsToDelete.Count > 0)
                            {
                                result.AddMessages(
                                    await FileSystem.Delete(absPathsToDelete)
                                );
                            }
                        }

                        {
                            var absPathsToDelete = new List<string>();

                            // [dho] files - 04/10/19                    
                            foreach (var absFilePath in Directory.GetFiles(absOutDirPath, "*.*", SearchOption.AllDirectories))
                            {
                                var shouldDelete = result.AddMessages(ShouldDeleteFile(artifact, preservedRelPaths, absOutDirPath, absFilePath));

                                if(shouldDelete)
                                {
                                    absPathsToDelete.Add(absFilePath);
                                }
                            }

                            if(absPathsToDelete.Count > 0)
                            {
                                result.AddMessages(
                                    await FileSystem.Delete(absPathsToDelete)
                                );
                            }
                        }
                    }

                }
                else
                {
                    result.AddMessages(await FileSystem.Delete(new string[] { absOutDirPath }));
                }

                if (HasErrors(result) || token.IsCancellationRequested)
                {
                    CompilerHelpers.StopTimer(writeTimer);
                    CompilerHelpers.PrintElapsed(writeTimer);
                    return result;
                }

                var filesWritten = result.AddMessages(await FileSystem.Write(absOutDirPath, outFileCollection, token));

                result.Value = filesWritten;
            }

            CompilerHelpers.StopTimer(writeTimer);
            CompilerHelpers.PrintElapsed(writeTimer);

            return result;
        }

        private static Result<bool> ShouldDeleteDirectory(Artifact artifact, IList<string> preservedRelPaths, string absBaseDirPath, string absDirPath)
        {
            var result = new Result<bool>
            {
                Value = true
            };

            var relDirPath = absDirPath.Substring(absBaseDirPath.Length + 1);

            foreach (var preservedRelPath in preservedRelPaths)
            {
                if (relDirPath == preservedRelPath)
                {
                    result.AddMessages(new Message(MessageKind.Info, $"Compiler is preserving '{artifact.Name}' directory '{absDirPath}' from previous build"));
                    
                    result.Value = false;
                    
                    break;
                }
                // [dho] the relative item path is a child of a reserved path - 04/11/19
                else if (preservedRelPath.IndexOf(relDirPath) == 0)
                {
                    result.Value = false;

                    break;
                }
            }

            return result;
        }

        private static Result<bool> ShouldDeleteFile(Artifact artifact, IList<string> preservedRelPaths, string absBaseDirPath, string absFilePath)
        {
            var result = new Result<bool>
            {
                Value = true
            };

            var relFilePath = absFilePath.Substring(absBaseDirPath.Length + 1);

            foreach (var preservedRelPath in preservedRelPaths)
            {
                if (relFilePath == preservedRelPath)
                {
                    result.AddMessages(new Message(MessageKind.Info, $"Compiler is preserving '{artifact.Name}' file '{absFilePath}' from previous build"));
                    
                    result.Value = false;
                    
                    break;
                }
            }

            return result;
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