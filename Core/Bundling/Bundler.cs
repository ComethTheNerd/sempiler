using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;
using Sempiler.Emission;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;

namespace Sempiler.Bundling
{
    public interface IBundler
    { 
        // [dho] creates a bundle of files that can be deployed and executed in the target platform, eg. inferring and injecting
        // a manifest file where none has been explicitly provided - 21/05/19
        Task<Sempiler.Diagnostics.Result<OutFileCollection>> Bundle(Session session, Artifact artifact, List<Shard> shards, CancellationToken token);

        IList<string> GetPreservedDebugEmissionRelPaths(Session session, Artifact artifact, CancellationToken token);
    }

    public static class BundlerHelpers
    {
        public static string ProductIdentifier(Artifact artifact, Shard shard)
        {
            return (PackageIdentifier(artifact) + "." + shard.Name).ToLower();  
        }

        public static string PackageIdentifier(Artifact artifact)
        {
            return ("com." + artifact.TeamName).ToLower();  
        }

        public static Shard GetMainAppOrThrow(Session session, Artifact artifact)
        {
            foreach(var shard in session.Shards[artifact.Name])
            {
                if(shard.Role == ShardRole.MainApp)
                {
                    return shard;
                }
            }

            throw new System.Exception($"Could not find main app for artifact '{artifact.Name}'");
        }

        public static bool IsInferredSessionEntrypointComponent(Session session, Component component)
        {
            var relParentDirPath = component.Name.Replace(session.BaseDirectory.ToPathString(), "");

            foreach(var inputPath in session.InputPaths)
            {
                if(inputPath.Replace(session.BaseDirectory.ToPathString(), "") == relParentDirPath)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInferredArtifactEntrypointComponent(Session session, Artifact artifact, Component component)
        {
            var relParentDirPath = component.Name.Replace(session.BaseDirectory.ToPathString(), "");

            // [dho] is this component the entrypoint for the whole artifact - 21/06/19
            return (relParentDirPath.ToLower().IndexOf(GetNameOfExpectedArtifactEntrypointComponent(session, artifact)) == 0);
        }

        public static bool IsOutsideArtifactInferredSourceDir(Session session, Component component)
        {
            var relComponentPath = component.Name.Replace(session.BaseDirectory.ToPathString(), "");

            // [dho] check component is not inside the inferred source directory for any artifact in the session - 18/07/19
            foreach (var kv in session.Artifacts)
            {
                var artifactName = kv.Key;
                var inferredArtifactSourceDirPath = $"/{Sempiler.Core.Main.InferredConfig.SourceDirName}/{artifactName}/";

                if (relComponentPath.IndexOf(inferredArtifactSourceDirPath) > -1)
                {
                    return false;
                }
            }
            return true;
        }

        public static string GetNameOfExpectedArtifactEntrypointComponent(Session session, Artifact artifact)
        {
            return $"/{Sempiler.Core.Main.InferredConfig.SourceDirName}/{artifact.Name}/{Sempiler.Core.Main.InferredConfig.EntrypointFileName}".ToLower();
        }
        

        public static Result<List<string>> AddResourceFiles(Session session, Artifact artifact, Shard shard, OutFileCollection ofc, string relResourcesOutputPath)
        {
            var result = new Result<List<string>>();

            var relResourcePaths = new List<string>();

            foreach (var resource in shard.Resources)
            {
                switch (resource.Source.Kind)
                {
                    case SourceKind.File:
                        {

                            var sourceFile = (ISourceFile)resource.Source;

                            var srcPath = sourceFile.Location.ToPathString();

                            // var relPath = FileSystem.ParseFileLocation($@"./{artifact.Name}/{relResourcesOutputPath}{
                            //     srcPath.Replace($"{session.BaseDirectory.ToPathString()}/{Sempiler.Core.Main.InferredConfig.ResDirName}/{artifact.Name}/", "")
                            // }").ToPathString();

                            // [dho] for now we just strip off the 
                            // parent path components and just use the filename - 20/10/19 
                            var relPath = FileSystem.ParseFileLocation(
                                $@"{relResourcesOutputPath}{
                                    resource.TargetFileName ?? 
                                    (sourceFile.Location.Name + '.' + sourceFile.Location.Extension)
                                }"
                            ).ToPathString();

                            /* if(*/
                            AddCopyOfFileIfNotPresent(ofc, relPath, srcPath);//)
                                                                          // {
                            relResourcePaths.Add(relPath);
                            // }
                            // else
                            // {
                            //     result.AddMessages(
                            //         new Message(MessageKind.Warning, $"'{artifact.Name}' resource '{relPath}' could not be added because a file at the location already exists in the output file collection")
                            //     );
                            // }
                        }
                        break;

                    case SourceKind.Literal:
                        {
                            var sourceLiteral = (ISourceLiteral)resource.Source;
                            var srcPath = sourceLiteral.Location.ToPathString();

                            // var relPath = FileSystem.ParseFileLocation($@"./{artifact.Name}/{relResourcesOutputPath}{
                            //     srcPath.Replace($"{session.BaseDirectory.ToPathString()}/{Sempiler.Core.Main.InferredConfig.ResDirName}/{artifact.Name}/", "")
                            // }").ToPathString();

                            // [dho] for now we just strip off the 
                            // parent path components and just use the filename - 20/10/19 
                            var relPath = FileSystem.ParseFileLocation(
                                $@"{relResourcesOutputPath}{resource.TargetFileName ?? 
                                    (sourceLiteral.Location.Name + '.' + sourceLiteral.Location.Extension)
                                }"
                            ).ToPathString();


                            AddRawFileIfNotPresent(ofc, relPath, sourceLiteral.Text);
                            // if(AddRawFileIfMissing(ofc, relPath, ((ISourceLiteral)resource).Text))
                            // {
                            relResourcePaths.Add(relPath);
                            // }
                            // else
                            // {
                            //     result.AddMessages(
                            //         new Message(MessageKind.Warning, $"'{artifact.Name}' resource '{relPath}' could not be added because a file at the location already exists in the output file collection")
                            //     );
                            // }
                        }
                        break;

                    default:
                        {
                            result.AddMessages(
                                new Message(MessageKind.Error, $"'{artifact.Name}' resource has unsupported kind '{resource.Source.Kind}'")
                            );
                        }
                        break;
                }
            }

            result.Value = relResourcePaths;

            return result;
        }
        
