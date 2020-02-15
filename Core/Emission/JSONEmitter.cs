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
    public class JSONEmitter : BaseEmitter
    {
        public JSONEmitter() : base(new string[]{ ArtifactTargetLang.JSON, PhaseKind.Emission.ToString("g").ToLower() })
        {
            FileExtension = ".json";
        }

        public override Result<object> EmitNode(Node node, Context context, CancellationToken token)
        {
            var result = new Result<object>();
            var ast = context.AST;

            context.Emission.Append(node, "{");

            var childContext = ContextHelpers.Clone(context);

            context.Emission.Append(node, $"\"id\":\"{node.ID}\",");
            context.Emission.Append(node, $"\"kind\":\"{(SemanticKind)node.Kind}\",");
            context.Emission.Append(node, $"\"origin\":");

            result.AddMessages(EmitNodeOrigin(node, context, token));

            context.Emission.Append(node, ",");
            context.Emission.Append(node, "\"children\":[");
            foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, node.ID))
            {
                result.AddMessages(
                    EmitNode(child, childContext, token)
                );

                if(hasNext)
                {
                    context.Emission.Append(child, ",");
                }
            }
            context.Emission.Append(node, "]");
            context.Emission.Append(node, "}");

            return result;
        }

        private Result<object> EmitNodeOrigin(Node node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "{");
            context.Emission.Append(node, $"\"kind\":\"{(PhaseKind)node.Origin.Kind}\",");
            context.Emission.Append(node, $"\"lexeme\":\"{node.Origin.Lexeme}\",");
            context.Emission.Append(node, "}");

            return result;
        }
    }
}