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

            ASTHelpers.PreOrderTraversal(session, ast/*clonedAST*/, root, node => {

                if(node.Kind == SemanticKind.ObjectTypeDeclaration)
                {
                    var objTypeDecl = ASTNodeFactory.ObjectTypeDeclaration(ast/*clonedAST*/, node);

                    foreach(var a in objTypeDecl.Annotations)
                    {
                        if(a.Kind == SemanticKind.Annotation)
                        {
                            var annotation = ASTNodeFactory.Annotation(ast/*clonedAST*/, a);

                            if(annotation.Expression.Kind == SemanticKind.Identifier &&
                                ASTNodeFactory.Identifier(ast/*clonedAST*/, (DataNode<string>)annotation.Expression).Lexeme == "struct")
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
                // Swift example app are done through #codegen and so the compiler does not know about the symbols inside that string - 29/08/18
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

                    if(ASTNodeHelpers.IsIdentifierWithName(ast/*clonedAST*/, inv.Subject, "keypath"))
                    {
                        var args = inv.Arguments;

                        if(args.Length == 1 && args[0].Kind == SemanticKind.Identifier)
                        {
                            var lexeme = ASTNodeFactory.Identifier(ast/*clonedAST*/, (DataNode<string>)args[0]).Lexeme;
                            var code = NodeFactory.CodeConstant(ast/*clonedAST*/, inv.Origin, $"\\.{lexeme}");

                            ASTHelpers.Replace(ast/*clonedAST*/, inv.ID, new [] { code.Node });

                            return false;
                        }
                        else
                        {
                            result.AddMessages(new NodeMessage(MessageKind.Warning, $"`keypath` invocation expects identifier argument", inv)
                            {
                                Hint = GetHint(inv.Origin),
                                Tags = DiagnosticTags
                            });
                        }
                    }
                }

                return true;

            }, token);



            if(!HasErrors(result))
            {
                if(nodeIDsToRemove.Count > 0)
                {
                    ASTHelpers.RemoveNodes(ast/*clonedAST*/, nodeIDsToRemove.ToArray());
                }

                // result.Value = ast/*clonedAST*/;
            }

            return Task.FromResult(result);
        }

    }
}