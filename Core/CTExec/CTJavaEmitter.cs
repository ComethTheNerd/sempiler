// using System;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using System.Net;
// using System.Threading;
// using System.Threading.Tasks;
// using Sempiler.Diagnostics;
// using Sempiler.Emission;
// using Sempiler.AST;
// using Sempiler.AST.Diagnostics;
// using Sempiler.Languages;

// namespace Sempiler.CTExec
// {
//     using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
//     using static Sempiler.Diagnostics.DiagnosticsHelpers;
//     using System.Security.Cryptography;
//     using System.Text;



//     public class CTJavaEmitter : JavaEmitter
//     {
//         private readonly string CTID;

//         // private readonly string ReplaceNodeWithNodesFunctionIdentifier;
//         // private readonly string ReplaceNodeWithValueFunctionIdentifier;
//         private readonly string DeleteNodeFunctionIdentifier;

//         private readonly string IllegalBridgeDirectiveNodeFunctionIdentifier;

//         private readonly string InsertImmediateSiblingFromValueAndDeleteNodeFunctionIdentifier;

//         private readonly string InsertImmediateSiblingAndDeleteNodeFunctionIdentifier;

//         private readonly string EvalOnceFlagMapIdentifier;

//         private readonly string AddSourcesFunctionIdentifier;

//         private readonly string AddRawSourcesFunctionIdentifier;

//         private readonly string CompilerClientClassIdentifier;

//         private readonly IDirectoryLocation OutDirLocation;

//         private readonly IPAddress ServerIPAddress;
//         private readonly int ServerPort;

//         // private List<Node> Directives;

//         private Stack<Directive> ExistentialDependencies;

//         private Dictionary<string, string> FileLocationClassIdentifierMap = new Dictionary<string, string>();

//         public IEnumerable<string> FilePathsToInit;


//         public CTJavaEmitter(string ctID, IPAddress serverIPAddress, int serverPort, IDirectoryLocation outDirLocation) : base()
//         {
//             CTID = ctID;

//             // [dho] internal API - 16/05/19
//             // ReplaceNodeWithNodesFunctionIdentifier = CTID + "$$ReplaceNodeWithNodes";
//             // ReplaceNodeWithValueFunctionIdentifier = CTID + "$$ReplaceNodeWithValue";
//             DeleteNodeFunctionIdentifier = CTID + "$$DeleteNode";
//             IllegalBridgeDirectiveNodeFunctionIdentifier = CTID + "$$IllegalBridgeDirectiveNode";
//             CompilerClientClassIdentifier = CTID + "$$CompilerClient";
//             EvalOnceFlagMapIdentifier = CTID + "$$EvalOnceFlagMap";

//             // [dho] public API - 16/05/19
//             AddSourcesFunctionIdentifier = "add_sources";
//             AddRawSourcesFunctionIdentifier = "add_raw_sources";
//             InsertImmediateSiblingFromValueAndDeleteNodeFunctionIdentifier = "insert_immediate_sibling_from_value_and_delete_node";
//             InsertImmediateSiblingAndDeleteNodeFunctionIdentifier = "insert_immediate_sibling_and_delete_node";

//             ServerIPAddress = serverIPAddress;
//             ServerPort = serverPort;

//             OutDirLocation = outDirLocation;
//         }
                
//         // public bool RequiresCompileTimeEvaluation { get => Directives.Count > 0; }
        

//         private string ToClassIdentifier(string inputString)
//         {
//             StringBuilder sb = new StringBuilder(CTID + "$$");

//             foreach (byte b in SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(inputString)))
//             {
//                 sb.Append(b.ToString("X2"));
//             }

//             return sb.ToString();
//         }

//         private string ToStaticInitIdentifier()
//         {
//             return (CTID + "$$init");
//         }

//         private string ToIfDirectiveComputationFunctionIdentifier(ASTNode node)
//         {
//             return $"{CTID}$$IfDirectiveComputation$${node.ID}";
//         }
//         private string ToRunDirectiveComputationFunctionIdentifier(ASTNode node)
//         {
//             return $"{CTID}$$RunDirectiveComputation$${node.ID}";
//         }


//         public override Task<Result<OutFileCollection>> Emit(Session session, Artifact artifact, RawAST ast, Node node, CancellationToken token)
//         {
//             var result = new Result<OutFileCollection>();
            
//             var outFileCollection = new OutFileCollection();
            
//             // [dho] store the single file in the artifact - 12/04/19
//             var file = new FileEmission(
//                 FileSystem.CreateFileLocation(OutDirLocation, CTID, "java")
//             );

//             var context = new Context 
//             {
//                 Session = session,
//                 Artifact = artifact,
//                 Shard = shard,
//                 AST = ast,
//                 OutFileCollection = outFileCollection,
//                 Emission = file///new LiteralEmission()
//             };

//             // Directives = new List<Node>();
//             ExistentialDependencies = new Stack<Directive>();

//             // [dho] wrap everything in a class so floating functions are legal - 12/04/19
//             file.Append(node, $"class {CTID}{{");

//             file.Indent();

//             {
//                 file.AppendBelow(node, $"public static java.util.HashMap<String, Boolean> {EvalOnceFlagMapIdentifier} = new java.util.HashMap<>();");

//                 file.AppendBelow(node, $"public static class {CompilerClientClassIdentifier}{{");
                
//                 file.Indent();
                
//                 {
//                     file.AppendBelow(node, 
//                         "private static java.net.Socket clientSocket;",
//                         "private static java.io.PrintWriter out;",
//                         "private static java.io.BufferedReader in;"
//                     );

//                     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                     file.AppendBelow(node, "public static void startConnection() throws java.io.IOException {");

//                     file.Indent();

//                     {
//                         file.AppendBelow(node, 
//                             $"clientSocket = new java.net.Socket(\"{ServerIPAddress}\",{ServerPort});",
//                             "out = new java.io.PrintWriter(clientSocket.getOutputStream(), true);",
//                             "in = new java.io.BufferedReader(new java.io.InputStreamReader(clientSocket.getInputStream()));"
//                         );
//                     }

//                     file.Outdent();

//                     file.AppendBelow(node, "}");

//                     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                     file.AppendBelow(node, "public static String sendMessage(String msg) {");

//                     file.Indent();

//                     {
//                         file.AppendBelow(node, 
//                             "System.out.println(\"Sending message :: \" + msg);",

//                             // [dho] TODO CLEANUP `out.println` sends an extra `\n` at the end of the message,
//                             // but we are using a sentinel instead of just reading lines. Using `out.print`
//                             // does not seem to work though.. so keeping it this way for now as the extra `\n`
//                             // is stripped in our C# socket server anyway right now - 20/04/19
//                             $"out.println(msg + \"{DuplexSocketServer.MessageSentinel}\");",
                            
//                             $"java.util.Scanner scanner = new java.util.Scanner(in).useDelimiter(\"{DuplexSocketServer.MessageSentinel}\");",
        
//                             "String resp = scanner.hasNext() ? scanner.next() : \"\";",
//                             "System.out.println(\"Received message :: \" + resp);",
//                             "return resp;"
//                         );
//                     }

//                     file.Outdent();

//                     file.AppendBelow(node, "}");

//                     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                     file.AppendBelow(node, "public static void stopConnection() throws java.io.IOException {");

//                     file.Indent();

