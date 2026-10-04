namespace PdfMetadataAnalyzer.Models;

public class RawResult
{
    public ExtractorTool Tool { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public long ElapsedMs { get; set; }

    public Dictionary<string, string?> Fields { get; set; } = new();

    public string? Title => Fields.GetValueOrDefault("Title");
    public string? Author => Fields.GetValueOrDefault("Author");
    public string? Subject => Fields.GetValueOrDefault("Subject");
    public string? Keywords => Fields.GetValueOrDefault("Keywords");
    public string? Creator => Fields.GetValueOrDefault("Creator");
    public string? Producer => Fields.GetValueOrDefault("Producer");
    public string? CreationDate => Fields.GetValueOrDefault("CreationDate");
    public string? ModDate => Fields.GetValueOrDefault("ModDate");

    public int PageCount { get; set; }
    public double PageWidth { get; set; }
    public double PageHeight { get; set; }
    public string? PdfVersion { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsTagged { get; set; }
    public bool HasAcroForm { get; set; }
    public int FieldCount { get; set; }
    public int BookmarkCount { get; set; }
    public bool CanPrint { get; set; }
    public bool CanCopy { get; set; }
    public bool CanModify { get; set; }

    public int WordCount { get; set; }
    public int CharCount { get; set; }
    public List<string> Fonts { get; set; } = new();

    public double AccuracyScore { get; set; }
    public int FieldsFound { get; set; }
    public int FieldsExpected { get; set; }
}