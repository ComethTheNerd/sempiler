using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using Sempiler.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sempiler.Transformation
{
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

    public class TypeScriptSyntaxPolyfillTransformer : ITransformer
    {
        protected readonly string[] DiagnosticTags;


        const string KeypathSymbolLexeme = "keypath";

        public TypeScriptSyntaxPolyfillTransformer()
        {
            DiagnosticTags = new [] { "transformer", "typescript-syntax-polyfill-transformer" };
        }

        public Task<Diagnostics.Result<RawAST>> Transform(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<RawAST>();

            // var ast/*clonedAST*/ = ast.Clone();

            var context = new Context 
            {
                Artifact = artifact,
                Session = session,
                AST = ast/*clonedAST*/
            };

            var root = ASTHelpers.GetRoot(ast/*clonedAST*/);

            var nodeIDsToRemove = new List<string>();

            ASTHelpers.PreOrderLiveTraversal(ast/*clonedAST*/, root, node => {

                if(node.Kind == SemanticKind.ObjectTypeDeclaration)
                {
                    var objTypeDecl = ASTNodeFactory.ObjectTypeDeclaration(ast/*clonedAST*/, node);

                    foreach(var a in objTypeDecl.Annotations)
                    {
                        if(a.Kind == SemanticKind.Annotation)
                        {
                            var annotation = ASTNodeFactory.Annotation(ast/*clonedAST*/, a);

                            if(ASTNodeHelpers.IsIdentifierWithName(ast/*clonedAST*/, annotation.Expression, "extension"))
                            {
                                var meta = NodeFactory.Meta(ast/*clonedAST*/, new PhaseNodeOrigin(PhaseKind.Transformation), MetaFlag.ExtensionType);

                                ASTHelpers.Connect(ast/*clonedAST*/, node.ID, new [] { meta.Node }, SemanticRole.Meta);

                                nodeIDsToRemove.Add(a.ID);
                            }
                            else if(ASTNodeHelpers.IsIdentifierWithName(ast/*clonedAST*/, annotation.Expression, "struct"))
                            {
                                var meta = NodeFactory.Meta(ast/*clonedAST*/, new PhaseNodeOrigin(PhaseKind.Transformation), MetaFlag.ValueType);

                                ASTHelpers.Connect(ast/*clonedAST*/, node.ID, new [] { meta.Node }, SemanticRole.Meta);

                                nodeIDsToRemove.Add(a.ID);
                            }
                        }
                    }
                }
                else if(node.Kind == SemanticKind.ParameterDeclaration)
                {
                    var paramDecl = ASTNodeFactory.ParameterDeclaration(ast/*clonedAST*/, node);

                    foreach(var m in paramDecl.Modifiers)
                    {
                        if(m.Kind == SemanticKind.Modifier)
                        {
                            if(ASTNodeFactory.Modifier(ast/*clonedAST*/, (DataNode<string>)m).Lexeme == "ref")
                            {
                                var meta = NodeFactory.Meta(ast/*clonedAST*/, new PhaseNodeOrigin(PhaseKind.Transformation), MetaFlag.ByReference);

                                ASTHelpers.Connect(ast/*clonedAST*/, node.ID, new [] { meta.Node }, SemanticRole.Meta);

                                nodeIDsToRemove.Add(m.ID);
                            }
                        }
                    }

                }
                // [dho] this was an idea to detect any assignments to properties that exists on the 'this'/'self' and mark the method
                // as a mutating function automatically. However, abandoning it for now because most of the properties defined in the
                // Swift example app are done through #emit and so the compiler does not know about the symbols inside that string - 29/08/18
                // else if(node.Kind == SemanticKind.MethodDeclaration)
                // {
                //     var methodDecl = ASTNodeFactory.MethodDeclaration(ast/*clonedAST*/, node);

                //     var body = methodDecl.Body;

                //     if(body.Kind == SemanticKind.Block)
                //     {
                //         var block = ASTNodeFactory.Block(ast, body);

                //         foreach(var member in block.Content)
                //         {
                //             if(member.Kind == SemanticKind.Assignment)
                //             {
                //                 // if LHS is a property on the class
                //                 var ass = ASTNodeFactory.Assignment(ast, member);

                //                 var storage = result.AddMessages(
                //                     ASTNodeHelpers.ToIdentifierList(ast, ass.Storage)
                //                 );
                            
                //                 System.Diagnostics.Debug.Assert(storage.Count > 0);

                //                 var symbol = storage[0];

                //                 // language semantics for artifact
                //                 var ls = Languages.LanguageSemantics.Of(artifact.TargetLang);

                //                 System.Diagnostics.Debug.Assert(ls != null);


                //                 var s = ls.GetEnclosingScope(session, ast, symbol.Node, new Languages.Scope(symbol.Node), token);
                                
                //                 if(s.Declarations.ContainsKey(symbol.Lexeme))
                //                 {
                //                     int i = 0;
                //                 }


                //             }

                //         }
                //     }
                //     else if(body.Kind == SemanticKind.Assignment)
                //     {
                //         // if LHS is a property on the class
                //     }


                //     // foreach(var m in methodDecl.Modifiers)
                //     // {
                //     //     if(m.Kind == SemanticKind.Modifier)
                //     //     {
                //     //         if(ASTNodeFactory.Modifier(ast/*clonedAST*/, (DataNode<string>)m).Lexeme == "mutating")
                //     //         {
                //     //             var meta = NodeFactory.Meta(ast/*clonedAST*/, new PhaseNodeOrigin(PhaseKind.Transformation), MetaFlag.Mutation);

                //     //             ASTHelpers.Connect(ast/*clonedAST*/, node.ID, new [] { meta.Node }, SemanticRole.Meta);

                //     //             nodeIDsToRemove.Add(m.ID);
                //     //         }
                //     //     }
                //     // }

                // }
                else if(node.Kind == SemanticKind.Invocation)
                {
                    var inv = ASTNodeFactory.Invocation(ast/*clonedAST*/, node);

                    // [dho] polyfill for handling keypath support in Swift 
                    // https://www.swiftbysundell.com/articles/the-power-of-key-paths-in-swift/ - 14/12/19
                    if(ASTNodeHelpers.IsIdentifierWithName(ast/*clonedAST*/, inv.Subject, KeypathSymbolLexeme))
                    {
                        var args = inv.Arguments;

                        if(args.Length == 1)
                        {
                            var arg = args[0];

                            System.Diagnostics.Debug.Assert(arg.Kind == SemanticKind.InvocationArgument);

                            var invArg = ASTNodeFactory.InvocationArgument(ast, arg);
                            var invArgValue = invArg.Value;

                            if(invArgValue?.Kind == SemanticKind.Identifier)
                            {
                                var lexeme = ASTNodeFactory.Identifier(ast/*clonedAST*/, (DataNode<string>)invArgValue).Lexeme;
                                var code = NodeFactory.CodeConstant(ast/*clonedAST*/, inv.Origin, $"\\.{lexeme}");

                                ASTHelpers.Replace(ast/*clonedAST*/, inv.ID, new [] { code.Node });

                                return false; // [dho] do not explore subtree - 14/12/19
                            }                            
                        }

                        result.AddMessages(new NodeMessage(MessageKind.Error, $"'{KeypathSymbolLexeme}' invocation expects identifier argument", inv)
                        {
                            Hint = GetHint(inv.Origin),
                            Tags = DiagnosticTags
                        });
                    }
                }

                return true;

            }, token);



            if(!HasErrors(result))
            {
                if(nodeIDsToRemove.Count > 0)
                {
                    ASTHelpers.DisableNodes(ast/*clonedAST*/, nodeIDsToRemove.ToArray());
                }

                // result.Value = ast/*clonedAST*/;
            }

            result.AddMessages(
                ReplaceDynamicTypeConstructionsWithDictionaryConstructions(session, artifact, ast, token)
            );

            return Task.FromResult(result);
        }

        private Result<object> ReplaceDynamicTypeConstructionsWithDictionaryConstructions(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<object>();

            foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.ForcedCast))
            {
                if(token.IsCancellationRequested) return result;

                var forcedCast = ASTNodeFactory.ForcedCast(ast, node);

                result.AddMessages(
                    ReplaceWithDictionaryConstructionIfRequired(session, artifact, ast, forcedCast.TargetType, forcedCast.Subject, token)
                );

            }

            foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.SafeCast))
            {
                if(token.IsCancellationRequested) return result;

                var safeCast = ASTNodeFactory.SafeCast(ast, node);

                result.AddMessages(
                    ReplaceWithDictionaryConstructionIfRequired(session, artifact, ast, safeCast.TargetType, safeCast.Subject, token)
                );
            }

            foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.DataValueDeclaration))
            {
                if(token.IsCancellationRequested) return result;

                var dataValueDecl = ASTNodeFactory.DataValueDeclaration(ast, node);

                result.AddMessages(
                    ReplaceWithDictionaryConstructionIfRequired(session, artifact, ast, dataValueDecl.Type, dataValueDecl.Initializer, token)
                );
            }

            foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.ParameterDeclaration))
            {
                if(token.IsCancellationRequested) return result;

                var paramDecl = ASTNodeFactory.ParameterDeclaration(ast, node);

                result.AddMessages(
                    ReplaceWithDictionaryConstructionIfRequired(session, artifact, ast, paramDecl.Type, paramDecl.Default, token)
                );
            }

            return result;
        }

        private Result<object> ReplaceWithDictionaryConstructionIfRequired(Session session, Artifact artifact, RawAST ast, Node type, Node value, CancellationToken token)
        {
            var result = new Result<object>();

            if(type?.Kind == SemanticKind.DictionaryTypeReference)
            {
                var rawValue = ASTNodeHelpers.UnwrapAssociations(ast, value);

                if(rawValue?.Kind == SemanticKind.DynamicTypeConstruction)
                {
                    var dictMembers = new List<Node>();

                    var dictConstruction = NodeFactory.DictionaryConstruction(ast, rawValue.Origin);
                    var dynamicTypeConstruction = ASTNodeFactory.DynamicTypeConstruction(ast, rawValue);

                    foreach(var (member, hasNext) in ASTNodeHelpers.IterateMembers(dynamicTypeConstruction.Members))
                    {
                        if(member.Kind == SemanticKind.FieldDeclaration)
                        {
                            var fieldDecl = ASTNodeFactory.FieldDeclaration(ast, member);

                            var kv = NodeFactory.KeyValuePair(ast, fieldDecl.Origin);

                            ASTHelpers.Connect(ast, kv.ID, new [] { fieldDecl.Name }, SemanticRole.Key);
                            ASTHelpers.Connect(ast, kv.ID, new [] { fieldDecl.Initializer }, SemanticRole.Value);
                            ASTHelpers.Connect(ast, kv.ID, fieldDecl.Meta, SemanticRole.Meta);
                        
                            
                            foreach(var mod in fieldDecl.Modifiers)
                            {
                                result.AddMessages(
                                    CreateUnsupportedFeatureResult(mod, "dictionary entry modifiers")
                                );
                            }
                            
                            var fieldType = fieldDecl.Type;

                            if(fieldType != null)
                            {
                                result.AddMessages(
                                    CreateUnsupportedFeatureResult(fieldType, "dictionary entry types")
                                );
                            }

                            dictMembers.Add(kv.Node);
                        }
                        else
                        {
                            dictMembers.Add(member);
                        }
                    }

                    if(dictMembers.Count > 0)
                    {
                        ASTHelpers.Connect(ast, dictConstruction.ID, dictMembers.ToArray(), SemanticRole.Member);
                    }

                    ASTHelpers.Replace(ast, rawValue.ID, new [] { dictConstruction.Node });
                }

            }

            return result;
        }

    }
}