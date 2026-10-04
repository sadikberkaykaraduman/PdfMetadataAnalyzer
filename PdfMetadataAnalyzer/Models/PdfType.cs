namespace PdfMetadataAnalyzer.Models;

public enum PdfType
{
    NativeText,
    Scanned,
    Hybrid,
    Form,
    TaggedAccessible
}

public static class PdfTypeInfo
{
    public static string GetDisplayName(PdfType type) => type switch
    {
        PdfType.NativeText => "Native Text",
        PdfType.Scanned => "Scanned",
        PdfType.Hybrid => "Hybrid (Scanned + OCR layer)",
        PdfType.Form => "Form (AcroForm)",
        PdfType.TaggedAccessible => "Tagged / Accessible",
        _ => type.ToString()
    };

    public static string GetDescription(PdfType type) => type switch
    {
        PdfType.NativeText =>
            "Produced by a word processor or typesetting tool. Contains a real, extractable text layer.",
        PdfType.Scanned =>
            "Created by scanning a paper document. Contains only page images; no text layer exists.",
        PdfType.Hybrid =>
            "A scanned document that has an OCR-generated text layer added on top of the page images.",
        PdfType.Form =>
            "Contains interactive AcroForm fields such as text boxes, checkboxes, or dropdowns.",
        PdfType.TaggedAccessible =>
            "Contains a tagged structure tree intended for screen readers and accessibility compliance.",
        _ => ""
    };

    public static IEnumerable<PdfType> AllTypes => Enum.GetValues<PdfType>();
}