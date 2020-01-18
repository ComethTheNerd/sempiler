
namespace Sempiler.CTExec
{
    public static class CTDirective
    {
        public const string CodeExec = "compiler";
        public const string Emit = "emit";

        public static bool IsCTDirectiveName(string input)
        {
            switch(input)
            {
                case CodeExec:
                case Emit:
                    return true;

                default:
                    return false;
            }
        }
    }
}