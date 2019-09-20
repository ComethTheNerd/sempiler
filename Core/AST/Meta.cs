namespace Sempiler.AST
{
    using System;

    [Flags]
    public enum MetaFlag
    {
        // None = 0,
        Abstract = 1 << 0,
        Mutation = 1 << 1, // mutating func in Swift, out param C#... we have Mutable too.. lets clean this up
        Override = 1 << 2,
        Virtual = 1 << 3,
        Static = 1 << 4,
        Optional = 1 << 5,
        Convenience = 1 << 6,
        ReadOnly = 1 << 7, // what use is this vs constant?
        Volatile = 1 << 8,
        Constant = 1 << 9, // final, sealed, js const (Constant | BlockScope)
        Transient = 1 << 10,
        Synchronized = 1 << 11,
        FixedAddress = 1 << 12, // GC will not move a variable
        ByReference = 1 << 13,
        Operator = 1 << 14,
        Asynchronous = 1 << 15,
        TypeVisibility = 1 << 16,
        PackageVisibility = 1 << 17,
        SubtypeVisibility = 1 << 18,
        WorldVisibility = 1 << 19,
        FileVisibility = 1 << 20,
        FriendVisibility = 1 << 21,
        AssemblyVisibility = 1 << 22,
        Generator = 1 << 23,
        Mutable = 1 << 23,
        BlockScope = 1 << 24, // js let
        FunctionScope = 1 << 25, // js var
        StackAllocated = 1 << 26, // C# struct
        HeapAllocated = 1 << 27, // C# class

        GlobalSearch = 1 << 28,
        CaseInsensitive = 1 << 29,
        MultiLineSearch = 1 << 30,
        DotsAsNewLines = 1 << 31,
        UnicodeCodePoints = 1 << 32,
        StickySearch = 1 << 33,
        FlagSet = 1 << 34, // [dho] like bitset, OptionSet etc - 24/04/19
        ValueType = 1 <<35
    }

    public static class MetaHelpers 
    {
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

            foreach(var meta in ASTHelpers.QueryEdgeNodes(ast, node.ID, SemanticRole.Meta))
            {
                flags |= ASTNodeFactory.Meta(ast, (DataNode<MetaFlag>)meta).Flags;
            }

            return flags;
        }
    }
}