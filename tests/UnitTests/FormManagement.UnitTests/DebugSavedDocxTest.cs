using System.IO;
using FluentAssertions;
using FormManagement.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace FormManagement.UnitTests;

/// <summary>One-off debug test: load saved docx file from disk → run ExtractUsedFields.</summary>
public sealed class DebugSavedDocxTest
{
    [Fact]
    public void Debug_DumpExtractUsedFieldsOnSavedDocx()
    {
        var path = @"D:\slw\git-project\Jira-Clone\.claude\worktrees\distracted-solomon-b47d38\tmp-after-save.docx";
        if (!File.Exists(path))
        {
            return; // skip if file not staged
        }
        var bytes = File.ReadAllBytes(path);
        var svc = new OpenXmlDocumentConversionService(NullLogger<OpenXmlDocumentConversionService>.Instance);
        var fields = svc.ExtractUsedFields(bytes);

        // Print so we see in test output
        System.Console.Error.WriteLine($"=== extracted {fields.Count} fields ===");
        foreach (var f in fields) System.Console.Error.WriteLine($"  - {f}");

        fields.Should().NotBeEmpty();
    }

    [Fact]
    public void Debug_2111_FindOrphanedPlainGuillemets()
    {
        var path = @"D:\slw\git-project\Jira-Clone\.claude\worktrees\distracted-solomon-b47d38\tmp-2111.docx";
        if (!File.Exists(path)) return;
        var bytes = File.ReadAllBytes(path);
        using var ms = new MemoryStream(bytes);
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        int paraIdx = 0;
        int totalOrphanedGuillemets = 0;
        int totalWrappedFields = 0;
        foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            var runs = para.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>().ToList();
            int depth = 0;
            for (int i = 0; i < runs.Count; i++)
            {
                var run = runs[i];
                var startsInField = depth > 0;
                foreach (var fc in run.Descendants<DocumentFormat.OpenXml.Wordprocessing.FieldChar>())
                {
                    var type = fc.FieldCharType?.Value;
                    if (type == DocumentFormat.OpenXml.Wordprocessing.FieldCharValues.Begin) { depth++; totalWrappedFields++; }
                    else if (type == DocumentFormat.OpenXml.Wordprocessing.FieldCharValues.End) depth = System.Math.Max(0, depth - 1);
                }
                var hasFieldStuff = run.Descendants<DocumentFormat.OpenXml.Wordprocessing.FieldChar>().Any() || run.Descendants<DocumentFormat.OpenXml.Wordprocessing.FieldCode>().Any();
                if (startsInField || hasFieldStuff) continue;
                var rText = string.Concat(run.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                var matches = System.Text.RegularExpressions.Regex.Matches(rText, @"«[A-Za-z]\w*»");
                if (matches.Count > 0)
                {
                    totalOrphanedGuillemets += matches.Count;
                    System.Console.Error.WriteLine($"[P{paraIdx} R{i}] ORPHANED plain text with «...»: '{rText}'");
                }
            }
            paraIdx++;
        }
        System.Console.Error.WriteLine($"=== Summary ===");
        System.Console.Error.WriteLine($"  Wrapped MERGEFIELDs (fldChar begin): {totalWrappedFields}");
        System.Console.Error.WriteLine($"  Orphaned plain «...» (need wrapping): {totalOrphanedGuillemets}");
    }

    [Fact]
    public void Debug_DumpAllParagraphTextLooseTexts()
    {
        var path = @"D:\slw\git-project\Jira-Clone\.claude\worktrees\distracted-solomon-b47d38\tmp-after-save.docx";
        if (!File.Exists(path)) return;
        var bytes = File.ReadAllBytes(path);
        using var ms = new MemoryStream(bytes);
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        int idx = 0;
        foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            var texts = para.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>();
            var concat = string.Concat(texts.Select(t => t.Text));
            if (concat.Contains("CEMAIL"))
            {
                System.Console.Error.WriteLine($"[P{idx}] Per-run breakdown:");
                int rIdx = 0;
                foreach (var r in para.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>())
                {
                    var rText = string.Concat(r.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                    var hasField = r.Descendants<DocumentFormat.OpenXml.Wordprocessing.FieldChar>().Any() || r.Descendants<DocumentFormat.OpenXml.Wordprocessing.FieldCode>().Any();
                    var codes = string.Join(",", rText.Select(c => ((int)c).ToString("X")));
                    System.Console.Error.WriteLine($"  R{rIdx} field={hasField} text=[{rText}] codes=[{codes}]");
                    rIdx++;
                }
            }
            idx++;
        }
    }
}
