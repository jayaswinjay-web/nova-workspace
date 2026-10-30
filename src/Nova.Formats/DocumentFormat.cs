using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Presentation;
using PDFsharp;
using PDFsharp.Drawing;
using PDFsharp.Pdf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Nova.Formats;

/// <summary>
/// Supported document formats.
/// </summary>
public enum DocumentFormat
{
    Docx,
    Xlsx,
    Pptx,
    Pdf,
    Csv,
    Txt,
    Rtf
}

/// <summary>
/// Document converter for import/export between formats.
/// </summary>
public interface IDocumentConverter
{
    /// <summary>
    /// Convert a document to another format.
    /// </summary>
    Task<byte[]> ConvertAsync(byte[] source, DocumentFormat sourceFormat, DocumentFormat targetFormat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text from a document.
    /// </summary>
    Task<string> ExtractTextAsync(byte[] source, DocumentFormat format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get document metadata.
    /// </summary>
    Task<DocumentMetadata> GetMetadataAsync(byte[] source, DocumentFormat format, CancellationToken cancellationToken = default);
}

/// <summary>
/// Document metadata.
/// </summary>
public sealed class DocumentMetadata
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public string? Keywords { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
    public int PageCount { get; set; }
    public long FileSize { get; set; }
    public string? Application { get; set; }
    public Dictionary<string, string> CustomProperties { get; } = new();
}

/// <summary>
/// Implementation of document converter.
/// </summary>
public sealed class DocumentConverter : IDocumentConverter
{
    private readonly ILogger<DocumentConverter> _logger;

    public DocumentConverter(ILogger<DocumentConverter> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ConvertAsync(byte[] source, DocumentFormat sourceFormat, DocumentFormat targetFormat, CancellationToken cancellationToken = default)
    {
        // For now, implement key conversions
        if (sourceFormat == DocumentFormat.Docx && targetFormat == DocumentFormat.Pdf)
        {
            return await ConvertDocxToPdfAsync(source, cancellationToken);
        }
        if (sourceFormat == DocumentFormat.Xlsx && targetFormat == DocumentFormat.Pdf)
        {
            return await ConvertXlsxToPdfAsync(source, cancellationToken);
        }
        if (sourceFormat == DocumentFormat.Pptx && targetFormat == DocumentFormat.Pdf)
        {
            return await ConvertPptxToPdfAsync(source, cancellationToken);
        }
        if (sourceFormat == DocumentFormat.Pptx && targetFormat == DocumentFormat.Png)
        {
            return await ConvertPptxToPngAsync(source, cancellationToken);
        }

        throw new NotSupportedException($"Conversion from {sourceFormat} to {targetFormat} not implemented");
    }

    public async Task<string> ExtractTextAsync(byte[] source, DocumentFormat format, CancellationToken cancellationToken = default)
    {
        return format switch
        {
            DocumentFormat.Docx => await ExtractDocxTextAsync(source, cancellationToken),
            DocumentFormat.Xlsx => await ExtractXlsxTextAsync(source, cancellationToken),
            DocumentFormat.Pptx => await ExtractPptxTextAsync(source, cancellationToken),
            DocumentFormat.Pdf => await ExtractPdfTextAsync(source, cancellationToken),
            DocumentFormat.Txt => Encoding.UTF8.GetString(source),
            DocumentFormat.Csv => Encoding.UTF8.GetString(source),
            DocumentFormat.Rtf => await ExtractRtfTextAsync(source, cancellationToken),
            _ => throw new NotSupportedException($"Text extraction from {format} not supported")
        };
    }

    public async Task<DocumentMetadata> GetMetadataAsync(byte[] source, DocumentFormat format, CancellationToken cancellationToken = default)
    {
        var metadata = new DocumentMetadata { FileSize = source.Length };

        try
        {
            using var stream = new MemoryStream(source);
            switch (format)
            {
                case DocumentFormat.Docx:
                    using (var doc = WordprocessingDocument.Open(stream, false))
                    {
                        var coreProps = doc.PackageProperties;
                        metadata.Title = coreProps.Title;
                        metadata.Author = coreProps.Creator;
                        metadata.Subject = coreProps.Subject;
                        metadata.Keywords = coreProps.Keywords;
                        metadata.Created = coreProps.Created;
                        metadata.Modified = coreProps.Modified;
                        metadata.Application = "Microsoft Word";
                    }
                    break;

                case DocumentFormat.Xlsx:
                    using (var doc = SpreadsheetDocument.Open(stream, false))
                    {
                        var coreProps = doc.PackageProperties;
                        metadata.Title = coreProps.Title;
                        metadata.Author = coreProps.Creator;
                        metadata.Subject = coreProps.Subject;
                        metadata.Keywords = coreProps.Keywords;
                        metadata.Created = coreProps.Created;
                        metadata.Modified = coreProps.Modified;
                        metadata.Application = "Microsoft Excel";
                        // Count sheets
                        metadata.PageCount = doc.WorkbookPart?.Workbook.Sheets?.Count() ?? 0;
                    }
                    break;

                case DocumentFormat.Pptx:
                    using (var doc = PresentationDocument.Open(stream, false))
                    {
                        var coreProps = doc.PackageProperties;
                        metadata.Title = coreProps.Title;
                        metadata.Author = coreProps.Creator;
                        metadata.Subject = coreProps.Subject;
                        metadata.Keywords = coreProps.Keywords;
                        metadata.Created = coreProps.Created;
                        metadata.Modified = coreProps.Modified;
                        metadata.Application = "Microsoft PowerPoint";
                        metadata.PageCount = doc.PresentationPart?.Presentation.SlideIdList?.Count() ?? 0;
                    }
                    break;

                case DocumentFormat.Pdf:
                    using (var pdf = PdfDocument.Open(stream))
                    {
                        var info = pdf.Info;
                        metadata.Title = info.Title;
                        metadata.Author = info.Author;
                        metadata.Subject = info.Subject;
                        metadata.Keywords = info.Keywords;
                        metadata.Created = info.CreationDate;
                        metadata.Modified = info.ModificationDate;
                        metadata.Application = info.Creator;
                        metadata.PageCount = pdf.PageCount;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract metadata from {Format}", format);
        }

        return await Task.FromResult(metadata);
    }

    private async Task<byte[]> ConvertDocxToPdfAsync(byte[] source, CancellationToken cancellationToken)
    {
        // Use PDFsharp to create PDF from DOCX content
        // This is a simplified implementation - real implementation would need proper layout
        var text = await ExtractDocxTextAsync(source, cancellationToken);

        using var pdf = new PdfDocument();
        var page = pdf.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Segoe UI", 11);

        var lines = text.Split('\n');
        double y = 40;
        foreach (var line in lines)
        {
            if (y > page.Height - 40)
            {
                page = pdf.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = 40;
            }
            gfx.DrawString(line, font, XBrushes.Black, 40, y);
            y += 16;
        }

        using var ms = new MemoryStream();
        pdf.Save(ms);
        return ms.ToArray();
    }

    private async Task<byte[]> ConvertXlsxToPdfAsync(byte[] source, CancellationToken cancellationToken)
    {
        // Simplified - extract text and create PDF
        var text = await ExtractXlsxTextAsync(source, cancellationToken);

        using var pdf = new PdfDocument();
        var page = pdf.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Consolas", 10);

        var lines = text.Split('\n');
        double y = 40;
        foreach (var line in lines)
        {
            if (y > page.Height - 40)
            {
                page = pdf.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = 40;
            }
            gfx.DrawString(line, font, XBrushes.Black, 40, y);
            y += 14;
        }

        using var ms = new MemoryStream();
        pdf.Save(ms);
        return ms.ToArray();
    }

    private async Task<byte[]> ConvertPptxToPdfAsync(byte[] source, CancellationToken cancellationToken)
    {
        // Simplified - extract text and create PDF with one page per slide
        using var stream = new MemoryStream(source);
        using var doc = PresentationDocument.Open(stream, false);
        var presentation = doc.PresentationPart?.Presentation;
        var slideIds = presentation?.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        using var pdf = new PdfDocument();
        var font = new XFont("Segoe UI", 11);
        var titleFont = new XFont("Segoe UI", 20, XFontStyleEx.Bold);

        foreach (var slideId in slideIds)
        {
            var slidePart = (SlidePart)doc.PresentationPart!.GetPartById(slideId.RelationshipId!);
            var slide = slidePart.Slide;

            var page = pdf.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            double y = 60;

            // Extract text from slide
            var texts = slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text).ToList();
            if (texts.Count > 0)
            {
                gfx.DrawString(texts[0], titleFont, XBrushes.Black, 60, y);
                y += 40;
                for (int i = 1; i < texts.Count; i++)
                {
                    gfx.DrawString(texts[i], font, XBrushes.Black, 60, y);
                    y += 20;
                }
            }
        }

        using var ms = new MemoryStream();
        pdf.Save(ms);
        return ms.ToArray();
    }

    private async Task<byte[]> ConvertPptxToPngAsync(byte[] source, CancellationToken cancellationToken)
    {
        // Would use a rendering library - placeholder
        throw new NotImplementedException("PPTX to PNG conversion requires rendering engine");
    }

    private async Task<string> ExtractDocxTextAsync(byte[] source, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(source);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            foreach (var run in paragraph.Elements<Run>())
            {
                foreach (var text in run.Elements<Text>())
                {
                    sb.Append(text.Text);
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private async Task<string> ExtractXlsxTextAsync(byte[] source, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(source);
        using var doc = SpreadsheetDocument.Open(stream, false);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart == null) return string.Empty;

        var sb = new StringBuilder();
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable.Elements<SharedStringItem>().ToList() ?? new List<SharedStringItem>();

        foreach (var sheet in workbookPart.Workbook.Sheets!.Elements<Sheet>())
        {
            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
            if (sheetData == null) continue;

            sb.AppendLine($"=== {sheet.Name} ===");
            foreach (var row in sheetData.Elements<Row>())
            {
                var rowValues = new List<string>();
                foreach (var cell in row.Elements<Cell>())
                {
                    var value = GetCellValue(cell, sharedStrings);
                    rowValues.Add(value);
                }
                sb.AppendLine(string.Join("\t", rowValues));
            }
        }
        return sb.ToString();
    }

    private string GetCellValue(Cell cell, List<SharedStringItem> sharedStrings)
    {
        if (cell.CellValue == null) return "";
        var value = cell.CellValue.Text;
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            if (int.TryParse(value, out var index) && index < sharedStrings.Count)
            {
                return sharedStrings[index].InnerText;
            }
        }
        return value;
    }

    private async Task<string> ExtractPptxTextAsync(byte[] source, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(source);
        using var doc = PresentationDocument.Open(stream, false);
        var presentation = doc.PresentationPart?.Presentation;
        var slideIds = presentation?.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        var sb = new StringBuilder();
        int slideNum = 1;
        foreach (var slideId in slideIds)
        {
            var slidePart = (SlidePart)doc.PresentationPart!.GetPartById(slideId.RelationshipId!);
            var slide = slidePart.Slide;

            sb.AppendLine($"=== Slide {slideNum++} ===");
            var texts = slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text);
            foreach (var text in texts)
            {
                sb.AppendLine(text);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private async Task<string> ExtractPdfTextAsync(byte[] source, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(source);
        using var pdf = PdfDocument.Open(stream);
        var sb = new StringBuilder();

        for (int i = 0; i < pdf.PageCount; i++)
        {
            var page = pdf.GetPage(i);
            var text = ContentOrderTextExtractor.GetText(page);
            sb.AppendLine($"=== Page {i + 1} ===");
            sb.AppendLine(text);
        }
        return sb.ToString();
    }

    private async Task<string> ExtractRtfTextAsync(byte[] source, CancellationToken cancellationToken)
    {
        // RTF extraction would need a parser - simplified
        return Encoding.UTF8.GetString(source);
    }
}

/// <summary>
/// PDF-specific operations.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Merge multiple PDFs into one.
    /// </summary>
    Task<byte[]> MergeAsync(IEnumerable<byte[]> pdfs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Split a PDF into multiple PDFs.
    /// </summary>
    Task<IReadOnlyList<byte[]>> SplitAsync(byte[] pdf, IEnumerable<int> pageRanges, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract pages from a PDF.
    /// </summary>
    Task<byte[]> ExtractPagesAsync(byte[] pdf, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotate pages in a PDF.
    /// </summary>
    Task<byte[]> RotatePagesAsync(byte[] pdf, IEnumerable<(int page, int rotation)> rotations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add annotations to a PDF.
    /// </summary>
    Task<byte[]> AddAnnotationsAsync(byte[] pdf, IEnumerable<PdfAnnotation> annotations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fill form fields in a PDF.
    /// </summary>
    Task<byte[]> FillFormAsync(byte[] pdf, Dictionary<string, string> fields, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply password protection to a PDF.
    /// </summary>
    Task<byte[]> ProtectAsync(byte[] pdf, string userPassword, string ownerPassword, PdfPermissions permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove password protection from a PDF.
    /// </summary>
    Task<byte[]> UnprotectAsync(byte[] pdf, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Render a PDF page to an image.
    /// </summary>
    Task<byte[]> RenderPageToImageAsync(byte[] pdf, int pageNumber, int dpi, CancellationToken cancellationToken = default);
}

/// <summary>
/// PDF annotation types.
/// </summary>
public enum PdfAnnotationType
{
    Highlight,
    Underline,
    StrikeOut,
    Squiggly,
    FreeText,
    Line,
    Square,
    Circle,
    Polygon,
    PolyLine,
    Ink,
    Stamp,
    Caret,
    FileAttachment,
    Sound,
    Movie,
    Widget,
    Screen,
    PrinterMark,
    TrapNet,
    Watermark,
    ThreeD,
    Redact
}

/// <summary>
/// PDF annotation.
/// </summary>
public sealed class PdfAnnotation
{
    public PdfAnnotationType Type { get; set; }
    public int PageNumber { get; set; }
    public Rect Bounds { get; set; }
    public string? Contents { get; set; }
    public Color Color { get; set; } = Colors.Yellow;
    public double Opacity { get; set; } = 1.0;
    public Dictionary<string, object> Properties { get; } = new();
}

/// <summary>
/// PDF permissions.
/// </summary>
[Flags]
public enum PdfPermissions
{
    None = 0,
    Print = 1,
    Modify = 2,
    Copy = 4,
    Annotate = 8,
    FillForms = 16,
    Accessibility = 32,
    Assemble = 64,
    HighQualityPrint = 128,
    All = Print | Modify | Copy | Annotate | FillForms | Accessibility | Assemble | HighQualityPrint
}

/// <summary>
/// Implementation of PDF service using PDFsharp.
/// </summary>
public sealed class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> MergeAsync(IEnumerable<byte[]> pdfs, CancellationToken cancellationToken = default)
    {
        using var output = new PdfDocument();
        foreach (var pdfBytes in pdfs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var input = PdfDocument.Open(new MemoryStream(pdfBytes));
            foreach (var page in input.Pages)
            {
                output.AddPage(page);
            }
        }

        using var ms = new MemoryStream();
        output.Save(ms);
        return ms.ToArray();
    }

    public async Task<IReadOnlyList<byte[]>> SplitAsync(byte[] pdf, IEnumerable<int> pageRanges, CancellationToken cancellationToken = default)
    {
        using var source = PdfDocument.Open(new MemoryStream(pdf));
        var results = new List<byte[]>();

        foreach (var pageNum in pageRanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pageNum < 1 || pageNum > source.PageCount) continue;

            using var output = new PdfDocument();
            output.AddPage(source.Pages[pageNum - 1]);

            using var ms = new MemoryStream();
            output.Save(ms);
            results.Add(ms.ToArray());
        }

        return results;
    }

    public async Task<byte[]> ExtractPagesAsync(byte[] pdf, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
    {
        using var source = PdfDocument.Open(new MemoryStream(pdf));
        using var output = new PdfDocument();

        foreach (var pageNum in pageNumbers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pageNum >= 1 && pageNum <= source.PageCount)
            {
                output.AddPage(source.Pages[pageNum - 1]);
            }
        }

        using var ms = new MemoryStream();
        output.Save(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> RotatePagesAsync(byte[] pdf, IEnumerable<(int page, int rotation)> rotations, CancellationToken cancellationToken = default)
    {
        using var doc = PdfDocument.Open(new MemoryStream(pdf));
        foreach (var (pageNum, rotation) in rotations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pageNum >= 1 && pageNum <= doc.PageCount)
            {
                var page = doc.Pages[pageNum - 1];
                page.Rotate = (page.Rotate + rotation) % 360;
            }
        }

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> AddAnnotationsAsync(byte[] pdf, IEnumerable<PdfAnnotation> annotations, CancellationToken cancellationToken = default)
    {
        using var doc = PdfDocument.Open(new MemoryStream(pdf));
        foreach (var annotation in annotations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (annotation.PageNumber >= 1 && annotation.PageNumber <= doc.PageCount)
            {
                var page = doc.Pages[annotation.PageNumber - 1];
                AddAnnotationToPage(page, annotation);
            }
        }

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    private void AddAnnotationToPage(PdfPage page, PdfAnnotation annotation)
    {
        // PDFsharp annotation support is limited
        // This would need proper implementation with PDFsharp's annotation classes
        _logger.LogDebug("Adding annotation of type {Type} to page {Page}", annotation.Type, annotation.PageNumber);
    }

    public async Task<byte[]> FillFormAsync(byte[] pdf, Dictionary<string, string> fields, CancellationToken cancellationToken = default)
    {
        using var doc = PdfDocument.Open(new MemoryStream(pdf));
        var form = doc.AcroForm;
        if (form != null)
        {
            foreach (var (name, value) in fields)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var field = form.Fields[name];
                if (field != null)
                {
                    field.Value = new PdfString(value);
                }
            }
        }

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ProtectAsync(byte[] pdf, string userPassword, string ownerPassword, PdfPermissions permissions, CancellationToken cancellationToken = default)
    {
        using var doc = PdfDocument.Open(new MemoryStream(pdf));
        doc.SecuritySettings.UserPassword = userPassword;
        doc.SecuritySettings.OwnerPassword = ownerPassword;
        doc.SecuritySettings.PermitAccessibilityExtractContent = permissions.HasFlag(PdfPermissions.Accessibility);
        doc.SecuritySettings.PermitAnnotations = permissions.HasFlag(PdfPermissions.Annotate);
        doc.SecuritySettings.PermitAssembleDocument = permissions.HasFlag(PdfPermissions.Assemble);
        doc.SecuritySettings.PermitExtractContent = permissions.HasFlag(PdfPermissions.Copy);
        doc.SecuritySettings.PermitFormsFill = permissions.HasFlag(PdfPermissions.FillForms);
        doc.SecuritySettings.PermitFullQualityPrint = permissions.HasFlag(PdfPermissions.HighQualityPrint);
        doc.SecuritySettings.PermitModifyDocument = permissions.HasFlag(PdfPermissions.Modify);
        doc.SecuritySettings.PermitPrint = permissions.HasFlag(PdfPermissions.Print);

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> UnprotectAsync(byte[] pdf, string password, CancellationToken cancellationToken = default)
    {
        // PDFsharp doesn't support removing passwords easily
        // This would require a different approach
        throw new NotImplementedException("Removing password protection not implemented");
    }

    public async Task<byte[]> RenderPageToImageAsync(byte[] pdf, int pageNumber, int dpi, CancellationToken cancellationToken = default)
    {
        // PDFsharp doesn't have built-in rendering
        // Would need Pdfium or similar
        throw new NotImplementedException("PDF rendering to image requires Pdfium or similar library");
    }
}