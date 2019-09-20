// /*
//     [dho]
//     NOTE
//     NOTE
//     NOTE
//     NOTE
//     NOTE
//     NOTE
//     NOTE
//     NOTE
//     NOTE

//     THIS IS JUST A FILE CONTAINING A DUMP OF THE LEGACY C# EMITTER,
//     AND A COPY OF THE NEW JAVA EMITTER.

//     TODO, PUT THE C# LEGACY CODE INTO THE NEW EMITTER STRUCTURE!!

//     -   28/04/19
//  */




//     //     // [dho] TODO move to a helper for other emitters that will spit out 1 file per component, eg. Java etc - 21/09/18
//     //     // [dho] TODO CLEANUP this is duplicated in SwiftEmitter (apart from FileEmission extension) - 21/09/18
//     //     public override Result<object> EmitComponent(Component node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         if(node.Origin.Kind != NodeOriginKind.Source)
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Could not write emission in artifact due to unsupported origin kind '{node.Origin.Kind}'", node)
//     //             {
//     //                 Hint = GetHint(node.Origin),
//     //                 Tags = DiagnosticTags
//     //             }
//     //             );

//     //             return result;
//     //         }

//     //         var sourceWithLocation = ((SourceNodeOrigin)node.Origin).Source as ISourceWithLocation<IFileLocation>;

//     //         if(sourceWithLocation == null || sourceWithLocation.Location == null)
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Could not write emission in artifact because output location cannot be determined", node)
//     //             {
//     //                 Hint = GetHint(node.Origin),
//     //                 Tags = DiagnosticTags
//     //             }
//     //             );

//     //             return result;
//     //         }

//     //         var location = sourceWithLocation.Location;

//     //         // [dho] our emissions will be stored in a file with the same relative path and name
//     //         // as the original source for this component, eg. hello/World.ts => hello/World.cs - 29/08/18
//     //         var file = new FileEmission(
//     //             FileSystem.CreateFileLocation(location.ParentDir, location.Name, "cs")
//     //         );

//     //         if(context.Artifact.Contains(file.Destination))
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Could not write emission in artifact at '{file.Destination.ToPathString()}' because location already exists", node)
//     //             {
//     //                 Hint = GetHint(node.Origin),
//     //                 Tags = DiagnosticTags
//     //             }
//     //             );
//     //         }

//     //         var childContext = ContextHelpers.Clone(context);
//     //         childContext.Component = node;
//     //         // // childContext.Parent = node;
//     //         childContext.Emission = file; // [dho] this is where children of this component should write to - 29/08/18

//     //         foreach(var (child, hasNext) in NodeInterrogation.IterateChildren(node))
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(child, childContext, token).Messages
//     //             );

//     //             childContext.Emission.AppendBelow(node, "");

//     //             if(token.IsCancellationRequested)
//     //             {
//     //                 break;
//     //             }
//     //         }

//     //         if(!Diagnostics.DiagnosticsHelpers.HasErrors(result))
//     //         {
//     //             context.Artifact[file.Destination] = file;
//     //         }

//     //         return result;
//     //     }

//     //     public override Result<object> EmitFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         return EmitFunctionLikeDeclaration(node, context, token);
//     //     }


//     //     private Result<object> EmitFunctionLikeDeclaration(FunctionLikeDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

            

//     //         // [dho] EmitOrnamentation - 01/03/19
//     //         // if(node.Override)
//     //         // {
//     //         //     context.Emission.Append(node, "override ");
//     //         // }

//     //         // if(node.Static)
//     //         // {
//     //         //     context.Emission.Append(node, "static ");
//     //         // }

//     //         if(node.Name != null)
//     //         {

//     //             // [dho] TODO reuse function signature emission - 29/09/18

//     //             // [dho] TODO modifiers! - 21/09/18
                
//     //             if(node.Name.Kind == SemanticKind.Identifier)
//     //             {
//     //                 // return type
//     //                 if(node.Type != null)
//     //                 {
//     //                     result.AddMessages(
//     //                         EmitNode(node.Type, childContext, token).Messages
//     //                     );

//     //                     // [dho] the space after the return type - 21/09/18
//     //                     context.Emission.Append(node, " ");
//     //                 }
//     //                 else
//     //                 {
//     //                     result.AddMessages(
//     //                         new NodeMessage(MessageKind.Error, $"Expected function to have a specified return type", node)
//     //                         {
//     //                             Hint = GetHint(node.Origin),
//     //                             Tags = DiagnosticTags
//     //                         }
//     //                     );
//     //                 }

//     //                 result.AddMessages(
//     //                     EmitIdentifier((Identifier)node.Name, childContext, token).Messages
//     //                 );

//     //                 // generics
//     //                 var (templateMessages, typeParamsWithSuperConstraints) = EmitFunctionTemplate(node.Template, childContext, token);

//     //                 result.AddMessages(templateMessages);
                    
//     //                 // [dho] Now we can emit the parameter list after the diamond - 29/09/18
//     //                 result.AddMessages(
//     //                     EmitFunctionParameters(node.Parameters, childContext, token).Messages
//     //                 );

//     //                 // [dho] then we need to emit any constraints for the type parameters - 29/09/18
//     //                 result.AddMessages(
//     //                     EmitFunctionConstraints(node, typeParamsWithSuperConstraints, childContext, token).Messages
//     //                 );
                    
//     //                 // body
//     //                 result.AddMessages(
//     //                     EmitBlockLike(node.Body, childContext, token).Messages
//     //                 );
//     //             }
//     //             else
//     //             {
//     //                 result.AddMessages(
//     //                     new NodeMessage(MessageKind.Error, $"Function has unsupported Name type : '{node.Name.Kind}'", node.Name)
//     //                 {
//     //                     Hint = GetHint(node.Name.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //                 );
//     //             }
//     //         }
//     //         else
//     //         {
//     //             result.AddMessages(
//     //                     new NodeMessage(MessageKind.Error, $"Function must have a name", node)
//     //                 {
//     //                     Hint = GetHint(node.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //                 );
//     //         }


//     //         return result;
//     //     }

//     //     public override Result<object> EmitLambdaDeclaration(LambdaDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         // [dho] TODO modifiers! -16/11/18

//     //         if (node.Name != null)
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Lambda must be anonymous", node.Name)
//     //                 {
//     //                     Hint = GetHint(node.Name.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         if (node.Template != null)
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Lambda must not be templated", node.Template)
//     //                 {
//     //                     Hint = GetHint(node.Template.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         result.AddMessages(
//     //             EmitFunctionParameters(node.Parameters, childContext, token).Messages
//     //         );

//     //         if(node.Type != null)
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Lambda must not have type", node.Type)
//     //                 {
//     //                     Hint = GetHint(node.Type.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         context.Emission.Append(node, "=>");

//     //         result.AddMessages(
//     //             EmitBlockLike(node.Body, childContext, token).Messages
//     //         );
          
//     //         return result;
//     //     }

//     //     #region function parts

//     //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     //     private Result<object> EmitFunctionName(Node name, Context context, CancellationToken token)
//     //     {
//     //         if (name.Kind == SemanticKind.Identifier)
//     //         {
//     //             return EmitIdentifier((Identifier)name, context, token);
//     //         }
//     //         else
//     //         {
//     //             var result = new Result<object>();

//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Unsupported name type : '{name.Kind}'", name)
//     //                 {
//     //                     Hint = GetHint(name.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );

//     //             return result;
//     //         }
//     //     }

//     //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     //     private Result<IList<TypeParameterDeclaration>> EmitFunctionTemplate(Node template, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<IList<TypeParameterDeclaration>>();

//     //         if (template != null)
//     //         {
//     //             var typeParamsWithConstraints = new List<TypeParameterDeclaration>();        

//     //             // [dho] first we have to emit the names in the diamond that comes after the function name, but
//     //             // before the parameter list, eg. `Foo<T, U, V>(...)` - 29/09/18 
//     //             context.Emission.Append(template, "<");

//     //             foreach(var (member, hasNext) in NodeInterrogation.IterateMembers(template))
//     //             {
//     //                 if(member.Kind == SemanticKind.TypeParameterDeclaration)
//     //                 {
//     //                     var typeParam = (TypeParameterDeclaration)member;

//     //                     result.AddMessages(
//     //                         EmitNode(typeParam.Name, context, token).Messages
//     //                     );

//     //                     if(hasNext)
//     //                     {
//     //                         context.Emission.Append(template, ",");
//     //                     }

//     //                     if(typeParam.Constraints != null)
//     //                     {
//     //                         // [dho] we have to emit the constraints after the parameter list below - 29/09/18
//     //                         typeParamsWithConstraints.Add(typeParam);
//     //                     }

//     //                     if(typeParam.Default != null)
//     //                     {
//     //                         result.AddMessages(
//     //                             CreateUnsupportedFeatureResult(typeParam.Default, "default type constraints", context).Messages
//     //                         );
//     //                     }
//     //                 }
//     //                 else
//     //                 {  
//     //                     result.AddMessages(
//     //                         new NodeMessage(MessageKind.Error, $"Expected a Type Parameter but found node of type : '{member.Kind}'", member)
//     //                         {
//     //                             Hint = GetHint(member.Origin),
//     //                             Tags = DiagnosticTags
//     //                         }
//     //                     );
//     //                 }
//     //             }

//     //             context.Emission.Append(template, ">");

//     //             if(!Diagnostics.DiagnosticsHelpers.HasErrors(result))
//     //             {
//     //                 result.Value = typeParamsWithConstraints;
//     //             }
//     //         }

//     //         return result;
//     //     }

//     //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     //     private Result<object> EmitFunctionParameters(Node parameters, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(parameters ?? context.Parent, "(");

//     //         if (parameters != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitCSV(parameters, context, token).Messages
//     //             );
//     //         }

//     //         context.Emission.Append(parameters ?? context.Parent, ")");

//     //         return result;
//     //     }

//     //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     //     private Result<object> EmitFunctionConstraints(Node node, IList<TypeParameterDeclaration> typeParamsWithConstraints, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         if (typeParamsWithConstraints?.Count > 0)
//     //         {
//     //             // [dho] then we need to emit any constraints for the type parameters - 29/09/18
//     //             foreach(var typeParam in typeParamsWithConstraints)
//     //             {
//     //                 context.Emission.Append(node, " where ");

//     //                 result.AddMessages(
//     //                     EmitNode(typeParam.Name, context, token).Messages
//     //                 );

//     //                 context.Emission.Append(node, ":");

//     //                 result.AddMessages(
//     //                     EmitCSV(typeParam.Constraints, context, token).Messages
//     //                 );
//     //             }
//     //         }

//     //         return result;
//     //     }



//     //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     //     private Result<object> EmitBlockLike(Node body, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         if (body != null)
//     //         {
//     //             if (body.Kind == SemanticKind.Block)
//     //             {
//     //                 result.AddMessages(
//     //                     EmitBlock((Block)body, context, token).Messages
//     //                 );
//     //             }
//     //             else
//     //             {
//     //                 context.Emission.Append(body, "{");
//     //                 context.Emission.Indent();

//     //                 foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(body))
//     //                 {
//     //                     context.Emission.AppendBelow(body, "");

//     //                     result.AddMessages(
//     //                         EmitNode(member, context, token).Messages
//     //                     );

//     //                     context.Emission.Append(member, ";");
//     //                 }

//     //                 context.Emission.Outdent();
//     //                 context.Emission.AppendBelow(body, "}");
//     //             }
//     //         }
//     //         else
//     //         {
//     //             context.Emission.Append(context.Parent, "{}");
//     //         }

//     //         return result;
//     //     }

//     //     #endregion

//     //     // private Result<object> EmitParameterList(FunctionDeclaration node, Context context, CancellationToken token)
//     //     // {
//     //     //     var result = new Result<object>();

//     //     //     var parameters = node.Parameters;

//     //     //     context.Emission.Append(node, "(");

//     //     //     if(node.Parameters != null)
//     //     //     {
//     //     //         result.AddMessages(
//     //     //             EmitCSV(node.Parameters, context, token).Messages
//     //     //         );
//     //     //     }

//     //     //     context.Emission.Append(node, ")");
        
//     //     //     return result;
//     //     // }

//     //     public override Result<object> EmitParameterDeclaration(ParameterDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         if(node.Type != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Type, childContext, token).Messages
//     //             );
                
