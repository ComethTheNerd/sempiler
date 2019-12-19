namespace Sempiler.AST
{
    using System;

    [Flags]
    public enum MetaFlag : long
    {
        // None = 0,
        Abstract = 1L << 0,
        Mutation = 1L << 1, // mutating func in Swift, out param C#... we have Mutable too.. lets clean this up
        Override = 1L << 2,
        Virtual = 1L << 3,
        Static = 1L << 4,
        Optional = 1L << 5,
        Convenience = 1L << 6,
        ReadOnly = 1L << 7, // what use is this vs constant?
        Volatile = 1L << 8,
        Constant = 1L << 9, // final, sealed, js const (Constant | BlockScope)
        Transient = 1L << 10,
        Synchronized = 1L << 11,
        FixedAddress = 1L << 12, // GC will not move a variable
        ByReference = 1L << 13,
        Operator = 1L << 14,
        Asynchronous = 1L << 15,
        TypeVisibility = 1L << 16,
        PackageVisibility = 1L << 17,
        SubtypeVisibility = 1L << 18,
        WorldVisibility = 1L << 19,
        FileVisibility = 1L << 20,
        FriendVisibility = 1L << 21,
        AssemblyVisibility = 1L << 22,
        Generator = 1L << 23,
        Mutable = 1L << 23,
        BlockScope = 1L << 24, // js let
        FunctionScope = 1L << 25, // js var
        StackAllocated = 1L << 26, // C# struct
        HeapAllocated = 1L << 27, // C# class

        GlobalSearch = 1L << 28,
        CaseInsensitive = 1L << 29,
        MultiLineSearch = 1L << 30,
        DotsAsNewLines = 1L << 31,
        UnicodeCodePoints = 1L << 32,
        StickySearch = 1L << 33,
        FlagSet = 1L << 34, // [dho] like bitset, OptionSet etc - 24/04/19
        ValueType = 1L << 35,
        ExtensionType = 1L << 36, // [dho] like `extension` in Swift and C# - 28/10/19
        CTExec = 1L << 37, // computed at compile time

        EmittedFlagsMask = ~0 ^ CTExec // mask for flags that affect emission output
    }

    public static class MetaHelpers 
    {
        public static bool HasFlags(ASTNode node, MetaFlag flags) => HasFlags(node.AST, node.Node, flags);

        public static bool HasFlags(RawAST ast, Node node, MetaFlag flags)
        {
            return (MetaHelpers.ReduceFlags(ast, node) & flags) == flags;
        }

        public static MetaFlag ReduceFlags(ASTNode wrapper)
        {
            MetaFlag flags = 0x0;//MetaFlag.None;

            foreach(var (m, _) in ASTNodeHelpers.IterateMembers(wrapper.Meta))
            {
                flags |= ASTNodeFactory.Meta(wrapper.AST, (DataNode<MetaFlag>)m).Flags;
            }

            return flags;
        }

        public static MetaFlag ReduceFlags(RawAST ast, Node node)
        {
            MetaFlag flags = 0x0;//MetaFlag.None;

            foreach(var meta in ASTHelpers.QueryLiveEdgeNodes(ast, node.ID, SemanticRole.Meta))
            {
                flags |= ASTNodeFactory.Meta(ast, (DataNode<MetaFlag>)meta).Flags;
            }

            return flags;
        }
    }
}