//                     {
//                         file.AppendBelow(node, 
//                             "in.close();",
//                             "out.close();",
//                             "clientSocket.close();"
//                         );
//                     }

//                     file.Outdent();

//                     file.AppendBelow(node, "}");

//                     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//                 }

//                 file.Outdent();

//                 file.AppendBelow(node, "}");

//                 // [dho] emit the functions that will handle communicating with the compiler runtime via a socket interface in order to access
//                 // and mutate information about the program (eg. AST) - 20/04/19

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 // file.AppendBelow(node, $"public static void {ReplaceNodeWithNodesFunctionIdentifier}(String nodeID, String ... replacementNodeIDs){{");

//                 // file.Indent();

//                 // {
//                 //     file.AppendBelow(node,
//                 //         $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{artifact.Name}::{(int)CTProtocolCommandKind.ReplaceNodeWithNodes}(%s,%s)\",nodeID,String.join(\",\", replacementNodeIDs)));"
//                 //     );
//                 // }

//                 // file.Outdent();

//                 // file.AppendBelow(node, "}");

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 // // [dho] we use this function so that javac does the heavy lifting of inferring the type `T` for us, rather than us hoisting out a run directive
//                 // // and having to know the type of the result explicitly. So this function acts as a passthrough that will tell the compiler to replace a node
//                 // // in the AST, and then propagate the result back to the call site - 20/04/19
//                 // file.AppendBelow(node, $"public static <T> T {ReplaceNodeWithValueFunctionIdentifier}(String nodeID, T t){{");

//                 // file.Indent();

//                 // {
//                 //     file.AppendBelow(node,
//                 //         $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{artifact.Name}::{(int)CTProtocolCommandKind.ReplaceNodeWithValue}(%s,%s,%s)\", nodeID, t.getClass().getCanonicalName(), t));",
//                 //         "return t;"
//                 //     );
//                 // }

//                 // file.Outdent();

//                 // file.AppendBelow(node, "}");

//                 /*
//                 private readonly string InsertImmediateSiblingFromValueFunctionIdentifier;

//         private readonly string InsertImmediateSiblingFunctionIdentifier;
                
//                  */

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 // [dho] we use this function so that javac does the heavy lifting of inferring the type `T` for us, rather than us hoisting out a run directive
//                 // and having to know the type of the result explicitly. So this function acts as a passthrough that will tell the compiler to replace a node
//                 // in the AST - 20/04/19
//                 file.AppendBelow(node, $"public static <T> T {InsertImmediateSiblingFromValueAndDeleteNodeFunctionIdentifier}(String insertionPointID, T t, String removeeID){{");

//                 file.Indent();

//                 {
//                     file.AppendBelow(node,
//                         $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{artifact.Name}::{(int)CTProtocolCommandKind.InsertImmediateSiblingAndFromValueAndDeleteNode}(%s,%s,%s,%s)\", insertionPointID, t.getClass().getCanonicalName(), t, removeeID));"
//                         ,"return t;"
//                     );
//                 }

//                 file.Outdent();

//                 file.AppendBelow(node, "}");

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 file.AppendBelow(node, $"public static void {InsertImmediateSiblingAndDeleteNodeFunctionIdentifier}(String insertionPointID, String inserteeID, String removeeID){{");

//                 file.Indent();

//                 {
//                     file.AppendBelow(node,
//                         $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{artifact.Name}::{(int)CTProtocolCommandKind.InsertImmediateSiblingAndDeleteNode}(%s,%s,%s)\",insertionPointID,inserteeID,removeeID));"
//                     );
//                 }

//                 file.Outdent();

//                 file.AppendBelow(node, "}");

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 file.AppendBelow(node, $"public static <T> T {DeleteNodeFunctionIdentifier}(String nodeID, T t){{");

//                 file.Indent();

//                 {
//                     file.AppendBelow(node,
//                         $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{artifact.Name}::{(int)CTProtocolCommandKind.DeleteNode}(%s)\", nodeID));"
//                         ,"return t;"
//                     );
//                 }

//                 file.Outdent();

//                 file.AppendBelow(node, "}");

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 file.AppendBelow(node, $"public static void {DeleteNodeFunctionIdentifier}(String nodeID){{");

//                 file.Indent();

//                 {
//                     file.AppendBelow(node,
//                         $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{artifact.Name}::{(int)CTProtocolCommandKind.DeleteNode}(%s)\", nodeID));"
//                     );
//                 }

//                 file.Outdent();

//                 file.AppendBelow(node, "}");

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 file.AppendBelow(node, $"public static <T> T {IllegalBridgeDirectiveNodeFunctionIdentifier}(String nodeID){{");

//                 file.Indent();

//                 {
//                     file.AppendBelow(node,
//                         $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{artifact.Name}::{(int)CTProtocolCommandKind.IllegalBridgeDirectiveNode}(%s)\",nodeID));",
//                         "return (T)null;"
//                     );
//                 }

//                 file.Outdent();

//                 file.AppendBelow(node, "}");

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 // [dho] emitting the artifact meta so that directives can reference it for conditional execution - 07/05/19

//                 file.AppendBelow(node, $"public static class artifact {{");

//                 file.Indent();

//                 {
//                     file.AppendBelow(node,
//                         $"public static final String name=\"{artifact.Name}\";",
//                         $"public static final String target_lang=\"{artifact.TargetLang}\";",
//                         $"public static final String target_platform=\"{artifact.TargetPlatform}\";"
//                     );
//                 }

//                 file.Outdent();

//                 file.AppendBelow(node, "}");

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                
//                 var root = ASTHelpers.GetRoot(ast);

//                 result.AddMessages(EmitNode(root, context, token));
                
//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//                 // [dho] entrypoint function - 13/04/19
//                 file.AppendBelow(node, "public static void main(String[] args){");

//                 file.Indent();

//                 {
//                     file.AppendBelow(node, "try {");

//                     file.Indent();

//                     {
//                         file.AppendBelow(node, $"{CompilerClientClassIdentifier}.startConnection();");

//                         // [dho] when we add new sources to the tree we want to only init them on subcompiles,
//                         // as we will be 'halfway through' evaluated the file from where the sources were added - 06/05/19
//                         if(FilePathsToInit != null)
//                         {
//                             foreach(var filePath in FilePathsToInit)
//                             {
//                                 if(FileLocationClassIdentifierMap.ContainsKey(filePath))
//                                 {
//                                     file.AppendBelow(node, $"{FileLocationClassIdentifierMap[filePath]}.{ToStaticInitIdentifier()}();");
//                                 }
//                                 else
//                                 {
//                                     result.AddMessages(new Message(MessageKind.Error, $"File path to init was not emitted : '{filePath}'")
//                                     {
//                                         Tags = DiagnosticTags
//                                     });
//                                 }
//                             }
//                         }
//                         else
//                         {
//                             // [dho] NOTE putting the initializers for nested classes first implies that
//                             // nested hooks fire before parent hooks
//                             foreach(var kv in FileLocationClassIdentifierMap)
//                             {
//                                 file.AppendBelow(node, $"{kv.Value}.{ToStaticInitIdentifier()}();");
//                             }
//                         }

//                         file.AppendBelow(node, $"{CompilerClientClassIdentifier}.stopConnection();");
//                     }

//                     file.Outdent();

//                     file.AppendBelow(node, "} catch(Exception e) {");

//                     file.Indent();