//     //             context.Emission.Append(node, " ");
//     //         }
//     //         else if(node.Parent.Kind != SemanticKind.LambdaDeclaration)
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Expected parameter to have a specified type", node)
//     //                 {
//     //                     Hint = GetHint(node.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         if(node.Label != null)
//     //         {
//     //             result.AddMessages(
//     //                 CreateUnsupportedFeatureResult(node.Label, "parameter labels", childContext).Messages
//     //             );
//     //         }

//     //         result.AddMessages(
//     //             EmitNode(node.Name, childContext, token).Messages
//     //         );


//     //         if(node.Default != null)
//     //         {
//     //             context.Emission.Append(node, "=");

//     //             result.AddMessages(
//     //                 EmitNode(node.Default, childContext, token).Messages
//     //             );
//     //         }

//     //         return result;
//     //     }

//     //     public override Result<object> EmitIdentifier(Identifier node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, NodeInterrogation.GetLexeme(node));

//     //         return result;
//     //     }

//     //     public override Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         // [dho] TODO EmitOrnamentation - 01/03/19
//     //         // if(!node.Required)
//     //         // {
//     //         //     result.AddMessages(
//     //         //         CreateUnsupportedFeatureResult(node, context).Messages
//     //         //     );
//     //         // }

//     //         if(node.Clauses != null)
//     //         {
//     //             if(node.Clauses.Kind == SemanticKind.EntityDestructuring)
//     //             {
//     //                 var clauses = (EntityDestructuring)node.Clauses;

//     //                 // [dho] if we had the TypeScript `import { X as Y, Z } from 'System.Text';`
//     //                 // then we need to generate the C#
//     //                 // ```
//     //                 // using Y = System.Text.X;
//     //                 // using Z = System.Text.Z;
//     //                 // ```
//     //                 // - 11/10/18
//     //                 foreach(var (member, hasNext) in NodeInterrogation.IterateMembers(clauses.Members))
//     //                 {
//     //                     if(member.Kind == SemanticKind.DestructuredMember)
//     //                     {
//     //                         var destructuredMember = (DestructuredMember)member;

//     //                         context.Emission.Append(node, "using ");

//     //                         Node from = default(Node);
//     //                         Node to = default(Node);

//     //                         if(destructuredMember.Name.Kind == SemanticKind.ReferenceAliasDeclaration)
//     //                         {
//     //                             var alias = (ReferenceAliasDeclaration)destructuredMember.Name;
                                
//     //                             from = alias.From;
//     //                             to = alias.To;
//     //                         }
//     //                         else
//     //                         {
//     //                             from = to = destructuredMember.Name;
//     //                         }

//     //                         if(destructuredMember.Default != null)
//     //                         {
//     //                             result.AddMessages(
//     //                                 new NodeMessage(MessageKind.Error, $"Import clause member has unsupported default value", destructuredMember)
//     //                                 {
//     //                                     Hint = GetHint(destructuredMember.Origin),
//     //                                     Tags = DiagnosticTags
//     //                                 }
//     //                             );
//     //                         }

//     //                         result.AddMessages(
//     //                             EmitNode(from, childContext, token).Messages
//     //                         );
                        
//     //                         context.Emission.Append(node, "=");

//     //                         result.AddMessages(
//     //                             EmitNode(node.Specifier, childContext, token).Messages
//     //                         );

//     //                         context.Emission.Append(node, ".");

//     //                         result.AddMessages(
//     //                             EmitNode(to, childContext, token).Messages
//     //                         );

//     //                         context.Emission.Append(node, ";");
//     //                     }
//     //                     else
//     //                     {
//     //                         result.AddMessages(
//     //                             new NodeMessage(MessageKind.Error, $"Import clause member has unsupported type : '{member.Kind}'", member)
//     //                             {
//     //                                 Hint = GetHint(member.Origin),
//     //                                 Tags = DiagnosticTags
//     //                             }
//     //                         );
//     //                     }
//     //                 }
//     //             }
//     //             else
//     //             {
//     //                 result.AddMessages(
//     //                             new NodeMessage(MessageKind.Error, $"Import clauses have unsupported type : '{node.Clauses.Kind}'", node.Clauses)
//     //                         {
//     //                             Hint = GetHint(node.Clauses.Origin),
//     //                             Tags = DiagnosticTags
//     //                         }
//     //                         );
//     //             }
//     //         }
//     //         else
//     //         {
//     //             // [dho] we emit in the form `using System.Text;` - 11/10/18

//     //             context.Emission.Append(node, $"using ");

//     //             // [dho] CLEANUP HACK - when the author writes `import "System";`
//     //             // we are just going to emit `System` without the quotes - 17/03/19 
//     //             if(node.Specifier.Kind == SemanticKind.StringConstant)
//     //             {
//     //                 var packageName = ((StringConstant)node.Specifier).Value;

//     //                 context.Emission.Append(node.Specifier, packageName);
//     //             }
//     //             else
//     //             {
//     //                 result.AddMessages(
//     //                     EmitNode(node.Specifier, childContext, token).Messages
//     //                 );
//     //             }

//     //             context.Emission.Append(node, ";");
//     //         }

//     //         return result;
//     //     }

//     //     public override Result<object> EmitNamedTypeReference(NamedTypeReference node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Name, childContext, token).Messages
//     //         );

//     //         if(node.Template != null)
//     //         {
//     //             context.Emission.Append(node, "<");

//     //             result.AddMessages(
//     //                 EmitCSV(node.Template, childContext, token).Messages
//     //             );

//     //             context.Emission.Append(node, ">");
//     //         }

//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA SwiftEmitter - 12/01/19
//     //     public override Result<object> EmitBlock(Block node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, "{");

//     //         if (node.Content != null)
//     //         {
//     //             context.Emission.Indent();

//     //             foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(node.Content))
//     //             {
//     //                 context.Emission.AppendBelow(node, "");

//     //                 result.AddMessages(
//     //                     EmitNode(member, context, token).Messages
//     //                 );

//     //                 context.Emission.Append(member, ";");
//     //             }

//     //             context.Emission.Outdent();
//     //         }

//     //         context.Emission.AppendBelow(node, "}");

//     //         return result;
//     //     }

//     //     #region constants

//     //     public override Result<object> EmitBooleanConstant(BooleanConstant node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, node.Value ? "true" : "false");

//     //         return result;
//     //     }

//     //     public override Result<object> EmitNumericConstant(NumericConstant node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, node.Value.ToString());

//     //         return result;
//     //     }

//     //     public override Result<object> EmitStringConstant(StringConstant node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         if(NodeInterrogation.IsMultilineString(node))
//     //         {
//     //             context.Emission.Append(node, $"@\"{node.Value}\"");
//     //         }
//     //         else
//     //         {
//     //             context.Emission.Append(node, $"\"{node.Value}\"");
//     //         }

//     //         return result;
//     //     }

//     //     #endregion

//     //     #region meta

//     //     public override Result<object> EmitAnnotation(Annotation annotation, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(annotation, "[");

//     //         result.AddMessages(
//     //             EmitNode(((Annotation)annotation).Expression, context, token)
//     //         );

//     //         context.Emission.Append(annotation, "]");

//     //         return result;
//     //     }
        

//     //     public override Result<object> EmitModifier(Modifier modifier, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(modifier, NodeInterrogation.GetLexeme(modifier));

//     //         return result;
//     //     }

//     //     #endregion

//     //     #region intrinsics

//     //     // public virtual Result<object> EmitDynamicTypeReference(DynamicTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     return CreateUnsupportedFeatureResult(node, context);
//     //     // }

//     //     // public virtual Result<object> EmitAnyTypeReference(IAnyTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     // [dho] this will produce a fairly confusing error message due to the word 'any'.. - 30/09/18
//     //     //     return CreateUnsupportedFeatureResult(node, context);
//     //     // }

//     //     public override Result<object> EmitArrayTypeReference(ArrayTypeReference node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitCSV(node.Type, childContext, token).Messages
//     //         );
            
//     //         context.Emission.Append(node, "[]");

//     //         return result;
//     //     }

//     //     // public override Result<object> EmitBooleanTypeReference(IBooleanTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     var result = new Result<object>();

//     //     //     context.Emission.Append(node, "bool");

//     //     //     return result;
//     //     // }

//     //     // public virtual Result<object> EmitDictionaryTypeReference(DictionaryTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     return CreateUnsupportedFeatureResult(node, context);
//     //     // }

//     //     // public override Result<object> EmitDoublePrecisionNumberTypeReference(IDoublePrecisionNumberTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     var result = new Result<object>();

//     //     //     context.Emission.Append(node, "double");

//     //     //     return result;
//     //     // }

//     //     // public override Result<object> EmitFloatingPointNumberTypeReference(IFloatingPointNumberTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     var result = new Result<object>();

//     //     //     context.Emission.Append(node, "float");

//     //     //     return result;
//     //     // }

//     //     // public override Result<object> EmitIntegralNumberTypeReference(IIntegralNumberTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     var result = new Result<object>();

//     //     //     context.Emission.Append(node, "int");

//     //     //     return result;
//     //     // }

//     //     // public virtual Result<object> EmitIntersectionTypeReference(IntersectionTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     return CreateUnsupportedFeatureResult(node, context);
//     //     // }

//     //     // public virtual Result<object> EmitNeverTypeReference(INeverTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     return CreateUnsupportedFeatureResult(node, context);
//     //     // }

//     //     // public override Result<object> EmitStringTypeReference(IStringTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     var result = new Result<object>();

//     //     //     context.Emission.Append(node, "string");

//     //     //     return result;
//     //     // }

//     //     public override Result<object> EmitTupleTypeReference(TupleTypeReference node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, "(");
         
//     //         if(node.Types != null)
//     //         {
//     //             var childContext = ContextHelpers.Clone(context);
//     //             // // childContext.Parent = node;

//     //             result.AddMessages(
//     //                 EmitCSV(node.Types, childContext, token).Messages
//     //             );
//     //         }

//     //         context.Emission.Append(node, ")");

//     //         return result;
//     //     }

//     //     // public virtual Result<object> EmitUnionTypeReference(UnionTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     return CreateUnsupportedFeatureResult(node, context);
//     //     // }

//     //     // public override Result<object> EmitVoidTypeReference(IVoidTypeReference node, Context context, CancellationToken token)
//     //     // {
//     //     //     var result = new Result<object>();

//     //     //     context.Emission.Append(node, "void");

//     //     //     return result;
//     //     // }

//     //     #endregion

//     //     #region controls

//     //     // [dho] COPYPASTA from Swift Emitter - 17/11/18
//     //     public override Result<object> EmitMatchClause(MatchClause node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         if(node.Expression != null)
//     //         {
//     //             context.Emission.Append(node, "case ");

//     //             result.AddMessages(
//     //                 EmitNode(node.Expression, context, token).Messages
//     //             );

//     //             context.Emission.Append(node, ":");
//     //         }
//     //         else
//     //         {
//     //             context.Emission.Append(node, "default:");
//     //         }

//     //         result.AddMessages(
//     //             EmitBlockLike(node.Body, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitMatchJunction(MatchJunction node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node, "switch(");

//     //         result.AddMessages(
//     //             EmitNode(node.Subject, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, ")");

//     //         result.AddMessages(
//     //             EmitBlockLike(node.Clauses, childContext, token).Messages
//     //         );
            
//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA from Swift Emitter - 17/11/18
//     //     public override Result<object> EmitPredicateFlat(PredicateFlat node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Predicate, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, "?");

//     //         result.AddMessages(
//     //             EmitNode(node.TrueValue, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, ":");

//     //         result.AddMessages(
//     //             EmitNode(node.FalseValue, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitPredicateJunction(PredicateJunction node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node, "if(");

//     //         result.AddMessages(
//     //             EmitNode(node.Predicate, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, ")");

//     //         result.AddMessages(
//     //             EmitBlockLike(node.TrueBranch, childContext, token).Messages
//     //         );

//     //         if(node.FalseBranch != null)
//     //         {
//     //             context.Emission.AppendBelow(node, "else ");

//     //             if(node.FalseBranch.Kind == SemanticKind.PredicateJunction)
//     //             {
//     //                 result.AddMessages(
//     //                     EmitPredicateJunction((PredicateJunction)node.FalseBranch, childContext, token).Messages
//     //                 );
//     //             }
//     //             else
//     //             {
//     //                 result.AddMessages(
//     //                     EmitBlockLike(node.FalseBranch, childContext, token).Messages
//     //                 );
//     //             }
//     //         }
            
//     //         return result;
//     //     }

//     //     #endregion

//     //     public override Result<object> EmitAssignment(Assignment node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Storage, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, "=");

//     //         result.AddMessages(
//     //             EmitNode(node.Value, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     #region arithmetic

