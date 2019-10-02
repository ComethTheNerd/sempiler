# 🗓 02/10/19
- 🐛 `ImportInfo` fails to find symbolic references to imported clauses (eg. `import { A, B, C } from "../x";`). In this case they are types that referred to symbolically in type parameters and type annotations (need to investigate whether it applies to only types, or all symbols - maybe types are considered inelligible for currently symbolic reference matching)  *[dho]*
- 🐛 Firebase Functions Bundler currently rewrites import declarations to require statements because imports would be invalid in inlined IIFEs, however when importing types this will be incorrect as the TypeScript compiler erases types during compilation. What we need to do instead is rewrite the imported symbol name and hoist all imports to the top level, and let the TypeScript compiler remove those that refer to types )  *[dho]*

# 🗓 30/09/19
- 🐛 Matching property name identifier and value identifier causes stack overflow when trying to find reference matches, eg. `let remoteCommandManager = new Bar(...{ foo })` OR `let remoteCommandManager = new Bar(...{ foo : foo })` (NOTE: did some work already to fix this kind of thing but this may be a different issue) *[dho]*

# 🗓 26/09/19
- 🐛 `addDependency` should dedup, and also complain if the input code asks adds the same library multiple times with different version specifiers *[dho]*

# 🗓 23/09/19
- 🐛 Semicolon inside interpolated string expression causes `NullPointerException` (eg. `hello ${[1,2,3].join();} world`);  *[dho]*
- 💭 Should we use `Unknown` as a new Node type for cases where the declaration was not found, instead of `null`?  *[dho]*
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
- 🐛 **(CANNOT REPRO 30/09/19 TODO CHECK! *[dho]*)**  compiler throws exception if it finds codegen in properties: 
```typescript
const a = new Foo(...{
    b : #codegen `...`
})
```
 *[dho]*
