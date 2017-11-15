# Sempiler TypeScript To C++ Hello World

## Prerequisites

- Visual Studio Code
- Sempiler Visual Studio Code Extension
- C++ Compiler (for running the code)

## Setup

1. Open the directory in Visual Studio Code
2. Open the `semconfig.json` and add a `postCmd` that calls your C++ compiler, eg. `"postCmd" : "clang++ -std=c++14 ${sempiler.outputPaths} -o ./helloworld`
3. Run Sempiler (either through the `compileOnSave` option or the `Sempiler : Run` Visual Studio task)
4. Execute the emitted binary from the Visual Studio Code terminal, eg. `./helloworld`
5. Edit the files in `src` directory
6. Run Sempiler (either through the `compileOnSave` option or the `Sempiler : Run` Visual Studio task)
7. Execute the emitted binary to see your changes

## License

The source code pertaining exclusively to this sample project can be freely copied, modified and distributed in commercial and personal projects.

For general Sempiler license information please refer to the `LICENSE.txt` in the repository root. 

## More

See [http://sempiler.com](http://sempiler.com) for more information!


Copyright (c) 2017 Quantum Commune