# SmartConsole Developer Guide

SmartConsole is a small framework for building self‑describing command line applications. It provides
attribute based tasks, built‑in help, and an optional interactive shell so that a console application can be
composed with minimal code.

## Features

- **Attribute driven tasks** – decorate classes derived from `ConsoleTask` with `TaskHelp` and
  arguments with `ArgumentHelp` to automatically generate help and validation.
- **Interactive shell** – start without arguments to enter a shell where tasks can be executed
  repeatedly.
- **Task packages** – group multiple tasks into a package and execute them as a single command or
  from a script file.
- **Built‑in help** – tasks and arguments automatically display usage information.

## Getting Started

Install the NuGet package or build the library from source.

```bash
# using dotnet CLI
# dotnet add package Bessett.SmartConsole
```

Create a program that forwards command line arguments to `ConsoleProgram.Start`:

```csharp
class Program
{
    static void Main(string[] args)
    {
        ConsoleProgram.Start(args);
    }
}
```

Define tasks by deriving from `ConsoleTask` and adding attributes:

```csharp
[TaskHelp("Some Help for the Task")]
public class DoSomething : ConsoleTask
{
    [ArgumentHelp(IsRequired = true, HelpText = "This will do something.", ErrorText = "Invalid value")]
    public string RequiredValue { get; set; }

    [ArgumentHelp("This is a parameter", IsRequired = false)]
    public string ParamValue { get; set; }

    [ArgumentHelp]
    public int BatchSize { get; set; }

    [ArgumentHelp]
    public string Filename { get; set; }

    [ArgumentHelp]
    public bool IsFinal { get; set; }

    public override void Start()
    {
        Console.WriteLine("Started!");
    }
}
```

Execute a task directly or launch the shell:

```bash
# run a task once
MyApp DoSomething -RequiredValue MyValue -BatchSize 20

# start the shell (help and other built-in commands are available)
MyApp
```

## Building from Source

The solution targets **.NET Standard 2.0** and **.NET Framework 4.5**. Use the .NET SDK or
Visual Studio to build:

```bash
# build all projects
# dotnet build SmartConsole.sln
```

The projects `SmartConsole.Test` and `SmartConsole.TestDNC` provide examples that exercise the library.

## Creating Task Packages

Combine tasks into reusable packages by implementing `TaskPackage` classes or by creating a command
script file. Packages can be executed with the built‑in `RunPackage` task.

## License

SmartConsole is released under the MIT License. See `License.txt` for details.