//     //     public override Result<object> EmitPostDecrement(PostDecrement node, Context context, CancellationToken token)
//     //     {
//     //         return EmitPostUnaryArithmetic(node, "--", context, token);
//     //     }

//     //     public override Result<object> EmitPreDecrement(PreDecrement node, Context context, CancellationToken token)
//     //     {
//     //         return EmitPreUnaryArithmetic(node, "--", context, token);
//     //     }

//     //     public override Result<object> EmitPostIncrement(PostIncrement node, Context context, CancellationToken token)
//     //     {
//     //         return EmitPostUnaryArithmetic(node, "++", context, token);
//     //     }

//     //     public override Result<object> EmitPreIncrement(PreIncrement node, Context context, CancellationToken token)
//     //     {
//     //         return EmitPreUnaryArithmetic(node, "++", context, token);
//     //     }

//     //     private Result<object> EmitPostUnaryArithmetic(UnaryArithmetic node, string operatorToken, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Operand, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, operatorToken);

//     //         return result;
//     //     }

//     //     private Result<object> EmitPreUnaryArithmetic(UnaryArithmetic node, string operatorToken, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, operatorToken);

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Operand, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitAddition(Addition node, Context context, CancellationToken token)
//     //     {
//     //         return EmitBinaryArithmetic(node, "+", context, token);
//     //     }

//     //     public override Result<object> EmitDivision(Division node, Context context, CancellationToken token)
//     //     {
//     //         return EmitBinaryArithmetic(node, "/", context, token);
//     //     }

//     //     public override Result<object> EmitMultiplication(Multiplication node, Context context, CancellationToken token)
//     //     {
//     //         return EmitBinaryArithmetic(node, "*", context, token);
//     //     }

//     //     public override Result<object> EmitRemainder(Remainder node, Context context, CancellationToken token)
//     //     {
//     //         return EmitBinaryArithmetic(node, "%", context, token);
//     //     }

//     //     public override Result<object> EmitSubtraction(Subtraction node, Context context, CancellationToken token)
//     //     {
//     //         return EmitBinaryArithmetic(node, "-", context, token);
//     //     }

//     //     private Result<object> EmitBinaryArithmetic(BinaryArithmetic node, string operatorToken, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         // left operand
//     //         result.AddMessages(
//     //             EmitNode(node.LeftOperand, childContext, token).Messages
//     //         );

//     //         // operator
//     //         context.Emission.Append(node, operatorToken);

//     //         // right operand
//     //         result.AddMessages(
//     //             EmitNode(node.RightOperand, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     #endregion

//     //     #region bitwise

//     //     public override Result<object> EmitBitwiseAnd(BitwiseAnd node, Context context, CancellationToken token)
//     //     {
//     //         return EmitBinaryLike(node, "&", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseAndAssignment(BitwiseAndAssignment node, Context context, CancellationToken token)
//     //     {
//     //         return EmitAssignmentLike(node, "&=", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseExclusiveOr(BitwiseExclusiveOr node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, "^", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseExclusiveOrAssignment(BitwiseExclusiveOrAssignment node, Context context, CancellationToken token)
//     //     {
//     //         return EmitAssignmentLike(node, "^=", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseLeftShift(BitwiseLeftShift node, Context context, CancellationToken token)
//     //     {
//     //         return EmitBitwiseShift(node, "<<", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseLeftShiftAssignment(BitwiseLeftShiftAssignment node, Context context, CancellationToken token)
//     //     {
//     //         return EmitAssignmentLike(node, "<<=", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseOr(BitwiseOr node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, "|", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseOrAssignment(BitwiseOrAssignment node, Context context, CancellationToken token)
//     //     {
//     //         return EmitAssignmentLike(node, "|=", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseRightShift(BitwiseRightShift node, Context context, CancellationToken token)
//     //     {
//     //         return EmitBitwiseShift(node, ">>", context, token);
//     //     }

//     //     public override Result<object> EmitBitwiseRightShiftAssignment(BitwiseRightShiftAssignment node, Context context, CancellationToken token)
//     //     {
//     //         return EmitAssignmentLike(node, ">>=", context, token);
//     //     }

//     //     #endregion

//     //     #region logic

//     //     public override Result<object> EmitLogicalAnd(LogicalAnd node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, "&&", context, token);
//     //     }

//     //     public override Result<object> EmitLogicalNegation(LogicalNegation node, Context context, CancellationToken token)
//     //     {
//     //         return EmitPrefixUnaryLike(node, "!", context, token);
//     //     }

//     //     public override Result<object> EmitLogicalOr(LogicalOr node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, "||", context, token);
//     //     }

//     //     #endregion

//     //     #region equality

//     //     public override Result<object> EmitStrictEquivalent(StrictEquivalent node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, "==", context, token);
//     //     }
        
//     //     public override Result<object> EmitStrictGreaterThan(StrictGreaterThan node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, ">", context, token);
//     //     }

//     //     public override Result<object> EmitStrictGreaterThanOrEquivalent(StrictGreaterThanOrEquivalent node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, ">=", context, token);
//     //     }

//     //     public override Result<object> EmitStrictLessThan(StrictLessThan node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, "<", context, token);
//     //     }

//     //     public override Result<object> EmitStrictLessThanOrEquivalent(StrictLessThanOrEquivalent node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, "<=", context, token);
//     //     }

//     //     public override Result<object> EmitStrictNonEquivalent(StrictNonEquivalent node, Context context, CancellationToken token)
//     //     {
//     //        return EmitBinaryLike(node, "!=", context, token);
//     //     }

//     //     #endregion

//     //     #region constructions

//     //     public override Result<object> EmitArrayConstruction(ArrayConstruction node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node, "new");

//     //         if(node.Type != null)
//     //         {
//     //             context.Emission.Append(node, " ");

//     //             result.AddMessages(
//     //                 EmitNode(node.Type, childContext, token).Messages
//     //             );
//     //         }

//     //         if(node.Size != null)
//     //         {
//     //             context.Emission.Append(node, "[");

//     //             result.AddMessages(
//     //                 EmitNode(node.Size, childContext, token).Messages
//     //             );

//     //             context.Emission.Append(node, "]");
//     //         }
//     //         else
//     //         {
//     //             context.Emission.Append(node, "[]");
//     //         }

//     //         if(node.Members != null)
//     //         {
//     //             context.Emission.Append(node, "{");

//     //             result.AddMessages(
//     //                 EmitCSV(node.Members, childContext, token).Messages
//     //             );

//     //             context.Emission.Append(node, "}");
//     //         }

//     //         return result;
//     //     }

//     //     public override Result<object> EmitDynamicTypeConstructions(DynamicTypeConstruction node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node, "new {");

//     //         if(node.Members != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitCSV(node.Members, childContext, token).Messages
//     //             );
//     //         }

//     //         context.Emission.Append(node, "}");

//     //         return result;
//     //     }

//     //     public override Result<object> EmitIntrinsicTypeReference(IntrinsicTypeReference node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         switch(node.Role)
//     //         {
//     //             case TypeRole.Boolean:
//     //                 context.Emission.Append(node, "bool");
//     //             break;

//     //             case TypeRole.Double64:
//     //                 context.Emission.Append(node, "double");
//     //             break;

//     //             case TypeRole.Integer32:
//     //                 context.Emission.Append(node, "int");
//     //             break;

//     //             case TypeRole.String:
//     //                 context.Emission.Append(node, "string");
//     //             break;

//     //             case TypeRole.Void:
//     //                 context.Emission.Append(node, "void");
//     //             break;

//     //             default:
//     //                 result.AddMessages(
//     //                     CreateUnsupportedFeatureResult(node, $"Intrisinsic {node.Role.ToString()} types", context)
//     //                 );
//     //             break;
//     //         }

//     //         return result;
//     //     }

//     //     public override Result<object> EmitNamedTypeConstruction(NamedTypeConstruction node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node, "new ");

//     //         result.AddMessages(
//     //             EmitNode(node.Name, childContext, token).Messages
//     //         );

//     //         if(node.Template != null)
//     //         {
//     //             context.Emission.Append(node.Template, "<");

//     //             result.AddMessages(
//     //                 EmitCSV(node.Template, context, token).Messages
//     //             );

//     //             context.Emission.Append(node.Template, ">");
//     //         }

//     //         context.Emission.Append(node, "(");

//     //         if(node.Arguments != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitCSV(node.Arguments, childContext, token).Messages
//     //             );
//     //         }

//     //         context.Emission.Append(node, ")");

//     //         return result;
//     //     }

//     //     public override Result<object> EmitTupleConstruction(TupleConstruction node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node, "(");

//     //         if(node.Members != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitCSV(node.Members, childContext, token).Messages
//     //             );
//     //         }

//     //         context.Emission.Append(node, ")");

//     //         return result;
//     //     }

//     //     #endregion 


//     //     // [dho] COPYPASTA from Swift Emitter - 18/11/18
//     //     private Result<object> EmitPrefixUnaryLike(UnaryLike node, string operatorToken, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         // operator
//     //         context.Emission.Append(node, operatorToken);
            
//     //         // operand
//     //         result.AddMessages(
//     //             EmitNode(node.Operand, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA from Swift Emitter - 18/11/18
//     //     private Result<object> EmitBinaryLike(BinaryLike node, string operatorToken, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         // left operand
//     //         result.AddMessages(
//     //             EmitNode(node.LeftOperand, childContext, token).Messages
//     //         );

//     //         // operator
//     //         context.Emission.Append(node, operatorToken);

//     //         // right operand
//     //         result.AddMessages(
//     //             EmitNode(node.RightOperand, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA from Swift Emitter - 18/11/18
//     //     private Result<object> EmitBitwiseShift(BitwiseShift node, string operatorToken, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         // left operand
//     //         result.AddMessages(
//     //             EmitNode(node.Subject, childContext, token).Messages
//     //         );

//     //         // operator
//     //         context.Emission.Append(node, operatorToken);

//     //         // right operand
//     //         result.AddMessages(
//     //             EmitNode(node.Offset, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA from Swift Emitter - 18/11/18
//     //     private Result<object> EmitAssignmentLike(AssignmentLike node, string operatorToken, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Storage, childContext, token).Messages
//     //         );

//     //         // operator
//     //         context.Emission.Append(node, operatorToken);

//     //         result.AddMessages(
//     //             EmitNode(node.Value, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     // public override Result<object> EmitOrderedGroup(OrderedGroup node, Context context, CancellationToken token)
//     //     // {
//     //     //     throw new NotImplementedException();
//     //     // }

//     //     public override Result<object> EmitFunctionTermination(FunctionTermination node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         if(node.Value != null)
//     //         {
//     //             context.Emission.Append(node, "return ");

//     //             var childContext = ContextHelpers.Clone(context);
//     //             // // childContext.Parent = node;

//     //             result.AddMessages(
//     //                 EmitNode(node.Value, childContext, token).Messages
//     //             );

//     //             // [dho] end of return statment - 21/09/18
//     //             context.Emission.Append(node, ";");
//     //         }
//     //         else
//     //         {
//     //             context.Emission.Append(node, "return;");
//     //         }

//     //         return result;
//     //     }
        
//     //     public override Result<object> EmitTypeAliasDeclaration(TypeAliasDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;
            
//     //         context.Emission.AppendBelow(node, "using ");

//     //         result.AddMessages(
//     //             EmitNode(node.From, childContext, token).Messages
//     //         );

//     //         if(node.Template != null)
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Type alias must not be templated", node.From)
//     //                 {
//     //                     Hint = GetHint(node.From.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         context.Emission.Append(node, "=");

//     //         result.AddMessages(
//     //             EmitNode(node.To, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitAssociation(Association node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, "(");

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Subject, childContext, token).Messages  
//     //         );

//     //         context.Emission.Append(node, ")");

//     //         return result;
//     //     }

//     //     public override Result<object> EmitDataValueDeclaration(DataValueDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;


//     //         // [dho] TODO EmitOrnamentation - 01/03/19

//     //         if (node.Type != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Type, childContext, token).Messages
//     //             );
//     //         }
//     //         else
//     //         {
//     //             context.Emission.Append(node, "var");
//     //         }

//     //         context.Emission.Append(node, " ");

//     //         result.AddMessages(
//     //             EmitNode(node.Name, childContext, token).Messages
//     //         );

//     //         if (node.Initializer != null)
//     //         {
//     //             context.Emission.Append(node, "=");

//     //             result.AddMessages(
//     //                 EmitNode(node.Initializer, childContext, token).Messages
//     //             );
//     //         }

