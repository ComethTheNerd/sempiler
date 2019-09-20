using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using Sempiler.Diagnostics;
using Sempiler.Languages;
using System.Collections.Generic;
using System.Threading;
namespace Sempiler.Transformation
{
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

    public class IOSSwiftUITransformer : ViewTransformer
    {
        // // [dho] this map translates source prop names to their corresponding
        // // SwiftUI names (init argument name or builder function name) - 30/06/19
        // private static readonly Dictionary<string, Dictionary<string, string>> SourcePropNameToSwiftUIName = new Dictionary<string, Dictionary<string, string>>
        // {          
        //     { "Button", new Dictionary<string, string> { { "action", "action" }, { "label", "label" } } },
        //     { "Text", new Dictionary<string, string> { { "text", "text" } } }
        // };

        // [dho] this map tells the code whether the name is an init argument (instead of builder function name)
        // and if so, whether it should have a label or not - 30/06/19
        public static readonly Dictionary<string, (string, bool)[]> SwiftUIViewInitArguments = new Dictionary<string, (string, bool)[]>
        {          
            // { "AnyView", new [] { ("", false) }},
            { "Button", new [] { ("action", true), ("label", true) } },
            { "EditButton", new (string,bool)[] { } },
            { "ForEach", new [] { ("data",false), ("content", true) }  },
            { "GeometryReader", new [] { ("content",false) }},
            { "HStack", new [] { ("alignment",true), ("spacing", true), ("content", true) } },
            { "Image", new [] {  ( "src", false ), ( "nsImage", true ), ( "systemName", true ), ( "uiImage", true ), ( "decorative", true ), ( "bundle", true ), ( "scale", true ), ( "label", true ) } },
            { "MenuButton", new [] { ("label", true), ("content", true) } },
            { "NavigationLink", new [] { ( "destination", true ), ( "in", true ), ( "label", true ) } },
            { "List", new [] { ("selection", false), ("content", false) } },
            { "PasteButton", new [] { ("supportedTypes", true), ("onTrigger", true) } },
            { "Path", new [] { ("string", true), ("ellipseIn", true), ("roundedRect", true), ("cornerRadius", true), ("cornerSize", true), ("style", true), ("path", false) }},
            { "PresentationLink", new [] { ( "destination", true ), ( "label", true ), } },
            { "ScrollView", new [] { ("isScrollEnabled", true), ("alwaysBounceHorizontal", true), ("alwaysBounceVertical", true), ("showsHorizontalIndicator", true), ("showsVerticalIndicator", true), ("content", true) }},
            { "Section", new [] { ("header", true), ("footer", true), ("content", true) } },
            { "Spacer", new [] { ("minLength", true) } },
            { "Stepper", new [] { ("label", false), ("value", true), ("in", true), ("step", true), ("onIncrement", true), ("onDecrement", true), ("onEditingChanged", true) } },
            { "Text", new [] { ( "text", false ), ( "verbatim", true ), ( "tableName", true ), ( "bundle", true ), ( "comment", true ) } },
            { "TextField", new [] { ("label", false), ("text", true), ("value", true), ("formatter", true), ("onEditingChanged", true), ("onCommit", true) } },
            { "Toggle", new[] { ("isOn", true), ("label", true) } }, 
            { "VStack", new [] { ("alignment",true), ("spacing", true), ("content", true) } },
            { "ZStack", new [] { ("alignment",true), ("content", true) } }
        };


        public static readonly Dictionary<string, string> SempilerViewNameSwiftUIAliases = new Dictionary<string, string>{
            { Sempiler.Core.SempilerPackageSymbols.View , "VStack" },
            { Sempiler.Core.SempilerPackageSymbols.Column, "VStack" },
            { Sempiler.Core.SempilerPackageSymbols.Row, "HStack" }
        };



        public IOSSwiftUITransformer() : base(new[] { "transformer", "ios-swiftui" })
        {
        }

        protected override Result<object> TransformViewDeclaration(Session session, RawAST ast, ViewDeclaration node, Context context, CancellationToken token)
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


            System.Diagnostics.Debug.Assert(node.Name.Kind == SemanticKind.Identifier);

