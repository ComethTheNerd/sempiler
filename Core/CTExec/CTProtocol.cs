using Sempiler.AST;

namespace Sempiler.CTExec
{
    public enum CTProtocolCommandKind
    {
        Unknown = 0,
        // [dho] Delete a node from the AST - 13/04/19
        DeleteNode,
        // [dho] Replace a node in the AST with a node parsed from a typed value - 13/04/19
        // ReplaceNodeWithValue,
        // // [dho] Replace a node in the AST with another node - 13/04/19
        // ReplaceNodeWithNodes,

        InsertImmediateSiblingAndFromValueAndDeleteNode,
        ReplaceNodeByCodeConstant,
        InsertImmediateSiblingAndDeleteNode,

        AddArtifact,
        AddCapability,
        AddDependency,
        AddEntitlement,
        AddManifestEntry,
        AddPermission,
        AddAsset,
        AddSources,
        AddRes,
        AddRawSources,
        AddShard,

        IsArtifactName,
        IsTargetPlatform,
        IsTargetLanguage,

        SetDisplayName,
        SetTeamName,
        SetVersion,

        IllegalBridgeDirectiveNode,

        Terminate

        // [dho] Propagate an error that originated in user code - 13/04/19
        // PropagateUserCodeError,

        /*
            Describing build setting stuff:
                - Adding a file to compile
                - Adding an emitter
                - Adding a consumer
                - Adding a transformer
        

            #emit bar(/$.ts/)



            #run foo();

            function foo()
            {
                Compiler.addInputFile("some path relative to my file", "foo.h", "bar.x");

                Compiler.addConsumer("hello", "d", "de");        
                Compiler.addEmitter("fdf", ".*");

                File.
            }
        
            function glsl_string(Node node)
            {

            }

            var x = #emit glsl_string 
                function hello()
                {
                    
                    
                }


        
         */
    }

    public struct CTProtocolCommand 
    {
        public string ArtifactName;
        public int ShardIndex;
        public string NodeID;
        public string MessageID;
        public CTProtocolCommandKind Kind;
        public string[] Arguments;
        
    }

    public static class CTProtocolTerminateCommand
    {
        public const int StatusCodeIndex = 0;
        public const int MessageIndex = 1;
        public const int FilePathIndex = 2;
        public const int LineNumberStartIndex = 3;
        public const int ColumnIndexStartIndex = 4;
    }

    public static class CTProtocolDeleteNodeCommand
    {
        public const int NodeIDIndex = 0;
    }

    // public static class CTProtocolReplaceNodeWithValueCommand
    // {
    //     public const int NodeIDIndex = 0;
    //     public const int TypeIndex = 1;
    //     public const int ValueIndex = 2;
    // }

    // public static class CTProtocolReplaceNodeWithNodesCommand
    // {
    //     public const int NodeIDIndex = 0;
    // }

    public static class CTProtocolAddCapabilityCommand
    {
        public const int NameIndex = 0;
        public const int TypeIndex = 1;

        // [dho] anything after index 1 is a value - 29/09/19
    }

    public static class CTProtocolAddManifestEntryCommand
    {
        public const int PathIndex = 0;
        public const int TypeIndex = 1;

        // [dho] anything after index 1 is a value - 18/01/19
    }

    public static class CTProtocolAddDependencyCommand
    {
        public const int NameIndex = 0;
        public const int VersionIndex = 1;
        public const int PackageManagerIndex = 2;
        public const int URLIndex = 3;
    }

    public static class CTProtocolAddEntitlementCommand
    {
        public const int NameIndex = 0;
        public const int TypeIndex = 1;

        // [dho] anything after index 1 is a value - 15/09/19
    }

    public static class CTProtocolAddPermissionCommand
    {
        public const int NameIndex = 0;
        public const int DescriptionIndex = 1;
    }

    public static class CTProtocolAddAssetCommand
    {
        public const int BaseDirPathIndex = 0;
        public const int RoleIndex = 1;
        public const int SourcePathIndex = 2;
    }

    public static class CTProtocolAddArtifactCommand
    {
        public const int BaseDirPathIndex = 0;
        public const int NameIndex = 1;
        public const int LanguageNameIndex = 2;
        public const int PlatformNameIndex = 3;
        public const int SourcePathIndex = 4;
    }


    public static class CTProtocolAddShardCommand
    {
        public const int BaseDirPathIndex = 0;
        public const int RoleIndex = 1;
        public const int SourcePathIndex = 2;
    }

    public static class CTProtocolAddSourcesCommand
    {
        public const int BaseDirPathIndex = 0;
    }

    public static class CTProtocolAddResCommand
    {
        public const int BaseDirPathIndex = 0;
        public const int SourcePathIndex = 1;
        public const int TargetFileNameIndex = 2;
    }

    public static class CTProtocolAddRawSourcesCommand
    {
        public const int BaseDirPathIndex = 0;
    }

    public static class CTProtocolIsArtifactNameCommand
    {
        public const int ArtifactNameIndex = 0;
    }