//     //         return result;
//     //     }

//     //     public override Result<object> EmitIndexedAccess(IndexedAccess node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Incident, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, "[");

//     //         result.AddMessages(
//     //             EmitNode(node.Member, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, "]");

//     //         return result;
//     //     }

//     //     #region types

//     //     public override Result<object> EmitNamespaceDeclaration(NamespaceDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node, "namespace ");

//     //         if (node.Name != null)
//     //         {
//     //             var name = node.Name;

//     //             if (name.Kind == SemanticKind.Identifier)
//     //             {
//     //                 result.AddMessages(
//     //                     EmitIdentifier((Identifier)name, childContext, token).Messages
//     //                 );
//     //             }
//     //             else
//     //             {
//     //                 result.AddMessages(
//     //                     new NodeMessage(MessageKind.Error, $"Namespace declaration has unsupported name type : '{name.Kind}'", name)
//     //                     {
//     //                         Hint = GetHint(name.Origin),
//     //                         Tags = DiagnosticTags
//     //                     }
//     //                 );
//     //             }
//     //         }
//     //         else
//     //         {
//     //             result.AddMessages(
//     //                 CreateUnsupportedFeatureResult(node, context).Messages
//     //             );
//     //         }

//     //         result.AddMessages(
//     //             EmitBlockLike(node.Members, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitEnumerationTypeDeclaration(EnumerationTypeDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node, "enum ");

//     //         if (node.Name != null)
//     //         {
//     //             var name = node.Name;

//     //             if (name.Kind == SemanticKind.Identifier)
//     //             {
//     //                 result.AddMessages(
//     //                     EmitIdentifier((Identifier)name, childContext, token).Messages
//     //                 );
//     //             }
//     //             else
//     //             {
//     //                 result.AddMessages(
//     //                     new NodeMessage(MessageKind.Error, $"Enumeration type declaration has unsupported name type : '{name.Kind}'", name)
//     //                     {
//     //                         Hint = GetHint(name.Origin),
//     //                         Tags = DiagnosticTags
//     //                     }
//     //                 );
//     //             }
//     //         }
//     //         else
//     //         {
//     //             result.AddMessages(
//     //                 CreateUnsupportedFeatureResult(node, context).Messages
//     //             );
//     //         }

//     //         // generics
//     //         result.AddMessages(
//     //             EmitTypeDeclarationTemplate(node.Template, childContext, token).Messages
//     //         );

//     //         result.AddMessages(
//     //             EmitTypeDeclarationHeritage(node, context, token).Messages
//     //         );

//     //         context.Emission.Append(node, "{");

//     //         result.AddMessages(
//     //             EmitTypeDeclarationMembers(node, childContext, token).Messages
//     //         );

//     //         context.Emission.AppendBelow(node, "}");

//     //         return result;
//     //     }

//     //     public override Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         // [dho] TODO modifiers! - 21/09/18

//     //         if(node.Name != null)
//     //         {
//     //             var name = node.Name;

//     //             if(name.Kind == SemanticKind.Identifier)
//     //             {
//     //                 context.Emission.Append(node, "class ");
                    
//     //                 result.AddMessages(
//     //                     EmitIdentifier((Identifier)name, childContext, token).Messages
//     //                 );
//     //             }
//     //             else
//     //             {
//     //                 result.AddMessages(
//     //                     new NodeMessage(MessageKind.Error, $"Reference type declaration has unsupported name type : '{name.Kind}'", name)
//     //                     {
//     //                         Hint = GetHint(name.Origin),
//     //                         Tags = DiagnosticTags
//     //                     }
//     //                 );
//     //             }
//     //         }
//     //         else
//     //         {
//     //             result.AddMessages(
//     //                 CreateUnsupportedFeatureResult(node, context).Messages
//     //             );
//     //         }

//     //         // generics
//     //         if(node.Template != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitTypeDeclarationTemplate(node.Template, childContext, token).Messages
//     //             );
//     //         }

//     //         result.AddMessages(
//     //             EmitTypeDeclarationHeritage(node, context, token).Messages
//     //         );

//     //         context.Emission.Append(node, "{");

//     //         result.AddMessages(
//     //             EmitTypeDeclarationMembers(node, childContext, token).Messages
//     //         );

//     //         context.Emission.AppendBelow(node, "}");

//     //         return result;
//     //     }

//     //     public override Result<object> EmitPropertyDeclaration(PropertyDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         if (node.Type != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Type, childContext, token).Messages
//     //             );
//     //         }
//     //         else
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Expected property to have a specified type", node)
//     //                 {
//     //                     Hint = GetHint(node.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         context.Emission.Append(node, " ");

//     //         result.AddMessages(
//     //             EmitNode(node.Name, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, "{");

//     //         context.Emission.Indent();

//     //         if(node.Accessor != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Accessor, childContext, token).Messages
//     //             );
//     //         }

//     //         if(node.Mutator != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Mutator, childContext, token).Messages
//     //             );
//     //         }

//     //         context.Emission.Outdent();

//     //         context.Emission.AppendBelow(node, "}");

//     //         return result;
//     //     }

//     //     public override Result<object> EmitForcedCast(ForcedCast node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.Append(node.TargetType, "(");
            
//     //         result.AddMessages(
//     //             EmitNode(node.TargetType, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node.TargetType, ")");

//     //         result.AddMessages(
//     //             EmitNode(node.Subject, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitSafeCast(SafeCast node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Subject, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, " as ");
            
//     //         result.AddMessages(
//     //             EmitNode(node.TargetType, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA from Swift Emitter - 04/11/18
//     //     public override Result<object> EmitTypeTest(TypeTest node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Subject, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, " is ");
            
//     //         result.AddMessages(
//     //             EmitNode(node.Criteria, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     #endregion

//     //     #region type declaration parts

//     //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     //     private Result<object> EmitTypeDeclarationHeritage(TypeDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var supers = node.Supers;
//     //         var interfaces = node.Interfaces;

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         if(supers != null)
//     //         {
//     //             context.Emission.Append(node, ":");

//     //             result.AddMessages(
//     //                 EmitCSV(supers, childContext, token).Messages
//     //             );

//     //             if(interfaces != null)
//     //             {
//     //                 context.Emission.Append(node, ",");

//     //                 result.AddMessages(
//     //                     EmitCSV(interfaces, childContext, token).Messages
//     //                 );
//     //             }
//     //         }
//     //         else if(interfaces != null)
//     //         {
//     //             context.Emission.Append(node, ":");

//     //             result.AddMessages(
//     //                 EmitCSV(interfaces, childContext, token).Messages
//     //             );
//     //         }

//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA from SwiftEmitter - 04/11/18
//     //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     //     private Result<object> EmitTypeDeclarationTemplate(Node template, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         if (template != null)
//     //         {
//     //             // [dho] TODO `where T : Foo` clauses - 21/09/18
//     //             context.Emission.Append(template, "<");

//     //             result.AddMessages(
//     //                 EmitCSV(template, context, token).Messages
//     //             );

//     //             context.Emission.Append(template, ">");
//     //         }

//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA from SwiftEmitter - 09/11/18
//     //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     //     private Result<object> EmitTypeDeclarationMembers(TypeDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         if (node.Members != null)
//     //         {
//     //             context.Emission.Indent();

//     //             foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(node.Members))
//     //             {
//     //                 context.Emission.AppendBelow(node.Members, "");

//     //                 result.AddMessages(
//     //                     EmitNode(member, context, token).Messages
//     //                 );
//     //             }

//     //             context.Emission.Outdent();
//     //         }

//     //         return result;
//     //     }

//     //     #endregion

//     //     public override Result<object> EmitConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.AppendBelow(node, "");

//     //         // [dho] TODO modifiers! - 20/09/18
//     //         // [dho] TODO super call - 29/09/18

//     //         if(node.Name != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Name, childContext, token).Messages
//     //             );
//     //         }
//     //         else
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Constructor must have a name", node)
//     //                 {
//     //                     Hint = GetHint(node.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         result.AddMessages(
//     //             EmitFunctionTemplate(node.Template, childContext, token).Messages
//     //         );

//     //         result.AddMessages(
//     //             EmitFunctionParameters(node.Parameters, childContext, token).Messages
//     //         );

//     //         if(node.Type != null)
//     //         {
//     //             result.AddMessages(
//     //                 CreateUnsupportedFeatureResult(node.Type, "constructor return types", childContext).Messages
//     //             );
//     //         }

//     //         result.AddMessages(
//     //             EmitBlockLike(node.Body, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitDestructorDeclaration(DestructorDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         context.Emission.AppendBelow(node, $"~");

//     //         if(node.Name != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Name, childContext, token).Messages
//     //             );
//     //         }
//     //         else
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Destructor must have a name", node)
//     //                 {
//     //                     Hint = GetHint(node.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         if(node.Parameters != null)
//     //         {
//     //             result.AddMessages(
//     //                 CreateUnsupportedFeatureResult(node.Parameters, "destructor parameters", childContext).Messages
//     //             );
//     //         }

//     //         if(node.Template != null)
//     //         {
//     //             result.AddMessages(
//     //                 CreateUnsupportedFeatureResult(node.Template, "destructor templates", childContext).Messages
//     //             );
//     //         }

//     //         if(node.Type != null)
//     //         {
//     //             result.AddMessages(
//     //                 CreateUnsupportedFeatureResult(node.Type, "destructor return types", childContext).Messages
//     //             );
//     //         }

//     //         result.AddMessages(
//     //             EmitBlockLike(node.Body, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitEnumerationMemberDeclaration(EnumerationMemberDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Name, childContext, token).Messages
//     //         );

//     //         if(node.Initializer != null)
//     //         {
//     //             context.Emission.Append(node.Initializer, "=");

//     //             result.AddMessages(
//     //                 EmitNode(node.Initializer, childContext, token).Messages
//     //             );
//     //         }

//     //         return result;
//     //     }

//     //     public override Result<object> EmitFieldDeclaration(FieldDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         if (node.Type != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Type, childContext, token).Messages
//     //             );

//     //             context.Emission.Append(node, " ");
//     //         }
//     //         else
//     //         {
//     //             result.AddMessages(
//     //                 new NodeMessage(MessageKind.Error, $"Field must have a type", node)
//     //                 {
//     //                     Hint = GetHint(node.Origin),
//     //                     Tags = DiagnosticTags
//     //                 }
//     //             );
//     //         }

//     //         result.AddMessages(
//     //             EmitNode(node.Name, childContext, token).Messages
//     //         );


//     //         if (node.Initializer != null)
//     //         {
//     //             context.Emission.Append(node.Initializer, "=");

//     //             result.AddMessages(
//     //                 EmitNode(node.Initializer, childContext, token).Messages
//     //             );
//     //         }

//     //         return result;
//     //     }

//     //     public override Result<object> EmitMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
//     //     {
//     //         return EmitFunctionLikeDeclaration(node, context, token);
//     //     }

//     //     public override Result<object> EmitInvocation(Invocation node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         var subject = node.Subject;
//     //         var template = node.Template;
//     //         var arguments = node.Arguments;

//     //         result.AddMessages(
//     //             EmitNode(subject, childContext, token).Messages
//     //         );

//     //         if (template != null)
//     //         {
//     //             context.Emission.Append(template, "<");

//     //             result.AddMessages(
//     //                 EmitCSV(template, childContext, token).Messages
//     //             );

//     //             context.Emission.Append(template, ">");
//     //         }

//     //         context.Emission.Append(node, "(");

//     //         if (arguments != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitCSV(arguments, childContext, token).Messages
//     //             );
//     //         }

//     //         context.Emission.Append(node, ")");

//     //         return result;
//     //     }

//     //     // [dho] COPYPASTA from SwiftEmitter - 12/01/19
//     //     public override Result<object> EmitInvocationArgument(InvocationArgument node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         if(node.Label != null)
//     //         {
//     //             result.AddMessages(
//     //                 EmitNode(node.Label, childContext, token).Messages
//     //             );

//     //             context.Emission.Append(node, " : ");
//     //         }

//     //         result.AddMessages(
//     //             EmitNode(node.Value, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }


//     //     public override Result<object> EmitQualifiedAccess(QualifiedAccess node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         var childContext = ContextHelpers.Clone(context);
//     //         // // childContext.Parent = node;

//     //         result.AddMessages(
//     //             EmitNode(node.Incident, childContext, token).Messages
//     //         );

//     //         context.Emission.Append(node, ".");

//     //         result.AddMessages(
//     //             EmitNode(node.Member, childContext, token).Messages
//     //         );

