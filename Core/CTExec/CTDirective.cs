
namespace Sempiler.CTExec
{
    public static class CTDirective
    {
        public const string CodeExec = "compiler";
        public const string CodeGen = "codegen";

        public static bool IsCTDirectiveName(string input)
        {
            switch(input)
            {
                case CodeExec:
                case CodeGen:
                    return true;

                default:
                    return false;
            }
        }
    }
}