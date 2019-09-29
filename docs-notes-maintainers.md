** IN PROGRESS : Rough notes to maintenance guide / API reference **

# AST
TODO

# Symbolic Dependencies

## Unresolved Symbols

If a piece of code depends on a symbol that cannot be resolved to a known declaration in the AST, it will still be returned as a `SymbolicDepency` but the `Declaration` propery will not be set (NOTE: considering using a special `SemanticKind` for this instead of null).

Nodes with unresolved symbolic dependencies are considered **not statically computable**.

This means that the node will be excluded from compile time execution. 

For example, referencing `process` in Node (a globally available symbol) will result in an unresolved symbolic dependency. Even though the compile time execution currently takes place in Node it would be incorrect to *path* this declaration (ie. supply a shim for it) because we are supposed to respect the target platform (not the platform we are using for compile time execution).


# Compile Time Execution

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