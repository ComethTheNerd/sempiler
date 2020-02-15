# 🗓 07/02/20
- 🐛 `addManifestEntry` assumes any array value is a string array, and any individual value is a string *[dho]*

# 🗓 03/02/20
- 🐛 Adding a `#compiler ... #emit` inside a source file leads to weird issue where the first line of the file was `import * as config from './config'` and the error was `Cannot emit a ReferenceAliasDeclaration out of context`, and somehow the `ImportDeclaration` part was missing and instead the `ReferenceAliasDeclaration` was a direct child of the `Component`. Possibly related to issue reported **20/01/20**, and maybe to do with `MetaFlag.CTExec`? *[dho]*

# 🗓 20/01/20
- 🐛 Nesting/mixing `#emit` with `#compiler` produces invalid CT exec code. We need to support the case where the emission is conditional and unconditional *[dho]*

# 🗓 16/01/20
- 🐛 If the last argument to a call is a lambda the SwiftUI emitter will emit it in braces form outside the argument list. However this seems to cause an issue, possibly this is because the signature for the function has optional arguments after the lambda argument in the declaration signature? *[dho]*

# 🗓 12/01/20
- 🐛 SwiftUI transformer does not correctly change nested ternary expressions eg. `x ? y : z ? a : b` is being transformed to `if x { y } else { z ? a : b }` *[dho]*

- 🐛 If function declares it returns `View` but delegates to other functions, swiftc will complain so we need the compiler to recognise when a function returns `View` and then wrap all the returns in `AnyView(...)` (just like it does with view constructions already) *[dho]*

# 🗓 11/01/20
- 🐛 Directives do not parse when used where type members are expected, eg. `interface Foo { x : y; #compiler addSource('z') }` *[dho]*

# 🗓 07/01/20
- 💡 Shall we move the `init.rb` execution into the compiler so what you get out is the an `.xcodeproj` file rather than an `init.rb`. Then it is on the user to just run `pod install` afterwards *[dho]*

# 🗓 05/01/20
- 💡 We need to support SPM version matching that is not exact in nature, but let user use wildcards etc *[dho]*

# 🗓 03/01/20
- 💡 It seems like SwiftUI does not support having local variables inside a `@ViewBuilder` closure at the moment (https://forums.swift.org/t/local-vars-in-swiftui-builders/26672/8), eg. this bites us when we use a `GeometryReader` and try to have local variables in it *[dho]*
- 💡 Swift Packages support rather than using Podfile (though we will still need the `init.rb` step because this builds the `.xcodeproj` file etc.) *[dho]*

# 🗓 20/10/19
- 🐛 Resolving source file patterns with a file path yields multiple fuzzy matches *[dho]*
- 🐛 **(FIXED 21/10/19  *[dho]*)** Compilation hangs for a session with multiple artifacts that have one or more facets *[dho]*

# 🗓 02/10/19
- 🐛 `ImportInfo` fails to find symbolic references to imported clauses (eg. `import { A, B, C } from "../x";`). In this case they are types that referred to symbolically in type parameters and type annotations (need to investigate whether it applies to only types, or all symbols - maybe types are considered inelligible for currently symbolic reference matching)  *[dho]*
- 🐛 Firebase Functions Bundler currently rewrites import declarations to require statements because imports would be invalid in inlined IIFEs, however when importing types this will be incorrect as the TypeScript compiler erases types during compilation. What we need to do instead is rewrite the imported symbol name and hoist all imports to the top level, and let the TypeScript compiler remove those that refer to types )  *[dho]*

# 🗓 30/09/19
- 🐛 Matching property name identifier and value identifier causes stack overflow when trying to find reference matches, eg. `let remoteCommandManager = new Bar(...{ foo })` OR `let remoteCommandManager = new Bar(...{ foo : foo })` (NOTE: did some work already to fix this kind of thing but this may be a different issue) *[dho]*

# 🗓 26/09/19
- 🐛 `addDependency` should dedup, and also complain if the input code asks adds the same library multiple times with different version specifiers *[dho]*

# 🗓 23/09/19
- 🐛 Semicolon inside interpolated string expression causes `NullPointerException` (eg. `hello ${[1,2,3].join();} world`);  *[dho]*
- 💡 Should we use `Unknown` as a new Node type for cases where the declaration was not found, instead of `null`?  *[dho]*
- 🐛 Use of enum causes `SyntaxError : Unexpected reserved word` during compile time execution (need to transpile enums..) *[dho]*


# 🗓 22/09/19
- 🐛 Server Inliner Info code currently treats exports from any file to be a 'route', but I think we only want exports from the artifact entrypoint to be considered 'routes'.. everything else is just an export for sharing symbols between files  *[dho]*
- 🐛 **(FIXED 23/09/19  *[dho]*)** `GetEnclosingScope` fails on non identifier names (eg `[request] = ...` or `{ x } = ...`)  *[dho]*
- 🐛 **(FIXED 22/09/19  *[dho]*)** Firebase Functions Bundler requires parameter names so will not work with `export foo({ x, y } : Bar)` because the param has no 'label'  *[dho]*
- 🐛 **(FIXED 22/09/19  *[dho]*)** `GetEnclosingScope`does not recognise `err` declaration in `localReadStream.pipe(remoteWriteStream).on('error', err => reject(err))`, and so cannot resolve the declaration when it is used  *[dho]*


# 🗓 17/09/19
- 🐛 Parser chokes on double semi colon `;;` (in this case, at end of a field declaration) *[dho]*


# 🗓 15/09/19
- 🐛 Infinite loop if JSX is not well formed in input *[dho]*
- 🐛 Not checking array lengths in `HandleServerMessage` *[dho]*


# 🗓 14/09/19
- 🐛 **(CANNOT REPRO 30/09/19 TODO CHECK! *[dho]*)**  compiler throws exception if it finds emit in properties: 
```typescript
const a = new Foo(...{
    b : #emit `...`
})
```
 *[dho]*
