# Sempiler 💎

🚀 **Latest Patch Notes** 
can be found [here](https://github.com/QuantumCommune/sempiler/blob/master/patch-notes.md)

🐛 **Q&D Issue Tracker** can be found [here](https://github.com/QuantumCommune/sempiler/blob/master/compiler-issues.md)

🔜 **Documentation** is a high priority... waiting for an incoming API _rejig_ to land first 🙈

## Overview

Popular solutions for x-platform programming offer convenience to the developer at the expense of the user experience. 

These solutions abstract away the semantics of the target environment (APIs, memory model, rules etc), giving the impression that these platforms can be considered equivalent. 

Not only does this require layers of complex, hard to debug framework glue, it also prevents developers from being able to truly write native, semantic, idiomatic or optimal code.

However, Sempiler puts semantics at the forefront of development, allowing you to use your favourite programming language to produce code that is all of those things!

### Transpile Intent 🧞‍
Sempiler empowers teams to emit native code for any platform, without switching language.

- Decouple syntax from semantics
- No more developer time wasted learning new syntax
- No more frustrating typos caused by switching between similar languages
- High level primitives for common concepts like types, networking, and concurrency
- Native emissions to any language, without virtual machines or frameworks

### Democratise Compilation 🗽
Sempiler empowers teams to introspect, instrument, and optimise their code, from their code.

- Write build logic in the same language as your business logic
- Dictate how the compiler transforms your code, to match your use case
- No need to configure a flaky and fragmented toolchain
- Mix and match plugins across a fully programmable pipeline

### High Level Features ⭐
- Compile time arbitrary code execution
- X-platform zero overhead native code emission (native emissions, no frameworks or VMs)
- Multiple artifact generation from single codebase

## Usage 🚀
The compiler can be leveraged in several ways.

### Proto
The `Proto` project can be built and then passed a single input file path.

You can see an example of this in the `.vscode/launch.json` for the `Compile` config. It compiles the `Ìnputs/src/app.ts` file.

### Visual Studio Code Plugin
There is a basic VSC plugin implementation in `Distribution/VisualStudioCode` that can be used for a more interactive development experience, with diagnostics underlined in editor.

### CLI
Under the hood the VSC plugin uses the CLI interface, which really just creates a duplex socket server over which it accepts and interprets commands. 

This is similar to how the compiler performs compile time code execution.

## Languages 🗣
The compiler can parse and emit native code in a variety of programming languages.

### Sources 💡
There are no hard constraints on suitable source languages, though typed source languages are preferable when emitting to typed target languages (for correctness and/or more accurate declarations).

Hopefully the supported source languages list will grow over time, making it trivial to link source files/libraries expressed in different languages.

Currently supported source languages:
- TypeScript (.ts files)
- TSX ie. TypeScript + JSX Tags (.tsx files)

**NOTE** There is no need to use TypeScript declaration files (`.d.ts`) because the code you write is transpiled to the target context where it is actually type checked/executed. Visual Studio Code users should [disable validation](https://stackoverflow.com/a/42633555/) to prevent it complaining about missing symbol definitions.


### Targets 🎯
There are no hard constraints on target languages, and hopefully the supported target languages list will grow over time.

Currently supported target languages:
- Swift 5 🍏
- Java 9 ☕
- TypeScript 3
- JavaScript

#### vFuture 🔮
- WASM


## Platforms 💻
The compiler can produce artifacts for different environments. 

Emitted artifacts contain the transpiled/transformed source files, as well as any autogenerated shard files required by the platform (eg. plists, podfile, manifests etc.).

### Targets 🎯
Currently supported target platforms:
- iOS 13 🍏
- Android 🤖
- [Zeit Now](https://www.youtube.com/watch?v=dzjQUAYNL60&feature=youtu.be) ◼️
- Firebase Functions 🔥
- Express

#### Firebase Functions

Look at the sample in `Inputs/firebase-functions`. 

If you open this repository in Visual Studio Code there should be a launch configuration called `FirebaseFunctions`.

Running that should create and populate the `Inputs/firebase-functions/out` directory with a bundle set up to be deployed via the Firebase CLI.

Assuming you have the [Firebase CLI](https://firebase.google.com/docs/cli) installed and configured (eg. you are logged in to Firebase), you should be able to run the following script:

```bash
cd Inputs/firebase-functions/out/server/functions && \
    npm i && \
    npm run build && \
    cd ../ && \
    firebase emulators:start --only functions,hosting --project <your-firebase-project-id>

# Use the local server address, and append the name of one of the functions. eg. http://localhost:5000/hello
# (TIP : make sure you are using the correct HTTP verb!)"
```

#### vFuture 🔮
- Web

## Compile Time Code Execution 🛠
Control over your code does not stop once you hand it to the compiler. You can control the compilation process **from** your source code.

This is useful for:
- Optional code inclusion (dependent on variables like the build config, or target language/platform)
- Code generation (generating repetitive or boilerplate code, rather than having to write and maintain it)
- AST mutations (using APIs to perform bespoke transformations during the build, like how Sempiler transforms view declarations)
- Business logic assertions (check a switch is exhaustive, or that business logic is upheld, rather than use unit tests)
- Syntactic polyfills (like how Sempiler transforms the following TypeScript snippet `foo(...{ x : "hello", y : "world"})`, into named arguments in Swift `foo(x:"hello", y:"world")`)
- Target platform meta (conditionally adding dependencies, orientation, capabilities, entitlements and permissions for the artifact being generated)

And because compile time logic is expressed directly in source files, it can be shared between projects just like a regular runtime dependency.

### Compiler Directive 📣
Use the `#compiler` directive to write code that will be executed at **compilation time**. 

Note, all compile time code execution is performed in JavaScript with Node, regardless of the target language/platform being generated.

The syntax is:

`#compiler <expression>`

Compiler directives are evaluated in the **depth first traversal order**.

Whilst the operand can be of any form, the result of evaluating the actual directive is **void**. This means that `#compiler` directives are removed entirely from the program AST after compilation time, and are not present in the final emitted artifact.

In the following case the operands are *expressions*, and executed in the first, second and third order as per their names:

```typescript
#compiler first();

function foo()
{
    #compiler second();
}

#compiler third();
```

Compile time execution can interact and mutate the entire program AST, but can also reference static or constant symbols - allowing some code in your program to be available at compile time **and** run time.

```typescript
#compiler const myValue = square(3)

function square(n : number)
{
    return n * n;
}
```

The `square` function has no dependency on run time symbols and so is able to be invoked at compile time - but will also be present in the emitted artifact for invocation at run time.

### API 🎛
The compiler API is barebones at present, but allows for the following:
- `addArtifact(name : string, targetLanguage : string, targetPlatform : string, entrypointSourcePath : string)` declares an artifact
- `addCapability(name : string, value : string | string[])` declares a capability that the artifact provides (eg. `addCapability("UIBackgroundModes", ["audio"])`)
- `addDependency(name : string, version? : string)` declares a dependency that the artifact requires (eg. Stripe)
- `addEntitlement(name : string, value : string | string[])` declares a target platform entitlement that the artifact requires (eg. Apple Pay) 
- `addManifestEntry(path : string, value : string | string[])` declares a target platform manifest entry (eg. an entry in the iOS .plist file) (hint : use '/' separator to qualify the path)
- `addPermission(name : string, description : string)` declares a target platform permission that the artifact requires (eg. camera access)
- `addRawSources(...relativePaths : string[])` adds verbatim files to the artifact (ie. they will *not* be parsed/transformed)
- `addSources(...relativePaths : string[])` add files that will be parsed/transformed and added to the artifact
- `addRes(path : string, targetFileName? : string)` adds verbatim resource files to the artifact (ie. they will *not* be parsed/transformed). Optionally set the target file name when the `path` resolves to a single file
- `addShard(role : ShardRole, entrypointSourcePath : string)` add shard (eg share extension) to the Artifact
- `addAsset(role : AssetRole, sourcePath : string)` add asset (eg image) to the Shard
- `setDisplayName(name : string) : void` to set the display name of the generated artifact
- `setTeamName(name : string) : void` to set the team name of the generated artifact
- `setVersion(version : string) : void` to set the version of the generated artifact
- `isTargetPlatform(name : string) : bool` to check whether the artifact being generated is for the given target platform
- `isTargetLanguage(name : string) : bool` to check whether the artifact being generated is for the given target language

#### vFuture 🔮
- AST operations (currently only available in the C# engine, not the compile time execution context)
- Post compile time code execution (for running post build tasks, like invoking the target language compiler - with diagnostics mapped back to source code)
- `#compiler <declaration>` allow for symbols to be defined for compile time and referenced in the same scope

### Codegen Directive 📣
Use the `#emit` directive to write code that will be executed at *compilation time* to generate code that will be incuded in the emitted artifact

The syntax is:

`#emit <interpolated string>`

The emit feature can be useful for including some platform specific code verbatim, especially if it contains syntactic constructs not available in your source language (like `#available(iOS 11.0, *)` below):

```typescript
#emit`if #available(iOS 11.0, *)
{
    let window = UIApplication.shared.windows[0]
    let safeFrame = window.safeAreaLayoutGuide.layoutFrame
    topSafeAreaHeight = safeFrame.minY
    bottomSafeAreaHeight = window.frame.maxY - safeFrame.maxY
}`
```

The interpolated string can contain symbols, as long as they can be resolved during compile time.


## Views 🎨
A typical need for x-platform native emissions involves UI driven experiences, like mobile apps.

To facilitate reusable views x-platform Sempiler riffs heavily off the React-inspired functional components. 

You can define a view as a function, and a compile time translation will transform the declaration to it's equivalent in the context of the target platform/language.

```jsx
function MyView(
    name : string,
    @ObservedObject foo : Foo
) : View 
{
    return (
        <Column>
            <Row>
                <Text text={`Hello ${name}!`} />
            </Row>
        </Column>
    )
}
```

- Function declaration with a `View` return type annotation
- Uses compiler primitives like `Column`/`Row` (transformed to `VStack`/`HStack` in SwiftUI)
- Inspired by SwiftUI's reactive binding annotations (`@ObservedObject`, `@EnvironmentObject`, `@State`, `@Binding`)

#### vFuture 🔮
- Parity for reactive binding support in Litho

### Targets 🎯
Currently supported target view frameworks:
- Litho (Android) 🤖
- SwiftUI (iOS) 🍏


## Package Managers 📦
Project dependencies are often listed in shard files, such as manifests. 

A goal of Sempiler is to allow the developer to dictate all aspects of a project directly from source code:

```jsx
#compiler addDependency(name : string, version? : string)`
```

- The directive to tell the compiler to execute the operand at compile time
- `addDependency(...)` will add the dependency to the relevant shard file for the target platform (eg. podfile)

### Targets 🎯
Currently supported target package managers:
- CocoaPods
- NPM


## Debugging 🐛
Sempiler automatically parses the location (ie. file, line, column) of diagnostic messages (infos, warnings, errors) from the transformed target code back to the problematic line in your source code.

### Supported Formats 📄
- GCC family
- JavaC ☕

#### vFuture 🔮
- Step through debugging in process


## Dependencies 🔗
Dependencies fall into two categories, whether they are dependencies for an emitted artifact, or whether they are dependencies to build the code in this repository.

### Artifact Dependencies
- Node (for compile time code execution by JavaScript)
- **iOS only** Ruby (iOS only - Sempiler creates an `init.rb` that uses [xcodeproj](https://github.com/CocoaPods/Xcodeproj) to generate an `.xcworkspace` file)
- **iOS only** macOS Catalina (for SwiftUI, iOS 13.0, SwiftC, xcrun, xcbuild, simctl)
- **iOS only** CocoaPods (for dependency management)

### Project Dependencies
The Sempiler source code requires the following dependencies to build the actual compiler:
- .NET Core 3.0


## Links 📍
Here are various related links for the project.

### Project
- [Blog](https://medium.com/sempiler)
- [Website](https://sempiler.com)
- [Zeit Hackathon](https://twitter.com/ComethTheNerd/status/1134897104030240768)

### Personal 🕺🏻
- [Twitter](https://twitter.com/ComethTheNerd)
- [LinkedIn](https://www.linkedin.com/in/darius-hodaei-5866733a/)