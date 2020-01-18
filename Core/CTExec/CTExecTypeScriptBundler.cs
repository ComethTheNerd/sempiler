// using System;
// using System.Threading;
// using System.Collections.Generic;
// using Sempiler.AST;
// using Sempiler.AST.Diagnostics;
// using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
// using Sempiler.Diagnostics;
// using static Sempiler.Diagnostics.DiagnosticsHelpers;
// using Sempiler.Emission;
// using Sempiler.Languages;

// namespace Sempiler.CTExec
// {
//     public class CTExecTypeScriptBundler// : IBundler
//     {
//         static readonly string[] DiagnosticTags = new string[] { "bundler", "ct-exec-typescript" };

//         public readonly string[] InjectParentPathForCTAPISymbols = new string[] {
//             CTAPISymbols.AddSources,
//             CTAPISymbols.AddAsset,
//             CTAPISymbols.AddRes,
//             CTAPISymbols.AddRawSources,
//             CTAPISymbols.AddShard
//         };

//         static readonly string ServerInteropHandleLexeme = CompilerHelpers.NextInternalGUID();

//         static readonly ServerInteropFunctionIdentifiers ServerInteropFnIDs = new ServerInteropFunctionIdentifiers
//         {
//             ReplaceNodeByCodeConstant = $"global.{ServerInteropHandleLexeme}.{CTAPISymbols.ReplaceNodeByCodeConstant}",
//             InsertImmediateSiblingFromValueAndDeleteNode = $"global.{ServerInteropHandleLexeme}.{CTAPISymbols.InsertImmediateSiblingFromValueAndDeleteNode}",
//             DeleteNode = $"global.{ServerInteropHandleLexeme}.{CTAPISymbols.DeleteNode}",
//             Terminate = $"global.{ServerInteropHandleLexeme}.{CTAPISymbols.Terminate}",
//         };

//         // const string EntrypointFileName = "app";

//         const string ArtifactNameSymbolLexeme = "artifactName";
//         const string ShardIndexSymbolLexeme = "shardIndex";
//         const string MessageIDSymbolLexeme = "messageID";

//         // [dho] prepended to every CT Command - 24/11/19
//         static readonly string CTCommandPreamble = $"${{{ArtifactNameSymbolLexeme}}}{CTProtocolHelpers.ArgumentDelimiter}${{{ShardIndexSymbolLexeme}}}{CTProtocolHelpers.ArgumentDelimiter}${{{MessageIDSymbolLexeme}}}{CTProtocolHelpers.CommandStartToken}";

//         const string CTExecErrorLexeme = "ctExecError";

//         // public const string ExecRBScriptFileName = "ct-exec.rb";


//         // public IEnumerable<string> FilePathsToInit;


//         // public IList<string> GetPreservedDebugEmissionRelPaths() => new string[] { };

//         // public async Task<Result<OutFileCollection>> Bundle(Session session, Artifact artifact, List<Shard> shards, CancellationToken token)
//         // {
//         //     var result = new Result<OutFileCollection>();


//         //     System.Diagnostics.Debug.Assert(shards.Count == 1);
//         //     var shard = shards[0];
//         //     var ast = shard.AST;


//         //     // if (artifact.Role != ArtifactRole.Client)
//         //     // {
//         //     //     result.AddMessages(
//         //     //         new Message(MessageKind.Error,
//         //     //             $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
//         //     //     );

//         //     //     return result;
//         //     // }


//         //     var ofc = default(OutFileCollection);//new OutFileCollection();
//         //     var inlinedComponent = default(Component);

//         //     // [dho] emit source files - 21/05/19
//         //     {
//         //         var emitter = default(IEmitter);
//         //         // [dho] NOTE we operate on a clone of the AST to avoid mutating
//         //         // the original AST when we instrument the program for CT execution - 14/07/19
//         //         var ctAST = ast.Clone();

//         //         // if (artifact.TargetLang == ArtifactTargetLang.TypeScript)
//         //         // {
//         //         inlinedComponent = result.AddMessages(TypeScriptInlining(session, artifact, shard, ctAST, token));

//         //         if (HasErrors(result) || token.IsCancellationRequested) return result;

//         //         emitter = new TypeScriptEmitter();
//         //         {
//         //             var emittedFiles = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ctAST, token));

//         //             if (HasErrors(result) || token.IsCancellationRequested) return result;


//         //             // [dho] TODO CLEANUP HACK we are calling ourselves a TypeScript bundler, but we want the CT Exec to be
//         //             // as fast as possible, so we are removing all types and just changing the file extension to `.js`. 
//         //             // CLEANUP once we have a JavaScript emitter and can just use that.. - 12/07/19

//         //             ofc = new OutFileCollection();

//         //             foreach (var outFile in emittedFiles)
//         //             {
//         //                 var filePath = outFile.Path;

//         //                 if (filePath.EndsWith(".ts"))
//         //                 {
//         //                     filePath = filePath.Substring(0, filePath.Length - ".ts".Length) + ".js";
//         //                 }

//         //                 ofc[FileSystem.ParseFileLocation(filePath)] = outFile.Emission;
//         //             }

//         //         }


//         //         // }
//         //         // // [dho] TODO JavaScript! - 01/06/19
//         //         // else
//         //         // {
//         //         //     result.AddMessages(
//         //         //         new Message(MessageKind.Error,
//         //         //             $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
//         //         //     );
//         //         // }

//         //         if (HasErrors(result) || token.IsCancellationRequested) return result;
//         //     }

//         //     // [dho] synthesize any requisite files for the target platform - 01/06/19
//         //     {

//         //         //                 AddRawFileIfMissing(ofc, "package.json", 
//         //         // $@"{{
//         //         //   ""name"": ""{artifact.Name}"",
//         //         //   ""private"": true,
//         //         //   ""version"": ""1.0.0"",
//         //         //   ""license"": ""MIT"",
//         //         //   ""devDependencies"": {{
//         //         //     ""@types/node-fetch"": ""^2.1.4"",
//         //         //     ""ts-node"": ""^7.0.1"",
//         //         //     ""typescript"": ""^3.2.4""
//         //         //   }},
//         //         //   ""dependencies"": {{
//         //         //     ""node-fetch"": ""^2.3.0""
//         //         //   }},
//         //         //   ""scripts"": {{ 
//         //         //     ""start"": ""tsc && node '{inlinedComponent.Name}'"" 
//         //         //   }}
//         //         // }}");


//         //         //                 // [dho] because some of the types may exist in the target platform but not have explicit definitions,
//         //         //                 // we will just remove all the types for now and use TypeScript as a way to evalute the code rather
//         //         //                 // than check the correctness of it - 11/07/19
//         //         //                 AddRawFileIfMissing(ofc, $"tsconfig.json", 
//         //         // $@"{{
//         //         //     ""compilerOptions"": {{
//         //         //        ""target"": ""es5"" /* Specify ECMAScript target version: 'ES3' (default), 'ES5', 'ES2015', 'ES2016', 'ES2017','ES2018' or 'ESNEXT'. */,
//         //         //         ""module"": ""commonjs"" /* Specify module code generation: 'none', 'commonjs', 'amd', 'system', 'umd', 'es2015', or 'ESNext'. */,
//         //         //         ""lib"": [
//         //         //             ""es2015""
//         //         //         ] /* Specify library files to be included in the compilation. */,
//         //         //         ""strict"": false /* Enable all strict type-checking options. */,
//         //         //         ""noImplicitAny"": false /* Raise error on expressions and declarations with an implied 'any' type. */,
//         //         //         ""strictNullChecks"": false /* Enable strict null checks. */,
//         //         //         ""strictFunctionTypes"": false /* Enable strict checking of function types. */,
//         //         //         ""strictBindCallApply"": false /* Enable strict 'bind', 'call', and 'apply' methods on functions. */,
//         //         //         ""strictPropertyInitialization"": false /* Enable strict checking of property initialization in classes. */,
//         //         //         ""noImplicitThis"": false /* Raise error on 'this' expressions with an implied 'any' type. */,
//         //         //         ""alwaysStrict"": false /* Parse in strict mode and emit ""use strict"" for each source file. */,
//         //         //         ""esModuleInterop"": true /* Enables emit interoperability between CommonJS and ES Modules via creation of namespace objects for all imports. Implies 'allowSyntheticDefaultImports'. */
//         //         //     }},
//         //         //     ""include"": [
//         //         //         ""{inlinedComponent.Name}.*""
//         //         //     ]
//         //         // }}");

//         //         // AddRawFileIfMissing(ofc, ExecRBScriptFilename, $"`tsc && node '{inlinedComponent.Name}'`");

//         //         result.Value = ofc;
//         //     }


//         //     return result;
//         // }

//         private struct ServerInteropFunctionIdentifiers
//         {
//             public string ReplaceNodeByCodeConstant;
//             public string InsertImmediateSiblingFromValueAndDeleteNode;
//             public string DeleteNode;
//             public string Terminate;
//         }

//         public Result<object> AddSessionCTExecSourceFiles(Session session, OutFileCollection ofc, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var content = CreateCTExecSessionAPICode(session);
//             var relPath = ServerInteropHandleLexeme + ".js";

//             System.Diagnostics.Debug.Assert(
//                 Sempiler.Bundling.BundlerHelpers.AddRawFileIfMissing(ofc, relPath, content)
//             );

//             return result;
//         }

//         public Result<string[]> AddArtifactCTExecSourceFiles(Session session, Artifact artifact, Shard mainAppShard, List<Component> seedComponents, OutFileCollection ofc, CancellationToken token)
//         {
//             /*
//             (async () => {
//                 require('server-code.js')
//                 let ctExecError = null;
//                 try 
//                 {
//                     ....inlined initially parsed components
//                 }
//                 catch(err)
//                 {
//                     ctExecError = err;
//                 }

