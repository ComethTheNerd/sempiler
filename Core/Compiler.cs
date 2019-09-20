using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sempiler;
using Sempiler.AST;
using Sempiler.Consumption;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Emission;
using Sempiler.Transformation;
using Sempiler.Bundler;
using Sempiler.Parsing;

namespace Sempiler
{
    public static class CompilerHelpers
    {
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
            }
            

            var l = new object();

            Parallel.ForEach(inputPaths, (inputPath, state) =>
            {
                // if (sfResult.Value != null)
                // {
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

                    lock (l)
                    {
                        // result.AddMessages(sfResult.Messages);

                        result.AddMessages(parseMessages);

                        if (token.IsCancellationRequested)
                        {
                            state.Break();
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
                    }

                // }
                // else
                // {
                //     lock (l)
                //     {
                //         result.AddMessages(sfResult.Messages);
                //     }
                // }

            });

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


        public static Result<OutFileCollection> Bundle(IBundler bundler, Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            // var l = new object();

            var outFileCollection = default(OutFileCollection);

            var root = ASTHelpers.GetRoot(ast);

            var task = bundler.Bundle(session, artifact, ast, token);

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

        public static Result<OutFileCollection> Emit(IEmitter emitter, Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            // var l = new object();

            var outFileCollection = default(OutFileCollection);

            var root = ASTHelpers.GetRoot(ast);

            var task = emitter.Emit(session, artifact, ast, root, token);

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
    }
}

