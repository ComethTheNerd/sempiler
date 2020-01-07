using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sempiler.Core;
using Sempiler.AST;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Emission;
using Sempiler.Bundling;
using Sempiler.Parsing;

namespace Sempiler
{
    public static class CompilerHelpers
    {
        public static string NextInternalGUID()
        {
            return "_" + System.Guid.NewGuid().ToString().Replace('-', '_');
        }

        public struct Timer 
        {
            public string Label;
            public System.Diagnostics.Stopwatch Stopwatch;
        }

        public static Timer StartTimer(string label)
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            stopwatch.Start();

            return new Timer {
                Label = label,
                Stopwatch = stopwatch
            };
        }

        public static void StopTimer(Timer t)
        {
            t.Stopwatch.Stop();
        }

        public static void PrintElapsed(Timer t)
        {
            var ts = t.Stopwatch.Elapsed;

            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);

            Console.WriteLine("⏱  " +  t.Label + " " + elapsedTime);
        }

        public static Result<List<Component>> Parse(IParser parser, Session session, RawAST ast, ISourceProvider sourceProvider, IEnumerable<string> inputPaths, CancellationToken token)
        {
            var result = new Result<List<Component>>
            {
                Value = new List<Component>()
            };

            // RawAST ast = new RawAST();

            Domain domain = default(Domain); 
            
            var currentRoot = ASTHelpers.GetRoot(ast);

            if(currentRoot != null)
            {
                System.Diagnostics.Debug.Assert(currentRoot.Kind == SemanticKind.Domain);

                domain = ASTNodeFactory.Domain(ast, currentRoot);
            }
            else
            {
                domain = Sempiler.AST.NodeFactory.Domain(ast, new PhaseNodeOrigin(PhaseKind.Parsing));
                ASTHelpers.Register(ast, domain.Node);
            }
            
            foreach(var inputPath in inputPaths)
            {
                MessageCollection parseMessages = null;
                var parsed = default(Component[]);

                var source = sourceProvider.ProvideFromPath(inputPath);

                var task = parser.Parse(session, ast, source, token);

                try
                {
                    // SessionHelpers.AddSource(session, source);

                    task.Wait();

                    parseMessages = task.Result.Messages;
                    parsed = task.Result.Value;
                }
                catch (Exception e)
                {
                    parseMessages = new MessageCollection
                    {
                        Errors = new List<Message>
                        {
                            CreateErrorFromException(e)
                        }
                    };
                }

                // lock (l)
                // {
                    // result.AddMessages(sfResult.Messages);

                    result.AddMessages(parseMessages);

                    if (token.IsCancellationRequested)
                    {
                        // state.Break();
                        break;
                    }
                    else if (parsed != null)
                    {
                        // [dho] annoying conversion logic between legacy, agnostic Node and new
                        // ASTNode types - 30/05/19
                        var insertees = new Node[parsed.Length];
                        for(int i = 0; i < insertees.Length; ++i) insertees[i] = parsed[i].Node; 

                        ASTHelpers.Connect(ast, domain.ID, insertees, SemanticRole.Component);

                        result.Value.AddRange(parsed);
                    }
                // }
            }



            // var l = new object();

            // Parallel.ForEach(inputPaths, (inputPath, state) =>
            // {
            //     // if (sfResult.Value != null)
            //     // {
            //         MessageCollection parseMessages = null;
            //         var parsed = default(Component[]);

            //         var source = sourceProvider.ProvideFromPath(inputPath);

            //         var task = parser.Parse(session, ast, source, token);

            //         try
            //         {
            //             // SessionHelpers.AddSource(session, source);

            //             task.Wait();

            //             parseMessages = task.Result.Messages;
            //             parsed = task.Result.Value;
            //         }
            //         catch (Exception e)
            //         {
            //             parseMessages = new MessageCollection
            //             {
            //                 Errors = new List<Message>
            //                 {
            //                     CreateErrorFromException(e)
            //                 }
            //             };
            //         }

            //         lock (l)
            //         {
            //             // result.AddMessages(sfResult.Messages);

            //             result.AddMessages(parseMessages);

            //             if (token.IsCancellationRequested)
            //             {
            //                 state.Break();
            //             }
            //             else if (parsed != null)
            //             {
            //                 // [dho] annoying conversion logic between legacy, agnostic Node and new
            //                 // ASTNode types - 30/05/19
            //                 var insertees = new Node[parsed.Length];
            //                 for(int i = 0; i < insertees.Length; ++i) insertees[i] = parsed[i].Node; 

            //                 ASTHelpers.Connect(ast, domain.ID, insertees, SemanticRole.Component);

            //                 result.Value.AddRange(parsed);
            //             }
            //         }

            //     // }
            //     // else
            //     // {
            //     //     lock (l)
            //     //     {
            //     //         result.AddMessages(sfResult.Messages);
            //     //     }
            //     // }

            // });

            // if (!HasErrors(result))
            // {
            //     result.Value = ast;
            // }

            return result;
        }

        // public static Result<RawAST> Transform(ITransformer transformer, Session session, RawAST ast, CancellationToken token)
        // {
        //     try
        //     {
        //         var task = transformer.Transform(session, ast, token);

        //         task.Wait();

        //         return task.Result;
        //     }
        //     catch (Exception e)
        //     {
        //         var result = new Result<RawAST>();

        //         result.AddMessages(CreateErrorFromException(e));

        //         return result;
        //     }
        // }


        public static Result<OutFileCollection> Bundle(IBundler bundler, Session session, Artifact artifact, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            // var l = new object();

            var outFileCollection = default(OutFileCollection);

            var shards = session.Shards[artifact.Name];

            var task = bundler.Bundle(session, artifact, shards, token);

            var pkgMessages = default(MessageCollection);

            try
            {
                task.Wait();

                pkgMessages = task.Result.Messages;
                outFileCollection = task.Result.Value;
            }
            catch (Exception e)
            {
                pkgMessages = new MessageCollection
                {
                    Errors = new List<Message>
                    {
                        CreateErrorFromException(e)
                    }
                };
            }

            
            result.AddMessages(pkgMessages);


            result.Value = outFileCollection;
    

            return result;
        }

        public static Result<OutFileCollection> Emit(IEmitter emitter, Session session, Artifact artifact, Shard shard, RawAST ast, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            // var l = new object();

            var outFileCollection = default(OutFileCollection);

            var root = ASTHelpers.GetRoot(ast);

            var task = emitter.Emit(session, artifact, shard, ast, root, token);

            var emitMessages = default(MessageCollection);

            try
            {
                task.Wait();

                emitMessages = task.Result.Messages;
                outFileCollection = task.Result.Value;
            }
            catch (Exception e)
            {
                emitMessages = new MessageCollection
                {
                    Errors = new List<Message>
                    {
                        CreateErrorFromException(e)
                    }
                };
            }

            
            result.AddMessages(emitMessages);

           
        

            // Parallel.ForEach(ASTNode.IterateChildren(ast, root.ID), (item, state) =>
            // {
            //     var child = item.Item1;

            //     var task = emitter.Emit(session, artifact, ast, child, token);

            //     MessageCollection emitMessages = null;
            //     OutFileCollection childOutFileCollection = null;

            //     try
            //     {
            //         task.Wait();

            //         emitMessages = task.Result.Messages;
            //         childOutFileCollection = task.Result.Value;
            //     }
            //     catch (Exception e)
            //     {
            //         emitMessages = new MessageCollection
            //         {
            //             Errors = new List<Message>
            //             {
            //                 CreateErrorFromException(e)
            //             }
            //         };
            //     }

            //     lock (l)
            //     {
            //         result.AddMessages(emitMessages);

            //         if (token.IsCancellationRequested)
            //         {
            //             state.Break();
            //         }
            //         else
            //         {
            //             var hasEmitErrors = emitMessages?.Errors?.Count > 0;

            //             if(!hasEmitErrors)
            //             {
            //                 result.AddMessages(OutFileHelpers.Merge(outFileCollection, childOutFileCollection));
            //             }
            //         }
            //     }
            // });

            // if (!HasErrors(result))
            // {
                result.Value = outFileCollection;
            // }

            return result;
        }

        

        // public static Result<object> Consume(IConsumer consumer, Session session, RawAST ast, OutFileCollection outFileCollection, CancellationToken token)
        // {
        //     try
        //     {
        //         var task = consumer.Consume(session, ast, artifact, token);

        //         task.Wait();

        //         return task.Result;
        //     }
        //     catch(Exception e)
        //     {
        //         var result = new Result<object>();

        //         result.AddMessages(CreateErrorFromException(e));

        //         return result;
        //     }
        // }

        public static (Artifact, Shard, int) CreateArtifact(Session session, ArtifactRole role, string name, string targetLanguage, string targetPlatform)
        {
            var newArtifact = new Artifact(role, name, targetLanguage, targetPlatform);

            session.Artifacts[name] = newArtifact;

            var newAST = new RawAST();
            NodeFactory.Domain(newAST, new PhaseNodeOrigin(PhaseKind.Transformation));
            
            var shards = session.Shards[name] = new List<Shard>();

            var newShard = new Shard(ShardRole.MainApp, newAST);
            var newShardIndex = shards.Count;
            
            shards.Add(newShard); 

            return (newArtifact, newShard, newShardIndex);
        } 


        // [dho] TODO move arbitrary transformations to user code - 25/11/19
        public static async Task<Result<RawAST>> PerformLegacyTransformations(Session session, Artifact artifact, Shard shard, List<Component> newlyAddedComponents, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<RawAST>();

            // [dho] TODO CHECK it is OK to put this transformer here.. if we ever have a different input language other
            // than TypeScript/JavaScript then we probably do not want to always run this transformer unless the source from which
            // the artifact originated was a typescript file
            // (could we just check the origin is a file with a ts/tsx/js/jsx extension?) - 04/08/19
            {
                var tsSyntaxPolyfillTransformer = new Sempiler.Transformation.TypeScriptSyntaxPolyfillTransformer();

                /* ast = */
                result.AddMessages(await tsSyntaxPolyfillTransformer.Transform(session, artifact, shard.AST, token));

                if (HasErrors(result) || token.IsCancellationRequested) return result;
            }

            // [dho] find and replace any nodes that are intent to create a `ViewDeclaration`. For example, a function that returns
            // a view chunk may be detected to be meant as a `ViewDeclaration`, ie. a factory for producing a view - 14/06/19
            {
                var viewDeclIntentTransformer = new Sempiler.Transformation.ViewDeclarationIntentTransformer();

                /* ast = */
                result.AddMessages(await viewDeclIntentTransformer.Transform(session, artifact, shard.AST, token));

                if (HasErrors(result) || token.IsCancellationRequested) return result;
            }

            result.Value = shard.AST;

            // [dho] NOTE we look for MPInfo on *only* the newly added components, not the entire AST as we may
            // be subcompiling a subset of the tree because of injecting new sources etc. - 30/05/19
            var mpInfo = MetaProgramming.GetMetaProgrammingInfo(session, shard.AST, newlyAddedComponents, token);

            // [dho] only acquire lock if we found bridge intent directives - 30/05/19
            if (mpInfo.BridgeIntentDirectives.Count > 0)
            {
                lock (shard.BridgeIntents)
                {
                    // [dho] NOTE we use `AddRange` because the `ProcessArtifact` function may get called multiple
                    // times during the compilation of an artifact, so we do not want to squash anything by wiping out
                    // the existing data in the map each time - 30/05/19 
                    shard.BridgeIntents.AddRange(mpInfo.BridgeIntentDirectives);
                }
            }

            return result;
        }

        public static Node GetClosestNode(string filePath, int lineNumberStart, int columnIndexStart, Dictionary<string, OutFile> filesWritten)
        {
            // [dho] the error may originate from a supporting file (ie. not in the artifact), 
            // in which case we would not have an ArtifactItem to refer back to in order to parse
            // the Node. We first do our best to track down the Node that caused the compilation error, 
            // and fall back to just reporting a general error if we are unable to find the culprit - 11/08/18
            if (filesWritten.ContainsKey(filePath))
            {
                var file = filesWritten[filePath].Emission as IEmission;

                if (file != null && lineNumberStart > -1 && columnIndexStart > -1)
                {   
                    // [dho] NOTE purposely not using a range because we will not know
                    // the precise range if the hint was parsed from a command line message - 16/09/18
                    var pos = file.GetPositionFromLineAndColumn(lineNumberStart, columnIndexStart);

                    // [dho] we allow some margin of error when looking for markers because if our emitter
                    // emits a statement for an Node eg. `import Foo.Bar.Baz`, but the consumer (eg. swiftc)
                    // complains about a subpart of that statement eg. `Foo.Bar.Baz` then none of our markers
                    // would line up with that exact position in the file. So in that case we fall back to markers 
                    // closest to before that position which would give us the Node for `import Foo.Bar.Baz` - 29/08/18
                    var markers = file.GetClosestMarkersAtOrBeforePosition(pos);

                    if (markers.Count > 0)
                    {
                        // [dho] we might find multiple nodes at this position but for now we 
                        // just find the shortest one and go with that.. this will highlight the wrong
                        // node in some cases but at least the position will be right for now - 10/08/18
                        return markers.OrderBy(x => x.EndPos - x.StartPos).First().Node;
                    }
                }
            }

            return null;
        }
    }
}