            var viewDeclNameLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)node.Name).Lexeme;

            var parameters = node.Parameters;

            // [dho] because we will emit a struct in SwiftUI, we need to qualify all references
                // to the original ViewDeclaration parameters with `self.<name>` - 30/06/19
                {
                    for(int i = 0; i < parameters.Length; ++i)
                    {
                        var p = parameters[i];

                        System.Diagnostics.Debug.Assert(p.Kind == SemanticKind.ParameterDeclaration);

                        var parameter = ASTNodeFactory.ParameterDeclaration(ast, p);

                        var name = parameter.Name;
                    
                        var rawParamNameLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;

                        if(rawParamNameLexeme.StartsWith("$"))
                        {
                            // [dho] TODO CLEANUP in SwiftUI writing `$<name>` creates a binding reference
                            // so for now we are reserving that to avoid clashes with the generated Swift code - 30/06/19
                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"Parameter cannot start with '$'", p)
                                {
                                    Hint = GetHint(p.Origin),
                                    Tags = DiagnosticTags
                                }
                            );
                        }

                        var bodyScope = new Scope(node.Body);

                        bodyScope.Declarations[rawParamNameLexeme] = name;
                        // [dho] incase it is used as a binding - 30/06/19
                        // [dho] TODO CLEANUP having to do this..? - 30/06/19
                        bodyScope.Declarations[$"${rawParamNameLexeme}"] = name;
                    
                        // var parameter = bodyScope.Declarations[paramName];

                        QualifyParameterReferences(session, ast, bodyScope, node.Node, rawParamNameLexeme, token);
                        // [dho] incase it is used as a binding - 30/06/19
                        // [dho] TODO CLEANUP having to do this..? - 30/06/19
                        QualifyParameterReferences(session, ast, bodyScope, node.Node, $"${rawParamNameLexeme}", token);
                    }
                }

            // struct
            var swiftUIStructDecl = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, swiftUIStructDecl.ID, node.Meta, SemanticRole.Meta);
                ASTHelpers.Connect(ast, swiftUIStructDecl.ID, node.Template, SemanticRole.Template);
                
                // [dho] set the flag to say this is a struct (value type), not a class (ref type) - 30/06/19
                {
                    var valueTypeFlag = NodeFactory.Meta(
                        ast,
                        new PhaseNodeOrigin(PhaseKind.Transformation),
                        MetaFlag.ValueType
                    );

                    ASTHelpers.Connect(ast, swiftUIStructDecl.ID, new[] { valueTypeFlag.Node }, SemanticRole.Meta);
                }

                // [dho] has to implement the `View` protocol - 29/06/19
                {
                    var protocolType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                    var protocolTypeName = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "View");

                    ASTHelpers.Connect(ast, protocolType.ID, new[] { protocolTypeName.Node }, SemanticRole.Name);

                    ASTHelpers.Connect(ast, swiftUIStructDecl.ID, new[] { protocolType.Node }, SemanticRole.Interface);
                }


                var structName = NodeFactory.Identifier(ast, n.Origin, viewDeclNameLexeme);
                ASTHelpers.Connect(ast, swiftUIStructDecl.ID, new[] { structName.Node }, SemanticRole.Name);

                //////////////////
                /// PARAMETERS ///
                //////////////////
                

                var structMembers = new Node[parameters.Length + 1 /* for body property */];

                



                {
                    // each parameter needs to be converted to a field
                    for(int i = 0; i < parameters.Length; ++i)
                    {
                        var p = parameters[i];

                        if(p.Kind == SemanticKind.ParameterDeclaration)
                        {
                            var parameter = ASTNodeFactory.ParameterDeclaration(ast, p);

                            var name = parameter.Name;
                            var type = parameter.Type;
                            var @default = parameter.Default;


                            if(name.Kind != SemanticKind.Identifier)
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Error, $"View declaration parameter name has unsupported kind '{name.Kind}'", name)
                                    {
                                        Hint = GetHint(name.Origin),
                                        Tags = DiagnosticTags
                                    }
                                );
                            }
                            else if(ASTNodeHelpers.IsIdentifierWithName(ast, name, "body"))
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Error, $"View declaration parameters targeting SwiftUI can not use the name 'body'", name)
                                    {
                                        Hint = GetHint(name.Origin),
                                        Tags = DiagnosticTags
                                    }
                                );
                            }

                            var field = NodeFactory.FieldDeclaration(ast, p.Origin);

                            ASTHelpers.Connect(ast, field.ID, new[] { name }, SemanticRole.Name);

                            if(type != null)
                            {
                                ASTHelpers.Connect(ast, field.ID, new[] { type }, SemanticRole.Type);
                            }

                            if(@default != null)
                            {
                                ASTHelpers.Connect(ast, field.ID, new[] { @default }, SemanticRole.Initializer);
                            }

                            ASTHelpers.Connect(ast, field.ID, parameter.Annotations, SemanticRole.Annotation);

                            ASTHelpers.Connect(ast, field.ID, parameter.Modifiers, SemanticRole.Modifier);

                            ASTHelpers.Connect(ast, field.ID, parameter.Meta, SemanticRole.Meta);


                            structMembers[i] = field.Node;
                        }
                        else
                        {
                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"View declaration parameter has unsupported kind '{p.Kind}'", p)
                                {
                                    Hint = GetHint(p.Origin),
                                    Tags = DiagnosticTags
                                }
                            );
                        }
                    }
                }


                var bodyField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                {
                    {
                        var publicFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Bundling),
                            MetaFlag.WorldVisibility
                        );

                        ASTHelpers.Connect(ast, bodyField.ID, new[] { publicFlag.Node }, SemanticRole.Meta);
                    }

                    ASTHelpers.Connect(ast, bodyField.ID, new[] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "body").Node
                    }, SemanticRole.Name);

                    var bodyFieldType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        ASTHelpers.Connect(ast, bodyFieldType.ID, new [] { 
                            NodeFactory.Modifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "some").Node
                        }, SemanticRole.Modifier);


                        ASTHelpers.Connect(ast, bodyFieldType.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "View").Node
                        }, SemanticRole.Name);

                    }
                    ASTHelpers.Connect(ast, bodyField.ID, new[] { bodyFieldType.Node }, SemanticRole.Type);

                    var bodyFieldInitializer = NodeFactory.LambdaDeclaration(ast, node.Origin);
                    {
                        ASTHelpers.Connect(ast, bodyFieldInitializer.ID, new[] { node.Body }, SemanticRole.Body);
                    }
                    ASTHelpers.Connect(ast, bodyField.ID, new[] { bodyFieldInitializer.Node }, SemanticRole.Initializer);


                    structMembers[structMembers.Length - 1] = bodyField.Node;
                }

                ASTHelpers.Connect(ast, swiftUIStructDecl.ID, structMembers, SemanticRole.Member);
            }

            {
                var previewStructDecl = default(Node);
                var previews = node.Previews;

                if(previews.Length > 0)
                {
                    previewStructDecl = result.AddMessages(
                        GeneratePreview(session, ast, node, viewDeclNameLexeme, previews[previews.Length - 1], context, token)
                    );

                    for(int i = 0; i < previews.Length - 1; ++i)
                    {
                        var ignoredPreview = previews[i];

                        result.AddMessages(new NodeMessage(MessageKind.Warning, $"Cannot define multiple previews", ignoredPreview)
                        {
                            Hint = GetHint(ignoredPreview.Origin),
                            Tags = DiagnosticTags
                        });
                    }
                }
                // else if(parameters.Length == 0 || AllParameterValuesImplicit())
                // {
                        // generate preview
                // }


                var replacementNodes = default(Node[]);
     
                if(previewStructDecl != null)
                {
                    replacementNodes = new [] { swiftUIStructDecl.Node, previewStructDecl };
                }
                else
                {
                    replacementNodes = new [] { swiftUIStructDecl.Node };
                }

                ASTHelpers.Replace(ast, node.ID, replacementNodes);
            }

            return result;
        }

        private Result<Node> GeneratePreview(Session session, RawAST ast, ViewDeclaration node, string viewDeclNameLexeme, Node preview, Context context, CancellationToken token)
        {
            var result = new Result<Node>();
     
            var previewStructDecl = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, previewStructDecl.ID, node.Meta, SemanticRole.Meta);
                
                // [dho] set the flag to say this is a struct (value type), not a class (ref type) - 05/07/19
                {
                    var valueTypeFlag = NodeFactory.Meta(
                        ast,
                        new PhaseNodeOrigin(PhaseKind.Transformation),
                        MetaFlag.ValueType
                    );

                    ASTHelpers.Connect(ast, previewStructDecl.ID, new[] { valueTypeFlag.Node }, SemanticRole.Meta);
                }

                // [dho] has to implement the `PreviewProvider` protocol - 05/07/19
                {
                    var protocolType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                    var protocolTypeName = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "PreviewProvider");

                    ASTHelpers.Connect(ast, protocolType.ID, new[] { protocolTypeName.Node }, SemanticRole.Name);

                    ASTHelpers.Connect(ast, previewStructDecl.ID, new[] { protocolType.Node }, SemanticRole.Interface);
                }


                var structName = NodeFactory.Identifier(ast, preview.Origin, $"{viewDeclNameLexeme}_Preview");
                ASTHelpers.Connect(ast, previewStructDecl.ID, new[] { structName.Node }, SemanticRole.Name);

                var previewsField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                {
                    {
                        var publicFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Bundling),
                            MetaFlag.WorldVisibility
                        );

                        var staticFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Bundling),
                            MetaFlag.Static
                        );

                        ASTHelpers.Connect(ast, previewsField.ID, new[] { publicFlag.Node, staticFlag.Node }, SemanticRole.Meta);
                    }

                    ASTHelpers.Connect(ast, previewsField.ID, new[] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "previews").Node
                    }, SemanticRole.Name);

                    var previewsFieldType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        ASTHelpers.Connect(ast, previewsFieldType.ID, new [] { 
                            NodeFactory.Modifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "some").Node
                        }, SemanticRole.Modifier);


                        ASTHelpers.Connect(ast, previewsFieldType.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "View").Node
                        }, SemanticRole.Name);

                    }
                    ASTHelpers.Connect(ast, previewsField.ID, new[] { previewsFieldType.Node }, SemanticRole.Type);

                
                    var previewsFieldInitializer = NodeFactory.LambdaDeclaration(ast, node.Origin);
                    {
                        ASTHelpers.Connect(ast, previewsFieldInitializer.ID, new[] { preview }, SemanticRole.Body);
                    }
                    ASTHelpers.Connect(ast, previewsField.ID, new[] { previewsFieldInitializer.Node }, SemanticRole.Initializer);
                }

                ASTHelpers.Connect(ast, previewStructDecl.ID, new [] { previewsField.Node }, SemanticRole.Member);
            }


            result.Value = previewStructDecl.Node;




            return result;
        }

        private void QualifyParameterReferences(Session session, RawAST ast, Scope scope, Node viewDecl, string name, CancellationToken token)
        {
            var references = LanguageSemantics.Swift.GetUnqualifiedReferenceMatches(session, ast, viewDecl, scope, name, token);

            foreach(var reference in references)
            {
                // [dho] replace the unqualified reference to the parameter with a 
                // qualified access of the form `self.<name>` - 30/06/19
                var qa = NodeFactory.QualifiedAccess(ast, reference.Origin);
                {
                    var incident = NodeFactory.IncidentContextReference(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    var member = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), name);

                    ASTHelpers.Connect(ast, qa.ID, new [] { incident.Node }, SemanticRole.Incident);
                    ASTHelpers.Connect(ast, qa.ID, new [] { member.Node }, SemanticRole.Member);
                }
                ASTHelpers.Replace(ast, reference.ID, new [] { qa.Node });
            }
        }

        protected override Result<object> TransformViewConstruction(Session session, RawAST ast, ViewConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();
            
            result.AddMessages(
                CreateSwiftUIViewConstruction(session, ast, node, context, token)
            );

            return result;
        }

        private Result<Node> CreateSwiftUIViewConstruction(Session session, RawAST ast, ViewConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<Node>();

            var construction = NodeFactory.NamedTypeConstruction(ast, node.Origin);
            {
                // [dho] this here because depending on the properties, we may not return the `construction`,
                // but a qualified access containing it, but we want to preserve the `construction` variable name
                // in this scope untainted in any case - 30/06/19
                result.Value = construction.Node;

                var name = node.Name;

                System.Diagnostics.Debug.Assert(name != null);

                foreach(var key in SempilerViewNameSwiftUIAliases.Keys)
                {
                    if(ASTNodeHelpers.IsIdentifierWithName(ast, name, key))
                    {
                        var alias = SempilerViewNameSwiftUIAliases[key];

                        // [dho] going to default to using VStack for a default View - 29/06/19
                        name = ASTNodeHelpers.RefactorName(ast, name, alias).Node;

                        break;
                    }
                }




                var properties = node.Properties;
                var children = ASTHelpers.QueryEdgeNodes(ast, node.ID, SemanticRole.Child);
                var hasChildren = children.Length > 0;

                var arguments = default(List<Node>);

                // var sourcePropNameToSwiftUINameConfig = default(Dictionary<string, string>);
                var swiftUINativeViewArgConfig = default((string, bool)[]);
                var swiftUIViewInitArgumentOrder = default(string[]);
                
                // [dho] TODO support for qualified access ? - 30/06/19
                if(name?.Kind == SemanticKind.Identifier)
                {
                    var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;
                    
                    var encScope = LanguageSemantics.Swift.GetEnclosingScope(session, ast, node.Node, new Scope(node.Node), token);
                    

                    // [dho] check the symbol does not refer to a declaration
                    // that shows a default - 30/06/19
                    // [dho] TODO HACK? This check actually obviates the need for having to import the 
                    // View in the original source file.. seeing as we are automatically resolving
                    // that symbol for the user... - 30/60/19
                    if(!encScope.Declarations.ContainsKey(lexeme))
                    {
                        // if(SourcePropNameToSwiftUIName.ContainsKey(lexeme))
                        // {
                        //     sourcePropNameToSwiftUINameConfig = SourcePropNameToSwiftUIName[lexeme];
                        // }

                        if(SwiftUIViewInitArguments.ContainsKey(lexeme))
                        {
                            swiftUINativeViewArgConfig = SwiftUIViewInitArguments[lexeme];

                            swiftUIViewInitArgumentOrder = new string[swiftUINativeViewArgConfig.Length];
                            
                            for(int i = 0; i < swiftUINativeViewArgConfig.Length; ++i)
                            {
                                swiftUIViewInitArgumentOrder[i] = swiftUINativeViewArgConfig[i].Item1;
                            }
                        }
                    }
                }

                if(properties.Length > 0)
                {
                    var viewDeclParamNames = default(string[]);

                    if(swiftUIViewInitArgumentOrder != null)
                    {
                        viewDeclParamNames = swiftUIViewInitArgumentOrder;
                        arguments = new List<Node>(viewDeclParamNames.Length);
                    }
                    else
                    {
                        var resolvedSymbol = result.AddMessages(ResolveSymbol(session, ast, name, token));
                        
                        if(resolvedSymbol != null)
                        {
                            if(resolvedSymbol.Kind == SemanticKind.ViewDeclaration)
                            {
                                var cParameters = ASTNodeFactory.ViewDeclaration(ast, resolvedSymbol).Parameters;

                                viewDeclParamNames = new string[cParameters.Length];
                                // [dho] allocate enough space to potentially saw all parameters, even though
                                // some may be missing (eg. if the call is invalid, or those parameters have defaults) - 02/07/19
                                arguments = new List<Node>(viewDeclParamNames.Length);

                                for(int i = 0; i < cParameters.Length; ++i)
                                {
                                    var p = cParameters[i];
                                    System.Diagnostics.Debug.Assert(p.Kind == SemanticKind.ParameterDeclaration);

                                    var parameter = ASTNodeFactory.ParameterDeclaration(ast, p);
                                    var parameterName = parameter.Name;

                                    var rawParameterNameLexeme = default(string);

                                    if(parameterName.Kind == SemanticKind.Identifier)
                                    {
                                        rawParameterNameLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)parameterName).Lexeme;
                                    }

                                    viewDeclParamNames[i] = rawParameterNameLexeme;
                                }
                            }
                            else
                            {
                                result.AddMessages(new NodeMessage(MessageKind.Warning, $"Symbol used as name for view construction was resolved to unexpected kind '{resolvedSymbol.Kind}'", resolvedSymbol)
                                {
                                    Hint = GetHint(resolvedSymbol.Origin),
                                    Tags = DiagnosticTags
                                });
                            }
                        }
                    }

                    {
                        arguments = arguments ?? new List<Node>();
                        for(int i = 0; i < arguments.Capacity; ++i) arguments.Add(null);

                        System.Diagnostics.Debug.Assert(arguments.Count == arguments.Capacity);
                    }

                    
                    // [dho] for every property we add a named argument to the constructor call - 15/06/19
                    for (int i = 0; i < properties.Length; ++i)
                    {
                        var property = properties[i];

                        if (property == null) continue;

                        if (token.IsCancellationRequested) return result;

                        var propName = default(Identifier);
                        var propValue = default(Node);
                        {
                            // [dho] `x` - 15/06/19
                            if (property.Kind == SemanticKind.Identifier)
                            {
                                propName = ASTNodeFactory.Identifier(ast, (DataNode<string>)property);
                                // propValue = NodeFactory.Identifier(ast, property.Origin, propName.Lexeme).Node;
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
                            // [dho] convert `{...{ a : 1, b : 2 }}` to named arguments - 30/06/19
                            else if(property.Kind == SemanticKind.SpreadDestructuring)
                            {
                                var namedArguments = result.AddMessages(
                                    new SwiftNamedArgumentsTransformer().AsNamedArguments(session, ast, property, context, token)
                                );

                                if(namedArguments != null)
                                {
                                    foreach(var namedArg in namedArguments)
                                    {
                                        result.AddMessages(
                                            AddArgument(ast, viewDeclParamNames, propName, propValue, arguments)
                                        );
                                    }
                                }

                                continue;
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

                        // [dho] value may be an expression that contains views - 04/07/19
                        // propValue = result.AddMessages(this.TransformNode(session, propValue, context, token));


                        
                        {
                            var rawPropNameLexeme = propName.Lexeme;

                            // if(sourcePropNameToSwiftUINameConfig != null)
                            // {
                            //     if(sourcePropNameToSwiftUINameConfig.ContainsKey(rawPropNameLexeme))
                            //     {
                            //         rawPropNameLexeme = sourcePropNameToSwiftUINameConfig[rawPropNameLexeme];
                            //     }
                            // }

                            // [dho] if we are instantiating a native SwiftUI type then we check how
                            // to pass the properties to it, whether as init arguments, or as builder function invocations
                            if(swiftUINativeViewArgConfig != null)
                            {
                                var initArgIndex = -1;

                                for(int index = 0; index < swiftUINativeViewArgConfig.Length; ++index)
                                {
                                    if(swiftUINativeViewArgConfig[index].Item1 == rawPropNameLexeme)
                                    {
                                        initArgIndex = index;
                                        break;
                                    }
                                }



                                if(initArgIndex > -1)
                                {
                                    var argument = NodeFactory.InvocationArgument(ast, property.Origin);
                                    
                                    ASTHelpers.Connect(ast, argument.ID, propValue != null ? new [] { propValue } : new Node[]{}, SemanticRole.Value);
        
                                    var useLabel = swiftUINativeViewArgConfig[initArgIndex].Item2;

                                    if(useLabel)
                                    {
                                        ASTHelpers.Connect(ast, argument.ID, new [] { propName.Node }, SemanticRole.Label);
                                    }

                                    result.AddMessages(
                                        AddArgument(ast, viewDeclParamNames, propName, argument.Node, arguments)
                                    );
                                }
                                else
                                {
                                    result.Value = CreateBuilder(ast, result.Value, propName.Node, propValue != null ? new [] { propValue } : new Node[]{}, property.Origin);
                                }
                            }
                            else
                            {
                                var index = IndexOfArgument(viewDeclParamNames, propName);

                                if(index > -1)
                                {
                                    var argument = NodeFactory.InvocationArgument(ast, property.Origin);
                                        
                                    ASTHelpers.Connect(ast, argument.ID, propValue != null ? new [] { propValue } : new Node[]{}, SemanticRole.Value);

                                    ASTHelpers.Connect(ast, argument.ID, new [] { propName.Node }, SemanticRole.Label);

                                    result.AddMessages(AddArgument(ast, viewDeclParamNames, propName, argument.Node, arguments));
                                }
                                else
                                {
                                    result.Value = CreateBuilder(ast, result.Value, propName.Node, propValue != null ? new [] { propValue } : new Node[]{}, property.Origin);
                                }
                            }
                        }
                    }
                }


                if(hasChildren)
                {
                    var lambda = NodeFactory.LambdaDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        var lambdaBody = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                        {
                            foreach(var child in children)
                            {
                                if(child.Kind == SemanticKind.PredicateFlat)
                                {
                                    var ifStatement = NodeFactory.PredicateJunction(ast, child.Origin);

                                    var predicateFlat = ASTNodeFactory.PredicateFlat(ast, child);
                                    var predicate = predicateFlat.Predicate;
                                    var trueValue = predicateFlat.TrueValue;
                                    var falseValue = predicateFlat.FalseValue;


                                    if(trueValue.Kind == SemanticKind.Null)
                                    {
                                        trueValue = CreateNopView(ast, trueValue.Origin);
                                    }

                                    if(falseValue.Kind == SemanticKind.Null)
                                    {
                                        falseValue = CreateNopView(ast, falseValue.Origin);
                                    }

                                    ASTHelpers.Connect(ast, ifStatement.ID, new [] { predicate }, SemanticRole.Predicate);
                                    ASTHelpers.Connect(ast, ifStatement.ID, new [] { trueValue }, SemanticRole.TrueBranch);
                                    ASTHelpers.Connect(ast, ifStatement.ID, new [] { falseValue }, SemanticRole.FalseBranch);

                                    ASTHelpers.Replace(ast, child.ID, new [] { ifStatement.Node });
                                }
                            }


                            // var content = new Node[children.Length];

                            // for(int i = 0; i < content.Length; ++i)
                            // {
                            //     content[i] = result.AddMessages(
                            //         TransformNode(session, children[i], context, token)
                            //     );
                            // }

                            // if(!HasErrors(result))
                            // {
                                ASTHelpers.Connect(ast, lambdaBody.ID, node.Children, SemanticRole.Content);
                            // }
                        }
                        ASTHelpers.Connect(ast, lambda.ID, new [] { lambdaBody.Node }, SemanticRole.Body);
                    }


                    var lambdaArgument = NodeFactory.InvocationArgument(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
                    {
                        ASTHelpers.Connect(ast, lambdaArgument.ID, new [] { lambda.Node }, SemanticRole.Value);
                    }

                    (arguments = arguments ?? new List<Node>()).Add(lambdaArgument.Node);
                }


                ASTHelpers.Connect(ast, construction.ID, new [] { name }, SemanticRole.Name);
                
                if(arguments != null)
                {
                    // [dho] NOTE we filter out any arguments that were not provided (null) - 02/07/19
                    ASTHelpers.Connect(ast, construction.ID, arguments.FindAll(x => x != default(Node)).ToArray(), SemanticRole.Argument);
                }
            }


            ASTHelpers.Replace(ast, node.ID, new [] { result.Value });



            // if (!HasErrors(result))
            // {
            //     // [dho] the code we generated has to go in the first non-value position
            //     // in the code.. eg. `return <View>...</View>` would be illegal as `return final Component foo = ...;`,
            //     // so it has to be reordered to become `final Component foo = ...; return foo;` - 20/06/19
            //     {
            //         var focus = node.Node;

            //         while (LanguageSemantics.Swift.IsValue(ast, focus))
            //         {
            //             focus = ASTHelpers.GetPosition(ast, focus.ID).Parent;
            //         }




            //         if (focus.ID != node.ID)
            //         {
            //             var pos = ASTHelpers.GetPosition(ast, focus.ID);

            //             ASTHelpers.Replace(ast, node.ID,
            //                 new[] { NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), dataValueDeclNameLexeme).Node });

            //             ASTHelpers.InsertBefore(ast, pos.Node.ID, replacementNodes.ToArray(), pos.Role);
            //         }
            //         else
            //         {
            //             ASTHelpers.Replace(ast, node.ID, replacementNodes.ToArray());
            //         }
            //     }
            // }

            return result;
        }

        private Result<Node> ResolveSymbol(Session session, RawAST ast, Node symbol, CancellationToken token)
        {
            var result = new Result<Node>();

            var identifierList = result.AddMessages(ASTNodeHelpers.ToIdentifierList(ast, symbol));

            if(HasErrors(result) || token.IsCancellationRequested) return result;

            if(identifierList.Count > 0)
            {
                var scope = LanguageSemantics.Swift.GetEnclosingScope(session, ast, symbol, new Scope(symbol), token);
            
                var firstIdentifierLexeme = identifierList[0].Lexeme;

                if(scope.Declarations.ContainsKey(firstIdentifierLexeme))
                {
                    var incident = scope.Declarations[firstIdentifierLexeme];

                    bool resolved = true;

                    for(int i = 1; i < identifierList.Count; ++i)
                    {
                        var members = ASTHelpers.QueryEdgeNodes(ast, incident.ID, SemanticRole.Member);

                        resolved = false;

                        foreach(var member in members)
                        {
                            // [dho] TODO if resolves to import clause.. follow import - 02/07/19
                            if(ASTNodeHelpers.IsIdentifierWithName(ast, member, identifierList[i].Lexeme))
                            {
                                incident = member;
                                resolved = true;
                                break;
                            }
                        }                     
                    }

                    if(resolved)
                    {
                        result.Value = incident;
                    }
                }
            }
            else
            {
                result.AddMessages(new NodeMessage(MessageKind.Error, $"Could not derive identifier list for symbol resolution from symbol provided of kind '{symbol.Kind}'", symbol));
            }

            return result;
        }


        private Node CreateNopView(RawAST ast, INodeOrigin origin)
        {
            var nopView = NodeFactory.NamedTypeConstruction(ast, origin);
            {
                var name = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "EmptyView");

                ASTHelpers.Connect(ast, nopView.ID, new [] { name.Node }, SemanticRole.Name);
            }

            return nopView.Node;

            // var nopView = NodeFactory.NamedTypeConstruction(ast, origin);
            // {
            //     var name = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "Text");

            //     ASTHelpers.Connect(ast, nopView.ID, new [] { name.Node }, SemanticRole.Name);

            //     var textArg = NodeFactory.InvocationArgument(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            //     {
            //         ASTHelpers.Connect(ast, textArg.ID, new [] { 
            //             NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "NOP VIEW").Node
            //         }, SemanticRole.Value);
            //     }

            //     ASTHelpers.Connect(ast, nopView.ID, new [] { textArg.Node }, SemanticRole.Argument);
            // }

            // var frame = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "frame");
        
            // var widthArg = NodeFactory.InvocationArgument(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            // {
            //     ASTHelpers.Connect(ast, widthArg.ID, new [] { 
            //         NodeFactory.NumericConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "0").Node
            //      }, SemanticRole.Value);
            //     ASTHelpers.Connect(ast, widthArg.ID, new [] {
            //         NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "width").Node 
            //      }, SemanticRole.Label);
            // }

            // var heightArg = NodeFactory.InvocationArgument(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            // {
            //     ASTHelpers.Connect(ast, heightArg.ID, new [] { 
            //         NodeFactory.NumericConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "0").Node
            //      }, SemanticRole.Value);
            //     ASTHelpers.Connect(ast, heightArg.ID, new [] { 
            //         NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "height").Node 
            //     }, SemanticRole.Label);
            // }

            // return CreateBuilder(ast, nopView.Node, frame.Node, new [] { widthArg.Node, heightArg.Node }, origin);
        }

        private Node CreateBuilder(RawAST ast, Node incident, Node member, Node[] arguments, INodeOrigin origin)
        {
            var builder = NodeFactory.Invocation(ast, origin);

            var qa = NodeFactory.QualifiedAccess(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            {
                ASTHelpers.Connect(ast, qa.ID, new [] { incident }, SemanticRole.Incident);
                ASTHelpers.Connect(ast, qa.ID, new [] { member }, SemanticRole.Member);
            }
            ASTHelpers.Connect(ast, builder.ID, new [] { qa.Node }, SemanticRole.Subject);

            ASTHelpers.Connect(ast, builder.ID, arguments, SemanticRole.Argument);
            
            return builder.Node;
        }

        ///<summary>
        /// Inserts the argument at the correct index of the parameter for the struct, because during
        /// struct initialization, arguments have to match the order the data is laid out in - 02/07/19
        ///</summary>
        private Result<object> AddArgument(RawAST ast, string[] parameterNames, Identifier argName, Node argValue, List<Node> arguments)
        {
            System.Diagnostics.Debug.Assert(argValue.Kind == SemanticKind.InvocationArgument);

            var result = new Result<object>();

            int index = IndexOfArgument(parameterNames, argName);

            if(index > -1)
            {
                System.Diagnostics.Debug.Assert(arguments.Capacity > index);

                arguments[index] = argValue;
            }
            else
            {
                result.AddMessages(new NodeMessage(MessageKind.Warning, "Could not determine argument index because the view declaration parameters were not provided", argValue)
                {
                    Hint = GetHint(argValue.Origin),
                    Tags = DiagnosticTags
                });

                arguments.Add(argValue);
            }

            return result;
        }

        private int IndexOfArgument(string[] parameterNames, Identifier argName)
        {
            int index = -1;

            if(argName != null && parameterNames != null)
            {
                var argNameLexeme = argName.Lexeme;

                for(int i = 0; i < parameterNames.Length; ++i)
                {
                    if(parameterNames[i] == argNameLexeme)
                    {
                        index = i;
                        break;
                    }
                }
            }

            return index;
        }


    }
}