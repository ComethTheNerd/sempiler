14/09/19
- compiler throws exception if it finds codegen in properties: 
```typescript
const a = new Foo(...{
    b : #codegen `...`
})
```

15/09/19
- `Directives` VSC task uses a stale build of the compiler, despite a `dotnet build` call in the pre task
- `post-sub.sh` launches a stale version of app on simulator, despite using `uninstall` and `install`
- Infinite loop if JSX is not well formed in input
- Not checking array lengths in `HandleServerMessage`
- `Directives` VSC task executes the post command even if the build fails

17/09/19
- Parser chokes on double semi colon `;;` (in this case, at end of a field declaration)