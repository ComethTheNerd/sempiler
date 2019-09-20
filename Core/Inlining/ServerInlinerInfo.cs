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
    public static class ServerInlining 
    {
        public struct ServerInlinerInfo
        {
            public List<Node> Imports;
            public List<ServerRouteInfo> RouteInfos;
            public List<Node> Members;
        }

        public struct ServerRouteInfo 
        {
            public Node Handler;
            public string[] QualifiedHandlerName;
            public string[] APIRelPath;
        }

        public static Result<ServerInlinerInfo> GetInlinerInfo(Session session, RawAST ast, Component component, BaseLanguageSemantics languageSemantics, string[] parentAPIRelPath, string[] parentQualifiedName, CancellationToken token)
{
            var result = new Result<ServerInlinerInfo>();

            var inlinerInfo = new ServerInlinerInfo
            {
                Imports = new List<Node>(),
                RouteInfos = new List<ServerRouteInfo>(),
                Members = new List<Node>()
            };

            foreach(var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, component.ID))
            {
                if(child.Kind == SemanticKind.ImportDeclaration)
                {
                    inlinerInfo.Imports.Add(child);
                    continue;
                }
                else if(child.Kind == SemanticKind.ExportDeclaration)
                {
                    var exportDecl = ASTNodeFactory.ExportDeclaration(ast, child);

                    // [dho] check if it is an export of a handler (ie. function like declaration) - 01/06/19
                    if(exportDecl.Specifier == default(Node))
                    {
                        var clauses = exportDecl.Clauses;
                        
                        if(clauses.Length == 1)
                        {
                            var clause = clauses[0];

                            if(languageSemantics.IsFunctionLikeDeclarationStatement(ast, clause))
                            {
                                // [dho] TODO CLEANUP HACK to get function name!! - 01/06/19
                                var handlerName = ASTHelpers.GetSingleMatch(ast, clause.ID, SemanticRole.Name);
                                
                                if(handlerName?.Kind == SemanticKind.Identifier)
                                {
                                    var lexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)handlerName).Lexeme;

                                    var apiRelPath = new string[parentAPIRelPath.Length + 1];
                                    System.Array.Copy(parentAPIRelPath, apiRelPath, parentAPIRelPath.Length);
                                    apiRelPath[apiRelPath.Length - 1] = lexeme; 

                                    var qualifedHandlerName = new string[parentQualifiedName.Length + 1];
                                    System.Array.Copy(parentQualifiedName, qualifedHandlerName, parentQualifiedName.Length);
                                    qualifedHandlerName[qualifedHandlerName.Length - 1] = lexeme;

                                    inlinerInfo.RouteInfos.Add(new ServerRouteInfo
                                    {
                                        APIRelPath = apiRelPath,
                                        QualifiedHandlerName = qualifedHandlerName,
                                        Handler = child
                                    });
                                }
                                else
                                {
                                    result.AddMessages(
                                        new NodeMessage(MessageKind.Error, $"Handler must have a name unless it is the default export", child)
                                        {
                                            Hint = GetHint(child.Origin)
                                        }
                                    );
                                }
                            }
                            else
                            {
                                // [dho] TODO lambda exports?  `export default hello = () => ...` - 01/06/19
                            }
                        }
                    }
                }
                else if(child.Kind == SemanticKind.Assignment) // [dho] check for `module.exports = ...` - 01/06/19
                {
                    var assignment = ASTNodeFactory.Assignment(ast, child);

                    var storage = assignment.Storage;

                    if(storage.Kind == SemanticKind.QualifiedAccess)
                    {
                        var qa = ASTNodeFactory.QualifiedAccess(ast, storage);

                        if(ASTNodeHelpers.IsIdentifierWithName(ast, qa.Incident, "module") && 
                            ASTNodeHelpers.IsIdentifierWithName(ast, qa.Member, "exports"))
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Error, $"`module.exports` is not yet supported", child)
                                    {
                                        Hint = GetHint(child.Origin)
                                    }
                                );


                                // var value = assignment.Value;

                                // if(LanguageHelpers.TypeScriptTreatsAsFunctionLikeDeclaration(value))
                                // {
                                //     var path = default(string[]);
                                    /*
                                    var path = new string[parentPath.Length + 1];
                                    System.Array.Copy(parentPath, path, parentPath.Length);
                                    path[path.Length - 1] = ASTNodeFactory.Identifier(ast, (DataNode<string>)handlerName).Lexeme;

                                     */
                                //     // [dho] TODO CLEANUP HACK to get function name!! - 01/06/19
                                //     var handlerName = ASTHelpers.GetSingleMatch(ast, value.ID, SemanticRole.Name);
                                
                                //     if(handlerName?.Kind == SemanticKind.Identifier)
                                //     {
                                //         path = new string[] { ASTNodeFactory.Identifier(ast, (DataNode<string>)handlerName).Lexeme };
                                //     }
                                //     else 
                                //     {
                                //         path = new string[] { component.Name.Substring(component.Name.LastIndexOf("/")) };
                                //     }

                                //     inlinerInfo.Handlers.Add(new RouteInfo
                                //     {
                                    // APIRelPath = df,
                                //         Path = path,
                                //         Handler = child
                                //     });
                                // }
                                // else
                                // {
                                //     x;// error
                                // }


                                // continue;

                            }
                    }
                }
                
                // [dho] fallsthrough to here if it is not a handler function - 01/06/19
                inlinerInfo.Members.Add(child); 
            }

            result.Value = inlinerInfo;

            return result;
        }
    }
}