        public static bool AddRawFileIfNotPresent(OutFileCollection ofc, string relPath, string content)
        {
            var location = FileSystem.ParseFileLocation(relPath);

            if (!ofc.Contains(location))
            {
                ofc[location] = new Sempiler.Emission.RawOutFileContent(System.Text.Encoding.UTF8.GetBytes(content));

                return true;
            }

            return false;
        }

        public static bool AddCopyOfFileIfNotPresent(OutFileCollection ofc, string relPath, string sourcePath)
        {
            var location = FileSystem.ParseFileLocation(relPath);

            if (!ofc.Contains(location))
            {
                ofc[location] = new Sempiler.Emission.RawOutFileContent(System.IO.File.ReadAllBytes(sourcePath));

                return true;
            }

            return false;
        }

        public static Result<MethodDeclaration> ConvertToStaticMethodDeclaration(Session session, RawAST ast, FunctionLikeDeclaration node, CancellationToken token)
        {
            var result = new Result<MethodDeclaration>();

            var decl = NodeFactory.MethodDeclaration(ast, node.Origin);

            var name = node.Name;

            if (name == null)
            {
                result.AddMessages(
                    new AST.Diagnostics.NodeMessage(MessageKind.Error, $"Function must have a name", node)
                    {
                        Hint = GetHint(node.Origin)
                    }
                );

                return result;
            }

            ASTHelpers.Connect(ast, decl.ID, new[] { name }, SemanticRole.Name);

            ASTHelpers.Connect(ast, decl.ID, node.Template, SemanticRole.Template);

            ASTHelpers.Connect(ast, decl.ID, node.Parameters, SemanticRole.Parameter);

            var type = node.Type;

            if(type != null)
            {
                ASTHelpers.Connect(ast, decl.ID, new[] { type }, SemanticRole.Type);
            }


            ASTHelpers.Connect(ast, decl.ID, new[] { node.Body }, SemanticRole.Body);

            ASTHelpers.Connect(ast, decl.ID, node.Annotations, SemanticRole.Annotation);

            ASTHelpers.Connect(ast, decl.ID, node.Modifiers, SemanticRole.Modifier);


            AddMetaFlag(session, ast, decl, MetaFlag.Static, token);

            result.Value = decl;

            return result;
        }

        public static void AddMetaFlag(Session session, RawAST ast, ASTNode node, MetaFlag newFlag, CancellationToken token)
        {
            var metaFlags = MetaHelpers.ReduceFlags(node);

            var meta = node.Meta;

            // [dho] make the method static if it is not already - 16/04/19
            if ((metaFlags & newFlag) == 0)
            {
                var f = NodeFactory.Meta(
                    ast,
                    new PhaseNodeOrigin(PhaseKind.Bundling),
                    newFlag
                );

                var m = new Node[meta.Length + 1];

                if (meta.Length > 0)
                {
                    System.Array.Copy(meta, m, meta.Length);
                }

                m[m.Length - 1] = f.Node;

                ASTHelpers.Connect(ast, node.ID, m, SemanticRole.Meta);
            }
            else
            {
                ASTHelpers.Connect(ast, node.ID, meta, SemanticRole.Meta);
            }

        }


        public static void FilterNonEmptyComponents(RawAST ast)
        {
            var nodeIDsToDisable = new List<string>();

            foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.Component))
            {
                var isEmpty = ASTHelpers.QueryLiveChildEdges(ast, node.ID).Length == 0;

                if(isEmpty)
                {
                    nodeIDsToDisable.Add(node.ID);
                }
            }

            if(nodeIDsToDisable.Count > 0)
            {
                ASTHelpers.DisableNodes(ast, nodeIDsToDisable.ToArray());
            }
        }
        
        public static void FilterSharedComponents(RawAST ast, Dictionary<string, bool> componentsProcessed)
        {
            var nodeIDsToDisable = new List<string>();

            foreach(var node in ASTHelpers.QueryByKind(ast, SemanticKind.Component))
            {
                if(componentsProcessed.ContainsKey(node.ID))
                {
                    nodeIDsToDisable.Add(node.ID);
                }
                else
                {
                    componentsProcessed[node.ID] = true;
                }
            }

            if(nodeIDsToDisable.Count > 0)
            {
                ASTHelpers.DisableNodes(ast, nodeIDsToDisable.ToArray());
            }
        }

        public static bool IsRawSource(Component node)
        {
            foreach(var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(node.AST, node))
            {
                return child.Kind == SemanticKind.CodeConstant && !hasNext;
            }

            return false;
        }

        // public static string GenerateCTID()
        // {
        //     return "_" + System.Guid.NewGuid().ToString().Replace('-', '_');
        // }
    }
}