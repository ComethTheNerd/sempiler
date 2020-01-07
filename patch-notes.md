** IN PROGRESS : Rough repo patch notes **

# 🗓 07/01/20
- SwiftUI transformer now uses unique lexeme for geometry handle when injecting code to `matchParent` dimensions, instead of using the lexeme `parent` which has a higher probability of clashing with symbols in user code

# 🗓 06/01/20
- SwiftUI transformer now injects `some` modifier to any function that has return type `: View`, in order to satisfy the Swift compiler
- Adding `BUILD_LIBRARY_FOR_DISTRIBUTION=YES` build setting to ensure compatibility between the user code, and frameworks compiled with a different version of the Swift compiler (https://stackoverflow.com/a/58656323/300037)

# 🗓 05/01/20
- Adding timers to investigate improving performance
- Passing session and artifact to preserved debug file paths bundler function to allow for artifact specific file preservation

# 🗓 04/01/20
- Added support for Swift Package Manager dependencies in iOS bundler

# 🗓 02/01/20
- Fixed bugs around transforming ternary expressions in view constructions for Swift targets
- Increased socket server buffer size from 1kb to 512kb to help tackle performance issues 

# 🗓 01/01/20
- Added function to return the left hand side of a qualified access node

# 🗓 31/12/19
- Fixed issue where bundler preserved paths were not being preserved if the path contained more than one level

# 🗓 30/12/19 
- SwiftUI transformation now converts ternary expressions in views to their equivalent if statement, with only non-null branches present in the emitted output (instead of replacing `null` values with `EmptyView` and wrapping branches in `AnyView(...)` construct to satisfy swiftc)

# 🗓 28/12/19 
- Fixed bug where `for of` body was not being attached as child of `ForMembersLoop`
- Fixed bug for emitting strings containing escape sequences (ie. '\') in TypeScript Emitter
- Firebase Functions Bundler now only returns detailed unexpected error diagnostics if not in production environment (`process.env.ENV !== 'production'`)

# 🗓 27/12/19 
- Fix TypeScript Emitter bug for imported and exported symbols that are not aliased
- Firebase Functions Bundler no longer performs inlining, and instead emits one file per component
- Fix for issue that arose with previous Firebase Functions Bundler strategy that used inlining and IIFEs, which meant it was tricky to share type definitions between files

# 🗓 26/12/19 
- Firebase Functions Bundler `skipLibCheck` in generated TypeScript config to avoid build errors with untyped or badly typed dependencies
- Firebase Functions Bundler now hoists platform imports and uses `esModuleInterop` in generated TypeScript config (instead of nested dynamic `import(...)`) to avoid asynchronous IIFE wrapper at top level (which is invalid for Firebase Functions anyway)

# 🗓 24/12/19 
- Firebase Functions Bundler inlining change to support dynamic `import(...)`, so that type definitions are available to TypeScript compiler and source code

# 🗓 19/12/19 
- Fixed bug with parsing `httpVerb` annotation for Firebase Functions

# 🗓 14/12/19 
- Myriad fixes for bugs in AST mutation operations
- Fixed bug with parsing `keypath` annotation for iOS 

# 🗓 13/12/19
- Argument expressions are now parsed into `InvocationArgument` nodes, rather than just incorrectly using their raw expression value (without the wrapper)

# 🗓 11/12/19
- Sharing single CT exec Node process for all artifacts as opposed to overhead of orchestrating one process per artifact

# 🗓 10/12/19
- Added compiler internal artifact placeholder so there is now no longer a need to have the `build(..)` command as the first line of the input file. CT exec requires an artifact instance, which is now the compiler artifact by default, and replaced by explicitly created artifacts in the input sources 
- Renamed `build` to `addArtifact`
- Added source file parameter to `addArtifact` so caller has to specify the root input file for an artifact now
- Renamed `Ancillary` to `Shard`

# 🗓 09/12/19
- Added ability to query AST by `SemanticKind` to find all Nodes in tree of particular kind in O(1) time

# 🗓 07/12/19
- Fix for bug in copying component state accidentally when cloning a component from the cache

# 🗓 03/12/19
- Component caching so a source file only has to be read from disk and parsed once
- iOS bundler no longer combines separate ancillary ASTs into a single AST before emission

# 🗓 01/12/19
- Extracted CT exec code into new centralized `CTExec` directory
- Clearer and cleaner logging output for determining if a session was successful or failed 📘📕📙📗

# 🗓 26/11/19
- Separated compilation into logical frontend and backend phases
- Removed some instances of parallelization to simplify engine for now and make debugging easier

# 🗓 25/11/19
- `CTExecInfo` added to `Session` object to help share CT exec information across the compilation of separate artifacts

# 🗓 21/11/19
- Null coallesce operator support (eg. `x ?? y`)

# 🗓 16/11/19
- iOS Bundler now supports setting team name (front end API `setTeamName(...)`)
- iOS Bundler now supports setting display name (front end API `setDisplayName(...)`)
- iOS Bundler now supports setting version (front end API `setVersion(...)`)
- Backend support for setting orientation flags (no frontend API yet)
- iOS Bundler now supports setting orientation
- Support for adding `"font"` asset role for adding font resources
- iOS Bundler now bundles font assets

# 🗓 09/11/19
- Frontend `addAsset(..)` CT exec support for adding image and app icon assets to a build, implementing in iOS Bundler backend

# 🗓 07/11/19
- iOS Bundler supports _magic_ attribute `matchParent` on `ViewDeclarations` which performs the necessary heavy-lifting wrapping view constructions with `GeometryReader` logic to fill the available space in the parent

# 🗓 06/11/19
- Added SwiftUI definitions for `ForEach` and `LinearGradient`

# 🗓 05/11/19
- `IOSSwiftUITransformer` automatically detects multiple return statements in a `ViewDeclaration`, and wraps each in an `AnyView(...)` call to satisfy the swiftc compiler

# 🗓 31/10/19
- Firebase Functions Bundler now parses parameters from `req.params` and `req.query` for GET requests

# 🗓 30/10/19
- Fixed emission of membership tests, and erroneous semicolons and braces for fall through switch clauses

# 🗓 29/10/19
- Added support in Firebase Functions Bundler for exporting data value declarations (symbols), so now you can write `export const foo = require("firebase-functions").pubsub.schedule(...).onRun(...)` and the compiler is smart enough to not wire it up as an express route
- Firebase Functions Bundler no longer dereferences the `user` object in route handler, so the programmer has to explicitly use `this.user` or `const { user } = this` in their route handler
- Firebase Functions Bundler now also passes the `req` and `res` objects in the route handler context, ie. `const { req, res } = this`
- Firebase Functions Bundler now supports forcing the HTTP verb to use for a route through a new annotation `@httpVerb("post")` (note, by default the verb is `GET` if the route has no parameters, and `POST` otherwise)
- Fixed bug in TypeScript Emitter where the operands for a type alias were emitted in reverse order
- Fixed bug in Swift Emitter where the operands for a type alias were emitted in reverse order

# 🗓 28/10/19
- Added support for `@extension` annotation on `ObjectTypeDeclaration` for declaring extensions to existing classes in languages like Swift and C#

# 🗓 24/10/19
- FirebaseBundler now infers HTTP response code based on type of error thrown, and whether it was expected or not (`TypeError`, `ReferenceError`, `RangeError` etc)

# 🗓 21/10/19
- Fix for live lock during CT Exec when nested sources were waiting on Duplex Socket Server to accept new connections

# 🗓 20/10/19
- Added SwiftUI definition for built-in `Alert` component
- Resources are now per Ancillary, rather than per Artifact, to allow for Ancillaries to use their own distinct resources without clashes

# 🗓 19/10/19
- Fixed bugs in iOS Bundler around share extension generation

# 🗓 18/10/19
- Added AST `DeepRegister(...)` helper function that registers all nodes from a source subtree in a destination subtree, for quick _copying_ of arbitrarily deep subtrees
- iOS Bundler no longer generates `AppDelegate` and `SceneDelegate` automatically
- iOS Bundler no longer inlines all components into single output file, due to constraints of this abstraction when generating arbitrarily complex source programs in a way that stays true to the source. Components are emitted to separate files and shared between multiple ancillary schemes in the manifest
- iOS Bundler now omits empty components from the emitted artifact bundle for more compact output

# 🗓 17/10/19
- Fixed iOS Bundler so it now produces a valid `.xcodeproj` with schemes and Podfile for main app and share extension
- Added compile time API support for Ancillary creation with `addAncillary(role, entrypointSource)`
- Cleaned up naming convention for AST manipulation API

# 🗓 16/10/19
- Artifacts can now have ancillaries (such as an iOS share extension) that define the multiple entrypoints/usages in context

# 🗓 14/10/19
- Fix for CT Exec choking on Compiler Hint nodes (eg. `declare let x : any`), now they are discarded from the AST used for compile time evaluation
- Fix for computed properties for single identifiers (eg. `[x] : ...`) being treated as normal identifiers (ie. `x`)
- Added new `SemanticKind` for `ComputedValue`
- Added emission support for `ComputedValue` in TypeScript Emitter
- Added limited emission support for `DynamicTypeConstruction` in Swift Emitter, specifically only when keys are identifiers

# 🗓 06/10/19
- Fixed bug where new AST edges were connected with indices based on live edges rather than all edges
- Renamed `GetEdges` helper function to `GetLiveEdges` to provide clarity

# 🗓 05/10/19
- `use_frameworks!` only included in Pod if target language is Swift
- Mapped `init` arguments for `TabView` in SwiftUI
- Fundamental reimplementation of CT Exec code generation to fix bugs with CT API messages being replayed multiple times which caused an RST message that kills the socket connection
- Fixed bug in iOS Bundler where `static` modifier was errantly added to declarations at file scope

# 🗓 04/10/19
- Bundlers can now specify files and directories to *preserve* (reuse) between debug emissions, such as folders containing dependencies that might otherwise be time consuming and frustrate to populate each time
- Bug fixes around directive parsing meaning they can now be used robustly in more expression and statement contexts, such as inline in argument lists (eg. `foo(x, #codgen ..., y)`)
- Added support for annotations and modifiers on `export` declarations
- Added support for detecting `@enforceAuth` annotation on inferred server routes
- Firebase Functions Bundler exposes `user` symbol implicitly in **all** route handler bodies (but value may be null if authorization is not enforced)
- Firebase Functions Bundler returns `401 Unauthorized` when authorization fails to validate for routes with `@enforceAuth`
- Disabling automatic qualification of incident symbol references in iOS Bundler because neither TypeScript semantics (source) or Swift semantics (target) do this implicitly, and also the current implementation does not account for inherited instance symbols
- Fixed inconsistencies with Firebase Functions Bundler error response schema
- Removed `data` wrapper around Firebase Functions Bundler response value
- iOS Bundler now configures `Info.plist` to allow local networking without TLS

# 🗓 03/10/19
- iOSBundler generated Podfile now specifier iOS 13.0 as target version, in lieu of making this configurable ultimately
- Swift/iOSBundler automatic qualifying of instance symbols by prefixing `self.` to instance symbol references inside closures
- `INamed` interface for AST Nodes that have a `Name` property
- Fix for passing correct operator token when creating binary expression

# 🗓 02/10/19
- Fixed parsing conditional expressions (eg. `x ? y : z`) caused by bug in parsing maybe null (safe unwraps) (eg `x?.y`)
- Fixed bug parsing not null expressions (eg. `x!.y`)
- Renamed placeholder name `Sempiler.Parsing.S1` to `Sempiler.Parsing.TokenUtils`
- Renamed placeholder name `Sempiler.Parsing.Lexer.XToken` to `Sempiler.Parsing.Lexer.Token`
- Fixed bug in Firebase Functions Bundler when failing to bail out early if a Component import specifier was not resolved
- Added support to specify the Proto internal duplex socket server port with command line switch (eg. `--p 1234`)

# 🗓 01/10/19
- Fix for exception caused by lexer reporting wrong position when parsing single identifier parameters
- Fixed bug when parser was silently eating an extra token in not null expressions (eg. `x!.y`)
- Added support for parsing maybe null (safe unwraps) (eg `x?.y`)

# 🗓 30/09/19
- Fix for body of error trap (eg. `try`) not being emitted by any target emitter
- Implemented error trap emission for Swift (`try/catch`)

# 🗓 29/09/19
- Added support for `addCapability(name : string, value : string | string[])` compile time API function
- Added support for `isTargetLanguage(name : string) : bool` compile time API function
- Added support for `isTargetPlatform(name : string) : bool` compile time API function
- iOS Bundler now allows for entrypoint function to access launch options, eg. `export default function main(launchOptions : { [key : UIApplication.LaunchOptionsKey] : Any }){...}`
- Fixed `NullPointerException` when `addRawSources(...)` did not match any file paths

# 🗓 28/09/19
- Fixed parsing `NullPointerException` when encountering type literal without members
- Fixed bug with `switch` that meant a clause body was null if it contained more than 1 statement

# 🗓 26/09/19
- Proto now has non zero exit code if compilation errors occurred
- Firebase Functions Bundler routes return error message, code and stack if an exception is thrown
- Fixed bug with TypeScript emitter not emitting annotations and modifiers on lambda declarations

# 🗓 24/09/19
- Firebase Functions Bundler bug fixes, now able to build a correct bundle from source files

# 🗓 23/09/19
- Many bug fixes around complex symbol resolution in arbitrarily nested scopes, including unresolved symbols now getting propagated to caller (rather than silently ignored)
- Majority of work done to support Firebase Functions as a valid target platform
- Fixed compile time execution of `await` and `throw` statements
- Better support for determining if a subtree is statically computable at compile time
- Fixed a lot of TypeScript emitter bugs
- Sharing previous duplicated code between bundlers
- Sharing previous duplicated code between language semantics
- Starting to collate notes for various documentation
- Other minor fixes

# 🗓 20/09/19
- initial migration to GitHub Sempiler repo started