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

        // public enum TransformationMode 
        // {
        //     Hoist,
        //     Replace
        // }

        public static Result<object> HoistDirectives(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, BaseLanguageSemantics languageSemantics, CTInSituFunctionIdentifiers serverInteropFnIDs,/* TransformationMode mode, */List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        {
            var result = new Result<object>();

            foreach(var node in ASTHelpers.QueryLiveDescendantsByKind(ast, component.Node, SemanticKind.Directive))
            {
                // // [dho] right now we do not have an API to query by kind for nodes that are
                // // descendants of another node (eg. in a particular component), but the CT exec
                // // replacements/deletions and this check should prevent us doing unnecesary
                // // work as new sources are added to the AST during CT exec and evaluated - 21/01/20
                // if(!ASTHelpers.IsLive(ast, node.ID)) continue;

            
                var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

                // [dho] eg. `#compiler ...` - 11/07/19
                if (directive.Name == CTDirective.CodeExec)
                {
                    result.AddMessages(
                        TransformCTCodeExecDirective(session, artifact, shard, ast, directive, languageSemantics, serverInteropFnIDs, /*mode,*/ hoistedDirectiveStatements, hoistedNodes, token)
                    );
                }
            }

            foreach(var node in ASTHelpers.QueryLiveDescendantsByKind(ast, component.Node, SemanticKind.Directive))
            {
                // // [dho] right now we do not have an API to query by kind for nodes that are
                // // descendants of another node (eg. in a particular component), but the CT exec
                // // replacements/deletions and this check should prevent us doing unnecesary
                // // work as new sources are added to the AST during CT exec and evaluated - 21/01/20
                // if(!ASTHelpers.IsLive(ast, node.ID)) continue;

            
                var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

                if (directive.Name == CTDirective.Emit)
                {
                    result.AddMessages(
                        TransformCTEmitDirective(session, artifact, shard, ast, directive, languageSemantics, serverInteropFnIDs, /*mode,*/ hoistedDirectiveStatements, hoistedNodes, token)
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

        // public static Result<object> HoistDirectives(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, Node node, BaseLanguageSemantics languageSemantics, CTInSituFunctionIdentifiers serverInteropFnIDs, TransformationMode mode, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        // {
        //     var result = new Result<object>();

        //     // foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, node.ID))
        //     // {
        //     //     // [dho] compile time exec needs to have happened on the children first in case the
        //     //     // parent node relies on them (and this is generally what is expected in programming anyway!) - 11/07/19
        //     //     result.AddMessages(
        //     //         HoistDirectives(session, artifact, shard, ast, component, child, languageSemantics, serverInteropFnIDs, mode, hoistedDirectiveStatements, hoistedNodes, token)
        //     //     );
        //     // }

        //     if (node.Kind == SemanticKind.Directive)
        //     {
        //         var directiveLexeme = ASTNodeHelpers.GetLexeme(node).Replace("*/", "*\\/");

        //         var comment = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"/* DIRECTIVE : `{directiveLexeme}`*/").Node;

        //         MarkAsCTExec(ast, comment);

        //         hoistedDirectiveStatements.Add(comment);

        //         var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

        //         // [dho] eg. `#compiler ...` - 11/07/19
        //         if (directive.Name == CTDirective.CodeExec)
        //         {
        //             result.AddMessages(
        //                 TransformCTCodeExecDirective(session, artifact, shard, ast, component, directive, languageSemantics, serverInteropFnIDs, mode, hoistedDirectiveStatements, hoistedNodes, token)
        //             );
        //         }
        //         else if (directive.Name == CTDirective.Emit)
        //         {
        //             result.AddMessages(
        //                 TransformCTEmitDirective(session, artifact, shard, ast, component, directive, languageSemantics, serverInteropFnIDs, mode, hoistedDirectiveStatements, hoistedNodes, token)
        //             );
        //         }
        //         else if (CTDirective.IsCTDirectiveName(directive.Name))
        //         {
        //             result.AddMessages(
        //                 CreateUnsupportedFeatureResult(directive.Node, $"compile time {directive.Name} directives")
        //             );
        //         }
        //         else
        //         {
        //             // [dho] leave the directive as is, but just do not include it at compile time for
        //             // evaluation as we do not recognize it? - 11/07/19
        //         }
        //     }

        //     return result;
        // }



        private static Result<object> TransformCTCodeExecDirective(Session session, Artifact artifact, Shard shard, RawAST ast, Directive directive, 
        BaseLanguageSemantics languageSemantics, CTInSituFunctionIdentifiers serverInteropFnIDs, 
            /*TransformationMode mode,*/ List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        {
            var result = new Result<object>();

            var subject = directive.Subject;

            // var content = new List<Node>();

            // var depMode = TransformationMode.Replace;

            // [dho] have to hoist all dependencies recursively - 11/07/19
            result.AddMessages(
                HoistDependencies(session, artifact, ast, subject, languageSemantics, /*TransformationMode.Hoist,*/ hoistedDirectiveStatements, hoistedNodes, token)
            );

            /*
            #compiler  {
    config.mode = 'DEBUG';
    
    if(true){
        addSources("./main.ts");

    }
    else 
    {
        #emit `hello`;
        console.log("He")
    }
}
            
            hoisted_dependencies();

            await (async () => {

                const result = (async () => {

                    if(true)
                    {
                        await addSources("./main.ts");
                    }
                    else
                    {
                        insertCodeAt(..., "`hello`");
                        console.log("He");
                    }

                })

                // if value expression
                replace(result);
                // or
                delete();
            })()
            */
            var replacementDVDecl = NodeFactory.DataValueDeclaration(ast, directive.Origin);
            var replacementLexeme = replacementDVDecl.ID;
            {
                ASTHelpers.Connect(ast, replacementDVDecl.ID, new [] {
                    NodeFactory.Identifier(ast, directive.Origin, replacementLexeme).Node
                }, SemanticRole.Name);

                ASTHelpers.Connect(ast, replacementDVDecl.ID, new [] {
                    NodeFactory.StringConstant(ast, directive.Origin, string.Empty).Node
                }, SemanticRole.Initializer);
            }

            // [dho] NOTE using `directive.Node` as root, NOT `subject`, incase the subject is also a directive... otherwise
            // we would miss it - 21/01/20
            foreach(var child in ASTHelpers.QueryLiveDescendantsByKind(ast, directive.Node, SemanticKind.Directive))
            {
                var childDirective = ASTNodeFactory.Directive(ast, (DataNode<string>)child);

                result.AddMessages(
                    TransformCTCodeExecNestedDirective(
                        session, artifact, shard, ast, childDirective, 
                        replacementLexeme, token
                    )
                );
            }

            var outerLambdaContent = new List<Node>();      
            {
                var hoistedNameLexeme = directive.ID;

                outerLambdaContent.Add(CreateDirectiveComment(ast, directive));

                outerLambdaContent.Add(replacementDVDecl.Node);

                var resultDVDecl = NodeFactory.DataValueDeclaration(ast, directive.Origin);
                {
                    ASTHelpers.Connect(ast, resultDVDecl.ID, new [] {
                        NodeFactory.Identifier(ast, directive.Origin, hoistedNameLexeme).Node
                    }, SemanticRole.Name); 

            
                    var (userCodeIIFEInv, userCodeIIFELambda) = ASTNodeHelpers.CreateIIFE(ast, new List<Node> { subject });
                    {
                        var asyncFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Transformation),
                            MetaFlag.Asynchronous
                        );

                        ASTHelpers.Connect(ast, userCodeIIFELambda.ID, new [] { asyncFlag.Node }, SemanticRole.Meta);
                    }

                    ASTHelpers.Connect(ast, resultDVDecl.ID, new [] {
                        ASTNodeHelpers.CreateAwait(ast, userCodeIIFEInv.Node).Item1.Node
                    }, SemanticRole.Initializer);
                }


                outerLambdaContent.Add(resultDVDecl.Node);


                // [dho] we will replace the `#compiler` directive by a code constant made up of
                // concatenating all the `#emit` directives that get hit in the body. If none are hit
                // then this will replace the directive with the empty string which is a NOP anyway - 21/01/20 
                var inv = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                {
                    var invSubject = default(Node);
                    var invArguments = new List<Node>();

                    invSubject = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), serverInteropFnIDs.ReplaceNodeByCodeConstant).Node;

                    invArguments.Add(
                        CreateInvocationArgument(ast,
                            NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
                        ).Node
                    );

                    invArguments.Add(
                        CreateInvocationArgument(ast, 
                            NodeFactory.Identifier(ast, directive.Origin, replacementLexeme).Node
                        ).Node
                    );

                    ASTHelpers.Connect(ast, inv.ID, new[] { invSubject }, SemanticRole.Subject);
                    ASTHelpers.Connect(ast, inv.ID, invArguments.ToArray(), SemanticRole.Argument);
                }

    
                BindSubjectToPassContext(session, artifact, shard, ast, inv.Node, token);

                InterimSuspension awaitInv = ASTNodeHelpers.CreateAwait(ast, inv.Node).Item2;

                outerLambdaContent.Add(awaitInv.Node);

            }


            // List<Node> content = new List<Node>();
            {
                var (codeExecIIFEInv, codeExecIIFELambda) = ASTNodeHelpers.CreateIIFE(ast, outerLambdaContent);
                var asyncFlag = NodeFactory.Meta(
                    ast,
                    new PhaseNodeOrigin(PhaseKind.Transformation),
                    MetaFlag.Asynchronous
                );

                ASTHelpers.Connect(ast, codeExecIIFELambda.ID, new [] { asyncFlag.Node }, SemanticRole.Meta);

                InterimSuspension awaitInv = ASTNodeHelpers.CreateAwait(ast, codeExecIIFEInv.Node).Item2;

                MarkAsCTExec(ast, awaitInv.Node);

                hoistedDirectiveStatements.Add(awaitInv.Node);
            }

            
            _SuppressOriginalDirectiveNode(ast, directive, languageSemantics);


            return result;
        }


        private static Result<object> TransformCTCodeExecNestedDirective(Session session, Artifact artifact, Shard shard, RawAST ast, 
            Directive directive, string replacementLexeme,  CancellationToken token)
        {
            var result = new Result<object>();

            // if(node.Kind == SemanticKind.Directive)
            // {
            //     var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

                if(directive.Name == CTDirective.CodeExec)
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"compile time code execution directives cannot be nested", directive.Node)
                        {
                            Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(directive.Origin)
                        }
                    );
                }
                else if (directive.Name == CTDirective.Emit)
                {
                    // [dho] transform `#emit `foo`` into `result += `foo`` because then we will
                    // replace the - 21/01/20
                    
                    var subject = directive.Subject;

                    // if(subject.Kind == SemanticKind.InterpolatedString)
                    // {
                        var assignment = NodeFactory.AdditionAssignment(ast, directive.Origin);
                        
                        ASTHelpers.Connect(ast, assignment.ID, new [] {
                            NodeFactory.Identifier(ast, directive.Origin, replacementLexeme).Node
                        }, SemanticRole.Storage);

                        ASTHelpers.Connect(ast, assignment.ID, new [] {
                            directive.Subject
                        }, SemanticRole.Value);
                        
                        ASTHelpers.Replace(ast, directive.ID, new [] { 
                            CreateDirectiveComment(ast, directive),
                            assignment.Node 
                        });
                    // }
                    // else
                    // {
                    //     result.AddMessages(
                    //         new NodeMessage(MessageKind.Error, "expected interpolated string", subject)
                    //     );
                    // }
                }
                else if (CTDirective.IsCTDirectiveName(directive.Name))
                {
                    result.AddMessages(
                        CreateUnsupportedFeatureResult(directive.Node, $"compile time {directive.Name} directives")
                    );
                }
            // }

            return result;
        }
        
        private static Result<object> TransformCTEmitDirective(Session session, Artifact artifact, Shard shard, RawAST ast, Directive directive, BaseLanguageSemantics languageSemantics, CTInSituFunctionIdentifiers serverInteropFnIDs, /*TransformationMode mode,*/ List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        {
            var result = new Result<object>();

            var subject = directive.Subject;

            // System.Diagnostics.Debug.Assert(subject.Kind == SemanticKind.InterpolatedString);

            // [dho] DISABLING support for symbolic dependencies in #emit because when the #emit
            // is conditional, this will break currently. TODO FIX - 20/01/20
            // if(ASTNodeFactory.InterpolatedString(ast, subject).Members.Length > 1)
            // {
            //     result.AddMessages(CreateUnsupportedFeatureResult(subject, "symbolic dependencies"));
            //     return result;
            // }

            // var content = new List<Node>();

            // [dho] have to hoist all dependencies recursively - 11/07/19
            result.AddMessages(
                HoistDependencies(session, artifact, ast, subject, languageSemantics, /*mode,*/ hoistedDirectiveStatements, hoistedNodes, token)
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

            InterimSuspension awaitInv = ASTNodeHelpers.CreateAwait(ast, inv.Node).Item2;

            MarkAsCTExec(ast, awaitInv.Node);

            // [dho] NOTE by this point, the static dependencies will have been renamed 
            // and appear in the preceding statements - 11/07/19
            // content.Add(awaitInv.Node);

            // var block = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

            // ASTHelpers.Connect(ast, block.ID, content.ToArray(), SemanticRole.Content);

            // hoistedDirectiveStatements.Add(block.Node);
            
            // if(mode == TransformationMode.Replace)
            // {
            //     ASTHelpers.Replace(ast, directive.ID, new [] { awaitInv.Node });
            // }
            // if(mode == TransformationMode.Hoist)
            // {
                
                hoistedDirectiveStatements.Add(CreateDirectiveComment(ast, directive));
                
                hoistedDirectiveStatements.Add(awaitInv.Node);
         
                _SuppressOriginalDirectiveNode(ast, directive, languageSemantics);
            // }


            return result;
        }

        private static void _SuppressOriginalDirectiveNode(RawAST ast, Directive directive, BaseLanguageSemantics languageSemantics)
        {
            if(languageSemantics.IsValueExpression(ast, directive.Node) || 
                directive.Parent?.Kind != SemanticKind.ObjectTypeDeclaration)
            {
                // [dho] the result of an emit directive is void - 21/01/20 
                ASTHelpers.Replace(ast, directive.ID, new [] { 
                    NodeFactory.CodeConstant(ast, directive.Origin, "(void 0)").Node
                });
            }
            else
            {
                ASTHelpers.DisableNodes(ast, new [] { directive.ID });
            }
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


            foreach (var symbolName in CTAPISymbols.EnumerateCTAPIFunctionNames())
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

        private static Result<object> HoistDependencies(Session session, Artifact artifact, RawAST ast, Node node, BaseLanguageSemantics languageSemantics, /*TransformationMode mode, */List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
        {
            var result = new Result<object>();

            MarkAsCTExec(ast, node);

            // [dho] first, get the symbols that the node depends on (references in its subtree) - 11/07/19
            var dependencies = languageSemantics.GetSymbolicDependencies(session, artifact, ast, node, token);

            foreach (var dependency in dependencies)
            {
                var decl = dependency.Declaration;

                // [dho] if we were able to find the declaration of the symbol - 11/07/19
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

                    // [dho] we use the ID of the node as the guid for the hoisted expression/statement - 11/07/19
                    var hoistedNameLexeme = decl.ID;

                    // [dho] check we haven't already hoisted this node - 11/07/19
                    if (!hoistedNodes.ContainsKey(hoistedNameLexeme))
                    {
                        // [dho] I'm intentionally not guarding against array out of bounds, because if the declaration
                        // does not have a name then this will need some refactoring - 11/07/19
                        var name = ASTHelpers.QueryLiveEdgeNodes(ast, decl.ID, SemanticRole.Name)[0];

                        // [dho] we need to rename the hoisted expression to match the guid we are
                        // giving the name of the hoisted expression - 11/07/19
                        ASTNodeHelpers.RefactorName(ast, name, hoistedNameLexeme);

                        // [dho] now we need to rename all references to the original symbolic name - 11/07/19
                        // [dho] TODO CHECK could we just create a single alias from the original symbol to 
                        // the new name instead??? - 11/07/19
                        foreach (var lexeme in dependency.References.Keys)
                        {
                            foreach (var reference in dependency.References[lexeme])
                            {
                                System.Diagnostics.Debug.Assert(reference.Kind == SemanticKind.Identifier);

                                ASTNodeHelpers.RefactorName(ast, reference, hoistedNameLexeme);
                            }
                        }

                        // [dho] record the work we've done so we do not duplicate any hoisting,
                        // and have a handle to the hoisted nodes for emitting - 11/07/19 
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

        private static Node CreateDirectiveComment(RawAST ast, Directive directive)
        {
            var directiveLexeme = ASTNodeHelpers.GetLexeme(directive).Replace("*/", "*\\/");

            var comment = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"/* DIRECTIVE : `{directiveLexeme}`*/").Node;

            MarkAsCTExec(ast, comment);

            return comment;
        }

    }
}