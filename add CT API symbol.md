To add a `#compiler` API symbol (eg. a compiler API function that can be called at compile time):
- in `Core/Directives/CTProtocol.cs`, add `CTProtocolCommandKind` and `CTProtocolCommand`
- in `Core/NewCompilerAPI.cs`, add case in `HandleServerMessage(...)`
- in `Core/Directives/CTAPISymbols.cs`, add `CTAPISymbols` string and ensure symbol is in `EnumerateCTAPISymbolNames`
- in `Core/Bundling/CTExecTypeScriptBundler.cs`, inside `TypeScriptInlining` add the function declaration, the symbol in the return statement from the IIFE, and if it is a public symbol, also add it on the line that exposes constants for the client code to reference 