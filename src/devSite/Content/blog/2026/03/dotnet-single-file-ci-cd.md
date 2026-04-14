---
title: Single File-based app CI/CD in .NET
description: "With .NET 10, perhaps a single file app build.cs is all you need."
date: 2026-03-21
tags: ["c#", "dotnet", "ci-cd", "build-scripts"]
is_draft: true
---

Now that we've had .NET 10 for a decent amount of time, I thought I'd share the success I've had in using a single file-based app as my build system instead of the traditional project-based alternatives like [NUKE](https://nuke.build/) and [Cake](https://cakebuild.net/). Both of these are great tools, but I found the requirement of learning a DSL on top of C# as well as bringing along all of the tooling a bit unnecessary for smaller projects

The C# team has made steady progress since the initial release of .NET Core (arguably before this even) in removing a lot of the ceremony from getting code up and running in dotnet.

## Brief history

### CSX Scripting

VS 2015 came with a new executable called csi.exe, this launched a REPL that could run and execute real C# code. Along with this, C# Scripting was introduced, allowing devs to write C# in `.csx` files with syntax that was clearly a precursor to what we have today with file-based apps. Assemblies could be referenced with `#r path/to/reference.dll` and other `.csx` scripts with `#load "CsScriptName.csx"` at the top of the file. Great stuff, but one of the biggest drawbacks was that referencing NuGet packages was not supported, whereas GAC references were. It isn't clear to me how much traction these features got, but considering I don't know many devs who have actually used these features I'd argue it's clear not much. On the other hand, this was around the time PowerShell was picking up a lot of traction in IT circles leading to the development of [psake](https://github.com/psake/psake), [Invoke-Build](https://github.com/nightroman/Invoke-Build) and [pester](https://github.com/pester/Pester), whilst I would argue most developers continued using custom Targets and Tasks in MSBuild.

### dotnet-script

When .NET Core 1.0 arrived, a new open source cross-platform attempt was made at scripting C# with [dotnet-script](https://github.com/dotnet-script/dotnet-script) This essentially used the same format/syntax of the previous `.csx` files, but added several new features:

- NuGet packages could be installed by defining a `project.json` alongside the C# script files (this was an early precursor to the modern SDK style `.csproj` format)
- script files could be debugged with the new VS Code C# Extension by invoking the `dotnet-script.dll` directly.
- with the Dotnet.Script.Extras package other scripts could also be referenced over HTTP.

As of dotnet-script 2.0.0 the `project.json` is no longer needed, a lot of the featureset is now built directly into dotnet-script, with further configuration provided using the `omnisharp.json` file used by VS Code. Additionally NuGet packages can now be referenced directly in the `.csx` file using similar syntax as the traditional `.csx` references: `#r "nuget: AutoMapper, 6.1.0"`.

### .NET 10.0 and file-based apps

With .NET 10 we can now move away from big external tools and `.csx` files. The SDK now allows us to call `dotnet run app.cs`, where `app.cs` is a standard C# file that takes advantage of several newer features to dotnet and C#:

- [top-level statements](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/top-level-statements) (introduced in .NET 5).
- [implicit usings](https://devblogs.microsoft.com/dotnet/welcome-to-csharp-10/#implicit-usings) (introduced in .NET 6).
- compiled using [NativeAOT](https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/#native-aot) (introduced in .NET 7).

> When `dotnet run` is called for the first time, there is some delay as the script is compiled, but subsequent runs will take advantage of the native binaries and be near instant.

The syntax for referencing nuget package is similar to `.csx` and dotnet-scripts

```cs
//Top line shebang allowing script file to be invoked by the shell on unix systems
#!/usr/bin/dotnet run
// nuget package imports
#:package McMaster.Extensions.CommandLineUtils@4.1.1
#:package Bullseye@6.0.0
// wildcards are supported
#:package SimpleExec@12.*.0
```

I won't go into more detail in how single file apps work, there are many other great blog posts [detailing](https://www.ottorinobruni.com/csharp-file-based-apps-dotnet-10-run-build-apps-from-single-cs-file/) [this](https://andrewlock.net/exploring-dotnet-10-preview-features-2-behind-the-scenes-of-dotnet-run-app.cs/).

## build.cs

Utilising file based apps and a couple of helper libraries, 80% of the value from Nuke/Cake can be achieved in a single file:

- A cli arg parser for manipulating cli arguments into sensible options, C# has many mature options as of 2025 (Spectre.Console.Cli, McMaster.Extensions.CommandLineUtils, ConsoleAppFramework, CliFx)
- [Bullseye](https://github.com/adamralph/bullseye) for building build target dependency graphs
- [SimpleExec](https://github.com/adamralph/simple-exec) or [CliWrap](https://github.com/Tyrrrz/CliWrap) for cleanly calling external commands
[Here](https://raw.githubusercontent.com/jamesshenry/henryjs.Net.Templates/refs/heads/main/templates/content/csharp/CAFSln/.build/targets.cs) is a template example of a full build.cs or targets.cs used in building, testing and releasing projects. The first ~50 lines or so mostly deal with parsing the cli args, meaning the remaining build system is defined in less than 150 lines of code!

> Importing a cli library to parse args is not strictly necessary, Bullseye effectively provides an arg parser out of the box, so even this example could be slimmed down somewhat.

### Executing CI

A common requirement is running tests when a PR is opened. By calling `dotnet run targets.cs test`, the dotnet SDK will AOT compile and execute `targets.cs`, Bullseye will build the dependency graph based on the targets defined.

The `test` target depends on `build`:

```cs
    Target(
        "test",
        ["build"],
        async () =>
        {
            var testResultFolder = "TestResults";
            var coverageFileName = "coverage.xml";
            var testResultPath = Directory.CreateDirectory(Path.Combine(root, testResultFolder));
            await RunAsync(
                "dotnet",
                $"test --solution {solution} --configuration {configuration} --coverage --coverage-output {Path.Combine(testResultPath.FullName, coverageFileName)} --coverage-output-format xml --ignore-exit-code 8"
            );
        }
    );

```

The `build` target depends on `restore`:

```cs
    Target(
        "build",
        ["restore"],
        async () => await RunAsync("dotnet", $"build {solution} --configuration {configuration} --no-restore")
    );
```

And the `restore` target has no dependencies:

```cs
    Target(
        "restore",
        async () =>
        {
            var rid = ridOption.Value();
            var runtimeArg = !string.IsNullOrEmpty(rid) ? $"--runtime {rid}" : string.Empty;
            await RunAsync("dotnet", $"restore {solution} {runtimeArg}");
        }
    );
```

This naturally leads to actions workflow files that are fairly lean:

```yaml
# .github/workflows/ci.yml
name: Continuous Integration

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    name: Test on ${{ matrix.os }}
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest]

    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        global-json-file: global.json

    - name: Restore .NET tools
      run: dotnet tool restore

    - name: Run Tests
      run: dotnet run .build/targets.cs test

    - name: Upload test results
      if: always() 
      uses: actions/upload-artifact@v4
      with:
        name: test-results-${{ matrix.os }}
        path: TestResults/
```

These example are simple but can easily be extended, it's just normal C#.

> If NativeAOT incompatible features are required such as assembly scanning with reflection, this can be disabled with a property alongside the package references at the top of the file:
>
> ```cs
> #:property PublishAot=false
> ```
