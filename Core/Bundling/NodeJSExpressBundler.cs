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

namespace Sempiler.Bundling.NodeJS
{
    using static BundlerHelpers;

    public class ExpressBundler : IBundler
    {
        private class ExpressBundler_FunctionsTypeScriptEmitter : TypeScriptEmitter
        {
            public override string RelativeComponentOutFilePath(Session session, Artifact artifact, Shard shard, Component node)
            {
                var filenameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(node.Name);

                var filename = BundlerHelpers.IsRawSource(node) ? (
                    filenameWithoutExt + System.IO.Path.GetExtension(node.Name)
                ) : (
                    filenameWithoutExt + "$" + node.ID + FileExtension
                );

                return UserCodeRelDirPath + '/' + filename;
            }
        }

        public static List<Dependency> RequiredDependencies = new List<Dependency>{
            // new Dependency { Name = "typescript", Version = "^3.2.4", PackageManager = PackageManager.NPM },
            // new Dependency { Name = "@types/node", PackageManager = PackageManager.NPM },
            new Dependency { Name = "express", PackageManager = PackageManager.NPM }
        };

    
        static readonly string[] DiagnosticTags = new string[] { "bundler", "nodejs/express" };

        const string UserCodeDirName = "src";
        static readonly string UserCodeRelDirPath = UserCodeDirName;
        const string EntrypointFilenameWithoutExt = "index";


        public IList<string> GetPreservedDebugEmissionRelPaths(Session session, Artifact artifact, CancellationToken token) => new string[] { "node_modules" };

        public async Task<Result<OutFileCollection>> Bundle(Session session, Artifact artifact, List<Shard> shards, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            if (artifact.Role != ArtifactRole.Server)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                );
            }