//                 await sXeXrver.terminate(ctExecError)
//             })()
//             */  
            
//             var result = new Result<string[]>();

//             var shardIndex = session.Shards[artifact.Name].IndexOf(mainAppShard);

//             System.Diagnostics.Debug.Assert(shardIndex > -1);

//             // [dho] NOTE we operate on a clone of the AST to avoid mutating
//             // the original AST when we instrument the program for CT execution - 14/07/19
//             var ast = mainAppShard.AST.Clone();

//             var domain = ASTHelpers.GetRoot(ast);

//             System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

//             // var xx = new List<string>();

//             // foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, domain.ID))
//             // {
//             //     var remove = true;

//             //     foreach(var x in seedComponents)
//             //     {
//             //         if(child.ID == x.ID)
//             //         {
//             //             remove = false;
//             //             break;
//             //         }
//             //     }

//             //     if(remove)
//             //     {
//             //         xx.Add(child.ID);
//             //     }
//             // }


//             var languageSemantics = LanguageSemantics.Of(artifact.TargetLang);

//             if(languageSemantics == null)
//             {
//                 result.AddMessages(new Message(MessageKind.Warning, $"Language semantics not configured for {artifact.TargetLang}, using TypeScript semantics as a fallback")
//                 {
//                     Tags = DiagnosticTags
//                 });

//                 languageSemantics = LanguageSemantics.TypeScript;
//             }

//             result.AddMessages(
//                 _______Hello(session, artifact, mainAppShard, ast, languageSemantics, seedComponents, ofc, token)
//             );

//             var artifactRootComponent = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Transformation), CompilerHelpers.NextInternalGUID());

//             var libIncludes = CTExecEmitter.CreateAwaitIncludes(session.CTExecInfo.AbsoluteLibPaths);
//             var componentPaths = new List<string>();

//             var emitter = new CTExecEmitter(artifactRootComponent.ID, languageSemantics);

//             foreach(var component in seedComponents)
//             {
//                 componentPaths.Add(
//                     "./" + emitter.RelativeComponentOutFilePath(session, component)
//                 );
//             }   

//             var componentIncludes = CTExecEmitter.CreateAwaitIncludes(componentPaths);        

//             ASTHelpers.Connect(ast, artifactRootComponent.ID, new [] {
//                 NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"
//     let {CTExecErrorLexeme} = null;

//     try{{
//         {componentIncludes}
//     }}
//     catch(error){{ 
//         {CTExecErrorLexeme} = error;
//     }}

//     await {ServerInteropFnIDs.Terminate}.call({{ 

//         {ArtifactNameSymbolLexeme} : '{artifact.Name}',
//         {ShardIndexSymbolLexeme} : '{shardIndex}',
//         {MessageIDSymbolLexeme} : '{CompilerHelpers.NextInternalGUID()}'

//     }}, {CTExecErrorLexeme});"
//                 ).Node
//             }, SemanticRole.None);

            
//             // [dho] add the component containing the inlined app code to the tree - 01/06/19
//             ASTHelpers.Connect(ast, domain.ID, new [] { artifactRootComponent.Node }, SemanticRole.Component);
            
            
//             result.AddMessages(
//                 ___EmitASTAndAddOutFiles(session, artifact, ast, artifactRootComponent.ID, languageSemantics, ofc, token)
//             );

//             result.Value = new [] { emitter.RelativeComponentOutFilePath(session, artifactRootComponent) };


//             return result;
//         }
        
//         public Result<object> AddSourcesCTExecSourceFiles(Session session, Artifact artifact, Shard shard, List<Component> addedSources, OutFileCollection ofc, CancellationToken token)
//         {
//             /*
//                 (async () => {
//                     require('server-code.js')

//                     ....inlined components
                    
//                 })
//             */  

//             var result = new Result<object>();

//             var languageSemantics = LanguageSemantics.Of(artifact.TargetLang);

//             if(languageSemantics == null)
//             {
//                 result.AddMessages(new Message(MessageKind.Warning, $"Language semantics not configured for {artifact.TargetLang}, using TypeScript semantics as a fallback")
//                 {
//                     Tags = DiagnosticTags
//                 });

//                 languageSemantics = LanguageSemantics.TypeScript;
//             }

//             var ast = shard.AST.Clone();

//             result.AddMessages(
//                 _______Hello(session, artifact, shard, ast, languageSemantics, addedSources, ofc, token)
//             );

//             result.AddMessages(
//                 ___EmitASTAndAddOutFiles(session, artifact, ast, null, languageSemantics, ofc, token)
//             );

//             return result;
//         }

//         public Result<object> _______Hello(Session session, Artifact artifact, Shard shard, RawAST ast, BaseLanguageSemantics languageSemantics, List<Component> addedSources, OutFileCollection ofc, CancellationToken token)
//         {
//             var result = new Result<object>();

//             // // [dho] NOTE we operate on a clone of the AST to avoid mutating
//             // // the original AST when we instrument the program for CT execution - 14/07/19
//             // var ast = shard.AST.Clone();
            
//             var domain = ASTHelpers.GetRoot(ast);

//             System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

//             var componentIDsToRemove = new List<string>();
 
//             foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, domain.ID))
//             {
//                 System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

//                 var component = ASTNodeFactory.Component(ast, (DataNode<string>)child);
//                 var isFocus = false;

//                 foreach(var fc in addedSources)
//                 {
//                     if(fc.ID == component.ID)
//                     {
//                         isFocus = true;

//                         var hoistedNodes = new Dictionary<string, Node>();
//                         var hoistedDirectiveStatements = new List<Node>();
                        
//                         result.AddMessages(
//                             TransformCTAPIInvocations(session, artifact, shard, ast, component, languageSemantics, token)
//                         );

//                         // [dho] NOTE do this first to ensure we still compute directives inside nodes that we do not actually emit
//                         // in the CT exec program, eg. types, view declarations etc. - 12/07/19
//                         result.AddMessages(
//                             HoistDirectives(session, artifact, shard, ast, component, component.Node, languageSemantics, ServerInteropFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
//                         );

//                         ASTHelpers.Connect(ast, component.ID, hoistedDirectiveStatements.ToArray(), SemanticRole.None, 0);

//                         break;
//                     }
//                 }

//                 if(!isFocus)
//                 {
//                     componentIDsToRemove.Add(component.ID);
//                 }
//             }

//             ASTHelpers.DisableNodes(ast, componentIDsToRemove.ToArray());

//             return result;
//         }
// //         public Result<object> AddSourcesCTExecSourceFiles(Session session, Artifact artifact, Shard shard, List<Component> addedSources, OutFileCollection ofc, CancellationToken token)
// //         {
// //             /*
// //                 (async () => {
// //                     require('server-code.js')

// //                     ....inlined components
                    
// //                 })
// //             */  

// //             Console.WriteLine("🌕 1");
            
// //             var result = new Result<object>();

// //             // [dho] NOTE we operate on a clone of the AST to avoid mutating
// //             // the original AST when we instrument the program for CT execution - 14/07/19
// //             var ast = shard.AST.Clone();

// //             var domain = ASTHelpers.GetRoot(ast);

// //             System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);
// // Console.WriteLine("🌕 2");
// //             var languageSemantics = LanguageSemantics.Of(artifact.TargetLang);

// //             if(languageSemantics == null)
// //             {
// //                 result.AddMessages(new Message(MessageKind.Warning, $"Language semantics not configured for {artifact.TargetLang}, using TypeScript semantics as a fallback")
// //                 {
// //                     Tags = DiagnosticTags
// //                 });

// //                 languageSemantics = LanguageSemantics.TypeScript;
// //             }

// //             var component = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Transformation), CompilerHelpers.NextInternalGUID());

// //             {
// //                 var componentIDsToRemove = new List<string>();
// //                 var importDecls = new List<Node>();
// //                 var hoistedNodes = new Dictionary<string, Node>();
// //                 var content = new List<Node>();

// //                 content.Add(
// //                     NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"
// // module.exports = async () => {{ 
// //     {"await (require('" + string.Join("')()); await (require('", session.CTExecInfo.AbsoluteLibPaths) + "')());"}").Node
// //                 );
// // Console.WriteLine("🌕 3");

// //                 result.AddMessages(
// //                     AddXXXX(session, artifact, shard, ast, addedSources, languageSemantics, token, ref importDecls, ref content, ref componentIDsToRemove)
// //                 );

// // Console.WriteLine("🌕 4");
// //                 content.Add(
// //                     NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"
// // }}").Node
// //                 );

// // Console.WriteLine("🌕 5");
// //                 // [dho] remove the components from the tree because now they have all been inlined - 01/06/19
// //                 ASTHelpers.RemoveNodes(ast, componentIDsToRemove.ToArray());

// //                 // [dho] combine the imports - 01/06/19
// //                 ASTHelpers.Connect(ast, component.ID, importDecls.ToArray(), SemanticRole.None);

// //                 ASTHelpers.Connect(ast, component.ID, content.ToArray(), SemanticRole.None);
// //                 Console.WriteLine("🌕 6");
// //             }

// //             // [dho] add the component containing the inlined app code to the tree - 01/06/19
// //             ASTHelpers.Connect(ast, domain.ID, new [] { component.Node }, SemanticRole.Component);
            
// //             Console.WriteLine("🌕 7");
// //             result.AddMessages(
// //                 ___EmitASTAndAddOutFiles(session, artifact, ast, ofc, token)
// //             );

// // Console.WriteLine("🌕 8");
// //             return result;
// //         }

// //         private Result<object> AddXXXX(Session session, Artifact artifact, Shard shard, RawAST ast, List<Component> focusComponents, BaseLanguageSemantics languageSemantics, CancellationToken token, ref List<Node> importDecls, ref List<Node> content, ref List<string> componentIDsToRemove)
// //         {
            
