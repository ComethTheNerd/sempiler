namespace Sempiler.AST
{
    using System;

    public enum TypeRole
    {
        None = 0, // treat the type name literally (passthrough)
        Boolean,
        String,
        
        Integer32,
        
        UnsignedInteger32,

        Float32,
        Double64,
    
        Char,

        // ........ other primitive types
        RootObject, // `object` in C#, `Object` in Java

        Any,
        Never,
        Void,
        Error
    }

}