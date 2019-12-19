using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;
using Sempiler.Emission;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.Diagnostics;
using Sempiler.AST.Diagnostics;
using Sempiler.Languages;
using static Sempiler.Diagnostics.DiagnosticsHelpers;

namespace Sempiler.Inlining
{
    public static class ClientInlining 
    {
        public struct ClientInlinerInfo
        {
            public Node Entrypoint;
            public Node EntrypointUserCode;

            public List<ExportDeclaration> ExportedSymbols;
            public List<Node> ImportDeclarations;
            public List<NamespaceDeclaration> NamespaceDeclarations;
            public List<ObjectTypeDeclaration> ObjectTypeDeclarations;
            public List<FunctionDeclaration> FunctionDeclarations;
            public List<Node> ExecOnLoads;
            public List<ViewDeclaration> ViewDeclarations;
        }

        public static Result<ClientInlinerInfo> GetInlinerInfo(Session session, RawAST ast, Node component, BaseLanguageSemantics languageSemantics, CancellationToken token)
        {
            var result = new Result<ClientInlinerInfo>();

            var inlinerInfo = new ClientInlinerInfo
            {
                Entrypoint = default(Node),
                EntrypointUserCode = default(Node),
                ExportedSymbols = new List<ExportDeclaration>(),
                ImportDeclarations = new List<Node>(),
                NamespaceDeclarations = new List<NamespaceDeclaration>(),
                ObjectTypeDeclarations = new List<ObjectTypeDeclaration>(),
                FunctionDeclarations = new List<FunctionDeclaration>(),
                ExecOnLoads = new List<Node>(),
                // ViewConstructions = new List<ViewConstruction>(),
                ViewDeclarations = new List<ViewDeclaration>()
            };

            foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, component.ID))
            {
                if (child.Kind == SemanticKind.Assignment) // [dho] check for `module.exports = ...` - 01/06/19
                {
                    var assignment = ASTNodeFactory.Assignment(ast, child);

                    if (languageSemantics.IsFunctionLikeDeclarationStatement(ast, assignment.Value))
                    {
                        var storage = assignment.Storage;

                        if (storage.Kind == SemanticKind.QualifiedAccess)
                        {
                            var qa = ASTNodeFactory.QualifiedAccess(ast, storage);

                            if (ASTNodeHelpers.IsIdentifierWithName(ast, qa.Incident, "module") &&
                                ASTNodeHelpers.IsIdentifierWithName(ast, qa.Member, "exports"))
                            {
                                if (inlinerInfo.Entrypoint == default(Node))
                                {
                                    inlinerInfo.Entrypoint = assignment.Node;
                                    inlinerInfo.EntrypointUserCode = assignment.Value;
                                    // // entrypoint found
                                    // inlinerInfo.Entrypoint = assignment.Value;
                                    // // [dho] remove the original assignment statement because we only need the function like declaration
                                    // // it is assigned to - 01/06/19
                                    // ASTHelpers.RemoveNodes(ast, assignment.ID);
                                }
                                else
                                {
                                    result.AddMessages(
                                        new NodeMessage(MessageKind.Error, $"Entrypoint is already defined", child)
                                        {
                                            Hint = GetHint(child.Origin)
                                        }
                                    );
                                }
                            }
                            else
                            {
                                inlinerInfo.ExecOnLoads.Add(child);
                            }
                        }
                        else
                        {
                            inlinerInfo.ExecOnLoads.Add(child);
                        }
                    }
                    else
                    {
                        inlinerInfo.ExecOnLoads.Add(child);
                    }
                }
                else if (child.Kind == SemanticKind.ExportDeclaration)
                {
                    var exportDecl = ASTNodeFactory.ExportDeclaration(ast, child);

                    if (inlinerInfo.Entrypoint == default(Node))
                    {
                        var clauses = exportDecl.Clauses;

                        if (clauses.Length == 1)
                        {
                            var clause = clauses[0];

                            if (clause.Kind == SemanticKind.ReferenceAliasDeclaration)
                            {
                                var refAliasDecl = ASTNodeFactory.ReferenceAliasDeclaration(ast, clause);

                                if (refAliasDecl.From.Kind == SemanticKind.DefaultExportReference)
                                {
                                    if (languageSemantics.IsFunctionLikeDeclarationStatement(ast, refAliasDecl.Name))
                                    {
                                        inlinerInfo.Entrypoint = exportDecl.Node;
                                        inlinerInfo.EntrypointUserCode = refAliasDecl.Name;
                                        // inlinerInfo.Entrypoint = refAliasDecl.To;
                                        // // [dho] remove the original export statement because we only need the function like declaration
                                        // // it is assigned to - 01/06/19
                                        // ASTHelpers.RemoveNodes(ast, child.ID);
                                    }
                                }
                                else
                                {
                                    result.AddMessages(
                                        new NodeMessage(MessageKind.Error, $"Entrypoint is already defined", child)
                                        {
                                            Hint = GetHint(child.Origin)
                                        }
                                    );
                                }
                            }
                            else
                            {
                                inlinerInfo.ExportedSymbols.Add(exportDecl);
                            }
                        }
                        else
                        {
                            inlinerInfo.ExportedSymbols.Add(exportDecl);
                        }
                    }
                    else
                    {
                        inlinerInfo.ExportedSymbols.Add(exportDecl);
                    }
                }
                else if (child.Kind == SemanticKind.ImportDeclaration)
                {
                    inlinerInfo.ImportDeclarations.Add(child);
                }
                else if (child.Kind == SemanticKind.NamespaceDeclaration)
                {
                    var namespaceDecl = ASTNodeFactory.NamespaceDeclaration(ast, child);

                    inlinerInfo.NamespaceDeclarations.Add(namespaceDecl);
                }
                else if (child.Kind == SemanticKind.ObjectTypeDeclaration)
                {
                    var objectTypeDecl = ASTNodeFactory.ObjectTypeDeclaration(ast, child);

                    inlinerInfo.ObjectTypeDeclarations.Add(objectTypeDecl);
                }
                else if (child.Kind == SemanticKind.FunctionDeclaration)
                {
                    var fnDecl = ASTNodeFactory.FunctionDeclaration(ast, child);

                    inlinerInfo.FunctionDeclarations.Add(fnDecl);
                }
                // else if(child.Kind == SemanticKind.ViewConstruction)
                // {
                //     var view = ASTNodeFactory.ViewConstruction(ast, child);

                //     inlinerInfo.ViewConstructions.Add(view);
                // }
                else if (child.Kind == SemanticKind.ViewDeclaration)
                {
                    var viewDecl = ASTNodeFactory.ViewDeclaration(ast, child);

                    inlinerInfo.ViewDeclarations.Add(viewDecl);
                }
                else
                {
                    inlinerInfo.ExecOnLoads.Add(child);
                }
            }

            result.Value = inlinerInfo;

            return result;
        }
    }
}