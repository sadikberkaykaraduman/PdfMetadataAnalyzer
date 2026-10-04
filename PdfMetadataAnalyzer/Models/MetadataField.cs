namespace PdfMetadataAnalyzer.Models;

public enum ConfidenceLevel
{
    None,
    Low,
    Medium,
    High,
    Consensus
}

public class MetadataField
{
    public string? Value { get; set; }
    public ConfidenceLevel Confidence { get; set; }
    public List<string> Sources { get; set; } = new();
    public string? Conflict { get; set; }

    public string ConfidenceLabel => Confidence switch
    {
        ConfidenceLevel.Consensus => "Consensus",
        ConfidenceLevel.High => "High",
        ConfidenceLevel.Medium => "Medium",
        ConfidenceLevel.Low => "Low",
        _ => "—"
    };
}