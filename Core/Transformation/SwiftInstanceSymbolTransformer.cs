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


    // [dho] transformer that qualifies the use of instance symbols inside nested functional contexts,
    // eg given a field called 'foo', it will change all references to 'foo' inside nested lambdas/functions
    // to 'self.foo' - 03/10/19
    public class SwiftInstanceSymbolTransformer : ITransformer
    {
        protected readonly string[] DiagnosticTags;

        public SwiftInstanceSymbolTransformer()
        {
            DiagnosticTags = new[] { "transformer", "swift-instance-symbol-transformer" };
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

        private Result<object> TransformNode(Session session, Node start, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var ast = context.AST;

            ASTHelpers.PreOrderTraversal(session, ast, start, node =>
            {
                if(node.Kind == SemanticKind.ObjectTypeDeclaration)
                {
                    var objectTypeDecl = ASTNodeFactory.ObjectTypeDeclaration(ast, node);

                    result.AddMessages(
                        QualifyReferencesToObjectTypeDeclarationInstanceSymbols(session, objectTypeDecl, context, token)
                    );
                }

                return true;
            }, token);


            return result;
        }

        private Result<object> QualifyReferencesToObjectTypeDeclarationInstanceSymbols(Session session, ObjectTypeDeclaration objectTypeDecl, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var ast = context.AST;

            var instanceMembers = new List<Node>();

            foreach(var m in objectTypeDecl.Members)
            {
                if((ASTNodeHelpers.GetMetaFlags(context.AST, m.ID) & MetaFlag.Static) == 0)
                {   
                    instanceMembers.Add(m);
                }
            }

            ASTHelpers.PreOrderTraversal(session, ast, objectTypeDecl.Node, node =>
            {
                // [dho] nested function context - 03/10/19
                if(node.Kind == SemanticKind.LambdaDeclaration || node.Kind == SemanticKind.FunctionDeclaration)
                {
                    var body = ASTHelpers.GetSingleMatch(ast, node.ID, SemanticRole.Body);

                    if(body != null)
                    {
                        QualifyReferencesToInstanceSymbols(session, ast, body, instanceMembers, false, token);
                    }

                    // return false;
                }

                return true;
            }, token);


            return result;
        }

        public static void QualifyReferencesToInstanceSymbols(Session session, RawAST ast, Node start, IEnumerable<Node> nodes, bool assumeBindings, CancellationToken token)
        {
            foreach(var node in nodes)
            {
                var name = ASTHelpers.GetSingleMatch(ast, node.ID, SemanticRole.Name);

                if(name == null)
                {
                    continue;
                }

                System.Diagnostics.Debug.Assert(name.Kind == SemanticKind.Identifier);

                var rawNameLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;

                var bodyScope = new Scope(start);

                bodyScope.Declarations[rawNameLexeme] = name;

                QualifyReferencesToInstanceSymbol(session, ast, bodyScope, start, rawNameLexeme, token);

                // [dho] incase it is used as a binding - 30/06/19
                // [dho] TODO CLEANUP having to do this..? - 30/06/19
                if(assumeBindings)
                {
                    bodyScope.Declarations[$"${rawNameLexeme}"] = name;

                    QualifyReferencesToInstanceSymbol(session, ast, bodyScope, start, $"${rawNameLexeme}", token);
                }
            }
        }

        private static void QualifyReferencesToInstanceSymbol(Session session, RawAST ast, Scope scope, Node start, string name, CancellationToken token)
        {
            var references = LanguageSemantics.Swift.GetUnqualifiedReferenceMatches(session, ast, start, scope, name, token);

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
    }
}