// //             var result = new Result<object>();

// //             var domain = ASTHelpers.GetRoot(ast);

// //             System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

// //             var hoistedNodes = new Dictionary<string, Node>();
// // Console.WriteLine("🌈 1");
// //             foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, domain.ID))
// //             {
// //                 System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

// //                 var component = ASTNodeFactory.Component(ast, (DataNode<string>)child);
// //                 var isFocus = false;

// //                 foreach(var fc in focusComponents)
// //                 {
// //                     if(fc.ID == component.ID)
// //                     {
// //                         isFocus = true;
// //                         break;
// //                     }
// //                 }

// //                 if(component.Node.Origin as SourceNodeOrigin != null)
// //                 {
// //                     Console.WriteLine("🌈 2");
// //                     var inlinedComponent = result.AddMessages(
// //                         InlineComponent(session, artifact, shard, ast, component, isFocus, languageSemantics, ServerInteropFnIDs, token, ref importDecls, ref content, ref hoistedNodes)
// //                     );
// // Console.WriteLine("🌈 3");
// //                     if(inlinedComponent != null)
// //                     {
// //                         content.Add(
// //                             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"/* COMPONENT : `{component.Name}`*/").Node
// //                         );
// //                         content.Add(inlinedComponent.Node);
// //                     }Console.WriteLine("🌈 4");
// //                 }

// //                 componentIDsToRemove.Add(component.ID);
// //             }
// // Console.WriteLine("🌈 5");
// //             return result;
// //         }
        


//         private Result<object> ___EmitASTAndAddOutFiles(Session session, Artifact artifact, RawAST ast, string artifactRootComponentNodeID, BaseLanguageSemantics languageSemantics, OutFileCollection ofc, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var emitter = new CTExecEmitter(artifactRootComponentNodeID, languageSemantics);
//             {
//                 var emittedFiles = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ast, token));

//                 if (HasErrors(result) || token.IsCancellationRequested) return result;

//                 // [dho] TODO CLEANUP HACK we are calling ourselves a TypeScript bundler, but we want the CT Exec to be
//                 // as fast as possible, so we are removing all types and just changing the file extension to `.js`. 
//                 // CLEANUP once we have a JavaScript emitter and can just use that.. - 12/07/19

//                 foreach (var outFile in emittedFiles)
//                 {
//                     ofc[FileSystem.ParseFileLocation(outFile.Path)] = outFile.Emission;
//                 }
//             }

//             return result;

//         }

//         // private IFileLocation XXXX(OutFile outFile)
//         // {
//         //     var filePath = outFile.Path;

//         //     if (filePath.EndsWith(".ts"))
//         //     {
//         //         filePath = filePath.Substring(0, filePath.Length - ".ts".Length) + ".js";
//         //     }

//         //     return FileSystem.ParseFileLocation(filePath);
//         // }



// //         private Result<Component> TypeScriptInlining(Session session, Artifact artifact, Shard shard, RawAST ast, CancellationToken token)
// //         {
// //             var result = new Result<Component>();

// //             // var shardIndex = session.Shards[artifact.Name].IndexOf(shard);

// //             var domain = ASTHelpers.GetRoot(ast);

// //             System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

// //             var languageSemantics = LanguageSemantics.Of(artifact.TargetLang);

// //             if (languageSemantics == null)
// //             {
// //                 result.AddMessages(new Message(MessageKind.Warning, $"Language semantics not configured for {artifact.TargetLang}, using TypeScript semantics as a fallback")
// //                 {
// //                     Tags = DiagnosticTags
// //                 });

// //                 languageSemantics = LanguageSemantics.TypeScript;
// //             }


// //             var component = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Transformation), EntrypointFileName);

// //             {
// //                 var componentIDsToRemove = new List<string>();
// //                 var importDecls = new List<Node>();
// //                 var iifeContent = new List<Node>();

// //                 var hoistedNodes = new Dictionary<string, Node>();

                
// //                 iifeContent.Add(
// //                     CreateCTAPICode(session, ast, token)
// //                 );
                

// //                 iifeContent.Add(
// //                     NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation),
// // $@"let {CTExecErrorLexeme} = null;
// // try{{").Node
// //                 );


// //                 foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, domain.ID))
// //                 {
// //                     System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

// //                     var c = ASTNodeFactory.Component(ast, (DataNode<string>)child);

// //                     if (c.Node.Origin as SourceNodeOrigin != null)
// //                     {
// //                         var inlinedComponent = result.AddMessages(
// //                             InlineComponent(session, artifact, shard, ast, c, languageSemantics, ServerInteropFnIDs, token, ref importDecls, ref iifeContent, ref hoistedNodes)
// //                         );

// //                         if (inlinedComponent != null)
// //                         {
// //                             iifeContent.Add(
// //                                 NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"/* COMPONENT : `{c.Name}`*/").Node
// //                             );
// //                             iifeContent.Add(inlinedComponent.Node);
// //                         }
// //                     } 

// //                     componentIDsToRemove.Add(child.ID);
// //                 }

// //                 // [dho] remove the components from the tree because now they have all been inlined - 01/06/19
// //                 ASTHelpers.RemoveNodes(ast, componentIDsToRemove.ToArray());

// //                 // [dho] combine the imports - 01/06/19
// //                 ASTHelpers.Connect(ast, component.ID, importDecls.ToArray(), SemanticRole.None);


// //                 iifeContent.Add(
// //                 NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation),
// // $@"}}
// // catch(error){{ 
// //     {CTExecErrorLexeme} = error;
// // }}

// // await {ServerInteropFnIDs.Terminate}({CTExecErrorLexeme});
// // ").Node
// //                 );

// //                 CreateIIFEAndConnectToComponent(session, artifact, shard, ast, component, iifeContent, token);
                
// //             }


// //             // [dho] add the component containing the inlined app code to the tree - 01/06/19
// //             ASTHelpers.Connect(ast, domain.ID, new[] { component.Node }, SemanticRole.Component);

// //             result.Value = component;

// //             return result;
// //         }

        


//         // private void CreateIIFEAndConnectToComponent(Session session, Artifact artifact, RawAST ast, Component component, List<Node> iifeContent, CancellationToken token)
//         // {
//         //     // var result = new Result<object>();

//         //     // var shardIndex = session.Shards[artifact.Name].IndexOf(shard);

//         //     // var domain = ASTHelpers.GetRoot(ast);

//         //     // System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

//         //     // [dho] wrap content in an IIFE - 12/07/19
//         //     var iifeCreation = ASTNodeHelpers.CreateIIFE(ast, iifeContent);
//         //     {
//         //         var lambdaDecl = iifeCreation.Item2;
//         //         {
//         //             var asyncFlag = NodeFactory.Meta(
//         //                 ast,
//         //                 new PhaseNodeOrigin(PhaseKind.Transformation),
//         //                 MetaFlag.Asynchronous
//         //             );

//         //             ASTHelpers.Connect(ast, lambdaDecl.ID, new[] { asyncFlag.Node }, SemanticRole.Meta);
//         //         }

//         //         var iife = iifeCreation.Item1;

//         //         ASTHelpers.Connect(ast, component.ID, new[] { iife.Node }, SemanticRole.None);
//         //     }
            
//         //     // // [dho] add the component containing the inlined app code to the tree - 01/06/19
//         //     // ASTHelpers.Connect(ast, domain.ID, new[] { component.Node }, SemanticRole.Component);

//         //     // result.Value = component;

//         //     // return result;
//         // }

//         private string CreateCTExecSessionAPICode(Session session)
//         {
//             var host = session.Server.IPAddress;
//             var port = session.Server.Port;

//             return (
// $@"
// module.exports = async function () {{
//     async function createClient(host, port)
//     {{
//         // if(_client) return _client;

//         const socket = new require('net').Socket();
        
//         await new Promise((resolve, reject) => {{
//             socket.connect(port, host, resolve);
//         }})
        

//         let continueExecution = true;

//         function sendMessage(id, message)
//         {{
//             return new Promise((resolve, reject) => {{
//                 // console.log('send message ' + id + ', continue exec? ' + continueExecution);
//                 // [dho] flag to avoid us doing unnecessary work if the server
//                 // tells us to stop - 25/11/19
//                 if(!continueExecution) return;

//                 // console.log(`SENDING :::: ${{message}}{DuplexSocketServer.MessageSentinel}`)

//                 socket.write(`${{message}}{DuplexSocketServer.MessageSentinel}`);
            
//                 socket.on('data', buffer => {{
                    
//                     const data = buffer.toString('utf8');

//                     const serializedJSON = data.substring(0, data.length - '{DuplexSocketServer.MessageSentinel}'.length);

//                     const {{ ok, id : respMessageID, data : respData }} = JSON.parse(serializedJSON);


//                     if(!ok)
//                     {{
//                         // [dho] flag to avoid us doing unnecessary work if the server
//                         // tells us to stop - 25/11/19
//                         continueExecution = false;
//                         return;
//                     }}

//                     if(respMessageID === id)
//                     {{
                        
//                         // console.log('ok:' + ok + ', original id: ' + id + ', response message id: ' + respMessageID + ', continue exec? ' + continueExecution);
                        
//                         if(continueExecution)
//                         {{
//                             resolve(respData);
//                         }}
//                     }}

//                 }});
            
//                 socket.on('error', (err) => {{
//                     // console.log('GOT ERR', err);
//                     if(continueExecution)
//                     {{
//                         // [dho] 'ECONNRESET' means the server hung up - 26/11/19
//                         if(err.syscall !== 'ECONNRESET')
//                         {{
//                             reject(err)
//                         }}
//                     }}
                    
//                     // reject(err) || destroy()
//                 }});
//             }});
//         }}

