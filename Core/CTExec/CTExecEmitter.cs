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

    public class CTExecEmitter : JavaScriptEmitter
    {
        private readonly BaseLanguageSemantics LanguageSemantics;
        private readonly string ShardRootComponentNodeID;
        private readonly bool InvokeShardRootComponent;

        public CTExecEmitter(BaseLanguageSemantics languageSemantics) : this(null, false, languageSemantics)
        {
        }
        public CTExecEmitter(string shardRootComponentNodeID, bool invokeShardRootComponent, BaseLanguageSemantics languageSemantics)
        {
            ShardRootComponentNodeID = shardRootComponentNodeID;
            InvokeShardRootComponent = invokeShardRootComponent;
            LanguageSemantics = languageSemantics;

            FileExtension = ".js";
        }

        // public override Result<object> EmitNode(Node node, Context context, CancellationToken token)
        // {
        //     if(
        //         node.Kind != SemanticKind.Directive && 
        //         MetaHelpers.HasFlags(context.AST, node, MetaFlag.CTExec)
        //     )
        //     {
        //         return ScriptEmitter.EmitNode(node, context, token);
        //     }
        //     // else //if(node.Kind == SemanticKind.Domain || node.Kind == SemanticKind.Component)
        //     // {
        //         return base.EmitNode(node, context, token);
        //     // }
        //     // else
        //     // {
        //     //     return __IgnoreNode(node);
        //     // }
            
        //     // switch(node.Kind)
        //     // {
        //     //     case SemanticKind.ArrayConstruction:
        //     //     case SemanticKind.Block:
        //     //     case SemanticKind.CompilerHint:
        //     //     case SemanticKind.Component:
        //     //     case SemanticKind.ConstructorDeclaration:
        //     //     case SemanticKind.ConstructorSignature:
        //     //     case SemanticKind.DataValueDeclaration:
        //     //     case SemanticKind.Directive:
        //     //     case SemanticKind.Domain:
        //     //     case SemanticKind.FieldDeclaration:
        //     //     case SemanticKind.FieldSignature:
        //     //     case SemanticKind.ForcedCast:
        //     //     case SemanticKind.FunctionDeclaration:
        //     //     case SemanticKind.Identifier:
        //     //     case SemanticKind.ImportDeclaration:
        //     //     case SemanticKind.InterfaceDeclaration:
        //     //     case SemanticKind.MethodDeclaration:
        //     //     case SemanticKind.MethodSignature:
        //     //     case SemanticKind.ObjectTypeDeclaration:
        //     //     case SemanticKind.ParameterDeclaration:
        //     //     case SemanticKind.PropertyDeclaration:
        //     //     case SemanticKind.SafeCast:
        //     //     case SemanticKind.TypeAliasDeclaration:
        //     //         break;

        //     //     default: {
        //     //         var ast = context.AST;
        
        //     //         var pos = ASTHelpers.GetPosition(ast, node.ID);

        //     //         if (pos.Role == SemanticRole.Type ||
        //     //             pos.Role == SemanticRole.Template ||
        //     //             pos.Role == SemanticRole.Interface)
        //     //         {
        //     //             return __IgnoreNode(node);
        //     //         }

        //     //         var artifact = context.Artifact;
        //     //         var session = context.Session;

        //     //         var encScope = LanguageSemantics.GetEnclosingScopeStart(ast, node, token);

        //     //         System.Diagnostics.Debug.Assert(encScope != null);
        //     //         System.Console.WriteLine("🌈🌈 CTExec EMITTER IsCTComputable", node.ID);
        //     //         if(!LanguageSemantics.IsCTComputable(session, artifact, ast, node, token))
        //     //         {
        //     //             if (!LanguageSemantics.IsFunctionLikeDeclarationStatement(ast, encScope))
        //     //             {
        //     //                 if (LanguageSemantics.IsValueExpression(ast, node))
        //     //                 {
        //     //                     // [dho] we strip out any dynamism in the compile time program - 11/07/19
        //     //                     context.Emission.Append(node, "void 0");

        //     //                     return new Result<object>();
        //     //                 }
        //     //                 else
        //     //                 {
        //     //                     return __IgnoreNode(node);
        //     //                 }
        //     //             }
        //     //         }
        //     //     }
        //     //     break;
        //     // }

        //     // return base.EmitNode(node, context, token);
        // }

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

            file.Append(node, postamble);
            
            return result;
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

            return IgnoreNode(node.Node);
        }

        public override Result<object> EmitExportDeclaration(ExportDeclaration node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
        }

        public override Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
        {
            return IgnoreNode(node.Node);
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

        public static string CreateMessageIDCode()
        {
            return $"`${{{ArtifactNameSymbolLexeme}}}_${{{ShardIndexSymbolLexeme}}}_{CompilerHelpers.NextInternalGUID()}`";
        }
    }
}