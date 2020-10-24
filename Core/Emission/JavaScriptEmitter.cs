using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Sempiler.Emission;
using Sempiler.Diagnostics;
using Sempiler.AST;
using Sempiler.AST.Diagnostics;

namespace Sempiler.Emission
{
    public class JavaScriptEmitter : TypeScriptEmitter
    {
        public JavaScriptEmitter() : base(new string[]{ ArtifactTargetLang.JavaScript, PhaseKind.Emission.ToString("g").ToLower() })
        {
            FileExtension = ".js";
        }

        public override Result<object> EmitArrayConstruction(ArrayConstruction node, Context context, CancellationToken token)
        {
            return __EmitWithoutTypeInfo(node, context, base.EmitArrayConstruction, token);
        }

        public override Result<object> EmitDataValueDeclaration(DataValueDeclaration node, Context context, CancellationToken token)
        {
            return __EmitWithoutTypeInfo(node, context, base.EmitDataValueDeclaration, token);
        }

        public override Result<object> EmitPropertyDeclaration(PropertyDeclaration node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
        }

        public override Result<object> EmitFieldDeclaration(FieldDeclaration node, Context context, CancellationToken token)
        {
            if(node.Parent.Kind == SemanticKind.DynamicTypeConstruction)
            {
                return __EmitWithoutTypeInfo(node, context, base.EmitFieldDeclaration, token);
            }

            return __EmitTypeDeclarationMember(node, context, base.EmitFieldDeclaration, token);
        }

        public override Result<object> EmitFieldSignature(FieldSignature node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
        }