            if (artifact.TargetLang != ArtifactTargetLang.TypeScript)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                );
            }

            if(HasErrors(result) || token.IsCancellationRequested) return result;


            var ofc = new OutFileCollection();

            foreach (var shard in shards)
            {
                if (shard.Role == ShardRole.MainApp)
                {
                    var mainAppOFC = result.AddMessages(
                        EmitMainAppShard(session, artifact, shard, token)
                    );

                    if(mainAppOFC != null)
                    {
                        ofc.AddAll(mainAppOFC);
                    }
                }
                else
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"Unsupported shard role '{shard.Role.ToString()}' in bundler for artifact role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                    );
                }
            }

            result.Value = ofc;

            return result;
        }

        private Result<OutFileCollection> EmitMainAppShard(Session session, Artifact artifact, Shard shard, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();
            var ofc = new OutFileCollection();

            // [dho] DEPENDENCIES - 25/02/20
            {
                // [dho] make sure all the dependencies are added to the shard - 25/02/20
                foreach(var dependency in NodeJSPartialBundler.RequiredDependencies)
                {
                    result.AddMessages(
                        DependencyHelpers.AddIfNotPresent(ref shard.Dependencies, dependency)
                    );
                }
                foreach(var dependency in ExpressBundler.RequiredDependencies)
                {
                    result.AddMessages(
                        DependencyHelpers.AddIfNotPresent(ref shard.Dependencies, dependency)
                    );
                }
            }

            var nodeOutputFiles = result.AddMessages(
                new NodeJSPartialBundler().EmitMainAppShard(session, artifact, shard, token)
            );

            if(nodeOutputFiles?.Count > 0)
            {
                ofc.AddAll(nodeOutputFiles);
            }

            result.AddMessages(
                EmitExpressEntrypoint(session, artifact, shard, ofc, token)
            );
            
            var relResourcePaths = result.AddMessages(
                AddResourceFiles(session, artifact, shard, ofc, "res/")
            );

            // DEPENDENCIES - 25/02/20
            {
                // [dho] make sure all the dependencies for firebase functions are added to the shard - 25/02/20
                foreach(var dependency in ExpressBundler.RequiredDependencies)
                {
                    result.AddMessages(
                        DependencyHelpers.AddIfNotPresent(ref shard.Dependencies, dependency)
                    );
                }
                    
                var packageJSONCode = result.AddMessages(
                    NodeJS.PackageJSONHelpers.EmitManifestCode(shard.AST, artifact.Name, "1.0.0", $"lib/{EntrypointFilenameWithoutExt}.js", shard.Dependencies, token)
                );

                if(!string.IsNullOrEmpty(packageJSONCode))
                {
                    var packageJSONRelFilePath = "package.json";

                    // [dho] create the file containing the manifest code - 25/02/20
                    var didAddPackageJSON = AddRawFileIfNotPresent(ofc, packageJSONRelFilePath, packageJSONCode);

                    // [dho] guard to ensure we did add the manifest to the output successfully - 25/02/20
                    if(!didAddPackageJSON)
                    {
                        result.AddMessages(
                            new Message(
                                MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                                $"Could not create package.json manifest because file called '{packageJSONRelFilePath}' already exists in output"
                            )
                        );
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(HasErrors(result), 
                        "Expected result to have errors if package.json manifest code is null or empty, but did not find any errors");
                }
            }

            // TSCONFIG - 25/02/20
            {  
                var tsConfigCode = result.AddMessages(
                    NodeJS.TSConfigHelpers.EmitConfigCode(shard.AST, new string[] { UserCodeDirName }, "lib", token)
                );

                if(!string.IsNullOrEmpty(tsConfigCode))
                {
                    var tsConfigRelFilePath = "tsconfig.json";

                    // [dho] create the file containing the manifest code - 25/02/20
                    var didAddTSConfig = AddRawFileIfNotPresent(ofc, tsConfigRelFilePath, tsConfigCode);

                    // [dho] guard to ensure we did add the manifest to the output successfully - 25/02/20
                    if(!didAddTSConfig)
                    {
                        result.AddMessages(
                            new Message(
                                MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                                $"Could not create tsconfig.json because file called '{tsConfigRelFilePath}' already exists in output"
                            )
                        );
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(HasErrors(result), 
                        "Expected result to have errors if tsconfig.json code is null or empty, but did not find any errors");
                }
            }

            result.Value = ofc;

            return result;
        }
        
        private static Result<object> EmitExpressEntrypoint(Session session, Artifact artifact, Shard shard, OutFileCollection ofc, CancellationToken token)
        {
            var result = new Result<object>();

            var entrypointContent = new System.Text.StringBuilder();
            entrypointContent.AppendLine($"import {{ {NodeJSPartialBundler.UserCodeSymbolicLexeme} }} from './{NodeJSPartialBundler.IndexFilenameWithoutExt}'");
            entrypointContent.AppendLine($"import $expressApp from './{NodeJSPartialBundler.IndexFilenameWithoutExt}'");
            entrypointContent.AppendLine($"const setup = ({NodeJSPartialBundler.UserCodeSymbolicLexeme} as any).default ?? (() => {{}});");
            entrypointContent.AppendLine("const port = process.env.PORT || 8080");
            entrypointContent.AppendLine("const useSSL = process.env.SSL !== void 0");
            entrypointContent.AppendLine(NodeJS.ExpressHelpers.IsProductionDeclaration);
            entrypointContent.AppendLine(
$@"if(useSSL || {NodeJS.ExpressHelpers.IsProductionSymbolicLexeme})
{{
    const fs = require('fs');
    const path = require('path');
    const privateKey  = fs.readFileSync(path.join(__dirname, '../res/localhost.key'), 'utf8');
    const certificate = fs.readFileSync(path.join(__dirname, '../res/localhost.crt'), 'utf8');

    const credentials = {{ key : privateKey, cert : certificate }};

    Promise.resolve(setup({{}}))
        .then(() => require('https').createServer(credentials, $expressApp).listen(port, () => console.log('Server started on ' + port + '!')))
        .catch(err => {{ throw err; }});
}}
else {{
    Promise.resolve(setup({{}}))
        .then(() => require('http').createServer($expressApp).listen(port, () => console.log('Server started on ' + port + '!')))
        .catch(err => {{ throw err; }});
}}");
            
            var entrypointRelFilePath = UserCodeRelDirPath + "/" + EntrypointFilenameWithoutExt + ".ts";

            var didAddEntrypoint = AddRawFileIfNotPresent(ofc, entrypointRelFilePath, entrypointContent.ToString());

            // [dho] guard to ensure we did add the router file to the output successfully - 25/02/20
            if(!didAddEntrypoint)
            {
                result.AddMessages(
                    new Message(
                        MessageKind.Warning, // [dho] NOTE only a warning for now? - 25/02/20
                        $"Could not create Firebase functions entrypoint because file called '{entrypointRelFilePath}' already exists in output"
                    )
                );
            }
            
            return result;
        }
    }

}