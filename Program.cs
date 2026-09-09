using System.Diagnostics;
using System.Text;

const long MaxFileSize = 2 * 1024 * 1024; // 2 MB per file

Console.WriteLine("CodeColletor");
Console.WriteLine("============");
Console.WriteLine();

Console.Write("Enter path to a directory or .csproj file: ");

var input = Console.ReadLine();

if (string.IsNullOrWhiteSpace(input))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("No path was provided.");
    Console.ResetColor();

    WaitAndExit();
    return;
}

// Allow paths pasted with quotes.
input = input.Trim().Trim('"');
input = Path.GetFullPath(input);

// Validate input.
if (!Directory.Exists(input) && !File.Exists(input))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("The specified path does not exist:");
    Console.WriteLine(input);
    Console.ResetColor();

    WaitAndExit();
    return;
}

// Determine the root directory.
string rootDirectory;

if (File.Exists(input))
{
    rootDirectory = Path.GetDirectoryName(input)!;
}
else
{
    rootDirectory = input;
}

Console.WriteLine();
Console.WriteLine($"Root: {rootDirectory}");
Console.WriteLine();

// Files that should be collected.
var allowedExtensions = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
{
    // C#
    ".cs",
    ".csproj",

    // Other .NET projects
    ".fs",
    ".fsproj",
    ".vb",
    ".vbproj",
    ".sln",

    // MSBuild
    ".props",
    ".targets",

    // Configuration / data
    ".json",
    ".xml",
    ".config",
    ".yaml",
    ".yml",

    // Resources
    ".resx",

    // Web
    ".razor",
    ".cshtml",
    ".html",
    ".css",
    ".scss",

    // SQL
    ".sql",

    // Documentation
    ".txt",
    ".md"
};


// Directories that should be ignored.
var excludedDirectories = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
{
    "bin",
    "obj",
    ".git",
    ".vs",
    ".idea",
    "node_modules",
    "packages",
    "TestResults",
    "Debug",
    "Release"
};

Console.WriteLine("Searching for files...");

// Find all relevant files.
var files = new List<string>();

try
{
    foreach (var file in Directory.EnumerateFiles(
                 rootDirectory,
                 "*",
                 SearchOption.AllDirectories))
    {
        var extension = Path.GetExtension(file);

        if (!allowedExtensions.Contains(extension))
            continue;

        var relativePath = Path.GetRelativePath(
            rootDirectory,
            file);

        var pathParts = relativePath.Split(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        // Ignore files inside excluded directories.
        if (pathParts
            .SkipLast(1)
            .Any(part => excludedDirectories.Contains(part)))
        {
            continue;
        }

        // Ignore files that are too large.
        try
        {
            var fileInfo = new FileInfo(file);

            if (fileInfo.Length > MaxFileSize)
            {
                Console.WriteLine(
                    $"Skipping large file: {relativePath} " +
                    $"({fileInfo.Length / 1024 / 1024} MB)");

                continue;
            }
        }
        catch
        {
            continue;
        }

        files.Add(file);
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("Error while searching for files:");
    Console.WriteLine(ex.Message);
    Console.ResetColor();

    WaitAndExit();
    return;
}

// Sort files by relative path.
files = files
    .OrderBy(file => Path.GetRelativePath(rootDirectory, file))
    .ToList();

// Determine output filename.
string outputFile;

if (File.Exists(input))
{
    var inputName = Path.GetFileNameWithoutExtension(input);

    outputFile = Path.Combine(
        rootDirectory,
        $"{inputName}-context.txt");
}
else
{
    var directoryName = new DirectoryInfo(rootDirectory).Name;

    outputFile = Path.Combine(
        rootDirectory,
        $"{directoryName}-context.txt");
}

// Make sure we don't include our own output file.
files.RemoveAll(file =>
    string.Equals(
        Path.GetFullPath(file),
        Path.GetFullPath(outputFile),
        StringComparison.OrdinalIgnoreCase));

Console.WriteLine();
Console.WriteLine($"Found {files.Count} files.");
Console.WriteLine($"Output: {outputFile}");
Console.WriteLine();

// Create output content.
var builder = new StringBuilder();

builder.AppendLine("================================================================================");
builder.AppendLine("CODE COLLETOR - PROJECT CONTEXT");
builder.AppendLine("================================================================================");
builder.AppendLine();

builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
builder.AppendLine($"Root:      {rootDirectory}");
builder.AppendLine($"Files:     {files.Count}");

builder.AppendLine();
builder.AppendLine("================================================================================");
builder.AppendLine("FILE LIST");
builder.AppendLine("================================================================================");
builder.AppendLine();

for (var i = 0; i < files.Count; i++)
{
    var relativePath = Path.GetRelativePath(
        rootDirectory,
        files[i]);

    builder.AppendLine($"{i + 1}. {relativePath}");
}

builder.AppendLine();
builder.AppendLine();

// Add each file.
foreach (var file in files)
{
    var relativePath = Path.GetRelativePath(
        rootDirectory,
        file);

    Console.WriteLine($"Reading: {relativePath}");

    builder.AppendLine("================================================================================");
    builder.AppendLine($"FILE: {relativePath}");
    builder.AppendLine("================================================================================");
    builder.AppendLine();

    try
    {
        var content = await File.ReadAllTextAsync(
            file,
            Encoding.UTF8);

        builder.AppendLine(content);

        if (!content.EndsWith(Environment.NewLine))
        {
            builder.AppendLine();
        }
    }
    catch (Exception ex)
    {
        builder.AppendLine("[ERROR READING FILE]");
        builder.AppendLine();
        builder.AppendLine(ex.Message);
        builder.AppendLine();
    }

    builder.AppendLine();
}

// Save output.
try
{
    await File.WriteAllTextAsync(
        outputFile,
        builder.ToString(),
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false));
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("Could not create output file:");
    Console.WriteLine(ex.Message);
    Console.ResetColor();

    WaitAndExit();
    return;
}

// Finished.
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Done!");
Console.ResetColor();

Console.WriteLine();
Console.WriteLine($"Created: {outputFile}");
Console.WriteLine($"Files:   {files.Count}");
Console.WriteLine();

// Open the generated file using the default application.
try
{
    Process.Start(new ProcessStartInfo
    {
        FileName = outputFile,
        UseShellExecute = true
    });

    Console.WriteLine("Opening the generated file...");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Could not open the generated file automatically.");
    Console.WriteLine(ex.Message);
    Console.ResetColor();
}

WaitAndExit();


// -----------------------------------------------------------------------------
// Helper methods
// -----------------------------------------------------------------------------

static void WaitAndExit()
{
    Console.WriteLine();
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
