using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Emission;
using Sempiler.Languages;
using Sempiler.Inlining;
using Sempiler.Core;

namespace Sempiler.Bundling
{
    using static BundlerHelpers;

    public class NodeJSPartialBundler
    {
        public class TypeScriptEmitter : Emission.TypeScriptEmitter
        {
            private string RelDirPath;
            public TypeScriptEmitter(string relDirPath)
            {
                RelDirPath = relDirPath;
            }

            public override string RelativeComponentOutFilePath(Session session, Artifact artifact, Shard shard, Component node)
            {
                var filenameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(node.Name);

                var filename = BundlerHelpers.IsRawSource(node) ? (
                    filenameWithoutExt + System.IO.Path.GetExtension(node.Name)
                ) : (
                    filenameWithoutExt + "$" + node.ID + FileExtension
                );

                return RelDirPath + '/' + filename;
            }
        }

        public static readonly List<Dependency> RequiredDependencies = new List<Dependency>{
            new Dependency { Name = "typescript", Version = "^3.8", PackageManager = PackageManager.NPM },
            new Dependency { Name = "@types/node", PackageManager = PackageManager.NPM }
        };

        public const string IndexFilenameWithoutExt = "app";

        const string UserCodeDirName = "src";
        public const string UserCodeSymbolicLexeme = "userCode";
        const string ExpressRouterAppFactoryFilenameWithoutExt = "express";

        public static readonly TypeScriptEmitter ImportSpecifierEmitter = new TypeScriptEmitter(".")
        {
            FileExtension = string.Empty
        };


        public Result<OutFileCollection> EmitMainAppShard(Session session, Artifact artifact, Shard shard, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            var ast = shard.AST;

            FilterNonEmptyComponents(ast);

            var entrypointComponent = default(Component);
            var routeInfos = default(List<ServerInlining.ServerRouteInfo>);

            var nodeIDsToRemove = new List<string>();


            foreach(var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, ASTHelpers.GetRoot(ast).ID))
            {
                System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

                var component = ASTNodeFactory.Component(ast, (DataNode<string>)child);

                var r = ServerInlining.GetInlinerInfo(
                    session, ast, component, LanguageSemantics.TypeScript, new string[] {}, new string[] {}, token
                );

                var inlinerInfo = result.AddMessages(r);

                if(HasErrors(r)) continue;
                else if(token.IsCancellationRequested) return result;


                if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
                {
                    entrypointComponent = component;
                    routeInfos = inlinerInfo.RouteInfos;
                }

                foreach(var im in inlinerInfo.Imports)
                {
                    var imDecl = ASTNodeFactory.ImportDeclaration(ast, im);

                    var r2 = ImportHelpers.ParseImportDescriptor(imDecl, token);

                    var imDescriptor = result.AddMessages(r2);

                    if(HasErrors(r)) continue;
                    else if(token.IsCancellationRequested) return result;

                    switch(imDescriptor.Type)
                    {
                        case ImportHelpers.ImportType.Component:
                        {
                            var importedComponent = result.AddMessages(
                                ImportHelpers.ResolveComponentImport(session, artifact, ast, imDescriptor, component, token)
                            );

                            if(importedComponent != null)
                            {
                                var newSpecifierLexeme = ImportSpecifierEmitter.RelativeComponentOutFilePath(session, artifact, shard, importedComponent);

                                ASTHelpers.Replace(ast, imDecl.Specifier.ID, new [] { 
                                    NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), newSpecifierLexeme).Node
                                });
                            }
                            else
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Error,
                                        $"Could not resolve Component import '{imDescriptor.SpecifierLexeme}'", im)
                                );
                            }
                        }
                        break;

                        case ImportHelpers.ImportType.Compiler:{
                            nodeIDsToRemove.Add(im.ID);
                        }
                        break;


                        case ImportHelpers.ImportType.Platform:
                        case ImportHelpers.ImportType.Unknown:
                        break;

                        default:{
                            System.Diagnostics.Debug.Fail(
                                $"Unhandled import type in NodeJS Bundler '{imDescriptor.Type}'"
                            );
                        }
                        break;
                    }                
                } 


            }


            System.Diagnostics.Debug.Assert(entrypointComponent != null);



            if (HasErrors(result) || token.IsCancellationRequested) return result;


            if(nodeIDsToRemove.Count > 0)
            {
                ASTHelpers.DisableNodes(ast, nodeIDsToRemove.ToArray());
            }

            var emitter = new TypeScriptEmitter(UserCodeDirName);

            var ofc = result.Value = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, shard, shard.AST, token));


            if (HasErrors(result) || token.IsCancellationRequested) return result;

            // [dho] INDEX - 25/02/20
            {
                var indexContent = new System.Text.StringBuilder();

                var userCodeImportSpecifierLexeme = ImportSpecifierEmitter.RelativeComponentOutFilePath(session, artifact, shard, entrypointComponent);

                indexContent.AppendLine("import * as " + UserCodeSymbolicLexeme + " from '" + userCodeImportSpecifierLexeme + "';");
                indexContent.AppendLine($"import $createExpressApp from './{ExpressRouterAppFactoryFilenameWithoutExt}'");
                indexContent.AppendLine($"const $expressApp = $createExpressApp({UserCodeSymbolicLexeme})");
                
                indexContent.AppendLine($"export {{ {UserCodeSymbolicLexeme} }};");
                indexContent.AppendLine($"export default $expressApp;");

                var indexRelFilePath = UserCodeDirName + "/" + IndexFilenameWithoutExt + ".ts";

                var didAddIndex = AddRawFileIfNotPresent(ofc, indexRelFilePath, indexContent.ToString());

                // [dho] guard to ensure we did add the router file to the output successfully - 25/02/20
                if(!didAddIndex)
                {
                    result.AddMessages(
                        new Message(
                            MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                            $"Could not create index because file called '{indexRelFilePath}' already exists in output"
                        )
                    );
                }
            }

            // [dho] EXPRESS ROUTER - 25/02/20
            {

                var routerCode = result.AddMessages(NodeJS.ExpressHelpers.EmitRouterCode(ast, routeInfos, token));

                if(!string.IsNullOrEmpty(routerCode))
                {
                    // [dho] make sure all the dependencies for express are added to the shard - 25/02/20
                    foreach(var dependency in NodeJS.ExpressHelpers.RequiredDependencies)
                    {
                        result.AddMessages(
                            DependencyHelpers.AddIfNotPresent(ref shard.Dependencies, dependency)
                        );
                    }

                    var routerRelFilePath = $"{UserCodeDirName}/{ExpressRouterAppFactoryFilenameWithoutExt}.ts";

                    // [dho] create the file containing all the express app router code - 25/02/20
                    var didAddRouter = AddRawFileIfNotPresent(ofc, routerRelFilePath, routerCode);

                    // [dho] guard to ensure we did add the router file to the output successfully - 25/02/20
                    if(!didAddRouter)
                    {
                        result.AddMessages(
                            new Message(
                                MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                                $"Could not create source code for express router because file called '{routerRelFilePath}' already exists in output"
                            )
                        );
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(HasErrors(result), 
                        "Expected result to have errors if router code is null or empty, but did not find any errors");
                }
            }

         

            return result;
        }
    }

}