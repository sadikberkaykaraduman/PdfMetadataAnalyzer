using PdfMetadataAnalyzer.Models;
using System.IO;

namespace PdfMetadataAnalyzer.Services;

public class Consolidator
{
    public ExtractionResult Merge(
        string path, PdfType selectedType,
        RawResult pig, RawResult ps, RawResult? ocr = null)
    {
        var ex = new ExtractionResult
        {
            FilePath = path,
            FileName = Path.GetFileName(path),
            FileSizeBytes = File.Exists(path) ? new FileInfo(path).Length : 0,
            SelectedType = selectedType,
            PdfPigRaw = pig,
            PdfSharpRaw = ps,
            OcrRaw = ocr,
            OcrWasUsed = ocr != null,
        };

        var activeSources = ocr != null
            ? new[] { pig, ps, ocr }.Where(r => r.Success).ToList()
            : new[] { pig, ps }.Where(r => r.Success).ToList();

        var applicableFields = MetadataFieldCatalog.ForType(selectedType);

        foreach (var fieldDef in applicableFields)
            ex.Fields[fieldDef.Key] = MergeField(fieldDef, activeSources, ocr);

        ex.PageCount = Majority(activeSources, r => r.PageCount);
        ex.PdfVersion = FirstNonEmpty(activeSources, r => r.PdfVersion) ?? "";
        ex.IsEncrypted = activeSources.Any(r => r.IsEncrypted);
        ex.IsTagged = activeSources.Any(r => r.IsTagged);
        ex.HasAcroForm = activeSources.Any(r => r.HasAcroForm);
        ex.FieldCount = activeSources.Select(r => r.FieldCount).DefaultIfEmpty(0).Max();
        ex.BookmarkCount = activeSources.Select(r => r.BookmarkCount).DefaultIfEmpty(0).Max();
        ex.CanPrint = activeSources.Any(r => r.CanPrint);
        ex.CanCopy = activeSources.Any(r => r.CanCopy);
        ex.CanModify = activeSources.Any(r => r.CanModify);

        var textSource = ocr?.Success == true ? ocr : pig;
        ex.WordCount = textSource.WordCount;
        ex.CharCount = textSource.CharCount;
        ex.Fonts = pig.Fonts.Concat(ps.Fonts)
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .OrderBy(f => f)
                          .ToList();

        pig.AccuracyScore = ScoreInfoTool(pig, applicableFields);
        ps.AccuracyScore = ScoreInfoTool(ps, applicableFields);
        if (ocr != null) ocr.AccuracyScore = ScoreOcrTool(ocr);

        ex.Scores = ComputeScores(pig, ps, ocr, ex, applicableFields);

        return ex;
    }

    private static MetadataField MergeField(
        MetadataFieldDefinition fieldDef, List<RawResult> sources, RawResult? ocr)
    {
        var field = new MetadataField();

        var candidates = sources
            .Select(r => new { r.Tool, Value = r.Fields.GetValueOrDefault(fieldDef.Key) })
            .Where(c => !string.IsNullOrWhiteSpace(c.Value))
            .ToList();

        if (candidates.Count == 0)
        {
            if (fieldDef.Key == "Title" &&
                ocr?.Fields.GetValueOrDefault("OcrInferredTitle") is string ocrTitle &&
                !string.IsNullOrWhiteSpace(ocrTitle))
            {
                field.Value = ocrTitle;
                field.Confidence = ConfidenceLevel.Low;
                field.Sources = new List<string> { "TesseractOCR (inferred)" };
                field.Conflict = "Inferred from OCR first line, not from metadata fields";
            }
            return field;
        }

        field.Sources = candidates.Select(c => c.Tool.ToString()).ToList();

        var groups = candidates
            .GroupBy(c => Normalize(c.Value!), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key.Length)
            .ToList();

        if (groups.Count == 1)
        {
            field.Value = candidates.OrderByDescending(c => c.Value!.Length).First().Value;
            field.Confidence = candidates.Count >= 2
                ? ConfidenceLevel.High
                : ConfidenceLevel.Low;
        }
        else
        {
            field.Conflict = string.Join(" | ", groups.Select(g =>
                $"[{string.Join(",", g.Select(x => x.Tool))}]={g.Key}"));

            var primaryValue = candidates
                .FirstOrDefault(c => c.Tool == fieldDef.PrimaryTool)?.Value;

            if (!string.IsNullOrWhiteSpace(primaryValue))
            {
                field.Value = primaryValue;
                field.Confidence = ConfidenceLevel.Medium;
            }
            else
            {
                var winner = groups.First();
                field.Value = winner.OrderByDescending(c => c.Value!.Length).First().Value;
                field.Confidence = winner.Count() >= 2
                    ? ConfidenceLevel.Medium
                    : ConfidenceLevel.Low;
            }
        }

        return field;
    }

