using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using FormManagement.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace FormManagement.UnitTests;

/// <summary>
/// Test suite cho WrapGuillemetsAsMergeFields + ExtractUsedFields. Bao gồm 6 case theo checklist:
///   1. Fresh wrap: doc chỉ có plain «...» → tạo MERGEFIELD đúng số lượng + balanced fldChar.
///   2. Idempotency: doc đã wrap → wrap lại không thay đổi structure.
///   3. Mixed: doc có cả MERGEFIELD cũ + plain «...» mới → giữ cũ, không wrap mới (do guard skip-paragraph-with-fldChar; new « » trong paragraph riêng vẫn được wrap).
///   4. Invalid pattern: «Mã KH» (space) → giữ nguyên text, không wrap (regex chỉ match identifier chars).
///   5. Empty doc / no «...»: không lỗi, không đổi.
///   6. ExtractUsedFields trên doc mixed: union {MERGEFIELD codes} ∪ {valid plain «NAME»}.
/// </summary>
public sealed class WrapGuillemetsTests
{
    private static readonly OpenXmlDocumentConversionService _svc =
        new(NullLogger<OpenXmlDocumentConversionService>.Instance);

    private static byte[] BuildDocx(params string[] paragraphTexts)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            var body = new Body();
            foreach (var text in paragraphTexts)
            {
                var p = new Paragraph();
                var r = new Run();
                r.AppendChild(new Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });
                p.AppendChild(r);
                body.AppendChild(p);
            }
            main.Document = new Document(body);
            main.Document.Save();
        }
        return ms.ToArray();
    }

    /// <summary>Đếm fldChar/MERGEFIELD/«...» trong document.xml — ground truth cho assertions.</summary>
    private static (int Begin, int Sep, int End, int MergeField, int Guill) Inspect(byte[] docx)
    {
        using var ms = new MemoryStream(docx);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        int begin = body.Descendants<FieldChar>().Count(f => f.FieldCharType?.Value == FieldCharValues.Begin);
        int sep = body.Descendants<FieldChar>().Count(f => f.FieldCharType?.Value == FieldCharValues.Separate);
        int end = body.Descendants<FieldChar>().Count(f => f.FieldCharType?.Value == FieldCharValues.End);
        int mf = body.Descendants<FieldCode>().Count(c => (c.Text ?? "").Contains("MERGEFIELD"));
        int guill = body.Descendants<Text>().Sum(t => System.Text.RegularExpressions.Regex.Matches(t.Text ?? "", "«[^»]+»").Count);
        return (begin, sep, end, mf, guill);
    }

    [Fact]
    public void Case1_FreshWrap_PureGuillemets_CreatesMergeFields()
    {
        var docx = BuildDocx("Bên A: «CTEN», MST «CMST»");
        var before = Inspect(docx);
        before.Should().Be((0, 0, 0, 0, 2));

        var wrapped = _svc.WrapGuillemetsAsMergeFields(docx);
        var after = Inspect(wrapped);

        after.Begin.Should().Be(2, "2 «...» → 2 MERGEFIELD");
        after.Sep.Should().Be(2);
        after.End.Should().Be(2);
        after.MergeField.Should().Be(2);
        after.Guill.Should().Be(2, "display runs preserved");
    }

    [Fact]
    public void Case2_Idempotency_WrappedDoc_NotDoubleWrapped()
    {
        var docx = BuildDocx("Bên A: «CTEN»");
        var pass1 = _svc.WrapGuillemetsAsMergeFields(docx);
        var pass2 = _svc.WrapGuillemetsAsMergeFields(pass1);
        var pass3 = _svc.WrapGuillemetsAsMergeFields(pass2);

        var a1 = Inspect(pass1);
        var a2 = Inspect(pass2);
        var a3 = Inspect(pass3);

        a1.Should().Be(a2, "2nd pass must be no-op");
        a2.Should().Be(a3, "3rd pass must be no-op");
        a1.MergeField.Should().Be(1);
        a1.Begin.Should().Be(1);
        a1.End.Should().Be(1);
    }

    [Fact]
    public void Case3_MixedDoc_OldFieldsPreserved_NewParaWrapped()
    {
        // 2 paragraphs: one already wrapped, one fresh plain «...».
        var first = BuildDocx("Paragraph 1: «OLD»"); // will be wrapped → contains fldChar
        var wrappedFirst = _svc.WrapGuillemetsAsMergeFields(first);

        // Build a second doc with same wrapped «OLD» paragraph + a fresh plain paragraph.
        // Phải dùng expandable MemoryStream — `new MemoryStream(byte[])` non-expandable, Save() vỡ.
        using var ms = new MemoryStream();
        ms.Write(wrappedFirst, 0, wrappedFirst.Length);
        ms.Position = 0;
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            var newPara = new Paragraph(new Run(new Text("Paragraph 2: «NEW»")
            {
                Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve
            }));
            body.AppendChild(newPara);
            doc.MainDocumentPart.Document.Save();
        } // dispose flushes zip
        byte[] mixed = ms.ToArray();

        var before = Inspect(mixed);
        before.MergeField.Should().Be(1, "wrapped OLD still there");
        before.Guill.Should().Be(2, "1 display «OLD» + 1 plain «NEW»");

        var wrapped = _svc.WrapGuillemetsAsMergeFields(mixed);
        var after = Inspect(wrapped);

        after.MergeField.Should().Be(2, "OLD preserved + NEW wrapped");
        after.Begin.Should().Be(2);
        after.End.Should().Be(2);
        after.Sep.Should().Be(2);
    }

    [Fact]
    public void Case4_InvalidPattern_GuillemetsWithSpace_NotWrapped()
    {
        var docx = BuildDocx("Tên bên A: «Mã KH» quan trọng"); // space inside → invalid identifier
        var wrapped = _svc.WrapGuillemetsAsMergeFields(docx);
        var after = Inspect(wrapped);

        after.MergeField.Should().Be(0, "invalid identifier → no wrap");
        after.Begin.Should().Be(0);
        after.Guill.Should().Be(1, "«Mã KH» preserved as plain text");
    }

    [Fact]
    public void Case5_NoGuillemets_NoChange()
    {
        var docx = BuildDocx("Plain content, no fields here at all");
        var wrapped = _svc.WrapGuillemetsAsMergeFields(docx);
        var after = Inspect(wrapped);

        after.Should().Be((0, 0, 0, 0, 0));
    }

    [Fact]
    public void Case6_ExtractUsedFields_DetectsMergeFieldAndPlainGuillemets()
    {
        // Wrap 1 paragraph then append a 2nd plain paragraph (valid + invalid).
        var first = _svc.WrapGuillemetsAsMergeFields(BuildDocx("«ALPHA»"));
        using var ms = new MemoryStream();
        ms.Write(first, 0, first.Length);
        ms.Position = 0;
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            body.AppendChild(new Paragraph(new Run(new Text("«BETA» and «Bad Name»")
            { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve })));
            doc.MainDocumentPart.Document.Save();
        }
        byte[] mixed = ms.ToArray();

        var fields = _svc.ExtractUsedFields(mixed);

        fields.Should().Contain("ALPHA"); // from MERGEFIELD instrText
        fields.Should().Contain("BETA");  // from plain text «BETA»
        fields.Should().NotContain("Bad Name"); // filtered (space → invalid identifier)
        fields.Should().HaveCount(2);
    }

    [Fact]
    public void Case9_MailMerge_SplitDisplayRun_StillSubstitutesNewField()
    {
        // Cùng scenario Case8 nhưng test mail-merge: plain «CEMAIL» chèn giữa "«CCHUC_VU" và "»"
        // phải vẫn được substitute. Test trực tiếp MailMergeAsync via service.
        var docx = BuildDocx("");
        using var ms = new MemoryStream();
        ms.Write(docx, 0, docx.Length);
        ms.Position = 0;
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            body.RemoveAllChildren<Paragraph>();
            var para = new Paragraph();
            string[] runTexts = { "«CCHUC_VU»", "«CCHUC_VU", "«CEMAIL»", "»", "«BNGAY_CAP»" };
            foreach (var txt in runTexts)
                para.AppendChild(new Run(new Text(txt) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }));
            body.AppendChild(para);
            doc.MainDocumentPart.Document.Save();
        }
        var input = ms.ToArray();
        var data = new Dictionary<string, object?>
        {
            ["CCHUC_VU"] = "Giam Doc",
            ["CEMAIL"] = "test@example.com",
            ["BNGAY_CAP"] = "2026-05-14"
        };
        var result = _svc.MailMergeAsync(input, data, FormManagement.Domain.ExportFormat.Docx).GetAwaiter().GetResult();
        result.IsSuccess.Should().BeTrue();

        // Inspect output: tất cả 3 field phải được substitute, không còn «...» valid identifier nào sót.
        using var outMs = new MemoryStream(result.Data!);
        using var outDoc = WordprocessingDocument.Open(outMs, false);
        var outBody = outDoc.MainDocumentPart!.Document.Body!;
        var outConcat = string.Concat(outBody.Descendants<Text>().Select(t => t.Text));
        outConcat.Should().Contain("test@example.com", "CEMAIL phải được substitute");
        outConcat.Should().Contain("Giam Doc", "CCHUC_VU phải được substitute");
        outConcat.Should().Contain("2026-05-14", "BNGAY_CAP phải được substitute");
        System.Text.RegularExpressions.Regex.IsMatch(outConcat, @"«[A-Za-z]\w*»").Should().BeFalse("không còn valid identifier guillemet nào");
    }

    [Fact]
    public void Case8_ExtractUsedFields_SplitDisplayRun_DoesNotEatNewField()
    {
        // Reproduce real bug: OnlyOffice split display run «CCHUC_VU» thành 2 w:t ("«CCHUC_VU" và "»")
        // rồi user dùng plugin paste «CEMAIL» VÀO GIỮA → concat thành "«CCHUC_VU«CEMAIL»".
        // Regex greedy bắt cả cụm, fail filter → mất CEMAIL.
        // Strict identifier regex `«[A-Za-z]\w*»` phải bỏ qua chỗ bị split và bắt đúng «CEMAIL».
        var docx = BuildDocx(""); // 1 empty paragraph
        using var ms = new MemoryStream();
        ms.Write(docx, 0, docx.Length);
        ms.Position = 0;
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            body.RemoveAllChildren<Paragraph>();
            var para = new Paragraph();
            // Mô phỏng dump R1-R6 từ docx thật:
            //   «CCHUC_VU» | «CCHUC_VU» | «CCHUC_VU (cụt) | «CEMAIL» (insert) | » (orphan) | «BNGAY_CAP»
            string[] runTexts = { "«CCHUC_VU»", "«CCHUC_VU»", "«CCHUC_VU", "«CEMAIL»", "»", "«BNGAY_CAP»" };
            foreach (var txt in runTexts)
            {
                para.AppendChild(new Run(new Text(txt) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }));
            }
            body.AppendChild(para);
            doc.MainDocumentPart.Document.Save();
        }
        var mixed = ms.ToArray();

        var fields = _svc.ExtractUsedFields(mixed);

        fields.Should().Contain("CCHUC_VU");
        fields.Should().Contain("CEMAIL");
        fields.Should().Contain("BNGAY_CAP");
    }

    [Fact]
    public void Case10_NewPlainFieldInsertedIntoMixedParagraph_GetsWrapped()
    {
        // Reproduce user-reported bug: paragraph đã có wrapped «OLD» MERGEFIELD,
        // user dùng plugin paste «NEW» plain vào cùng paragraph. Old idempotency
        // guard skip cả paragraph → «NEW» mãi mãi không thành MERGEFIELD.
        // Per-run logic phải wrap được «NEW» dù paragraph có MERGEFIELD khác.

        // Step 1: wrap «OLD» trong 1 paragraph riêng để có MERGEFIELD đúng cấu trúc.
        var wrappedOld = _svc.WrapGuillemetsAsMergeFields(BuildDocx("Header: «OLD» content"));
        var oldInspect = Inspect(wrappedOld);
        oldInspect.MergeField.Should().Be(1);

        // Step 2: chèn 1 Run plain "«NEW»" SAU MERGEFIELD «OLD» trong cùng paragraph.
        using var ms = new MemoryStream();
        ms.Write(wrappedOld, 0, wrappedOld.Length);
        ms.Position = 0;
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            var para = body.Descendants<Paragraph>().First();
            para.AppendChild(new Run(new Text(" extra «NEW» tail")
            { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }));
            doc.MainDocumentPart.Document.Save();
        }
        var mixed = ms.ToArray();
        var before = Inspect(mixed);
        before.MergeField.Should().Be(1, "vẫn chỉ 1 MERGEFIELD «OLD» trước wrap");
        // before.Guill có thể là 1 (chỉ display của OLD) hoặc 2 (display + new plain) tuỳ runs.

        // Step 3: wrap lại — fix per-run phải pick up «NEW» mà không động chạm «OLD».
        var wrappedMixed = _svc.WrapGuillemetsAsMergeFields(mixed);
        var after = Inspect(wrappedMixed);

        after.MergeField.Should().Be(2, "OLD preserved + NEW now wrapped");
        after.Begin.Should().Be(2);
        after.Sep.Should().Be(2);
        after.End.Should().Be(2);

        // Verify content text intact
        using var outMs = new MemoryStream(wrappedMixed);
        using var outDoc = WordprocessingDocument.Open(outMs, false);
        var concatOut = string.Concat(outDoc.MainDocumentPart!.Document.Body!.Descendants<Text>().Select(t => t.Text));
        concatOut.Should().Contain("Header:");
        concatOut.Should().Contain("content");
        concatOut.Should().Contain("extra");
        concatOut.Should().Contain("tail");
    }

    [Fact]
    public void Case11_Idempotency_StillNoOp_AfterPerRunRefactor()
    {
        // Regression: per-run logic vẫn idempotent. Wrap 2 lần cùng doc → counts không đổi.
        var docx = BuildDocx("«ALPHA» và «BETA»");
        var pass1 = _svc.WrapGuillemetsAsMergeFields(docx);
        var pass2 = _svc.WrapGuillemetsAsMergeFields(pass1);
        var pass3 = _svc.WrapGuillemetsAsMergeFields(pass2);
        Inspect(pass1).Should().Be(Inspect(pass2));
        Inspect(pass2).Should().Be(Inspect(pass3));
        Inspect(pass1).MergeField.Should().Be(2);
    }

    [Fact]
    public void Case7_MultipleSameField_DedupedInExtract_AllWrappedInDoc()
    {
        var docx = BuildDocx("«CTEN» giới thiệu «CTEN» rồi «CTEN»");
        var wrapped = _svc.WrapGuillemetsAsMergeFields(docx);
        var after = Inspect(wrapped);

        after.MergeField.Should().Be(3, "3 occurrences each get own MERGEFIELD wrapper");
        after.Begin.Should().Be(3);
        after.End.Should().Be(3);

        var fields = _svc.ExtractUsedFields(wrapped);
        fields.Should().ContainSingle().Which.Should().Be("CTEN");
    }
}
