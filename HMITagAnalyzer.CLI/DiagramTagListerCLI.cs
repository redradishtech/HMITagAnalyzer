using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace HMITagAnalyzer.CLI;

/**
 * A half-baked CLI. Needs fixing to generate JSON, and output reused tags in {Screen} -> Control format like the GUI.
 */
// ReSharper disable once InconsistentNaming
public static class DiagramTagListerCLI
{
    private static ILog _logger;

    [STAThread]
    public static void Main(string[] args)
    {
        _logger = LogManager.GetLogger(typeof(DiagramTagListerCLI));
        _logger.Info("Application started.");
        if (args.Length == 0 || args[0] == "--help")
        {
            PrintHelp();
            return;
        }

        string projectPath = args[0];
        if (!File.Exists(projectPath))
        {
            Console.Error.WriteLine($"Project file {projectPath} does not exist.");
            Environment.Exit(1);
        }

        var ext = Path.GetExtension(projectPath);
        if (ext != ".hprb")
        {
            Console.Error.WriteLine($"Project file {projectPath} does not have a hprb extension.");
            Environment.Exit(1);
        }

        var diagramLister = new HMIProjectInfo(projectPath, _logger);
        if (diagramLister.InvalidTags.Any())
        {
            Console.WriteLine($"Found {diagramLister.InvalidTags.Count()} invalid tags:");
            foreach (var invalidTag in diagramLister.InvalidTags) Console.WriteLine(invalidTag);
        }


// Only run if reused tag locations exist
        var reused = diagramLister.ReusedTagLocations();
        if (reused.Any())
        {
            Console.WriteLine($"Found {reused.Count()} reused tags:");
            Console.WriteLine(DumpObject(reused));
        }
    }

    
    public static string DumpObject(object obj, int indentLevel = 0)
    {
        string indent = new string(' ', indentLevel * 2);

        if (obj == null)
            return "null";

        if (obj is string s)
            return $"\"{s}\"";

        if (obj is IDictionary dict)
        {
            var entries = new List<string>();
            foreach (var key in dict.Keys)
            {
                string keyStr = key?.ToString() ?? "null";
                string valueStr = DumpObject(dict[key], indentLevel + 1);
                entries.Add($"{indent}  {keyStr}: {valueStr}");
            }
            return "{\n" + string.Join("\n", entries) + $"\n{indent}}}";
        }

        if (obj is IEnumerable list and not string)
        {
            var items = new List<string>();
            foreach (var item in list)
            {
                items.Add($"{indent}  - {DumpObject(item, indentLevel + 1)}");
            }
            return "[\n" + string.Join("\n", items) + $"\n{indent}]";
        }

        // Default fallback for other object types
        return obj.ToString();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("     DiagramTagListerCLI <file>");
        Console.WriteLine();
        Console.WriteLine("Description:");
        Console.WriteLine(" A tool to analyze .hprb HMI diagram file and print invalid tag references.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine(" <file>   The file to analyze.");
    }
}