    private static ToolScores ComputeScores(
        RawResult pig, RawResult ps, RawResult? ocr,
        ExtractionResult ex, List<MetadataFieldDefinition> applicableFields)
    {
        var scores = new ToolScores
        {
            PdfPigMs = pig.ElapsedMs,
            PdfSharpMs = ps.ElapsedMs,
            OcrMs = ocr?.ElapsedMs ?? 0,
            TotalMs = pig.ElapsedMs + ps.ElapsedMs + (ocr?.ElapsedMs ?? 0),
            PdfPigScore = pig.AccuracyScore,
            PdfSharpScore = ps.AccuracyScore,
            OcrScore = ocr?.AccuracyScore ?? 0,
            FieldsExpected = applicableFields.Count,
        };

        double totalWeight = applicableFields.Sum(f => f.Weight);
        double earnedWeight = 0;
        int foundCount = 0;
        int consensusCount = 0;

        foreach (var fieldDef in applicableFields)
        {
            if (!ex.Fields.TryGetValue(fieldDef.Key, out var field) ||
                string.IsNullOrWhiteSpace(field.Value))
                continue;

            foundCount++;
            if (field.Confidence >= ConfidenceLevel.High) consensusCount++;

            double multiplier = field.Confidence switch
            {
                ConfidenceLevel.Consensus => 1.0,
                ConfidenceLevel.High => 0.9,
                ConfidenceLevel.Medium => 0.75,
                ConfidenceLevel.Low => 0.5,
                _ => 0.3
            };

            earnedWeight += fieldDef.Weight * multiplier;
        }

        scores.FieldsFound = foundCount;
        scores.ConsensusCount = consensusCount;

        scores.CombinedScore = totalWeight > 0
            ? (earnedWeight / totalWeight) * 100.0
            : 0;

        var allToolScores = new List<double> { pig.AccuracyScore, ps.AccuracyScore };
        if (ocr != null) allToolScores.Add(ocr.AccuracyScore);
        double bestToolScore = allToolScores.Max();

        if (scores.CombinedScore < bestToolScore)
            scores.CombinedScore = bestToolScore;

        return scores;
    }

    private static double ScoreInfoTool(
        RawResult r, List<MetadataFieldDefinition> applicableFields)
    {
        if (!r.Success) return 0;

        double totalWeight = applicableFields.Sum(f => f.Weight);
        double earnedWeight = 0;

        foreach (var fieldDef in applicableFields)
        {
            var value = r.Fields.GetValueOrDefault(fieldDef.Key);
            if (!string.IsNullOrWhiteSpace(value))
                earnedWeight += fieldDef.Weight * Normalizer.ValueScore(value);
        }

        return totalWeight > 0 ? (earnedWeight / totalWeight) * 100.0 : 0;
    }

    private static double ScoreOcrTool(RawResult r)
    {
        if (!r.Success || r.CharCount == 0) return 0;

        double wordDensity = r.CharCount > 0 ? (double)r.WordCount / r.CharCount : 0;
        double densityScore = Math.Min(wordDensity / 0.18, 1.0) * 60.0;
        double volumeScore = Math.Min(r.WordCount / 50.0, 1.0) * 40.0;

        return Math.Round(densityScore + volumeScore, 1);
    }

    private static string Normalize(string s) =>
        s.Trim().ToLowerInvariant().TrimEnd('.', ',', ';');

    private static int Majority(List<RawResult> sources, Func<RawResult, int> selector)
    {
        var values = sources.Select(selector).Where(v => v > 0).ToList();
        return values.Count == 0
            ? 0
            : values.GroupBy(v => v).OrderByDescending(g => g.Count()).First().Key;
    }

    private static string? FirstNonEmpty(
        IEnumerable<RawResult> sources, Func<RawResult, string?> selector)
    {
        foreach (var r in sources)
        {
            var value = selector(r);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return null;
    }
}