//     //         return result;
//     //     }

//     //     public override Result<object> EmitIncidentContextReference(IncidentContextReference node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, "this");

//     //         return result;
//     //     }

//     //     public override Result<object> EmitSuperContextReference(SuperContextReference node, Context context, CancellationToken token)
//     //     {
//     //         var result = new Result<object>();

//     //         context.Emission.Append(node, "base");

//     //         return result;
//     //     }
//     // }


// using System;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using System.Threading;
// using System.Threading.Tasks;
// using Sempiler.Diagnostics;
// using Sempiler.AST;
// using Sempiler.AST.Diagnostics;

// namespace Sempiler.Emission
// {
//     using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

//     public class CSharpEmitter : BaseEmitter
//     {
//         public CSharpEmitter() : base(new string[]{ "c#", PhaseKind.Emission.ToString("g").ToLower() })
//         {
//         }

//         public override Result<object> EmitAccessorDeclaration(AccessorDeclaration node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitAccessorSignature(AccessorSignature node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitAddition(Addition node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "+", context, token);
//         }

//         public override Result<object> EmitAdditionAssignment(AdditionAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "+=", context, token);
//         }

//         public override Result<object> EmitAddressOf(AddressOf node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitAnnotation(Annotation node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, "@");

//             result.AddMessages(
//                 EmitNode(node.Expression, context, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitArithmeticNegation(ArithmeticNegation node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "-");

//             result.AddMessages(
//                 EmitNode(node.Operand, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitArrayConstruction(ArrayConstruction node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             if(node.Type == null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }

//             context.Emission.Append(node, "new ");

//             result.AddMessages(
//                 EmitNode(node.Type, childContext, token)
//             );

//             if(node.Size != null)
//             {
//                 context.Emission.Append(node, "[");

//                 result.AddMessages(
//                     EmitNode(node.Size, childContext, token)
//                 );

//                 context.Emission.Append(node, "]");
//             }
//             else
//             {
//                 context.Emission.Append(node, "[]");
//             }

//             if(node.Members != null)
//             {
//                 context.Emission.Append(node, "{");

//                 result.AddMessages(
//                     EmitCSV(node.Members, childContext, token)
//                 );

//                 context.Emission.Append(node, "}");
//             }

//             return result;
//         }

//         public override Result<object> EmitArrayTypeReference(ArrayTypeReference node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Type, childContext, token)
//             );
            
//             context.Emission.Append(node, "[]");

//             return result;
//         }

//         public override Result<object> EmitAssignment(Assignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "=", context, token);
//         }

//         public override Result<object> EmitAssociation(Association node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, "(");

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Subject, childContext, token)  
//             );

//             context.Emission.Append(node, ")");

//             return result;
//         }

//         public override Result<object> EmitBitwiseAnd(BitwiseAnd node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "&", context, token);
//         }

//         public override Result<object> EmitBitwiseAndAssignment(BitwiseAndAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "&=", context, token);
//         }

//         public override Result<object> EmitBitwiseExclusiveOr(BitwiseExclusiveOr node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "^", context, token);
//         }

//         public override Result<object> EmitBitwiseExclusiveOrAssignment(BitwiseExclusiveOrAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "^=", context, token);
//         }

//         public override Result<object> EmitBitwiseLeftShift(BitwiseLeftShift node, Context context, CancellationToken token)
//         {
//             return EmitBitwiseShift(node, "<<", context, token);
//         }

//         public override Result<object> EmitBitwiseLeftShiftAssignment(BitwiseLeftShiftAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "<<=", context, token);
//         }

//         public override Result<object> EmitBitwiseNegation(BitwiseNegation node, Context context, CancellationToken token)
//         {
//             return EmitPrefixUnaryLike(node, "~", context, token);
//         }

//         public override Result<object> EmitBitwiseOr(BitwiseOr node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "|", context, token);
//         }

//         public override Result<object> EmitBitwiseOrAssignment(BitwiseOrAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "|=", context, token);
//         }

//         public override Result<object> EmitBitwiseRightShift(BitwiseRightShift node, Context context, CancellationToken token)
//         {
//             return EmitBitwiseShift(node, ">>", context, token);
//         }

//         public override Result<object> EmitBitwiseRightShiftAssignment(BitwiseRightShiftAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, ">>=", context, token);
//         }

//         public override Result<object> EmitBitwiseUnsignedRightShift(BitwiseUnsignedRightShift node, Context context, CancellationToken token)
//         {
//             return EmitBitwiseShift(node, ">>>", context, token);
//         }

//         public override Result<object> EmitBitwiseUnsignedRightShiftAssignment(BitwiseUnsignedRightShiftAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, ">>>=", context, token);
//         }

//         public override Result<object> EmitBlock(Block node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, "{");

//             if (node.Content != null)
//             {
//                 context.Emission.Indent();

//                 foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(node.Content))
//                 {
//                     context.Emission.AppendBelow(node, "");

//                     result.AddMessages(
//                         EmitNode(member, context, token)
//                     );

//                     if(RequiresSemicolonSentinel(member))
//                     {
//                         context.Emission.Append(member, ";");
//                     }
//                 }

//                 context.Emission.Outdent();
//             }

//             context.Emission.AppendBelow(node, "}");

//             return result;
//         }

//         public override Result<object> EmitBooleanConstant(BooleanConstant node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, node.Value ? "true" : "false");

//             return result;
//         }

//         public override Result<object> EmitBreakpoint(Breakpoint node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitClauseBreak(ClauseBreak node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "break");
            
//             if(node.Expression != null)
//             {
//                 context.Emission.Append(node, " ");

//                 result.AddMessages(EmitNode(node.Expression, childContext, token));
//             }

//             return result;
//         }

//         public override Result<object> EmitCodeConstant(CodeConstant node, Context context, CancellationToken token)
//         {
//             return base.EmitCodeConstant(node, context, token);
//         }

//         public override Result<object> EmitCollectionDestructuring(CollectionDestructuring node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitCompilerHint(CompilerHint node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         // [dho] TODO move to a helper for other emitters that will spit out 1 file per component, eg. CSharp etc - 21/09/18 (ported 24/04/19)
//         // [dho] TODO CLEANUP this is duplicated in CSEmitter (apart from FileEmission extension) - 21/09/18 (ported 24/04/19)
//         public override Result<object> EmitComponent(Component node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             if (node.Origin.Kind != NodeOriginKind.Source)
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Could not write emission in artifact due to unsupported origin kind '{node.Origin.Kind}'", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );

//                 return result;
//             }

//             var sourceWithLocation = ((SourceNodeOrigin)node.Origin).Source as ISourceWithLocation<IFileLocation>;

//             if (sourceWithLocation == null || sourceWithLocation.Location == null)
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Could not write emission in artifact because output location cannot be determined", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );

//                 return result;
//             }

//             var location = sourceWithLocation.Location;

//             // [dho] our emissions will be stored in a file with the same relative path and name
//             // as the original source for this component, eg. hello/World.ts => hello/World.java - 26/04/19
//             var file = new FileEmission(
//                 FileSystem.CreateFileLocation(location.ParentDir, location.Name, "java")
//             );

//             if (context.OutFileCollection.Contains(file.Destination))
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Could not write emission in artifact at '{file.Destination.ToPathString()}' because location already exists", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );
//             }

//             var childContext = ContextHelpers.Clone(context);
//             childContext.Component = node;
//             // // childContext.Parent = node;
//             childContext.Emission = file; // [dho] this is where children of this component should write to - 29/08/18

//             foreach (var (child, hasNext) in NodeInterrogation.IterateChildren(node))
//             {
//                 result.AddMessages(
//                     EmitNode(child, childContext, token)
//                 );

//                 childContext.Emission.AppendBelow(node, "");

//                 if (token.IsCancellationRequested)
//                 {
//                     break;
//                 }
//             }

//             if (!Diagnostics.DiagnosticsHelpers.HasErrors(result))
//             {
//                 context.OutFileCollection[file.Destination] = file;
//             }

//             return result;
//         }

//         public override Result<object> EmitConcatenation(Concatenation node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             var operands = node.Operands;

//             result.AddMessages(EmitDelimited(operands, "+", childContext, token));

//             return result;
//         }

//         public override Result<object> EmitConcatenationAssignment(ConcatenationAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "+=", context, token);
//         }

//         public override Result<object> EmitConditionalTypeReference(ConditionalTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitLiteralTypeReference(LiteralTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.AppendBelow(node, "");

//             // [dho] TODO modifiers! - 20/09/18
//             // [dho] TODO super call - 29/09/18

//             if(node.Name != null)
//             {
//                 result.AddMessages(
//                     EmitNode(node.Name, childContext, token)
//                 );
//             }
//             else
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Constructor must have a name", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );
//             }

//             result.AddMessages(
//                 EmitFunctionTemplate(node, childContext, token)
//             );

//             result.AddMessages(
//                 EmitFunctionParameters(node, childContext, token)
//             );

//             if(node.Type != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Type, "constructor return types", childContext)
//                 );
//             }

//             result.AddMessages(
//                 EmitBlockLike(node.Body, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitConstructorSignature(ConstructorSignature node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitConstructorTypeReference(ConstructorTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDataValueDeclaration(DataValueDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // [dho] TODO EmitOrnamentation - 01/03/19
//             // final, etc.

//             if (node.Type != null)
//             {
//                 result.AddMessages(
//                     EmitNode(node.Type, childContext, token)
//                 );
//             }
//             else
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }

//             context.Emission.Append(node, " ");

//             result.AddMessages(
//                 EmitNode(node.Name, childContext, token)
//             );

//             if (node.Initializer != null)
//             {
//                 context.Emission.Append(node, "=");

//                 result.AddMessages(
//                     EmitNode(node.Initializer, childContext, token)
//                 );
//             }

//             return result;
//         }

//         public override Result<object> EmitDefaultExportReference(DefaultExportReference node, Context context, CancellationToken token)
//         {
//             return base.EmitDefaultExportReference(node, context, token);
//         }

//         public override Result<object> EmitDestruction(Destruction node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDestructorDeclaration(DestructorDeclaration node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDestructorSignature(DestructorSignature node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDestructuredMember(DestructuredMember node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDictionaryConstruction(DictionaryConstruction node, Context context, CancellationToken token)
//         {
//             // [dho] should we use HashMap here? But the interface for it is
//             // different, so that would have needed to be abstracted away? - 26/04/19
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDictionaryTypeReference(DictionaryTypeReference node, Context context, CancellationToken token)
//         {
//             // [dho] should we use HashMap here? But the interface for it is
//             // different, so that would have needed to be abstracted away? - 26/04/19
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDivision(Division node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "/", context, token);
//         }

//         public override Result<object> EmitDivisionAssignment(DivisionAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "/=", context, token);
//         }

//         public override Result<object> EmitDoOrDieErrorTrap(DoOrDieErrorTrap node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDoOrRecoverErrorTrap(DoOrRecoverErrorTrap node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDoWhilePredicateLoop(DoWhilePredicateLoop node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.AppendBelow(node, "do");

//             result.AddMessages(EmitBlockLike(node.Body, childContext, token));

//             context.Emission.AppendBelow(node, "while(");

//             result.AddMessages(EmitNode(node.Condition, childContext, token));

//             context.Emission.Append(node, ")");

//             return result;
//         }

//         public override Result<object> EmitDomain(Domain node, Context context, CancellationToken token)
//         {
//             return base.EmitDomain(node, context, token);
//         }

//         public override Result<object> EmitDynamicTypeConstruction(DynamicTypeConstruction node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitDynamicTypeReference(DynamicTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitEntityDestructuring(EntityDestructuring node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitEnumerationMemberDeclaration(EnumerationMemberDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Name, childContext, token)
//             );

//             if(node.Initializer != null)
//             {
//                 context.Emission.Append(node.Initializer, "=");

//                 result.AddMessages(
//                     EmitNode(node.Initializer, childContext, token)
//                 );
//             }

//             return result;
//         }

//         public override Result<object> EmitEnumerationTypeDeclaration(EnumerationTypeDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "enum ");

//             if (node.Name != null)
//             {
//                 var name = node.Name;

//                 if (name.Kind == SemanticKind.Identifier)
//                 {
//                     result.AddMessages(
//                         EmitIdentifier(NodeInterrogation.Identifier(node.AST, name), childContext, token)
//                     );
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Enumeration type declaration has unsupported name type : '{name.Kind}'", name)
//                         {
//                             Hint = GetHint(name.Origin),
//                             Tags = DiagnosticTags
//                         }
//                     );
//                 }
//             }
//             else
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }

