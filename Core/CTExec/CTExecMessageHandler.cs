using System;
using Sempiler.Core;
using Sempiler.AST;
using Sempiler.Diagnostics;
using Sempiler.Emission;
using Sempiler.Languages;
using Sempiler.AST.Diagnostics;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using static Sempiler.SourceHelpers;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace Sempiler.CTExec
{
    using static CTExecHelpers;
    public static class CTExecMessageHandlerHelpers
    {
        public static DuplexSocketServer.OnMessageDelegate CreateMessageHandler(Session session, ISourceProvider sourceProvider, DuplexSocketServer server, MessageCollection diagnostics, CancellationToken token)
        {
            // AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
            // DuplexSocketServer.OnMessageDelegate handler = null;
            // System.Diagnostics.Process proc = null;
            // System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            // System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            // stopwatch.Start();

            DuplexSocketServer.OnMessageDelegate handler = (socket, message) =>
            {
                // if(stopwatch.IsRunning)
                // {
                //     stopwatch.Stop();
                //     var ts = stopwatch.Elapsed;

                //     string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                //         ts.Hours, ts.Minutes, ts.Seconds,
                //         ts.Milliseconds / 10);

                //     Console.WriteLine("\n\n🔥🔥🔥🔥 Interval between commands took " + elapsedTime + "\n\n");

                //     stopwatch.Reset();               
                // }

                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                var command = CTProtocolHelpers.ParseCommand(message);

                // // [dho] this message was meant for a different subscriber - 06/05/19
                // if (command.ArtifactName != artifact.Name) return;


                // var isNewMessage = true;

                // lock (MessageHistoryLock)
                // {
                //     isNewMessage = !MessageHistoryCache.ContainsKey(command.MessageID);

                //     MessageHistoryCache[command.MessageID] = true; // has definitively been seen now in any case, even if wasn't new this time - 22/11/19
                // }

                // string serializedResponseData = default(string);



                // if (isNewMessage)
                // {
                    Result<string> r;

                    

                    var artifact = session.Artifacts[command.ArtifactName];
                    var shard = session.Shards[artifact.Name][command.ShardIndex];
                    var proc = session.CTExecInfo.Process;

                    // Console.WriteLine("COMMAND KIND " + command.Kind  + ", MESSAGE ID " + command.MessageID + ", SHARD " + shard.Name);
                    
        
                    if(command.Kind == CTProtocolCommandKind.Terminate)
                    {
                        // server.Send(socket, $"{{ \"ok\": {isOK.ToString().ToLower()}, \"id\": \"{command.MessageID}\", \"data\": null }}");
                        // server.Kill(socket);

                        var statusCode = System.Int32.Parse(command.Arguments[CTProtocolTerminateCommand.StatusCodeIndex]);
                        
                        var isOK = statusCode == 0;

                        if(!isOK)
                        {
                            var errorMessage = command.Arguments[CTProtocolTerminateCommand.MessageIndex];
                            var filePath = command.Arguments[CTProtocolTerminateCommand.FilePathIndex];
                            var lineNumberStart = System.Int32.Parse(command.Arguments[CTProtocolTerminateCommand.LineNumberStartIndex]);
                            var columnIndexStart = System.Int32.Parse(command.Arguments[CTProtocolTerminateCommand.ColumnIndexStartIndex]);

                            // [dho] TODO FIX this does not work because the markers refer to where the Node was created in the original source file,
                            // when it should be relative to the position in the CT Exec generated file.. so currently we do not find the correct marker when we 
                            // use the position that is reported from the Node process - 12/12/19
                            var closestNode = default(Node);//CompilerHelpers.GetClosestNode(filePath, lineNumberStart, columnIndexStart, session.CTExecInfo.FilesWritten);
            

                            var error = new Message(MessageKind.Error, errorMessage)
                            {
                                Hint = closestNode != null ? GetHint(closestNode.Origin) : null
                            };

                            lock(diagnostics)
                            {
                                diagnostics.Add(error);
                            }
                        }

                        server.Send(socket, $"{{ \"ok\": true, \"id\": \"{command.MessageID}\", \"data\": null }}");

                        // [dho] TODO FIX HACK where the Node process does not exit for some reason - 28/02/20
                        System.Threading.Tasks.Task.Delay(3000).ContinueWith(task => { 
                            if(!proc.HasExited)
                            {
                                Console.WriteLine("Force killing CT Exec Process after waiting for it to exit itself");
                                proc.Kill();
                            }
                        });
                    }
                    else
                    {
                        // [dho] NOTE no lock on ast because the caller provides a fresh clone to manipulate - 14/05/19
                        r = HandleNonTerminalServerMessage(session, command, artifact, shard, sourceProvider, server, token);

                        lock(diagnostics)
                        {
                            diagnostics.AddAll(r.Messages);
                        }

                        bool isOK = !HasErrors(r);

                        if(isOK)
                        {
                            server.Send(socket, $"{{ \"ok\": {isOK.ToString().ToLower()}, \"id\": \"{command.MessageID}\", \"data\": {r.Value ?? "null"} }}");
                        }
                        else 
                        {
                            server.Kill(socket);
                            proc.Kill();
                        }
                    }

                    stopwatch.Stop();
                    var ts = stopwatch.Elapsed;

                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);

                    Console.WriteLine("\n\n>>> Processing Command '" + artifact.Name + "->" + shard.Name + "->" + command.Kind + "' took " + elapsedTime);
                    // Console.WriteLine("\n\n>>> Processing Command '" + artifact.Name + "->" + shard.Name + "->" + command.Kind);

                    // stopwatch.Start();
                // }
                
            };

                // server.OnMessage += handler;

                // var arguments = "\"" + String.Join("\" \"", filesWritten.Keys) + "\"";

                // proc = new System.Diagnostics.Process()
                // {
                //     StartInfo = {
                //         FileName = "node",
                //         Arguments = "\"" + String.Join("\" \"", filesWritten.Keys) + "\"",
                //         RedirectStandardInput = true,
                //         // RedirectStandardError = true,
                //         CreateNoWindow = true,
                //         UseShellExecute = false
                //     },
                //     EnableRaisingEvents = false
                // };


                // try
                // {
                //     stopwatch.Start();
                //     if(!proc.Start())
                //     {
                //         result.AddMessages(new Message(MessageKind.Error, $"Could not start CT Exec process for file '{arguments}'"));
                //     }
                // }
                // catch(Exception e) // [dho] eg. throws if fileName is not known - 21/11/19
                // {
                //     result.AddMessages(CreateErrorFromException(e));
                //     stopWaitHandle.Set();
                // }
            // }

            // stopWaitHandle.WaitOne();

            // if(handler != null)
            // {
            //     server.OnMessage -= handler;
            // }

            // if(proc != null)
            // {
            //     stopwatch.Stop();
            //     var ts = stopwatch.Elapsed;

            //     string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //         ts.Hours, ts.Minutes, ts.Seconds,
            //         ts.Milliseconds / 10);

            //     Console.WriteLine("CT Exec Run Time " + elapsedTime);

            //     try
            //     {
            //         // StreamReader myStreamReader = proc.StandardError;
            //         // // Read the standard error of net.exe and write it on to console.
            //         // var err = myStreamReader.ReadLine();
                
            //         proc.Kill();
            //     }
            //     catch(Exception e)
            //     {
            //         result.AddMessages(CreateErrorFromException(e));
            //     }
            // }

            // cts.Dispose();

            return handler;
        }

        private static Result<string> HandleNonTerminalServerMessage(Session session, CTProtocolCommand command, Artifact artifact, Shard shard, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<string>();

            switch (command.Kind)
            {
                case CTProtocolCommandKind.AddCapability:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot add capability outside of an artifact"));
                            return result;
                        }

                        var name = command.Arguments[CTProtocolAddCapabilityCommand.NameIndex];
                        var type = (ConfigurationPrimitive)Enum.Parse(typeof(ConfigurationPrimitive), command.Arguments[CTProtocolAddCapabilityCommand.TypeIndex]);
                        // var values = new string[command.Arguments.Length - 2];

                        int valueStartIndex = CTProtocolAddCapabilityCommand.TypeIndex + 1;
                        object value = result.AddMessages(DeserializeConfigurationPrimitiveValue(type, command.Arguments, valueStartIndex));

                        shard.Capabilities.Add(
                            new Capability
                            {
                                Name = name,
                                Value = value
                            }
                        );
                    }
                    break;

                case CTProtocolCommandKind.AddManifestEntry:
                {
                    if(artifact.Role == ArtifactRole.Compiler)
                    {
                        result.AddMessages(new Message(MessageKind.Error, "Cannot add config value outside of an artifact"));
                        return result;
                    }

                    var path = command.Arguments[CTProtocolAddManifestEntryCommand.PathIndex];
                    var type = (ConfigurationPrimitive)Enum.Parse(typeof(ConfigurationPrimitive), command.Arguments[CTProtocolAddManifestEntryCommand.TypeIndex]);
                    
                    int valueStartIndex = CTProtocolAddManifestEntryCommand.TypeIndex + 1;
                    object value = result.AddMessages(DeserializeConfigurationPrimitiveValue(type, command.Arguments, valueStartIndex));

                    shard.ManifestEntries.Add(new ManifestEntry { 
                        Path = path.Split('/'), 
                        Value = value 
                    });
                }
                break;

                case CTProtocolCommandKind.AddDependency:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot add dependency outside of an artifact"));
                            return result;
                        }

                        var name = command.Arguments[CTProtocolAddDependencyCommand.NameIndex];
                        var version = command.Arguments.Length > CTProtocolAddDependencyCommand.VersionIndex ? command.Arguments[CTProtocolAddDependencyCommand.VersionIndex] : null;
                        var packageManager = command.Arguments.Length > CTProtocolAddDependencyCommand.PackageManagerIndex ? command.Arguments[CTProtocolAddDependencyCommand.PackageManagerIndex] : null;
                        var url = command.Arguments.Length > CTProtocolAddDependencyCommand.URLIndex ? command.Arguments[CTProtocolAddDependencyCommand.URLIndex] : null;
                        
                        shard.Dependencies.Add(
                            new Dependency
                            {
                                Name = name,
                                Version = version,
                                PackageManager = packageManager,
                                URL = url
                            }
                        );
                    
                    }
                    break;

                case CTProtocolCommandKind.AddEntitlement:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot add entitlement outside of an artifact"));
                            return result;
                        }

                        var name = command.Arguments[CTProtocolAddEntitlementCommand.NameIndex];
                        var type = (ConfigurationPrimitive)Enum.Parse(typeof(ConfigurationPrimitive), command.Arguments[CTProtocolAddEntitlementCommand.TypeIndex]);
                        
                        int valueStartIndex = CTProtocolAddEntitlementCommand.TypeIndex + 1;
                        object value = result.AddMessages(DeserializeConfigurationPrimitiveValue(type, command.Arguments, valueStartIndex));


                        shard.Entitlements.Add(
                            new Entitlement
                            {
                                Name = name,
                                Value = value
                            }
                        );
                    }
                    break;

                case CTProtocolCommandKind.AddPermission:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot add permission outside of an artifact"));
                            return result;
                        }

                        var name = command.Arguments[CTProtocolAddPermissionCommand.NameIndex];
                        var description = command.Arguments[CTProtocolAddPermissionCommand.DescriptionIndex];

                        shard.Permissions.Add(
                            new Permission
                            {
                                Name = name,
                                Description = description
                            }
                        );
                    }
                    break;

                case CTProtocolCommandKind.IsArtifactName:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot perform query outside of an artifact"));
                            return result;
                        }

                        var name = command.Arguments[CTProtocolIsArtifactNameCommand.ArtifactNameIndex];
                        
                        result.Value = artifact.Name == name ? "true" : "false";
                    }
                    break;

                case CTProtocolCommandKind.IsTargetLanguage:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot perform query outside of an artifact"));
                            return result;
                        }

                        var lang = command.Arguments[CTProtocolIsTargetLanguageCommand.LanguageNameIndex];
                        
                        result.Value = artifact.TargetLang == lang ? "true" : "false";
                    }
                    break;

                case CTProtocolCommandKind.IsTargetPlatform:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot perform query outside of an artifact"));
                            return result;
                        }

                        var platform = command.Arguments[CTProtocolIsTargetPlatformCommand.PlatformNameIndex];
                        
                        result.Value = artifact.Name == platform ? "true" : "false";
                    }
                    break;

                case CTProtocolCommandKind.DeleteNode:
                    {
                        var nodeID = command.Arguments[CTProtocolDeleteNodeCommand.NodeIDIndex];

                        // Console.WriteLine("🦷 🦷 🦷 DeleteNode > nodeID : " + nodeID + ", shard : " + shard.Name + ", ast ID : " + shard.AST.ID);

                        var node = ASTHelpers.GetNode(shard.AST, nodeID);

                        System.Diagnostics.Debug.Assert(node != null);
                        // System.Diagnostics.Debug.Assert(
                        //     ASTHelpers.Contains(shard.AST, ASTHelpers.GetRoot(shard.AST), 
                        //     node.ID, token)
                        // );
                        // if (node != null)
                        // {

                            ASTHelpers.DisableNodes(shard.AST, node.ID);

                            // System.Diagnostics.Debug.Assert(
                            //     !ASTHelpers.Contains(shard.AST, ASTHelpers.GetRoot(shard.AST), 
                            //     node.ID, token)
                            // );
                            // result.Value = ("Deleted node " + node.ID);
                            break;
                            // if (ASTHelpers.DeregisterNode(ast, node))
                            // {
                            //     result.Value = ("Deleted node " + node.ID);
                            //     break;
                            // }
                        // }
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

                case CTProtocolCommandKind.AddArtifact: // #run add_sources("foo.ts", "hello-world.ts") 
                    {
                        var name = command.Arguments[CTProtocolAddArtifactCommand.NameIndex];
                        var targetLanguage = command.Arguments[CTProtocolAddArtifactCommand.LanguageNameIndex];
                        var targetPlatform = command.Arguments[CTProtocolAddArtifactCommand.PlatformNameIndex];
                        ArtifactRole role = default(ArtifactRole);

                        if(session.Artifacts.ContainsKey(name))
                        {
                            result.AddMessages(
                                new Message(MessageKind.Error, $"Artifact '{name}' could not be created because it already exists'")
                            );
                        }

                        {
                            switch (targetPlatform)
                            {
                                case ArtifactTargetPlatform.NodeJSExpress:
                                case ArtifactTargetPlatform.FirebaseFunctions:
                                case ArtifactTargetPlatform.AWSLambda:
                                // case ArtifactTargetPlatform.AWSLambdaExpress:
                                case ArtifactTargetPlatform.ZeitNow:
                                    role = ArtifactRole.Server;
                                    break;

                                case ArtifactTargetPlatform.Android:
                                case ArtifactTargetPlatform.IOS:
                                case ArtifactTargetPlatform.IPhone:
                                case ArtifactTargetPlatform.IPad:
                                // case ArtifactTargetPlatform.SwiftUI:
                                case ArtifactTargetPlatform.WebBrowser:
                                    role = ArtifactRole.Client;
                                    break;

                                default:
                                    if (targetLanguage == ArtifactTargetLang.SQL)
                                    {
                                        role = ArtifactRole.Database;
                                    }
                                    else
                                    {
                                        result.AddMessages(
                                            new Message(MessageKind.Error, $"Could not infer artifact role for '{name}' from target language '{targetLanguage}' and target platform '{targetPlatform}'")
                                        );
                                    }
                                    break;
                            }
                        }

                        if(HasErrors(result) || token.IsCancellationRequested) return result;

                        var baseDirPath = command.Arguments[CTProtocolAddArtifactCommand.BaseDirPathIndex];

                        var sourcePath = command.Arguments[CTProtocolAddArtifactCommand.SourcePathIndex];
                        
                        var absoluteEntryPointPath = FileSystem.Resolve(baseDirPath, sourcePath);

                        var (newArtifact, newShard, newShardIndex) = CompilerHelpers.CreateArtifact(session, role, name, targetLanguage, targetPlatform, absoluteEntryPointPath);
                        

                        var patterns = new string[] { sourcePath };

                        // AddCTExecSourceFilesDelegate addFilesDelegate = AddSourcesCTExecSourceFiles;

                        var (newComponents, newPathsForCTExec) = result.AddMessages(
                            // [dho] NOTE passing in new target! - 16/10/19
                            AddSources(session, newArtifact, newShard, sourceProvider, server, baseDirPath, patterns, /* addFilesDelegate,*/ token)
                        );



                        if(HasErrors(result) || token.IsCancellationRequested) return result;

                    
                        // // [dho] TODO REMOVE!! transformations should be explicitly invoked from user code - 10/12/19 
                        // var resultAST = result.AddMessages(
                        //     CompilerHelpers.PerformLegacyTransformations(session, newArtifact, newShard, newlyAddedComponents, sourceProvider, server, token)
                        // );

                        // System.Diagnostics.Debug.Assert(resultAST == null || resultAST == newShard.AST, "AST has unexpectedly changed");


                        result.Value = $"{{ \"{NewArtifactNameSymbolLexeme}\" : \"{name}\", \"{NewShardIndexSymbolLexeme}\" : \"{newShardIndex}\", \"{NewSourcesSymbolLexeme}\" : {JSONStringify(newPathsForCTExec)} }}";
                        
                    }
                    break;

                case CTProtocolCommandKind.AddShard: // #run add_sources("foo.ts", "hello-world.ts") 
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot add shard outside of an artifact"));
                            return result;
                        }

                        var newShard = default(Shard);
                        var newShardIndex = -1;

                        ShardRole? shardRole = null;

                        var rawShardRole = command.Arguments[CTProtocolAddShardCommand.RoleIndex];

                        switch(rawShardRole)
                        {
                            case "share-extension":
                                shardRole = ShardRole.ShareExtension;
                            break;

                            case "static-site":
                                shardRole = ShardRole.StaticSite;
                            break;

                            default:
                                result.AddMessages(
                                    new Message(MessageKind.Error, $"Unsupported shard role '{rawShardRole}'")
                                );
                            break;
                        }

                        if(HasErrors(result) || token.IsCancellationRequested) return result;
                      
                        System.Diagnostics.Debug.Assert(shardRole.HasValue, "Expected shard role to have been parsed");

                        var baseDirPath = command.Arguments[CTProtocolAddShardCommand.BaseDirPathIndex];
                        var sourcePath = command.Arguments[CTProtocolAddShardCommand.SourcePathIndex];

                        var absoluteEntryPointPath = FileSystem.Resolve(baseDirPath, sourcePath);
                        
                        var newAST = new RawAST();
                        var newDomain = NodeFactory.Domain(newAST, new PhaseNodeOrigin(PhaseKind.Transformation));


                        // [dho] TODO CHECK do we want to clone `shard.AST` here, or use a fresh AST? - 16/10/19
                        newShard = new Shard(shardRole.Value, absoluteEntryPointPath, newAST);

                        var artifactShards = session.Shards[artifact.Name];
                        newShardIndex = artifactShards.Count;

                        artifactShards.Add(newShard);
                    

                        if (!HasErrors(result) && !token.IsCancellationRequested)
                        {
                            var patterns = new string[] { sourcePath };

                            // AddCTExecSourceFilesDelegate addFilesDelegate = AddSourcesCTExecSourceFiles;

                            var (newComponents, newPathsForCTExec) = result.AddMessages(
                                // [dho] NOTE passing in new target! - 16/10/19
                                AddSources(session, artifact, newShard, sourceProvider, server, baseDirPath, patterns, /* addFilesDelegate,*/ token)
                            );

                            if(HasErrors(result) || token.IsCancellationRequested) return result;

                    
                            // // [dho] TODO REMOVE!! transformations should be explicitly invoked from user code - 10/12/19 
                            // var resultAST = result.AddMessages(
                            //     CompilerHelpers.PerformLegacyTransformations(session, artifact, newShard, newlyAddedComponents, sourceProvider, server, token)
                            // );

                            // System.Diagnostics.Debug.Assert(resultAST == null || resultAST == newShard.AST, "AST has unexpectedly changed");


                            
                            result.Value = $"{{ \"{NewShardIndexSymbolLexeme}\" : \"{newShardIndex}\", \"{NewSourcesSymbolLexeme}\" : {JSONStringify(newPathsForCTExec)} }}";
                        }
                    }
                    break;

                case CTProtocolCommandKind.AddSources: // #run add_sources("foo.ts", "hello-world.ts") 
                    {
                        var baseDirPath = command.Arguments[CTProtocolAddSourcesCommand.BaseDirPathIndex];

                        var patternsStartIndex = CTProtocolAddSourcesCommand.BaseDirPathIndex + 1;
                        var patterns = new string[command.Arguments.Length - patternsStartIndex];

                        Array.Copy(command.Arguments, patternsStartIndex, patterns, 0, patterns.Length);

                        // AddCTExecSourceFilesDelegate addFilesDelegate = AddSourcesCTExecSourceFiles;


                        var (newComponents, newPathsForCTExec)  = result.AddMessages(
                            AddSources(session, artifact, shard, sourceProvider, server, baseDirPath, patterns, /* addFilesDelegate,*/ token)
                        );

                        if(newPathsForCTExec != null)
                        {
                            // [dho] JSON string array - 24/11/19
                            result.Value = JSONStringify(newPathsForCTExec);
                        }
                    }
                    break;

                case CTProtocolCommandKind.AddAsset:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot add asset outside of an artifact"));
                            return result;
                        }

                        var baseDirPath = command.Arguments[CTProtocolAddAssetCommand.BaseDirPathIndex];

                        var rawRole = command.Arguments[CTProtocolAddAssetCommand.RoleIndex];
                        AssetRole role = AssetRole.None;

                        switch(rawRole)
                        {
                            case "splash":{
                                role = AssetRole.Splash;
                            }
                            break;

                            case "none":{
                                role = AssetRole.None;
                            }
                            break;

                            case "app-icon":{
                                role = AssetRole.AppIcon;
                            }
                            break;

                            case "font":{
                                role = AssetRole.Font;
                            }
                            break;

                            case "image":{
                                role = AssetRole.Image;
                            }
                            break;

                            default:{
                                result.AddMessages(
                                    new Message(MessageKind.Error, $"Unsupported asset role '{rawRole}'")
                                );
                            }
                            break;
                        }


                        if (!HasErrors(result) && !token.IsCancellationRequested)
                        {
                            var sourcePath = command.Arguments[CTProtocolAddAssetCommand.SourcePathIndex];

                            if(role == AssetRole.Splash)
                            {
                                var parts = sourcePath.Split('?');
                                sourcePath = parts[0];

                                List<SourceFilePatternMatchInput> inputs = new List<SourceFilePatternMatchInput>()
                                {
                                    new SourceFilePatternMatchInput(baseDirPath, sourcePath)
                                };

                                // [dho] TODO filter - 08/11/19
                                SourceFileFilterDelegate filter = path => true;
                            
                                var parsedPaths = result.AddMessages(
                                    FilterNewSourceFilePaths(session, shard.AST, inputs, filter, token)
                                );

                                if(parsedPaths.NewSourceFiles.Count == 0)
                                {
                                    result.AddMessages(new Message(MessageKind.Error, "No files found at splash asset path"));
                                }

                                if (HasErrors(result) || token.IsCancellationRequested) return result;

                                ImageAssetMember? image = null, imageX2 = null, imageX3 = null;

                                foreach(var sf in parsedPaths.NewSourceFiles)
                                {
                                    var meta = ParseImageAssetMemberMeta(sf);
                                    var img = new ImageAssetMember {
                                        Source = sf,
                                        Size = meta.Size,
                                        Scale = meta.Scale
                                    };

                                    switch(meta.Scale)
                                    {
                                        case "3x":
                                            imageX3 = img;
                                        break;

                                        case "2x":
                                            imageX2 = img;
                                        break;

                                        default:
                                        case "1x":
                                            image = img;
                                        break;
                                    }
                                }


                                var backgroundRGB = new int[] { 0, 0, 0 };
                                int? width = null;
                                int? height = null;

                                // [dho] background color after 'anchor', 
                                // eg. `some/file/here.png?bg=#222324` - 03/03/20
                                if(parts.Length == 2)
                                {
                                    foreach(var kv in parts[1].Split('&'))
                                    {
                                        var keyAndValue = kv.Split('=');

                                        switch(keyAndValue[0])
                                        {
                                            case "bg":
                                            {
                                                var hex = keyAndValue[1];
                                                var hexR = hex.Substring(1, 2);
                                                var hexG = hex.Substring(3, 2);
                                                var hexB = hex.Substring(5, 2);

                                                backgroundRGB[0] = int.Parse(hexR, System.Globalization.NumberStyles.HexNumber);
                                                backgroundRGB[1] = int.Parse(hexG, System.Globalization.NumberStyles.HexNumber);
                                                backgroundRGB[2] = int.Parse(hexB, System.Globalization.NumberStyles.HexNumber);
                                            }
                                            break;

                                            case "width":
                                            {
                                                width = int.Parse(keyAndValue[1]);
                                            }
                                            break;

                                            case "height":
                                            {
                                                height = int.Parse(keyAndValue[1]);
                                            }
                                            break;
                                        }
                                    }
                                }


                                imageX2 = imageX2 ?? image;
                                imageX3 = imageX3 ?? imageX2;

                                var splashAsset = new SplashAsset 
                                {
                                    BackgroundRGB = backgroundRGB,
                                    Image = image.Value,
                                    ImageX2 = imageX2.Value,
                                    ImageX3 = imageX3.Value,
                                    Width = width,
                                    Height = height,
                                    Role = role,
                                };

                                shard.Assets.Add(splashAsset);
                            
                            }
                            else
                            {
                                List<SourceFilePatternMatchInput> inputs = new List<SourceFilePatternMatchInput>()
                                {
                                    new SourceFilePatternMatchInput(baseDirPath, sourcePath)
                                };

                                // [dho] TODO filter - 08/11/19
                                SourceFileFilterDelegate filter = path => true;
                            
                                var parsedPaths = result.AddMessages(
                                    FilterNewSourceFilePaths(session, shard.AST, inputs, filter, token)
                                );

                                if (HasErrors(result) || token.IsCancellationRequested) return result;

                                
                                if(role == AssetRole.AppIcon || role == AssetRole.Image)
                                {
                                    AddImageAssets(shard, role, parsedPaths.NewSourceFiles);
                                }
                                else if(role == AssetRole.Font)
                                {
                                    foreach (var sf in parsedPaths.NewSourceFiles)
                                    {
                                        string name = Path.GetFileNameWithoutExtension(sf.GetPathString());
                                    
                                        var fontAsset = new FontAsset
                                        {
                                            Name = name,
                                            Source = sf,
                                            Role = role
                                        };

                                        shard.Assets.Add(fontAsset);                                
                                    }
                                }
                                else if(role == AssetRole.None)
                                {
                                    var rawAsset = new RawAsset
                                    {
                                        SourcePath = sourcePath,
                                        Files = parsedPaths.NewSourceFiles,
                                        Role = role
                                    };

                                    shard.Assets.Add(rawAsset);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Assert(false, $"Unhandled asset role '{role}'");
                                }
                            }



                        }
                    }
                    break;

                case CTProtocolCommandKind.AddRes:
                    {
                        if(artifact.Role == ArtifactRole.Compiler)
                        {
                            result.AddMessages(new Message(MessageKind.Error, "Cannot add resource outside of an artifact"));
                            return result;
                        }

                        var baseDirPath = command.Arguments[CTProtocolAddResCommand.BaseDirPathIndex];

                        var sourcePath = command.Arguments[CTProtocolAddResCommand.SourcePathIndex];

                        List<SourceFilePatternMatchInput> inputs = new List<SourceFilePatternMatchInput>()
                    {
                        new SourceFilePatternMatchInput(baseDirPath, sourcePath)
                    };

                        // [dho] TODO filter - 08/11/19
                        SourceFileFilterDelegate filter = path => true;

                        var parsedPaths = result.AddMessages(
                            FilterNewSourceFilePaths(session, shard.AST, inputs, filter, token)
                        );

                        if (HasErrors(result) || token.IsCancellationRequested) return result;

                        var newResources = new List<Resource>();

                        var newSourceFiles = parsedPaths.NewSourceFiles;

                        // [dho] user can opt to specify the target file name of the resource - 11/02/20
                        var hasTargetFileName = command.Arguments.Length > CTProtocolAddResCommand.TargetFileNameIndex;

                        var targetFileName = hasTargetFileName ? command.Arguments[CTProtocolAddResCommand.TargetFileNameIndex] : null;

                        if(hasTargetFileName && newSourceFiles.Count > 1)
                        {
                            result.AddMessages(
                                new Message(MessageKind.Error, 
                                    $"Cannot set resource target file name of '{targetFileName}' when adding multiple resources at once"
                                )
                            );

                            return result;
                        }

                        foreach(var source in newSourceFiles)
                        {
                            newResources.Add(new Resource {
                                Source = source,
                                TargetFileName = targetFileName
                            });
                        }

                        shard.Resources.AddRange(newResources);
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

                        var componentNames = ASTHelpers.GetComponentNames(shard.AST);
                        SourceFileFilterDelegate filter = path => System.Array.IndexOf(componentNames, path) == -1;

                        var parsedPaths = result.AddMessages(
                            FilterNewSourceFilePaths(session, shard.AST, inputs, filter, token)
                        );

                        var newPaths = parsedPaths.NewPaths;

                        if (newPaths.Count > 0)
                        {
                            var newComponents = new Node[newPaths.Count];

                            for (int i = 0; i < newPaths.Count; ++i)
                            {
                                var path = newPaths[i];
                                var text = default(string);


                                // Console.WriteLine("🍀🍀🍀 " + path + "🍀🍀🍀");
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

                                var newComponent = NodeFactory.Component(shard.AST, new PhaseNodeOrigin(PhaseKind.Parsing), path);
                                {
                                    ASTHelpers.Connect(shard.AST, newComponent.ID, new[] {
                                    NodeFactory.CodeConstant(shard.AST, new PhaseNodeOrigin(PhaseKind.Parsing), text).Node
                                }, SemanticRole.None);
                                }

                                newComponents[i] = newComponent.Node;
                            }

                            if (HasErrors(result) || token.IsCancellationRequested) return result;


                            var root = ASTHelpers.GetRoot(shard.AST);

                            System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

                            ASTHelpers.Connect(shard.AST, root.ID, newComponents, SemanticRole.Component);
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

                case CTProtocolCommandKind.SetDisplayName:{
                    if(artifact.Role == ArtifactRole.Compiler)
                    {
                        result.AddMessages(new Message(MessageKind.Error, "Cannot set display name outside of an artifact"));
                        return result;
                    }

                    var displayName = command.Arguments[CTProtocolSetDisplayNameCommand.NameIndex];

                    // [dho] TODO validate valid name, lower case letters, no spaces - 16/11/19
                    
                    shard.Name = displayName;
                }
                break;

                case CTProtocolCommandKind.SetTeamName:{

                    if(artifact.Role == ArtifactRole.Compiler)
                    {
                        result.AddMessages(new Message(MessageKind.Error, "Cannot set team name outside of an artifact"));
                        return result;
                    }

                    var teamName = command.Arguments[CTProtocolSetTeamNameCommand.NameIndex];

                    // [dho] TODO validate valid name, lower case letters, no spaces - 16/11/19

                    artifact.TeamName = teamName;
                }
                break;

                case CTProtocolCommandKind.SetVersion:{
                    if(artifact.Role == ArtifactRole.Compiler)
                    {
                        result.AddMessages(new Message(MessageKind.Error, "Cannot set version outside of an artifact"));
                        return result;
                    }


                    var version = command.Arguments[CTProtocolSetVersionCommand.VersionIndex];

                    // [dho] TODO validate valid version - 16/11/19

                    shard.Version = version;
                }
                break;

                case CTProtocolCommandKind.ReplaceNodeByCodeConstant:
                    {
                        var removeeID = command.Arguments[CTReplaceNodeByCodeConstantCommand.RemoveeIDIndex];

                        var pos = ASTHelpers.GetPosition(shard.AST, removeeID);

                        // bool match = false;
                        // bool match2 = false;
                        // foreach (var sh in session.Shards)
                        // {
                        //     foreach(var s in sh.Value)
                        //     {
                        //         if(ASTHelpers.Contains(s.AST, removeeID, token))
                        //         {
                        //             match = true;
                        //             Console.WriteLine("The AST CONTAINS for " + s.Name + " was true");
                        //         }

                        //         if(s.AST.Nodes.ContainsKey(removeeID))
                        //         {
                        //             match2 = true;
                        //             Console.WriteLine("The AST NODES for " + s.Name + " was true");
                        //         }
                        //     }
                        // }

                        // System.Diagnostics.Debug.Assert(match && match2);
                        // System.Diagnostics.Debug.Assert(pos.Index > -1 && pos.Alive);



                        // [dho] NOTE this implies that it is not possible to replace the root - 14/07/19
                        // if (pos.Index > -1)
                        // {
                            // // [dho] TODO FIX messages are being played more than once.. it could be because more than
                            // // one artifact is referencing the same component.. but I haven't looked into whether this
                            // // is the reason, or why it is. Previous CTExec kept a log of which message IDs it had seen to
                            // // avoid replaying messages, but if the reason is multiple artifacts referencing the same component
                            // // then the message ID would only be played for one AST. So I'll just check that the position is 
                            // // alive and ignore the message if not.. because if we try to connect a node that already exists, it will
                            // // disable the node - 30/11/19
                            // if(pos.Alive)
                            // {
                                // [dho] insertion - 14/07/19
                                var codeConstant = command.Arguments[CTReplaceNodeByCodeConstantCommand.CodeConstantIndex];

                                Console.WriteLine("ReplaceNodeByCodeConstant\n" + artifact.Name + " : " + shard.Name + " : " + removeeID + "\n" + codeConstant);

                                // AST, removeeID, codeConstant

                                // [dho] TODO origin! - 14/07/19
                                ASTHelpers.Replace(shard.AST, removeeID, new[] {
                                    NodeFactory.CodeConstant(shard.AST, pos.Node.Origin, codeConstant).Node
                                });
                            // }

                            // // [dho] removal - 14/07/19
                            // {
                            //     var removeeID = command.Arguments[CTReplaceNodeByCodeConstantCommand.RemoveeIDIndex];

                            //     ASTHelpers.RemoveNodes(ast, new string[] { removeeID });
                            // }

                            // break;
                        // }
                        // else
                        // {
                        //     System.Diagnostics.Debug.Assert(shard.AST.Disabled.ContainsKey(removeeID));
                        // }

                        // [dho] NOTE no longer treating the inability to find a node as an error because of 
                        // the case where you have a #run inside an #if, which would mean the #if is deleted 
                        // before the #run asks to be deleted - 03/05/19
                    }
                    break;


                case CTProtocolCommandKind.InsertImmediateSiblingAndFromValueAndDeleteNode:
                    {
                        var insertionPointID = command.Arguments[CTInsertImmediateSiblingFromValueAndDeleteNodeCommand.InsertionPointIDIndex];

                        var pos = ASTHelpers.GetPosition(shard.AST, insertionPointID);

                        System.Diagnostics.Debug.Assert(pos.Index > -1 && pos.Alive);

                        // if (pos.Index > -1) // [dho] NOTE this implies that it is not possible to replace the root - 14/05/19
                        // {
                            // [dho] insertion - 18/05/19
                            {
                                var type = command.Arguments[CTInsertImmediateSiblingFromValueAndDeleteNodeCommand.TypeIndex];
                                var value = command.Arguments[CTInsertImmediateSiblingFromValueAndDeleteNodeCommand.ValueIndex];

                                if (type == "java.lang.Integer") // [dho] TODO make this language agnostic!!! - 14/05/19
                                {
                                    // [dho] TODO origin! - 23/04/19
                                    var replacement = NodeFactory.NumericConstant(shard.AST, pos.Node.Origin, value);

                                    ASTHelpers.Connect(shard.AST, pos.Parent.ID, new[] { replacement.Node }, pos.Role, pos.Index);
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
                                
                                Console.WriteLine("🦷 🦷 🦷 InsertImmediateSiblingAndFromValueAndDeleteNode > insertPointID : " + insertionPointID + ", removeeID " + removeeID + ", shard : " + shard.Name + ", ast ID : " + shard.AST.ID);

                                System.Diagnostics.Debug.Assert(ASTHelpers.GetNode(shard.AST, removeeID) != null ||  shard.AST.Disabled.ContainsKey(removeeID));

                                ASTHelpers.DisableNodes(shard.AST, new string[] { removeeID });


                            }

                            // break;
                        // }
                        // else
                        // {
                        //     System.Diagnostics.Debug.Assert(shard.AST.Disabled.ContainsKey(insertionPointID));
                        // }

                        // [dho] NOTE no longer treating the inability to find a node as an error because of 
                        // the case where you have a #run inside an #if, which would mean the #if is deleted 
                        // before the #run asks to be deleted - 03/05/19
                    }
                    break;

                case CTProtocolCommandKind.InsertImmediateSiblingAndDeleteNode:
                    {
                        var insertionPointID = command.Arguments[CTInsertImmediateSiblingAndDeleteNodeCommand.InsertionPointIDIndex];

                        var pos = ASTHelpers.GetPosition(shard.AST, insertionPointID);
                        
                        System.Diagnostics.Debug.Assert(pos.Index > -1 && pos.Alive);

                        // if (pos.Node != null)
                        // {
                            System.Diagnostics.Debug.Assert(pos.Parent != null);

                            // [dho] insertion - 18/05/19
                            {
                                var insertee = ASTHelpers.GetNode(shard.AST, command.Arguments[CTInsertImmediateSiblingAndDeleteNodeCommand.InserteeIDIndex]);

                                if (insertee != null)
                                {
                                    ASTHelpers.Connect(shard.AST, pos.Parent.ID, new[] { insertee }, pos.Role, pos.Index);


                                }
                            }

                            // [dho] removal - 18/05/19
                            {
                                var removeeID = command.Arguments[CTInsertImmediateSiblingAndDeleteNodeCommand.RemoveeIDIndex];

                                ASTHelpers.DisableNodes(shard.AST, new string[] { removeeID });
                            }
                        // }
                    }
                    break;

                case CTProtocolCommandKind.IllegalBridgeDirectiveNode:
                    {
                        var nodeID = command.Arguments[CTProtocolIllegalBridgeDirectiveNodeCommand.NodeIDIndex];
                        var node = ASTHelpers.GetNode(shard.AST, nodeID);

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

        private static Result<object> DeserializeConfigurationPrimitiveValue(ConfigurationPrimitive type, string[] commandArgs, int valueStartIndex)
        {
            var result = new Result<object>();

            switch(type)
            {
                case ConfigurationPrimitive.String:
                {
                    result.Value = commandArgs[valueStartIndex];
                }
                break;

                case ConfigurationPrimitive.StringArray:
                {
                    var arr = new string[commandArgs.Length - valueStartIndex];

                    Array.Copy(commandArgs, valueStartIndex, arr, 0, arr.Length);

                    result.Value = arr;
                }
                break;

                default:{
                    result.AddMessages(
                        new Message(MessageKind.Error, "No handler configured to deserialize configuration primitive type '" + type + "'")
                    );       
                }
                break;
            }

            return result;
        }

        struct ImageAssetMemberMeta 
        {
            public string Name;
            public string Size;
            public string Scale;
        }

        private static ImageAssetMemberMeta ParseImageAssetMemberMeta(ISourceFile sourceFile)
        {
            string name = Path.GetFileNameWithoutExtension(sourceFile.GetPathString());
            string size = null;
            string scale = null;

            // [dho] NOTE does not support subpixels - 09/11/19
            var rx = new Regex(@"(-[0-9]+x[0-9]+)?(@[0-9]+x)?$", System.Text.RegularExpressions.RegexOptions.Compiled);

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

            return new ImageAssetMemberMeta {
                Name = name,
                Size = size,
                Scale = scale
            };
        }

        private static void AddImageAssets(Shard shard, AssetRole role, List<ISourceFile> sourceFiles)
        {
            foreach (var sf in sourceFiles)
            {
                // Console.WriteLine("🔥🔥🔥🔥");

                var meta = ParseImageAssetMemberMeta(sf);

                var existingImgIndex = IndexOfImageAsset(shard.Assets, role, meta.Name);

                if (existingImgIndex > -1)
                {
                    var imgAssetSet = (ImageAssetSet)shard.Assets[existingImgIndex];
                    
                    imgAssetSet.Images.Add(new ImageAssetMember
                    {
                        Size = meta.Size,
                        Scale = meta.Scale,
                        Source = sf
                    });
                }
                else
                {
                    var imgAssetSet = new ImageAssetSet
                    {
                        Name = meta.Name,
                        Role = role,
                        Images = new List<ImageAssetMember>()
                    };

                    imgAssetSet.Images.Add(new ImageAssetMember
                    {
                        Size = meta.Size,
                        Scale = meta.Scale,
                        Source = sf
                    });

                    shard.Assets.Add(imgAssetSet);
                }
            }
        }

        private static Result<(List<Component>, string[])> AddSources(
            Session session, Artifact artifact, Shard shard, 
            ISourceProvider sourceProvider, DuplexSocketServer server, 
            string baseDirPath, string[] patterns, //AddCTExecSourceFilesDelegate addFilesDelegate, 
            CancellationToken token
        )
        {
            var result = new Result<(List<Component>, string[])>();

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
                ParseNewSources(inputs, session, artifact, shard.AST, sourceProvider, server, token)
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
            {
                try
                {
                    // [dho] TODO CLEANUP this transformation logic should be called by source files, not assumed by the compiler!! - 25/11/19
                    var transformTask = CompilerHelpers.PerformLegacyTransformations(
                        session, artifact, shard, parsedSources.NewComponents, sourceProvider, server, token
                    );

                    transformTask.Wait();

                    lock(result)
                    {
                        var resultAST = result.AddMessages(transformTask.Result);

                        System.Diagnostics.Debug.Assert(resultAST == null || resultAST == shard.AST, "AST has unexpectedly changed");
                    }
                }
                catch (Exception e)
                {
                    lock (result)
                    {
                        result.AddMessages(CreateErrorFromException(e));
                    }
                }
            }

            if (HasErrors(result) || token.IsCancellationRequested) return result;


            var ofc = new OutFileCollection();

            var newFilePaths = result.AddMessages(
                AddSources_AddCTExecSourceFiles(session, artifact, shard, parsedSources.NewComponents, ofc, token)
            );

            if (HasErrors(result) || token.IsCancellationRequested) return result;

            var absOutDirPath = session.CTExecInfo.OutDirectory.ToPathString();

            try
            {
                var task = FileSystem.Write(absOutDirPath, ofc, token);

                // var task = ProcessArtifact(session, artifact, shard, parsedSources.NewComponents, sourceProvider, server, token);

                task.Wait();

                var filesWritten = result.AddMessages(task.Result);
            

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                {
                    foreach(var kv in filesWritten)
                    {
                        session.CTExecInfo.FilesWritten.Add(kv.Key, kv.Value);
                    }
                }

                // string[] keys = new string[filesWritten.Keys.Count];
                // filesWritten.Keys.CopyTo(keys, 0);

                result.Value = (parsedSources.NewComponents, newFilePaths);
            }
            catch (Exception e)
            {
                result.AddMessages(CreateErrorFromException(e));
            }

            return result;
        }

        public static Result<string[]> AddSources_AddCTExecSourceFiles(Session session, Artifact artifact, Shard shard, List<Component> addedSources, OutFileCollection ofc, CancellationToken token)
        {
            /*
                (async () => {
                    require('server-code.js')

                    ....inlined components
                    
                })
            */  

            var result = new Result<string[]>();

            var languageSemantics = LanguageSemantics.Of(artifact.TargetLang);

            if(languageSemantics == null)
            {
                result.AddMessages(new Message(MessageKind.Warning, $"Language semantics not configured for {artifact.TargetLang}, using TypeScript semantics as a fallback"));

                languageSemantics = LanguageSemantics.TypeScript;
            }


            var ctExecShardAST = /*session.CTExecInfo.ASTs[shard.Name]*/shard.AST.Clone();

            result.AddMessages(
                FilterAndTransformAddedSources(session, artifact, shard, ctExecShardAST, languageSemantics, addedSources, ofc, token)
            );

            var emitter = new CTExecEmitter(languageSemantics);

            result.AddMessages(
                EmitASTAndAddOutFiles(session, artifact, shard, ctExecShardAST, emitter, ofc, token)
            );

            var newPaths = new string[addedSources.Count];
            for(int i = 0; i < newPaths.Length; ++i)
            {
                newPaths[i] = "." + System.IO.Path.DirectorySeparatorChar + 
                                emitter.RelativeComponentOutFilePath(session, artifact, shard, addedSources[i]);
            }

            result.Value = newPaths;

            return result;
        }

        private static int IndexOfImageAsset(IList<Asset> assets, AssetRole role, string needleName)
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
    }
}