//                     {
//                         file.AppendBelow(node,
//                             "System.out.println(\"EXCEPTION: \" + e.getMessage());",
//                             "System.exit(1);"
//                         );
//                     }

//                     file.Outdent();

//                     file.AppendBelow(node, "}");    
//                 }

//                 file.AppendBelow(node, "System.out.println(\"ALL DONE. PROGRAM FINISHING\");");

//                 file.Outdent();

//                 // [dho] end `main` - 13/04/19
//                 file.AppendBelow(node, "}");

//                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//             }

//             file.Outdent();

//             // [dho] end wrapper class - 13/04/19
//             file.AppendBelow(node, "}");


//             outFileCollection[file.Destination] = file;

//             result.Value = outFileCollection;

//             return Task.FromResult(result);
//         }

//         // public override Result<object> EmitNode(Node node, Context context, CancellationToken token)
//         // {
//         //     context.Emission.AppendBelow(node, $"/* {node.Kind.ToString()} :: {node.ID} */");
//         //     return base.EmitNode(node, context, token);
//         // }

//         public override Result<object> EmitComponent(Component node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             if (node.Origin.Kind != NodeOriginKind.Source)
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Could not write emission in artifact due to unsupported origin kind '{node.Origin.Kind}'", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );

//                 return result;
//             }

//             var sourceWithLocation = ((SourceNodeOrigin)node.Origin).Source as ISourceWithLocation<IFileLocation>;

//             if (sourceWithLocation == null || sourceWithLocation.Location == null)
//             {
//                 result.AddMessages(
//                     new NodeMessage(MessageKind.Error, $"Could not write emission in artifact because output location cannot be determined", node)
//                     {
//                         Hint = GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     }
//                 );

//                 return result;
//             }

//             var location = sourceWithLocation.Location;

//             var filePathString = location.ToPathString();


//             var classIdentifier = ToClassIdentifier(filePathString);

//             // [dho] do we need this map yet? or only when we rewrite imports?? - 12/04/19
//             FileLocationClassIdentifierMap[filePathString] = classIdentifier;


//             context.Emission.AppendBelow(node, $"// FILE : {filePathString}:");

//             // [dho] every file is going to be emitted as a nested static class
//             context.Emission.AppendBelow(node, $"static class {classIdentifier} {{");

//             context.Emission.AppendBelow(node, "");

//             context.Emission.Indent();

//             ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//             // [dho] we have an add sources function in every component because we need to silently inject the component path so that we can resolve include paths
//             // relative to it - 07/05/19

//             context.Emission.AppendBelow(node, $"public static void {AddSourcesFunctionIdentifier}(String ... includePaths){{");

//             context.Emission.Indent();

//             {
//                 // [dho] sources will be resolved relative to the same directory - 07/05/19
//                 var parentDirPath = location.ParentDir.ToPathString();

//                 context.Emission.AppendBelow(node,
//                     $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{context.Artifact.Name}::{(int)CTProtocolCommandKind.AddSources}(%s,%s)\",\"{parentDirPath}\",String.join(\",\", includePaths)));"
//                 );
//             }

//             context.Emission.Outdent();

//             context.Emission.AppendBelow(node, "}");

//             ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//             // [dho] we have an add sources function in every component because we need to silently inject the component path so that we can resolve include paths
//             // relative to it - 08/07/19

//             context.Emission.AppendBelow(node, $"public static void {AddRawSourcesFunctionIdentifier}(String ... includePaths){{");

//             context.Emission.Indent();

//             {
//                 // [dho] sources will be resolved relative to the same directory - 08/07/19
//                 var parentDirPath = location.ParentDir.ToPathString();

//                 context.Emission.AppendBelow(node,
//                     $"{CompilerClientClassIdentifier}.sendMessage(String.format(\"{context.Artifact.Name}::{(int)CTProtocolCommandKind.AddRawSources}(%s,%s)\",\"{parentDirPath}\",String.join(\",\", includePaths)));"
//                 );
//             }

//             context.Emission.Outdent();

//             context.Emission.AppendBelow(node, "}");

//             ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                



//             {
//                 var childContext = ContextHelpers.Clone(context);
//                 childContext.Component = node;
//                 // // childContext.Parent = node;

//                 // result.AddMessages(EmitCompileTimeDirectiveComputation(node, childContext, token));

//                 List<Node> initItems = new List<Node>();

//                 result.AddMessages(
//                     EmitDeclarationsForStaticInitializer(
//                         // [dho] TODO CLEANUP! - 03/05/19
//                         StaticRootDeclarationsHack(node, childContext, token), initItems, context, token
//                     )
//                 );
        
//                 result.AddMessages(
//                     EmitStaticInitializer(node, initItems, context, token)
//                 );
            
//             }

//             context.Emission.Outdent();

//             context.Emission.AppendBelow(node, "}");

            

//             return result;
//         }

//         private Result<object> EmitCompileTimeDirectiveComputation(ASTNode nodeWrapper, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);

//             foreach(var d in FindCurrentObjectTypeCompileTimeDirectives(nodeWrapper.AST, nodeWrapper.Node))
//             {
//                 var directive = ASTNodeFactory.Directive(nodeWrapper.AST, (DataNode<string>)d);
            
//                 var subject = directive.Subject;

//                 foreach(var decl in FindCurrentObjectTypeCompileTimeDeclarations(directive.AST, subject))
//                 {
//                     result.AddMessages(EmitNode(decl, childContext, token));
//                 }
//             }

//             return result;
//         }


//        /// <summary>
//         /// We basically need to find all the compile time directives inside the class, but not any nested
//         /// classes, because we need to code gen the 'state machine' for the if/run expression at the class
//         /// level where it will be syntactically legal to define a function etc. - 19/04/19
//         /// </summary>
//         private static IEnumerable<Node> FindCurrentObjectTypeCompileTimeDirectives(RawAST ast, Node node)
//         {
//             foreach(var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, node.ID))
//             {
//                 if(child.Kind == SemanticKind.Directive) yield return child;

//                 if(child.Kind != SemanticKind.DataValueDeclaration && 
//                     LanguageSemantics.Java.IsDeclarationStatement(ast, child))
//                 {
//                     continue;
//                 }

//                 foreach(var compileTimeDirective in FindCurrentObjectTypeCompileTimeDirectives(ast, child))
//                 {
//                     yield return compileTimeDirective;
//                 }
//             }
//         }

//         private static IEnumerable<Node> FindCurrentObjectTypeCompileTimeDeclarations(RawAST ast, Node node)
//         {
//             if(node.Kind != SemanticKind.DataValueDeclaration && 
//                 LanguageSemantics.Java.IsDeclarationStatement(ast, node))
//             {
//                 yield return node;
//             }
//             else
//             {
//                 foreach(var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, node.ID))
//                 {
//                     foreach(var compileTimeDirective in FindCurrentObjectTypeCompileTimeDeclarations(ast, child))
//                     {
//                         yield return compileTimeDirective;
//                     }
//                 }
//             }
//         }

//         // [dho] TODO CLEANUP HACK!! - 16/05/19
//         private IEnumerable<(Node, bool)> StaticRootDeclarationsHack(Component node, Context context, CancellationToken token)
//         {
//             foreach(var tuple in ASTNodeHelpers.IterateChildren(node.AST, node.ID))
//             {
//                 if(LanguageSemantics.Java.IsDeclarationStatement(node.AST, tuple.Item1))
//                 {
//                     // [dho] TODO CLEANUP HACK decls that are written at root level in a script
//                     // should be declared `static`, but for now we are just going to cope with the fact
//                     // that may not have that meta flag set - 20/04/19
//                     if((ASTNodeHelpers.GetMetaFlags(node.AST, tuple.Item1.ID) & MetaFlag.Static) == 0)
//                     {
//                         context.Emission.AppendBelow(tuple.Item1, "static ");
//                     }
//                 }

