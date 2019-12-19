** IN PROGRESS : Rough notes to maintenance guide / API reference **

# Session

# Artifact

# Shard

# AST

# Node

# ASTNode

# Domain

The root of the AST is a domain node. 

The children of the domain are component instances.

The domain has no parent.

# Component

A component can be thought of as something that will produce an output file in the generated shard.

A component could be instantiated by parsing a source file, or from some internal compiler operation such as bundling - whereby manifest files for a target platform (eg. iOS) are implemented as synthesized components.

# Component Cache

The session maintains a component cache. Any component that is created during a compilation session (usually by parsing an input source) is cached in the session.

Different ASTs can then deep register a component from the cache. This saves having to construct the component (which can be expensive, especially if parsed from a source file).

Deep register means that references to all contained nodes in the component are added to a given AST. The AST can then modify the branches of the component tree cheaply, without it impacting any other AST that is using that same component.

This has the added benefit of the compiler being able to generate the CT Exec file for a given component (eg. source file) once, and share it across artifacts and shards - because the node IDs will be present in each AST (every shard has an AST).

Lastly, the memory footprint is lessened because the same source file referenced across shards will share a single component, rather than a duplicate for each shard.




# Symbolic Dependencies

## Unresolved Symbols

If a piece of code depends on a symbol that cannot be resolved to a known declaration in the AST, it will still be returned as a `SymbolicDepency` but the `Declaration` propery will not be set (NOTE: considering using a special `SemanticKind` for this instead of null).

Nodes with unresolved symbolic dependencies are considered **not statically computable**.

This means that the node will be excluded from compile time execution. 

For example, referencing `process` in Node (a globally available symbol) will result in an unresolved symbolic dependency. Even though the compile time execution currently takes place in Node it would be incorrect to *path* this declaration (ie. supply a shim for it) because we are supposed to respect the target platform (not the platform we are using for compile time execution).


# Compile Time Execution (CTExec)

## Restrictions

Any imports that are not inside the scope of a CTExec directive will be ignored. This is necessary because imports that also execute at run time may reference files or libraries that only exist in the context of the target platform. The compiler context has no access to the target platform context.

You cannot use `#codegen` on values that are not statically known. For example, a `#codegen` cannot try to bake in a parameter reference, because that parameter may be different each time a function is invoked, and so the generate code generation cannot be baked.






## API

### Adding API Symbols
To add a `#compiler` API symbol (eg. a compiler API function that can be called at compile time):
- in `Core/Directives/CTProtocol.cs`, add `CTProtocolCommandKind` and `CTProtocolCommand`
- in `Core/Main.cs`, add case in `HandleServerMessage(...)`
- in `Core/Directives/CTAPISymbols.cs`, add `CTAPISymbols` string and ensure symbol is in `EnumerateCTAPISymbolNames`
- in `Core/Bundling/CTExecTypeScriptBundler.cs`, inside `TypeScriptInlining` add the function declaration, the symbol in the return statement from the IIFE, and if it is a public symbol, also add it on the line that exposes constants for the client code to reference 


# Troubleshooting

## Package Restore Error

Packages fail to be restored citing incompatible .NET core versions

**FIX** Open project in VS Mac and let it restore packages there. Come back to VS Code and refresh window.