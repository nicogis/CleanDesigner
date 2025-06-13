
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CleanDesigner;
public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // Show help if no parameters are provided
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  -path <directory>      Path to the directory containing designer and custom class files");
            Console.WriteLine("  -prefix <char>         Prefix for backing fields (default: f)");
            Console.WriteLine("  -clean                 Clean designer files");
            Console.WriteLine("  -report                Report duplicates and backing fields");
            Console.WriteLine();
            Console.WriteLine("Note: -clean and -report are mutually exclusive.");
            return;
        }

        // Default values
        string? path = null;
        char prefixBackField = 'f';
        bool doClean = false;
        bool doReport = false;

        // Parse command line arguments with prefix
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-path" && i + 1 < args.Length && !args[i + 1].StartsWith('-'))
            {
                path = args[i + 1];
                i++;
            }
            else if (args[i] == "-prefix" && i + 1 < args.Length && !string.IsNullOrEmpty(args[i + 1]) && !args[i + 1].StartsWith('-'))
            {
                if (args[i + 1].Length != 1)
                {
                    Console.WriteLine("❌ The -prefix parameter must be a single character.");
                    return;
                }
                prefixBackField = args[i + 1][0];
                i++;
            }
            else if (args[i] == "-clean")
            {
                doClean = true;
            }
            else if (args[i] == "-report")
            {
                doReport = true;
            }
        }

        if (!doReport && !doClean)
        {
            Console.WriteLine("❌ Set -clean or -report");
            return;
        }

        if (doReport && doClean)
        {
            Console.WriteLine("❌ The -report and -clean parameters are mutually exclusive. Use only one.");
            return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine($"❌ Set path");
            return;
        }

        if (!Directory.Exists(path))
        {
            Console.WriteLine($"❌ Directory not found: '{path}'");
            return;
        }


        XpoDesignerCleaner.Path = path;
        XpoDesignerCleaner.PrefixBackField = prefixBackField;

        if (doReport)
        {
            XpoDesignerCleaner.ReportDuplicatesAndBackFields();
        }

        if (doClean)
        {
            XpoDesignerCleaner.CleanDesigners();
        }
    }
}






class XpoDesignerCleaner
{
    private const string designerSuffix = ".Designer.cs";
    public static string? Path { get; set; }
    public static char PrefixBackField { get; set; }

    public static void CleanDesigners()
    {
        foreach (var f in Directory.EnumerateFiles(XpoDesignerCleaner.Path!, $"*{designerSuffix}"))
        {
            CleanDesigner(System.IO.Path.GetFileName(f));
        }
    }

    public static void ReportDuplicatesAndBackFields()
    {
        foreach (var f in Directory.EnumerateFiles(XpoDesignerCleaner.Path!, $"*{designerSuffix}"))
        {
            ReportDuplicatesAndBackField(System.IO.Path.GetFileName(f));
        }
    }