//             // generics
//             if(node.Template != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }

//             if(node.Supers != null || node.Interfaces != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }

//             context.Emission.Append(node, "{");

//             result.AddMessages(
//                 EmitTypeDeclarationMembers(node, childContext, token)
//             );

//             context.Emission.AppendBelow(node, "}");

//             return result;
//         }

//         public override Result<object> EmitErrorFinallyClause(ErrorFinallyClause node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.AppendBelow(node, "finally ");

//             if(node.Expression != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Expression, "finally clause expression", context)
//                 );
//             }

//             result.AddMessages(
//                 EmitBlockLike(node.Body, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitErrorHandlerClause(ErrorHandlerClause node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.AppendBelow(node, "catch ");

//             if(node.Expression != null)
//             {
//                 context.Emission.Append(node, "(");

//                 result.AddMessages(
//                     EmitNode(node.Expression, childContext, token)
//                 );

//                 context.Emission.Append(node, ")");
//             }

//             result.AddMessages(
//                 EmitBlockLike(node.Body, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitErrorTrapJunction(ErrorTrapJunction node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.AppendBelow(node, "try ");

//             result.AddMessages(
//                 EmitBlockLike(node.Subject, childContext, token)
//             );
            
//             result.AddMessages(EmitNode(node.Clauses, childContext, token));

//             return result;
//         }

//         public override Result<object> EmitEvalToVoid(EvalToVoid node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitExponentiation(Exponentiation node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitExponentiationAssignment(ExponentiationAssignment node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitExportDeclaration(ExportDeclaration node, Context context, CancellationToken token)
//         {
//             return base.EmitExportDeclaration(node, context, token);
//         }

//         public override Result<object> EmitFieldDeclaration(FieldDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             if (node.Type != null)
//             {
//                 result.AddMessages(
//                     EmitNode(node.Type, childContext, token)
//                 );

//                 context.Emission.Append(node, " ");
//             }
//             else
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Field must have a type", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );
//             }

//             result.AddMessages(
//                 EmitNode(node.Name, childContext, token)
//             );


//             if (node.Initializer != null)
//             {
//                 context.Emission.Append(node.Initializer, "=");

//                 result.AddMessages(
//                     EmitNode(node.Initializer, childContext, token)
//                 );
//             }

//             return result;
//         }

//         public override Result<object> EmitFieldSignature(FieldSignature node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             if (node.Type != null)
//             {
//                 context.Emission.AppendBelow(node, " ");

//                 result.AddMessages(
//                     EmitNode(node.Type, childContext, token)
//                 );

//                 context.Emission.Append(node, " ");
//             }
//             else
//             {
//                 result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Field must have a type", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                     );
//             }
        
//             result.AddMessages(
//                 EmitNode(node.Name, childContext, token)
//             );


//             if (node.Initializer != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Initializer, "field signature initializer", childContext)
//                 );
//             }

//             return result;
//         }

//         public override Result<object> EmitForKeysLoop(ForKeysLoop node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitForMembersLoop(ForMembersLoop node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.AppendBelow(node, "for(");

//             result.AddMessages(
//                 EmitNode(node.Handle, childContext, token)
//             );

//             context.Emission.Append(node, " : ");

//             result.AddMessages(
//                 EmitNode(node.Subject, childContext, token)
//             );

//             context.Emission.Append(node, ")");

//             result.AddMessages(
//                 EmitBlockLike(node.Body, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitForPredicateLoop(ForPredicateLoop node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.AppendBelow(node, "for(");

//             if(node.Initializer != null)
//             {
//                 result.AddMessages(
//                     EmitNode(node.Initializer, childContext, token)
//                 );
//             }

//             context.Emission.Append(node, ";");

//             if(node.Condition != null)
//             {
//                 result.AddMessages(
//                     EmitNode(node.Condition, childContext, token)
//                 );
//             }

//             context.Emission.Append(node, ";");

//             if(node.Iterator != null)
//             {
//                 result.AddMessages(
//                     EmitNode(node.Iterator, childContext, token)
//                 );
//             }

//             context.Emission.Append(node, ")");

//             result.AddMessages(
//                 EmitBlockLike(node.Body, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitForcedCast(ForcedCast node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node.TargetType, "(");
            
//             result.AddMessages(
//                 EmitNode(node.TargetType, childContext, token)
//             );

//             context.Emission.Append(node.TargetType, ")");

//             result.AddMessages(
//                 EmitNode(node.Subject, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
//         {
//             // [dho] because CSharp only has methods right?? - 26/04/19
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitFunctionTermination(FunctionTermination node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             if(node.Value != null)
//             {
//                 context.Emission.Append(node, "return ");

//                 var childContext = ContextHelpers.Clone(context);
//                 // // childContext.Parent = node;

//                 result.AddMessages(
//                     EmitNode(node.Value, childContext, token)
//                 );

//                 // [dho] end of return statment - 21/09/18
//                 // context.Emission.Append(node, ";");
//             }
//             else
//             {
//                 context.Emission.Append(node, "return");
//             }

//             return result;
//         }

//         public override Result<object> EmitFunctionTypeReference(FunctionTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitGeneratorSuspension(GeneratorSuspension node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitGlobalDeclaration(GlobalDeclaration node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitIdentity(Identity node, Context context, CancellationToken token)
//         {
//             return EmitPrefixUnaryLike(node, "+", context, token);
//         }

//         public override Result<object> EmitIfDirective(IfDirective node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // [dho] TODO EmitOrnamentation - 01/03/19
//             // if(!node.Required)
//             // {
//             //     result.AddMessages(
//             //         CreateUnsupportedFeatureResult(node, context)
//             //     );
//             // }

//             // [dho] TODO if the import is the default import then we should just append a 
//             // '*' to the imported package name - 26/04/19
//             if(node.Clauses != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Clauses, context)
//                 );
//             }
            
//             context.Emission.Append(node, "import ");

//             result.AddMessages(
//                 EmitNode(node.Specifier, childContext, token)
//             );

//             context.Emission.Append(node, ";");
            
//             return result;
//         }

//         public override Result<object> EmitIncidentContextReference(IncidentContextReference node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, "this");

//             return result;
//         }

//         public override Result<object> EmitIncidentTypeConstraint(IncidentTypeConstraint node, Context context, CancellationToken token)
//         {
//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "implements ");

//             return EmitNode(node.Type, context, token);
//         }

//         public override Result<object> EmitIndexTypeQuery(IndexTypeQuery node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitInferredTypeQuery(InferredTypeQuery node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitIndexedAccess(IndexedAccess node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Incident, childContext, token)
//             );

//             context.Emission.Append(node, "[");

//             result.AddMessages(
//                 EmitNode(node.Member, childContext, token)
//             );

//             context.Emission.Append(node, "]");

//             return result;
//         }

//         public override Result<object> EmitIndexerSignature(IndexerSignature node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitInterfaceDeclaration(InterfaceDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // [dho] TODO modifiers! - 21/09/18

//             if(node.Name != null)
//             {
//                 var name = node.Name;

//                 if(name.Kind == SemanticKind.Identifier)
//                 {
//                     context.Emission.Append(node, "interface ");
                    
//                     result.AddMessages(
//                         EmitIdentifier(NodeInterrogation.Identifier(node.AST, name), childContext, token)
//                     );
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Interface type declaration has unsupported name type : '{name.Kind}'", name)
//                         {
//                             Hint = GetHint(name.Origin),
//                             Tags = DiagnosticTags
//                         }
//                     );
//                 }
//             }
//             else
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }

//             // generics
//             if(node.Template != null)
//             {
//                 result.AddMessages(
//                     EmitTypeDeclarationTemplate(node, childContext, token)
//                 );
//             }

//             result.AddMessages(
//                 EmitTypeDeclarationHeritage(node, context, token)
//             );

//             context.Emission.Append(node, "{");

//             result.AddMessages(
//                 EmitTypeDeclarationMembers(node, childContext, token)
//             );

//             context.Emission.AppendBelow(node, "}");

//             return result;
//         }

//         public override Result<object> EmitInterimSuspension(InterimSuspension node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitInterpolatedString(InterpolatedString node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitInterpolatedStringConstant(InterpolatedStringConstant node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitIntersectionTypeReference(IntersectionTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitIntrinsicTypeReference(IntrinsicTypeReference node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             switch(node.Role)
//             {
//                 case TypeRole.Boolean:
//                     context.Emission.Append(node, "boolean");
//                 break;

//                 case TypeRole.Char:
//                     context.Emission.Append(node, "char");
//                 break;

//                 case TypeRole.Double64:
//                     context.Emission.Append(node, "double");
//                 break;

//                 case TypeRole.Float32:
//                     context.Emission.Append(node, "float");
//                 break;

//                 case TypeRole.Integer32:
//                     context.Emission.Append(node, "int");
//                 break;

//                 case TypeRole.RootObject:
//                     context.Emission.Append(node, "Object");
//                 break;

//                 case TypeRole.String:
//                     context.Emission.Append(node, "String");
//                 break;

//                 case TypeRole.Void:
//                     context.Emission.Append(node, "void");
//                 break;

//                 default:
//                     result.AddMessages(
//                         CreateUnsupportedFeatureResult(node, $"Intrisinsic {node.Role.ToString()} types", context)
//                     );
//                 break;
//             }

//             return result;
//         }

//         public override Result<object> EmitInvocation(Invocation node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             var subject = node.Subject;
//             var template = node.Template;
//             var arguments = node.Arguments;

//             result.AddMessages(
//                 EmitNode(subject, childContext, token)
//             );

//             if (template.Length > 0)
//             {
//                 context.Emission.Append(node, "<");

//                 result.AddMessages(
//                     EmitCSV(template, childContext, token)
//                 );

//                 context.Emission.Append(node, ">");
//             }

//             context.Emission.Append(node, "(");

//             if (arguments != null)
//             {
//                 result.AddMessages(
//                     EmitCSV(arguments, childContext, token)
//                 );
//             }

//             context.Emission.Append(node, ")");

//             return result;
//         }

//         public override Result<object> EmitInvocationArgument(InvocationArgument node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             if(node.Label != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Label, "argument labels", childContext)
//                 );
//             }

//             result.AddMessages(
//                 EmitNode(node.Value, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitJumpToNextIteration(JumpToNextIteration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "continue");
            
//             if(node.Label != null)
//             {
//                 context.Emission.Append(node, " ");

//                 result.AddMessages(EmitNode(node.Label, childContext, token));
//             }

//             return result;
//         }

//         public override Result<object> EmitKeyValuePair(KeyValuePair node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitLabel(Label node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(EmitNode(node.Name, childContext, token));

//             context.Emission.Append(node, ":");

//             return result;
//         }

//         public override Result<object> EmitLambdaDeclaration(LambdaDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // [dho] TODO modifiers! -16/11/18

//             var name = node.Name;
//             var template = node.Template;

//             if (name != null)
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Lambda must be anonymous", name)
//                     {
//                         Hint = GetHint(name.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );
//             }



//             if (template.Length > 0)
//             {
//                 var templateMarker = template[0];

//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Lambda must not be templated", templateMarker)
//                     {
//                         Hint = GetHint(templateMarker.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );
//             }
            

//             result.AddMessages(
//                 EmitFunctionParameters(node, childContext, token)
//             );

//             if(node.Type != null)
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Lambda must not have type", node.Type)
//                     {
//                         Hint = GetHint(node.Type.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );
//             }

//             context.Emission.Append(node, "->");

//             result.AddMessages(
//                 EmitBlockLike(node.Body, childContext, token)
//             );
          
//             return result;
//         }

//         public override Result<object> EmitLogicalAnd(LogicalAnd node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "&&", context, token);
//         }

//         public override Result<object> EmitLogicalNegation(LogicalNegation node, Context context, CancellationToken token)
//         {
//             return EmitPrefixUnaryLike(node, "!", context, token);
//         }

//         public override Result<object> EmitLogicalOr(LogicalOr node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "||", context, token);
//         }

//         public override Result<object> EmitLoopBreak(LoopBreak node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "break");
            
//             if(node.Expression != null)
//             {
//                 context.Emission.Append(node, " ");

//                 result.AddMessages(EmitNode(node.Expression, childContext, token));
//             }

//             return result;
//         }

//         public override Result<object> EmitLooseEquivalent(LooseEquivalent node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitLooseNonEquivalent(LooseNonEquivalent node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitLowerBoundedTypeConstraint(LowerBoundedTypeConstraint node, Context context, CancellationToken token)
//         {
//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "super ");