//                 yield return tuple;
//             }
//         }

//         // public override Result<object> EmitRunDirective(Directive runDirective, Context context, CancellationToken token)
//         // {
//         //     var result = new Result<object>();

//         //     var childContext = ContextHelpers.Clone(context);
//         //     // // childContext.Parent = runDirective;

//         //     var emission = context.Emission;

//         //     // Directives.Add(runDirective);
            
//         //     var subject = runDirective.Subject;

//         //     var astEffect = RunDirectiveASTEffect(runDirective);

//         //     if(astEffect == CTProtocolCommandKind.ReplaceNodeWithValue)
//         //     {
//         //         emission.Append(runDirective, $"{ReplaceNodeWithValueFunctionIdentifier}(\"{runDirective.ID}\",");

//         //         // [dho] NOTE do NOT quit out here if this results in an error, otherwise we will
//         //         // be neglecting to decrement the scope depth which happens at the end of this function - 12/04/19
//         //         result.AddMessages(EmitNode(subject, childContext, token));

//         //         // [dho] end invocation statement - 12/04/19
//         //         emission.Append(runDirective, ")");
//         //     }
//         //     else if(astEffect == CTProtocolCommandKind.DeleteNode)
//         //     {
//         //         result.AddMessages(EmitCompileTimeDirectiveBranch(runDirective.Subject, childContext, token));
                    
//         //         emission.AppendBelow(runDirective, $"{DeleteNodeFunctionIdentifier}(\"{runDirective.ID}\");");
//         //     }
//         //     else
//         //     {
//         //         result.AddMessages(new Message(MessageKind.Error, $"Unsupported run directive AST effect : '{astEffect}'")
//         //         {
//         //             Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(runDirective.Origin),
//         //             Tags = DiagnosticTags
//         //         });
//         //     }

//         //     return result;
//         // }

//         // CTProtocolCommandKind RunDirectiveASTEffect(Directive runDirective)
//         // {
//         //     Node focus = runDirective;
//         //     Node parent = runDirective.Parent;

//         //     while(parent?.Kind == SemanticKind.Association)
//         //     {
//         //         focus = parent;
//         //         parent = parent.Parent;
//         //     }
            
//         //     if(parent != null)
//         //     {
//         //         // [dho] `const x = #run foo()`  - 18/04/19
//         //         if(parent.Kind == SemanticKind.DataValueDeclaration)
//         //         {
//         //             return CTProtocolCommandKind.ReplaceNodeWithValue;
//         //         }

//         //         // [dho] `++#run foo()`  - 18/04/19
//         //         // [dho] `x + #run bar()`  - 18/04/19
//         //         if(parent is UnaryLike || parent is BinaryLike)
//         //         {
//         //             return CTProtocolCommandKind.ReplaceNodeWithValue;
//         //         }

//         //         if(parent.Kind == SemanticKind.OrderedGroup)
//         //         {
//         //             focus = parent;
//         //             parent = parent.Parent;
                    
//         //             if(parent == null)
//         //             {
//         //                 return CTProtocolCommandKind.DeleteNode;
//         //             }
//         //         }

//         //         // [dho] hello(1, #run world())  - 18/04/19
//         //         if(parent.Kind == SemanticKind.Invocation && focus == ((Invocation)parent).Arguments)
//         //         {
//         //             return CTProtocolCommandKind.ReplaceNodeWithValue;
//         //         }

//         //         // // [dho] function hello(n : number, #run world())  - 18/04/19
//         //         // if(parent is FunctionLikeDeclaration && focus == ((FunctionLikeDeclaration)parent).Parameters)
//         //         // {
//         //         //     return MarshalledCommand.ReplaceNodeWithTypedValue;
//         //         // }
//         //     }

//         //     return CTProtocolCommandKind.DeleteNode;
//         // }

//         // Node GetEnclosingJavaScope(ASTNode nodeWrapper)
//         // {
//         //     Node parent = nodeWrapper.Parent;

//         //     while(parent != null)
//         //     {
//         //         if(LanguageHelpers.JavaTreatsAsFunctionLikeDeclaration(parent))
//         //         {
//         //             return parent;
//         //         }
//         //         else if(parent.Kind == SemanticKind.ObjectTypeDeclaration || parent.Kind == SemanticKind.Component)
//         //         {
//         //             return parent;
//         //         }

//         //         parent = ASTHelpers.GetParent(nodeWrapper.AST, parent.ID);
//         //     }

//         //     return null;
//         // }

//         string EmittedIfDirectiveResultIdentifier(ASTNode ifDirective)
//         {
//             return $"{CTID}IF{ifDirective.ID}";
//         }

//         string EmittedIfDirectiveDidSettleFlagIdentifier(ASTNode ifDirective)
//         {
//             return $"{EmittedIfDirectiveResultIdentifier(ifDirective)}DidSettle";
//         }

//         // public override Result<object> EmitIfDirective(IfDirective ifDirective, Context context, CancellationToken token)
//         // {
//         //     var result = new Result<object>();

//         //     var childContext = ContextHelpers.Clone(context);
//         //     // // childContext.Parent = ifDirective;

//         //     var emission = context.Emission;

//         //     // Directives.Add(ifDirective);

//         //     emission.AppendBelow(ifDirective, $"if({ToIfDirectiveComputationFunctionIdentifier(ifDirective)}())"); what_is_the_signature;
            
//         //     emission.AppendBelow(ifDirective.TrueBranch, "");
            
//         //     result.AddMessages(EmitCompileTimeDirectiveBranch(ifDirective.TrueBranch, childContext, token));
        
//         //     if(ifDirective.FalseBranch != null)
//         //     {
//         //         emission.Append(ifDirective.FalseBranch, " else ");
                
//         //         emission.AppendBelow(ifDirective.FalseBranch, "");

//         //         result.AddMessages(EmitCompileTimeDirectiveBranch(ifDirective.FalseBranch, childContext, token));
//         //     }
           
//         //     return result;
//         // }
        

//         // private Result<object> EmitCompileTimeDirectiveBranch(Node branch, Context context, CancellationToken token)
//         // {
//         //     var result = new Result<object>();

//         //     context.Emission.AppendBelow(branch, "{");
//         //     context.Emission.Indent();

//         //     {
//         //         foreach (var (member, hasNext) in IterateBranchReplacementNodes(branch))
//         //         {
//         //             if(member is Declaration)
//         //             {
//         //                 // [dho] declarations are hoisted and emitted as part of the 
//         //                 // `EmitIfDirectiveComputation` routine - 03/05/19
//         //                 continue;
//         //             }
//         //             else
//         //             {
//         //                 context.Emission.AppendBelow(member, "");
                        
//         //                 result.AddMessages(
//         //                     EmitNode(member, context, token).Messages
//         //                 );

//         //                 if(RequiresSemicolonSentinel(member))
//         //                 {
//         //                     context.Emission.Append(member, ";");
//         //                 }
//         //             }
//         //         }
//         //     }

