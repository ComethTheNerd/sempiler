using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sempiler.Diagnostics;
using Sempiler.AST;
using Sempiler.AST.Diagnostics;

namespace Sempiler.Emission
{
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

    public class SwiftEmitter : BaseEmitter
    {
        public SwiftEmitter() : base(new string[]{ "swift", PhaseKind.Emission.ToString("g").ToLower() })
        {
        }

        const MetaFlag AccessorDeclarationFlags = TypeDeclarationMemberFlags;// | /* MetaFlag.Generator | MetaFlag.Asynchronous |*/ MetaFlag.Optional;

        public override Result<object> EmitAccessorDeclaration(AccessorDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitFlagsForTypeDeclarationMember(node, metaFlags, context, token));

            context.Emission.Append(node, "get ");

            if (node.Name != null)
            {
                result.AddMessages(
                    EmitFunctionName(node, childContext, token)
                );
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, "Function must have a name", node)
                        {
                            Hint = GetHint(node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
            }

            // if((metaFlags & MetaFlag.Optional) == MetaFlag.Optional)
            // {
            //     context.Emission.Append(node, "?");
            // }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~AccessorDeclarationFlags, context, token)
            );

            if(node.Template.Length > 0)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node, "accessor templates"));
            }

            if(node.Parameters.Length > 0)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node, "accessor parameters"));
            }

            if(node.Type != null)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node.Type, "accessor return types"));
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitAccessorSignature(AccessorSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
 
            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            context.Emission.Append(node, "get ");

            if (node.Name != null)
            {
                result.AddMessages(
                    EmitFunctionName(node, childContext, token)
                );
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, "Function must have a name", node)
                        {
                            Hint = GetHint(node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
            }

            if(node.Template.Length > 0)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node, "accessor signature templates"));
            }

            if(node.Parameters.Length > 0)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node, "accessor signature parameters"));
            }

            if(node.Type != null)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node.Type, "accessor signature return types"));
            }

            return result;
        }

        public override Result<object> EmitAddition(Addition node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "+", context, token);
        }

        public override Result<object> EmitAdditionAssignment(AdditionAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "+=", context, token);
        }

        public override Result<object> EmitAddressOf(AddressOf node, Context context, CancellationToken token)
        {
            return base.EmitAddressOf(node, context, token);
        }

        public override Result<object> EmitAnnotation(Annotation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "@");

            result.AddMessages(
                EmitNode(node.Expression, context, token)
            );

            return result;
        }

        public override Result<object> EmitArithmeticNegation(ArithmeticNegation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "-");

            result.AddMessages(
                EmitNode(node.Operand, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitArrayConstruction(ArrayConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "[");

            result.AddMessages(
                EmitCSV(node.Members, childContext, token)
            );  

            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitArrayTypeReference(ArrayTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "[");

            result.AddMessages(
                EmitNode(node.Type, childContext, token)
            );
            
            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitAssignment(Assignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "=", context, token);
        }

        public override Result<object> EmitAssociation(Association node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "(");

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitBitwiseAnd(BitwiseAnd node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "&", context, token);
        }

        public override Result<object> EmitBitwiseAndAssignment(BitwiseAndAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "&=", context, token);
        }

        public override Result<object> EmitBitwiseExclusiveOr(BitwiseExclusiveOr node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "^", context, token);
        }

        public override Result<object> EmitBitwiseExclusiveOrAssignment(BitwiseExclusiveOrAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "^=", context, token);
        }

        public override Result<object> EmitBitwiseLeftShift(BitwiseLeftShift node, Context context, CancellationToken token)
        {
            return EmitBitwiseShift(node, "<<", context, token);
        }

        public override Result<object> EmitBitwiseLeftShiftAssignment(BitwiseLeftShiftAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "<<=", context, token);
        }

        public override Result<object> EmitBitwiseNegation(BitwiseNegation node, Context context, CancellationToken token)
        {
            return base.EmitBitwiseNegation(node, context, token);
        }

        public override Result<object> EmitBitwiseOr(BitwiseOr node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "|", context, token);
        }

        public override Result<object> EmitBitwiseOrAssignment(BitwiseOrAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "|=", context, token);
        }

        public override Result<object> EmitBitwiseRightShift(BitwiseRightShift node, Context context, CancellationToken token)
        {
            return EmitBitwiseShift(node, ">>", context, token);
        }

        public override Result<object> EmitBitwiseRightShiftAssignment(BitwiseRightShiftAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, ">>=", context, token);
        }

        public override Result<object> EmitBitwiseUnsignedRightShift(BitwiseUnsignedRightShift node, Context context, CancellationToken token)
        {
            return base.EmitBitwiseUnsignedRightShift(node, context, token);
        }

        public override Result<object> EmitBitwiseUnsignedRightShiftAssignment(BitwiseUnsignedRightShiftAssignment node, Context context, CancellationToken token)
        {
            return base.EmitBitwiseUnsignedRightShiftAssignment(node, context, token);
        }

        public override Result<object> EmitBlock(Block node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "{");

            context.Emission.Indent();

            foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Content))
            {
                context.Emission.AppendBelow(node, "");

                result.AddMessages(
                    EmitNode(member, context, token)
                );
            }

            context.Emission.Outdent();
        
            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitBooleanConstant(BooleanConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, node.Value ? "true" : "false");

            return result;
        }

        public override Result<object> EmitBreakpoint(Breakpoint node, Context context, CancellationToken token)
        {
            return base.EmitBreakpoint(node, context, token);
        }

        public override Result<object> EmitClauseBreak(ClauseBreak node, Context context, CancellationToken token)
        {
            return base.EmitClauseBreak(node, context, token);
        }

        public override Result<object> EmitCodeConstant(CodeConstant node, Context context, CancellationToken token)
        {
            return base.EmitCodeConstant(node, context, token);
        }

        public override Result<object> EmitCollectionDestructuring(CollectionDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitCompilerHint(CompilerHint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        // [dho] TODO move to a helper for other emitters that will spit out 1 file per component, eg. Java etc - 21/09/18 (ported 24/04/19)
        // [dho] TODO CLEANUP this is duplicated in CSEmitter (apart from FileEmission extension) - 21/09/18 (ported 24/04/19)
        public override Result<object> EmitComponent(Component node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var location = node.Name;//sourceWithLocation.Location;

            var relParentDirPath = node.Name.Replace(context.Session.BaseDirectory.ToPathString(), "");

            // [dho] our emissions will be stored in a file with the same relative path and name
            // as the original source for this component, eg. hello/World.ts => hello/World.java - 26/04/19
            var file = new FileEmission(
                FileSystem.ParseFileLocation($"{relParentDirPath}.swift")
            );

            if (context.OutFileCollection.Contains(file.Destination))
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Could not write emission in artifact at '{file.Destination.ToPathString()}' because location already exists", node)
                    {
                        Hint = GetHint(node.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            var childContext = ContextHelpers.Clone(context);
            childContext.Component = node;
            // childContext.Parent = node;
            childContext.Emission = file; // [dho] this is where children of this component should write to - 29/08/18

            foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(node.AST, node))
            {
                result.AddMessages(
                    EmitNode(child, childContext, token)
                );

                // if(RequiresSemicolonSentinel(child))
                // {
                //     childContext.Emission.Append(child, ";");
                // }

                childContext.Emission.AppendBelow(node, "");

                if (token.IsCancellationRequested)
                {
                    break;
                }
            }

            if (!Diagnostics.DiagnosticsHelpers.HasErrors(result))
            {
                context.OutFileCollection[file.Destination] = file;
            }

            return result;
        }

        public override Result<object> EmitConcatenation(Concatenation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var operands = node.Operands;

            result.AddMessages(EmitDelimited(operands, "+", childContext, token));

            return result;
        }

        public override Result<object> EmitConcatenationAssignment(ConcatenationAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "+=", context, token);
        }

        public override Result<object> EmitConditionalTypeReference(ConditionalTypeReference node, Context context, CancellationToken token)
        {
            return base.EmitConditionalTypeReference(node, context, token);
        }

        public override Result<object> EmitLiteralTypeReference(LiteralTypeReference node, Context context, CancellationToken token)
        {
            return base.EmitLiteralTypeReference(node, context, token);
        }

        public override Result<object> EmitConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // [dho] TODO modifiers! - 20/09/18
            // [dho] TODO super call - 29/09/18

            context.Emission.AppendBelow(node, $"init");

            var name = node.Name;
            var type = node.Type;
            var body = node.Body;

            if(name != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(name, "constructor names")
                );
            }

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(type != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(type, "constructor return types")
                );
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitConstructorSignature(ConstructorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitConstructorTypeReference(ConstructorTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        const MetaFlag DataValueDeclarationScopeFlags = MetaFlag.BlockScope | MetaFlag.FunctionScope | MetaFlag.Static;

        public override Result<object> EmitDataValueDeclaration(DataValueDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            var metaFlags = MetaHelpers.ReduceFlags(node);

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));


            if((metaFlags & MetaFlag.Static) == MetaFlag.Static)
            {
                context.Emission.Append(node, "static ");
            }

            // [dho] just putting this here for now.. in a `for` loop we should omit the `let` or `var` - 04/08/19
            if(ASTHelpers.GetPosition(node.AST, node.ID).Role != SemanticRole.Handle)
            {
                if((metaFlags & MetaFlag.Constant) == MetaFlag.Constant)
                {
                    context.Emission.Append(node, "let ");
                }
                else // if((metaFlags & MetaFlag.FunctionScope) == MetaFlag.FunctionScope)
                {
                    context.Emission.Append(node, "var ");
                }
            }


            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~(MetaFlag.Constant | DataValueDeclarationScopeFlags), context, token)
            );
            
            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            if (node.Type != null)
            {
                context.Emission.Append(node, " : ");

                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );
            }

            if (node.Initializer != null)
            {
                context.Emission.Append(node, " = ");

                result.AddMessages(
                    EmitNode(node.Initializer, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitDefaultExportReference(DefaultExportReference node, Context context, CancellationToken token)
        {
            return base.EmitDefaultExportReference(node, context, token);
        }

        public override Result<object> EmitDestruction(Destruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDestructorDeclaration(DestructorDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, $"deinit");

            var name = node.Name;
            var template = node.Template;
            var parameters = node.Parameters;
            var type = node.Type;

            if(name != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(name, "destructor names")
                );
            }

            if(parameters.Length > 0)
            {
                var parameterMarker = parameters[0];

                result.AddMessages(
                    CreateUnsupportedFeatureResult(parameterMarker, "destructor parameters")
                );
            }

            if(template.Length > 0)
            {
                var templateMarker = template[0];
                result.AddMessages(
                    CreateUnsupportedFeatureResult(templateMarker, "destructor templates")
                );
            }

            if(type != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(type, "destructor return types")
                );
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitDestructorSignature(DestructorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDestructuredMember(DestructuredMember node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDictionaryConstruction(DictionaryConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var entryContext = ContextHelpers.Clone(context);
            // entry// // Context.Parent = node;

            context.Emission.Append(node, "[");

            context.Emission.Indent();
            {
                foreach(var (entry, hasNext) in ASTNodeHelpers.IterateMembers(node.Members))
                {
                    result.AddMessages(
                        EmitNode(entry, entryContext, token)
                    );

                    if(hasNext)
                    {
                        entryContext.Emission.Append(node, ",");
                    }

                    entryContext.Emission.AppendBelow(node, "");

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            context.Emission.Outdent();

            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitDictionaryTypeReference(DictionaryTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
   
            context.Emission.Append(node, "[");

            result.AddMessages(EmitNode(node.KeyType, childContext, token));

            context.Emission.Append(node, ":");

            result.AddMessages(EmitNode(node.StoredType, childContext, token));

            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitDirective(Directive node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDivision(Division node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "/", context, token);
        }

        public override Result<object> EmitDivisionAssignment(DivisionAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "/=", context, token);
        }

        public override Result<object> EmitDoOrDieErrorTrap(DoOrDieErrorTrap node, Context context, CancellationToken token)
        {
            return base.EmitDoOrDieErrorTrap(node, context, token);
        }

        public override Result<object> EmitDoOrRecoverErrorTrap(DoOrRecoverErrorTrap node, Context context, CancellationToken token)
        {
            return base.EmitDoOrRecoverErrorTrap(node, context, token);
        }

        public override Result<object> EmitDoWhilePredicateLoop(DoWhilePredicateLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.AppendBelow(node, "repeat ");

            context.Emission.AppendBelow(node, "{");

            context.Emission.Indent();

            result.AddMessages(EmitBlockLike(node.Body, node.Node, childContext, token));

            context.Emission.Outdent();

            context.Emission.AppendBelow(node, "}");

            context.Emission.AppendBelow(node, "while ");

            result.AddMessages(EmitNode(node.Condition, childContext, token));

            return result;
        }

        public override Result<object> EmitDomain(Domain node, Context context, CancellationToken token)
        {
            return base.EmitDomain(node, context, token);
        }

        public override Result<object> EmitDynamicTypeConstruction(DynamicTypeConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, "[");

            context.Emission.Indent();
            {
                foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Members))
                {
                    if(member.Kind == SemanticKind.FieldDeclaration)
                    {
                        var fieldDecl = ASTNodeFactory.FieldDeclaration(node.AST, member);
                        var fieldName = fieldDecl.Name;

                        if(fieldName?.Kind == SemanticKind.Identifier)
                        {
                            context.Emission.AppendBelow(member, "\"");

                            result.AddMessages(
                                EmitNode(fieldName, childContext, token)
                            );

                            context.Emission.Append(member, "\" : ");

                            result.AddMessages(
                                EmitNode(fieldDecl.Initializer, childContext, token)
                            );

                            context.Emission.Append(member, ",");
                            
                            continue;
                        }
                    }

                    result.AddMessages(
                        CreateUnsupportedFeatureResult(member, $"Dynamic type construction members of type '{member.Kind}'")
                    );
                }

            }
            context.Emission.Outdent();

            context.Emission.AppendBelow(node, "]");

            return result;
        }

        public override Result<object> EmitDynamicTypeReference(DynamicTypeReference node, Context context, CancellationToken token)
        {
            return base.EmitDynamicTypeReference(node, context, token);
        }

        public override Result<object> EmitEntityDestructuring(EntityDestructuring node, Context context, CancellationToken token)
        {
            return base.EmitEntityDestructuring(node, context, token);
        }

        public override Result<object> EmitEnumerationMemberDeclaration(EnumerationMemberDeclaration node, Context context, CancellationToken token)
        {
            // [dho] cases for enums might not always have just an identifer
            // as the name:
            //
            // `case upc(Int, Int, Int, Int)`
            //
            // - 27/10/18

            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "case ");
            
            var name = node.Name;
            var initializer = node.Initializer;

            result.AddMessages(
                EmitNode(name, childContext, token)
            );

            if(initializer != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Initializer, "enum member initializers")
                );
            }

            return result;
        }

        public override Result<object> EmitEnumerationTypeDeclaration(EnumerationTypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "enum ");

            var name = node.Name;

            if (name != null)
            {

                if (name.Kind == SemanticKind.Identifier)
                {
                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(context.AST, (DataNode<string>)name), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Enumeration type declaration has unsupported name type : '{name.Kind}'", name)
                        {
                            Hint = GetHint(name.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            // generics
            result.AddMessages(
                EmitTypeDeclarationTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitTypeDeclarationHeritage(node, context, token)
            );

            context.Emission.Append(node, "{");

            result.AddMessages(
                EmitTypeDeclarationMembers(node, childContext, token)
            );

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitErrorFinallyClause(ErrorFinallyClause node, Context context, CancellationToken token)
        {
            return base.EmitErrorFinallyClause(node, context, token);
        }

        public override Result<object> EmitErrorHandlerClause(ErrorHandlerClause node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "catch ");

            var expression = node.Expression;
            var body = node.Body;

            if(expression.Length == 1)
            {

                result.AddMessages(
                    EmitNode(expression[0], childContext, token)
                );
            }
            else if(expression.Length > 1)
            {
                result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected at most one expression but found {expression.Length}", node)
                {
                    Hint = GetHint(node.Origin),
                    Tags = DiagnosticTags
                });
            }
            

            result.AddMessages(
                EmitBlockLike(body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitErrorTrapJunction(ErrorTrapJunction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "do ");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );
            
            foreach(var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Clauses))
            {
                result.AddMessages(EmitNode(member, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitEvalToVoid(EvalToVoid node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitExponentiation(Exponentiation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, "pow(");

            result.AddMessages(EmitBlockLike(node.Base, node.Node, childContext, token));

            context.Emission.Append(node, ", ");

            result.AddMessages(EmitNode(node.Exponent, childContext, token));

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitExponentiationAssignment(ExponentiationAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitExportDeclaration(ExportDeclaration node, Context context, CancellationToken token)
        {
            // [dho] use the SwiftSymbolVisibilityTransformer that changes the visibility flags of
            // symbols (eg. declarations) to obviate need for an explicit export declarations - 28/06/19
            return CreateUnsupportedFeatureResult(node);
        }


        const MetaFlag FieldDeclarationFlags = TypeDeclarationMemberFlags | MetaFlag.Optional;
        public override Result<object> EmitFieldDeclaration(FieldDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            result.AddMessages(
                EmitAnnotationsAndModifiers(node, childContext, token)
            );

            result.AddMessages(EmitFlagsForTypeDeclarationMember(node, metaFlags, context, token));

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~FieldDeclarationFlags, context, token)
            );

            context.Emission.Append(node, "var ");

            var name = node.Name;
            var type = node.Type;
            var initializer = node.Initializer;

            result.AddMessages(
                EmitNode(name, childContext, token)
            );

            if (type != null)
            {
                context.Emission.Append(type, ": ");

                result.AddMessages(
                    EmitNode(type, childContext, token)
                );

                // [dho] TODO EmitOrnamentation - 01/03/19
                // switch(node.Nullability)
                // {
                //     case Nullability.Safe:
                //         context.Emission.Append(node, "?");
                //     break;

                //     case Nullability.Forced:
                //         context.Emission.Append(node, "!");
                //     break;
                // }
            }

            if((metaFlags & MetaFlag.Optional) == MetaFlag.Optional)
            {
                context.Emission.Append(node, "?");
            }

            if (initializer != null)
            {
                if(initializer.Kind == SemanticKind.LambdaDeclaration)
                {
                    int i = 0;
                }
                else
                {
                    context.Emission.Append(initializer, " = ");
                }

                result.AddMessages(
                    EmitNode(initializer, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitFieldSignature(FieldSignature node, Context context, CancellationToken token)
        {
            return base.EmitFieldSignature(node, context, token);
        }

        public override Result<object> EmitForKeysLoop(ForKeysLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "for ");

            result.AddMessages(
                EmitNode(node.Handle, childContext, token)
            );

            context.Emission.Append(node, " in ");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitForMembersLoop(ForMembersLoop node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitForPredicateLoop(ForPredicateLoop node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitForcedCast(ForcedCast node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, " as! ");
            
            result.AddMessages(
                EmitNode(node.TargetType, childContext, token)
            );

            return result;
        }


        const MetaFlag FunctionDeclarationFlags = MetaFlag.FileVisibility;

        public override Result<object> EmitFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            var metaFlags = MetaHelpers.ReduceFlags(node);

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            context.Emission.Append(node, "func ");

            if (node.Name != null)
            {
                result.AddMessages(
                    EmitFunctionName(node, childContext, token)
                );
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~FunctionDeclarationFlags, context, token)
            );

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionType(node.Type, childContext, token)
            );

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitFunctionTermination(FunctionTermination node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var value = node.Value;

            if (value!= null)
            {
                context.Emission.Append(node, "return ");

                var childContext = ContextHelpers.Clone(context);
                // // childContext.Parent = node;

                result.AddMessages(
                    EmitNode(value, childContext, token)
                );
            }
            else
            {
                context.Emission.Append(node, "return");
            }

            return result;
        }

        public override Result<object> EmitFunctionTypeReference(FunctionTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            // [dho] TODO NOTE wrapping in parens to fix bug for optional parameters,
            // ie. instead of `(x, y) -> Void?` it is now `((x,y) -> Void)?` - 08/09/19
            context.Emission.Append(node, "(");

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionType(node.Type, childContext, token)
            );

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitGeneratorSuspension(GeneratorSuspension node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitGlobalDeclaration(GlobalDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitIdentity(Identity node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);

            context.Emission.AppendBelow(node, "+");

            return EmitNode(node.Operand, childContext, token);
        }

        public override Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // [dho] TODO EmitOrnamentation - 01/03/19
            // if(!node.Required)
            // {
            //     result.AddMessages(
            //         CreateUnsupportedFeatureResult(node)
            //     );
            // }

            var clauses = node.Clauses;
            var specifier = node.Specifier;

            if(clauses.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(clauses, "import declaration clause")
                );
            }
            
            context.Emission.Append(node, "import ");

            result.AddMessages(
                EmitNode(specifier, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitIncidentContextReference(IncidentContextReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "self");

            return result;
        }

        public override Result<object> EmitIncidentTypeConstraint(IncidentTypeConstraint node, Context context, CancellationToken token)
        {
            // var childContext = ContextHelpers.Clone(context);

            // context.Emission.Append(node, ": ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitIndexTypeQuery(IndexTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitInferredTypeQuery(InferredTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitIndexedAccess(IndexedAccess node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            result.AddMessages(
                EmitNode(node.Incident, childContext, token)
            );

            context.Emission.Append(node, "[");

            result.AddMessages(
                EmitNode(node.Member, childContext, token)
            );

            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitIndexerSignature(IndexerSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitInterfaceDeclaration(InterfaceDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, childContext, token));

            result.AddMessages(EmitTypeDeclarationVisibilityFlags(node, metaFlags, context, token));
                
            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~TypeDeclarationVisibilityFlags, context, token)
            );

            var name = node.Name;
            var template = node.Template;
            // var members = node.Members;

            if(name != null)
            {
                if(name.Kind == SemanticKind.Identifier)
                {
                    context.Emission.Append(node, "protocol ");
                    
                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(context.AST, (DataNode<string>)name), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Interface type declaration has unsupported name type : '{name.Kind}'", name)
                        {
                            Hint = GetHint(name.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node, "anonymous interface declarations")
                );
            }

            // generics
            if(template.Length > 0)
            {
                result.AddMessages(
                    EmitTypeDeclarationTemplate(node, childContext, token)
                );
            }

            result.AddMessages(
                EmitTypeDeclarationHeritage(node, context, token)
            );

            context.Emission.Append(node, "{");

            result.AddMessages(
                EmitTypeDeclarationMembers(node, childContext, token)
            );

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitInterimSuspension(InterimSuspension node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitInterpolatedString(InterpolatedString node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var parserName = node.ParserName;
            var members = node.Members;

            if(parserName != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(parserName, "interpolated string named parsers")
                );
            }   

            if(ASTNodeHelpers.IsMultilineString(node))
            {
                context.Emission.Append(node, "\"\"\"");

                result.AddMessages(
                    EmitInterpolatedStringMembers(members, childContext, token)
                );

                context.Emission.Append(node, "\"\"\"");
            }
            else
            {
                context.Emission.Append(node, "\"");

                result.AddMessages(
                    EmitInterpolatedStringMembers(members, childContext, token)
                );

                context.Emission.Append(node, "\"");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitInterpolatedStringMembers(Node[] members, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            foreach(var (member, hasNext) in ASTNodeHelpers.IterateMembers(members))
            {
                if(member.Kind == SemanticKind.InterpolatedStringConstant)
                {
                    result.AddMessages(
                        EmitInterpolatedStringConstant(ASTNodeFactory.InterpolatedStringConstant(context.AST, (DataNode<string>)member), context, token)
                    );
                }
                else
                {
                    context.Emission.Append(member, "\\(");

                    result.AddMessages(
                        EmitNode(member, context, token)
                    );

                    context.Emission.Append(member, ")");
                }
            }

            return result;
        }

        public override Result<object> EmitInterpolatedStringConstant(InterpolatedStringConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            // [dho] NOTE no delimiters because the interpolated string that this node sits inside
            // will have the delimiters on it - 28/10/18
            context.Emission.Append(node, node.Value);

            return result;
        }

        public override Result<object> EmitIntersectionTypeReference(IntersectionTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitIntrinsicTypeReference(IntrinsicTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            switch(node.Role)
            {
                // case TypeRole.Any:
                //     context.Emission.Append(node, "any");
                // break;
                    
                case TypeRole.Boolean:
                    context.Emission.Append(node, "Bool");
                break;

                case TypeRole.Double64:
                    context.Emission.Append(node, "Double");
                break;

                case TypeRole.Float32:    
                    context.Emission.Append(node, "Float");
                break;

                case TypeRole.Integer32:
                    context.Emission.Append(node, "Int");
                break;

                case TypeRole.Never:
                    context.Emission.Append(node, "Never");
                break;

                case TypeRole.String:
                    context.Emission.Append(node, "String");
                break;

                case TypeRole.Void:
                    context.Emission.Append(node, "Void");
                break;

                default:
                    result.AddMessages(
                        CreateUnsupportedFeatureResult(node, $"Intrisinsic {node.Role.ToString()} types")
                    );
                break;
            }

            return result;
        }

        public override Result<object> EmitInvocation(Invocation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var subject = node.Subject;
            var template = node.Template;

            result.AddMessages(
                EmitNode(subject, childContext, token)
            );
   
            if(template.Length > 0)
            {
                context.Emission.Append(node, "<");

                result.AddMessages(
                    EmitCSV(template, context, token)
                );

                context.Emission.Append(node, ">");
            }

            result.AddMessages(EmitInvocationArguments(node, childContext, token));

            return result;
        }

        public override Result<object> EmitInvocationArgument(InvocationArgument node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var label = node.Label;
            var value = node.Value;

            if(label != null)
            {
                result.AddMessages(
                    EmitNode(label, childContext, token)
                );

                context.Emission.Append(node, ": ");
            }

            result.AddMessages(
                EmitNode(value, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitJumpToNextIteration(JumpToNextIteration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, "continue");
            
            if(node.Label != null)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node.Label, "continue labels"));
            }

            return result;
        }

        public override Result<object> EmitKeyValuePair(KeyValuePair node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLabel(Label node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLambdaDeclaration(LambdaDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var name = node.Name;
            var template = node.Template;
            var parameters = node.Parameters;
            var type = node.Type;
            var body = node.Body;

            if(parameters.Length > 0 || type != null)
            {
                // [dho] TODO modifiers! - 20/09/18
                context.Emission.Append(node, "{");

                if (name != null)
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Lambda must be anonymous", name)
                        {
                            Hint = GetHint(name.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }



                if (template.Length > 0)
                {
                    var templateMarker = template[0];

                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Lambda must not be templated", templateMarker)
                        {
                            Hint = GetHint(templateMarker.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }

                {
                    if(parameters.Length == 1)
                    {
                        var paramListParens = false;
                        var parameter = parameters[0];
                        if(parameter.Kind == SemanticKind.ParameterDeclaration)
                        {
                            var decl = ASTNodeFactory.ParameterDeclaration(context.AST, parameter);
                        
                            paramListParens = decl.Type != null || decl.Default != null;
                        }

                        result.AddMessages(
                            paramListParens ? 
                                EmitFunctionParameters(node, childContext, token) : 
                                EmitNode(parameter, childContext, token)
                        );
                    }
                    else
                    {
                        result.AddMessages(EmitFunctionParameters(node, childContext, token));
                    }
                }

        

                result.AddMessages(
                    EmitFunctionType(type, childContext, token)
                );

                context.Emission.Append(node, " in ");

                context.Emission.Indent();
                {
                    context.Emission.AppendBelow(node, "");

                    if(body?.Kind == SemanticKind.Block)
                    {
                        var content = ASTNodeFactory.Block(context.AST, body).Content;

                        foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(content))
                        {
                            context.Emission.AppendBelow(node, "");

                            result.AddMessages(
                                EmitNode(member, context, token)
                            );
                        }
                    }
                    else
                    {
                        result.AddMessages(
                            EmitNode(body, context, token)
                        );
                    }
                    
                    context.Emission.Outdent();
                }

                context.Emission.AppendBelow(node, "}");
            }
            // else if (parameters.Length == 1)
            // {
            //     // [dho] TODO modifiers! - 20/09/18

            //     if (name != null)
            //     {
            //         result.AddMessages(
            //             new NodeMessage(MessageKind.Error, $"Lambda must be anonymous", name)
            //             {
            //                 Hint = GetHint(name.Origin),
            //                 Tags = DiagnosticTags
            //             }
            //         );
            //     }



            //     if (template.Length > 0)
            //     {
            //         var templateMarker = template[0];

            //         result.AddMessages(
            //             new NodeMessage(MessageKind.Error, $"Lambda must not be templated", templateMarker)
            //             {
            //                 Hint = GetHint(templateMarker.Origin),
            //                 Tags = DiagnosticTags
            //             }
            //         );
            //     }

            //     {
            //         var paramListParens = false;
            //         var parameter = parameters[0];
            //         if(parameter.Kind == SemanticKind.ParameterDeclaration)
            //         {
            //             var decl = ASTNodeFactory.ParameterDeclaration(context.AST, parameter);
                    
            //             paramListParens = decl.Type != null || decl.Default != null;
            //         }
            
            //         result.AddMessages(
            //             paramListParens ? 
            //                 EmitFunctionParameters(node, childContext, token) : 
            //                 EmitNode(parameter, childContext, token)
            //         );
            //     }

            //     result.AddMessages(
            //         EmitFunctionType(node.Type, childContext, token)
            //     );

            //     context.Emission.Append(node, " in ");

            //     result.AddMessages(
            //         EmitBlockLike(node.Body, node.Node, childContext, token)
            //     );
            // }
            else
            {
                result.AddMessages(
                    EmitBlockLike(node.Body, node.Node, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitLogicalAnd(LogicalAnd node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "&&", context, token);
        }

        public override Result<object> EmitLogicalNegation(LogicalNegation node, Context context, CancellationToken token)
        {
            return EmitPrefixUnaryLike(node, "!", context, token);
        }

        public override Result<object> EmitLogicalOr(LogicalOr node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "||", context, token);
        }

        public override Result<object> EmitLoopBreak(LoopBreak node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, "break");
            
            if(node.Expression != null)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node.Expression, "break expressions"));
            }

            return result;
        }

        public override Result<object> EmitLooseEquivalent(LooseEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLooseNonEquivalent(LooseNonEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLowerBoundedTypeConstraint(LowerBoundedTypeConstraint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMappedDestructuring(MappedDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMappedTypeReference(MappedTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMatchClause(MatchClause node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var expression = node.Expression;

            if(expression.Length == 1)
            {
                context.Emission.Append(node, "case ");

                result.AddMessages(
                    EmitNode(expression[0], context, token)
                );

                context.Emission.Append(node, " : ");
            }
            else if(expression.Length == 0)
            {
                context.Emission.Append(node, "default:");
            }
            else
            {
                result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected single expression but found {expression.Length}", node)
                {
                    Hint = GetHint(node.Origin),
                    Tags = DiagnosticTags
                });
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitMatchJunction(MatchJunction node, Context context, CancellationToken token)
        {
            /*
                switch some value to consider {
                    case value 1:
                        respond to value 1
                    case value 2,
                        value 3:
                        respond to value 2 or 3
                    default:
                        otherwise, do something else
                }

             */

            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "switch ");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            result.AddMessages(
                EmitBlockLike(node.Clauses, node.Node, childContext, token)
            );
            
            return result;
        }

        public override Result<object> EmitMaybeNull(MaybeNull node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, "?");

            return result;
        }

        public override Result<object> EmitMemberNameReflection(MemberNameReflection node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMemberTypeConstraint(MemberTypeConstraint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMembershipTest(MembershipTest node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMeta(Meta node, Context context, CancellationToken token)
        {
            return base.EmitMeta(node, context, token);
        }

        public override Result<object> EmitMetaProperty(MetaProperty node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        const MetaFlag MethodDeclarationFlags = TypeDeclarationMemberFlags | MetaFlag.Mutation;

        public override Result<object> EmitMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            var metaFlags = MetaHelpers.ReduceFlags(node);

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitFlagsForTypeDeclarationMember(node, metaFlags, context, token));


            if((metaFlags & MetaFlag.Mutation) == MetaFlag.Mutation)
            {
                context.Emission.Append(node, "mutating ");
            }

            context.Emission.Append(node, "func ");
 
            if (node.Name != null)
            {
                result.AddMessages(
                    EmitFunctionName(node, childContext, token)
                );
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, "Method must have a name", node)
                        {
                            Hint = GetHint(node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
            }

            if((metaFlags & MetaFlag.Optional) == MetaFlag.Optional)
            {
                context.Emission.Append(node, "?");
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~MethodDeclarationFlags, context, token)
            );

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionType(node.Type, childContext, token)
            );

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitMethodSignature(MethodSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
 
            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            if (node.Name != null)
            {
                result.AddMessages(
                    EmitFunctionName(node, childContext, token)
                );
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, "Method must have a name", node)
                        {
                            Hint = GetHint(node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
            }

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionType(node.Type, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitModifier(Modifier node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, node.Lexeme);

            return result;
        }

        public override Result<object> EmitMultiplication(Multiplication node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "*", context, token);
        }

        public override Result<object> EmitMultiplicationAssignment(MultiplicationAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "*=", context, token);
        }

        const MetaFlag MutatorDeclarationFlags = AccessorDeclarationFlags;

        public override Result<object> EmitMutatorDeclaration(MutatorDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitFlagsForTypeDeclarationMember(node, metaFlags, context, token));

            context.Emission.Append(node, "set ");

            if (node.Name != null)
            {
                result.AddMessages(
                    EmitFunctionName(node, childContext, token)
                );
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, "Function must have a name", node)
                        {
                            Hint = GetHint(node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
            }

            // if((metaFlags & MetaFlag.Optional) == MetaFlag.Optional)
            // {
            //     context.Emission.Append(node, "?");
            // }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~MutatorDeclarationFlags, context, token)
            );

            if(node.Template.Length > 0)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node, "mutator templates"));
            }

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(node.Type != null)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node.Type, "mutator return types"));
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitMutatorSignature(MutatorSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            context.Emission.Append(node, "set ");

            if (node.Name != null)
            {
                result.AddMessages(
                    EmitFunctionName(node, childContext, token)
                );
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, "Function must have a name", node)
                        {
                            Hint = GetHint(node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
            }

            if(node.Template.Length > 0)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node, "mutator signature templates"));
            }

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(node.Type != null)
            {
                result.AddMessages(CreateUnsupportedFeatureResult(node.Type, "mutator signature return types"));
            }

            return result;
        }

        public override Result<object> EmitMutex(AST.Mutex node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNamedTypeConstruction(NamedTypeConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
  
            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            var template = node.Template;

            if(template.Length > 0)
            {
                context.Emission.Append(node, "<");

                result.AddMessages(
                    EmitCSV(template, context, token)
                );

                context.Emission.Append(node, ">");
            }

            result.AddMessages(EmitInvocationArguments(node, childContext, token));

            return result;
        }

        public override Result<object> EmitNamedTypeQuery(NamedTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNamedTypeReference(NamedTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitAnnotationsAndModifiers(node, childContext, token)
            );
            
            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            var template = node.Template;

            if (template.Length > 0)
            {
                context.Emission.Append(node, "<");

                result.AddMessages(
                    EmitCSV(template, childContext, token)
                );

                context.Emission.Append(node, ">");
            }

            return result;
        }

        public override Result<object> EmitNamespaceDeclaration(NamespaceDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNamespaceReference(NamespaceReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNonMembershipTest(NonMembershipTest node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNop(Nop node, Context context, CancellationToken token)
        {
            return new Result<object>();
        }

        public override Result<object> EmitNotNull(NotNull node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, "!");

            return result;
        }

        public override Result<object> EmitNotNumber(NotNumber node, Context context, CancellationToken token)
        {
            return base.EmitNotNumber(node, context, token);
        }

        public override Result<object> EmitNull(Null node, Context context, CancellationToken token)
        {
            context.Emission.Append(node, "nil");
            
            return new Result<object>();
        }

        public override Result<object> EmitNullCoalescence(NullCoalescence node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
 
            result.AddMessages(EmitDelimited(node.Operands, " ?? ", childContext, token));

            return result;
        }

        public override Result<object> EmitNumericConstant(NumericConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, node.Value.ToString());

            return result;
        }

        // [dho] TODO CLEANUP straight up copypasta from CSEmitter - 22/09/18

        const MetaFlag ObjectTypeDeclarationVisibilityFlags = TypeDeclarationVisibilityFlags | MetaFlag.ValueType;

        public override Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            var metaFlags = MetaHelpers.ReduceFlags(node);

            var isStruct = (metaFlags & MetaFlag.ValueType) > 0;

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));
            
            result.AddMessages(EmitTypeDeclarationVisibilityFlags(node, metaFlags, context, token));
            
            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~ObjectTypeDeclarationVisibilityFlags, context, token)
            );

            context.Emission.Append(node, $"{(isStruct ? "struct" : "class")} ");

            if (node.Name != null)
            {
                var name = node.Name;

                if (name.Kind == SemanticKind.Identifier)
                {
                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(childContext.AST, (DataNode<string>)name), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Reference type declaration has unsupported name type : '{name.Kind}'", name)
                        {
                            Hint = GetHint(name.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            // generics
            result.AddMessages(
                EmitTypeDeclarationTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitTypeDeclarationHeritage(node, context, token)
            );

            context.Emission.Append(node, "{");

            result.AddMessages(
                EmitTypeDeclarationMembers(node, childContext, token)
            );

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitParameterDeclaration(ParameterDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var label = node.Label;
            var name = node.Name;
            var type = node.Type;
            var @default = node.Default;

            if(node.Parent.Kind != SemanticKind.LambdaDeclaration)
            {
                if (label != null)
                {
                    // [dho] only emit the label separately to the parameter name if the label
                    // is different from the name.. otherwise the name alone suffices to represent 
                    // both synctactically - 09/12/18
                    if(label.Kind != SemanticKind.Identifier ||
                        ASTNodeHelpers.GetLexeme(label) != ASTNodeHelpers.GetLexeme(name))
                    {
                        result.AddMessages(
                            EmitNode(label, childContext, token)
                        );

                        context.Emission.Append(node, " ");
                    }
                }
                else
                {
                    context.Emission.Append(node, "_ ");
                }
            }


            result.AddMessages(
                EmitNode(name, childContext, token)
            );

            if (type != null)
            {
                context.Emission.Append(node, " : ");

                var metaFlags = MetaHelpers.ReduceFlags(node);
                
                if((metaFlags & MetaFlag.ByReference) == MetaFlag.ByReference)
                {
                    context.Emission.Append(node, "inout ");
                }

                result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

                result.AddMessages(
                    EmitNode(type, childContext, token)
                );

                if((metaFlags & MetaFlag.Optional) == MetaFlag.Optional)
                {
                    context.Emission.Append(node, "?");
                }

                result.AddMessages(
                    ReportUnsupportedMetaFlags(node, metaFlags & ~(MetaFlag.ByReference | MetaFlag.Optional), context, token)
                );
                // [dho] TODO EmitOrnamentation - 01/03/19
                // switch(node.Nullability)
                // {
                //     case Nullability.Safe:
                //         context.Emission.Append(node, "?");
                //     break;

                //     case Nullability.Forced:
                //         context.Emission.Append(node, "!");
                //     break;
                // }
            }
            else if(node.Parent.Kind != SemanticKind.LambdaDeclaration)
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Expected parameter to have a specified type", node)
                    {
                        Hint = GetHint(node.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            if (@default != null)
            {
                context.Emission.Append(node, "=");

                result.AddMessages(
                    EmitNode(@default, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitParenthesizedTypeReference(ParenthesizedTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPointerDereference(PointerDereference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPostDecrement(PostDecrement node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPostIncrement(PostIncrement node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPreDecrement(PreDecrement node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPreIncrement(PreIncrement node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPredicateFlat(PredicateFlat node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Predicate, childContext, token)
            );

            context.Emission.Append(node, " ? ");

            result.AddMessages(
                EmitNode(node.TrueValue, childContext, token)
            );

            context.Emission.Append(node, " : ");

            result.AddMessages(
                EmitNode(node.FalseValue, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitPredicateJunction(PredicateJunction node, Context context, CancellationToken token)
        {
            /*
                if condition 1 {
                    statements to execute if condition 1 is true
                } else if condition 2 {
                    statements to execute if condition 2 is true
                } else {
                    statements to execute if both conditions are false
                }
            */

            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "if ");

            result.AddMessages(
                EmitNode(node.Predicate, childContext, token)
            );

            result.AddMessages(
                EmitBlockLike(node.TrueBranch, node.Node, childContext, token)
            );

            if(node.FalseBranch != null)
            {
                context.Emission.AppendBelow(node, "else ");

                if(node.FalseBranch.Kind == SemanticKind.PredicateJunction)
                {
                    result.AddMessages(
                        EmitPredicateJunction(ASTNodeFactory.PredicateJunction(node.AST, node.FalseBranch), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        EmitBlockLike(node.FalseBranch, node.Node, childContext, token)
                    );
                }
            }
            
            return result;
        }

        public override Result<object> EmitPrioritySymbolResolutionContext(PrioritySymbolResolutionContext node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPropertyDeclaration(PropertyDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "var ");

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            var type = node.Type;
            var accessor = node.Accessor;
            var mutator = node.Mutator;

            if (type != null)
            {
                context.Emission.Append(node, " : ");

                result.AddMessages(
                    EmitNode(type, childContext, token)
                );
            }

            context.Emission.Append(node, "{");

            context.Emission.Indent();

            if(accessor != null)
            {
                result.AddMessages(
                    EmitNode(accessor, childContext, token)
                );
            }

            if(mutator != null)
            {
                result.AddMessages(
                    EmitNode(mutator, childContext, token)
                );
            }

            context.Emission.Outdent();

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitQualifiedAccess(QualifiedAccess node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Incident, childContext, token)
            );

            var rhs = ASTNodeHelpers.RHS(context.AST, node.Incident);

            if(Languages.LanguageSemantics.Swift.IsInvocationLikeExpression(context.AST, rhs))
            {
                var parent = node.Parent;

                if(parent?.Kind == SemanticKind.QualifiedAccess)
                {
                    context.Emission.AppendBelow(node, ".");
                    // [dho] keep same indentation - 30/06/19
                    result.AddMessages(
                        EmitNode(node.Member, childContext, token)
                    );
                }
                else
                {
                    context.Emission.Indent();
                    context.Emission.AppendBelow(node, ".");
                    result.AddMessages(
                        EmitNode(node.Member, childContext, token)
                    );
                    context.Emission.Outdent();
                }
            }
            else
            {
                context.Emission.Append(node, ".");
           
                result.AddMessages(
                    EmitNode(node.Member, childContext, token)
                );
            }


            return result;
        }

        public override Result<object> EmitRaiseError(RaiseError node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitReferenceAliasDeclaration(ReferenceAliasDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }
    
        public override Result<object> EmitRegularExpressionConstant(RegularExpressionConstant node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitRemainder(Remainder node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "%", context, token);
        }

        public override Result<object> EmitRemainderAssignment(RemainderAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "*=", context, token);
        }

        public override Result<object> EmitSafeCast(SafeCast node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, " as? ");
            
            result.AddMessages(
                EmitNode(node.TargetType, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitSmartCast(SmartCast node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitSpreadDestructuring(SpreadDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitStrictEquivalent(StrictEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "==", context, token);
        }

        public override Result<object> EmitStrictGreaterThan(StrictGreaterThan node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, ">", context, token);
        }

        public override Result<object> EmitStrictGreaterThanOrEquivalent(StrictGreaterThanOrEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, ">=", context, token);
        }

        public override Result<object> EmitStrictLessThan(StrictLessThan node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "<", context, token);
        }

        public override Result<object> EmitStrictLessThanOrEquivalent(StrictLessThanOrEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "<=", context, token);
        }

        public override Result<object> EmitStrictNonEquivalent(StrictNonEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "!=", context, token);
        }

        public override Result<object> EmitStringConstant(StringConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if(ASTNodeHelpers.IsMultilineString(node))
            {
                context.Emission.Append(node, $"\"\"\"{node.Value}\"\"\"");
            }
            else
            {
                context.Emission.Append(node, $"\"{node.Value}\"");
            }

            return result;
        }

        public override Result<object> EmitSubtraction(Subtraction node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "-", context, token);
        }

        public override Result<object> EmitSubtractionAssignment(SubtractionAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "-=", context, token);
        }

        public override Result<object> EmitSuperContextReference(SuperContextReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "super");

            return result;
        }

        public override Result<object> EmitTupleConstruction(TupleConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "(");

            result.AddMessages(
                EmitCSV(node.Members, childContext, token).Messages
            );
        
            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitTupleTypeReference(TupleTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, "(");

            result.AddMessages(
                EmitCSV(node.Types, childContext, token).Messages
            );
        
            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitTypeAliasDeclaration(TypeAliasDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;
            
            context.Emission.AppendBelow(node, "typealias ");

            var from = node.From;
            var template = node.Template;
            var to = node.Name;

            result.AddMessages(
                EmitNode(to, childContext, token)
            );


            if(template.Length > 0)
            {
                var templateMarker = template[0];

                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Type alias must not be templated", templateMarker)
                    {
                        Hint = GetHint(templateMarker.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            context.Emission.Append(node, "=");

            result.AddMessages(
                EmitNode(from, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitTypeInterrogation(TypeInterrogation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, "type(of:");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token).Messages
            );
        
            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitTypeParameterDeclaration(TypeParameterDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            var name = node.Name;
            var constraints = node.Constraints;
            var @default = node.Default;

            result.AddMessages(
                EmitNode(name, childContext, token)
            );

            if (constraints.Length > 0)
            {
                context.Emission.Append(node, " : ");

                result.AddMessages(EmitDelimited(constraints, " & ", childContext, token));
            }

            // if (node.SuperConstraints != null)
            // {
            //     var constraints = node.SuperConstraints;

            //     if (constraints.Kind == SemanticKind.OrderedGroup)
            //     {
            //         result.AddMessages(
            //             new NodeMessage(MessageKind.Error, $"Expected a single super type constraint but found a group", constraints)
            //             {
            //                 Hint = GetHint(constraints.Origin),
            //                 Tags = DiagnosticTags
            //             }
            //         );
            //     }
            //     else
            //     {
            //         context.Emission.Append(node, " : ");

            //         result.AddMessages(
            //             EmitNode(constraints, childContext, token)
            //         );
            //     }
            // }

            // if (node.SubConstraints != null)
            // {
            //     result.AddMessages(
            //         CreateUnsupportedFeatureResult(node.SubConstraints, "sub type constraints", childContext)
            //     );
            // }

            if (@default != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(@default, "default type constraints")
                );
            }

            return result;
        }

        public override Result<object> EmitTypeQuery(TypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitTypeTest(TypeTest node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, " is ");
            
            result.AddMessages(
                EmitNode(node.Criteria, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitUpperBoundedTypeConstraint(UpperBoundedTypeConstraint node, Context context, CancellationToken token)
        {
            // var childContext = ContextHelpers.Clone(context);

            // context.Emission.Append(node, ": ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitUnionTypeReference(UnionTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitViewConstruction(ViewConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitViewDeclaration(ViewDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitWhilePredicateLoop(WhilePredicateLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.AppendBelow(node, "while ");

            result.AddMessages(EmitNode(node.Condition, childContext, token));

            context.Emission.AppendBelow(node, "{");

            context.Emission.Indent();

            result.AddMessages(EmitBlockLike(node.Body, node.Node, childContext, token));

            context.Emission.Outdent();

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitWildcardExportReference(WildcardExportReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        private Result<object> EmitBinaryLike(BinaryLike node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var operands = node.Operands;

            result.AddMessages(EmitDelimited(operands, $" {operatorToken} ", childContext, token));

            return result;
        }

        private Result<object> EmitPrefixUnaryLike(UnaryLike node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // operator
            context.Emission.Append(node, operatorToken);
            
            // operand
            result.AddMessages(
                EmitNode(node.Operand, childContext, token)
            );

            return result;
        }

        private Result<object> EmitBitwiseShift(BitwiseShift node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // left operand
            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            // operator
            context.Emission.Append(node, operatorToken);

            // right operand
            result.AddMessages(
                EmitNode(node.Offset, childContext, token)
            );

            return result;
        }

        private Result<object> EmitAssignmentLike(AssignmentLike node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Storage, childContext, token)
            );

            // operator
            context.Emission.Append(node, $" {operatorToken} ");

            result.AddMessages(
                EmitNode(node.Value, childContext, token)
            );

            return result;
        }

        #region type parts

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // [dho] TODO CLEANUP straight up copypasta from CSEmitter - 22/09/18
        private Result<object> EmitTypeDeclarationHeritage(TypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var supers = node.Supers;
            var interfaces = node.Interfaces;

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            if (supers.Length > 0)
            {
                context.Emission.Append(node, " : ");

                result.AddMessages(
                    EmitCSV(supers, childContext, token)
                );

                if (interfaces.Length > 0)
                {
                    context.Emission.Append(node, ", ");

                    result.AddMessages(
                        EmitCSV(interfaces, childContext, token)
                    );
                }
            }
            else if (interfaces.Length > 0)
            {
                context.Emission.Append(node, " : ");

                result.AddMessages(
                    EmitCSV(interfaces, childContext, token)
                );
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitTypeDeclarationMembers(TypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var members = node.Members;

            context.Emission.Indent();

            foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(members))
            {
                context.Emission.AppendBelow(member, "");

                result.AddMessages(
                    EmitNode(member, context, token)
                );
            }

            context.Emission.Outdent();
        

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitTypeDeclarationTemplate(TypeDeclaration typeDecl, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var template = typeDecl.Template;

            if (template.Length > 0)
            {
                context.Emission.Append(typeDecl, "<");

                result.AddMessages(
                    EmitCSV(template, context, token)
                );

                context.Emission.Append(typeDecl, ">");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<object> EmitAnnotationsAndModifiers<T>(T node, Context context, CancellationToken token) where T : ASTNode, IAnnotated, IModified
        {
            var result = new Result<object>();

            {
                if(node.Annotations.Length > 0)
                {
                    result.AddMessages(EmitDelimited(node.Annotations, " ", context, token));

                    context.Emission.Append(node, " ");
                }

                if(node.Modifiers.Length > 0)
                {
                    result.AddMessages(EmitDelimited(node.Modifiers, " ", context, token));

                    context.Emission.Append(node, " ");
                }
            }

            return result;
        }

        #endregion

        #region function parts

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitFunctionName<T>(T fn, Context context, CancellationToken token) where T : ASTNode, IFunctionLike
        {
            var name = fn.Name;

            if (name.Kind == SemanticKind.Identifier)
            {
                return EmitIdentifier(ASTNodeFactory.Identifier(fn.AST, (DataNode<string>)name), context, token);
            }
            else
            {
                var result = new Result<object>();

                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Unsupported name type : '{name.Kind}'", name)
                    {
                        Hint = GetHint(name.Origin),
                        Tags = DiagnosticTags
                    }
                );

                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitFunctionTemplate<T>(T fn, Context context, CancellationToken token) where T : ASTNode, ITemplated
        {
            var result = new Result<object>();

            var template = fn.Template;

            if (template.Length > 0)
            {
                context.Emission.Append(fn, "<");

                result.AddMessages(
                    EmitCSV(template, context, token)
                );

                context.Emission.Append(fn, ">");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitFunctionParameters<T>(T fn, Context context, CancellationToken token) where T : ASTNode, IParametered
        {
            var result = new Result<object>();

            var parameters = fn.Parameters;

            context.Emission.Append(fn, "(");

            if (parameters.Length > 0)
            {
                result.AddMessages(
                    EmitCSV(parameters, context, token)
                );
            }

            context.Emission.Append(fn, ")");

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitInvocationArguments<T>(T invocation, Context context, CancellationToken token) where T : ASTNode, IArgumented
        {
            var result = new Result<object>();

            var arguments = invocation.Arguments;

            if(arguments.Length > 0)
            {
                var lastArg = arguments[arguments.Length - 1];

                var maybeLambda = lastArg;

                if(maybeLambda.Kind == SemanticKind.InvocationArgument)
                {
                    maybeLambda = ASTNodeFactory.InvocationArgument(context.AST, lastArg).Value;
                }


                if(maybeLambda.Kind == SemanticKind.LambdaDeclaration)
                {
                    var precedingArgumentsCount = arguments.Length - 1;

                    if(precedingArgumentsCount > 0)
                    {
                        context.Emission.Append(invocation, "(");
                        
                        var precedingArguments = new Node[arguments.Length - 1];
                        
                        System.Array.Copy(arguments, precedingArguments, precedingArguments.Length);

                        result.AddMessages(
                            EmitCSV(precedingArguments, context, token)
                        );

                        context.Emission.Append(invocation, ")");
                    }

                    {
                        var lambdaDecl = ASTNodeFactory.LambdaDeclaration(context.AST, maybeLambda);

                        result.AddMessages(EmitLambdaDeclaration(lambdaDecl, context, token));
                    }
                }
                else
                {
                    context.Emission.Append(invocation, "(");

                    if (arguments.Length > 0)
                    {
                        result.AddMessages(
                            EmitCSV(arguments, context, token)
                        );
                    }

                    context.Emission.Append(invocation, ")");
                }
            }
            else
            {
                context.Emission.Append(invocation, "()");
            }

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitBlockLike(Node body, Node parent, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if (body != null)
            {
                if (body.Kind == SemanticKind.Block)
                {
                    result.AddMessages(
                        EmitBlock(ASTNodeFactory.Block(context.AST, body), context, token)
                    );
                }
                else
                {
                    context.Emission.Append(body, "{");
                    context.Emission.Indent();

                    context.Emission.AppendBelow(body, "");

                    result.AddMessages(
                        EmitNode(body, context, token)
                    );

                    context.Emission.Outdent();
                    context.Emission.AppendBelow(body, "}");
                }
            }
            else
            {
                context.Emission.Append(parent, "{}");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitBlockLike(Node[] members, Node parent, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if (members?.Length > 0)
            {
                context.Emission.Append(parent, "{");
                context.Emission.Indent();

                for(int i = 0; i < members.Length; ++i)
                {
                    var member = members[i];

                    context.Emission.AppendBelow(member, "");

                    result.AddMessages(
                        EmitNode(member, context, token)
                    );

                    // if(RequiresSemicolonSentinel(member))
                    // {
                    //     context.Emission.Append(member, ";");
                    // }
                }

                context.Emission.Outdent();
                context.Emission.AppendBelow(parent, "}");
            
            }
            else
            {
                context.Emission.Append(parent, "{}");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitFunctionType(Node type, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if (type != null)
            {
                context.Emission.Append(type, " -> ");

                result.AddMessages(
                    EmitNode(type, context, token)
                );
            }

            return result;
        }

        #endregion

        #region meta

        const MetaFlag TypeDeclarationVisibilityFlags = MetaFlag.FileVisibility | /* MetaFlag.SubtypeVisibility | */MetaFlag.TypeVisibility | MetaFlag.WorldVisibility;

        private Result<object> EmitTypeDeclarationVisibilityFlags(TypeDeclaration node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if((flags & MetaFlag.FileVisibility) == MetaFlag.FileVisibility)
            {
                context.Emission.Append(node, "fileprivate ");
            }
            
            if((flags & MetaFlag.TypeVisibility) == MetaFlag.TypeVisibility)
            {
                context.Emission.Append(node, "private ");
            }

            if((flags & MetaFlag.WorldVisibility) == MetaFlag.WorldVisibility)
            {
                context.Emission.Append(node, "public ");
            }

            return result;
        }

        const MetaFlag TypeDeclarationMemberFlags = MetaFlag.Static | /* MetaFlag.SubtypeVisibility |*/ MetaFlag.TypeVisibility | MetaFlag.WorldVisibility;

        private Result<object> EmitFlagsForTypeDeclarationMember(Declaration node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if((flags & MetaFlag.Static) == MetaFlag.Static)
            {
                context.Emission.Append(node, "static ");
            }

            if((flags & MetaFlag.TypeVisibility) == MetaFlag.TypeVisibility)
            {
                context.Emission.Append(node, "private ");
            }

            if((flags & MetaFlag.WorldVisibility) == MetaFlag.WorldVisibility)
            {
                context.Emission.Append(node, "public ");
            }

            return result;
        }

        #endregion

        private Result<object> EmitCSV(Node[] nodes, Context context, CancellationToken token)
        {
            return EmitDelimited(nodes, ", ", context, token);
        }
    }
}
            