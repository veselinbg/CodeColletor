# CodeColletor

A .NET 10 console tool that collects the source files of a project into **one text file**, ready to give to an AI assistant (Claude, ChatGPT and others) as context.

## How it works

1. Run the tool and enter the path to a folder or a `.csproj` file.
2. It finds all relevant files under that folder:
   - code and projects: `.cs`, `.csproj`, `.fs`, `.vb`, `.sln`, `.props`, `.targets`
   - configuration: `.json`, `.xml`, `.config`, `.yaml`, `.yml`, `.resx`
   - web: `.razor`, `.cshtml`, `.html`, `.css`, `.scss`
   - SQL and docs: `.sql`, `.txt`, `.md`
3. It skips `bin`, `obj`, `.git`, `.vs`, `.idea`, `node_modules`, `packages`, `TestResults`, `Debug`, `Release`, and files larger than 2 MB.
4. It writes `<name>-context.txt` in the root folder: a header (date, root, file count) and every file with its relative path, sorted by path.

The tool never includes its own output file, so it can be run again safely.

## Run

Requires the .NET 10 SDK.

```
dotnet run
```

Paths can be pasted with quotes.

## Tech stack

C# · .NET 10 · console app, no external packages