//         //     context.Emission.Outdent();
//         //     context.Emission.AppendBelow(branch, "}");

//         //     return result;
//         // }


//         // private IEnumerable<(Node, bool)> IterateBranchReplacementNodes(Node branch)
//         // {
//         //     var result = new Result<object>();

//         //     Node members = default(Node);

//         //     if(branch?.Kind == SemanticKind.Block)
//         //     {
//         //         members = ((Block)branch).Content;
//         //     }
//         //     else
//         //     {
//         //         members = branch;
//         //     }

//         //     return ASTNode.IterateMembers(members);
//         // }

//         public override Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var childContext = ContextHelpers.Clone(context);
//             // // childContext.Parent = node;

//             // [dho] TODO modifiers! - 21/09/18

//             var name = node.Name;
//             var template = node.Template;
//             var members = node.Members;

//             if(name != null)
//             {
//                 if(name.Kind == SemanticKind.Identifier)
//                 {
//                     context.Emission.Append(node, "class ");

//                     result.AddMessages(
//                         EmitIdentifier(ASTNodeFactory.Identifier(childContext.AST, (DataNode<string>)name), childContext, token).Messages
//                     );
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         new NodeMessage(MessageKind.Error, $"Reference type declaration has unsupported name type : '{name.Kind}'", name)
//                         {
//                             Hint = GetHint(name.Origin),
//                             Tags = DiagnosticTags
//                         }
//                     );
//                 }
//             }
//             else
//             {
//                 result.AddMessages(
//                     CreateUnsupportedFeatureResult(node).Messages
//                 );
//             }

//             // generics
//             if(template.Length > 0)
//             {
//                 result.AddMessages(
//                     EmitTypeDeclarationTemplate(node, childContext, token).Messages
//                 );
//             }

//             result.AddMessages(
//                 EmitTypeDeclarationHeritage(node, context, token).Messages
//             );

//             context.Emission.Append(node, "{");

//             // result.AddMessages(EmitCompileTimeDirectiveComputation(node, childContext, token));

//             context.Emission.Indent();

//             {
//                 List<Node> initItems = new List<Node>();

//                 result.AddMessages(
//                     EmitDeclarationsForStaticInitializer(
//                         ASTNodeHelpers.IterateMembers(members), initItems, context, token
//                     )
//                 );
        
//                 result.AddMessages(
//                     EmitStaticInitializer(node, initItems, context, token)
//                 );
//             }

//             context.Emission.Outdent();

//             // [dho] end object type declaration - 18/04/19
//             context.Emission.AppendBelow(node, "}");

//             return result;
//         }

//         private Result<object> EmitDeclarationsForStaticInitializer(IEnumerable<(Node, bool)> members, List<Node> initItems, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             foreach (var (member, hasNext) in members)
//             {
//                 if (token.IsCancellationRequested)
//                 {
//                     break;
//                 }

//                 if(LanguageSemantics.Java.IsDeclarationStatement(context.AST, member))
//                 {
//                     context.Emission.AppendBelow(member, "");
                    
//                     // [dho] inner class must be `static` - 03/05/19
//                     if(member.Kind == SemanticKind.ObjectTypeDeclaration)
//                     {
//                         if((ASTNodeHelpers.GetMetaFlags(context.AST, member.ID) & MetaFlag.Static) == 0)
//                         {
//                             context.Emission.AppendBelow(member, "static ");
//                         }
//                     }


//                     result.AddMessages(
//                         EmitNode(member, context, token).Messages
//                     );

//                     if(RequiresSemicolonSentinel(member))
//                     {
//                         context.Emission.Append(member, ";");
//                     }

//                     if(HasErrors(result)) return result;

//                     if(member.Kind == SemanticKind.ObjectTypeDeclaration)
//                     {
//                         var className = ASTNodeHelpers.GetLexeme(ASTNodeFactory.ObjectTypeDeclaration(context.AST, member).Name);

//                         initItems.Add(
//                             NodeFactory.CodeConstant(context.AST, member.Origin, $"{className}.{ToStaticInitIdentifier()}();").Node
//                         );
//                     }
//                 }
//                 else if(member.Kind == SemanticKind.Directive)
//                 {
//                     initItems.Add(member);

//                     var directive = ASTNodeFactory.Directive(context.AST, (DataNode<string>)member);

//                     var subject = directive.Subject;

//                     // [dho] TODO clarify if we need all this crap here - 17/05/19
//                     if(subject?.Kind == SemanticKind.Block)
//                     {
//                         result.AddMessages(
//                             EmitDeclarationsForStaticInitializer(
//                                 ASTNodeHelpers.IterateMembers(ASTNodeFactory.Block(context.AST, subject).Content), new List<Node>(), context, token
//                             )
//                         );
//                     }
//                     else
//                     {
//                         result.AddMessages(
//                             EmitDeclarationsForStaticInitializer(
//                                 ASTNodeHelpers.IterateMembers(new [] { subject }), new List<Node>(), context, token
//                             )
//                         );
//                     }
//                 }
//             }

//             return result;
//         }

//         // private Result<object> EmitCTDirectiveEvalOnceIfComputation(PredicateJunction node, Context context, CancellationToken token)
//         // {
//         //     var result = new Result<object>();

//         //     var childContext = ContextHelpers.Clone(context);
//         //     // // childContext.Parent = ifDirective;

//         //     var emission = context.Emission;

//         //     // [dho] we use a settle flag to guard against an `if` predicate that references a conditional declaration
//         //     // ie. some function that may or not be emitted, depending on the result of evaluating this `if` - 13/04/19
//         //     var didSettleFlagVarName = EmittedIfDirectiveDidSettleFlagIdentifier(node);
//         //     var evalResultVarName = EmittedIfDirectiveResultIdentifier(node);

//         //     /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//         //     emission.AppendBelow(node, $"private static boolean {didSettleFlagVarName} = false;");
//         //     emission.AppendBelow(node, $"private static boolean {evalResultVarName};");




//         //     emission.AppendBelow(node, $"static boolean {ToIfDirectiveComputationFunctionIdentifier(node)}");

            
//         //     var valueDeps = new Dictionary<string, int>();

//         //     result.AddMessages(InferValueDependencies(context.AST, node.Predicate, valueDeps));

//         //     // [dho] `if(bar < 100 && foo.bar(x)` has value dependencies `<T0, T1>(T0 p0, T1 p1)` - 15/05/19 
//         //     if(valueDeps.Count > 0)
//         //     {
//         //         var sb = new System.Text.StringBuilder();

//         //         // [dho] template - 15/05/19
//         //         {
//         //             sb.Append("<");

//         //             for(int i = 0; i < valueDeps.Count; ++i)
//         //             {
//         //                 var t = "T" + i;

//         //                 if(i < valueDeps.Count - 1)
//         //                 {
//         //                     sb.Append(",");    
//         //                 }
//         //             }

//         //             sb.Append(">");
//         //         }


//         //         {
//         //             sb.Append("(");    

//         //             var index = 0;

//         //             foreach(var kv in valueDeps)
//         //             {
//         //                 sb.Append($"T{kv.Value} p{kv.Value}");

//         //                 if(index++ < valueDeps.Count - 1)
//         //                 {
//         //                     sb.Append(",");  
//         //                 }
//         //             }

//         //             sb.Append(")"); 
//         //         }

//         //         emission.Append(node, sb.ToString());
//         //     }
//         //     else
//         //     {
//         //         emission.Append(node, "()");
//         //     }