//         function destroy()
//         {{
//             socket.destroy();
//             _client = null;
//         }}
        
//         return _client = {{ sendMessage, destroy }};
//     }}
//     // let _client = null;



//     async function {CTAPISymbols.AddCapability}(name, value)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         if(Array.isArray(value))
//         {{
//             return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddCapability}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}{(int)ConfigurationPrimitive.StringArray}{CTProtocolHelpers.ArgumentDelimiter}${{value.join('{CTProtocolHelpers.ArgumentDelimiter}')}})`);
//         }}
//         else 
//         {{
//             return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddCapability}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}{(int)ConfigurationPrimitive.String}{CTProtocolHelpers.ArgumentDelimiter}${{value}})`);
//         }}
//     }}

//     async function {CTAPISymbols.AddDependency}(name, version)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         if(arguments.length === 1)
//         {{
//             return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddDependency}(${{name}})`);
//         }}
//         else
//         {{
//             return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddDependency}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}${{version}})`);
//         }}
//     }}

//     async function {CTAPISymbols.AddEntitlement}(name, value)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         if(Array.isArray(value))
//         {{
//             return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddEntitlement}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}{(int)ConfigurationPrimitive.StringArray}{CTProtocolHelpers.ArgumentDelimiter}${{value.join('{CTProtocolHelpers.ArgumentDelimiter}')}})`);
//         }}
//         else 
//         {{
//             return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddEntitlement}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}{(int)ConfigurationPrimitive.String}{CTProtocolHelpers.ArgumentDelimiter}${{value}})`);
//         }}
//     }}

//     async function {CTAPISymbols.AddPermission}(name, description)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddPermission}(${{name}}{CTProtocolHelpers.ArgumentDelimiter}${{description || ''}})`);
//     }}

//     async function {CTAPISymbols.AddRes}(parentDirPath, sourcePath)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddRes}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{sourcePath}})`);
//     }}

//     async function {CTAPISymbols.AddRawSources}(parentDirPath, ...includedPaths)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddRawSources}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{includedPaths.join('{CTProtocolHelpers.ArgumentDelimiter}')}})`);
//     }}

//     async function {CTAPISymbols.AddSources}(parentDirPath, ...includedPaths)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         const newSources = await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddSources}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{includedPaths.join('{CTProtocolHelpers.ArgumentDelimiter}')}})`);
    
//         await consumeNewSources(newSources);

//         console.log('😈 Done addSources');
//     }}

//     async function {CTAPISymbols.AddAsset}(parentDirPath, role, sourcePath)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddAsset}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{role}}{CTProtocolHelpers.ArgumentDelimiter}${{sourcePath}})`);
//     }}

//     async function {CTAPISymbols.AddShard}(parentDirPath, role, sourcePath)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         const newSources = await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.AddShard}(${{parentDirPath}}{CTProtocolHelpers.ArgumentDelimiter}${{role}}{CTProtocolHelpers.ArgumentDelimiter}${{sourcePath}})`);
    
//         await consumeNewSources(newSources);

//         console.log('😈 Done addShard');
//     }}

//     async function {CTAPISymbols.SetDisplayName}(name)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.SetDisplayName}(${{name}})`);
//     }}

//     async function {CTAPISymbols.SetTeamName}(name)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.SetTeamName}(${{name}})`);
//     }}

//     async function {CTAPISymbols.SetVersion}(version)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.SetVersion}(${{version}})`);
//     }}

//     async function {CTAPISymbols.IsArtifactName}(name)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.IsArtifactName}(${{name}})`);
//     }}

//     async function {CTAPISymbols.IsTargetLanguage}(lang)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.IsTargetLanguage}(${{lang}})`);
//     }}

//     async function {CTAPISymbols.IsTargetPlatform}(platform)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.IsTargetPlatform}(${{platform}})`);
//     }}

//     async function {CTAPISymbols.DeleteNode}(nodeID)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.DeleteNode}(${{nodeID}})`);
//     }}

//     async function {CTAPISymbols.ReplaceNodeByCodeConstant}(removeeID, codeConstant)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.ReplaceNodeByCodeConstant}(${{removeeID}}{CTProtocolHelpers.ArgumentDelimiter}${{codeConstant}})`);
//     }}

//     async function {CTAPISymbols.InsertImmediateSiblingFromValueAndDeleteNode}(insertionPointID, value, removeeID)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;

//         return await (await createClient(""{host}"", {port})).sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.InsertImmediateSiblingAndFromValueAndDeleteNode}(${{insertionPointID}}{CTProtocolHelpers.ArgumentDelimiter}${{value.constructor.name}}{CTProtocolHelpers.ArgumentDelimiter}${{value}}{CTProtocolHelpers.ArgumentDelimiter}${{removeeID}})`);
//     }}

//     async function {CTAPISymbols.Terminate}(error)
//     {{
//         const {{ {ArtifactNameSymbolLexeme}, {ShardIndexSymbolLexeme}, {MessageIDSymbolLexeme} }} = this;
//         const c = await (await createClient(""{host}"", {port}));

//         if(!!error)
//         {{
//             const statusCode = 1;
//             const {{ message, stack }} = error;

//             const result = c.sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.Terminate}(${{statusCode}}{CTProtocolHelpers.ArgumentDelimiter}${{message}}{CTProtocolHelpers.ArgumentDelimiter}${{stack}})`);
    
//             c.destroy();

//             return result;
//         }}
//         else
//         {{
//             const statusCode = 0;

//             const result = c.sendMessage({MessageIDSymbolLexeme}, `{CTCommandPreamble}{(int)CTProtocolCommandKind.Terminate}(${{statusCode}})`);
    
//             c.destroy();

//             return result;
//         }}
//     }}

//     async function consumeNewSources(newSources)
//     {{
//         console.log('😈 Consume new sources');
//         return Promise.all(
//             {""/* [dho] we load the async exported function and invoke it - 26/11/19 */}
//             newSources.map(async newSource => require(newSource)())
//         );
//     }}

//     global.{ServerInteropHandleLexeme} = {{ 
//         {CTAPISymbols.AddCapability},
//         {CTAPISymbols.AddDependency},
//         {CTAPISymbols.AddEntitlement},
//         {CTAPISymbols.AddAsset},
//         {CTAPISymbols.AddPermission},
//         {CTAPISymbols.AddRawSources},
//         {CTAPISymbols.AddRes},
//         {CTAPISymbols.AddSources}, 
//         {CTAPISymbols.AddShard}, 
//         {CTAPISymbols.IsArtifactName},
//         {CTAPISymbols.IsTargetLanguage},
//         {CTAPISymbols.IsTargetPlatform},
//         {CTAPISymbols.SetDisplayName},
//         {CTAPISymbols.SetTeamName},
//         {CTAPISymbols.SetVersion},
//         {CTAPISymbols.ReplaceNodeByCodeConstant},
//         {CTAPISymbols.InsertImmediateSiblingFromValueAndDeleteNode}, 
//         {CTAPISymbols.DeleteNode},
//         {CTAPISymbols.Terminate}
//     }};

//     // [dho] make the public compiler API symbols available in scope as unqualified names - 12/07/19
//     global.{CTAPISymbols.AddCapability} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddCapability};
//     global.{CTAPISymbols.AddDependency} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddDependency};
//     global.{CTAPISymbols.AddEntitlement} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddEntitlement};
//     global.{CTAPISymbols.AddAsset} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddAsset};
//     global.{CTAPISymbols.AddPermission} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddPermission};
//     global.{CTAPISymbols.AddRes} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddRes};
//     global.{CTAPISymbols.AddRawSources} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddRawSources};
//     global.{CTAPISymbols.AddSources} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddSources};
//     global.{CTAPISymbols.AddShard} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.AddShard};
//     global.{CTAPISymbols.IsArtifactName} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.IsArtifactName};
//     global.{CTAPISymbols.IsTargetLanguage} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.IsTargetLanguage};
//     global.{CTAPISymbols.IsTargetPlatform} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.IsTargetPlatform};
//     global.{CTAPISymbols.SetDisplayName} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.SetDisplayName};
//     global.{CTAPISymbols.SetTeamName} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.SetTeamName};
//     global.{CTAPISymbols.SetVersion} = global.{ServerInteropHandleLexeme}.{CTAPISymbols.SetVersion};
// }};
// "
//            );
//         }


//         private (Association, InterimSuspension) CreateAwait(RawAST ast, Node operand)
//         {
//             var awaitExp = NodeFactory.InterimSuspension(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//             {
//                 ASTHelpers.Connect(ast, awaitExp.ID, new[] { operand }, SemanticRole.Operand);
//             }

//             var parentheses = NodeFactory.Association(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//             {
//                 ASTHelpers.Connect(ast, parentheses.ID, new[] { awaitExp.Node }, SemanticRole.Subject);
//             }

//             return (parentheses, awaitExp);
//         }

//         private InvocationArgument CreateInvocationArgument(RawAST ast, Node value)
//         {
//             var arg = NodeFactory.InvocationArgument(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//             {
//                 ASTHelpers.Connect(ast, arg.ID, new[] { value }, SemanticRole.Value);
//             }

//             return arg;
//         }

// //         // [dho] NOTE `isFocus` means, is this a component we are preparing to evaluate at compile time, or is it just a component that is in the 
// //         // tree from a previous round of CT Exec - 25/11/19
// //         private Result<DataValueDeclaration> InlineComponent(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, bool isFocus, BaseLanguageSemantics languageSemantics, ServerInteropFunctionIdentifiers serverInteropFnIDs, CancellationToken token, ref List<Node> imports, ref List<Node> hoistedDirectiveStatements, ref Dictionary<string, Node> hoistedNodes)
// //         {
// //             var result = new Result<DataValueDeclaration>();
// //             Console.WriteLine("🐙 1");
// //             if(isFocus)
// //             {
// //                 result.AddMessages(
// //                     TransformCTAPIInvocations(session, artifact, shard, ast, component, languageSemantics, token)
// //                 );

