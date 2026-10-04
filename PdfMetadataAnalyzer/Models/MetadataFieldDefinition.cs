namespace PdfMetadataAnalyzer.Models;

public enum ExtractorTool
{
    PdfPig,
    PDFsharp,
    TesseractOcr
}

public enum FieldCategory
{
    InfoDictionary,
    Xmp,
    Structural,
    ContentDerived
}

public class MetadataFieldDefinition
{
    public string Key { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public FieldCategory Category { get; init; }
    public ExtractorTool PrimaryTool { get; init; }
    public double Weight { get; init; }
    public List<PdfType> ApplicableTypes { get; init; } = new();
}

public static class MetadataFieldCatalog
{
    public static readonly List<MetadataFieldDefinition> All = new()
    {
        new() { Key = "Title",        DisplayName = "Title",             Category = FieldCategory.InfoDictionary, PrimaryTool = ExtractorTool.PdfPig,       Weight = 3.0, ApplicableTypes = AllTypes() },
        new() { Key = "Author",       DisplayName = "Author",            Category = FieldCategory.InfoDictionary, PrimaryTool = ExtractorTool.PdfPig,       Weight = 3.0, ApplicableTypes = AllTypes() },
        new() { Key = "Subject",      DisplayName = "Subject",           Category = FieldCategory.InfoDictionary, PrimaryTool = ExtractorTool.PdfPig,       Weight = 2.0, ApplicableTypes = AllTypes() },
        new() { Key = "Keywords",     DisplayName = "Keywords",          Category = FieldCategory.InfoDictionary, PrimaryTool = ExtractorTool.PdfPig,       Weight = 1.5, ApplicableTypes = AllTypes() },
        new() { Key = "Creator",      DisplayName = "Creator",           Category = FieldCategory.InfoDictionary, PrimaryTool = ExtractorTool.PdfPig,       Weight = 1.5, ApplicableTypes = AllTypes() },
        new() { Key = "Producer",     DisplayName = "Producer",          Category = FieldCategory.InfoDictionary, PrimaryTool = ExtractorTool.PdfPig,       Weight = 1.0, ApplicableTypes = AllTypes() },
        new() { Key = "CreationDate", DisplayName = "Creation Date",     Category = FieldCategory.InfoDictionary, PrimaryTool = ExtractorTool.PdfPig,       Weight = 2.0, ApplicableTypes = AllTypes() },
        new() { Key = "ModDate",      DisplayName = "Modification Date", Category = FieldCategory.InfoDictionary, PrimaryTool = ExtractorTool.PdfPig,       Weight = 1.0, ApplicableTypes = AllTypes() },

        new() { Key = "XmpTitle",       DisplayName = "XMP Title",        Category = FieldCategory.Xmp, PrimaryTool = ExtractorTool.PdfPig, Weight = 2.0, ApplicableTypes = new() { PdfType.NativeText, PdfType.Hybrid, PdfType.TaggedAccessible } },
        new() { Key = "XmpCreator",     DisplayName = "XMP Creator",      Category = FieldCategory.Xmp, PrimaryTool = ExtractorTool.PdfPig, Weight = 1.5, ApplicableTypes = new() { PdfType.NativeText, PdfType.Hybrid, PdfType.TaggedAccessible } },
        new() { Key = "XmpCreateDate",  DisplayName = "XMP Create Date",  Category = FieldCategory.Xmp, PrimaryTool = ExtractorTool.PdfPig, Weight = 1.5, ApplicableTypes = new() { PdfType.NativeText, PdfType.Hybrid, PdfType.TaggedAccessible } },
        new() { Key = "XmpModifyDate",  DisplayName = "XMP Modify Date",  Category = FieldCategory.Xmp, PrimaryTool = ExtractorTool.PdfPig, Weight = 1.0, ApplicableTypes = new() { PdfType.NativeText, PdfType.Hybrid, PdfType.TaggedAccessible } },
        new() { Key = "XmpProducer",    DisplayName = "XMP Producer",     Category = FieldCategory.Xmp, PrimaryTool = ExtractorTool.PdfPig, Weight = 1.0, ApplicableTypes = new() { PdfType.NativeText, PdfType.Hybrid, PdfType.TaggedAccessible } },

        new() { Key = "PageCount",    DisplayName = "Page Count",     Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 1.5, ApplicableTypes = AllTypes() },
        new() { Key = "PdfVersion",   DisplayName = "PDF Version",    Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 1.0, ApplicableTypes = AllTypes() },
        new() { Key = "PageLayout",   DisplayName = "Page Layout",    Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 0.5, ApplicableTypes = AllTypes() },
        new() { Key = "PageMode",     DisplayName = "Page Mode",      Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 0.5, ApplicableTypes = AllTypes() },
        new() { Key = "IsEncrypted",  DisplayName = "Is Encrypted",   Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 1.0, ApplicableTypes = AllTypes() },
        new() { Key = "IsTagged",     DisplayName = "Is Tagged",      Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 1.5, ApplicableTypes = new() { PdfType.TaggedAccessible } },
        new() { Key = "HasAcroForm",  DisplayName = "Has AcroForm",   Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 1.5, ApplicableTypes = new() { PdfType.Form } },
        new() { Key = "FieldCount",   DisplayName = "Form Field Count", Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 1.5, ApplicableTypes = new() { PdfType.Form } },
        new() { Key = "BookmarkCount",DisplayName = "Bookmark Count", Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 1.0, ApplicableTypes = AllTypes() },
        new() { Key = "CanPrint",     DisplayName = "Can Print",      Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 0.5, ApplicableTypes = AllTypes() },
        new() { Key = "CanCopy",      DisplayName = "Can Copy",       Category = FieldCategory.Structural, PrimaryTool = ExtractorTool.PDFsharp, Weight = 0.5, ApplicableTypes = AllTypes() },

        new() { Key = "WordCount",       DisplayName = "Word Count",       Category = FieldCategory.ContentDerived, PrimaryTool = ExtractorTool.PdfPig,       Weight = 1.5, ApplicableTypes = new() { PdfType.NativeText, PdfType.Hybrid, PdfType.Form, PdfType.TaggedAccessible } },
        new() { Key = "FontCount",       DisplayName = "Font Count",       Category = FieldCategory.ContentDerived, PrimaryTool = ExtractorTool.PdfPig,       Weight = 1.0, ApplicableTypes = new() { PdfType.NativeText, PdfType.Hybrid, PdfType.Form, PdfType.TaggedAccessible } },
        new() { Key = "OcrWordCount",    DisplayName = "OCR Word Count",   Category = FieldCategory.ContentDerived, PrimaryTool = ExtractorTool.TesseractOcr, Weight = 2.0, ApplicableTypes = new() { PdfType.Scanned, PdfType.Hybrid } },
        new() { Key = "OcrInferredTitle",DisplayName = "OCR Inferred Title", Category = FieldCategory.ContentDerived, PrimaryTool = ExtractorTool.TesseractOcr, Weight = 1.5, ApplicableTypes = new() { PdfType.Scanned, PdfType.Hybrid } },
    };

    public static List<MetadataFieldDefinition> ForType(PdfType type) =>
        All.Where(f => f.ApplicableTypes.Contains(type)).ToList();

    public static List<MetadataFieldDefinition> ForTool(ExtractorTool tool) =>
        All.Where(f => f.PrimaryTool == tool).ToList();

    private static List<PdfType> AllTypes() => PdfTypeInfo.AllTypes.ToList();
}