//         //     emission.AppendBelow(node, "{");


//         //     emission.Indent();

//         //     {
//         //         // [dho] we only check the if condition if we haven't already evaluated it - 19/04/19
//         //         emission.AppendBelow(ifDirective, $"if(!{didSettleFlagVarName}){{");

//         //         emission.Indent();

//         //         {
//         //             // [dho] set evaluation result - 19/04/19
//         //             emission.AppendBelow(ifDirective, $"if({evalResultVarName}=");

//         //             result.AddMessages(EmitNode(ifDirective.Predicate, childContext, token));

//         //             emission.Append(ifDirective, "){");

//         //             emission.Indent();
                    
//         //             {
//         //                 EmitIfDirectiveEffect(ifDirective, ifDirective.TrueBranch, emission);
//         //             }

//         //             emission.Outdent();

//         //             emission.AppendBelow(ifDirective, "} else {");

//         //             emission.Indent();

//         //             {
//         //                 EmitIfDirectiveEffect(ifDirective, ifDirective.FalseBranch, emission);
//         //             }

//         //             emission.Outdent();

//         //             // [dho] end of else statement - 19/04/19
//         //             emission.AppendBelow(ifDirective, "}");

//         //             // [dho] mark the if directive as settled - 19/04/19
//         //             emission.AppendBelow(ifDirective, $"{didSettleFlagVarName}=true;");
//         //         }

//         //         emission.Outdent();

//         //         emission.AppendBelow(ifDirective, "}");
//         //     }

//         //     // [dho] return the result of evaluating the if predicate - 18/04/19
//         //     emission.AppendBelow(ifDirective, $"return {evalResultVarName};");

//         //     emission.Outdent();

//         //     // [dho] end of function for if directive - 19/04/19
//         //     emission.AppendBelow(ifDirective, "}");
 
//         //     return result;
//         // }

//         // // [dho] `if(bar < 100 && foo.bar(x)` has value dependencies `[bar, foo.bar(x)]` - 15/05/19
//         // private Result<object> InferValueDependencies(RawAST ast, Node node, Dictionary<string, int> sss)
//         // {
//         //     var result = new Result<object>();

//         //     foreach(var (child, hasNext) in ASTNode.IterateChildren(ast, node.ID))
//         //     {
//         //         if(child.Kind == SemanticKind.Invocation)
//         //         {
//         //             sss[child.ID] = sss.Count;

//         //             // var invocation = ASTNode.Invocation(ast, child);

//         //             // foreach(var a in invocation.Arguments) 
//         //             // {
//         //             //     result.AddMessages(InferValueDependencies(ast, a, sss));
//         //             // }
//         //         }
//         //         else if(child.Kind == SemanticKind.Identifier)
//         //         {
//         //             sss[child.ID] = sss.Count;
//         //         }
//         //         else if(child.Kind == SemanticKind.NamedTypeConstruction)
//         //         {
//         //             sss[child.ID] = sss.Count;

//         //             // var namedTypeCon = ASTNode.NamedTypeConstruction(ast, child);

//         //             // foreach(var a in namedTypeCon.Arguments) 
//         //             // {
//         //             //     result.AddMessages(InferValueDependencies(ast, a, sss));
//         //             // }
//         //         }
//         //         // [dho] TODO CLEANUP HACK to check for constructions!! - 15/05/19
//         //         else if(child.Kind.ToString().EndsWith("Construction"))
//         //         {
//         //             sss[child.ID] = sss.Count;

//         //             // var members = ASTHelpers.QueryEdgeNodes(ast, child.ID, SemanticRole.Member);

//         //             // foreach(var m in members) 
//         //             // {
//         //             //     result.AddMessages(InferValueDependencies(ast, m, sss));
//         //             // }
//         //         }
//         //         else
//         //         {
//         //             result.AddMessages(InferValueDependencies(ast, child, sss));
//         //         }
//         //     }

//         //     return result;
//         // }

//         // private Result<object> EmitRunDirectiveComputation(Directive runDirective, Context context, CancellationToken token)
//         // {
//         //     var result = new Result<object>();

//         //     var childContext = ContextHelpers.Clone(context);
//         //     // // childContext.Parent = runDirective;

//         //     var emission = context.Emission;

    
//         //     // emission.AppendBelow(runDirective, $"static void {ToRunDirectiveComputationFunctionIdentifier(runDirective)}(){{");
            
//         //     // emission.Indent();

//         //     // emission.Outdent();

//         //     // // [dho] end of function for run directive - 03/05/19
//         //     // emission.AppendBelow(runDirective, "}");
 
//         //     return result;
//         // }

    

//         // [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         // private void EmitIfDirectiveEffect(IfDirective ifDirective, Node branch, IEmission emission)
//         // {
//         //     if(branch != null)
//         //     {
//         //         // [dho] tell the compiler that we want to replace the current node with the members of
//         //         // inside the replacement - 19/04/19
//         //         emission.AppendBelow(ifDirective, $"{ReplaceNodeWithNodesFunctionIdentifier}(\"{ifDirective.ID}\",");

//         //         // [dho] NOTE we do not replace with the true branch itself, as if this is a block `{...}` then the compiler
//         //         // would not know to unpack all statements in the block, or to use an actual block.. so we have to be
//         //         // unambiguous about what we want to replace the if directive with - 19/04/19
//         //         foreach(var (child, hasNext) in ASTNode.IterateMembers(branch, true /* filter nulls */))
//         //         {
//         //             emission.Append(child, $"\"{child.ID}\"" + (hasNext ? "," : ""));
//         //         }

//         //         emission.Append(ifDirective, ");");
//         //     }
//         //     else
//         //     {
//         //         emission.AppendBelow(ifDirective, $"{DeleteNodeFunctionIdentifier}(\"{ifDirective.ID}\");");
//         //     }
//         // }

//         private Result<object> EmitStaticInitializer(ASTNode nodeWrapper, List<Node> initItems, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();
//             // [dho] Append the static initializer function for the class, that will ensure the hooks are all
//             // set up for itself, and nested classes - 18/04/19
//             context.Emission.AppendBelow(nodeWrapper, $"public static void {ToStaticInitIdentifier()}(){{");

//             context.Emission.Indent();

//             // [dho] NOTE putting the initializers for nested classes first implies that
//             // nested hooks fire before parent hooks
//             foreach(var item in initItems)
//             {
//                 context.Emission.AppendBelow(nodeWrapper, "");

//                 result.AddMessages(
//                     EmitNode(item, context, token)
//                 );
//             }

//             context.Emission.Outdent();

//             // [dho] end static initializer - 18/04/19
//             context.Emission.AppendBelow(nodeWrapper, "}");

//             return result;
//         }

//         // private Node WrapWithExistentialDependencyAssertion(Node node, string name, Context context, CancellationToken token)
//         // {
//         //     if(ExistentialDependencies.Count == 0) return node;

//         //     // [dho] create an assertion that will fail if the node's existence depends on other `if` directives
//         //     // evaluting to `true`. This will prevent code being able to invoke functions at compile time that are inside `if` 
//         //     // directives that have evaluated to false - 13/04/19

//         //     Block block = NodeFactory.Block(context.AST, node.Origin);

//         //     OrderedGroup content = NodeFactory.OrderedGroup(context.AST, node.Origin);

//         //     content.Members = new NodeChildren(content);


