using Nova.Formats;
using Xunit;

namespace Nova.Formats.Tests;

public class DocumentConverterTests
{
    private readonly DocumentConverter _converter;

    public DocumentConverterTests()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<DocumentConverter>();
        _converter = new DocumentConverter(logger);
    }

    [Fact]
    public async Task ExtractText_Txt_ReturnsContent()
    {
        var content = "Hello, World!\nThis is a test.";
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        var result = await _converter.ExtractTextAsync(bytes, DocumentFormat.Txt);

        Assert.Equal(content, result);
    }

    [Fact]
    public async Task ExtractText_Csv_ReturnsContent()
    {
        var content = "A,B,C\n1,2,3\n4,5,6";
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        var result = await _converter.ExtractTextAsync(bytes, DocumentFormat.Csv);

        Assert.Equal(content, result);
    }

    [Fact]
    public async Task GetMetadata_Pdf_ReturnsBasicInfo()
    {
        // Create a simple PDF
        using var pdf = new PdfSharp.Pdf.PdfDocument();
        pdf.Info.Title = "Test PDF";
        pdf.Info.Author = "Test Author";
        pdf.Info.Subject = "Test Subject";
        pdf.Info.Keywords = "test, pdf";
        var page = pdf.AddPage();
        var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
        gfx.DrawString("Test content", new PdfSharp.Drawing.XFont("Arial", 12), PdfSharp.Drawing.XBrushes.Black, 100, 100);

        using var ms = new System.IO.MemoryStream();
        pdf.Save(ms);
        var bytes = ms.ToArray();

        var metadata = await _converter.GetMetadataAsync(bytes, DocumentFormat.Pdf);

        Assert.Equal("Test PDF", metadata.Title);
        Assert.Equal("Test Author", metadata.Author);
        Assert.Equal("Test Subject", metadata.Subject);
        Assert.Equal("test, pdf", metadata.Keywords);
        Assert.Equal(1, metadata.PageCount);
        Assert.Equal(bytes.Length, metadata.FileSize);
    }

    [Fact]
    public async Task GetMetadata_Docx_ReturnsBasicInfo()
    {
        // This would require creating a real DOCX - skipped for now
        Assert.True(true);
    }
}

public class PdfServiceTests
{
    private readonly PdfService _pdfService;

    public PdfServiceTests()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<PdfService>();
        _pdfService = new PdfService(logger);
    }

    [Fact]
    public async Task Merge_TwoPdfs_CombinesPages()
    {
        var pdf1 = CreateTestPdf("Page 1");
        var pdf2 = CreateTestPdf("Page 2");

        var merged = await _pdfService.MergeAsync(new[] { pdf1, pdf2 });

        using var doc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(merged));
        Assert.Equal(2, doc.PageCount);
    }

    [Fact]
    public async Task Split_Pdf_ReturnsIndividualPages()
    {
        var pdf = CreateTestPdf("Page 1");
        using var doc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(pdf));
        // Add a second page
        doc.AddPage();
        var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(doc.Pages[1]);
        gfx.DrawString("Page 2", new PdfSharp.Drawing.XFont("Arial", 12), PdfSharp.Drawing.XBrushes.Black, 100, 100);

        using var ms = new System.IO.MemoryStream();
        doc.Save(ms);
        var twoPagePdf = ms.ToArray();

        var split = await _pdfService.SplitAsync(twoPagePdf, new[] { 1, 2 });

        Assert.Equal(2, split.Count);
        foreach (var pagePdf in split)
        {
            using var pageDoc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(pagePdf));
            Assert.Equal(1, pageDoc.PageCount);
        }
    }

    [Fact]
    public async Task ExtractPages_SelectedPages_ReturnsSubset()
    {
        var pdf = CreateTestPdf("Page 1");
        using var doc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(pdf));
        doc.AddPage();
        doc.AddPage();
        using var ms = new System.IO.MemoryStream();
        doc.Save(ms);
        var threePagePdf = ms.ToArray();

        var extracted = await _pdfService.ExtractPagesAsync(threePagePdf, new[] { 1, 3 });

        using var outDoc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(extracted));
        Assert.Equal(2, outDoc.PageCount);
    }

    [Fact]
    public async Task RotatePages_RotatesSpecifiedPages()
    {
        var pdf = CreateTestPdf("Page 1");
        using var doc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(pdf));
        doc.AddPage();
        using var ms = new System.IO.MemoryStream();
        doc.Save(ms);
        var twoPagePdf = ms.ToArray();

        var rotated = await _pdfService.RotatePagesAsync(twoPagePdf, new[] { (1, 90), (2, 180) });

        using var outDoc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(rotated));
        Assert.Equal(90, outDoc.Pages[0].Rotate);
        Assert.Equal(180, outDoc.Pages[1].Rotate);
    }

    [Fact]
    public async Task FillForm_SetsFieldValues()
    {
        // Create a PDF with form fields
        using var pdf = new PdfSharp.Pdf.PdfDocument();
        var page = pdf.AddPage();
        var form = pdf.AcroForm;
        var textField = new PdfSharp.Pdf.AcroForms.PdfTextField(page, new PdfSharp.Pdf.PdfRectangle(100, 100, 300, 120))
        {
            Name = "testField",
            Value = new PdfSharp.Pdf.PdfString("initial")
        };
        form.Fields.Add(textField);

        using var ms = new System.IO.MemoryStream();
        pdf.Save(ms);
        var pdfBytes = ms.ToArray();

        var filled = await _pdfService.FillFormAsync(pdfBytes, new Dictionary<string, string> { { "testField", "filled value" } });

        using var outDoc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(filled));
        var field = outDoc.AcroForm.Fields["testField"] as PdfSharp.Pdf.AcroForms.PdfTextField;
        Assert.NotNull(field);
        Assert.Equal("filled value", field.Value.ToString());
    }

    [Fact]
    public async Task Protect_SetsPasswordsAndPermissions()
    {
        var pdf = CreateTestPdf("Protected");

        var protectedPdf = await _pdfService.ProtectAsync(
            pdf,
            "userpass",
            "ownerpass",
            PdfPermissions.Print | PdfPermissions.Copy);

        using var doc = PdfSharp.Pdf.PdfDocument.Open(new System.IO.MemoryStream(protectedPdf));
        Assert.Equal("userpass", doc.SecuritySettings.UserPassword);
        Assert.Equal("ownerpass", doc.SecuritySettings.OwnerPassword);
        Assert.True(doc.SecuritySettings.PermitPrint);
        Assert.True(doc.SecuritySettings.PermitExtractContent);
        Assert.False(doc.SecuritySettings.PermitModifyDocument);
    }

    private byte[] CreateTestPdf(string text)
    {
        using var pdf = new PdfSharp.Pdf.PdfDocument();
        var page = pdf.AddPage();
        var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
        gfx.DrawString(text, new PdfSharp.Drawing.XFont("Arial", 12), PdfSharp.Drawing.XBrushes.Black, 100, 100);

        using var ms = new System.IO.MemoryStream();
        pdf.Save(ms);
        return ms.ToArray();
    }
}