//             return EmitNode(node.Type, context, token);
//         }

//         public override Result<object> EmitMappedDestructuring(MappedDestructuring node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitMappedTypeReference(MappedTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitMatchClause(MatchClause node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             if(node.Expression != null)
//             {
//                 context.Emission.Append(node, "case ");

//                 result.AddMessages(
//                     EmitNode(node.Expression, context, token)
//                 );

//                 context.Emission.Append(node, ":");
//             }
//             else
//             {
//                 context.Emission.Append(node, "default:");
//             }

//             result.AddMessages(
//                 EmitBlockLike(node.Body, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitMatchJunction(MatchJunction node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "switch(");

//             result.AddMessages(
//                 EmitNode(node.Subject, childContext, token)
//             );

//             context.Emission.Append(node, ")");

//             result.AddMessages(
//                 EmitBlockLike(node.Clauses, childContext, token)
//             );
            
//             return result;
//         }

//         public override Result<object> EmitMaybeNull(MaybeNull node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitMemberNameReflection(MemberNameReflection node, Context context, CancellationToken token)
//         {
//             return base.EmitMemberNameReflection(node, context, token);
//         }

//         public override Result<object> EmitMemberTypeConstraint(MemberTypeConstraint node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitMembershipTest(MembershipTest node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitMeta(Meta node, Context context, CancellationToken token)
//         {
//             return base.EmitMeta(node, context, token);
//         }

//         public override Result<object> EmitMetaProperty(MetaProperty node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
//         {
//             return EmitFunctionLikeDeclaration(node, context, token);
//         }

//         public override Result<object> EmitMethodSignature(MethodSignature node, Context context, CancellationToken token)
//         {
//             return EmitFunctionLikeSignature(node, context, token);
//         }

//         public override Result<object> EmitModifier(Modifier node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, NodeInterrogation.GetLexeme(node));

//             return result;
//         }

//         public override Result<object> EmitMultiplication(Multiplication node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "*", context, token);
//         }

//         public override Result<object> EmitMultiplicationAssignment(MultiplicationAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "*=", context, token);
//         }

//         public override Result<object> EmitMutatorDeclaration(MutatorDeclaration node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitMutatorSignature(MutatorSignature node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitMutex(AST.Mutex node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitNamedTypeConstruction(NamedTypeConstruction node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "new ");

//             result.AddMessages(
//                 EmitNode(node.Name, childContext, token)
//             );

//             var template = node.Template;

//             if(node.Template != null)
//             {
//                 context.Emission.Append(node, "<");

//                 result.AddMessages(
//                     EmitCSV(template, context, token)
//                 );

//                 context.Emission.Append(node, ">");
//             }

//             context.Emission.Append(node, "(");

//             if(node.Arguments != null)
//             {
//                 result.AddMessages(
//                     EmitCSV(node.Arguments, childContext, token)
//                 );
//             }

//             context.Emission.Append(node, ")");

//             return result;
//         }

//         public override Result<object> EmitNamedTypeQuery(NamedTypeQuery node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitNamedTypeReference(NamedTypeReference node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Name, childContext, token)
//             );

//             if(node.Template != null)
//             {
//                 context.Emission.Append(node, "<");

//                 result.AddMessages(
//                     EmitCSV(node.Template, childContext, token)
//                 );

//                 context.Emission.Append(node, ">");
//             }

//             return result;
//         }

//         public override Result<object> EmitNamespaceDeclaration(NamespaceDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "package ");

//             if (node.Name != null)
//             {
//                 var name = node.Name;

//                 if (name.Kind == SemanticKind.Identifier)
//                 {
//                     result.AddMessages(
//                         EmitIdentifier(NodeInterrogation.Identifier(node.AST, name), childContext, token)
//                     );
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Namespace declaration has unsupported name type : '{name.Kind}'", name)
//                         {
//                             Hint = GetHint(name.Origin),
//                             Tags = DiagnosticTags
//                         }
//                     );
//                 }
//             }
//             else
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }

//             foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(node.Members))
//             {
//                 context.Emission.AppendBelow(member, "");

//                 result.AddMessages(
//                     EmitNode(member, context, token)
//                 );
//             }

//             return result;
//         }

//         public override Result<object> EmitNamespaceReference(NamespaceReference node, Context context, CancellationToken token)
//         {
//             return base.EmitNamespaceReference(node, context, token);
//         }

//         public override Result<object> EmitNonMembershipTest(NonMembershipTest node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitNop(Nop node, Context context, CancellationToken token)
//         {
//             return base.EmitNop(node, context, token);
//         }

//         public override Result<object> EmitNotNull(NotNull node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitNotNumber(NotNumber node, Context context, CancellationToken token)
//         {
//             return base.EmitNotNumber(node, context, token);
//         }

//         public override Result<object> EmitNull(Null node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, "null");

//             return result;
//         }

//         public override Result<object> EmitNullCoalescence(NullCoalescence node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(EmitDelimited(node.Operands, "??", childContext, token));

//             return result;
//         }

//         public override Result<object> EmitNumericConstant(NumericConstant node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, node.Value.ToString());

//             return result;
//         }

//         public override Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // [dho] TODO modifiers! - 21/09/18

//             if(node.Name != null)
//             {
//                 var name = node.Name;

//                 if(name.Kind == SemanticKind.Identifier)
//                 {
//                     context.Emission.Append(node, "class ");
                    
//                     result.AddMessages(
//                         EmitIdentifier(NodeInterrogation.Identifier(node.AST, name), childContext, token)
//                     );
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Object type declaration has unsupported name type : '{name.Kind}'", name)
//                         {
//                             Hint = GetHint(name.Origin),
//                             Tags = DiagnosticTags
//                         }
//                     );
//                 }
//             }
//             else
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }

//             // generics
//             if(node.Template != null)
//             {
//                 result.AddMessages(
//                     EmitTypeDeclarationTemplate(node, childContext, token)
//                 );
//             }

//             result.AddMessages(
//                 EmitTypeDeclarationHeritage(node, context, token)
//             );

//             context.Emission.Append(node, "{");

//             result.AddMessages(
//                 EmitTypeDeclarationMembers(node, childContext, token)
//             );

//             context.Emission.AppendBelow(node, "}");

//             return result;
//         }

//         public override Result<object> EmitOrderedGroup(OrderedGroup node, Context context, CancellationToken token)
//         {
//             return base.EmitOrderedGroup(node, context, token);
//         }

//         public override Result<object> EmitParameterDeclaration(ParameterDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             if(node.Type != null)
//             {
//                 result.AddMessages(
//                     EmitNode(node.Type, childContext, token)
//                 );
                
//                 context.Emission.Append(node, " ");
//             }
//             else if(node.Parent.Kind != SemanticKind.LambdaDeclaration)
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Expected parameter to have a specified type", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );
//             }

//             if(node.Label != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Label, "parameter label", childContext)
//                 );
//             }

//             result.AddMessages(
//                 EmitNode(node.Name, childContext, token)
//             );


//             if(node.Default != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Default, "parameter default value", childContext)
//                 );
//             }

//             return result;
//         }

//         public override Result<object> EmitParenthesizedTypeReference(ParenthesizedTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitPointerDereference(PointerDereference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitPostDecrement(PostDecrement node, Context context, CancellationToken token)
//         {
//             return EmitPostUnaryArithmetic(node, "--", context, token);
//         }

//         public override Result<object> EmitPostIncrement(PostIncrement node, Context context, CancellationToken token)
//         {
//             return EmitPostUnaryArithmetic(node, "++", context, token);
//         }

//         public override Result<object> EmitPreDecrement(PreDecrement node, Context context, CancellationToken token)
//         {
//             return EmitPreUnaryArithmetic(node, "--", context, token);
//         }

//         public override Result<object> EmitPreIncrement(PreIncrement node, Context context, CancellationToken token)
//         {
//             return EmitPreUnaryArithmetic(node, "++", context, token);
//         }

//         public override Result<object> EmitPredicateFlat(PredicateFlat node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Predicate, childContext, token)
//             );

//             context.Emission.Append(node, "?");

//             result.AddMessages(
//                 EmitNode(node.TrueValue, childContext, token)
//             );

//             context.Emission.Append(node, ":");

//             result.AddMessages(
//                 EmitNode(node.FalseValue, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitPredicateJunction(PredicateJunction node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "if(");

//             result.AddMessages(
//                 EmitNode(node.Predicate, childContext, token)
//             );

//             context.Emission.Append(node, ")");

//             result.AddMessages(
//                 EmitBlockLike(node.TrueBranch, childContext, token)
//             );

//             if(node.FalseBranch != null)
//             {
//                 context.Emission.AppendBelow(node, "else ");

//                 if(node.FalseBranch.Kind == SemanticKind.PredicateJunction)
//                 {
//                     result.AddMessages(
//                         EmitPredicateJunction(NodeInterrogation.PredicateJunction(node.AST, node.FalseBranch), childContext, token)
//                     );
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         EmitBlockLike(node.FalseBranch, childContext, token)
//                     );
//                 }
//             }
            
//             return result;
//         }

//         public override Result<object> EmitPrioritySymbolResolutionContext(PrioritySymbolResolutionContext node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitPropertyDeclaration(PropertyDeclaration node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitQualifiedAccess(QualifiedAccess node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Incident, childContext, token)
//             );

//             context.Emission.Append(node, ".");

//             result.AddMessages(
//                 EmitNode(node.Member, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitRaiseError(RaiseError node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "throw");
            
//             if(node.Subject != null)
//             {
//                 context.Emission.Append(node, " ");

//                 result.AddMessages(EmitNode(node.Subject, childContext, token));
//             }

//             return result;
//         }

//         public override Result<object> EmitReferenceAliasDeclaration(ReferenceAliasDeclaration node, Context context, CancellationToken token)
//         {
//             return base.EmitReferenceAliasDeclaration(node, context, token);
//         }

//         public override Result<object> EmitRegularExpressionConstant(RegularExpressionConstant node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitRemainder(Remainder node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "%", context, token);
//         }

//         public override Result<object> EmitRemainderAssignment(RemainderAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "*=", context, token);
//         }

//         public override Result<object> EmitRunDirective(RunDirective node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitSafeCast(SafeCast node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitSmartCast(SmartCast node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitSpreadDestructuring(SpreadDestructuring node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitStrictEquivalent(StrictEquivalent node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "==", context, token);
//         }

//         public override Result<object> EmitStrictGreaterThan(StrictGreaterThan node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, ">", context, token);
//         }

//         public override Result<object> EmitStrictGreaterThanOrEquivalent(StrictGreaterThanOrEquivalent node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, ">=", context, token);
//         }

//         public override Result<object> EmitStrictLessThan(StrictLessThan node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "<", context, token);
//         }

//         public override Result<object> EmitStrictLessThanOrEquivalent(StrictLessThanOrEquivalent node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "<=", context, token);
//         }

//         public override Result<object> EmitStrictNonEquivalent(StrictNonEquivalent node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "!=", context, token);
//         }

//         public override Result<object> EmitStringConstant(StringConstant node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             if(NodeInterrogation.IsMultilineString(node))
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node, context)
//                 );
//             }
//             else
//             {
//                 context.Emission.Append(node, $"\"{node.Value}\"");
//             }

//             return result;
//         }

//         public override Result<object> EmitSubtraction(Subtraction node, Context context, CancellationToken token)
//         {
//             return EmitBinaryLike(node, "-", context, token);
//         }

//         public override Result<object> EmitSubtractionAssignment(SubtractionAssignment node, Context context, CancellationToken token)
//         {
//             return EmitAssignmentLike(node, "-=", context, token);
//         }

//         public override Result<object> EmitSuperContextReference(SuperContextReference node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, "super");

//             return result;
//         }

//         public override Result<object> EmitTupleConstruction(TupleConstruction node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitTupleTypeReference(TupleTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitTypeAliasDeclaration(TypeAliasDeclaration node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitTypeInterrogation(TypeInterrogation node, Context context, CancellationToken token)
//         {
//             return base.EmitTypeInterrogation(node, context, token);
//         }

//         public override Result<object> EmitTypeParameterDeclaration(TypeParameterDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Name, childContext, token)
//             );

//             if(node.Constraints != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Constraints, "type constraints", childContext)
//                 );
//             }

//             // if (node.SuperConstraints != null)
//             // {
//             //     context.Emission.Append(node.SuperConstraints, " extends ");

//             //     result.AddMessages(
//             //         EmitDelimited(node.SuperConstraints, "&", childContext, token)
//             //     );