//         //     StringBuilder assertionString = new StringBuilder("assert (");

//         //     string errorDescription = MessageToMarshalledString(node, MessageKind.Error, $"'{name}' does not exist in the current context");

//         //     foreach(var ifDirective in ExistentialDependencies)
//         //     {
//         //         assertionString.Append($"!{EmittedIfDirectiveDidSettleFlagIdentifier(ifDirective)} || {EmittedIfDirectiveResultIdentifier(ifDirective)} || ");
//         //     }

//         //     // [dho] HACK appending true just so we do not have to worry about unclosed `||` logic statement! - 13/04/19
//         //     assertionString.Append("true");

//         //     assertionString.Append($") : \"{errorDescription}\"");

//         //     Node assertion = NodeFactory.CodeConstant(context.AST, node.Origin, assertionString.ToString());

//         //     content.Members.Add(assertion);

//         //     content.Members.Add(node);

//         //     block.Content = content;

//         //     return block;
//         // }

//         public override Result<object> EmitFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
//         {
//             // var cachedBody = node.Body;
//             // var existentialCheckBody = WrapWithExistentialDependencyAssertion(cachedBody, ASTNode.GetLexeme(node.Name), context, token);

//             // [dho] because we wrap everything in an outer class, we will convert any functions we find into static methods - 29/04/19
//             if(LanguageSemantics.Java.GetEnclosingScopeStart(context.AST, node.Node, token)?.Kind == SemanticKind.Component)
//             {
//                 return EmitFunctionLikeDeclaration(node, context, token);
//             }
//             else
//             {
//                 // node.Body = existentialCheckBody;

//                 var result = base.EmitFunctionDeclaration(node, context, token);

//                 // node.Body = cachedBody;
                
//                 return result;
//             }
//         }

//         // private void TransferFunctionLikeDeclarationProperties(FunctionLikeDeclaration source, FunctionLikeDeclaration destination)
//         // {
//         //     destination.Meta = source.Meta;
//         //     destination.Modifiers = source.Modifiers;
//         //     destination.Annotations = source.Annotations;
//         //     destination.Name = source.Name;
//         //     destination.Parameters = source.Parameters;
//         //     destination.Template = source.Template;
//         //     destination.Type = source.Type;
//         //     destination.Parent = source.Parent;
//         //     destination.Body = source.Body;
//         // }

//         // public override Result<object> EmitConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
//         // {
//         //     // ++ScopeDepth;

//         //     var cachedBody = node.Body;

//         //     node.Body = WrapWithExistentialDependencyAssertion(cachedBody, ASTNode.GetLexeme(node.Name), context, token);

//         //     var result = base.EmitConstructorDeclaration(node, context, token);

//         //     node.Body = cachedBody;

//         //     // --ScopeDepth;

//         //     return result;
//         // }

//         // public override Result<object> EmitMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
//         // {
//         //     // ++ScopeDepth;

//         //     var cachedBody = node.Body;

//         //     node.Body = WrapWithExistentialDependencyAssertion(cachedBody, ASTNode.GetLexeme(node.Name), context, token);

//         //     var result = base.EmitMethodDeclaration(node, context, token);

//         //     node.Body = cachedBody;

//         //     // --ScopeDepth;

//         //     return result;
//         // }

//         public override Result<object> EmitLambdaDeclaration(LambdaDeclaration node, Context context, CancellationToken token)
//         {
//             // ++ScopeDepth;

//             var result = base.EmitLambdaDeclaration(node, context, token);

//             // --ScopeDepth;

//             return result;
//         }

//         public override Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
//         {
//             // [dho] TODO! Involves some rewriting of symbols if referring to other files in the project, seeing as
//             // we are inling everything in to one file with nested classes in this emission - 12/04/19
//             return CreateUnsupportedFeatureResult(node);
//         }

//         public override Result<object> EmitInvocation(Invocation node, Context context, CancellationToken token)
//         {
//             // [dho] only emit invocations that are not at file scope level (we want to remove dynamism) - 12/04/19
//             // if(ScopeDepth > 0)
//             // {
//                 return base.EmitInvocation(node, context, token);
//             // }
//             // else
//             // {
//             //     var result = new Result<object>();
            
//             //     result.AddMessages(new NodeMessage(MessageKind.Info, "Skipping emission of Invocation because it occurs at file scope level", node)
//             //         {
//             //             Hint = GetHint(node.Origin),
//             //             Tags = DiagnosticTags
//             //         }
//             //     );

//             //     return result;
//             // }
//         }

 
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


//         class DirectiveXEmitter : JavaEmitter
//         {
//             private readonly CTJavaEmitter Emitter;
//             public DirectiveXEmitter(CTJavaEmitter emitter)
//             {
//                 Emitter = emitter;
//             }

//             public override Result<object> EmitNode(Node node, Context context, CancellationToken token)
//             {
//                 if(node.Kind != SemanticKind.DataValueDeclaration && 
//                     LanguageSemantics.Java.IsDeclarationStatement(context.AST, node))
//                 {
//                     if(node.Kind == SemanticKind.ObjectTypeDeclaration)
//                     {
//                         var className = ASTNodeHelpers.GetLexeme(ASTNodeFactory.ObjectTypeDeclaration(context.AST, node).Name);

//                         context.Emission.AppendBelow(node, $"{className}.{Emitter.ToStaticInitIdentifier()}();");
//                     }

//                     return new Result<object>();
//                 }

//                 return base.EmitNode(node, context, token);
//             }

//             public override Result<object> EmitDirective(Directive node, Context context, CancellationToken token)
//             {
//                 return Emitter.EmitDirective(node, context, token);
//             }
//         }


//         // [dho] in the case that #emit is arbtrarily nested inside other directives, we need to insert the 
//         // generated code at the root most #compiler directive, otherwise the generated nodes will get removed 
//         // from the tree when the #compiler directives are pruned - 18/05/19
//         private Directive _codegenInsertionPoint = null;

//         public override Result<object> EmitDirective(Directive node, Context context, CancellationToken token)
//         {
//             var result = new Result<object>();

//             var name = node.Name;

//             var directiveContextEmitter = new DirectiveXEmitter(this);

//             // [dho] TODO centralise the names of the compile time directives - 15/05/19
//             if(name == CTDirective.CodeExec)
//             {
//                 if(_codegenInsertionPoint == null) _codegenInsertionPoint = node;

//                 // [dho] NOTE we check whether the #directive is a value position, NOT the subject
//                 // of the directive because we are working out whether the purpose of running this #directive
//                 // was to generate a value - 16/05/19
//                 if(LanguageSemantics.Java.IsValueExpression(node.AST, node.Node))
//                 {
//                     context.Emission.AppendBelow(node, $"{InsertImmediateSiblingFromValueAndDeleteNodeFunctionIdentifier}(\"{node.ID}\",");

//                     result.AddMessages(directiveContextEmitter.EmitNode(node.Subject, context, token));

//                     context.Emission.Append(node, $",\"{node.ID}\");");
//                 }
//                 else
//                 {
//                     // [dho] We have to output the delete command first in case the subject contains a return statement
//                     // that would render the code invalid because the delete command was unreachable - 17/05/19
//                     context.Emission.AppendBelow(node, $"{DeleteNodeFunctionIdentifier}(\"{node.ID}\");");

//                     var subject = node.Subject;

//                     result.AddMessages(directiveContextEmitter.EmitNode(subject, context, token));

