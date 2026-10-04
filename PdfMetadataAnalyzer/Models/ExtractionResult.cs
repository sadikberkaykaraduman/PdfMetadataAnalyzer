namespace PdfMetadataAnalyzer.Models;

public class ExtractionResult
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public PdfType SelectedType { get; set; }

    public string FileSizeLabel => FileSizeBytes < 1024 * 1024
        ? $"{FileSizeBytes / 1024.0:F1} KB"
        : $"{FileSizeBytes / 1024.0 / 1024.0:F2} MB";

    public string FileInfoLabel =>
        $"{FileSizeLabel} · {PageCount} pages · {PdfTypeInfo.GetDisplayName(SelectedType)}";

    public Dictionary<string, MetadataField> Fields { get; set; } = new();

    public MetadataField Title => Fields.GetValueOrDefault("Title") ?? new();
    public MetadataField Author => Fields.GetValueOrDefault("Author") ?? new();
    public MetadataField Subject => Fields.GetValueOrDefault("Subject") ?? new();
    public MetadataField Keywords => Fields.GetValueOrDefault("Keywords") ?? new();
    public MetadataField Creator => Fields.GetValueOrDefault("Creator") ?? new();
    public MetadataField Producer => Fields.GetValueOrDefault("Producer") ?? new();
    public MetadataField CreationDate => Fields.GetValueOrDefault("CreationDate") ?? new();
    public MetadataField ModificationDate => Fields.GetValueOrDefault("ModDate") ?? new();

    public int PageCount { get; set; }
    public string PdfVersion { get; set; } = "";
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

    public RawResult PdfPigRaw { get; set; } = new();
    public RawResult PdfSharpRaw { get; set; } = new();
    public RawResult? OcrRaw { get; set; }
    public bool OcrWasUsed { get; set; }

    public ToolScores Scores { get; set; } = new();
}

public class ToolScores
{
    public double PdfPigScore { get; set; }
    public double PdfSharpScore { get; set; }
    public double OcrScore { get; set; }
    public double CombinedScore { get; set; }

    public long PdfPigMs { get; set; }
    public long PdfSharpMs { get; set; }
    public long OcrMs { get; set; }
    public long TotalMs { get; set; }

    public int FieldsExpected { get; set; }
    public int FieldsFound { get; set; }
    public int ConsensusCount { get; set; }

    public string Grade => CombinedScore switch
    {
        >= 90 => "Excellent",
        >= 75 => "Good",
        >= 50 => "Fair",
        _ => "Poor"
    };
}