** IN PROGRESS : Rough notes to form user guide / cook book / API reference **


# Input Files

## Session Entrypoint
The single input file given to the compiler will be considered the **session entrypoint**. 

The first line of this file must contain a `#compiler build(...)` directive.

A single session can result in multiple artifacts, depending on the `#compiler build(...)` directives found during compilation.

## Conventional Inclusion
TODO

## Artifact Entrypoint
TODO

## Top Level Expressions
TODO

# Imports

Consider the following lines of input code:

```typescript
#compiler addDependency("my-package", "^1.0.0" /* optional version specifier */) // 1.

import * as MyPackage from "my-package"; // 2.
```

1. When your package needs to be loaded by the target's package manager you should use the compiler API function `addDependency` to ensure the dependency is listed in the generated manifest

2. To reference symbols in the package at **run time**, use a standard import statement


# Compile Time Execution

## Directives
TODO 

## Environment
TODO

## API 
TODO