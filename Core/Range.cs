namespace Sempiler
{
    public struct Range 
    {
        public readonly int Start;
        public readonly int End;

        public Range(int start = -1, int end = -1)
        {
            Start = start;
            End = end;
        }
    }

    public static class RangeHelpers
    {
        public static Range Clone(Range range)
        {
            // [dho] Range is a struct (value type) so it will have been
            // implicitly copied when passed to this function. If we ever change
            // Range to a reference type, here we will have to do more work to
            // actually copy the properties on the object to a new instance explicitly - 16/09/18
            return range;
        }

        public static bool IsValid(Range range)
        {
            return range.Start > -1 && range.End >= range.Start;
        }
    }
}