    public static class CTProtocolIsTargetPlatformCommand
    {
        public const int PlatformNameIndex = 0;
    }

    public static class CTProtocolIsTargetLanguageCommand
    {
        public const int LanguageNameIndex = 0;
    }

    public static class CTProtocolSetDisplayNameCommand
    {
        public const int NameIndex = 0;
    }

    public static class CTProtocolSetTeamNameCommand
    {
        public const int NameIndex = 0;
    }

    public static class CTProtocolSetVersionCommand
    {
        public const int VersionIndex = 0;
    }

    public static class CTInsertImmediateSiblingFromValueAndDeleteNodeCommand 
    {
        public const int InsertionPointIDIndex = 0; // [dho] the place to insert - 18/05/19
        public const int TypeIndex = 1;
        public const int ValueIndex = 2;

        public const int RemoveeIDIndex = 3; // [dho] the node to remove - 18/05/19
    }

    public static class CTReplaceNodeByCodeConstantCommand 
    {
        public const int RemoveeIDIndex = 0; // [dho] the node to replace - 18/05/19
        public const int CodeConstantIndex = 1;
    }

    public static class CTInsertImmediateSiblingAndDeleteNodeCommand
    {
        public const int InsertionPointIDIndex = 0; // [dho] the place to insert - 18/05/19
        public const int InserteeIDIndex = 1; // [dho] the node to insert - 18/05/19
        public const int RemoveeIDIndex = 2; // [dho] the node to remove - 18/05/19
    }

    public static class CTProtocolIllegalBridgeDirectiveNodeCommand
    {
        public const int NodeIDIndex = 0;
    }

    public static class CTProtocolHelpers
    {
        public const string CommandStartToken = ":--:";
        public const string ArgumentDelimiter = ";--;--;--;";

        public static CTProtocolCommand ParseCommand(string message)
        {
            var cmdStartIndex = message.IndexOf(CommandStartToken);

            if(cmdStartIndex > 0)
            {
                var preamble = message.Substring(0, cmdStartIndex);
                
                var preambleParts = preamble.Split(new string[] { ArgumentDelimiter }, System.StringSplitOptions.None);

                if(preambleParts.Length == 4)
                {
                    var artifactName = preambleParts[0];
                    var shardIndex = System.Int32.Parse(preambleParts[1]);
                    var nodeID = preambleParts[2];
                    var messageID = preambleParts[3];

                    var parenIndex = message.IndexOf("(");

                    if(parenIndex > 0 && message[message.Length - 1] == ')') // [dho] because we expect a number first - 20/04/19
                    {
                        try
                        {
                            var kindStartIndex = cmdStartIndex + CommandStartToken.Length;
                            var kindEndIndex = parenIndex - kindStartIndex;

                            var kind = (CTProtocolCommandKind)int.Parse(message.Substring(kindStartIndex, kindEndIndex));
                        
                            var argumentsStartIndex = parenIndex + 1;
                            var argumentsEndIndex = message.Length - 1;

                            var arguments = message.Substring(argumentsStartIndex, argumentsEndIndex - argumentsStartIndex).Split(new string[] { ArgumentDelimiter }, System.StringSplitOptions.None);

                            return new CTProtocolCommand
                            {
                                ArtifactName = artifactName,
                                ShardIndex = shardIndex,
                                NodeID = nodeID,
                                MessageID = messageID,
                                Kind = kind,
                                Arguments = arguments
                            };
                        }
                        catch
                        {

                        }
                    }
                }
            }

            return new CTProtocolCommand
            {
                ArtifactName = null,
                MessageID = null,
                Kind = CTProtocolCommandKind.Unknown,
                Arguments = new string[]{}
            };
        }
    


    
        // public static string DeleteNodeCommandString(Node node)
        // {
        //     return $"{CTProtocolMessageType.DeleteNode}({node.ID})";
        // }

        // public static string ReplaceNodeWithValueCommandString(Node node, string type, string value)
        // {
        //     return $"{CTProtocolMessageType.ReplaceNodeWithValue}({node.ID},{type},{value})";
        // }

        // public static string ReplaceNodeWithNodeCommandString(Node node, Node replacement)
        // {
        //     var parent = node.Parent;
        //     var parentID = parent.ID;
        //     var childID = node.ID;
        //     var replacementID = replacement.ID;

        //     var childIndex = NodeInterrogation.IndexOfChild(parent, childID);

        //     return $"{CTProtocolMessageType.ReplaceNodeWithNode}({parentID},{childIndex},{childID},{replacementID})";
        // }

        // public static string PropagateUserCodeErrorCommandString(Node node, string descriptionVarName)
        // {
        //     var parent = node.Parent;
        //     var parentID = parent.ID;
        //     var childID = node.ID;

        //     var childIndex = NodeInterrogation.IndexOfChild(parent, childID);

        //     return $"{CTProtocolMessageType.PropagateUserCodeError}({parentID},{childIndex},{childID},\"+{descriptionVarName}+\")";
        // }
    }
}