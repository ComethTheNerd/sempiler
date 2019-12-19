using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using Sempiler.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sempiler.Transformation
{
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

    // [dho] finds instances where the author is demonstrating the intent to define a view declaration,
    // and replaces that node with an actual `ViewDeclaration` node in the AST - 14/06/19
    public class ViewDeclarationIntentTransformer : ITransformer
    {
        protected readonly string[] DiagnosticTags;

        public ViewDeclarationIntentTransformer()
        {
            DiagnosticTags = new[] { "transformer", "view-declaration-intent" };
        }

        public Task<Diagnostics.Result<RawAST>> Transform(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<RawAST>();

            // var clonedAST = ast.Clone();

            var context = new Context
            {
                Artifact = artifact,
                Session = session,
                AST = ast//clonedAST
            };

            var root = ASTHelpers.GetRoot(/* clonedAST */ ast);

            result.AddMessages(TransformNode(session, root, context, token));

            // if (!HasErrors(result))
            // {
            //     result.Value = clonedAST;
            // }

            result.Value = ast;

            return Task.FromResult(result);
        }

        private Result<object> TransformNode(Session session, Node _node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var ast = context.AST;

            // var viewDecls = new List<ViewDeclaration>();

            ASTHelpers.TraversalDelegate del = node =>
            {
                var shouldExploreChildren = true;

                if (node?.Kind == SemanticKind.FunctionDeclaration)
                {
                    var fnDecl = ASTNodeFactory.FunctionDeclaration(ast, node);

                    if (result.AddMessages(IsViewDeclarationIntent(session, ast, fnDecl, token)))
                    {
                        // [dho] we replace the function declaration with a view declaration - 14/06/19
                        result.AddMessages(ReplaceWithViewDeclaration(session, ast, fnDecl, token));

                        // [dho] tell the traverser to NOT bother exploring this node any further - 14/06/19
                        shouldExploreChildren = false;
                    }
                }
                
                return shouldExploreChildren;
            };

            ASTHelpers.PreOrderLiveTraversal(ast, _node, del, token);

            return result;
        }

        private static Result<object> ReplaceWithViewDeclaration(Session session, RawAST ast, FunctionDeclaration node, CancellationToken token)
        {
            var result = new Result<object>();

            var viewDecl = NodeFactory.ViewDeclaration(ast, node.Origin);

            ASTHelpers.Connect(ast, viewDecl.ID, new [] { node.Name }, SemanticRole.Name);
            ASTHelpers.Connect(ast, viewDecl.ID, node.Template, SemanticRole.Template);
            ASTHelpers.Connect(ast, viewDecl.ID, node.Parameters, SemanticRole.Parameter);
            ASTHelpers.Connect(ast, viewDecl.ID, new [] { node.Body }, SemanticRole.Body);
            
            {
                var previews = new List<Node>();
                var annotations = new List<Node>();

                foreach(var a in node.Annotations)
                {
                    System.Diagnostics.Debug.Assert(a.Kind == SemanticKind.Annotation);
                    var annotation = ASTNodeFactory.Annotation(ast, a);

                    var expression = annotation.Expression;

                    if(expression.Kind == SemanticKind.Invocation)
                    {
                        var inv = ASTNodeFactory.Invocation(ast, expression);

                        if(ASTNodeHelpers.IsIdentifierWithName(ast, inv.Subject, "preview"))
                        {
                            var arguments = inv.Arguments;

                            if(arguments.Length == 1 && arguments[0].Kind == SemanticKind.ViewConstruction)
                            {
                                // var viewConstruction = ASTNodeFactory.ViewConstruction(ast, arguments[0]);
                                previews.Add(arguments[0]);
                            }
                            else
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Warning, $"Preview annotation for View Declaration has unsupported arguments and will be ignored", a)
                                    {
                                        Hint = GetHint(a.Origin)
                                    }
                                );
                            }

                            continue;
                        }
                    }
                    else if(ASTNodeHelpers.IsIdentifierWithName(ast, expression, "preview"))
                    {
                        result.AddMessages(
                            new NodeMessage(MessageKind.Warning, $"Preview annotation for View Declaration is missing argument list and will be ignored", a)
                            {
                                Hint = GetHint(a.Origin)
                            }
                        );

                        continue;
                    }

                    annotations.Add(a);
                }

                ASTHelpers.Connect(ast, viewDecl.ID, previews.ToArray(), SemanticRole.ViewPreview);
                ASTHelpers.Connect(ast, viewDecl.ID, annotations.ToArray(), SemanticRole.Annotation);
            }


            ASTHelpers.Connect(ast, viewDecl.ID, node.Meta, SemanticRole.Meta);            

            ASTHelpers.Replace(ast, node.ID, new [] { viewDecl.Node });

            return result;

            // var parameters = node.Parameters;

            // // [dho] if the node passed the `IsViewFactoryDeclaration` test then 
            // // we assume it to have 0 or 1 parameters - 14/06/19
            // System.Diagnostics.Debug.Assert(parameters.Length < 2);

            // if (parameters.Length == 1)
            // {
            //     var parameter = ASTNodeFactory.ParameterDeclaration(ast, parameters[0]);

            //     var parameterName = parameter.Name;
            //     // [dho] if the node passed the `IsViewFactoryDeclaration` test then 
            //     // we assume the single parameter as an entity destructuring - 14/06/19
            //     System.Diagnostics.Debug.Assert(parameterName?.Kind == SemanticKind.EntityDestructuring);

            //     var entityDestructuring = ASTNodeFactory.EntityDestructuring(ast, parameterName);


            //     var parameterType = parameter.Type;
            //     // [dho] if the node passed the `IsViewFactoryDeclaration` test then 
            //     // we assume the single parameter with a type of dynamic type reference - 14/06/19
            //     System.Diagnostics.Debug.Assert(parameterType?.Kind == SemanticKind.DynamicTypeReference);



            //     var needles = new Dictionary<string, Node>();

            //     foreach (var member in entityDestructuring.Members)
            //     {
            //         if (member?.Kind == SemanticKind.Identifier)
            //         {
            //             var memberName = ASTNodeFactory.Identifier(ast, (DataNode<string>)member).Lexeme;

            //             // [dho] entry will be populated by subsequent search - 14/06/19
            //             needles[memberName] = null;
            //         }
            //         else
            //         {
            //             error();
            //         }
            //     }

            //     if (result.AddMessages(ASTHelpers.LookUpMembersInTypeReference(ast, parameterType, needles)))
            //     {

            //         // sadads
            //         covert_parameters();
            //     }
            //     else
            //     {
            //         // [dho] if we could not find all the members we are after
            //         // then report an error - 14/06/19 
            //         error();
            //     }
            // }

            
        }


        private static Result<bool> IsViewDeclarationIntent(Session session, RawAST ast, FunctionDeclaration node, CancellationToken token)
        {
            var result = new Result<bool>() { Value = false };

            // var template = node.Template;

            // if (template.Length > 0) return result;


            var name = node.Name;

            // [dho] is named - 14/06/19
            if (name?.Kind == SemanticKind.Identifier)
            {
                var body = node.Body;

                // [dho] is implemented - 14/06/19
                if (body != null)
                {
                    var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;

                    var firstChar = lexeme[0];

                    // [dho] name starts with a capital letter - 14/06/19
                    if (firstChar >= 'A' && firstChar <= 'Z')
                    {
                        var type = node.Type;

                        if (type?.Kind == SemanticKind.NamedTypeReference)
                        {
                            var typeName = ASTNodeFactory.NamedTypeReference(ast, type).Name;

                            // [dho] has a return type of `View` - 14/06/19                
                            if (typeName?.Kind == SemanticKind.Identifier &&
                                ASTNodeFactory.Identifier(ast, (DataNode<string>)typeName).Lexeme == "View") // [dho] TODO CLEANUP hoist the name "View" - 14/06/19
                            {
                                // // [dho] either has 0 arity, or a single entity destructuring parameter - 14/06/19
                                // var parameters = node.Parameters;

                                // var isViewFactory = false;

                                // if (parameters.Length == 0)
                                // {
                                //     isViewFactory = true;
                                // }
                                // else if (parameters.Length == 1)
                                // {
                                //     var param = ASTNodeFactory.ParameterDeclaration(ast, parameters[0]);

                                //     // [dho] `Foo({ x, y, z} : Bar)`
                                //     isViewFactory = (param.Name?.Kind == SemanticKind.EntityDestructuring && param.Type != null);
                                // }


                                // if (!isViewFactory)
                                // {
                                //     // [dho] just warn the user if it looks like they meant to have a view factory here - 14/06/19
                                //     result.AddMessages(
                                //         new NodeMessage(MessageKind.Warning, $"This function may be intended as a view factory, but has unsupported parameters", node)
                                //         {
                                //             Hint = GetHint(node.Origin),
                                //             Tags = DiagnosticTags
                                //         }
                                //     );
                                // }

                                result.Value = true;//isViewFactory;
                            }
                        }

                        // var returnStatements = GetReturnStatements(session, ast, body, token);

                        // if(token.IsCancellationRequested) return false;

                        // var count = 0;

                        // foreach(var returnStatement in returnStatements)
                        // {
                        //     if(token.IsCancellationRequested) return false;

                        //     var value = returnStatement.Value;

                        //     // [dho] if we find a void return (ie. `return;`) this is not a ViewFactory - 14/06/19
                        //     if(value == null)
                        //     {
                        //         return false;
                        //     }

                        //     if(EvaluatesToViewConstruction(ast, value))
                        //     {
                        //         ++count;
                        //     }
                        // }

                        // // [dho] it is a ViewFactory if all return statements 
                        // // return a view construction - 14/06/19
                        // return count == returnStatements.Count;                   
                    }
                }
            }

            return result;
        }

        // private static bool EvaluatesToViewConstruction(RawAST ast, Node node)
        // {
        //     if (node.Kind == SemanticKind.ViewConstruction)
        //     {
        //         return true;
        //     }
        //     else if (node.Kind == SemanticKind.PredicateFlat)
        //     {
        //         var pred = ASTNodeFactory.PredicateFlat(ast, node);

        //         return EvaluatesToViewConstruction(ast, pred.TrueValue) ||
        //                     EvaluatesToViewConstruction(ast, pred.FalseValue);
        //     }
        //     else if (node.Kind == SemanticKind.Association)
        //     {
        //         var assoc = ASTNodeFactory.Association(ast, node);

        //         return EvaluatesToViewConstruction(ast, assoc.Subject);
        //     }

        //     return false;
        // }




    }
}