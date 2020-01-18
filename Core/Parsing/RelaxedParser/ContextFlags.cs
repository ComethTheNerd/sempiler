using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Sempiler.Parsing
{
    [Flags]
    public enum ContextFlag
    {
        None = 0,
        Let = 1 << 0, // Variable declaration
        Const = 1 << 1, // Variable declaration
        NestedNamespace = 1 << 2, // Namespace declaration
        Synthesized = 1 << 3, // Node was synthesized during transformation
        Namespace = 1 << 4, // Namespace declaration
        ExportContext = 1 << 5, // Export context (initialized by binding)
        ContainsThis = 1 << 6, // Interface contains references to "this"
        HasImplicitReturn = 1 << 7, // If function implicitly returns on one of codepaths (initialized by binding)
        HasExplicitReturn = 1 << 8, // If function has explicit reachable return on one of codepaths (initialized by binding)
        GlobalAugmentation = 1 << 9, // Set if module declaration is an augmentation for the global scope
        HasAsyncFunctions = 1 << 10, // If the file has async functions (initialized by binding)
        DisallowInContext = 1 << 11, // If node was parsed in a context where 'in-expressions' are not allowed
        YieldContext = 1 << 12, // If node was parsed in the 'yield' context created when parsing a generator
        DecoratorContext = 1 << 13, // If node was parsed as part of a decorator
        AwaitContext = 1 << 14, // If node was parsed in the 'await' context created when parsing an async function
        ThisNodeHasError = 1 << 15, // If the parser encountered an error when parsing the code that created this node
        JavaScriptFile = 1 << 16, // If node was parsed in a JavaScript
        ThisNodeOrAnySubNodesHasError = 1 << 17, // If this node or any of its children had an error
        HasAggregatedChildData = 1 << 18, // If we've computed data from children and cached it in this node

        // // [dho] adding this because parsing an object literal is ambiguous between dynamic type construction
        // // and dictionary construction. In the case where we know its a dictionary, we should influence the parser - 10/01/20
        // Dictionary = 1 << 19,

        BlockScoped = Let | Const,

        ReachabilityCheckFlags = HasImplicitReturn | HasExplicitReturn,
        ReachabilityAndEmitFlags = ReachabilityCheckFlags | HasAsyncFunctions,

        // Parsing context flags
        ContextFlags = DisallowInContext | YieldContext | DecoratorContext | AwaitContext | JavaScriptFile,

        // Exclude these flags when parsing a Type
        TypeExcludesFlags = YieldContext | AwaitContext
    }
}