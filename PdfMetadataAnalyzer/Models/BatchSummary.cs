namespace PdfMetadataAnalyzer.Models;

public class BatchSummary
{
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }

    public double AvgCombined { get; set; }
    public double AvgPdfPig { get; set; }
    public double AvgPdfSharp { get; set; }
    public double AvgOcr { get; set; }
    public double AvgMs { get; set; }

    public List<ExtractionResult> Results { get; set; } = new();

    public Dictionary<PdfType, List<ExtractionResult>> ByType =>
        Results.GroupBy(r => r.SelectedType)
               .ToDictionary(g => g.Key, g => g.ToList());

    public Dictionary<PdfType, double> AvgCombinedByType =>
        ByType.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Count > 0 ? kv.Value.Average(r => r.Scores.CombinedScore) : 0);

    public Dictionary<PdfType, double> AvgPdfPigByType =>
        ByType.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Count > 0 ? kv.Value.Average(r => r.Scores.PdfPigScore) : 0);

    public Dictionary<PdfType, double> AvgPdfSharpByType =>
        ByType.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Count > 0 ? kv.Value.Average(r => r.Scores.PdfSharpScore) : 0);

    public Dictionary<PdfType, double> AvgOcrByType =>
        ByType.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Count > 0 ? kv.Value.Average(r => r.Scores.OcrScore) : 0);
}