// // Console.WriteLine("🐙 2");
// //                 // [dho] NOTE do this first to ensure we still compute directives inside nodes that we do not actually emit
// //                 // in the CT exec program, eg. types, view declarations etc. - 12/07/19
// //                 result.AddMessages(
// //                     HoistDirectives(session, artifact, shard, ast, component, component.Node, languageSemantics, serverInteropFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
// //                 );
// //             }

// // Console.WriteLine("🐙 3");
// //             // [dho] because some of the types may exist in the target platform but not have explicit definitions,
// //             // we will just remove all the types for now and use TypeScript as a way to evalute the code rather
// //             // than check the correctness of it - 11/07/19
// //             result.AddMessages(
// //                 PrepareComponentForCTExec(session, artifact, ast, component, isFocus, languageSemantics, token)
// //             );
// // Console.WriteLine("🐙 4");
// //             var inlinedDVDecl = NodeFactory.DataValueDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

// //             {
// //                 var constantFlag = NodeFactory.Meta(
// //                     ast,
// //                     new PhaseNodeOrigin(PhaseKind.Transformation),
// //                     MetaFlag.Constant
// //                 );

// //                 ASTHelpers.Connect(ast, inlinedDVDecl.ID, new[] { constantFlag.Node }, SemanticRole.Meta);
// //             }

// //             // [dho] create name of namespace - 01/06/19
// //             {
// //                 var classIdentifier = ToInlinedIdentifier(session, component);

// //                 var inlinedDVDeclNameLexeme = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), classIdentifier);

// //                 ASTHelpers.Connect(ast, inlinedDVDecl.ID, new[] { inlinedDVDeclNameLexeme.Node }, SemanticRole.Name);
// //             }

// // Console.WriteLine("🐙 5");
// //             var iifeContent = new List<Node>();
// //             {
// //                 var exportedSymbols = new List<string>();

// //                 var inlinerInfo = result.AddMessages(
// //                     ClientInlining.GetInlinerInfo(session, ast, component.Node, languageSemantics, token)
// //                 );
// // Console.WriteLine("🐙 6");
// //                 if (HasErrors(result) || token.IsCancellationRequested) return result;

// //                 if (inlinerInfo.ImportDeclarations?.Count > 0)
// //                 {
// //                     var importsSortedByType = result.AddMessages(
// //                         ImportHelpers.SortImportDeclarationsByType(session, artifact, ast, component, inlinerInfo.ImportDeclarations, languageSemantics, token)
// //                     );
// // Console.WriteLine("🐙 7");
// //                     if (!HasErrors(result) && !token.IsCancellationRequested)
// //                     {
// //                         foreach (var im in importsSortedByType.SempilerImports)
// //                         {
// //                             // [dho] remove the "sempiler" import because it is a _fake_
// //                             // import we just use to be sure that the symbols the user refers
// //                             // to are for sempiler, and not something in global scope for a particular target platform - 24/06/19
// //                             ASTHelpers.RemoveNodes(ast, new[] { im.ImportDeclaration.ID });
// //                         }

// //                         foreach (var im in importsSortedByType.ComponentImports)
// //                         {
// //                             var importedComponentInlinedName = ToInlinedIdentifier(session, im.Component);

// //                             result.AddMessages(
// //                                 ImportHelpers.QualifyImportReferences(ast, im, importedComponentInlinedName)
// //                             );

// //                             // [dho] remove the import because all components are inlined into the same output file - 24/06/19
// //                             ASTHelpers.RemoveNodes(ast, new[] { im.ImportDeclaration.ID });
// //                         }

// //                         foreach (var im in importsSortedByType.PlatformImports)
// //                         {
// //                             // [dho] ignoring platform imports because they are references to packages that
// //                             // exist in the target platform, whereas we are still inside the compiler at CT Exec time - 11/07/19
// //                             //
// //                             // [dho] NOTE we are NOT removing platform imports because we want them to be in the final emission still! - 11/07/19
// //                             result.AddMessages(new NodeMessage(MessageKind.Info, $"Package '{im.ImportInfo.SpecifierLexeme}' is a platform import and will not be available during compile time execution", im.ImportDeclaration.Node)
// //                             {
// //                                 Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(im.ImportDeclaration.Node.Origin),
// //                                 Tags = DiagnosticTags
// //                             });


// //                             // imports.Add(im.ImportDeclaration.Node);
// //                         }
// //                     }
// //                 }
// //                 Console.WriteLine("🐙 8");

// //                 if (inlinerInfo.ObjectTypeDeclarations?.Count > 0)
// //                 {
// //                     var objectTypes = new Node[inlinerInfo.ObjectTypeDeclarations.Count];

// //                     for (int i = 0; i < objectTypes.Length; ++i)
// //                     {
// //                         var member = inlinerInfo.ObjectTypeDeclarations[i];

// //                         TransformObjectTypeDeclarationForCTExec(ast, member);

// //                         objectTypes[i] = member.Node;

// //                         var symbol = GetExportedSymbolNameLexeme(member);
// //                         if (symbol != null)
// //                         {
// //                             exportedSymbols.Add(symbol);
// //                         }
// //                     }

// //                     iifeContent.AddRange(objectTypes);
// //                 }
// // Console.WriteLine("🐙 9");
// //                 if (inlinerInfo.FunctionDeclarations?.Count > 0)
// //                 {
// //                     var fnDecls = new Node[inlinerInfo.FunctionDeclarations.Count];

// //                     for (int i = 0; i < fnDecls.Length; ++i)
// //                     {
// //                         var member = inlinerInfo.FunctionDeclarations[i];
// //                         fnDecls[i] = member.Node;

// //                         var symbol = GetExportedSymbolNameLexeme(member);
// //                         if (symbol != null)
// //                         {
// //                             exportedSymbols.Add(symbol);
// //                         }
// //                     }

// //                     iifeContent.AddRange(fnDecls);
// //                 }
// // Console.WriteLine("🐙 10");
// //                 {
// //                     var initFnDecl = NodeFactory.FunctionDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
// //                     {
// //                         {
// //                             // // [dho] make the class static - 11/07/19
// //                             // var staticFlag = NodeFactory.Meta(
// //                             //     ast,
// //                             //     new PhaseNodeOrigin(PhaseKind.Transformation),
// //                             //     MetaFlag.Static
// //                             // );

// //                             // [dho] make the class public - 11/07/19
// //                             var publicFlag = NodeFactory.Meta(
// //                                 ast,
// //                                 new PhaseNodeOrigin(PhaseKind.Transformation),
// //                                 MetaFlag.WorldVisibility
// //                             );

// //                             ASTHelpers.Connect(ast, initFnDecl.ID, new[] { publicFlag.Node/* , staticFlag.Node*/ }, SemanticRole.Meta);
// //                         }

// //                         var name = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $"{component.ID}_init");
// //                         ASTHelpers.Connect(ast, initFnDecl.ID, new[] { name.Node }, SemanticRole.Name);

// //                         var block = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

// //                         ASTHelpers.Connect(ast, block.ID, inlinerInfo.ExecOnLoads.ToArray(), SemanticRole.Content);

// //                         ASTHelpers.Connect(ast, initFnDecl.ID, new[] { block.Node }, SemanticRole.Body);
// //                     }

// //                     iifeContent.Add(initFnDecl.Node);
// //                 }
// // Console.WriteLine("🐙 11");

// //                 iifeContent.Add(
// //                     NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $"return {{ {string.Join(", ", exportedSymbols)} }}").Node
// //                 );
// //             }

// //             {
// //                 var (iife, lambdaDecl) = ASTNodeHelpers.CreateIIFE(ast, iifeContent);

// //                 // var asyncFlag = NodeFactory.Meta(
// //                 //     ast,
// //                 //     new PhaseNodeOrigin(PhaseKind.Transformation),
// //                 //     MetaFlag.Asynchronous
// //                 // );

// //                 // ASTHelpers.Connect(ast, lambdaDecl.ID, new[] { asyncFlag.Node }, SemanticRole.Meta);

// //                 ASTHelpers.Connect(ast, inlinedDVDecl.ID, new[] { iife.Node }, SemanticRole.Initializer);
// //             }

// //             Console.WriteLine("🐙 12");


// //             result.Value = inlinedDVDecl;

// //             return result;
// //         }


//         private void TransformObjectTypeDeclarationForCTExec(RawAST ast, ObjectTypeDeclaration objectTypeDecl)
//         {
//             // var fieldDecls = new List<FieldDeclaration>();

//             // foreach(var member in objectTypeDecl.Members)
//             // {
//             //     if(member.Kind == SemanticKind.FieldDeclaration)
//             //     {
//             //         var fieldDecl = ASTNodeFactory.FieldDeclaration(ast, member);

//             //         fieldDecls.Add(fieldDecl);
//             //     }
//             // }


//             // [dho] we inject a constructor that just throws an error, which will tell the user that they cannot
//             // `new` up objects during compile time execution - 12/07/19
//             var ctor = NodeFactory.ConstructorDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//             {
//                 var ctorBody = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//                 {
//                     ASTHelpers.Connect(ast, ctorBody.ID, new[] {
//                         NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "throw new Error('Cannot allocate new instances at compile time')").Node
//                     }, SemanticRole.Content);
//                 }

//                 ASTHelpers.Connect(ast, ctor.ID, new[] { ctorBody.Node }, SemanticRole.Body);
//             }
//             ASTHelpers.Connect(ast, objectTypeDecl.ID, new[] { ctor.Node }, SemanticRole.Member);
//         }

