using System.IO;
using PDFtoImage;
using SkiaSharp;

namespace PdfMetadataAnalyzer.Services;

public static class PageRenderer
{
    public static byte[]? RenderPageToPng(string pdfPath, int pageIndex, int dpi = 300)
    {
        try
        {
            byte[] pdfBytes = File.ReadAllBytes(pdfPath);

            using SKBitmap bitmap = Conversion.ToImage(
                pdfBytes,
                password: null,
                page: pageIndex,
                options: new RenderOptions { Dpi = dpi });

            if (bitmap == null) return null;

            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return data?.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public static int GetPageCount(string pdfPath)
    {
        try
        {
            byte[] pdfBytes = File.ReadAllBytes(pdfPath);
            return Conversion.GetPageCount(pdfBytes);
        }
        catch { return 0; }
    }
}