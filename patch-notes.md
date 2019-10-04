** IN PROGRESS : Rough repo patch notes **

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
- `IOSBundler` now allows for entrypoint function to access launch options, eg. `export default function main(launchOptions : { [key : UIApplication.LaunchOptionsKey] : Any }){...}`
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