//         private string GetExportedSymbolNameLexeme(ASTNode member)
//         {
//             if ((MetaHelpers.ReduceFlags(member) & MetaFlag.WorldVisibility) == MetaFlag.WorldVisibility)
//             {
//                 var nameEdgeNodes = ASTHelpers.QueryEdgeNodes(member.AST, member.ID, SemanticRole.Name);

//                 if (nameEdgeNodes.Length == 1)
//                 {
//                     var nameNode = nameEdgeNodes[0];

//                     if (nameNode.Kind == SemanticKind.Identifier)
//                     {
//                         return ASTNodeFactory.Identifier(member.AST, (DataNode<string>)nameNode).Lexeme;
//                     }
//                 }
//             }

//             return null;
//         }

// //         private Result<object> PrepareComponentForCTExec(Session session, Artifact artifact, RawAST ast, Component component, bool keepCTDirectives, BaseLanguageSemantics languageSemantics, CancellationToken token)
// //         {
// //             var result = new Result<object>();
// //             var nodesToRemove = new List<string>();
// // var x1 = Main.StartTimer("XXX PrepareComponentForCTExec");
// // Console.WriteLine("🦷 1");
// //             ASTHelpers.PreOrderTraversal(session, ast, component.Node, node =>
// //             {

// //                 if (node.Kind == SemanticKind.Component) return true;

// //                 if (node.Kind == SemanticKind.Directive)
// //                 {
// //                     if(!keepCTDirectives)
// //                     {
// //                         nodesToRemove.Add(node.ID);
// //                     }

// //                     return false;
// //                 }
// //                 // if(node.Kind == SemanticKind.Meta)
// //                 // {
// //                 //     // [dho] ignore any value types (`structs`) and treat them as classes .. 
// //                 //     // TODO CHECK if this is a good idea?! - 04/08/19
// //                 //     if(ASTNodeFactory.Meta(ast, (DataNode<MetaFlag>)node).Flags == MetaFlag.ValueType)
// //                 //     {
// //                 //         nodesToRemove.Add(node.ID);
// //                 //         return false;
// //                 //     }
// //                 // }

// //                 if (node.Kind == SemanticKind.ForcedCast || node.Kind == SemanticKind.SafeCast)
// //                 {
// //                     var subject = ASTHelpers.QueryEdgeNodes(ast, node.ID, SemanticRole.Subject)[0];

// //                     ASTHelpers.Replace(ast, subject.ID, new[] {
// //                         NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "any").Node
// //                     });

// //                     return true;
// //                 }


// //                 if (node.Kind == SemanticKind.InterfaceDeclaration ||
// //                     node.Kind == SemanticKind.TypeAliasDeclaration ||
// //                     // [dho] eg. `declare let x : any` - 14/10/19
// //                     node.Kind == SemanticKind.CompilerHint)
// //                 {
// //                     nodesToRemove.Add(node.ID);
// //                     return false;
// //                 }

// //                 // [dho] TODO finish swapping enum declarations for the equivalent JS
// //                 // map declaration. Caused a StackOverflowException that I didn't want to look
// //                 // into now.. so just continue the work below to debug that - 28/08/19 
// //                 // if(node.Kind == SemanticKind.EnumerationTypeDeclaration)
// //                 // {

// //                 //     var enumDecl = ASTNodeFactory.EnumerationTypeDeclaration(ast, node);

// //                 //     var dvDecl = NodeFactory.DataValueDeclaration(ast, enumDecl.Origin);

// //                 //     var name = enumDecl.Name;

// //                 //     System.Diagnostics.Debug.Assert(name.Kind == SemanticKind.Identifier);
// //                 //     var nameLexeme = ASTNodeFactory.Identifier(ast, (DataNode<string>)name).Lexeme;

// //                 //     ASTHelpers.Connect(ast, dvDecl.ID, new [] { name }, SemanticRole.Name);


// //                 //     var iifeContent = new List<Node>();
// //                 //     {





// //                 //     }

// //                 //     var (iifeInvocation, iifeLambdaDecl) = CreateIIFE(ast, iifeContent);    
// //                 //     {
// //                 //         var logicalOr = NodeFactory.LogicalOr(ast, node.Origin);
// //                 //         {
// //                 //             var leftOperand = NodeFactory.Identifier(ast, node.Origin, nameLexeme);

// //                 //             var rightOperand = NodeFactory.Association(ast, node.Origin);
// //                 //             {
// //                 //                 var assignment = NodeFactory.Assignment(ast, node.Origin);
// //                 //                 {
// //                 //                     ASTHelpers.Connect(ast, assignment.ID, new [] { 
// //                 //                         NodeFactory.Identifier(ast, node.Origin, nameLexeme).Node 
// //                 //                     }, SemanticRole.Storage);

// //                 //                     ASTHelpers.Connect(ast, assignment.ID, new [] { 
// //                 //                         NodeFactory.DynamicTypeConstruction(ast, node.Origin).Node 
// //                 //                     }, SemanticRole.Value);
// //                 //                 }
// //                 //             }
// //                 //         }

// //                 //         ASTHelpers.Connect(ast, iifeInvocation.ID, new [] { 
// //                 //             CreateInvocationArgument(ast, logicalOr.Node).Node 
// //                 //         }, SemanticRole.Argument);

// //                 //         var paramDecl = NodeFactory.ParameterDeclaration(ast, node.Origin);
// //                 //         {
// //                 //             ASTHelpers.Connect(ast, paramDecl.ID, new [] { 
// //                 //                 NodeFactory.Identifier(ast, node.Origin, nameLexeme).Node
// //                 //             }, SemanticRole.Name);
// //                 //         }

// //                 //         ASTHelpers.Connect(ast, iifeLambdaDecl.ID, new [] { 
// //                 //             paramDecl.Node
// //                 //         }, SemanticRole.Parameter);
// //                 //     }




// //                 //     //     var SessionUXType;
// //                 //     // (function (SessionUXType) {
// //                 //     //     SessionUXType[SessionUXType["landlord"] = 0] = "landlord";
// //                 //     //     SessionUXType[SessionUXType["renter"] = 1] = "renter";
// //                 //     // })(SessionUXType || (SessionUXType = {}));


// //                 //     ASTHelpers.Replace(ast, node.ID, new [] {
// //                 //         dvDecl.Node,
// //                 //         iifeInvocation.Node
// //                 //     });


// //                 //     // var objDecl = NodeFactory.DynamicTypeConstruction(ast, enumDecl.Origin);

// //                 //     // // ASTHelpers.Connect(ast, objDecl.ID, enumDecl.Members, SemanticRole.Member);
// //                 //     // foreach(var em in enumDecl.Members)
// //                 //     // {
// //                 //     //     System.Diagnostics.Debug.Assert(em.Kind == SemanticKind.EnumerationMemberDeclaration);

// //                 //     //     var enumMemberDecl = ASTNodeFactory.EnumerationMemberDeclaration(ast, em);

// //                 //     //     var fieldDecl = NodeFactory.FieldDeclaration(ast, em.Origin);

// //                 //     //     ASTHelpers.Connect(ast, fieldDecl.ID, new [] { enumMemberDecl.Name }, SemanticRole.Name);

// //                 //     //     if(enumMemberDecl.Initializer != null)
// //                 //     //     {
// //                 //     //         ASTHelpers.Connect(ast, fieldDecl.ID, new [] { enumMemberDecl.Initializer }, SemanticRole.Initializer);
// //                 //     //     }

// //                 //     //     ASTHelpers.Connect(ast, objDecl.ID, new [] { fieldDecl.Node }, SemanticRole.Member);



// //                 //     // }


// //                 //     // ASTHelpers.Connect(ast, dvDecl.ID, new [] { objDecl.Node }, SemanticRole.Initializer);

// //                 //     // ASTHelpers.Replace(ast, node.ID, new [] {
// //                 //     //     dvDecl.Node
// //                 //     // });

// //                 //     return false;
// //                 // }

// //                 if (node.Kind == SemanticKind.FieldDeclaration ||
// //                     node.Kind == SemanticKind.MethodDeclaration ||
// //                     node.Kind == SemanticKind.MethodSignature ||
// //                     node.Kind == SemanticKind.ConstructorDeclaration ||
// //                     node.Kind == SemanticKind.ConstructorSignature)
// //                 {
// //                     // [dho] if it is not static then remove it - 12/07/19
// //                     if ((MetaHelpers.ReduceFlags(ast, node) & MetaFlag.Static) == 0)
// //                     {
// //                         nodesToRemove.Add(node.ID);
// //                         return false;
// //                     }

// //                     return true;
// //                 }

// //                 var pos = ASTHelpers.GetPosition(ast, node.ID);

// //                 if (pos.Role == SemanticRole.Type ||
// //                     pos.Role == SemanticRole.Template ||
// //                     pos.Role == SemanticRole.Interface)
// //                 {
// //                     nodesToRemove.Add(node.ID);
// //                     return false;
// //                 }


// //                 var encScope = languageSemantics.GetEnclosingScopeStart(ast, node, token);

// //                 if (!languageSemantics.IsFunctionLikeDeclarationStatement(ast, encScope) &&
// //                     !languageSemantics.IsStaticallyComputable(session, artifact, ast, node, token))
// //                 {
// //                     if (languageSemantics.IsValueExpression(ast, node))
// //                     {
// //                         // [dho] we strip out any dynamism in the compile time program - 11/07/19
// //                         ASTHelpers.Replace(ast, node.ID, new[] {
// //                             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "void 0").Node
// //                         });
// //                     }
// //                     else
// //                     {
// //                         nodesToRemove.Add(node.ID);
// //                     }

// //                     return false;
// //                 }


// //                 return true;

// //             }, token);

// //             ASTHelpers.RemoveNodes(ast, nodesToRemove.ToArray());