//             //     if(NodeInterrogation.MemberCount(node.SubConstraints) > 0)
//             //     {
//             //         context.Emission.Append(node, ",");
//             //     }
//             // }

//             // if (node.SubConstraints != null)
//             // {
//             //     context.Emission.Append(node, " super ");

//             //     result.AddMessages(
//             //         EmitDelimited(node.SubConstraints, "&", childContext, token)
//             //     );
//             // }

//             if (node.Default != null)
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node.Default, "default type constraints", childContext)
//                 );
//             }

//             return result;
//         }

//         public override Result<object> EmitTypeQuery(TypeQuery node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitTypeTest(TypeTest node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Subject, childContext, token)
//             );

//             context.Emission.Append(node, " instanceof ");
            
//             result.AddMessages(
//                 EmitNode(node.Criteria, childContext, token)
//             );

//             return result;
//         }

//         public override Result<object> EmitUpperBoundedTypeConstraint(UpperBoundedTypeConstraint node, Context context, CancellationToken token)
//         {
//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.Append(node, "extends ");

//             return EmitNode(node.Type, context, token);
//         }

//         public override Result<object> EmitUnionTypeReference(UnionTypeReference node, Context context, CancellationToken token)
//         {
//             return CreateUnsupportedFeatureResult(node, context);
//         }

//         public override Result<object> EmitWhilePredicateLoop(WhilePredicateLoop node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             context.Emission.AppendBelow(node, "while(");

//             result.AddMessages(EmitNode(node.Condition, childContext, token));

//             context.Emission.Append(node, ")");

//             result.AddMessages(EmitBlockLike(node.Body, childContext, token));

//             return result;
//         }

//         public override Result<object> EmitWildcardExportReference(WildcardExportReference node, Context context, CancellationToken token)
//         {
//             return base.EmitWildcardExportReference(node, context, token);
//         }

//         private Result<object> EmitBinaryLike(BinaryLike node, string operatorToken, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             var operands = node.Operands;

//             result.AddMessages(EmitDelimited(operands, operatorToken, childContext, token));

//             return result;
//         }

//         private Result<object> EmitPrefixUnaryLike(UnaryLike node, string operatorToken, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // operator
//             context.Emission.Append(node, operatorToken);
            
//             // operand
//             result.AddMessages(
//                 EmitNode(node.Operand, childContext, token)
//             );

//             return result;
//         }

//         private Result<object> EmitBitwiseShift(BitwiseShift node, string operatorToken, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // left operand
//             result.AddMessages(
//                 EmitNode(node.Subject, childContext, token)
//             );

//             // operator
//             context.Emission.Append(node, operatorToken);

//             // right operand
//             result.AddMessages(
//                 EmitNode(node.Offset, childContext, token)
//             );

//             return result;
//         }

//         private Result<object> EmitAssignmentLike(AssignmentLike node, string operatorToken, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Storage, childContext, token)
//             );

//             // operator
//             context.Emission.Append(node, $" {operatorToken} ");

//             result.AddMessages(
//                 EmitNode(node.Value, childContext, token)
//             );

//             return result;
//         }

//         #region type parts

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         protected Result<object> EmitTypeDeclarationHeritage(TypeDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             if(node.Supers != null)
//             {
//                 context.Emission.Append(node, "extends ");

//                 result.AddMessages(
//                     EmitCSV(node.Supers, childContext, token)
//                 );
//             }
            
//             if(node.Interfaces != null)
//             {
//                 context.Emission.Append(node, "implements ");

//                 result.AddMessages(
//                     EmitCSV(node.Interfaces, childContext, token)
//                 );
//             }

//             return result;
//         }

//         // [dho] COPYPASTA from SwiftEmitter - 04/11/18
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         protected Result<object> EmitTypeDeclarationTemplate(TypeDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var template = node.Template;

//             if (template.Length > 0)
//             {
//                 // [dho] TODO `where T : Foo` clauses - 21/09/18
//                 context.Emission.Append(node, "<");

//                 result.AddMessages(
//                     EmitCSV(template, context, token)
//                 );

//                 context.Emission.Append(node, ">");
//             }

//             return result;
//         }

//         // [dho] COPYPASTA from SwiftEmitter - 09/11/18
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         protected Result<object> EmitTypeDeclarationMembers(TypeDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             if (node.Members != null)
//             {
//                 context.Emission.Indent();

//                 foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(node.Members))
//                 {
//                     context.Emission.AppendBelow(member, "");

//                     result.AddMessages(
//                         EmitNode(member, context, token)
//                     );
//                 }

//                 context.Emission.Outdent();
//             }

//             return result;
//         }

//         #endregion

//         #region function parts

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private Result<object> EmitFunctionLikeDeclaration(FunctionLikeDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // [dho] TODO EmitOrnamentation - 01/03/19
//             // if(node.Override)
//             // {
//             //     context.Emission.Append(node, "@Override ");
//             // }

//             // if(node.Static)
//             // {
//             //     context.Emission.Append(node, "static ");
//             // }

//             if(node.Name != null)
//             {

//                 // [dho] TODO reuse function signature emission - 29/09/18

//                 // [dho] TODO modifiers! - 21/09/18
                
//                 if(node.Name.Kind == SemanticKind.Identifier)
//                 {
//                     // return type
//                     if(node.Type != null)
//                     {
//                         result.AddMessages(
//                             EmitNode(node.Type, childContext, token)
//                         );

//                         // [dho] the space after the return type - 21/09/18
//                         context.Emission.Append(node, " ");
//                     }
//                     else
//                     {
//                         result.AddMessages(
//                             new NodeMessage(MessageKind.Error, $"Expected function to have a specified return type", node)
//                             {
//                                 Hint = GetHint(node.Origin),
//                                 Tags = DiagnosticTags
//                             }
//                         );
//                     }

//                     result.AddMessages(
//                         EmitIdentifier(NodeInterrogation.Identifier(node.AST, node.Name), childContext, token)
//                     );

//                     // generics
//                     result.AddMessages(
//                         EmitFunctionTemplate(node, childContext, token)
//                     );

//                     // [dho] Now we can emit the parameter list after the diamond - 29/09/18
//                     result.AddMessages(
//                         EmitFunctionParameters(node, childContext, token)
//                     );

//                     // body
//                     result.AddMessages(
//                         EmitBlockLike(node.Body, childContext, token)
//                     );
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Function has unsupported Name type : '{node.Name.Kind}'", node.Name)
//                     {
//                         Hint = GetHint(node.Name.Origin),
//                         Tags = DiagnosticTags
//                     }
//                     );
//                 }
//             }
//             else
//             {
//                 result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Function must have a name", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                     );
//             }


//             return result;
//         }

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private Result<object> EmitFunctionLikeSignature(FunctionLikeSignature node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // [dho] TODO EmitOrnamentation - 01/03/19
//             // if(node.Override)
//             // {
//             //     context.Emission.Append(node, "@Override ");
//             // }

//             // if(node.Static)
//             // {
//             //     context.Emission.Append(node, "static ");
//             // }

//             var name = node.Name;

//             if(name != null)
//             {

//                 // [dho] TODO reuse function signature emission - 29/09/18

//                 // [dho] TODO modifiers! - 21/09/18
                
//                 if(name.Kind == SemanticKind.Identifier)
//                 {
//                     var type = node.Type;

//                     // return type
//                     if(type != null)
//                     {
//                         result.AddMessages(
//                             EmitNode(type, childContext, token)
//                         );

//                         // [dho] the space after the return type - 21/09/18
//                         context.Emission.Append(node, " ");
//                     }
//                     else
//                     {
//                         result.AddMessages(
//                             new NodeMessage(MessageKind.Error, $"Expected function to have a specified return type", node)
//                             {
//                                 Hint = GetHint(node.Origin),
//                                 Tags = DiagnosticTags
//                             }
//                         );
//                     }

//                     result.AddMessages(
//                         EmitIdentifier(NodeInterrogation.Identifier(node.AST, name), childContext, token)
//                     );

//                     // generics
//                     result.AddMessages(
//                         EmitFunctionTemplate(node, childContext, token)
//                     );

//                     // [dho] Now we can emit the parameter list after the diamond - 29/09/18
//                     result.AddMessages(
//                         EmitFunctionParameters(node, childContext, token)
//                     );
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Function has unsupported Name type : '{node.Name.Kind}'", node.Name)
//                     {
//                         Hint = GetHint(node.Name.Origin),
//                         Tags = DiagnosticTags
//                     }
//                     );
//                 }
//             }
//             else
//             {
//                 result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Function must have a name", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                     );
//             }


//             return result;
//         }

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private Result<object> EmitFunctionName(FunctionLikeDeclaration fn, Context context, CancellationToken token)
//         {
//             var name = fn.Name;

//             if (name.Kind == SemanticKind.Identifier)
//             {
//                 return EmitIdentifier(NodeInterrogation.Identifier(fn.AST, name), context, token);
//             }
//             else
//             {
//                 var result = new Result<object>();

//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Unsupported name type : '{name.Kind}'", name)
//                     {
//                         Hint = GetHint(name.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );

//                 return result;
//             }
//         }

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private Result<object> EmitFunctionTemplate<T>(T fn, Context context, CancellationToken token) where T : NodeWrapper, ITemplated
//         {
//             var result = new Result<object>();

//             var template = fn.Template;

//             if (template.Length > 0)
//             {
//                 context.Emission.Append(fn, "<");

//                 result.AddMessages(
//                     EmitCSV(template, context, token)
//                 );

//                 context.Emission.Append(fn, ">");
//             }

//             return result;
//         }

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private Result<object> EmitFunctionParameters<T>(T fn, Context context, CancellationToken token) where T : NodeWrapper, IParametered
//         {
//             var result = new Result<object>();

//             var parameters = fn.Parameters;

//             context.Emission.Append(fn, "(");

//             if (parameters.Length > 0)
//             {
//                 result.AddMessages(
//                     EmitCSV(parameters, context, token)
//                 );
//             }

//             context.Emission.Append(fn, ")");

//             return result;
//         }

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         protected Result<object> EmitBlockLike(Node body, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             if (body != null)
//             {
//                 if (body.Kind == SemanticKind.Block)
//                 {
//                     result.AddMessages(
//                         EmitBlock((Block)body, context, token)
//                     );
//                 }
//                 else
//                 {
//                     context.Emission.Append(body, "{");
//                     context.Emission.Indent();

//                     foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(body))
//                     {
//                         if(member.Kind == SemanticKind.Nop) continue;

//                         context.Emission.AppendBelow(body, "");

//                         result.AddMessages(
//                             EmitNode(member, context, token)
//                         );

//                         if(RequiresSemicolonSentinel(member))
//                         {
//                             context.Emission.Append(member, ";");
//                         }
//                     }

//                     context.Emission.Outdent();
//                     context.Emission.AppendBelow(body, "}");
//                 }
//             }
//             else
//             {
//                 context.Emission.Append(context.Parent, "{}");
//             }

//             return result;
//         }

//         #endregion

//         protected bool RequiresSemicolonSentinel(Node node)
//         {
//             switch(node.Kind)
//             {
//                 case SemanticKind.IfDirective:
//                 case SemanticKind.MatchJunction:
//                 case SemanticKind.PredicateJunction:
//                 case SemanticKind.ObjectTypeDeclaration:
//                 case SemanticKind.MethodDeclaration:
//                 case SemanticKind.FunctionDeclaration:
//                 case SemanticKind.NamespaceDeclaration:
//                 case SemanticKind.ErrorTrapJunction:
//                 case SemanticKind.DoOrDieErrorTrap:
//                 case SemanticKind.DoOrRecoverErrorTrap:
//                 case SemanticKind.ForKeysLoop:
//                 case SemanticKind.ForMembersLoop:
//                 case SemanticKind.ForPredicateLoop:
//                 case SemanticKind.WhilePredicateLoop:
//                 case SemanticKind.DoWhilePredicateLoop:
//                     return false;

//                 default:
//                     return true;
//             }
//         }

//         private Result<object> EmitPostUnaryArithmetic(UnaryArithmetic node, string operatorToken, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Operand, childContext, token)
//             );

//             context.Emission.Append(node, operatorToken);

//             return result;
//         }

//         private Result<object> EmitPreUnaryArithmetic(UnaryArithmetic node, string operatorToken, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             context.Emission.Append(node, operatorToken);

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             result.AddMessages(
//                 EmitNode(node.Operand, childContext, token)
//             );

//             return result;
//         }
//     }
// }