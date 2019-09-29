** IN PROGRESS : Rough repo patch notes **

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