// // Console.WriteLine("🦷 2");
// // Main.StopTimer(x1);
// //             return result;
// //         }

//         private Result<object> HoistDirectives(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, Node node, BaseLanguageSemantics languageSemantics, ServerInteropFunctionIdentifiers serverInteropFnIDs, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
//         {
//             var result = new Result<object>();

//             foreach (var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, node.ID))
//             {
//                 // [dho] compile time exec needs to have happened on the children first in case the
//                 // parent node relies on them (and this is generally what is expected in programming anyway!) - 11/07/19
//                 result.AddMessages(
//                     HoistDirectives(session, artifact, shard, ast, component, child, languageSemantics, serverInteropFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
//                 );
//             }

//             if (node.Kind == SemanticKind.Directive)
//             {
//                 var directiveLexeme = ASTNodeHelpers.GetLexeme(node).Replace("*/", "*\\/");

//                 hoistedDirectiveStatements.Add(
//                     NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $@"/* DIRECTIVE : `{directiveLexeme}`*/").Node
//                 );

//                 var directive = ASTNodeFactory.Directive(ast, (DataNode<string>)node);

//                 // [dho] eg. `#compiler ...` - 11/07/19
//                 if (directive.Name == CTDirective.CodeExec)
//                 {
//                     result.AddMessages(
//                         TransformCTCodeExecDirective(session, artifact, shard, ast, component, directive, languageSemantics, serverInteropFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
//                     );
//                 }
//                 else if (directive.Name == CTDirective.Emit)
//                 {
//                     result.AddMessages(
//                         TransformCTEmitDirective(session, artifact, shard, ast, component, directive, languageSemantics, serverInteropFnIDs, hoistedDirectiveStatements, hoistedNodes, token)
//                     );
//                 }
//                 else if (CTDirective.IsCTDirectiveName(directive.Name))
//                 {
//                     result.AddMessages(
//                         CreateUnsupportedFeatureResult(directive.Node, $"compile time {directive.Name} directives")
//                     );
//                 }
//                 else
//                 {
//                     // [dho] leave the directive as is, but just do not include it at compile time for
//                     // evaluation as we do not recognize it? - 11/07/19
//                 }
//             }

//             return result;
//         }



//         private Result<object> TransformCTCodeExecDirective(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, Directive directive, BaseLanguageSemantics languageSemantics, ServerInteropFunctionIdentifiers serverInteropFnIDs, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var subject = directive.Subject;

//             var content = new List<Node>();

//             // [dho] have to hoist all dependencies recursively - 11/07/19
//             result.AddMessages(
//                 HoistDependencies(session, artifact, ast, subject, languageSemantics, content, hoistedNodes, token)
//             );


//             var inv = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//             {
//                 var invSubject = default(Node);
//                 var invArguments = new List<Node>();

//                 // [dho] NOTE we check whether the #directive is a value position, NOT the subject
//                 // of the directive because we are working out whether the purpose of running this #directive
//                 // was to generate a value - 11/07/19
//                 if (languageSemantics.IsValueExpression(ast, directive.Node))
//                 {
//                     var hoistedNameLexeme = directive.ID;

//                     invSubject = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), serverInteropFnIDs.InsertImmediateSiblingFromValueAndDeleteNode).Node;

//                     invArguments.Add(
//                         CreateInvocationArgument(ast,
//                             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), $"\"{directive.ID}\"").Node
//                         ).Node
//                     );

//                     var dvDecl = NodeFactory.DataValueDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//                     {
//                         ASTHelpers.Connect(ast, dvDecl.ID, new[] {
//                             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), hoistedNameLexeme).Node
//                         }, SemanticRole.Name);

//                         ASTHelpers.Connect(ast, dvDecl.ID, new[] { subject }, SemanticRole.Initializer);
//                     }


//                     content.Add(dvDecl.Node);

//                     invArguments.Add(
//                         CreateInvocationArgument(ast,
//                             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
//                         ).Node
//                     );

//                     invArguments.Add(
//                         CreateInvocationArgument(ast,
//                             NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
//                         ).Node
//                     );

//                     // [dho] make the original location point to the new hoisted declaration - 11/07/19
//                     // ASTHelpers.Replace(ast, directive.ID, new [] {
//                     //     NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), hoistedNameLexeme).Node
//                     // });
//                 }
//                 else
//                 {
//                     content.Add(subject);

//                     invSubject = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), serverInteropFnIDs.DeleteNode).Node;

//                     invArguments.Add(
//                         CreateInvocationArgument(ast,
//                             NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
//                         ).Node
//                     );
//                 }


//                 ASTHelpers.Connect(ast, inv.ID, new[] { invSubject }, SemanticRole.Subject);
//                 ASTHelpers.Connect(ast, inv.ID, invArguments.ToArray(), SemanticRole.Argument);
//             }

   
//             BindSubjectToPassContext(session, artifact, shard, ast, inv.Node, token);

//             InterimSuspension awaitInv = CreateAwait(ast, inv.Node).Item2;

//             MarkAsCTExec(ast, awaitInv.Node);

//             // [dho] NOTE by this point, the static dependencies will have been renamed 
//             // and appear in the preceding statements - 11/07/19
//             content.Add(awaitInv.Node);

//             // var block = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

//             // ASTHelpers.Connect(ast, block.ID, content.ToArray(), SemanticRole.Content);

//             // hoistedDirectiveStatements.Add(block.Node);
//             hoistedDirectiveStatements.AddRange(content);

//             ASTHelpers.DisableNodes(ast, new[] { directive.ID });

//             return result;
//         }

//         private Result<object> TransformCTEmitDirective(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, Directive directive, BaseLanguageSemantics languageSemantics, ServerInteropFunctionIdentifiers serverInteropFnIDs, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var subject = directive.Subject;

//             var content = new List<Node>();

//             // [dho] have to hoist all dependencies recursively - 11/07/19
//             result.AddMessages(
//                 HoistDependencies(session, artifact, ast, subject, languageSemantics, content, hoistedNodes, token)
//             );


//             var inv = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//             {
//                 var invSubject = default(Node);
//                 var invArguments = new List<Node>();

//                 var hoistedNameLexeme = directive.ID;

//                 invSubject = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), serverInteropFnIDs.ReplaceNodeByCodeConstant).Node;

//                 invArguments.Add(
//                     CreateInvocationArgument(ast,
//                         NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), directive.ID).Node
//                     ).Node
//                 );


//                 // if(subject.Kind == SemanticKind.InterpolatedString)
//                 // {
//                 //     var interp = ASTNodeFactory.InterpolatedString(ast, subject);

//                 //     foreach(var member in interp.Members)
//                 //     {
//                 //         if(member.Kind == SemanticKind.InterpolatedStringConstant)
//                 //         {
//                 //             var interpString = ASTNodeFactory.InterpolatedStringConstant(ast, (DataNode<string>)member);

//                 //             // if(interpString.Value.IndexOf("suffixes") > - 1)
//                 //             // {
//                 //             //     int xxxx = 0;
//                 //             // }

//                 //             // var value = interpString.Value.Replace("\\\\", "\\\\\\\\");

//                 //             // var replacement = NodeFactory.InterpolatedStringConstant(ast, interpString.Origin, value);

//                 //             // ASTHelpers.Replace(ast, interpString.ID, new [] { replacement.Node });
//                 //         }
//                 //     }
//                 // }
//                 // else if(subject.Kind == SemanticKind.StringConstant)
//                 // {
//                 //     var stringConstant = ASTNodeFactory.StringConstant(ast, (DataNode<string>)subject);

//                 //     var value = stringConstant.Value.Replace("\\\\", "\\\\\\\\");

//                 //     var replacement = NodeFactory.StringConstant(ast, stringConstant.Origin, value);

//                 //     ASTHelpers.Replace(ast, stringConstant.ID, new [] { replacement.Node });
//                 // }

//                 invArguments.Add(
//                     CreateInvocationArgument(ast, subject).Node
//                 );

//                 // // [dho] make the original location point to the new hoisted declaration - 11/07/19
//                 // ASTHelpers.Replace(ast, directive.ID, new [] {
//                 //     NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), hoistedNameLexeme).Node
//                 // });

//                 ASTHelpers.Connect(ast, inv.ID, new[] { invSubject }, SemanticRole.Subject);
//                 ASTHelpers.Connect(ast, inv.ID, invArguments.ToArray(), SemanticRole.Argument);
//             }

//             BindSubjectToPassContext(session, artifact, shard, ast, inv.Node, token);

//             InterimSuspension awaitInv = CreateAwait(ast, inv.Node).Item2;

//             MarkAsCTExec(ast, awaitInv.Node);

//             // [dho] NOTE by this point, the static dependencies will have been renamed 
//             // and appear in the preceding statements - 11/07/19
//             content.Add(awaitInv.Node);

//             // var block = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

//             // ASTHelpers.Connect(ast, block.ID, content.ToArray(), SemanticRole.Content);

//             // hoistedDirectiveStatements.Add(block.Node);
//             hoistedDirectiveStatements.AddRange(content);

//             ASTHelpers.DisableNodes(ast, new[] { directive.ID });

//             return result;
//         }

//         private Result<object> TransformCTAPIInvocations(Session session, Artifact artifact, Shard shard, RawAST ast, Component component, BaseLanguageSemantics languageSemantics, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var scope = new Scope(component.Node);


//             var parentDirPath = default(string);
//             {
//                 var sourceWithLocation = ((SourceNodeOrigin)component.Node.Origin).Source as ISourceWithLocation<IFileLocation>;
//                 {
//                     if (sourceWithLocation?.Location != null)
//                     {
//                         var location = sourceWithLocation.Location;

//                         // [dho] sources will be resolved relative to the same directory - 07/05/19
//                         parentDirPath = location.ParentDir.ToPathString();
//                     }
//                 }
//             }


