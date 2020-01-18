using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Sempiler.Emission;
using System.Threading.Tasks;
using Sempiler.Diagnostics;
using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using Sempiler.Languages;

namespace Sempiler.CTExec
{
    using static CTExecHelpers;

    public class CTExecEmitter : TypeScriptEmitter
    {
        private readonly BaseLanguageSemantics LanguageSemantics;
        private readonly string ShardRootComponentNodeID;
        private readonly bool InvokeShardRootComponent;

        private readonly TypeScriptEmitter VanillaEmitter;

        public CTExecEmitter(BaseLanguageSemantics languageSemantics) : this(null, false, languageSemantics)
        {
        }
        public CTExecEmitter(string shardRootComponentNodeID, bool invokeShardRootComponent, BaseLanguageSemantics languageSemantics)
        {
            ShardRootComponentNodeID = shardRootComponentNodeID;
            InvokeShardRootComponent = invokeShardRootComponent;
            LanguageSemantics = languageSemantics;
            VanillaEmitter = new TypeScriptEmitter();

            FileExtension = ".js";
        }

        public override Result<object> EmitNode(Node node, Context context, CancellationToken token)
        {
            if(
                node.Kind != SemanticKind.Directive && 
                MetaHelpers.HasFlags(context.AST, node, MetaFlag.CTExec)
            )
            {
                // [dho] any tree marked as CT Exec is just emitted entirely without
                // any special filtering of nodes or treatment - 28/11/19
                return VanillaEmitter.EmitNode(node, context, token);
            }
            // else //if(node.Kind == SemanticKind.Domain || node.Kind == SemanticKind.Component)
            // {
                return base.EmitNode(node, context, token);
            // }
            // else
            // {
            //     return __IgnoreNode(node);
            // }
            
            // switch(node.Kind)
            // {
            //     case SemanticKind.ArrayConstruction:
            //     case SemanticKind.Block:
            //     case SemanticKind.CompilerHint:
            //     case SemanticKind.Component:
            //     case SemanticKind.ConstructorDeclaration:
            //     case SemanticKind.ConstructorSignature:
            //     case SemanticKind.DataValueDeclaration:
            //     case SemanticKind.Directive:
            //     case SemanticKind.Domain:
            //     case SemanticKind.FieldDeclaration:
            //     case SemanticKind.FieldSignature:
            //     case SemanticKind.ForcedCast:
            //     case SemanticKind.FunctionDeclaration:
            //     case SemanticKind.Identifier:
            //     case SemanticKind.ImportDeclaration:
            //     case SemanticKind.InterfaceDeclaration:
            //     case SemanticKind.MethodDeclaration:
            //     case SemanticKind.MethodSignature:
            //     case SemanticKind.ObjectTypeDeclaration:
            //     case SemanticKind.ParameterDeclaration:
            //     case SemanticKind.PropertyDeclaration:
            //     case SemanticKind.SafeCast:
            //     case SemanticKind.TypeAliasDeclaration:
            //         break;

            //     default: {
            //         var ast = context.AST;
        
            //         var pos = ASTHelpers.GetPosition(ast, node.ID);

            //         if (pos.Role == SemanticRole.Type ||
            //             pos.Role == SemanticRole.Template ||
            //             pos.Role == SemanticRole.Interface)
            //         {
            //             return __IgnoreNode(node);
            //         }

            //         var artifact = context.Artifact;
            //         var session = context.Session;

            //         var encScope = LanguageSemantics.GetEnclosingScopeStart(ast, node, token);

            //         System.Diagnostics.Debug.Assert(encScope != null);
            //         System.Console.WriteLine("🌈🌈 CTExec EMITTER IsCTComputable", node.ID);
            //         if(!LanguageSemantics.IsCTComputable(session, artifact, ast, node, token))
            //         {
            //             if (!LanguageSemantics.IsFunctionLikeDeclarationStatement(ast, encScope))
            //             {
            //                 if (LanguageSemantics.IsValueExpression(ast, node))
            //                 {
            //                     // [dho] we strip out any dynamism in the compile time program - 11/07/19
            //                     context.Emission.Append(node, "void 0");

            //                     return new Result<object>();
            //                 }
            //                 else
            //                 {
            //                     return __IgnoreNode(node);
            //                 }
            //             }
            //         }
            //     }
            //     break;
            // }

            // return base.EmitNode(node, context, token);
        }

