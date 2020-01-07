using System;
using Sempiler.Core;
using Sempiler.AST;
using Sempiler.Diagnostics;
using Sempiler.Emission;
using Sempiler.Languages;
using Sempiler.AST.Diagnostics;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using static Sempiler.SourceHelpers;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace Sempiler.CTExec
{
    using static CTExecTransformerHelpers;

    public static class CTExecHelpers
    {

        private static readonly string CTInSituHandleLexeme = CompilerHelpers.NextInternalGUID();

        private static readonly string MaybeNullPolyfillHandleLexeme = "maybeNullPolyfill";
        private static readonly string NullCoalescencePolyfillHandleLexeme = "nullCoalescencePolyfill";

        public struct CTInSituFunctionIdentifiers
        {
            public string ReplaceNodeByCodeConstant;
            public string InsertImmediateSiblingFromValueAndDeleteNode;
            public string DeleteNode;
            public string Terminate;
            public string MaybeNullPolyfill;
            public string NullCoalescencePolyfill;
        }

        public static readonly CTInSituFunctionIdentifiers CTInSituFnIDs = new CTInSituFunctionIdentifiers
        {
            ReplaceNodeByCodeConstant = $"global.{CTInSituHandleLexeme}.{CTAPISymbols.ReplaceNodeByCodeConstant}",
            // InsertImmediateSiblingFromValueAndDeleteNode = $"global.{CTInSituHandleLexeme}.{CTAPISymbols.InsertImmediateSiblingFromValueAndDeleteNode}",
            DeleteNode = $"global.{CTInSituHandleLexeme}.{CTAPISymbols.DeleteNode}",
            Terminate = $"global.{CTInSituHandleLexeme}.{CTAPISymbols.Terminate}",
            MaybeNullPolyfill = $"global.{CTInSituHandleLexeme}.{MaybeNullPolyfillHandleLexeme}",
            NullCoalescencePolyfill = $"global.{CTInSituHandleLexeme}.{NullCoalescencePolyfillHandleLexeme}"
        };

        public const string ArtifactNameSymbolLexeme = "artifactName";
        public const string ShardIndexSymbolLexeme = "shardIndex";
        public const string MessageIDSymbolLexeme = "messageID";
        public const string NodeIDSymbolLexeme = "nodeID";

        public const string NewArtifactNameSymbolLexeme = "newArtifactName";
        public const string NewShardIndexSymbolLexeme = "newShardIndex";
        public const string NewSourcesSymbolLexeme = "newSources";

        public static readonly string[] LibIncludeArgs = new string[] {};
        public static readonly string[] ComponentIncludeArgs = new string[] { ArtifactNameSymbolLexeme, ShardIndexSymbolLexeme };


        // [dho] prepended to every CT Command - 24/11/19
        private static readonly string CTCommandPreamble = $"${{{ArtifactNameSymbolLexeme}}}{CTProtocolHelpers.ArgumentDelimiter}${{{ShardIndexSymbolLexeme}}}{CTProtocolHelpers.ArgumentDelimiter}${{{NodeIDSymbolLexeme}}}{CTProtocolHelpers.ArgumentDelimiter}${{{MessageIDSymbolLexeme}}}{CTProtocolHelpers.CommandStartToken}";

        private const string CTExecErrorLexeme = "ctExecError";


        public static Result<object> InitializeRunTime(Session session,  ISourceProvider sourceProvider, DuplexSocketServer server, MessageCollection diagnostics, CancellationToken token)
        {
            var result = new Result<object>();

            
            var ofc = new OutFileCollection();

            result.AddMessages(
                AddSessionCTExecSourceFiles(session, ofc, token)
            );

            if (HasErrors(result) || token.IsCancellationRequested) return result;

            var absOutDirPath = session.CTExecInfo.OutDirectory.ToPathString();
            var filesWritten = default(Dictionary<string, OutFile>);
            try
            {
                var task = FileSystem.Write(absOutDirPath, ofc, token);

                task.Wait();

                filesWritten = result.AddMessages(task.Result);
            
            }
            catch (Exception e)
            {
                result.AddMessages(CreateErrorFromException(e));
            }

            if (HasErrors(result) || token.IsCancellationRequested) return result;


            string[] absLibPaths = new string[filesWritten.Keys.Count];

            filesWritten.Keys.CopyTo(absLibPaths, 0);

            session.CTExecInfo.AbsoluteLibPaths = absLibPaths;
            

            {
                foreach(var kv in filesWritten)
                {
                    session.CTExecInfo.FilesWritten.Add(kv.Key, kv.Value);
                }
            }
       
            server.OnMessage += session.CTExecInfo.MessageHandler = CTExecMessageHandlerHelpers.CreateMessageHandler(session, sourceProvider, server, diagnostics, token);


            return result;
        }

        public static Result<object> DestroyRunTime(Session session, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<object>();

            var absOutDirPath = session.CTExecInfo.OutDirectory.ToPathString();

            try
            {
                // [dho] delete the build files we created - 06/05/19
                // [dho] NOTE bit worried this will complain if directory is not empty.. because before
                // we were explicitly deleting child directories first.. but let's see with this new impl! - 25/11/19
                // var deleteFilesWrittenTask = FileSystem.Delete(filesWritten.Keys); <-- DISABLED THIS!
                var task = FileSystem.Delete(new string[] { absOutDirPath });

                task.Wait();

                result.AddMessages(task.Result);
            }
            catch (Exception e)
            {
                result.AddMessages(CreateErrorFromException(e));
            }

            if (HasErrors(result) || token.IsCancellationRequested) return result;
           
            server.OnMessage -= session.CTExecInfo.MessageHandler;
            session.CTExecInfo.MessageHandler = null;

            return result;
        }

        

        // private delegate Result<string[]> AddCTExecSourceFilesDelegate(Session session, Artifact artifact, Shard shard, List<Component> seedComponents, OutFileCollection ofc, CancellationToken token);

        
        public struct ParseNewSourceFilesResult
        {
            public List<string> TotalPaths;
            public List<Component> NewComponents;
        }

        public static Result<ParseNewSourceFilesResult> ParseNewSources(IEnumerable<SourceFilePatternMatchInput> inputs, Session session, Artifact artifact, RawAST ast, ISourceProvider sourceProvider, DuplexSocketServer server, CancellationToken token)
        {
            var result = new Result<ParseNewSourceFilesResult>();

            var componentNames = ASTHelpers.GetComponentNames(ast);
            SourceFileFilterDelegate filter = path => System.Array.IndexOf(componentNames, path) == -1;
     
            var parsedPaths = result.AddMessages(FilterNewSourceFilePaths(session, ast, inputs, filter, token));

            var totalPaths = parsedPaths.TotalPaths;
            var pathsNotInAST = parsedPaths.NewPaths;
            var newComponents = new List<Component>();

            var domain = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(domain.Kind == SemanticKind.Domain);

            // [dho] parse components and add them to the tree - 14/05/19
            if (pathsNotInAST?.Count > 0)
            {
                var pathsToParse = new List<string>();

                foreach(var path in pathsNotInAST)
                {
                    if(session.ComponentCache.ContainsKey(path))
                    {
                        var cachedComponent = session.ComponentCache[path];

                        ASTHelpers.DeepRegister(cachedComponent.AST, ast, domain.ID, new[] { cachedComponent.Node }, token);

                        newComponents.Add(cachedComponent);
                    }
                    else
                    {
                        pathsToParse.Add(path);
                    }
                }

                if(pathsToParse.Count > 0)
                {
                    var parser = new Sempiler.Parsing.PolyParser();

                    var parsedComponents = result.AddMessages(
                        Sempiler.CompilerHelpers.Parse(parser, session, ast, sourceProvider, pathsNotInAST, token)
                    );

                    if(parsedComponents != null)
                    {
                        SessionHelpers.CacheComponents(session, parsedComponents, token);
                        newComponents.AddRange(parsedComponents);
                    }
                }
            }

            result.Value = new ParseNewSourceFilesResult
            {
                TotalPaths = totalPaths,
                NewComponents = newComponents
            };

            return result;
        }

        public struct FilterNewSourceFilePathsResult
        {
            public List<string> TotalPaths;
            public List<string> NewPaths;
            public List<ISourceFile> NewSourceFiles;
        }

        public delegate bool SourceFileFilterDelegate(string path);

        public static Result<FilterNewSourceFilePathsResult> FilterNewSourceFilePaths(Session session, RawAST ast, IEnumerable<SourceFilePatternMatchInput> inputs, SourceFileFilterDelegate filter, CancellationToken token)
        {
            var result = new Result<FilterNewSourceFilePathsResult>();


            var totalPaths = new List<string>();
            var newPaths = new List<string>();
            var newSourceFiles = new List<ISourceFile>();

            // [dho] set this in any case to protect calling code from `NullPointerException` - 29/09/19
            result.Value = new FilterNewSourceFilePathsResult
            {
                TotalPaths = totalPaths,
                NewPaths = newPaths,
                NewSourceFiles = newSourceFiles
            };


            // [dho] dedup the components based on ones already in the tree 
            // (in the case where the code has tried to add the same source multiple times) - 14/05/19
            {
                var sourceFiles = result.AddMessages(SourceHelpers.EnumerateSourceFilePatternMatches(inputs));

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                // [dho] filter out any paths we have already parsed before - 06/05/19
                foreach (var sourceFile in sourceFiles)
                {
                    // [dho] assumes we always name the component with the abs path to the file - 06/05/19
                    var sourceFilePath = sourceFile.GetPathString();

                    // [dho] ignore hidden crappy files on macOS - 28/07/19
                    if (Path.GetExtension(sourceFilePath) == ".ds_store")
                    {
                        continue;
                    }

                    if (filter(sourceFilePath))
                    {
                        newPaths.Add(sourceFilePath);
                        newSourceFiles.Add(sourceFile);
                    }

                    totalPaths.Add(sourceFilePath);
                }
            }


            return result;
        }

        public static Result<object> AddSessionCTExecSourceFiles(Session session, OutFileCollection ofc, CancellationToken token)
        {
            var result = new Result<object>();

            var content = CreateCTExecSessionAPICode(session);
            var relPath = CTInSituHandleLexeme + ".js";

            System.Diagnostics.Debug.Assert(
                Sempiler.Bundling.BundlerHelpers.AddRawFileIfMissing(ofc, relPath, content)
            );

            return result;
        }

        private static string CreateCTExecSessionAPICode(Session session)
        {
            var host = session.Server.IPAddress;
            var port = session.Server.Port;

            var sendMessagePreamble = $@"await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, ";

            return (
$@"
module.exports = async function () {{
    async function createClient(host, port)
    {{
        // if(_client) return _client;

        const socket = new require('net').Socket();
        
        await new Promise((resolve, reject) => {{
            socket.connect(port, host, resolve);
        }})
        

        let continueExecution = true;

        function sendMessage(id, message)
        {{
            return new Promise((resolve, reject) => {{
                // console.log('send message ' + id + ', continue exec? ' + continueExecution);
                // [dho] flag to avoid us doing unnecessary work if the server
                // tells us to stop - 25/11/19
                if(!continueExecution) return;

                // console.log(`SENDING :::: ${{message}}{DuplexSocketServer.MessageSentinel}`)

                const timerName = id;

                console.time(timerName);

                socket.write(`${{message}}{DuplexSocketServer.MessageSentinel}`);
            
                socket.on('data', buffer => {{
                    
                    const data = buffer.toString('utf8');

                    const serializedJSON = data.substring(0, data.length - '{DuplexSocketServer.MessageSentinel}'.length);

                    const {{ ok, id : respMessageID, data : respData }} = JSON.parse(serializedJSON);

                    // console.log('finished \'' + message + '\'');
                    console.timeEnd(timerName);

                    if(!ok)
                    {{
                        // [dho] flag to avoid us doing unnecessary work if the server
                        // tells us to stop - 25/11/19
                        continueExecution = false;
                        return;
                    }}

                    if(respMessageID === id)
                    {{
                        
                        // console.log('ok:' + ok + ', original id: ' + id + ', response message id: ' + respMessageID + ', continue exec? ' + continueExecution);
                        
                        if(continueExecution)
                        {{
                            resolve(respData);
                        }}
                    }}

                }});
            
                socket.on('error', (err) => {{
                    // console.log('GOT ERR', err);
                    if(continueExecution)
                    {{
                        // [dho] 'ECONNRESET' means the server hung up - 26/11/19
                        if(err.syscall !== 'ECONNRESET')
                        {{
                            reject(err)
                        }}
                    }}
                    
                    // reject(err) || destroy()
                }});
            }});
        }}

        function destroy()
        {{
            socket.destroy();
            _client = null;
        }}
        
        return _client = {{ sendMessage, destroy }};
    }}
    // let _client = null;



    async function {CTAPISymbols.AddCapability}(name, value)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        if(Array.isArray(value))
        {{
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddCapability}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}{(int)ConfigurationPrimitive.StringArray}{CTProtocolHelpers.ArgumentDelimiter}${{value.join('{CTProtocolHelpers.ArgumentDelimiter}')}})`);
        }}
        else 
        {{
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddCapability}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}{(int)ConfigurationPrimitive.String}{CTProtocolHelpers.ArgumentDelimiter}${{value}})`);
        }}
    }}

    async function {CTAPISymbols.AddDependency}(name, version, packageManager, url)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        {/* [dho] TODO CLEANUP - 04/01/20 */""}
        if(arguments.length === 1)
        {{
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddDependency}(${{name}})`);
        }}
        else if(arguments.length === 2)
        {{
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddDependency}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}${{version}})`);
        }}
        else if(arguments.length === 3)
        {{
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddDependency}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}${{version}}{CTProtocolHelpers.ArgumentDelimiter}${{packageManager}})`);
        }}
        else
        {{
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddDependency}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}${{version}}{CTProtocolHelpers.ArgumentDelimiter}${{packageManager}}{CTProtocolHelpers.ArgumentDelimiter}${{url}})`);
        }}
    }}

    async function {CTAPISymbols.AddEntitlement}(name, value)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        if(Array.isArray(value))
        {{
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddEntitlement}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}{(int)ConfigurationPrimitive.StringArray}{CTProtocolHelpers.ArgumentDelimiter}${{value.join('{CTProtocolHelpers.ArgumentDelimiter}')}})`);
        }}
        else 
        {{
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddEntitlement}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}{(int)ConfigurationPrimitive.String}{CTProtocolHelpers.ArgumentDelimiter}${{value}})`);
        }}
    }}

    async function {CTAPISymbols.AddPermission}(name, description)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddPermission}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}${{description || ''}})`);
    }}

    async function {CTAPISymbols.AddRes}(parentDirPath, sourcePath)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddRes}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{sourcePath}})`);
    }}

    async function {CTAPISymbols.AddRawSources}(parentDirPath, ...includedPaths)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddRawSources}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{includedPaths.join('{CTProtocolHelpers.ArgumentDelimiter}')}})`);
    }}

    async function {CTAPISymbols.AddSources}(parentDirPath, ...includedPaths)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        const newSources = {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddSources}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{includedPaths.join('{CTProtocolHelpers.ArgumentDelimiter}')}})`);
    
        await consumeNewSources({ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, newSources);
    }}

    async function {CTAPISymbols.AddAsset}(parentDirPath, role, sourcePath)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddAsset}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{role}}{CTProtocolHelpers.ArgumentDelimiter}${{sourcePath}})`);
    }}

    async function {CTAPISymbols.AddArtifact}(parentDirPath, name, targetLanguage, targetPlatform, sourcePath)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        const {{ {NewArtifactNameSymbolLexeme}, {NewShardIndexSymbolLexeme}, {NewSourcesSymbolLexeme} }} = {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddArtifact}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{name}}{CTProtocolHelpers.ArgumentDelimiter}${{targetLanguage}}{CTProtocolHelpers.ArgumentDelimiter}${{targetPlatform}}{CTProtocolHelpers.ArgumentDelimiter}${{sourcePath}})`);
    
        await consumeNewSources({NewArtifactNameSymbolLexeme}, {NewShardIndexSymbolLexeme}, {NewSourcesSymbolLexeme});
    }}

    async function {CTAPISymbols.AddShard}(parentDirPath, role, sourcePath)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        const {{ {NewShardIndexSymbolLexeme}, {NewSourcesSymbolLexeme} }} = {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.AddShard}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{role}}{CTProtocolHelpers.ArgumentDelimiter}${{sourcePath}})`);
    
        await consumeNewSources({ArtifactNameSymbolLexeme}, {NewShardIndexSymbolLexeme}, {NewSourcesSymbolLexeme});
    }}

    async function {CTAPISymbols.SetDisplayName}(name)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.SetDisplayName}(${{name}})`);
    }}

    async function {CTAPISymbols.SetTeamName}(name)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.SetTeamName}(${{name}})`);
    }}

    async function {CTAPISymbols.SetVersion}(version)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.SetVersion}(${{version}})`);
    }}

    async function {CTAPISymbols.IsArtifactName}(name)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.IsArtifactName}(${{name}})`);
    }}

    async function {CTAPISymbols.IsTargetLanguage}(lang)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.IsTargetLanguage}(${{lang}})`);
    }}

    async function {CTAPISymbols.IsTargetPlatform}(platform)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.IsTargetPlatform}(${{platform}})`);
    }}

    async function {CTAPISymbols.DeleteNode}(outgoingNodeID)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.DeleteNode}(${{outgoingNodeID}})`);
    }}

    async function {CTAPISymbols.ReplaceNodeByCodeConstant}(removeeID, codeConstant)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.ReplaceNodeByCodeConstant}(${{removeeID}}{CTProtocolHelpers.ArgumentDelimiter}${{codeConstant}})`);
    }}

    // async function {/*CTAPISymbols.InsertImmediateSiblingFromValueAndDeleteNode*/""}(insertionPointID, value, removeeID)
    // {{
    //     const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

    //     return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.InsertImmediateSiblingAndFromValueAndDeleteNode}(${{insertionPointID}}{CTProtocolHelpers.ArgumentDelimiter}${{value.constructor.name}}{CTProtocolHelpers.ArgumentDelimiter}${{value}}{CTProtocolHelpers.ArgumentDelimiter}${{removeeID}})`);
    // }}

    async function {CTAPISymbols.Terminate}(error)
    {{
        const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {NodeIDSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

        if(!!error)
        {{
            console.log('GOT AN ERROR');
            console.dir(error);
            const statusCode = 1;

            {/* [dho] adapted from : https://github.com/watilde/parse-error/blob/master/lib/main.js - 12/12/19 */""}
            var stack = error.stack ? error.stack : '';
            var stackObject = stack.split('\n');
            var {{ filename, line, row }} = _getErrPosition(stackObject);
            var splitMessage = error.message ? error.message.split('\n') : [''];
            var message = splitMessage[splitMessage.length - 1];
            // var type = error.type ? error.type : error.name;
          
            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.Terminate}(${{statusCode}}{CTProtocolHelpers.ArgumentDelimiter}${{message}}{CTProtocolHelpers.ArgumentDelimiter}${{filename}}{CTProtocolHelpers.ArgumentDelimiter}${{line}}{CTProtocolHelpers.ArgumentDelimiter}${{row}})`);
        }}
        else
        {{
            const statusCode = 0;

            return {sendMessagePreamble}`{CTCommandPreamble}{(int)CTProtocolCommandKind.Terminate}(${{statusCode}})`);
        }}
    }}

    {/*[dho] adapted from : https://github.com/watilde/parse-error/blob/master/lib/modules/getPosition.js - 12/12/19 */""}
    function _getErrPosition(stackObject) {{
        var filename, line, row;
        // Because the JavaScript error stack has not yet been standardized,
        // wrap the stack parsing in a try/catch for a soft fail if an
        // unexpected stack is encountered.
        try {{
            var filteredStack = stackObject
            .filter(function (s) {{
                return /\(.+?\)$/.test(s);
            }});
            var splitLine;
            // For current Node & Chromium Error stacks
            if(filteredStack.length > 0) {{
            splitLine = filteredStack[0]
                .match(/(?:\()(.+?)(?:\))$/)[1]
                .split(':');
            // For older, future, or otherwise unexpected stacks
            }} else {{
            splitLine = stackObject[0]
                .split(':');
            }}
            var splitLength = splitLine.length;
            filename = splitLine[splitLength - 3];
            line = Number(splitLine[splitLength - 2]);
            row = Number(splitLine[splitLength - 1]);
        }} catch(err) {{
            filename = '';
            line = 0;
            row = 0;
        }}
        return {{
            filename: filename,
            line: line,
            row: row
        }};
    }};

    async function consumeNewSources({ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, newSources)
    {{
        return Promise.all(
            {""/* [dho] we load the async exported function and invoke it - 26/11/19 */}
            newSources.map(newSource => require(newSource)({string.Join(",", ComponentIncludeArgs)}))
        );
    }}

    function {MaybeNullPolyfillHandleLexeme}(input)
    {{
        return input || new Proxy({{}}, {{ 
            get(){{ 
                return function () {{}} 
            }} 
        }});
    }}

    function {NullCoalescencePolyfillHandleLexeme}()
    {{
        for (let i = 0; i < arguments.length; i++) {{
            const arg = arguments[i];
            
            if(arg !== null && arg !== void 0)
            {{
                return arg;
            }}
        }}

        return null;
    }}

    global.{CTInSituHandleLexeme} = {{ 
        {CTAPISymbols.AddArtifact},
        {CTAPISymbols.AddCapability},
        {CTAPISymbols.AddDependency},
        {CTAPISymbols.AddEntitlement},
        {CTAPISymbols.AddAsset},
        {CTAPISymbols.AddPermission},
        {CTAPISymbols.AddRawSources},
        {CTAPISymbols.AddRes},
        {CTAPISymbols.AddSources}, 
        {CTAPISymbols.AddShard}, 
        {CTAPISymbols.IsArtifactName},
        {CTAPISymbols.IsTargetLanguage},
        {CTAPISymbols.IsTargetPlatform},
        {CTAPISymbols.SetDisplayName},
        {CTAPISymbols.SetTeamName},
        {CTAPISymbols.SetVersion},
        {CTAPISymbols.ReplaceNodeByCodeConstant},
        //{/*CTAPISymbols.InsertImmediateSiblingFromValueAndDeleteNode*/""}, 
        {CTAPISymbols.DeleteNode},
        {CTAPISymbols.Terminate},
        
        {MaybeNullPolyfillHandleLexeme},
        {NullCoalescencePolyfillHandleLexeme}
    }};

    // [dho] make the public compiler API symbols available in scope as unqualified names - 12/07/19
    global.{CTAPISymbols.AddArtifact} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddArtifact};
    global.{CTAPISymbols.AddCapability} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddCapability};
    global.{CTAPISymbols.AddDependency} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddDependency};
    global.{CTAPISymbols.AddEntitlement} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddEntitlement};
    global.{CTAPISymbols.AddAsset} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddAsset};
    global.{CTAPISymbols.AddPermission} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddPermission};
    global.{CTAPISymbols.AddRes} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddRes};
    global.{CTAPISymbols.AddRawSources} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddRawSources};
    global.{CTAPISymbols.AddSources} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddSources};
    global.{CTAPISymbols.AddShard} = global.{CTInSituHandleLexeme}.{CTAPISymbols.AddShard};
    global.{CTAPISymbols.IsArtifactName} = global.{CTInSituHandleLexeme}.{CTAPISymbols.IsArtifactName};
    global.{CTAPISymbols.IsTargetLanguage} = global.{CTInSituHandleLexeme}.{CTAPISymbols.IsTargetLanguage};
    global.{CTAPISymbols.IsTargetPlatform} = global.{CTInSituHandleLexeme}.{CTAPISymbols.IsTargetPlatform};
    global.{CTAPISymbols.SetDisplayName} = global.{CTInSituHandleLexeme}.{CTAPISymbols.SetDisplayName};
    global.{CTAPISymbols.SetTeamName} = global.{CTInSituHandleLexeme}.{CTAPISymbols.SetTeamName};
    global.{CTAPISymbols.SetVersion} = global.{CTInSituHandleLexeme}.{CTAPISymbols.SetVersion};
}};
"
           );
        }

        public static Result<string[]> AddSessionCTExecSourceFiles(Session session, Artifact artifact, Shard shard, List<Component> seedComponents, OutFileCollection ofc, CancellationToken token)
        {
            
            /*
            (async () => {
                require('server-code.js')
                let ctExecError = null;
                try 
                {
                    ....inlined initially parsed components
                }
                catch(err)
                {
                    ctExecError = err;
                }

                await sXeXrver.terminate(ctExecError)
            })()
            */  
            
            var result = new Result<string[]>();

            var shardIndex = session.Shards[artifact.Name].IndexOf(shard);

            System.Diagnostics.Debug.Assert(shardIndex > -1);

            // [dho] NOTE we operate on a clone of the AST to avoid mutating
            // the original AST when we instrument the program for CT execution - 14/07/19
            var ast = shard.AST.Clone();

            var domain = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

            // var xx = new List<string>();

            // foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, domain.ID))
            // {
            //     var remove = true;

            //     foreach(var x in seedComponents)
            //     {
            //         if(child.ID == x.ID)
            //         {
            //             remove = false;
            //             break;
            //         }
            //     }

            //     if(remove)
            //     {
            //         xx.Add(child.ID);
            //     }
            // }


            var languageSemantics = LanguageSemantics.Of(artifact.TargetLang);

            if(languageSemantics == null)
            {
                result.AddMessages(new Message(MessageKind.Warning, $"Language semantics not configured for {artifact.TargetLang}, using TypeScript semantics as a fallback"));

                languageSemantics = LanguageSemantics.TypeScript;
            }

            result.AddMessages(
                FilterAndTransformAddedSources(session, artifact, shard, ast, languageSemantics, seedComponents, ofc, token)
            );

            var shardRootComponent = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Transformation), CompilerHelpers.NextInternalGUID());

            var componentPaths = new List<string>();

            var emitter = new CTExecEmitter(
                shardRootComponent.ID,  
                shard.Role == ShardRole.MainApp, 
                languageSemantics
            );

            foreach(var component in seedComponents)
            {
                componentPaths.Add(
                    "." + System.IO.Path.DirectorySeparatorChar + 
                    emitter.RelativeComponentOutFilePath(session, artifact, shard, component)
                );
            }

            var libIncludes = CTExecEmitter.CreateAwaitIncludes(session.CTExecInfo.AbsoluteLibPaths, LibIncludeArgs);
            var componentIncludes = CTExecEmitter.CreateAwaitIncludes(componentPaths, ComponentIncludeArgs);        

            var shardScript = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"
    const {ArtifactNameSymbolLexeme} = '{artifact.Name}';
    const {ShardIndexSymbolLexeme} = '{shardIndex}';

    let {CTExecErrorLexeme} = null;

    try{{
        {libIncludes}
        {componentIncludes}
    }}
    catch(error){{
        {CTExecErrorLexeme} = error;
    }}
try {{
    await {CTInSituFnIDs.Terminate}.call({{ 

        {ArtifactNameSymbolLexeme},
        {ShardIndexSymbolLexeme},
        {MessageIDSymbolLexeme} : {CTExecEmitter.CreateMessageIDCode()}

    }}, {CTExecErrorLexeme}); }} catch(e) {{ console.dir(e) }} ").Node;

        

            MarkAsCTExec(ast, shardScript);


            ASTHelpers.Connect(ast, shardRootComponent.ID, new [] { shardScript }, SemanticRole.None);

            
            // [dho] add the component containing the inlined app code to the tree - 01/06/19
            ASTHelpers.Connect(ast, domain.ID, new [] { shardRootComponent.Node }, SemanticRole.Component);
            
            
            result.AddMessages(
                EmitASTAndAddOutFiles(session, artifact, shard, ast, emitter, ofc, token)
            );

            result.Value = new [] { 
                "." + System.IO.Path.DirectorySeparatorChar + 
                    emitter.RelativeComponentOutFilePath(session, artifact, shard, shardRootComponent) 
            };


            return result;
        }
        
        

        ///<summary>
        /// Filters out any components from the AST that are not present in the added sources,
        /// and prepares the added sources for CT Exec - 01/12/19
        ///</summary>
        public static Result<object> FilterAndTransformAddedSources(Session session, Artifact artifact, Shard shard, RawAST ast, BaseLanguageSemantics languageSemantics, List<Component> addedSources, OutFileCollection ofc, CancellationToken token)
        {
            var result = new Result<object>();

            // // [dho] NOTE we operate on a clone of the AST to avoid mutating
            // // the original AST when we instrument the program for CT execution - 14/07/19
            // var ast = shard.AST.Clone();
            
            var domain = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

            var componentIDsToRemove = new List<string>();
 
            foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, domain.ID))
            {
                System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

                var component = ASTNodeFactory.Component(ast, (DataNode<string>)child);
                var isFocus = false;
                var hasEmittedAlready = session.CTExecInfo.ComponentIDsEmitted.ContainsKey(component.ID);

                if(!hasEmittedAlready)
                {
                    foreach(var fc in addedSources)
                    {
                        if(fc.ID == component.ID)
                        {
                            isFocus = true;

                            var hoistedNodes = new Dictionary<string, Node>();
                            var hoistedDirectiveStatements = new List<Node>();
                            
                            result.AddMessages(
                                TransformCTAPIInvocations(session, artifact, shard, ast, component, languageSemantics, token)
                            );

                            // [dho] NOTE do this first to ensure we still compute directives inside nodes that we do not actually emit
                            // in the CT exec program, eg. types, view declarations etc. - 12/07/19
                            result.AddMessages(
                                HoistDirectives(session, artifact, shard, ast, component, component.Node, languageSemantics, CTInSituFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
                            );

                            ASTHelpers.Connect(ast, component.ID, hoistedDirectiveStatements.ToArray(), SemanticRole.None, 0);

                            break;
                        }
                    }
                }
                else
                {
                    int i = 0;
                }


                if(!isFocus)
                {
                    componentIDsToRemove.Add(component.ID);
                }
            }

            ASTHelpers.DisableNodes(ast, componentIDsToRemove.ToArray());

            return result;
        }

        public static Result<object> EmitASTAndAddOutFiles(Session session, Artifact artifact, Shard shard, RawAST ast, CTExecEmitter emitter, OutFileCollection ofc, CancellationToken token)
        {
            var result = new Result<object>();

            {

                var emittedFiles = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, shard, ast, token));

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                // [dho] record which components we have emitted so we do not duplicate the work during the session - 05/12/19
                {
                    var domain = ASTHelpers.GetRoot(ast);

                    System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

                    lock(session.CTExecInfo.ComponentIDsEmitted)
                    {
                        foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, domain.ID))
                        {
                            System.Diagnostics.Debug.Assert(child?.Kind == SemanticKind.Component);

                            session.CTExecInfo.ComponentIDsEmitted[child.ID] = true;
                        }
                    }
                }


                // [dho] TODO CLEANUP HACK we are calling ourselves a TypeScript bundler, but we want the CT Exec to be
                // as fast as possible, so we are removing all types and just changing the file extension to `.js`. 
                // CLEANUP once we have a JavaScript emitter and can just use that.. - 12/07/19

                foreach (var outFile in emittedFiles)
                {
                    ofc[FileSystem.ParseFileLocation(outFile.Path)] = outFile.Emission;
                }
            }

            return result;

        }

        public static string JSONStringify(string[] input)
        {
            var sb = new System.Text.StringBuilder("[");

            if(input.Length > 0)
            {
                sb.Append("\"" + string.Join("\",\"", input) + "\"");
            }

            sb.Append("]");

            return sb.ToString();
        }

        // [dho] by marking a node as CTExec, it means the CTExecEmitter will emit it for sure 
        // for use at compile time - 28/11/19
        public static void MarkAsCTExec(RawAST ast, Node node)
        {
            var meta = NodeFactory.Meta(
                ast,
                new PhaseNodeOrigin(PhaseKind.Transformation),
                MetaFlag.CTExec
            );
            
            ASTHelpers.Connect(ast, node.ID, new [] { meta.Node }, SemanticRole.Meta);
        }



    }
}