//             foreach (var symbolName in CTAPISymbols.EnumerateCTAPISymbolNames())
//             {
//                 scope.Declarations[symbolName] = component.Node;

//                 var references = languageSemantics.GetUnqualifiedReferenceMatches(session, ast, component.Node, scope, symbolName, token);


//                 if (parentDirPath != null)
//                 {
//                     if (Array.IndexOf(InjectParentPathForCTAPISymbols, symbolName) > -1)
//                     {
//                         foreach (var reference in references)
//                         {
//                             InjectParentDirPathArgument(ast, reference, parentDirPath);
//                         }
//                     }
//                 }


//                 foreach (var reference in references)
//                 {
//                     // [dho] the reference will be the identifier (name) of the invocation,
//                     // so we obtain a handle to the invocation itself - 27/11/19
//                     var inv = ASTHelpers.GetFirstAncestorOfKind(ast, reference.ID, SemanticKind.Invocation);

//                     System.Diagnostics.Debug.Assert(inv != null);

//                     // [dho] ensure the invocation is marked as computable at
//                     // compile time - 27/11/19
//                     if(!MetaHelpers.HasFlags(ast, reference, MetaFlag.CTExec))
//                     {
//                         MarkAsCTExec(ast, inv);
//                     }

//                     BindSubjectToPassContext(session, artifact, shard, ast, inv, token);

//                     var parentheses = NodeFactory.Association(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

//                     ASTHelpers.Replace(ast, inv.ID, new[] { parentheses.Node });

//                     var awaitExp = NodeFactory.InterimSuspension(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//                     {
//                         ASTHelpers.Connect(ast, awaitExp.ID, new[] { inv }, SemanticRole.Operand);
//                     }

//                     ASTHelpers.Connect(ast, parentheses.ID, new[] { awaitExp.Node }, SemanticRole.Subject);
//                 }
//             }


//             return result;
//         }

//         // [dho] conversion from `hello(a, b, c)` to `hello.bind({ artifactName : "foo", shardIndex : "bar", messageID : "x" })(a, b, c)` - 05/10/19
//         private void BindSubjectToPassContext(Session session, Artifact artifact, Shard shard, RawAST ast, Node inv, CancellationToken token)
//         {
//             var previousInvSubject = ASTNodeFactory.Invocation(ast, inv).Subject;

//             var boundInv = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Transformation));

//             ASTHelpers.Replace(ast, previousInvSubject.ID, new[] { boundInv.Node });
//             {
//                 ASTHelpers.Connect(ast, boundInv.ID, new[] {
//                     ASTNodeHelpers.ConvertToQualifiedAccessIfRequiredLTR(ast, new PhaseNodeOrigin(PhaseKind.Transformation), new [] {
//                         previousInvSubject,
//                         NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "bind").Node,
//                     })
//                 }, SemanticRole.Subject);


//                 var boundArg = NodeFactory.DynamicTypeConstruction(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//                 {
//                     // ARTIFACT NAME
//                     var artifactNameField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//                     {
//                         ASTHelpers.Connect(ast, artifactNameField.ID, new[] {
//                             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), ArtifactNameSymbolLexeme).Node
//                         }, SemanticRole.Name);

//                         ASTHelpers.Connect(ast, artifactNameField.ID, new[] {
//                             NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), artifact.Name).Node
//                         }, SemanticRole.Initializer);
//                     }

//                     ASTHelpers.Connect(ast, boundArg.ID, new[] { artifactNameField.Node }, SemanticRole.Member);

//                     // ANCILLARY INDEX
//                     var shardIndexField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//                     {
//                         var shardIndex = session.Shards[artifact.Name].IndexOf(shard);

//                         System.Diagnostics.Debug.Assert(shardIndex > -1);

//                         ASTHelpers.Connect(ast, shardIndexField.ID, new[] {
//                             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), ShardIndexSymbolLexeme).Node
//                         }, SemanticRole.Name);

//                         ASTHelpers.Connect(ast, shardIndexField.ID, new[] {
//                             NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), shardIndex + "").Node
//                         }, SemanticRole.Initializer);
//                     }

//                     ASTHelpers.Connect(ast, boundArg.ID, new[] { shardIndexField.Node }, SemanticRole.Member);

//                     // MESSAGE ID
//                     var messageIDField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Transformation));
//                     {
//                         var messageID = CompilerHelpers.NextInternalGUID();

//                         ASTHelpers.Connect(ast, messageIDField.ID, new[] {
//                             NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), MessageIDSymbolLexeme).Node
//                         }, SemanticRole.Name);

//                         ASTHelpers.Connect(ast, messageIDField.ID, new[] {
//                             NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), messageID).Node
//                         }, SemanticRole.Initializer);
//                     }

//                     ASTHelpers.Connect(ast, boundArg.ID, new[] { messageIDField.Node }, SemanticRole.Member);
//                 }
//                 ASTHelpers.Connect(ast, boundInv.ID, new[] { CreateInvocationArgument(ast, boundArg.Node).Node }, SemanticRole.Argument);
//             }

//         }

//         // [dho] by marking a node as CTExec, it means the CTExecEmitter will emit it for sure 
//         // for use at compile time - 28/11/19
//         private void MarkAsCTExec(RawAST ast, Node node)
//         {
//             var meta = NodeFactory.Meta(
//                 ast,
//                 new PhaseNodeOrigin(PhaseKind.Transformation),
//                 MetaFlag.CTExec
//             );
            
//             ASTHelpers.Connect(ast, node.ID, new [] { meta.Node }, SemanticRole.Meta);
//         }

//         private void InjectParentDirPathArgument(RawAST ast, Node reference, string parentDirPath)
//         {
//             var inv = ASTHelpers.GetFirstAncestorOfKind(ast, reference.ID, SemanticKind.Invocation);

//             System.Diagnostics.Debug.Assert(inv != null);

//             var args = ASTNodeFactory.Invocation(ast, inv).Arguments;


//             var parentDirPathArg = CreateInvocationArgument(ast,
//                 NodeFactory.StringConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), parentDirPath).Node
//             );

//             if (args.Length > 0)
//             {
//                 ASTHelpers.InsertBefore(ast, args[0].ID, new[] { parentDirPathArg.Node }, SemanticRole.Argument);
//             }
//             else
//             {
//                 ASTHelpers.Connect(ast, inv.ID, new[] { parentDirPathArg.Node }, SemanticRole.Argument);
//             }
//         }

//         private Result<object> HoistDependencies(Session session, Artifact artifact, RawAST ast, Node node, BaseLanguageSemantics languageSemantics, List<Node> hoistedDirectiveStatements, Dictionary<string, Node> hoistedNodes, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var dependencies = languageSemantics.GetSymbolicDependencies(session, artifact, ast, node, token);

//             foreach (var dependency in dependencies)
//             {
//                 var decl = dependency.Declaration;

//                 if (decl != null)
//                 {
//                     if (!languageSemantics.IsCTComputable(session, artifact, ast, decl, token))
//                     {
//                         result.AddMessages(new NodeMessage(MessageKind.Error, "Compile time execution can only depend on statically computable symbols", decl)
//                         {
//                             Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(decl.Origin),
//                             Tags = DiagnosticTags
//                         });

//                         continue;
//                     }


//                     var hoistedNameLexeme = decl.ID;

//                     if (!hoistedNodes.ContainsKey(hoistedNameLexeme))
//                     {
//                         // [dho] I'm intentionally not guarding against array out of bounds, because if the declaration
//                         // does not have a name then this will need some refactoring - 11/07/19
//                         var name = ASTHelpers.QueryEdgeNodes(ast, decl.ID, SemanticRole.Name)[0];

//                         ASTNodeHelpers.RefactorName(ast, name, hoistedNameLexeme);

//                         foreach (var lexeme in dependency.References.Keys)
//                         {
//                             foreach (var reference in dependency.References[lexeme])
//                             {
//                                 System.Diagnostics.Debug.Assert(reference.Kind == SemanticKind.Identifier);

//                                 ASTNodeHelpers.RefactorName(ast, reference, hoistedNameLexeme);
//                             }
//                         }

//                         hoistedDirectiveStatements.Add(decl);
//                         hoistedNodes[hoistedNameLexeme] = decl;
//                     }
//                 }
//                 else
//                 {
//                     // [dho] I guess for now if the symbol is assumed to be global/implicit because we did not 
//                     // resolve the declaration, then we should not need to hoist anything? - 11/07/19
//                 }
//             }

//             return result;
//         }

//         private static string ToInlinedIdentifier(Session session, Component component)
//         {
//             return component.ID;
//             // var componentName = component.Name;

//             // var relParentDirPath = componentName.Replace(session.BaseDirectory.ToPathString(), "");

//             // // var apiRelPath = default(string[]);
//             // var qualifiedName = default(string[]);

//             // {
//             //     var qualifiedNameInputStr = relParentDirPath.ToLower();

//             //     if(qualifiedNameInputStr.StartsWith("/"))
//             //     {
//             //         qualifiedNameInputStr = qualifiedNameInputStr.Substring(1);
//             //     }

//             //     var ext = System.IO.Path.GetExtension(qualifiedNameInputStr);

//             //     if(ext?.Length > 0)
//             //     {
//             //         qualifiedNameInputStr = qualifiedNameInputStr.Substring(0, qualifiedNameInputStr.Length - ext.Length);
//             //     }

//             //     qualifiedName = qualifiedNameInputStr.Length > 0 ? qualifiedNameInputStr.Split('/') : new string[] {};
//             // }


//             // var inlinedIdentifier = string.Join(".", qualifiedName); //ToInlinedNamespaceIdentifier(ctid, componentName);


//             // return inlinedIdentifier;
//         }
//     }

// }