        public override Result<object> EmitIndexedAccess(IndexedAccess node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var incident = node.Incident;

            result.AddMessages(
                EmitNode(incident, childContext, token)
            );

            context.Emission.Append(node, "[");

            result.AddMessages(
                EmitNode(node.Member, childContext, token)
            );

            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitInvocation(Invocation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            var subject = node.Subject;
            var arguments = node.Arguments;

            result.AddMessages(
                EmitNode(subject, childContext, token)
            );

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

        public override Result<object> EmitNamedTypeConstruction(NamedTypeConstruction node, Context context, CancellationToken token)
        {
            // [dho] get rid of generics - 04/12/19
            return __EmitWithoutTypeInfo(node, context, base.EmitNamedTypeConstruction, token);
        }

        public override Result<object> EmitFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
        {
            return __EmitWithoutTypeInfo(node, context, base.EmitFunctionDeclaration, token);
        }        

        public override Result<object> EmitMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
        {
            return __EmitTypeDeclarationMember(node, context, base.EmitMethodDeclaration, token);
        }

        public override Result<object> EmitMethodSignature(MethodSignature node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
        }

        public override Result<object> EmitConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
        {
            // var ast = context.AST;

            // // [dho] throw errors from constructors to let user know they can not use
            // // new at compile time - 27/11/19
            // var ctorBody = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
            // {
            //     ASTHelpers.Connect(ast, ctorBody.ID, new[] {
            //         NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "throw new Error('Cannot allocate new instances at compile time')").Node
            //     }, SemanticRole.Content);
            // }

            // var cachedBody = node.Body;

            // ASTHelpers.Replace(ast, cachedBody.ID, new [] { ctorBody.Node });

            var result = __EmitWithoutTypeInfo(node, context, base.EmitConstructorDeclaration, token);

            // ASTHelpers.Replace(ast, ctorBody.ID, new [] { cachedBody });

            return result;
        }

        public override Result<object> EmitConstructorSignature(ConstructorSignature node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
        }

        public override Result<object> EmitCompilerHint(CompilerHint node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
        }

        public override Result<object> EmitForcedCast(ForcedCast node, Context context, CancellationToken token)
        {
            return EmitNode(node.Subject, context, token);
        }

        public override Result<object> EmitInterfaceDeclaration(InterfaceDeclaration node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
        }

        public override Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
        {
            // [dho] the thinking here is that extensions can only contain instance methods
            // anyway, and since we do not allow instantiation at compile time, there is no need
            // to support extensions... which is lucky because JS does not support them anyway! - 29/11/19
            if(MetaHelpers.HasFlags(node, MetaFlag.ExtensionType))
            {
                return IgnoreNode(node.Node);
            }
            else
            {
                // [dho] NOTE we are not checking if this is a struct definition, because the difference
                // would only be notable upon instantiating the type, and we do not support that at compile
                // time anyway - 29/11/19
                return __EmitWithoutTypeInfo(node, context, base.EmitObjectTypeDeclaration, token);
            }
        }

        public override Result<object> EmitSafeCast(SafeCast node, Context context, CancellationToken token)
        {
            return EmitNode(node.Subject, context, token);
        }

        public override Result<object> EmitParameterDeclaration(ParameterDeclaration node, Context context, CancellationToken token)
        {
            return __EmitWithoutTypeInfo(node, context, base.EmitParameterDeclaration, token);
        }

        protected override Result<object> EmitTypeDeclarationHeritageInterfaces(TypeDeclaration node, Node[] interfaces, Context context, CancellationToken token)
        {
            return new Result<object>();
        }

        protected override Result<object> EmitTypeDeclarationTemplate<T>(T typeDecl, Context context, CancellationToken token)
        {
            return new Result<object>();
        }

        public override Result<object> EmitTypeAliasDeclaration(TypeAliasDeclaration node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
        }

        public static string CreateAwaitIncludes(IEnumerable<string> sources, string[] args)
        {
            var argsString = string.Join(",", args);

            return "await (require('" + string.Join($"')({argsString})); await (require('", sources) + $"')({argsString}));";
        }
 
        private Result<object> __EmitWithoutTypeInfo<T>(T node, Context context, EmitDelegate<T> emitDelegate, CancellationToken token) where T : ASTNode
        {
            var result = new Result<object>();
            var nodeIDsToDisableList = new List<string>();

            var annotations = (node as IAnnotated)?.Annotations;

            if(annotations != null)
            {
                foreach(var annotationMember in annotations)
                {
                    nodeIDsToDisableList.Add(annotationMember.ID);
                }
            }

            var type = (node as ITyped)?.Type;
            
            if(type != null)
            {
                nodeIDsToDisableList.Add(type.ID);
            }

            var template = (node as ITemplated)?.Template;
                
            if(template != null)
            {
                foreach(var templateMember in template)
                {
                    nodeIDsToDisableList.Add(templateMember.ID);
                }
            }

            var ast = node.AST;
            var meta = node.Meta;

            foreach(var (m, _) in ASTNodeHelpers.IterateMembers(meta))
            {
                var flags = ASTNodeFactory.Meta(ast, (DataNode<MetaFlag>)m).Flags;
    
                if((flags & MetaFlag.Optional) > 0)
                {
                    if(flags != MetaFlag.Optional)
                    {
                        // mixed flag
                        result.AddMessages(
                            new NodeMessage(MessageKind.Error, 
                            "Optional flag being mixed with other flags in one meta object is not supported", node)
                            {
                                Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(node.Origin),
                                Tags = DiagnosticTags
                            }
                        );
                    }

                    nodeIDsToDisableList.Add(m.ID);
                }
            }
            
            var nodeIDsToDisable = nodeIDsToDisableList.ToArray();

            ASTHelpers.DisableNodes(context.AST, nodeIDsToDisable);

            result.Value = result.AddMessages(emitDelegate(node, context, token));

            // [dho] unnecessary? we should be working on a throwaway clone
            // of the actual AST - 30/11/19
            ASTHelpers.EnableNodes(context.AST, nodeIDsToDisable);

            return result;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> __EmitTypeDeclarationMember<T>(T node, Context context, EmitDelegate<T> emitDelegate, CancellationToken token) where T : ASTNode
        {
            var result = new Result<object>();

            if(MetaHelpers.HasFlags(node, MetaFlag.Static))
            {
                return __EmitWithoutTypeInfo(node, context, emitDelegate, token);
            }
            else
            {
                return IgnoreNode(node.Node);
            }
        }
    }
}