//                     if(RequiresSemicolonSentinel(subject))
//                     {
//                         context.Emission.Append(subject, ";");
//                     }
//                 }

//                 if(_codegenInsertionPoint == node) _codegenInsertionPoint = null;

//             }
//             else if(name == CTDirective.Emit)
//             {
//                 // context.Emission.AppendBelow(node, $"if(Boolean.FALSE.equals({EvalOnceFlagMapIdentifier}.get(\"{node.ID}\"))){{");

//                 // context.Emission.Indent();
//                 // {
//                 //     // [dho] mark this compile time directive so it doesn't get evaluated more than once - 16/05/19
//                 //     context.Emission.AppendBelow(node, $"{EvalOnceFlagMapIdentifier}.put(\"{node.ID}\", true);");

//                     var subject = node.Subject;

//                     if(subject.Kind == SemanticKind.Directive && 
//                         ASTNodeFactory.Directive(context.AST, (DataNode<string>)subject).Name == CTDirective.CodeExec)
//                     {
//                         var d = ASTNodeFactory.Directive(context.AST, (DataNode<string>)subject);

//                         var n = d.Name;

//                         if(n == CTDirective.CodeExec)
//                         {
//                             // [dho] NOTE we check whether the #directive is a value position, NOT the subject
//                             // of the directive because we are working out whether the purpose of running this #directive
//                             // was to generate a value - 16/05/19
//                             if(LanguageSemantics.Java.IsValueExpression(d.AST, d.Node))
//                             {
//                                 context.Emission.AppendBelow(node, 
//                                     $"{InsertImmediateSiblingFromValueAndDeleteNodeFunctionIdentifier}(\"{(_codegenInsertionPoint ?? node).ID}\",");

//                                 result.AddMessages(directiveContextEmitter.EmitNode(d.Subject, context, token));

//                                 context.Emission.Append(node, $",\"{node.ID}\");");
//                             }
//                             else
//                             {
//                                 result.AddMessages(new NodeMessage(MessageKind.Error, "Expected a value", d.Subject)
//                                 {
//                                     Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(d.Subject.Origin),
//                                     Tags = DiagnosticTags
//                                 });
//                             }
//                         }
//                         else
//                         {
//                             context.Emission.AppendBelow(node, 
//                                 $"{InsertImmediateSiblingAndDeleteNodeFunctionIdentifier}(\"{(_codegenInsertionPoint ?? node).ID}\",\"{subject.ID}\",\"{node.ID}\");");
//                         }
//                     }
//                     else
//                     {
//                         context.Emission.AppendBelow(node, 
//                             $"{InsertImmediateSiblingAndDeleteNodeFunctionIdentifier}(\"{(_codegenInsertionPoint ?? node).ID}\",\"{subject.ID}\",\"{node.ID}\");");
//                     }
//                 // }
//                 // context.Emission.Outdent();

//                 // context.Emission.AppendBelow(node, "}");                
//             }
//             else
//             {
//                 // [dho] any code that hits a bridge directive during CT exec is illegal because those artifacts have not been created yet! - 29/05/19
//                 context.Emission.AppendBelow(node, 
//                             $"{IllegalBridgeDirectiveNodeFunctionIdentifier}(\"{node.ID}\");");

//                 /*
//                     result.AddMessages(new NodeMessage(MessageKind.Error, $"Unsupported directive '{name}'", node)
//                     {
//                         Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(node.Origin),
//                         Tags = DiagnosticTags
//                     });
                
//                  */
//             }

//             return result;
//         }

        

//         // class DirectiveEmitter : JavaEmitter
//         // {
//         //     public override Result<object> EmitDirective(Directive node, Context context, CancellationToken token)
//         //     {
//         //         var result = new Result<object>();

//         //         var name = node.Name;

//         //         // [dho] need to think about this...!! - 15/05/19
//         //         which_entrypoint_function_do_we_call_on_the_delegate_emitters();

//         //         // [dho] TODO centralise the names of the compile time directives - 15/05/19
//         //         if(name == "#compiler")
//         //         {
//         //             result.AddMessages(new CompileTimeEffectsEmitter().EmitNode(node.Node, context, token));
//         //         }
//         //         else if(name == "#emit")
//         //         {
//         //             result.AddMessages(new Codegen().EmitNode(node.Node, context, token));
//         //         }
//         //         else
//         //         {
//         //             z; // [dho] here we need to handle directives that may describe other artifacts.. such as the auto binding stuff.. maybe we just dont do that for now?
//         //             // i guess we should report an error here if those are encountered!! - 15/05/19
//         //         }

//         //         return result;
//         //     }
//         // }

//         // class CompileTimeEffectsEmitter : DirectiveEmitter 
//         // {
//         //     public Result<object> NotINBake(Directive directive, Context context, CancellationToken token)
//         //     {
//         //         var result = new Result<object>();

//         //         result.AddMessages(EmitNode(directive.Subject, context, token));

//         //         context.Emission.AppendBelow(directive, $"{DeleteNodeFunctionIdentifier}(\"{directive.ID}\");");

//         //         return result;
//         //     }

//         //     // public override Result<object> EmitNode(Node node, Context context, CancellationToken token)
//         //     // {
//         //     //     if(node.Kind.ToString().EndsWith("Declaration"))
//         //     //     {
//         //     //         return new Result<object>();
//         //     //     }
//         //     //     else
//         //     //     {
//         //     //         return base.EmitNode(node, context, token);
//         //     //     }
//         //     // }

//         //     public override Result<object> EmitPredicateJunction(PredicateJunction node, Context context, CancellationToken token)
//         //     {
//         //         var result = new Result<object>();

//         //         var settleFlag = EmittedIfDirectiveDidSettleFlagIdentifier(node);
            
//         //         context.Emission.AppendBelow(node, $"if(!{settleFlag}){{");

//         //         context.Emission.Indent();

//         //         {
//         //             context.Emission.AppendBelow(node, $"{settleFlag}=true;"); // [dho] avoid evaluation the code again - 15/05/19

//         //             result.AddMessages(base.EmitPredicateJunction(node, context, token));

//         //             // [dho] do the node removal here?? or assume the parent directive will remove the whole tree for us? - 15/05/19
//         //         }

//         //         context.Emission.Outdent();

//         //         context.Emission.AppendBelow(node, "}");

//         //         return result;
//         //     }
//         // }

//         // class Codegen : DirectiveEmitter 
//         // {

//         // }


// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private void EmitMarshalledMessagePrintStatement(Node node, MessageKind kind, string description, IEmission emission)
//         {
//             // [dho] output a serialized version of this message that will be automatically parsed 
//             // by our command line consumer. We use this to inject 'events' in the compile time artifact,
//             // like if a node should be pruned from the AST etc - 12/04/19
//             emission.AppendBelow(node, $"System.out.println(\"{MessageToMarshalledString(node, kind, description)}\");");
//         }

//         private string MessageToMarshalledString(Node node, MessageKind kind, string description)
//         {
//             // [dho] prepend the ctID to the description, which we will use to detect
//             // messages that we need to parse when the compile time artifact is consumed - 12/04/19
//             var message = new Message(kind, CTID + description)
//             {
//                 Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(node.Origin),
//                 Tags = DiagnosticTags
//             };

//             return "XXXXX!" + message.Description;//CommandLineDiagnosticsParser.GCC.Serialize(message);
//         }
//     }

// }