        public override Result<object> EmitComponent(Component node, Context context, CancellationToken token)
        {
            var file = new FileEmission(
                FileSystem.ParseFileLocation(
                    RelativeComponentOutFilePath(context.Session, context.Artifact, context.Shard, node)
                )
            );

            context.OutFileCollection[file.Destination] = file;

            // var libIncludes = CreateAwaitIncludes(context.Session.CTExecInfo.AbsoluteLibPaths, LibIncludeArgs);

            string preamble = string.Empty;
            string postamble = string.Empty;

            System.Diagnostics.Debug.Assert(node.ID != null);

            if(node.ID == ShardRootComponentNodeID)
            {
                if(InvokeShardRootComponent)
                {
                    preamble = "module.exports = (async () => {";
                    postamble = "})()";
                }
                else
                {
                    preamble =  "module.exports = async () => {";
                    postamble = "}";
                }
            }
            else
            {
                preamble =  $@"
module.exports = async ({ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}) => {{ 

    console.log('{file.Destination.ToPathString()} >>>>>>> {ArtifactNameSymbolLexeme} is ' + {ArtifactNameSymbolLexeme} + ', {ShardIndexSymbolLexeme} is ' + {ShardIndexSymbolLexeme});
    {/*libIncludes*/""}

";

                postamble = "}";
            }

            file.Append(node, preamble);

            var nodeIDsToDisableList = new List<string>();

            foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(context.AST, node))
            {
                if(child.Kind == SemanticKind.FunctionDeclaration || 
                    child.Kind == SemanticKind.ObjectTypeDeclaration)
                {
                    continue;
                }

                var isCTExec = MetaHelpers.HasFlags(context.AST, child, MetaFlag.CTExec);
            
                if(isCTExec)
                {
                    continue;
                }
                
                nodeIDsToDisableList.Add(child.ID);
            }

            ASTHelpers.DisableNodes(context.AST, nodeIDsToDisableList.ToArray());




            var result = base.EmitComponent(node, context, token);

            var c = ASTHelpers.QueryLiveEdgeNodes(context.AST, node.ID, SemanticRole.None);

            file.Append(node, postamble);
            
            return result;
        }
    
        public override Result<object> EmitArrayConstruction(ArrayConstruction node, Context context, CancellationToken token)
        {
            return __EmitWithoutTypeInfo(node, context, base.EmitArrayConstruction, token);
        }

        public override Result<object> EmitDataValueDeclaration(DataValueDeclaration node, Context context, CancellationToken token)
        {
            return __EmitWithoutTypeInfo(node, context, base.EmitDataValueDeclaration, token);
        }

        public override Result<object> EmitDirective(Directive node, Context context, CancellationToken token)
        {
            if(LanguageSemantics.IsValueExpression(context.AST, node.Node))
            {
                context.Emission.Append(node, "void 0");
                
                return new Result<object>();
            }
            else if(LanguageSemantics.IsFunctionLikeDeclarationStatement(context.AST, node.Parent))
            {
                context.Emission.Append(node, "{}");
                
                return new Result<object>();
            }

            return __IgnoreNode(node.Node);
        }

        public override Result<object> EmitPropertyDeclaration(PropertyDeclaration node, Context context, CancellationToken token)
        {
            return __IgnoreNode(node.Node);
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
            return __IgnoreNode(node.Node);
        }

        public override Result<object> EmitInvocation(Invocation node, Context context, CancellationToken token)
        {
            // [dho] get rid of generics - 04/12/19
            return __EmitWithoutTypeInfo(node, context, base.EmitInvocation, token);
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
            return __IgnoreNode(node.Node);
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
            return __IgnoreNode(node.Node);
        }

        public override Result<object> EmitCompilerHint(CompilerHint node, Context context, CancellationToken token)
        {
            return __IgnoreNode(node.Node);
        }

        public override Result<object> EmitForcedCast(ForcedCast node, Context context, CancellationToken token)
        {
            return EmitNode(node.Subject, context, token);
        }

        public override Result<object> EmitExportDeclaration(ExportDeclaration node, Context context, CancellationToken token)
        {
            return __IgnoreNode(node.Node);
        }

        public override Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
        {
            return __IgnoreNode(node.Node);
            // var result = new Result<object>();

            // var importDescriptor = result.AddMessages(
            //     Core.ImportHelpers.ParseImportDescriptor(node, token)
            // );

            // if(HasErrors(result) || token.IsCancellationRequested) return result;

            // // [dho] if the import is for another component then we emit that - 29/11/19
            // if(importDescriptor.Type == Core.ImportHelpers.ImportType.Component)
            // {
            //     //
            //     //
            //     // [dho] NOTE we were going to resolve the component first, but why bother..
            //     // node will error if it cannot be resolved anyway, so let's not do unnecessary
            //     // work! - 29/11/19
            //     //
            //     //

            //     // var resolvedComponent = result.AddMessages(
            //     //     Core.ImportHelpers.ResolveComponentImport(
            //     //         context.Session, context.Artifact, context.AST, 
            //     //         importDescriptor, context.Component, token
            //     //     )
            //     // );

            //     // if(HasErrors(result) || token.IsCancellationRequested) return result;

            //     // var relPath = RelativeComponentOutFilePath(context.Session, resolvedComponent);

            //     // context.Emission.AppendBelow(node, $"require('{relPath}');");

            //     context.Emission.AppendBelow(node, $"require('{importDescriptor.SpecifierLexeme}');");
            // }

            // return result;
        }

        public override Result<object> EmitInterfaceDeclaration(InterfaceDeclaration node, Context context, CancellationToken token)
        {
            return __IgnoreNode(node.Node);
        }

        public override Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
        {
            // [dho] the thinking here is that extensions can only contain instance methods
            // anyway, and since we do not allow instantiation at compile time, there is no need
            // to support extensions... which is lucky because JS does not support them anyway! - 29/11/19
            if(MetaHelpers.HasFlags(node, MetaFlag.ExtensionType))
            {
                return __IgnoreNode(node.Node);
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
            return __IgnoreNode(node.Node);
        }


        public override Result<object> EmitNullCoalescence(NullCoalescence node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, CTInSituFnIDs.NullCoalescencePolyfill + "(");

            result.AddMessages(EmitDelimited(node.Operands, ",", childContext, token));

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitMaybeNull(MaybeNull node, Context context, CancellationToken token)
        {
            return __EmitPolyfillInvocation(CTInSituFnIDs.MaybeNullPolyfill, node, node.Subject, context, token);
        }

        public override Result<object> EmitNotNull(NotNull node, Context context, CancellationToken token)
        {
            return __EmitPolyfillInvocation(CTInSituFnIDs.NotNullPolyfill, node, node.Subject, context, token);
        }

        private Result<Object> __EmitPolyfillInvocation(string polyfillName, ASTNode node, Node subject, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // // childContext.Parent = node;

            context.Emission.Append(node, polyfillName + "(");

            result.AddMessages(
                EmitNode(subject, childContext, token)
            );

            context.Emission.Append(node, ")");

            return result;
        }



        public static string CreateAwaitIncludes(IEnumerable<string> sources, string[] args)
        {
            var argsString = string.Join(",", args);

            return "await (require('" + string.Join($"')({argsString})); await (require('", sources) + $"')({argsString}));";
        }

        public static string CreateMessageIDCode()
        {
            return $"`${{{ArtifactNameSymbolLexeme}}}_${{{ShardIndexSymbolLexeme}}}_{CompilerHelpers.NextInternalGUID()}`";
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
        private Result<object> __IgnoreNode(AST.Node node)
        {
            return new Result<object>();
        }

        private delegate Result<object> EmitDelegate<T>(T node, Context context, CancellationToken token);

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
                return __IgnoreNode(node.Node);
            }
        }
    }
}