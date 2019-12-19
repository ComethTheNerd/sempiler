using System;
using Sempiler.AST;
using Sempiler.Diagnostics;
using Sempiler.Languages;
using Sempiler.AST.Diagnostics;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using System.Collections.Generic;
using System.Threading;

namespace Sempiler.CTExec
{
    using static CTExecHelpers;

    public static class CTExecTransformerHelpers
    {
        private static readonly string[] InjectParentPathForCTAPISymbols = new string[] {
            CTAPISymbols.AddArtifact,
            CTAPISymbols.AddSources,
            CTAPISymbols.AddAsset,
            CTAPISymbols.AddRes,
            CTAPISymbols.AddRawSources,
            CTAPISymbols.AddShard
        };

        public static Result<object> HoistDirectives(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, Node node, BaseLanguageSemantics languageSemantics, CTInSituFunctionIdentifiers serverInteropFnIDs, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        {
            var result = new Result<object>();

            foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, node.ID))
            {
                // [dho] compile time exec needs to have happened on the children first in case the
                // parent node relies on them (and this is generally what is expected in programming anyway!) - 11/07/19
                result.AddMessages(
                    HoistDirectives(session, artifact, shard, ast, component, child, languageSemantics, serverInteropFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
                );
            }

            if (node.Kind == SemanticKind.Directive)
            {
                var directiveLexeme = ASTNodeHelpers.GetLexeme(node).Replace("*/", "*\\/");

                var comment = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"/* DIRECTIVE : `{directiveLexeme}`*/").Node;

                MarkAsCTExec(ast, comment);

                hoistedDirectiveStatements.Add(comment);

                var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

                // [dho] eg. `#compiler ...` - 11/07/19
                if (directive.Name == CTDirective.CodeExec)
                {
                    result.AddMessages(
                        TransformCTCodeExecDirective(session, artifact, shard, ast, component, directive, languageSemantics, serverInteropFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
                    );
                }
                else if (directive.Name == CTDirective.CodeGen)
                {
                    result.AddMessages(
                        TransformCTCodeGenDirective(session, artifact, shard, ast, component, directive, languageSemantics, serverInteropFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
                    );
                }
                else if (CTDirective.IsCTDirectiveName(directive.Name))
                {
                    result.AddMessages(
                        CreateUnsupportedFeatureResult(directive.Node, $"compile time {directive.Name} directives")
                    );
                }
                else
                {
                    // [dho] leave the directive as is, but just do not include it at compile time for
                    // evaluation as we do not recognize it? - 11/07/19
                }
            }

            return result;
        }



        private static Result<object> TransformCTCodeExecDirective(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, Directive directive, BaseLanguageSemantics languageSemantics, CTInSituFunctionIdentifiers serverInteropFnIDs, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        {
            var result = new Result<object>();

            var subject = directive.Subject;

            var content = new List<Node>();

            // [dho] have to hoist all dependencies recursively - 11/07/19
            result.AddMessages(
                HoistDependencies(session, artifact, ast, subject, languageSemantics, content, hoistedNodes, token)
            );


            var inv = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                var invSubject = default(Node);
                var invArguments = new List<Node>();

                // [dho] NOTE we check whether the #directive is a value position, NOT the subject
                // of the directive because we are working out whether the purpose of running this #directive
                // was to generate a value - 11/07/19
                if (languageSemantics.IsValueExpression(ast, directive.Node))
                {
                    var hoistedNameLexeme = directive.ID;

                    invSubject = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), serverInteropFnIDs.InsertImmediateSiblingFromValueAndDeleteNode).Node;

                    invArguments.Add(
                        CreateInvocationArgument(ast,
                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $"\"{directive.ID}\"").Node
                        ).Node
                    );

                    var dvDecl = NodeFactory.DataValueDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        ASTHelpers.Connect(ast, dvDecl.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), hoistedNameLexeme).Node
                        }, SemanticRole.Name);

                        ASTHelpers.Connect(ast, dvDecl.ID, new[] { subject }, SemanticRole.Initializer);
                    }


                    content.Add(dvDecl.Node);

                    invArguments.Add(
                        CreateInvocationArgument(ast,
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
                        ).Node
                    );

                    invArguments.Add(
                        CreateInvocationArgument(ast,
                            NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
                        ).Node
                    );

                    // [dho] make the original location point to the new hoisted declaration - 11/07/19
                    // ASTHelpers.Replace(ast, directive.ID, new [] {
                    //     NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), hoistedNameLexeme).Node
                    // });
                }
                else
                {
                    content.Add(subject);

                    invSubject = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), serverInteropFnIDs.DeleteNode).Node;

                    invArguments.Add(
                        CreateInvocationArgument(ast,
                            NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
                        ).Node
                    );
                }


                ASTHelpers.Connect(ast, inv.ID, new[] { invSubject }, SemanticRole.Subject);
                ASTHelpers.Connect(ast, inv.ID, invArguments.ToArray(), SemanticRole.Argument);
            }

   
            BindSubjectToPassContext(session, artifact, shard, ast, inv.Node, token);

            InterimSuspension awaitInv = CreateAwait(ast, inv.Node).Item2;

            MarkAsCTExec(ast, awaitInv.Node);

            // [dho] NOTE by this point, the static dependencies will have been renamed 
            // and appear in the preceding statements - 11/07/19
            content.Add(awaitInv.Node);

            // var block = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

            // ASTHelpers.Connect(ast, block.ID, content.ToArray(), SemanticRole.Content);

            // hoistedDirectiveStatements.Add(block.Node);
            hoistedDirectiveStatements.AddRange(content);

            // ASTHelpers.DisableNodes(ast, new[] { directive.ID });

            return result;
        }

        private static Result<object> TransformCTCodeGenDirective(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, Directive directive, BaseLanguageSemantics languageSemantics, CTInSituFunctionIdentifiers serverInteropFnIDs, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        {
            var result = new Result<object>();

            var subject = directive.Subject;

            System.Diagnostics.Debug.Assert(subject.Kind == SemanticKind.InterpolatedString);

            var content = new List<Node>();

            // [dho] have to hoist all dependencies recursively - 11/07/19
            result.AddMessages(
                HoistDependencies(session, artifact, ast, subject, languageSemantics, content, hoistedNodes, token)
            );

            // foreach(var interpMember in ASTNodeFactory.InterpolatedString(ast, subject).Members)
            // {
            //     if(interpMember.Kind == SemanticKind.InterpolatedStringConstant)
            //     {
                    
            //     }
            // }


            var inv = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                var invSubject = default(Node);
                var invArguments = new List<Node>();

                var hoistedNameLexeme = directive.ID;

                invSubject = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), serverInteropFnIDs.ReplaceNodeByCodeConstant).Node;

                invArguments.Add(
                    CreateInvocationArgument(ast,
                        NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
                    ).Node
                );


                // if(subject.Kind == SemanticKind.InterpolatedString)
                // {
                //     var interp = ASTNodeFactory.InterpolatedString(ast, subject);

                //     foreach(var member in interp.Members)
                //     {
                //         if(member.Kind == SemanticKind.InterpolatedStringConstant)
                //         {
                //             var interpString = ASTNodeFactory.InterpolatedStringConstant(ast, (DataNode<string>)member);

                //             // if(interpString.Value.IndexOf("suffixes") > - 1)
                //             // {
                //             //     int xxxx = 0;
                //             // }

                //             // var value = interpString.Value.Replace("\\\\", "\\\\\\\\");

                //             // var replacement = NodeFactory.InterpolatedStringConstant(ast, interpString.Origin, value);

                //             // ASTHelpers.Replace(ast, interpString.ID, new [] { replacement.Node });
                //         }
                //     }
                // }
                // else if(subject.Kind == SemanticKind.StringConstant)
                // {
                //     var stringConstant = ASTNodeFactory.StringConstant(ast, (DataNode<string>)subject);

                //     var value = stringConstant.Value.Replace("\\\\", "\\\\\\\\");

                //     var replacement = NodeFactory.StringConstant(ast, stringConstant.Origin, value);

                //     ASTHelpers.Replace(ast, stringConstant.ID, new [] { replacement.Node });
                // }

                invArguments.Add(
                    CreateInvocationArgument(ast, subject).Node
                );

                // // [dho] make the original location point to the new hoisted declaration - 11/07/19
                // ASTHelpers.Replace(ast, directive.ID, new [] {
                //     NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), hoistedNameLexeme).Node
                // });

                ASTHelpers.Connect(ast, inv.ID, new[] { invSubject }, SemanticRole.Subject);
                ASTHelpers.Connect(ast, inv.ID, invArguments.ToArray(), SemanticRole.Argument);
            }

            BindSubjectToPassContext(session, artifact, shard, ast, inv.Node, token);

            InterimSuspension awaitInv = CreateAwait(ast, inv.Node).Item2;

            MarkAsCTExec(ast, awaitInv.Node);

            // [dho] NOTE by this point, the static dependencies will have been renamed 
            // and appear in the preceding statements - 11/07/19
            content.Add(awaitInv.Node);

            // var block = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

            // ASTHelpers.Connect(ast, block.ID, content.ToArray(), SemanticRole.Content);

            // hoistedDirectiveStatements.Add(block.Node);
            hoistedDirectiveStatements.AddRange(content);

            // ASTHelpers.DisableNodes(ast, new[] { directive.ID });

            return result;
        }

        public static Result<object> TransformCTAPIInvocations(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, BaseLanguageSemantics languageSemantics, CancellationToken token)
        {
            var result = new Result<object>();

            var scope = new Scope(component.Node);


            var parentDirPath = default(string);
            {
                var sourceWithLocation = ((SourceNodeOrigin)component.Node.Origin).Source as ISourceWithLocation<IFileLocation>;
                {
                    if (sourceWithLocation?.Location != null)
                    {
                        var location = sourceWithLocation.Location;

                        // [dho] sources will be resolved relative to the same directory - 07/05/19
                        parentDirPath = location.ParentDir.ToPathString();
                    }
                }
            }


            foreach (var symbolName in CTAPISymbols.EnumerateCTAPISymbolNames())
            {
                scope.Declarations[symbolName] = component.Node;

                var references = languageSemantics.GetUnqualifiedReferenceMatches(session, ast, component.Node, scope, symbolName, token);


                if (parentDirPath != null)
                {
                    if (Array.IndexOf(InjectParentPathForCTAPISymbols, symbolName) > -1)
                    {
                        foreach (var reference in references)
                        {
                            InjectParentDirPathArgument(ast, reference, parentDirPath);
                        }
                    }
                }


                foreach (var reference in references)
                {
                    // [dho] the reference will be the identifier (name) of the invocation,
                    // so we obtain a handle to the invocation itself - 27/11/19
                    var inv = ASTHelpers.GetFirstAncestorOfKind(ast, reference.ID, SemanticKind.Invocation);

                    System.Diagnostics.Debug.Assert(inv != null);

                    // [dho] ensure the invocation is marked as computable at
                    // compile time - 27/11/19
                    if(!MetaHelpers.HasFlags(ast, reference, MetaFlag.CTExec))
                    {
                        MarkAsCTExec(ast, inv);
                    }

                    BindSubjectToPassContext(session, artifact, shard, ast, inv, token);

                    var parentheses = NodeFactory.Association(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

                    ASTHelpers.Replace(ast, inv.ID, new[] { parentheses.Node });

                    var awaitExp = NodeFactory.InterimSuspension(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        ASTHelpers.Connect(ast, awaitExp.ID, new[] { inv }, SemanticRole.Operand);
                    }

                    ASTHelpers.Connect(ast, parentheses.ID, new[] { awaitExp.Node }, SemanticRole.Subject);
                }
            }


            return result;
        }

        // [dho] conversion from `hello(a, b, c)` to `hello.bind({ artifactName : "foo", shardIndex : "bar", messageID : "x" })(a, b, c)` - 05/10/19
        private static void BindSubjectToPassContext(Session session, Artifact artifact, Shard shard, RawAST ast, Node inv, CancellationToken token)
        {
            var previousInvSubject = ASTNodeFactory.Invocation(ast, inv).Subject;

            var boundInv = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

            ASTHelpers.Replace(ast, previousInvSubject.ID, new[] { boundInv.Node });
            {
                ASTHelpers.Connect(ast, boundInv.ID, new[] {
                    ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, new PhaseNodeOrigin(PhaseKind.Transformation), new [] {
                        previousInvSubject,
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "bind").Node,
                    })
                }, SemanticRole.Subject);


                var boundArg = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation),  
                                $"{{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme} : '{inv.ID}', {MessageIDSymbolLexeme} : {CTExecEmitter.CreateMessageIDCode()} }}");

                // var boundArg = NodeFactory.DynamicTypeConstruction(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                // {
                //     // ARTIFACT NAME
                //     var artifactNameField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                //     {
                //         ASTHelpers.Connect(ast, artifactNameField.ID, new[] {
                //             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), ArtifactNameSymbolLexeme).Node
                //         }, SemanticRole.Name);

                //         ASTHelpers.Connect(ast, artifactNameField.ID, new[] {
                //             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), ArtifactNameSymbolLexeme).Node
                //         }, SemanticRole.Initializer);
                //     }

                //     ASTHelpers.Connect(ast, boundArg.ID, new[] { artifactNameField.Node }, SemanticRole.Member);

                //     // ANCILLARY INDEX
                //     var shardIndexField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                //     {
                //         ASTHelpers.Connect(ast, shardIndexField.ID, new[] {
                //             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), ShardIndexSymbolLexeme).Node
                //         }, SemanticRole.Name);

                //         ASTHelpers.Connect(ast, shardIndexField.ID, new[] {
                //             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), ShardIndexSymbolLexeme).Node
                //         }, SemanticRole.Initializer);
                //     }

                //     ASTHelpers.Connect(ast, boundArg.ID, new[] { shardIndexField.Node }, SemanticRole.Member);

                //     // MESSAGE ID
                //     var messageIDField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                //     {
                //         var messageID = CTExecEmitter.CreateMessageIDCode();

                //         ASTHelpers.Connect(ast, messageIDField.ID, new[] {
                //             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), MessageIDSymbolLexeme).Node
                //         }, SemanticRole.Name);

                //         ASTHelpers.Connect(ast, messageIDField.ID, new[] {
                //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), messageID).Node
                //         }, SemanticRole.Initializer);
                //     }

                //     ASTHelpers.Connect(ast, boundArg.ID, new[] { messageIDField.Node }, SemanticRole.Member);
                // }
                ASTHelpers.Connect(ast, boundInv.ID, new[] { CreateInvocationArgument(ast, boundArg.Node).Node }, SemanticRole.Argument);
            }

        }


        private static void InjectParentDirPathArgument(RawAST ast, Node reference, string parentDirPath)
        {
            var inv = ASTHelpers.GetFirstAncestorOfKind(ast, reference.ID, SemanticKind.Invocation);

            System.Diagnostics.Debug.Assert(inv != null);

            var args = ASTNodeFactory.Invocation(ast, inv).Arguments;


            var parentDirPathArg = CreateInvocationArgument(ast,
                NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), parentDirPath).Node
            );

            if (args.Length > 0)
            {
                ASTHelpers.InsertBefore(ast, args[0].ID, new[] { parentDirPathArg.Node }, SemanticRole.Argument);
            }
            else
            {
                ASTHelpers.Connect(ast, inv.ID, new[] { parentDirPathArg.Node }, SemanticRole.Argument);
            }
        }

        private static Result<object> HoistDependencies(Session session, Artifact artifact, RawAST ast, Node node, BaseLanguageSemantics languageSemantics, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        {
            var result = new Result<object>();

            MarkAsCTExec(ast, node);

            var dependencies = languageSemantics.GetSymbolicDependencies(session, artifact, ast, node, token);

            foreach (var dependency in dependencies)
            {
                var decl = dependency.Declaration;

                if (decl != null)
                {
                    // System.Console.WriteLine("🌈🌈 CTExec TRANSFORMER IsCTComputable", node.ID);
                    // if (!languageSemantics.IsCTComputable(session, artifact, ast, decl, token))
                    // {
                    //     result.AddMessages(new NodeMessage(MessageKind.Error, "Compile time execution can only depend on statically computable symbols", decl)
                    //     {
                    //         Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(decl.Origin)
                    //     });

                    //     continue;
                    // }


                    MarkAsCTExec(ast, decl);

                    var hoistedNameLexeme = decl.ID;

                    if (!hoistedNodes.ContainsKey(hoistedNameLexeme))
                    {
                        // [dho] I'm intentionally not guarding against array out of bounds, because if the declaration
                        // does not have a name then this will need some refactoring - 11/07/19
                        var name = ASTHelpers.QueryLiveEdgeNodes(ast, decl.ID, SemanticRole.Name)[0];

                        ASTNodeHelpers.RefactorName(ast, name, hoistedNameLexeme);

                        foreach (var lexeme in dependency.References.Keys)
                        {
                            foreach (var reference in dependency.References[lexeme])
                            {
                                System.Diagnostics.Debug.Assert(reference.Kind == SemanticKind.Identifier);

                                ASTNodeHelpers.RefactorName(ast, reference, hoistedNameLexeme);
                            }
                        }

                        hoistedDirectiveStatements.Add(decl);
                        hoistedNodes[hoistedNameLexeme] = decl;
                    }
                }
                else
                {
                    // [dho] I guess for now if the symbol is assumed to be global/implicit because we did not 
                    // resolve the declaration, then we should not need to hoist anything? - 11/07/19
                }
            }

            return result;
        }

        private static InvocationArgument CreateInvocationArgument(RawAST ast, Node value)
        {
            var arg = NodeFactory.InvocationArgument(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, arg.ID, new[] { value }, SemanticRole.Value);
            }

            return arg;
        }

        private static (Association, InterimSuspension) CreateAwait(RawAST ast, Node operand)
        {
            var awaitExp = NodeFactory.InterimSuspension(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, awaitExp.ID, new[] { operand }, SemanticRole.Operand);
            }

            var parentheses = NodeFactory.Association(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, parentheses.ID, new[] { awaitExp.Node }, SemanticRole.Subject);
            }

            return (parentheses, awaitExp);
        }

    }
}