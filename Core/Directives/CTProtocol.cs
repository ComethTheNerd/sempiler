using Sempiler.AST;

namespace Sempiler.Core.Directives
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

        AddCapability,
        AddDependency,
        AddEntitlement,
        AddPermission,
        AddSources,
        AddRawSources,
        IllegalBridgeDirectiveNode

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
        public CTProtocolCommandKind Kind;
        public string[] Arguments;
        
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

    public static class CTProtocolAddDependencyCommand
    {
        public const int NameIndex = 0;
        public const int VersionIndex = 1;
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

    public static class CTProtocolAddSourcesCommand
    {
        public const int BaseDirPathIndex = 0;
    }

    public static class CTProtocolAddRawSourcesCommand
    {
        public const int BaseDirPathIndex = 0;
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
                var artifactName = message.Substring(0, cmdStartIndex);
                
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
                            Kind = kind,
                            Arguments = arguments
                        };
                    }
                    catch
                    {

                    }
                }
            }

            return new CTProtocolCommand
            {
                ArtifactName = null,
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