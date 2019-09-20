using System.Collections.Generic;

namespace Sempiler.Core.Directives
{
    public static class CTAPISymbols
    {
        public const string AddDependency = "addDependency";
        public const string AddEntitlement = "addEntitlement";
        public const string AddPermission = "addPermission";
        public const string AddRawSources = "addRawSources";
        public const string AddSources = "addSources";
        public const string Build = "build";
        public const string DeleteNode = "deleteNode";
        public const string ReplaceNodeByCodeConstant = "insertImmediateSiblingFromCodeConstantAndDeleteNode";
        public const string InsertImmediateSiblingFromValueAndDeleteNode = "insertImmediateSiblingFromValueAndDeleteNode";

        public static IEnumerable<string> EnumerateCTAPISymbolNames()
        {
            yield return AddDependency;
            yield return AddEntitlement;
            yield return AddPermission;
            yield return AddRawSources;
            yield return AddSources;
            yield return Build;
            yield return DeleteNode;
            yield return ReplaceNodeByCodeConstant;
            yield return InsertImmediateSiblingFromValueAndDeleteNode;
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