    private static string StripDesignerSuffix(string fileName)
    {

        if (fileName.EndsWith(designerSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return fileName[..^designerSuffix.Length] + ".cs";
        }
        return fileName;
    }

    private static void CleanDesigner(string designerFile)
    {
        string designerPath = System.IO.Path.Combine(Path!, designerFile);
        string partialPath = System.IO.Path.Combine(Path!, StripDesignerSuffix(designerFile));

        if (!File.Exists(designerPath))
        {
            Console.WriteLine($"❌ File not found: {designerPath}");
            return;
        }

        if (!File.Exists(partialPath))
        {
            Console.WriteLine($"❌ File not found: {partialPath}");
            return;
        }

        string designerCode, partialCode;
        try
        {
            designerCode = File.ReadAllText(designerPath);
            partialCode = File.ReadAllText(partialPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error reading files: {ex.Message}");
            return;
        }

        var designerTree = CSharpSyntaxTree.ParseText(designerCode);
        var partialTree = CSharpSyntaxTree.ParseText(partialCode);

        var designerRoot = designerTree.GetRoot();
        var partialRoot = partialTree.GetRoot();

        // Collect properties in the partial
        var partialProperties = partialRoot
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.Text)
            .ToHashSet();

        // Find the class in the designer file
        var classNode = designerRoot
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classNode == null)
        {
            Console.WriteLine($"❌ No class found in designer file: {designerFile}");
            return;
        }

        var newMembers = new List<MemberDeclarationSyntax>();

        foreach (var member in classNode.Members)
        {
            bool keep = true;

            if (member is PropertyDeclarationSyntax prop)
            {
                if (partialProperties.Contains(prop.Identifier.Text))
                {
                    Console.WriteLine($"⚠️ Removing duplicate property: {prop.Identifier.Text} - class: {designerFile}");
                    keep = false;
                }
            }

            if (member is FieldDeclarationSyntax field)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    string fieldName = variable.Identifier.Text;
                    
                    // Skip fields that do not start with the specified prefix
                    if (!fieldName.StartsWith(PrefixBackField))
                        continue;

                    var candidate = fieldName.StartsWith(PrefixBackField) ? fieldName[1..] : null;
                    if (!string.IsNullOrEmpty(candidate) &&
                        partialProperties.Contains(candidate) ||
                        partialProperties.Contains(char.ToUpperInvariant(candidate![0]) + candidate[1..]))
                    {
                        Console.WriteLine($"    ↳ Removing associated field: {fieldName} - class: {designerFile}");
                        keep = false;
                    }
                }
            }

            if (keep)
            {
                newMembers.Add(member);
            }
        }


        var newClass = classNode.WithMembers([.. newMembers]);
        var newRoot = designerRoot.ReplaceNode(classNode, newClass);

        try
        {
            File.WriteAllText(designerPath, newRoot.NormalizeWhitespace().ToFullString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error writing file: {designerPath} - {ex.Message}");
            return;
        }

        Console.WriteLine($"✅ Designer file updated: - class: {designerFile}");
    }

    private static void ReportDuplicatesAndBackField(string designerFile)
    {
        string designerPath = System.IO.Path.Combine(Path!, designerFile);
        string partialPath = System.IO.Path.Combine(Path!, StripDesignerSuffix(designerFile));

        if (!File.Exists(designerPath))
        {
            Console.WriteLine($"❌ File not found: {designerPath}");
            return;
        }

        if (!File.Exists(partialPath))
        {
            Console.WriteLine($"❌ File not found: {partialPath}");
            return;
        }

        string designerCode, partialCode;
        try
        {
            designerCode = File.ReadAllText(designerPath);
            partialCode = File.ReadAllText(partialPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error reading files: {ex.Message}");
            return;
        }

        var designerTree = CSharpSyntaxTree.ParseText(designerCode);
        var partialTree = CSharpSyntaxTree.ParseText(partialCode);

        var designerRoot = designerTree.GetRoot();
        var partialRoot = partialTree.GetRoot();

        var designerProperties = designerRoot
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .ToDictionary(p => p.Identifier.Text, p => p);

        var designerFields = designerRoot
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(f => f.Declaration.Variables)
            .ToDictionary(v => v.Identifier.Text, v => v);

        var partialProperties = partialRoot
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.Text)
            .ToHashSet();

        Console.WriteLine("🔍 Analyzing duplicates...");

        foreach (var propName in partialProperties)
        {
            if (designerProperties.ContainsKey(propName))
            {
                Console.WriteLine($"⚠️ Duplicate property found: {propName} - class: {designerFile}");

                // Find possible backing field (fPropName)
                string[] possibleFieldNames = [
                    PrefixBackField + propName
                ];

                foreach (var fname in possibleFieldNames)
                {
                    // Skip fields that do not start with the specified prefix
                    if (!fname.StartsWith(PrefixBackField))
                        continue;

                    if (designerFields.ContainsKey(fname))
                    {
                        Console.WriteLine($"    ↳ Associated private field: {fname} - class: {designerFile}");
                        break;
                    }
                }
            }
        }

        Console.WriteLine($"✅ Analysis completed: - class: {designerFile}");
    }
}
