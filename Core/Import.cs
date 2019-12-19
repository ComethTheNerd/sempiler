using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;
using Sempiler.Emission;
using Sempiler.Languages;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.Diagnostics;
using Sempiler.AST.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;

namespace Sempiler.Core
{
    public static class CompilerPackageSymbols
    {
        public const string Column = "Column";
        public const string Row = "Row";
        public const string View = "View";

        public static bool IsCompilerPackageSymbolName(string input)
        {
            switch (input)
            {
                case Column:
                case Row:
                case View:
                    return true;

                default:
                    return false;
            }
        }
    }

    public static class ImportHelpers
    {
        public enum ImportType
        {
            Unknown = 0,
            Compiler = 1,
            Platform = 2,
            Component = 3
        }

        public class ImportDescriptor
        {
            public readonly ImportType Type;
            public readonly ImportDeclaration Declaration;
            public readonly string SpecifierLexeme;

            public ImportDescriptor(ImportDeclaration decl, string specifier)
            {
                Declaration = decl;
                SpecifierLexeme = specifier;

                Type = specifier == "sempiler" ? (
                    ImportType.Compiler
                ) : (
                    specifier.StartsWith(".") ? (
                        ImportType.Component
                    ) : (
                        ImportType.Platform
                    )
                );
            }
        }

        public class ImportInfo
        {
            public readonly ImportDescriptor Descriptor;
            public ImportType Type { get => Descriptor.Type; }
            public string SpecifierLexeme { get => Descriptor.SpecifierLexeme; }
            public ImportDeclaration Declaration { get => Descriptor.Declaration; }

            public readonly Dictionary<string, Node> Clauses;

            // import * as x from "..."
            // will have [x] = all the nodes that reference x
            public readonly Dictionary<string, List<Node>> WildcardReferences;

            // import x from "..."
            // will have [x] = all the nodes that reference x
            public readonly Dictionary<string, List<Node>> DefaultReferences;

            // import { x } from "..."
            // will have [x] = all the nodes that reference x
            public readonly Dictionary<string, List<Node>> SymbolReferences;

            public ImportInfo(ImportDescriptor descriptor)
            {
                Descriptor = descriptor;
                Clauses = new Dictionary<string, Node>();
                WildcardReferences = new Dictionary<string, List<Node>>();
                DefaultReferences = new Dictionary<string, List<Node>>();
                SymbolReferences = new Dictionary<string, List<Node>>();
            }
        }

        public static Result<ImportDescriptor> ParseImportDescriptor(ImportDeclaration importDecl, CancellationToken token)
        {
            var result = new Result<ImportDescriptor>();

            var specifier = importDecl.Specifier;

            if (specifier.Kind == SemanticKind.StringConstant)
            {
                var ast = importDecl.AST;

                var specifierLexeme = ASTNodeFactory.StringConstant(ast, (DataNode<string>)specifier).Value;

                var importDescriptor = new ImportDescriptor(importDecl, specifierLexeme);

                result.Value = importDescriptor;
            }
            else
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Expected import specifier to be a string constant but found '{specifier.Kind}'", specifier)
                    {
                        Hint = GetHint(specifier.Origin)
                    }
                );
            }


