# Sempiler Zeit Integration

## Sempiler

The [Sempiler project](https://sempiler.com) is a monorepo compiler that emits native x-platform code for the client, server and database artifacts that power any app idea, without developers having to learn myriad programming languages or sacrifice runtime performance.

Sempiler initially takes a single TypeScript file as input. The developer articulates the build configuration and artifacts in TypeScript directly, and instructs the compiler via `#directives`.

By default the compiler will infer where to find source files (`./src/`) for each artifact, and where to emit the results (`./out`) for each artifact.

### Input Files

The demo input file is at `./inputs/hackathon.ts`.

In it you will find `#compiler` directives for the two artifacts we are generating (one of which is targeted at Now).

If you look inside the `./inputs/src` directory for each artifact you will find *web flavored* TypeScript files that reference the native APIs for each platform (hence Sempiler does not use Virtual Machines or runtime frameworks in the output).

**NOTE** If you are viewing the code in Visual Studio Code, it automatically tries to validate TypeScript and will wrongly complain about the demo inputs! [Disable autovalidation](https://stackoverflow.com/a/42633555/).

## Prerequisites

**NOTE** The `now.json` variable `SEMPILER_PATH` must be set to point to the CLI directory of a valid Sempiler build. 

Contact Darius via [email](mailto:darius@quantumcommune.com) or [twitter](https://twitter.com/ComethTheNerd) for a build.

**ALSO** This integration has only been run on my Macbook... so who knows whether it will work on yours without some tweaks! :)

## Integration

As a result of this hackathon, Sempiler can now generate a valid serverless Zeit Now bundle, for the server part of a project (and an Android bundle for the client). 

### Compilation

This Zeit dashboard integration allows the user to input their project index path (on their local machine), and invoke the compiler process via the UI Hook.

The compilation results are then displayed on the dashboard, along with any diagnostic messages, and relevant file links.

### Deployment

Additionally, the user can click the Deploy button and have each artifact build and run on the server side (localhost).

## Demo

I have recorded a [short video](https://youtu.be/dzjQUAYNL60) demonstrating the integration at work.

## Closing Remarks

Given more time I would have liked the compiler to generate the networking communication between the Now backend and Android frontend automatically.

Overall I'm happy with the result, and wish to thank everyone at Zeit for hosting this event which proved to be the breath of fresh air I needed!

Darius