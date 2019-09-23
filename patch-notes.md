** IN PROGRESS : Rough repo patch notes **

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