            return result;
        }


        public static Result<ImportInfo> GetImportClauseReferences(Session session, ImportDeclaration importDecl, BaseLanguageSemantics languageSemantics, CancellationToken token)
        {
            var result = new Result<ImportInfo>();

            var importDescriptor = result.AddMessages(
                ParseImportDescriptor(importDecl, token)
            );

            if (HasErrors(result) || token.IsCancellationRequested) return result;

            var ast = importDecl.AST;

            var importInfo = new ImportInfo(importDescriptor);

            var encScopeStartNode = languageSemantics.GetEnclosingScopeStart(ast, importDecl.Node, token);

            foreach (var importClause in importDecl.Clauses)
            {
                if (importClause.Kind == SemanticKind.ReferenceAliasDeclaration)
                {
                    var refAlias = ASTNodeFactory.ReferenceAliasDeclaration(ast, importClause);

                    var to = refAlias.Name;
                    System.Diagnostics.Debug.Assert(to.Kind == SemanticKind.Identifier);


                    var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)to).Lexeme;

                    importInfo.Clauses[lexeme] = importClause;

                    var startScope = new Scope(encScopeStartNode);

                    startScope.Declarations[lexeme] = importClause;

                    var from = refAlias.From;

                    var references = languageSemantics.GetUnqualifiedReferenceMatches(session, ast, startScope.Subject, startScope, lexeme, token);

                    if (from.Kind == SemanticKind.WildcardExportReference)
                    {
                        // import * as x from "...";
                        importInfo.WildcardReferences[lexeme] = references;
                    }
                    else if (from.Kind == SemanticKind.DefaultExportReference)
                    {
                        // import x from "...";
                        importInfo.DefaultReferences[lexeme] = references;
                    }
                    else
                    {
                        // import { X as Y } from "...";
                        importInfo.SymbolReferences[lexeme] = references;
                    }
                }
                else if (importClause.Kind == SemanticKind.Identifier)
                {
                    var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)importClause).Lexeme;

                    var startScope = new Scope(encScopeStartNode);

                    startScope.Declarations[lexeme] = importClause;

                    importInfo.Clauses[lexeme] = importClause;

                    importInfo.SymbolReferences[lexeme] = languageSemantics.GetUnqualifiedReferenceMatches(session, ast, startScope.Subject, startScope, lexeme, token);
                }
            }

            result.Value = importInfo;


            return result;
        }


        public class ImportsSortedByType
        {
            public List<ComponentImport> ComponentImports = new List<ComponentImport>();
            public List<PlatformImport> PlatformImports = new List<PlatformImport>();
            public List<SempilerImport> SempilerImports = new List<SempilerImport>();
        }

        public class BaseImport
        {
            public ImportDeclaration ImportDeclaration;
            public ImportInfo ImportInfo;
        }

        public class ComponentImport : BaseImport
        {
            public Component Component;
        }

        public class PlatformImport : BaseImport
        {
        }

        public class SempilerImport : BaseImport
        {
        }

        public static Result<ImportsSortedByType> SortImportDeclarationsByType(Session session, Artifact artifact, RawAST ast, Component component, List<Node> importDecls, BaseLanguageSemantics languageSemantics, CancellationToken token)
        {
            var result = new Result<ImportsSortedByType>();

            var importsSortedByType = new ImportsSortedByType();

            foreach (var im in importDecls)
            {
                var importDecl = ASTNodeFactory.ImportDeclaration(ast, im);

                // [dho] we need to refactor the symbols for imports because we are inlining the whole
                // program - 23/06/19
                var importInfo = result.AddMessages(
                    ImportHelpers.GetImportClauseReferences(session, importDecl, languageSemantics, token));

                // [dho] something went wrong with this import declaration - 23/06/19
                if (importInfo == null) continue;

                if (importInfo.Type == ImportType.Compiler)
                {
                    if (importInfo.DefaultReferences.Count > 0)
                    {
                        result.AddMessages(
                            new NodeMessage(MessageKind.Error, $"Package '{importInfo.SpecifierLexeme}' has no default export", importDecl)
                            {
                                Hint = GetHint(importDecl.Origin)
                            }
                        );
                    }

                    if (importInfo.WildcardReferences.Count > 0)
                    {
                        result.AddMessages(
                            new NodeMessage(MessageKind.Error, $"Package '{importInfo.SpecifierLexeme}' does not support wildcard importing", importDecl)
                            {
                                Hint = GetHint(importDecl.Origin)
                            }
                        );
                    }

                    foreach (var kv in importInfo.SymbolReferences)
                    {
                        var symbol = kv.Key;
                        var references = kv.Value;

                        var isSempilerSymbol = CompilerPackageSymbols.IsCompilerPackageSymbolName(symbol);

                        if (!isSempilerSymbol)
                        {
                            var clause = importInfo.Clauses[symbol];

                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"Symbol '{symbol}' does not exist in package '{importInfo.SpecifierLexeme}'", clause)
                                {
                                    Hint = GetHint(clause.Origin)
                                }
                            );
                        }
                    }

                    importsSortedByType.SempilerImports.Add(new SempilerImport
                    {
                        ImportDeclaration = importDecl,
                        ImportInfo = importInfo
                    });
                }
                // [dho] relative file path - 23/06/19
                else if (importInfo.Type == ImportType.Component)
                {
                    var matchedComponent = result.AddMessages(
                        ResolveComponentImport(session, artifact, ast, importInfo.Descriptor, component, token)
                    );

                    // [dho] NOTE resolver will report error if no component was resolved - 29/11/19
                    if (!HasErrors(result) && !token.IsCancellationRequested)
                    {
                        importsSortedByType.ComponentImports.Add(new ComponentImport
                        {
                            ImportDeclaration = importDecl,
                            ImportInfo = importInfo,
                            Component = matchedComponent
                        });
                    }
                }
                else if(importInfo.Type == ImportType.Platform)
                {
                    importsSortedByType.PlatformImports.Add(new PlatformImport
                    {
                        ImportDeclaration = importDecl,
                        ImportInfo = importInfo
                    });
                }
                else
                {
                    // [dho] should this be a compiler assertion error instead? - 29/11/19
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Import has unsupported type '{importInfo.Type}'", importDecl)
                        {
                            Hint = GetHint(importDecl.Origin)
                        }
                    );
                }
            }

            result.Value = importsSortedByType;

            return result;
        }


        public static Result<Component> ResolveComponentImport(Session session, Artifact artifact, RawAST ast, ImportDescriptor importDescriptor, Component component, CancellationToken token)
        {
            var result = new Result<Component>();

            var componentPath = component.Name;

            var absImportSpecifier = FileSystem.Resolve(
                System.IO.Directory.GetParent(componentPath).ToString(),
                importDescriptor.SpecifierLexeme
            );

            var importSpecifierHasFileExt = System.IO.Path.GetExtension(absImportSpecifier).Length > 0;


            var root = ASTHelpers.GetRoot(ast);
            System.Diagnostics.Debug.Assert(root.Kind == SemanticKind.Domain);

            var domain = ASTNodeFactory.Domain(ast, root);

            var matchedComponent = default(Component);
            var matchedName = default(string);

            foreach (var c in domain.Components)
            {
                var candidate = ASTNodeFactory.Component(ast, (DataNode<string>)c);
                var candidateName = candidate.Name;

                // [dho] strip the extension of the path because when someone writes an import
                // they do not have to specify the extension - 24/06/19
                if (!importSpecifierHasFileExt)
                {
                    var candidateFileExt = System.IO.Path.GetExtension(candidateName);

                    candidateName = candidateName.Substring(0, candidateName.Length - candidateFileExt.Length);
                }


                if (candidateName == absImportSpecifier)
                {
                    if (matchedComponent == null)
                    {
                        matchedComponent = candidate;
                        matchedName = candidateName;
                    }
                    else
                    {
                        var specifier = importDescriptor.Declaration.Specifier;

                        result.AddMessages(
                            new NodeMessage(MessageKind.Error, $"Package '{importDescriptor.SpecifierLexeme}' is ambiguous between '{matchedName} and {candidateName}'", specifier)
                            {
                                Hint = GetHint(specifier.Origin)
                            }
                        );
                    }
                }
            }

            if (matchedComponent != null)
            {
                result.Value = matchedComponent;
            }
            else
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Could not find package '{importDescriptor.SpecifierLexeme}'", importDescriptor.Declaration)
                    {
                        Hint = GetHint(importDescriptor.Declaration.Origin)
                    }
                );
            }

            return result;
        }
        public static Result<object> QualifyImportReferences(RawAST ast, BaseImport import, string importedComponentInlinedName)
        {
            var result = new Result<object>();

            var importDecl = import.ImportDeclaration;
            var importInfo = import.ImportInfo;

            if (importInfo.DefaultReferences.Count > 0)
            {
                // [dho] TODO support this.. find the default export in the component and
                // replace the reference with `{importedComponentInlinedName}.{nameOfDefaultExportInComponent}` - 24/06/19
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Package '{importInfo.SpecifierLexeme}' does not support default importing", importDecl)
                    {
                        Hint = GetHint(importDecl.Origin)
                    }
                );
            }

            foreach (var kv in importInfo.WildcardReferences)
            {
                var symbol = kv.Key;
                var references = kv.Value;

                foreach (var reference in references)
                {
                    ASTNodeHelpers.RefactorName(ast, reference, importedComponentInlinedName);
                }
            }

            foreach (var kv in importInfo.SymbolReferences)
            {
                var symbol = kv.Key;
                var references = kv.Value;

                foreach (var reference in references)
                {
                    ASTNodeHelpers.ConvertToPrefixedQualifiedAccess(ast, reference, importedComponentInlinedName);
                }
            }

            return result;
        }
    }
}