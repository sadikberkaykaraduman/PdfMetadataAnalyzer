using PdfMetadataAnalyzer.Models;
using System.Diagnostics;
using System.IO;
using Tesseract;

namespace PdfMetadataAnalyzer.Services.Extractors;

public sealed class TesseractOcrExtractor
{
    private const int MaxOcrPages = 5;
    private readonly string _tessDataPath;

    public TesseractOcrExtractor()
    {
        _tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    }

    public bool ShouldRunOcr(int pdfPigCharCount, int pageCount, PdfType selectedType)
    {
        if (pageCount == 0) return false;
        if (selectedType == PdfType.Scanned) return true;
        if (selectedType == PdfType.Hybrid) return true;
        double avgCharsPerPage = (double)pdfPigCharCount / pageCount;
        return avgCharsPerPage < 20;
    }

    public async Task<RawResult> ExtractAsync(string path, int pageCount, CancellationToken ct = default)
    {
        var r = new RawResult { Tool = ExtractorTool.TesseractOcr };
        var sw = Stopwatch.StartNew();

        try
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                if (!Directory.Exists(_tessDataPath))
                {
                    r.Error = $"tessdata folder not found: {_tessDataPath}";
                    return;
                }

                using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);

                int scanPages = Math.Min(pageCount, MaxOcrPages);
                int totalChars = 0;
                int totalWords = 0;
                string? firstPageText = null;

                for (int p = 0; p < scanPages; p++)
                {
                    ct.ThrowIfCancellationRequested();

                    var png = PageRenderer.RenderPageToPng(path, p, dpi: 300);
                    if (png == null) continue;

                    using var img = Pix.LoadFromMemory(png);
                    using var ocrPage = engine.Process(img);

                    var text = ocrPage.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        if (p == 0) firstPageText = text;
                        totalChars += text.Length;
                        totalWords += CountWords(text);
                    }
                }

                r.WordCount = totalWords;
                r.CharCount = totalChars;
                r.Fields["OcrWordCount"] = totalWords.ToString();

                if (!string.IsNullOrWhiteSpace(firstPageText))
                {
                    var firstLine = firstPageText
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault()?.Trim();

                    if (!string.IsNullOrWhiteSpace(firstLine) && firstLine.Length > 3)
                        r.Fields["OcrInferredTitle"] = Normalizer.Clean(firstLine);
                }

                r.FieldsFound = r.Fields.Count(kv => !string.IsNullOrWhiteSpace(kv.Value));
                r.FieldsExpected = MetadataFieldCatalog.ForTool(ExtractorTool.TesseractOcr).Count;

                r.Success = totalChars > 0;
                if (!r.Success)
                    r.Error = "OCR could not extract any text (image quality may be insufficient)";

            }, ct);
        }
        catch (OperationCanceledException) { r.Error = "Timeout"; }
        catch (Exception ex) { r.Error = $"OCR error: {ex.Message}"; }
        finally
        {
            sw.Stop();
            r.ElapsedMs = sw.ElapsedMilliseconds;
        }

        return r;
    }

    private static int CountWords(string text)
    {
        bool inWord = false;
        int count = 0;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch)) inWord = false;
            else if (!inWord) { inWord = true; count++; }
        }
        return count;
    }
}