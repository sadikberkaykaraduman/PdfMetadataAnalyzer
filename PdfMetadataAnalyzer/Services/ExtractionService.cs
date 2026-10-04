using CsvHelper;
using Newtonsoft.Json;
using PdfMetadataAnalyzer.Models;
using PdfMetadataAnalyzer.Services.Extractors;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace PdfMetadataAnalyzer.Services;

public class ExtractionService
{
    private readonly PdfPigExtractor _pig = new();
    private readonly PdfSharpExtractor _ps = new();
    private readonly TesseractOcrExtractor _ocr = new();
    private readonly Consolidator _consolidator = new();

    private readonly TimeSpan _timeout;
    private readonly int _concurrency;

    public ExtractionService(int timeoutSeconds = 30, int concurrency = 4)
    {
        _timeout = TimeSpan.FromSeconds(timeoutSeconds);
        _concurrency = concurrency;
    }

    public async Task<ExtractionResult> ProcessFileAsync(
        string path, PdfType selectedType,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(path);

        progress?.Report($"Extracting: {Path.GetFileName(path)}");

        var pigTask = RunWithTimeout(() => _pig.ExtractAsync(path, ct), ExtractorTool.PdfPig);
        var psTask = RunWithTimeout(() => _ps.ExtractAsync(path, ct), ExtractorTool.PDFsharp);
        await Task.WhenAll(pigTask, psTask);

        var pigResult = pigTask.Result;
        var psResult = psTask.Result;

        RawResult? ocrResult = null;
        if (pigResult.Success && _ocr.ShouldRunOcr(pigResult.CharCount, pigResult.PageCount, selectedType))
        {
            progress?.Report($"Scanned content detected, running OCR: {Path.GetFileName(path)}");
            ocrResult = await RunOcrWithTimeout(path, pigResult.PageCount, ct);
        }

        var result = _consolidator.Merge(path, selectedType, pigResult, psResult, ocrResult);

        progress?.Report(
            $"Done: {Path.GetFileName(path)} ({result.Scores.CombinedScore:F1} score" +
            (ocrResult != null ? ", OCR used" : "") + ")");

        return result;
    }

    public async Task<BatchSummary> ProcessDirectoryAsync(
        string dir, PdfType selectedType, bool recursive,
        IProgress<(int done, int total, string file)>? progress = null,
        CancellationToken ct = default)
    {
        var files = Directory.GetFiles(dir, "*.pdf",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        var summary = new BatchSummary { Total = files.Length };
        var results = new System.Collections.Concurrent.ConcurrentBag<ExtractionResult>();
        var semaphore = new SemaphoreSlim(_concurrency);
        int done = 0;

        await Task.WhenAll(files.Select(async file =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await ProcessFileAsync(file, selectedType, ct: ct);
                results.Add(result);
                Interlocked.Increment(ref done);
                progress?.Report((done, files.Length, Path.GetFileName(file)));
                lock (summary) summary.Succeeded++;
            }
            catch
            {
                lock (summary) summary.Failed++;
            }
            finally
            {
                semaphore.Release();
            }
        }));

        summary.Results = results.ToList();

        if (summary.Results.Count > 0)
        {
            var scores = summary.Results.Select(r => r.Scores).ToList();
            summary.AvgCombined = scores.Average(s => s.CombinedScore);
            summary.AvgPdfPig = scores.Average(s => s.PdfPigScore);
            summary.AvgPdfSharp = scores.Average(s => s.PdfSharpScore);
            summary.AvgOcr = scores.Average(s => s.OcrScore);
            summary.AvgMs = scores.Average(s => s.TotalMs);
        }

        return summary;
    }

    public void ExportJson(ExtractionResult result, string path)
    {
        var obj = BuildJsonObject(result);
        File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented), Encoding.UTF8);
    }

    public void ExportBatchJson(BatchSummary summary, string path)
    {
        var obj = new
        {
            summary.Total,
            summary.Succeeded,
            summary.Failed,
            avgCombined = Math.Round(summary.AvgCombined, 2),
            avgPdfPig = Math.Round(summary.AvgPdfPig, 2),
            avgPdfSharp = Math.Round(summary.AvgPdfSharp, 2),
            avgOcr = Math.Round(summary.AvgOcr, 2),
            avgMs = Math.Round(summary.AvgMs, 1),
            files = summary.Results.Select(BuildJsonObject).ToList()
        };
        File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented), Encoding.UTF8);
    }

    public void ExportBatchCsv(BatchSummary summary, string path)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(summary.Results.Select(r => new
        {
            r.FileName,
            PdfType = r.SelectedType.ToString(),
            FileSizeKB = Math.Round(r.FileSizeBytes / 1024.0, 1),
            Title = r.Title.Value,
            TitleConfidence = r.Title.ConfidenceLabel,
            Author = r.Author.Value,
            r.PageCount,
            r.WordCount,
            r.IsEncrypted,
            r.HasAcroForm,
            r.FieldCount,
            OcrUsed = r.OcrWasUsed,
            PdfPigScore = Math.Round(r.Scores.PdfPigScore, 1),
            PdfSharpScore = Math.Round(r.Scores.PdfSharpScore, 1),
            OcrScore = Math.Round(r.Scores.OcrScore, 1),
            CombinedScore = Math.Round(r.Scores.CombinedScore, 1),
            r.Scores.Grade,
            r.Scores.FieldsExpected,
            r.Scores.FieldsFound,
            r.Scores.TotalMs,
        }));
    }

    private async Task<RawResult> RunWithTimeout(Func<Task<RawResult>> action, ExtractorTool tool)
    {
        using var cts = new CancellationTokenSource(_timeout);
        try
        {
            return await action();
        }
        catch
        {
            return new RawResult { Tool = tool, Success = false, Error = "Timeout or unexpected error" };
        }
    }

    private async Task<RawResult?> RunOcrWithTimeout(string path, int pageCount, CancellationToken ct)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            return await _ocr.ExtractAsync(path, pageCount, cts.Token);
        }
        catch
        {
            return new RawResult
            {
                Tool = ExtractorTool.TesseractOcr,
                Success = false,
                Error = "OCR timeout (60s exceeded)"
            };
        }
    }

    private static object BuildJsonObject(ExtractionResult r) => new
    {
        file = new { r.FileName, r.FilePath, r.FileSizeBytes, pdfType = r.SelectedType.ToString() },
        metadata = r.Fields.ToDictionary(
            kv => kv.Key,
            kv => new { value = kv.Value.Value, confidence = kv.Value.Confidence.ToString(), sources = kv.Value.Sources }),
        structure = new
        {
            r.PageCount,
            r.PdfVersion,
            r.IsEncrypted,
            r.IsTagged,
            r.HasAcroForm,
            r.FieldCount,
            r.BookmarkCount,
            r.WordCount,
            r.CharCount,
            fontCount = r.Fonts.Count
        },
        permissions = new { r.CanPrint, r.CanCopy, r.CanModify },
        ocr = new { used = r.OcrWasUsed, score = r.Scores.OcrScore, timeMs = r.Scores.OcrMs },
        scores = new
        {
            r.Scores.PdfPigScore,
            r.Scores.PdfSharpScore,
            r.Scores.OcrScore,
            r.Scores.CombinedScore,
            r.Scores.Grade,
            r.Scores.FieldsExpected,
            r.Scores.FieldsFound,
            r.Scores.ConsensusCount,
            timings = new { r.Scores.PdfPigMs, r.Scores.PdfSharpMs, r.Scores.OcrMs, r.Scores.TotalMs }
        }
    };
}