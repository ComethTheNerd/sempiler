using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using Sempiler.Diagnostics;
using Sempiler.Languages;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sempiler.Transformation
{
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

    public class AndroidLithoTransformer : ViewTransformer
    {

        public AndroidLithoTransformer() : base(new[] { "transformer", "android-litho" })
        {
        }
   
        protected override Result<object> TransformViewDeclaration(Session session, Artifact artifact, RawAST ast, ViewDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var n = node.Name;

            if (n?.Kind != SemanticKind.Identifier)
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Expected view declaration name to be identifier but found '{n?.Kind}'", n ?? node.Node)
                    {
                        Hint = GetHint((n ?? node.Node).Origin),
                        Tags = DiagnosticTags
                    }
                );

                return result;
            }


            //ASTNodeFactory.Identifier(ast, (DataNode<string>)node.Name).Lexeme + "Spec"


            System.Diagnostics.Debug.Assert(node.Name.Kind == SemanticKind.Identifier);

            var viewDeclNameLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)node.Name).Lexeme;

            var hoistedNameLexeme = node.ID + "__" + viewDeclNameLexeme;
            var specNameLexeme = hoistedNameLexeme + "Spec";

            var viewDeclUserCodeDelegateInterfaceDeclLexeme = "ViewDeclarationUserCodeDelegate";
            var viewDeclUserCodeDelegateInterfaceDeclCreateMethodDeclLexeme = "create";
            var viewDeclUserCodeDelegateDataValueDeclLexeme = "HOOK";



            // [dho] rename instances of the component so it is unique now in global scope where it was hoisted to (another file) - 24/06/19
            {
                var encScopeStartNode = LanguageSemantics.Java.GetEnclosingScopeStart(ast, node.Node, token);

                var startScope = new Scope(encScopeStartNode);

                var references = LanguageSemantics.Java.GetUnqualifiedReferenceMatches(session, ast, startScope.Subject, startScope, viewDeclNameLexeme, token);

                foreach(var reference in references)
                {
                    ASTNodeHelpers.RefactorName(ast, reference, hoistedNameLexeme);
                }
            }


            var lithoSpecComponent = NodeFactory.Component(ast, node.Origin, specNameLexeme);
            {
                var packageIdentifier = Sempiler.Bundling.BundlerHelpers.PackageIdentifier(artifact);
                var packageHack = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $"package {packageIdentifier};");

                // [dho] this interface is used to pass a lambda from the original view declaration location, to the spec file
                // without having to worry about qualified names and visibility of nested symbols - 21/06/19
                var viewDeclDelegateInterface = NodeFactory.InterfaceDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                {
                    ASTHelpers.Connect(ast, viewDeclDelegateInterface.ID, new[] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), viewDeclUserCodeDelegateInterfaceDeclLexeme).Node
                    }, SemanticRole.Name);

                    ASTHelpers.Connect(ast, viewDeclDelegateInterface.ID, new[] {
                         NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation),
                            // [dho] TODO parameters for the view declaration should go in this signature - 21/06/19 
                            $"com.facebook.litho.Component {viewDeclUserCodeDelegateInterfaceDeclCreateMethodDeclLexeme}(com.facebook.litho.ComponentContext context);").Node
                    }, SemanticRole.Member);
                }


                // class
                var lithoSpecObjectType = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                {
                    var layoutSpecAnnotation = NodeFactory.Annotation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        ASTHelpers.Connect(ast, layoutSpecAnnotation.ID, new[] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "com.facebook.litho.annotations.LayoutSpec").Node
                    }, SemanticRole.Operand);

                        ASTHelpers.Connect(ast, lithoSpecObjectType.ID, new[] { layoutSpecAnnotation.Node }, SemanticRole.Annotation);
                    }

                    // [dho] litho class name has to end in `Spec`- 17/06/19
                    var specName = NodeFactory.Identifier(ast, n.Origin, ASTNodeFactory.Identifier(ast, (DataNode<string>)node.Name).Lexeme + "Spec");

                    ASTHelpers.Connect(ast, lithoSpecObjectType.ID, new[] { specName.Node }, SemanticRole.Name);

                    ASTHelpers.Connect(ast, lithoSpecObjectType.ID, node.Meta, SemanticRole.Meta);

                    var lithoViewDeclarationUserCodeDelegate = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation),
                        // [dho] HACK using an array because the var itself has to be `static final` otherwise javac complains - 21/06/19 
                        $"public static final {viewDeclUserCodeDelegateInterfaceDeclLexeme}[] {viewDeclUserCodeDelegateDataValueDeclLexeme} = new {viewDeclUserCodeDelegateInterfaceDeclLexeme}[1];");

                    var onCreateLayoutMethodDecl = NodeFactory.MethodDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        // [dho] make the class static - 17/06/19
                        var staticFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Transformation),
                            MetaFlag.Static
                        );

                        ASTHelpers.Connect(ast, onCreateLayoutMethodDecl.ID, new[] { staticFlag.Node }, SemanticRole.Meta);


                        var onCreateLayoutAnnotation = NodeFactory.Annotation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                        {
                            ASTHelpers.Connect(ast, onCreateLayoutAnnotation.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "com.facebook.litho.annotations.OnCreateLayout").Node
                        }, SemanticRole.Operand);

                            ASTHelpers.Connect(ast, onCreateLayoutMethodDecl.ID, new[] { onCreateLayoutAnnotation.Node }, SemanticRole.Annotation);
                        }


                        ASTHelpers.Connect(ast, onCreateLayoutMethodDecl.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "onCreateLayout").Node
                        }, SemanticRole.Name);


                        var onCreateLayoutReturnType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                        {
                            ASTHelpers.Connect(ast, onCreateLayoutReturnType.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "com.facebook.litho.Component").Node
                        }, SemanticRole.Name);

                            ASTHelpers.Connect(ast, onCreateLayoutMethodDecl.ID, new[] { onCreateLayoutReturnType.Node }, SemanticRole.Type);
                        }



                        //////////////////
                        /// PARAMETERS ///
                        //////////////////

                        {
                            // [dho] either has 0 arity, or a single entity destructuring parameter - 14/06/19
                            var parameters = node.Parameters;

                            if (parameters.Length > 0)
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Error, $"View declaration parameters are not yet supported", parameters[0])
                                    {
                                        Hint = GetHint(parameters[0].Origin),
                                        Tags = DiagnosticTags
                                    }
                                );

                                return result;
                            }

                            // if (parameters.Length == 1)
                            // {
                            //     var param = ASTNodeFactory.ParameterDeclaration(ast, parameters[0]);

                            //     // [dho] `Foo({ x, y, z} : Bar)`
                            //     if(param.Name?.Kind == SemanticKind.EntityDestructuring && param.Type != null)
                            //     {




                            //     }
                            //     else
                            //     {
                            //         error();
                            //     }
                            // }
                            // else if(parameters.Length > 0)
                            // {
                            //     error();
                            // }
                        }



                        var oldParams = node.Parameters;
                        // [dho] + 1 because we need to add the context param - 16/06/19
                        var synthesizedParamCount = 1;
                        var newParams = new Node[synthesizedParamCount + oldParams.Length];
                        {
                            // [dho] `final Context context` - 16/06/19
                            var contextParam = NodeFactory.ParameterDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                            {
                                ASTHelpers.Connect(ast, contextParam.ID, new[] {
                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "context").Node
                            }, SemanticRole.Name);

                                var contextParamType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                                {
                                    ASTHelpers.Connect(ast, contextParamType.ID, new[] {
                                    NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "com.facebook.litho.ComponentContext").Node
                                }, SemanticRole.Name);

                                    ASTHelpers.Connect(ast, contextParam.ID, new[] { contextParamType.Node }, SemanticRole.Type);
                                }

                                // [dho] make the parameter constant (`final`) - 16/06/19
                                var constantFlag = NodeFactory.Meta(ast, new PhaseNodeOrigin(PhaseKind.Transformation), MetaFlag.Constant);
                                {
                                    ASTHelpers.Connect(ast, contextParam.ID, new[] { constantFlag.Node }, SemanticRole.Meta);
                                }

                                newParams[0] = contextParam.Node;
                            }

                            // [dho] append the rest of the parameters - 16/06/19
                            for (int i = 0; i < oldParams.Length; ++i)
                            {
                                var parameter = oldParams[i];

                                // [dho] make the parameter constant (`final`). NOTE this means Java will let it appear in closures too - 16/06/19
                                var constantFlag = NodeFactory.Meta(ast, new PhaseNodeOrigin(PhaseKind.Transformation), MetaFlag.Constant);
                                {
                                    ASTHelpers.Connect(ast, parameter.ID, new[] { constantFlag.Node }, SemanticRole.Meta);
                                }

                                // [dho] add the Litho `@Prop` annotation - 16/06/19
                                var propAnnotation = NodeFactory.Annotation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                                {
                                    ASTHelpers.Connect(ast, propAnnotation.ID, new[] {
                                    NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "Prop").Node
                                }, SemanticRole.Operand);

                                    ASTHelpers.Connect(ast, parameter.ID, new[] { propAnnotation.Node }, SemanticRole.Annotation);
                                }

                                newParams[synthesizedParamCount + i] = parameter;
                            }

                            ASTHelpers.Connect(ast, onCreateLayoutMethodDecl.ID, newParams, SemanticRole.Parameter);
                        }


                        var body = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                        {
                            ASTHelpers.Connect(ast, body.ID, new[] {
                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation),
$@"try
{{
    // String key = context.getKey();
    // _X_startComponentScope(key);

    return {viewDeclUserCodeDelegateDataValueDeclLexeme}[0].{viewDeclUserCodeDelegateInterfaceDeclCreateMethodDeclLexeme}(context);
}}
finally
{{
    // _X_endComponentScope(key);
}}"
                            ).Node
                        }, SemanticRole.Content);

                            ASTHelpers.Connect(ast, onCreateLayoutMethodDecl.ID, new[] { body.Node }, SemanticRole.Body);
                        }


                    }

                    ASTHelpers.Connect(ast, lithoSpecObjectType.ID, new[] {
                        viewDeclDelegateInterface.Node,
                        lithoViewDeclarationUserCodeDelegate.Node,
                        onCreateLayoutMethodDecl.Node
                    }, SemanticRole.Member);
                }

                ASTHelpers.Connect(ast, lithoSpecComponent.ID, new[] {
                    packageHack.Node,
                    lithoSpecObjectType.Node }, SemanticRole.None);
            }


            {
                var dv = NodeFactory.Assignment(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                {
                    ASTHelpers.Connect(ast, dv.ID, new[] {
                        NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation),
                            $"{specNameLexeme}.{viewDeclUserCodeDelegateDataValueDeclLexeme}[0]").Node
                    }, SemanticRole.Storage);


                    var userCodeDelegateLambdaDecl = NodeFactory.LambdaDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        // [dho] `final Context context` - 17/06/19
                        var contextParam = NodeFactory.ParameterDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                        {
                            ASTHelpers.Connect(ast, contextParam.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "context").Node
                        }, SemanticRole.Name);

                            var contextParamType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                            {
                                ASTHelpers.Connect(ast, contextParamType.ID, new[] {
                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "com.facebook.litho.ComponentContext").Node
                            }, SemanticRole.Name);

                                ASTHelpers.Connect(ast, contextParam.ID, new[] { contextParamType.Node }, SemanticRole.Type);
                            }

                            // [dho] make the parameter constant (`final`) - 16/06/19
                            var constantFlag = NodeFactory.Meta(ast, new PhaseNodeOrigin(PhaseKind.Transformation), MetaFlag.Constant);
                            {
                                ASTHelpers.Connect(ast, contextParam.ID, new[] { constantFlag.Node }, SemanticRole.Meta);
                            }

                            ASTHelpers.Connect(ast, userCodeDelegateLambdaDecl.ID, new[] { contextParam.Node }, SemanticRole.Parameter);
                        }


                        var body = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                        {
                            var b = node.Body;

                            if (b != null)
                            {
                                foreach (var explicitExit in LanguageSemantics.Java.GetExplicitExits(session, ast, b, token))
                                {
                                    var exitValue = default(Node);

                                    if (explicitExit.Kind == SemanticKind.FunctionTermination)
                                    {
                                        exitValue = ASTNodeFactory.FunctionTermination(ast, explicitExit).Value;
                                    }
                                    else
                                    {
                                        exitValue = explicitExit;
                                    }

                                    if (exitValue != null)
                                    {
                                        // [dho] append `.build()` onto the (assumed) ViewConstruction - 17/06/19
                                        // [dho] TODO computed type checking rather than just assume return value type is correct!!! - 17/06/19
                                        var inv = NodeFactory.Invocation(ast, exitValue.Origin);
                                        {
                                            ASTHelpers.Replace(ast, exitValue.ID, new[] { inv.Node });

                                            var subject = NodeFactory.QualifiedAccess(ast, node.Origin);
                                            {
                                                ASTHelpers.Connect(ast, subject.ID, new[] { exitValue }, SemanticRole.Incident);

                                                ASTHelpers.Connect(ast, subject.ID, new[] {
                                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "build").Node
                                            }, SemanticRole.Member);

                                                ASTHelpers.Connect(ast, inv.ID, new[] { subject.Node }, SemanticRole.Subject);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // var v = ASTHelpers.QueryEdgeNodes(ast, returnStatement.ID, SemanticRole.Value);

                                        result.AddMessages(
                                            new NodeMessage(MessageKind.Error, $"View declaration must return a view", exitValue)
                                            {
                                                Hint = GetHint(exitValue.Origin),
                                                Tags = DiagnosticTags
                                            }
                                        );
                                    }
                                }

                                ASTHelpers.Connect(ast, body.ID, new[] { b }, SemanticRole.Content);
                            }
                            else
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Error, $"View declaration must return a view", node)
                                    {
                                        Hint = GetHint(node.Origin),
                                        Tags = DiagnosticTags
                                    }
                                );
                            }

                            ASTHelpers.Connect(ast, userCodeDelegateLambdaDecl.ID, new[] { body.Node }, SemanticRole.Body);
                        }
                    }

                    ASTHelpers.Connect(ast, dv.ID, new[] { userCodeDelegateLambdaDecl.Node }, SemanticRole.Value);
                }

                // [dho] CLEANUP HACK we do not have a Node type for `static { ... }` so just using code constants
                // in a sequence - 21/06/19
                ASTHelpers.Replace(ast, node.ID, new[] {
                    NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "static {").Node,
                    dv.Node,
                    NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "}").Node
                });
            }




            /*
                static {
                    BarSpec.hook[0] = (context) -> {

                        final com.facebook.litho.widget.Text.Builder node_1561125972623_$32=com.facebook.litho.widget.Text.create(context).textSizeDip(40).text("Hello Wdsfssfdfsorld");

                        final com.facebook.litho.Column.Builder node_1561125972624_$40=com.facebook.litho.Column.create(context);
                        node_1561125972624_$40.child(node_1561125972623_$32);
                        return node_1561125972624_$40.build();
                    };
                }
                
             */







            ASTHelpers.Connect(ast, ASTHelpers.GetRoot(ast).ID, new[] { /* result.Value =*/ lithoSpecComponent.Node }, SemanticRole.Component);

            return result;
        }

        protected override Result<object> TransformViewConstruction(Session session, Artifact artifact, RawAST ast, ViewConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var replacementNodes = new List<Node>();

            var dataValueDeclNameLexeme = result.AddMessages(
                PopulateReplacementNodesForViewConstruction(session, ast, node, replacementNodes, token)
            );

            if (!HasErrors(result))
            {
                // [dho] the code we generated has to go in the first non-value position
                // in the code.. eg. `return <View>...</View>` would be illegal as `return final Component foo = ...;`,
                // so it has to be reordered to become `final Component foo = ...; return foo;` - 20/06/19
                {
                    var focus = node.Node;

                    while (LanguageSemantics.Java.IsValueExpression(ast, focus))
                    {
                        focus = ASTHelpers.GetPosition(ast, focus.ID).Parent;
                    }

                    // var b = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    // ASTHelpers.Connect(ast, b.ID, replacementNodes.ToArray(), SemanticRole.Content);


                    if (focus.ID != node.ID)
                    {
                        var pos = ASTHelpers.GetPosition(ast, focus.ID);

                        ASTHelpers.Replace(ast, node.ID,
                            new[] { NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), dataValueDeclNameLexeme).Node });

                        ASTHelpers.InsertBefore(ast, pos.Node.ID, replacementNodes.ToArray(), pos.Role);
                    }
                    else
                    {
                        ASTHelpers.Replace(ast, node.ID, replacementNodes.ToArray());
                    }

                    // result.Value = b.Node;
                }
            }

            return result;
        }

        private Result<string> PopulateReplacementNodesForViewConstruction(Session session, RawAST ast, ViewConstruction node, List<Node> replacementNodes, CancellationToken token)
        {
            var result = new Result<string>();

            var childValues = new List<Node>();

            foreach (var child in ASTHelpers.QueryLiveEdgeNodes(ast, node.ID, SemanticRole.Child))
            {
                if (child == null) continue;

                if (token.IsCancellationRequested) return result;

                var cv = default(Node);

                if (child.Kind == SemanticKind.ViewConstruction)
                {
                    var childViewConstruction = ASTNodeFactory.ViewConstruction(ast, child);

                    result.AddMessages(
                        PopulateReplacementNodesForViewConstruction(session, ast, childViewConstruction, replacementNodes, token)
                    );

                    // [dho] we will use the identifier that refers to the child node view construction code
                    // and have that passed to the parent inside the `.child(...)` call - 16/06/19
                    cv = NodeFactory.Identifier(ast, childViewConstruction.Origin, child.ID).Node;
                }
                else
                {
                    cv = child;
                }

                childValues.Add(cv);
            }


            var n = node.Name;
            // if(n?.Kind != SemanticKind.Identifier)
            // {
            //     result.AddMessages(
            //         new NodeMessage(MessageKind.Error, $"Expected view construction name to be identifier but found '{n?.Kind}'", n ?? node.Node)
            //         {
            //             Hint = GetHint((n ?? node.Node).Origin),
            //             Tags = DiagnosticTags
            //         }
            //     );

            //     return result;
            // }

            // var replacementNodes = new List<Node>();

            // var name = ASTNodeFactory.Identifier(ast, (DataNode<string>)n);

            // [dho] `Foo.create(context)` - 15/06/19
            var lithoView = NodeFactory.Invocation(ast, n.Origin);
            {
                // [dho] `Foo.create` - 15/06/19
                var subject = NodeFactory.QualifiedAccess(ast, node.Origin);
                {
                    ASTHelpers.Connect(ast, subject.ID, new[] { n }, SemanticRole.Incident);

                    ASTHelpers.Connect(ast, subject.ID, new[] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "create").Node
                    }, SemanticRole.Member);
                }
                ASTHelpers.Connect(ast, lithoView.ID, new[] { subject.Node }, SemanticRole.Subject);

                // [dho] `(context)` - 15/06/19 
                ASTHelpers.Connect(ast, lithoView.ID, new[] {
                    NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "context").Node
                }, SemanticRole.Argument);
            }

            // [dho] for every property we add a `.foo(...)` to the builder chain - 15/06/19
            foreach (var property in node.Properties)
            {
                if (property == null) continue;

                if (token.IsCancellationRequested) return result;

                var propName = default(Identifier);
                var propValue = default(Node);
                {
                    // [dho] `x` - 15/06/19
                    if (property.Kind == SemanticKind.Identifier)
                    {
                        propName = ASTNodeFactory.Identifier(ast, (DataNode<string>)property);
                        propValue = NodeFactory.Identifier(ast, property.Origin, propName.Lexeme).Node;
                    }
                    // [dho] `x=y` - 15/06/19
                    else if (property.Kind == SemanticKind.KeyValuePair)
                    {
                        var kv = ASTNodeFactory.KeyValuePair(ast, property);

                        var key = kv.Key;

                        if (key.Kind == SemanticKind.Identifier)
                        {
                            propName = ASTNodeFactory.Identifier(ast, (DataNode<string>)key);
                            propValue = kv.Value;
                        }
                        else
                        {
                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"Expected view property name to be identifier but found '{key.Kind}'", key)
                                {
                                    Hint = GetHint(key.Origin),
                                    Tags = DiagnosticTags
                                }
                            );
                            continue;
                        }
                    }
                    else
                    {
                        // [dho] TODO spread destructuring could be this too - if we had type resolution we could do it - 16/06/19
                        result.AddMessages(
                            new NodeMessage(MessageKind.Error, $"Using '{property.Kind}' for view property is not yet supported", property)
                            {
                                Hint = GetHint(property.Origin),
                                Tags = DiagnosticTags
                            }
                        );
                        continue;
                    }
                }


                // [dho] conversion of raw property name and value to the equivalent
                // in Litho - 20/06/19
                {
                    var rawPropNameLexeme = propName.Lexeme;

                    switch (rawPropNameLexeme)
                    {
                        case "fontSize":
                            propName = NodeFactory.Identifier(ast, propName.Origin, "textSizeDip");
                            break;

                        case "height":
                        case "width":
                            // [dho] eg. `width` => `widthDip` - 20/06/19
                            propName = NodeFactory.Identifier(ast, propName.Origin, rawPropNameLexeme + "Dip");
                            break;

                        case "style":
                            {
                                // [dho] TODO this will be a lot of work!! - 20/06/19
                            }
                            break;
                    }
                }


                {
                    var subject = NodeFactory.QualifiedAccess(ast, node.Origin);
                    {
                        ASTHelpers.Connect(ast, subject.ID, new[] { lithoView.Node }, SemanticRole.Incident);

                        ASTHelpers.Connect(ast, subject.ID, new[] { propName.Node }, SemanticRole.Member);
                    }

                    lithoView = NodeFactory.Invocation(ast, n.Origin);

                    ASTHelpers.Connect(ast, lithoView.ID, new[] { subject.Node }, SemanticRole.Subject);

                    ASTHelpers.Connect(ast, lithoView.ID, new[] { propValue }, SemanticRole.Argument);
                }
            }


            // [dho] create `final Foo.Builder node123 = Foo.create(context).hello(1).world();` - 16/06/19
            var dataValueDeclNameLexeme = result.Value = node.ID;
            {
                var dataValueDecl = NodeFactory.DataValueDeclaration(ast, node.Origin);

                var constantFlag = NodeFactory.Meta(ast, new PhaseNodeOrigin(PhaseKind.Transformation), MetaFlag.Constant);
                {
                    ASTHelpers.Connect(ast, dataValueDecl.ID, new[] { constantFlag.Node }, SemanticRole.Meta);
                }

                var dataValueDeclName = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), dataValueDeclNameLexeme);
                {
                    ASTHelpers.Connect(ast, dataValueDecl.ID, new[] { dataValueDeclName.Node }, SemanticRole.Name);
                }

                var dataValueDeclType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                {
                    var identifierList = result.AddMessages(ASTNodeHelpers.ToIdentifierList(ast, n));

                    if(HasErrors(result) || token.IsCancellationRequested) return result;
                    
                    var items = new Node[identifierList.Count + 1];
                    {
                        for(int i = 0; i < identifierList.Count; ++i)
                        {
                            var lexeme = identifierList[i].Lexeme;

                            items[i] = NodeFactory.Identifier(ast, n.Origin, lexeme).Node;
                        }

                        items[items.Length - 1] = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "Builder").Node;
                    }

                    ASTHelpers.Connect(ast, dataValueDeclType.ID, new[] { ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, n.Origin, items) }, SemanticRole.Name);

                    ASTHelpers.Connect(ast, dataValueDecl.ID, new[] { dataValueDeclType.Node }, SemanticRole.Type);
                }

                ASTHelpers.Connect(ast, dataValueDecl.ID, new[] { lithoView.Node }, SemanticRole.Initializer);


                replacementNodes.Add(dataValueDecl.Node);
            }


            // [dho] for every child we add a `node123.child(...);` node - 16/06/19
            foreach (var cv in childValues)
            {
                if (cv == null) continue;

                if (token.IsCancellationRequested) return result;

                var subject = NodeFactory.QualifiedAccess(ast, cv.Origin);
                {
                    ASTHelpers.Connect(ast, subject.ID, new[] {
                        NodeFactory.Identifier(ast, cv.Origin, dataValueDeclNameLexeme).Node
                    }, SemanticRole.Incident);

                    ASTHelpers.Connect(ast, subject.ID, new[] {
                        NodeFactory.Identifier(ast, cv.Origin, "child").Node
                    }, SemanticRole.Member);
                }

                var inv = NodeFactory.Invocation(ast, n.Origin);

                ASTHelpers.Connect(ast, inv.ID, new[] { subject.Node }, SemanticRole.Subject);

                ASTHelpers.Connect(ast, inv.ID, new[] { cv }, SemanticRole.Argument);

                replacementNodes.Add(inv.Node);
            }

            return result;
        }


        


    }
}