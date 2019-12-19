using System.Collections.Generic;

namespace Sempiler.CTExec
{
    public static class CTAPISymbols
    {
        public const string AddArtifact = "addArtifact";
        public const string AddCapability = "addCapability";
        public const string AddDependency = "addDependency";
        public const string AddEntitlement = "addEntitlement";
        public const string AddAsset = "addAsset";
        public const string AddPermission = "addPermission";
        public const string AddRes = "addRes";
        public const string AddRawSources = "addRawSources";
        public const string AddSources = "addSources";
        public const string AddShard = "addShard";
        public const string IsArtifactName = "isArtifactName";
        public const string IsTargetLanguage = "isTargetLanguage";
        public const string IsTargetPlatform = "isTargetPlatform";
        public const string SetDisplayName = "setDisplayName";
        public const string SetTeamName = "setTeamName";
        public const string SetVersion = "setVersion";
        public const string DeleteNode = "deleteNode";
        public const string ReplaceNodeByCodeConstant = "replaceNodeByCodeConstant";
        // public const string InsertImmediateSiblingFromValueAndDeleteNode = "insertImmediateSiblingFromValueAndDeleteNode";
        public const string Terminate = "terminate";

        public static IEnumerable<string> EnumerateCTAPISymbolNames()
        {
            yield return AddArtifact;
            yield return AddCapability;
            yield return AddDependency;
            yield return AddEntitlement;
            yield return AddAsset;
            yield return AddPermission;
            yield return AddRes;
            yield return AddRawSources;
            yield return AddSources;
            yield return AddShard;
            yield return IsArtifactName;
            yield return IsTargetLanguage;
            yield return IsTargetPlatform;
            yield return SetDisplayName;
            yield return SetTeamName;
            yield return SetVersion;
            yield return DeleteNode;
            yield return ReplaceNodeByCodeConstant;
            // yield return InsertImmediateSiblingFromValueAndDeleteNode;
            yield return Terminate;
        }

        public static bool IsCTAPISymbolName(string input)
        {
            foreach(var symbol in EnumerateCTAPISymbolNames())
            {
                if(symbol == input)
                {
                    return true;
                }
            }

            return false;
        }
    }
}