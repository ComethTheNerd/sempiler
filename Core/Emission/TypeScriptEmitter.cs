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

    public class TypeScriptEmitter : BaseEmitter
    {
        public TypeScriptEmitter() : base(new string[]{ "typescript", PhaseKind.Emission.ToString("g").ToLower() })
        {
        }

        const MetaFlag AccessorDeclarationFlags = TypeDeclarationMemberFlags | /* MetaFlag.Generator | MetaFlag.Asynchronous |*/ MetaFlag.Optional;

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

            if((metaFlags & MetaFlag.Optional) == MetaFlag.Optional)
            {
                context.Emission.Append(node, "?");
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~AccessorDeclarationFlags, context, token)
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

        public override Result<object> EmitAccessorSignature(AccessorSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // context.Emission.AppendBelow(node, "");

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
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitAnnotation(Annotation node, Context context, CancellationToken token)
        {
            context.Emission.Append(node, "@");

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            return EmitNode(node.Expression, childContext, token);
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

            if(node.Members.Length > 0)
            {
                result.AddMessages(
                    EmitCSV(node.Members, childContext, token)
                );  
            }

            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitArrayTypeReference(ArrayTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(EmitNode(node.Type, childContext, token));

            context.Emission.Append(node, "[]");

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
            return EmitPrefixUnaryLike(node, "~", context, token);
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
            return EmitBitwiseShift(node, ">>>", context, token);
        }

        public override Result<object> EmitBitwiseUnsignedRightShiftAssignment(BitwiseUnsignedRightShiftAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, ">>>=", context, token);
        }

        public override Result<object> EmitBlock(Block node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "{");

            if (node.Content.Length > 0)
            {
                context.Emission.Indent();

                foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Content))
                {
                    context.Emission.AppendBelow(node, "");

                    result.AddMessages(
                        EmitNode(member, context, token)
                    );

                    if(RequiresSemicolonSentinel(member))
                    {
                        context.Emission.Append(member, ";");
                    }
                }

                context.Emission.Outdent();
            }

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
            var result = new Result<object>();

            context.Emission.Append(node, "debugger");

            return result;
        }

        public override Result<object> EmitClauseBreak(ClauseBreak node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "break");
            
            if(node.Expression != null)
            {
                context.Emission.Append(node, " ");

                result.AddMessages(EmitNode(node.Expression, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitCodeConstant(CodeConstant node, Context context, CancellationToken token)
        {
            return base.EmitCodeConstant(node, context, token);
        }

        public override Result<object> EmitCollectionDestructuring(CollectionDestructuring node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "[");

            if(node.Members.Length > 0)
            {
                result.AddMessages(
                    EmitCSV(node.Members, childContext, token)
                );  
            }

            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitCompilerHint(CompilerHint node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "declare ");

            return EmitNode(node.Subject, childContext, token);
        }

        // [dho] TODO move to a helper for other emitters that will spit out 1 file per component, eg. Java etc - 21/09/18 (ported 24/04/19)
        // [dho] TODO CLEANUP this is duplicated in CSEmitter (apart from FileEmission extension) - 21/09/18 (ported 24/04/19)
        public override Result<object> EmitComponent(Component node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            // if (node.Origin.Kind != NodeOriginKind.Source)
            // {
            //     result.AddMessages(
            //         new NodeMessage(MessageKind.Error, $"Could not write emission in artifact due to unsupported origin kind '{node.Origin.Kind}'", node)
            //         {
            //             Hint = GetHint(node.Origin),
            //             Tags = DiagnosticTags
            //         }
            //     );

            //     return result;
            // }

            // var sourceWithLocation = ((SourceNodeOrigin)node.Origin).Source as ISourceWithLocation<IFileLocation>;

            // if (sourceWithLocation == null || sourceWithLocation.Location == null)
            // {
            //     result.AddMessages(
            //         new NodeMessage(MessageKind.Error, $"Could not write emission in artifact because output location cannot be determined", node)
            //         {
            //             Hint = GetHint(node.Origin),
            //             Tags = DiagnosticTags
            //         }
            //     );

            //     return result;
            // }

            var location = node.Name;//sourceWithLocation.Location;

            var relParentDirPath = node.Name.Replace(context.Session.BaseDirectory.ToPathString(), "");

            // [dho] our emissions will be stored in a file with the same relative path and name
            // as the original source for this component, eg. hello/World.ts => hello/World.java - 26/04/19
            var file = new FileEmission(
                FileSystem.ParseFileLocation($"{relParentDirPath}.ts")
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
            // // childContext.Parent = node;
            childContext.Emission = file; // [dho] this is where children of this component should write to - 29/08/18

            foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(context.AST, node))
            {
                result.AddMessages(
                    EmitNode(child, childContext, token)
                );

                if(RequiresSemicolonSentinel(child))
                {
                    childContext.Emission.Append(child, ";");
                }

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
            // // childContext.Parent = node;

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
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(EmitNode(node.Predicate, childContext, token));

            context.Emission.Append(node, "?");

            result.AddMessages(EmitNode(node.TrueType, childContext, token));

            context.Emission.Append(node, ":");

            result.AddMessages(EmitNode(node.FalseType, childContext, token));

            return result;
        }

        public override Result<object> EmitLiteralTypeReference(LiteralTypeReference node, Context context, CancellationToken token)
        {
            return EmitNode(node.Literal, context, token);
        }

        public override Result<object> EmitComputedValue(ComputedValue node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, "[");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );  
        
            context.Emission.Append(node, "]");

            return result;
        }

        const MetaFlag ConstructorDeclarationFlags = TypeDeclarationMemberFlags | MetaFlag.Optional | MetaFlag.Abstract;

        public override Result<object> EmitConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitFlagsForTypeDeclarationMember(node, metaFlags, context, token));

            if((metaFlags & MetaFlag.Abstract) == MetaFlag.Abstract)
            {
                context.Emission.Append(node, "abstract ");
            }

            // [dho] TODO super call - 29/09/18

            context.Emission.AppendBelow(node, "constructor");

            if(node.Name != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Name, "constructor names")
                );
            }

            if((metaFlags & MetaFlag.Optional) == MetaFlag.Optional)
            {
                context.Emission.Append(node, "?");
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~ConstructorDeclarationFlags, context, token)
            );

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(node.Type != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Type, "constructor return types")
                );
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitConstructorSignature(ConstructorSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            context.Emission.Append(node, "constructor");

            if(node.Name != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Name, "constructor names")
                );
            }

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(node.Type != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Type, "constructor return types")
                );
            }

            return result;
        }

        public override Result<object> EmitConstructorTypeReference(ConstructorTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // [dho] TODO modifiers! - 20/09/18

            context.Emission.AppendBelow(node, "new ");

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(node.Type != null)
            {
                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );
            }

            return result;
        }

        const MetaFlag DataValueDeclarationScopeFlags = MetaFlag.BlockScope | MetaFlag.FunctionScope;

        public override Result<object> EmitDataValueDeclaration(DataValueDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            var isBlockScope = false;

            if((metaFlags & MetaFlag.Constant) == MetaFlag.Constant)
            {
                isBlockScope = true;

                context.Emission.Append(node, "const ");
            }
            else if((metaFlags & MetaFlag.BlockScope) == MetaFlag.BlockScope)
            {
                isBlockScope = true;

                context.Emission.Append(node, "let ");
            }

            if((metaFlags & MetaFlag.FunctionScope) == MetaFlag.FunctionScope || !isBlockScope)
            {
                context.Emission.Append(node, "var ");
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
            return new Result<object>();
        }

        public override Result<object> EmitDestruction(Destruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDestructorDeclaration(DestructorDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDestructorSignature(DestructorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDestructuredMember(DestructuredMember node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(EmitDestructuredMemberName(node.Name, childContext, token));

            if(node.Default != null)
            {
                context.Emission.Append(node, "=");

                result.AddMessages(EmitNode(node.Default, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitDictionaryConstruction(DictionaryConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var entryContext = ContextHelpers.Clone(context);
            // entry// // Context.Parent = node;

            context.Emission.Append(node, "{");

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

            context.Emission.Append(node, "}");

            return result;
        }

        public override Result<object> EmitDictionaryTypeReference(DictionaryTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "{ [key : ");

            result.AddMessages(EmitNode(node.KeyType, childContext, token));

            context.Emission.Append(node, "] : ");

            result.AddMessages(EmitNode(node.StoredType, childContext, token));

            context.Emission.Append(node, "}");

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
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDoOrRecoverErrorTrap(DoOrRecoverErrorTrap node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDoWhilePredicateLoop(DoWhilePredicateLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "do");

            result.AddMessages(EmitBlockLike(node.Body, node.Node, childContext, token));

            context.Emission.AppendBelow(node, "while(");

            result.AddMessages(EmitNode(node.Condition, childContext, token));

            context.Emission.Append(node, ")");

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
            // // childContext.Parent = node;

            context.Emission.Append(node, "{");

            context.Emission.Indent();
            {
                foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Members))
                {
                    context.Emission.AppendBelow(member, "");

                    result.AddMessages(
                        EmitNode(member, childContext, token)
                    );

                    context.Emission.Append(member, ",");
                }

            }
            context.Emission.Outdent();

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitDynamicTypeReference(DynamicTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "{");

            context.Emission.Indent();
            {
                foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Signatures))
                {
                    context.Emission.AppendBelow(member, "");

                    result.AddMessages(
                        EmitNode(member, childContext, token)
                    );
                }
            }
            context.Emission.Outdent();

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitEntityDestructuring(EntityDestructuring node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var entryContext = ContextHelpers.Clone(context);
            // entry// // Context.Parent = node;

            context.Emission.Append(node, "{");

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

            context.Emission.Append(node, "}");

            return result;
        }

        public override Result<object> EmitEnumerationMemberDeclaration(EnumerationMemberDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "case ");
            
            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            if(node.Initializer != null)
            {
                result.AddMessages(
                    EmitNode(node.Initializer, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitEnumerationTypeDeclaration(EnumerationTypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));
            
            result.AddMessages(EmitTypeDeclarationVisibilityFlags(node, metaFlags, context, token));
                
            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~TypeDeclarationVisibilityFlags, context, token)
            );
            
            context.Emission.Append(node, "enum ");

            var name = node.Name;
            var template = node.Template;
            var supers = node.Supers;
            var interfaces = node.Interfaces;
            // var members = node.Members;

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

            if(template.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node, "enum templates")
                );
            }

            if(supers.Length > 0 || interfaces.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            context.Emission.Append(node, "{");

            result.AddMessages(
                EmitTypeDeclarationMembers(node, childContext, token)
            );

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitErrorFinallyClause(ErrorFinallyClause node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "finally ");

            if(node.Expression.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Expression, "finally clause expression")
                );
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
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
                context.Emission.Append(node, "(");

                result.AddMessages(
                    EmitNode(expression[0], childContext, token)
                );

                context.Emission.Append(node, ")");
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
                EmitBlockLike(body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitErrorTrapJunction(ErrorTrapJunction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "try ");

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
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "void ");

            return EmitNode(node.Expression, childContext, token);
        }

        public override Result<object> EmitExponentiation(Exponentiation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitExponentiationAssignment(ExponentiationAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitExportDeclaration(ExportDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "export ");

            var clauses = node.Clauses;
            var specifier = node.Specifier;

            if(node.Clauses.Length > 0)
            {
                result.AddMessages(EmitImportOrExportClauses(node, childContext, token));

                if(specifier != null)
                {
                    if(specifier.Kind == SemanticKind.StringConstant)
                    {
                        context.Emission.Append(node, " from ");
                    }
                    else
                    {
                        context.Emission.Append(node, " = ");
                    }
                }

            }

            if(specifier != null)
            {
                result.AddMessages(
                    EmitNode(specifier, childContext, token)
                );
            }

            return result;
        }

        const MetaFlag FieldDeclarationFlags = TypeDeclarationMemberFlags | MetaFlag.ReadOnly;

        public override Result<object> EmitFieldDeclaration(FieldDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitFlagsForTypeDeclarationMember(node, metaFlags, context, token));

            if((metaFlags & MetaFlag.ReadOnly) == MetaFlag.ReadOnly)
            {
                context.Emission.Append(node, "readonly ");
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~FieldDeclarationFlags, context, token)
            );


            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            if (node.Type != null)
            {
                context.Emission.Append(node.Type, " : ");

                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );
            }

            if (node.Initializer != null)
            {
                context.Emission.Append(node.Initializer, 
                    node.Parent.Kind == SemanticKind.DynamicTypeConstruction ? " : " : " = "    
                );

                result.AddMessages(
                    EmitNode(node.Initializer, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitFieldSignature(FieldSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitNode(node.Name, childContext, token));

            if (node.Type != null)
            {
                context.Emission.Append(node.Type, " : ");

                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );
            }

            if (node.Initializer != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Initializer, "field signature initializer")
                );
            }

            return result;
        }

        public override Result<object> EmitForKeysLoop(ForKeysLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "for(");

            result.AddMessages(
                EmitNode(node.Handle, childContext, token)
            );

            context.Emission.Append(node, " in ");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, ")");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitForMembersLoop(ForMembersLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "for(");

            result.AddMessages(
                EmitNode(node.Handle, childContext, token)
            );

            context.Emission.Append(node, " of ");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, ")");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitForPredicateLoop(ForPredicateLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "for(");

            if(node.Initializer != null)
            {
                result.AddMessages(
                    EmitNode(node.Initializer, childContext, token)
                );
            }

            context.Emission.Append(node, ";");

            if(node.Condition != null)
            {
                result.AddMessages(
                    EmitNode(node.Condition, childContext, token)
                );
            }

            context.Emission.Append(node, ";");

            if(node.Iterator != null)
            {
                result.AddMessages(
                    EmitNode(node.Iterator, childContext, token)
                );
            }

            context.Emission.Append(node, ")");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitForcedCast(ForcedCast node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "<");
            
            result.AddMessages(
                EmitNode(node.TargetType, childContext, token)
            );

            context.Emission.Append(node, ">");


            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            return result;
        }

        const MetaFlag FunctionDeclarationFlags = MetaFlag.Generator | MetaFlag.Asynchronous | MetaFlag.WorldVisibility;

        public override Result<object> EmitFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            if((metaFlags & MetaFlag.Asynchronous) == MetaFlag.WorldVisibility)
            {
                context.Emission.Append(node, "public ");
            }

            if((metaFlags & MetaFlag.Asynchronous) == MetaFlag.Asynchronous)
            {
                context.Emission.Append(node, "async ");
            }

            context.Emission.Append(node, "function ");

            if (node.Name != null)
            {
                result.AddMessages(
                    EmitFunctionName(node, childContext, token)
                );
            }


            if((metaFlags & MetaFlag.Generator) == MetaFlag.Generator)
            {
                context.Emission.Append(node, "*");
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

            if (node.Value != null)
            {
                context.Emission.Append(node, "return ");

                var childContext = ContextHelpers.Clone(context);
                // // childContext.Parent = node;

                result.AddMessages(
                    EmitNode(node.Value, childContext, token)
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
            // // childContext.Parent = node;

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            context.Emission.Append(node, " => ");

            result.AddMessages(
                EmitFunctionType(node.Type, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitGeneratorSuspension(GeneratorSuspension node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "yield ");

            return EmitNode(node.Value, childContext, token);
        }

        public override Result<object> EmitGlobalDeclaration(GlobalDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitIdentity(Identity node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "+");

            return EmitNode(node.Operand, childContext, token);
        }

        // public override Result<object> EmitIfDirective(IfDirective node, Context context, CancellationToken token)
        // {
        //     return CreateUnsupportedFeatureResult(node);
        // }

        public override Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            context.Emission.Append(node, "import ");

            if(node.Clauses.Length > 0)
            {
                result.AddMessages(EmitImportOrExportClauses(node, childContext, token));

                if(node.Specifier.Kind == SemanticKind.StringConstant)
                {
                    context.Emission.Append(node, " from ");
                }
                else
                {
                    context.Emission.Append(node, " = ");
                }
            }

            result.AddMessages(
                EmitNode(node.Specifier, childContext, token)
            );
 
            return result;
        }

        public override Result<object> EmitIncidentContextReference(IncidentContextReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "this");

            return result;
        }

        public override Result<object> EmitIncidentTypeConstraint(IncidentTypeConstraint node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "implements ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitIndexTypeQuery(IndexTypeQuery node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "keyof ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitInferredTypeQuery(InferredTypeQuery node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "infer ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitIndexedAccess(IndexedAccess node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

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
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            context.Emission.Append(node, "[");

            result.AddMessages(EmitCSV(node.Parameters, childContext, token));

            context.Emission.Append(node, "] : ");

            result.AddMessages(EmitNode(node.Type, childContext, token));

            return result;
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
                    context.Emission.Append(node, "interface ");
                    
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
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "await ");

            return EmitNode(node.Operand, childContext, token);
        }

        public override Result<object> EmitInterpolatedString(InterpolatedString node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            string delimiter;

            var parserName = node.ParserName;

            if(parserName != null)
            {
                result.AddMessages(
                    EmitNode(parserName, childContext, token)
                );

                delimiter = "```";
            }   
            else
            {
                delimiter = "`";
            }

            context.Emission.Append(node, delimiter);

            result.AddMessages(
                EmitInterpolatedStringMembers(node.Members, childContext, token)
            );

            context.Emission.Append(node, delimiter);

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
                    context.Emission.Append(member, "${");

                    result.AddMessages(
                        EmitNode(member, context, token)
                    );

                    context.Emission.Append(member, "}");
                }
            }

            return result;
        }

        public override Result<object> EmitInterpolatedStringConstant(InterpolatedStringConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            // [dho] NOTE no delimiters because the interpolated string that this node sits inside
            // will have the delimiters on it - 28/10/18
            context.Emission.Append(node, node.Value.Replace("\\", "\\\\"));

            return result;
        }

        public override Result<object> EmitIntersectionTypeReference(IntersectionTypeReference node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            return EmitDelimited(node.Types, "&", context, token);
        }

        public override Result<object> EmitIntrinsicTypeReference(IntrinsicTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            switch(node.Role)
            {
                case TypeRole.Any:
                    context.Emission.Append(node, "any");
                break;
                    
                case TypeRole.Boolean:
                    context.Emission.Append(node, "boolean");
                break;

                case TypeRole.Double64:
                case TypeRole.Float32:
                case TypeRole.Integer32:
                    context.Emission.Append(node, "number");
                break;

                case TypeRole.Never:
                    context.Emission.Append(node, "never");
                break;

                case TypeRole.String:
                    context.Emission.Append(node, "string");
                break;

                case TypeRole.Void:
                    context.Emission.Append(node, "void");
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
            var arguments = node.Arguments;

            result.AddMessages(
                EmitNode(subject, childContext, token)
            );

            if (template.Length > 0)
            {
                context.Emission.Append(node, "<");
                
                result.AddMessages(
                    EmitCSV(template, context, token)
                );
            
                context.Emission.Append(node, ">");
            }

            context.Emission.Append(node, "(");

            if (arguments.Length > 0)
            {
                result.AddMessages(
                    EmitCSV(arguments, childContext, token)
                );
            }

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitInvocationArgument(InvocationArgument node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            if(node.Label != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Label, "argument labels")
                );
            }

            result.AddMessages(
                EmitNode(node.Value, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitJumpToNextIteration(JumpToNextIteration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "continue");
            
            if(node.Label != null)
            {
                context.Emission.Append(node, " ");

                result.AddMessages(EmitNode(node.Label, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitKeyValuePair(KeyValuePair node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLabel(Label node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(EmitNode(node.Name, childContext, token));

            context.Emission.Append(node, ":");

            return result;
        }

        const MetaFlag LambdaDeclarationFlags = MetaFlag.Asynchronous;

        public override Result<object> EmitLambdaDeclaration(LambdaDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var metaFlags = MetaHelpers.ReduceFlags(node);

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            if (node.Name != null)
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Lambda must be anonymous", node.Name)
                    {
                        Hint = GetHint(node.Name.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            if((metaFlags & MetaFlag.Asynchronous) == MetaFlag.Asynchronous)
            {
                context.Emission.Append(node, "async ");
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~LambdaDeclarationFlags, context, token)
            );

            var template = node.Template;

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

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(node.Type != null)
            {
                result.AddMessages(
                    EmitFunctionType(node.Type, childContext, token)
                );
            }

            context.Emission.Append(node, "=>");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );
          
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
            // // childContext.Parent = node;

            context.Emission.Append(node, "break");
            
            if(node.Expression != null)
            {
                context.Emission.Append(node, " ");

                result.AddMessages(EmitNode(node.Expression, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitLooseEquivalent(LooseEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "==", context, token);
        }

        public override Result<object> EmitLooseNonEquivalent(LooseNonEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "!=", context, token);
        }

        public override Result<object> EmitLowerBoundedTypeConstraint(LowerBoundedTypeConstraint node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "super ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitMappedDestructuring(MappedDestructuring node, Context context, CancellationToken token)
        {
            return base.EmitMappedDestructuring(node, context, token);
        }

        public override Result<object> EmitMappedTypeReference(MappedTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "{");

            // [dho] TODO emit meta - 27/04/19

            result.AddMessages(EmitNode(node.TypeParameter, childContext, token));

            context.Emission.Append(node, " in keyof ");

            result.AddMessages(EmitNode(node.Type, childContext, token));

            // [dho] putting this here because at the moment we call emit twice on the `Type``
            // and `TypeParameter`.. so in an effort to avoid duplicated error messages, we will bail
            // out if we have a problem here - 27/04/19
            // [dho] TODO optimize how we emit this so we do not call emit twice on the `Type` and 
            // `TypeParameter` - 27/04/19
            if(Diagnostics.DiagnosticsHelpers.HasErrors(result))
            {
                return result;
            }

            context.Emission.Append(node, ": ");

            result.AddMessages(EmitNode(node.Type, childContext, token));

            context.Emission.Append(node, "[");

            result.AddMessages(EmitNode(node.TypeParameter, childContext, token));

            context.Emission.Append(node, "]; }");

            return result;
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
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "switch(");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, ")");

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
            return base.EmitMemberNameReflection(node, context, token);
        }

        public override Result<object> EmitMemberTypeConstraint(MemberTypeConstraint node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "in keyof ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitMembershipTest(MembershipTest node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(EmitNode(node.Criteria, childContext, token));

            context.Emission.Append(node, " in ");

            result.AddMessages(EmitNode(node.Subject, childContext, token));

            return result;
        }

        public override Result<object> EmitMeta(Meta node, Context context, CancellationToken token)
        {
            return base.EmitMeta(node, context, token);
        }

        public override Result<object> EmitMetaProperty(MetaProperty node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "new.");

            return EmitNode(node.Name, childContext, token);
        }

        const MetaFlag MethodDeclarationFlags = TypeDeclarationMemberFlags | MetaFlag.Generator | MetaFlag.Asynchronous | MetaFlag.Optional | MetaFlag.Abstract;

        public override Result<object> EmitMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitFlagsForTypeDeclarationMember(node, metaFlags, context, token));

            if((metaFlags & MetaFlag.Abstract) == MetaFlag.Abstract)
            {
                context.Emission.Append(node, "abstract ");
            }

            if((metaFlags & MetaFlag.Asynchronous) == MetaFlag.Asynchronous)
            {
                context.Emission.Append(node, "async ");
            }

            if((metaFlags & MetaFlag.Generator) == MetaFlag.Generator)
            {
                context.Emission.Append(node, "*");
            }

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
            // // childContext.Parent = node;

            // context.Emission.AppendBelow(node, "");

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

            if((metaFlags & MetaFlag.Optional) == MetaFlag.Optional)
            {
                context.Emission.Append(node, "?");
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~MutatorDeclarationFlags, context, token)
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

        public override Result<object> EmitMutatorSignature(MutatorSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // context.Emission.AppendBelow(node, "");

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

        public override Result<object> EmitMutex(AST.Mutex node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNamedTypeConstruction(NamedTypeConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "new ");

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

            context.Emission.Append(node, "(");

            if(node.Arguments.Length > 0)
            {
                result.AddMessages(
                    EmitCSV(node.Arguments, childContext, token)
                );
            }

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitNamedTypeQuery(NamedTypeQuery node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(EmitNode(node.Name, childContext, token));

            context.Emission.Append(node, " ");

            result.AddMessages(EmitDelimited(node.Constraints, " ", childContext, token));
            
            return result;
        }

        public override Result<object> EmitNamedTypeReference(NamedTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

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
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // context.Emission.AppendBelow(node, "");

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));
            
            result.AddMessages(EmitNamespaceDeclarationFlags(node, metaFlags, context, token));
            
            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~NamespaceDeclarationFlags, context, token)
            );

            context.Emission.Append(node, "namespace ");

            result.AddMessages(EmitNode(node.Name, childContext, token));

            var template = node.Template;

            if(template.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node, "namespace templates")
                );
            }

            var members = node.Members;

            result.AddMessages(EmitBlockLike(members, node.Node, childContext, token));

            return result;
        }

        public override Result<object> EmitNamespaceReference(NamespaceReference node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "as namespace ");

            return EmitNode(node.Name, childContext, token);
        }

        public override Result<object> EmitNonMembershipTest(NonMembershipTest node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "!(");

            result.AddMessages(EmitNode(node.Criteria, childContext, token));

            context.Emission.Append(node, " in ");

            result.AddMessages(EmitNode(node.Subject, childContext, token));

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitNop(Nop node, Context context, CancellationToken token)
        {
            return new Result<object>();
        }

        public override Result<object> EmitNotNull(NotNull node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNotNumber(NotNumber node, Context context, CancellationToken token)
        {
            context.Emission.Append(node, "Number.NaN");
            
            return new Result<object>();
        }

        public override Result<object> EmitNull(Null node, Context context, CancellationToken token)
        {
            context.Emission.Append(node, "null");
            
            return new Result<object>();
        }

        public override Result<object> EmitNullCoalescence(NullCoalescence node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "!!");

            result.AddMessages(EmitDelimited(node.Operands, " || !!", childContext, token));

            return result;
        }

        public override Result<object> EmitNumericConstant(NumericConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, node.Value.ToString());

            return result;
        }

        // [dho] added in `MetaFlag.ValueType` so that we can use `@struct` in the source without this emitter
        // complaining - 04/08/19 
        const MetaFlag ObjectTypeDeclarationVisibilityFlags = TypeDeclarationVisibilityFlags | MetaFlag.ValueType;

        public override Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));
            
            result.AddMessages(EmitTypeDeclarationVisibilityFlags(node, metaFlags, context, token));
            
            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~ObjectTypeDeclarationVisibilityFlags, context, token)
            );
        
            context.Emission.Append(node, "class ");

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

        // public override Result<object> EmitOrderedGroup(OrderedGroup node, Context context, CancellationToken token)
        // {
        //     return base.EmitOrderedGroup(node, context, token);
        // }

        public override Result<object> EmitParameterDeclaration(ParameterDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            if(node.Label != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Label, "parameter label")
                );
            }

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            if(node.Type != null)
            {
                context.Emission.Append(node, ":");

                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );
            }

            if(node.Default != null)
            {
                context.Emission.Append(node, "=");
                
                result.AddMessages(
                    EmitNode(node.Default, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitParenthesizedTypeReference(ParenthesizedTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;
            
            context.Emission.Append(node, "(");

            result.AddMessages(EmitNode(node.Type, childContext, token));

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitPointerDereference(PointerDereference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPostDecrement(PostDecrement node, Context context, CancellationToken token)
        {
            return EmitPostUnaryArithmetic(node, "--", context, token);
        }

        public override Result<object> EmitPostIncrement(PostIncrement node, Context context, CancellationToken token)
        {
            return EmitPostUnaryArithmetic(node, "++", context, token);
        }

        public override Result<object> EmitPreDecrement(PreDecrement node, Context context, CancellationToken token)
        {
            return EmitPreUnaryArithmetic(node, "--", context, token);
        }

        public override Result<object> EmitPreIncrement(PreIncrement node, Context context, CancellationToken token)
        {
            return EmitPreUnaryArithmetic(node, "++", context, token);
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
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var predicate = node.Predicate;
            var trueBranch = node.TrueBranch;
            var falseBranch = node.FalseBranch;

            context.Emission.Append(node, "if(");

            result.AddMessages(
                EmitNode(predicate, childContext, token)
            );

            context.Emission.Append(node, ")");

            result.AddMessages(
                EmitBlockLike(trueBranch, node.Node, childContext, token)
            );

            if(falseBranch != null)
            {
                context.Emission.AppendBelow(node, "else ");

                if(falseBranch.Kind == SemanticKind.PredicateJunction)
                {
                    result.AddMessages(
                        EmitPredicateJunction(ASTNodeFactory.PredicateJunction(node.AST, falseBranch), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        EmitBlockLike(falseBranch, node.Node, childContext, token)
                    );
                }
            }
            
            return result;
        }

        public override Result<object> EmitPrioritySymbolResolutionContext(PrioritySymbolResolutionContext node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "with(");

            result.AddMessages(EmitNode(node.SymbolProvider, childContext, token));

            context.Emission.Append(node, ")");

            result.AddMessages(EmitBlockLike(node.Scope, node.Node, childContext, token));
            
            return result;
        }

        public override Result<object> EmitPropertyDeclaration(PropertyDeclaration node, Context context, CancellationToken token)
        {
            // var result = new Result<object>();

            // var childContext = ContextHelpers.Clone(context);
            // // // childContext.Parent = node;

            // result.AddMessages(
            //     EmitNode(node.Name, childContext, token)
            // );

            // if (node.Type != null)
            // {
            //     context.Emission.Append(node, " : ");

            //     result.AddMessages(
            //         EmitNode(node.Type, childContext, token)
            //     );
            // }

            // context.Emission.Append(node, "{");

            // context.Emission.Indent();

            // if(node.Accessor != null)
            // {
            //     result.AddMessages(
            //         EmitNode(node.Accessor, childContext, token)
            //     );
            // }

            // if(node.Mutator != null)
            // {
            //     result.AddMessages(
            //         EmitNode(node.Mutator, childContext, token)
            //     );
            // }

            // context.Emission.Outdent();

            // context.Emission.AppendBelow(node, "}");

            // return result;
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitQualifiedAccess(QualifiedAccess node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Incident, childContext, token)
            );

            context.Emission.Append(node, ".");

            result.AddMessages(
                EmitNode(node.Member, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitRaiseError(RaiseError node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "throw");
            
            if(node.Operand != null)
            {
                context.Emission.Append(node, " ");

                result.AddMessages(EmitNode(node.Operand, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitReferenceAliasDeclaration(ReferenceAliasDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            // [dho] CLEANUP infer the context from ancestors ? - 27/04/19
            result.AddMessages(
                new NodeMessage(MessageKind.Error, $"Cannot emit reference alias declaration out of context", node)
                {
                    Hint = GetHint(node.Origin),
                    Tags = DiagnosticTags
                }
            );

            return result;
        }
    
        const MetaFlag RegularExpressionConstantFlags = MetaFlag.GlobalSearch | MetaFlag.CaseInsensitive | MetaFlag.MultiLineSearch |
                                                          MetaFlag.DotsAsNewLines | MetaFlag.UnicodeCodePoints | MetaFlag.StickySearch;   
        public override Result<object> EmitRegularExpressionConstant(RegularExpressionConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();
            
            var metaFlags = MetaHelpers.ReduceFlags(node);

            context.Emission.Append(node, $"/{node.Value}/");

            if((metaFlags & MetaFlag.GlobalSearch) == MetaFlag.GlobalSearch)
            {
                context.Emission.Append(node, "g");

                metaFlags &= ~MetaFlag.GlobalSearch;
            }

            if((metaFlags & MetaFlag.CaseInsensitive) == MetaFlag.CaseInsensitive)
            {
                context.Emission.Append(node, "i");

                metaFlags &= ~MetaFlag.CaseInsensitive;
            }

            if((metaFlags & MetaFlag.MultiLineSearch) == MetaFlag.MultiLineSearch)
            {
                context.Emission.Append(node, "m");

                metaFlags &= ~MetaFlag.MultiLineSearch;
            }

            if((metaFlags & MetaFlag.DotsAsNewLines) == MetaFlag.DotsAsNewLines)
            {
                context.Emission.Append(node, "s");

                metaFlags &= ~MetaFlag.DotsAsNewLines;
            }

            if((metaFlags & MetaFlag.UnicodeCodePoints) == MetaFlag.UnicodeCodePoints)
            {
                context.Emission.Append(node, "u");

                metaFlags &= ~MetaFlag.UnicodeCodePoints;
            }

            if((metaFlags & MetaFlag.StickySearch) == MetaFlag.StickySearch)
            {
                context.Emission.Append(node, "y");

                metaFlags &= ~MetaFlag.StickySearch;
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~RegularExpressionConstantFlags, context, token)
            );
        
            return result;
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

            context.Emission.Append(node, " as ");
            
            result.AddMessages(
                EmitNode(node.TargetType, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitSmartCast(SmartCast node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, " is ");
            
            result.AddMessages(
                EmitNode(node.TargetType, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitSpreadDestructuring(SpreadDestructuring node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "...");
            
            return EmitNode(node.Subject, childContext, token);
        }

        public override Result<object> EmitStrictEquivalent(StrictEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "===", context, token);
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
            return EmitBinaryLike(node, "!==", context, token);
        }

        public override Result<object> EmitStringConstant(StringConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if(ASTNodeHelpers.IsMultilineString(node))
            {
                context.Emission.Append(node, $"`{node.Value}`");
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
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitTupleTypeReference(TupleTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitTypeAliasDeclaration(TypeAliasDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            // context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));
            
            context.Emission.Append(node, "type ");

            result.AddMessages(
                EmitNode(node.From, childContext, token)
            );

            result.AddMessages(
                EmitTypeDeclarationTemplate(node, childContext, token)
            );

            context.Emission.Append(node, "=");

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitTypeInterrogation(TypeInterrogation node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;
            
            context.Emission.AppendBelow(node, "typeof ");

            return EmitNode(node.Subject, childContext, token);
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
                context.Emission.Append(node, " ");

                result.AddMessages(EmitDelimited(constraints, " ", childContext, token));
            }

            if (@default != null)
            {
                context.Emission.Append(node, "=");

                result.AddMessages(EmitNode(@default, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitTypeQuery(TypeQuery node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;
            
            context.Emission.AppendBelow(node, "typeof ");

            return EmitNode(node.Subject, childContext, token);
        }

        public override Result<object> EmitTypeTest(TypeTest node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, " instanceof ");
            
            result.AddMessages(
                EmitNode(node.Criteria, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitUpperBoundedTypeConstraint(UpperBoundedTypeConstraint node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, "extends ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitUnionTypeReference(UnionTypeReference node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            return EmitDelimited(node.Types, "|", context, token);
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
            // // childContext.Parent = node;

            context.Emission.AppendBelow(node, "while(");

            result.AddMessages(EmitNode(node.Condition, childContext, token));

            context.Emission.Append(node, ")");

            result.AddMessages(EmitBlockLike(node.Body, node.Node, childContext, token));

            return result;
        }

        public override Result<object> EmitWildcardExportReference(WildcardExportReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.AppendBelow(node, "*");

            return result;
        }

        private Result<object> EmitBinaryLike(BinaryLike node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var operands = node.Operands;

            result.AddMessages(EmitDelimited(operands, operatorToken, childContext, token));

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
        private Result<object> EmitTypeDeclarationHeritage(TypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var supers = node.Supers;

            if(supers.Length > 0)
            {
                context.Emission.Append(node, " extends ");

                result.AddMessages(
                    EmitCSV(supers, childContext, token)
                );
            }
            
            var interfaces = node.Interfaces;

            if(interfaces.Length > 0)
            {
                context.Emission.Append(node, " implements ");

                result.AddMessages(
                    EmitCSV(interfaces, childContext, token)
                );
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<object> EmitTypeDeclarationTemplate<T>(T typeDecl, Context context, CancellationToken token) where T : ASTNode, ITemplated
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

        // [dho] COPYPASTA from SwiftEmitter - 09/11/18
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<object> EmitTypeDeclarationMembers(TypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var members = node.Members;

            if (members.Length > 0)
            {
                context.Emission.Indent();

                foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(members))
                {
                    context.Emission.AppendBelow(node, "");

                    result.AddMessages(
                        EmitNode(member, context, token)
                    );
                }

                context.Emission.Outdent();
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

                    if(RequiresSemicolonSentinel(member))
                    {
                        context.Emission.Append(member, ";");
                    }
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
                context.Emission.Append(type, " : ");

                result.AddMessages(
                    EmitNode(type, context, token)
                );
            }

            return result;
        }

        #endregion

        #region meta

        const MetaFlag TypeDeclarationVisibilityFlags = MetaFlag.FileVisibility | MetaFlag.SubtypeVisibility | MetaFlag.TypeVisibility | MetaFlag.WorldVisibility;

        private Result<object> EmitTypeDeclarationVisibilityFlags(TypeDeclaration node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if((flags & MetaFlag.SubtypeVisibility) == MetaFlag.SubtypeVisibility)
            {
                context.Emission.Append(node, "protected ");
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

        const MetaFlag NamespaceDeclarationFlags = MetaFlag.WorldVisibility;

        private Result<object> EmitNamespaceDeclarationFlags(NamespaceDeclaration node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if((flags & MetaFlag.WorldVisibility) == MetaFlag.WorldVisibility)
            {
                context.Emission.Append(node, "export ");
            }

            return result;
        }


        const MetaFlag TypeDeclarationMemberFlags = MetaFlag.Static | MetaFlag.SubtypeVisibility | MetaFlag.TypeVisibility | MetaFlag.WorldVisibility;

        private Result<object> EmitFlagsForTypeDeclarationMember(Declaration node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if((flags & MetaFlag.Static) == MetaFlag.Static)
            {
                context.Emission.Append(node, "static ");
            }

            if((flags & MetaFlag.SubtypeVisibility) == MetaFlag.SubtypeVisibility)
            {
                context.Emission.Append(node, "protected ");
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

        protected bool RequiresSemicolonSentinel(Node node)
        {
            switch(node.Kind)
            {
                case SemanticKind.Block:
                case SemanticKind.CodeConstant:
                case SemanticKind.Directive:
                case SemanticKind.DoWhilePredicateLoop:
                case SemanticKind.DoOrDieErrorTrap:
                case SemanticKind.DoOrRecoverErrorTrap:
                case SemanticKind.EnumerationTypeDeclaration:
                case SemanticKind.ErrorTrapJunction:
                case SemanticKind.ForKeysLoop:
                case SemanticKind.ForMembersLoop:
                case SemanticKind.ForPredicateLoop:
                case SemanticKind.FunctionDeclaration:
                case SemanticKind.InterfaceDeclaration:
                case SemanticKind.MatchJunction:
                case SemanticKind.MethodDeclaration:
                case SemanticKind.NamespaceDeclaration:
                case SemanticKind.ObjectTypeDeclaration:
                case SemanticKind.PredicateJunction:
                case SemanticKind.WhilePredicateLoop:
                    return false;

                default:
                    return true;
            }
        }

        private Result<object> EmitImportOrExportClauses(ImportOrExportDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            // [dho] FIX for the code just assuming all the clauses were `ReferenceAliasDeclarations`... 
            // why I originally wrote that, I don't know... - 01/06/19
            if(node.Clauses.Length == 1 && node.Clauses[0].Kind != SemanticKind.ReferenceAliasDeclaration)
            {
                return EmitNode(node.Clauses[0], context, token);
            }

            List<ReferenceAliasDeclaration> symbolAliases = new List<ReferenceAliasDeclaration>();

            bool hasPreviousClause = false;

            foreach(var (m, hasNext) in ASTNodeHelpers.IterateMembers(node.Clauses))
            {
                var alias = ASTNodeFactory.ReferenceAliasDeclaration(context.AST, m);

                var from = alias.From;

                if(from != null)
                {
                    if(from.Kind == SemanticKind.DefaultExportReference || from.Kind == SemanticKind.WildcardExportReference)
                    {
                        if(hasPreviousClause)
                        {
                            context.Emission.Append(m, ",");
                        }

                        result.AddMessages(
                            EmitImportOrExportAliasDeclaration(alias, context, token)
                        );

                        hasPreviousClause = true;
                    }
                    else
                    {
                        symbolAliases.Add(alias);
                    }
                }

            }

            if(symbolAliases.Count > 0)
            {
                if(hasPreviousClause)
                {
                    context.Emission.Append(node.Node, ",");
                }

                context.Emission.Append(node, "{");

                for(var i = 0 ; i < symbolAliases.Count; ++i)
                {
                    result.AddMessages(
                       EmitImportOrExportAliasDeclaration(symbolAliases[i], context, token)
                    );

                    if(i < symbolAliases.Count - 1)
                    {
                        context.Emission.Append(node, ",");
                    }
                }

                context.Emission.Append(node, "}");
            }

            return result;
        }

        private Result<object> EmitImportOrExportAliasDeclaration(ReferenceAliasDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var from = node.From;

            if(from.Kind != SemanticKind.DefaultExportReference)
            {
                result.AddMessages(
                    EmitNode(from, childContext, token)
                );

                context.Emission.Append(node, " as ");
            }

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );
    
            return result;
        }

        private Result<object> EmitDestructuredMemberName(Node node, Context context, CancellationToken token)
        {
            if(node.Kind == SemanticKind.ReferenceAliasDeclaration)
            {
                var result = new Result<object>();
                
                var alias = ASTNodeFactory.ReferenceAliasDeclaration(context.AST, node);

                var childContext = ContextHelpers.Clone(context);
                // // childContext.Parent = node;

                result.AddMessages(
                    EmitNode(alias.From, childContext, token)
                );

                context.Emission.Append(node, ":");

                result.AddMessages(
                    EmitNode(alias.Name, childContext, token)
                );

                return result;
            }
            else
            {
                return EmitNode(node, context, token);
            }
        }


        private Result<object> EmitPostUnaryArithmetic(UnaryArithmetic node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Operand, childContext, token)
            );

            context.Emission.Append(node, operatorToken);

            return result;
        }

        private Result<object> EmitPreUnaryArithmetic(UnaryArithmetic node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, operatorToken);

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Operand, childContext, token)
            );

            return result;
        }

        private Result<object> EmitCSV(Node[] nodes, Context context, CancellationToken token)
        {
            return EmitDelimited(nodes, ",", context, token);
        }
    }
}