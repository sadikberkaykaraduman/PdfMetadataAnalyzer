using System.Diagnostics;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using PdfMetadataAnalyzer.Models;

namespace PdfMetadataAnalyzer.Services.Extractors;

public sealed class PdfPigExtractor
{
    private const int MaxPages = 50;

    public async Task<RawResult> ExtractAsync(string path, CancellationToken ct = default)
    {
        var r = new RawResult { Tool = ExtractorTool.PdfPig };
        var sw = Stopwatch.StartNew();

        try
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                var options = new ParsingOptions { UseLenientParsing = true };
                using var doc = PdfDocument.Open(path, options);
                var info = doc.Information;

                r.Fields["Title"] = Normalizer.Clean(info.Title);
                r.Fields["Author"] = Normalizer.Clean(info.Author);
                r.Fields["Subject"] = Normalizer.Clean(info.Subject);
                r.Fields["Keywords"] = Normalizer.Keywords(info.Keywords);
                r.Fields["Creator"] = Normalizer.Clean(info.Creator);
                r.Fields["Producer"] = Normalizer.Clean(info.Producer);
                r.Fields["CreationDate"] = info.CreationDate;
                r.Fields["ModDate"] = info.ModifiedDate;

                r.PageCount = doc.NumberOfPages;

                var firstPage = doc.GetPage(1);
                r.PageWidth = firstPage.Width;
                r.PageHeight = firstPage.Height;

                TryReadXmp(doc, r);

                int wordCount = 0;
                int charCount = 0;
                var fonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int pagesToScan = Math.Min(doc.NumberOfPages, MaxPages);

                for (int p = 1; p <= pagesToScan; p++)
                {
                    ct.ThrowIfCancellationRequested();
                    var page = doc.GetPage(p);
                    var text = ContentOrderTextExtractor.GetText(page);

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        charCount += text.Length;
                        wordCount += CountWords(text);
                    }

                    foreach (var letter in page.Letters)
                    {
                        if (string.IsNullOrWhiteSpace(letter.FontName)) continue;
                        var fontName = letter.FontName;
                        var plusIndex = fontName.IndexOf('+');
                        if (plusIndex >= 0 && plusIndex < fontName.Length - 1)
                            fontName = fontName[(plusIndex + 1)..];
                        fonts.Add(fontName.Trim());
                    }
                }

                r.WordCount = wordCount;
                r.CharCount = charCount;
                r.Fonts = fonts.OrderBy(f => f).ToList();

                r.Fields["WordCount"] = wordCount.ToString();
                r.Fields["FontCount"] = fonts.Count.ToString();

                r.FieldsFound = r.Fields.Count(kv => !string.IsNullOrWhiteSpace(kv.Value));
                r.FieldsExpected = MetadataFieldCatalog.ForTool(ExtractorTool.PdfPig).Count;

                r.Success = true;

            }, ct);
        }
        catch (OperationCanceledException) { r.Error = "Timeout"; }
        catch (Exception ex) { r.Error = ex.Message; }
        finally
        {
            sw.Stop();
            r.ElapsedMs = sw.ElapsedMilliseconds;
        }

        return r;
    }

    private static void TryReadXmp(PdfDocument doc, RawResult r)
    {
        try
        {
            if (!doc.TryGetXmpMetadata(out var xmp)) return;

            var xdoc = xmp.GetXDocument();
            var ns = new
            {
                Dc = "http://purl.org/dc/elements/1.1/",
                Xmp = "http://ns.adobe.com/xap/1.0/",
                Pdf = "http://ns.adobe.com/pdf/1.3/",
                Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#"
            };

            var titleNode = xdoc.Descendants(System.Xml.Linq.XName.Get("title", ns.Dc)).FirstOrDefault();
            if (titleNode != null)
            {
                var li = titleNode.Descendants(System.Xml.Linq.XName.Get("li", ns.Rdf)).FirstOrDefault();
                r.Fields["XmpTitle"] = Normalizer.Clean(li?.Value ?? titleNode.Value);
            }

            var creatorNode = xdoc.Descendants(System.Xml.Linq.XName.Get("creator", ns.Dc)).FirstOrDefault();
            if (creatorNode != null)
            {
                var li = creatorNode.Descendants(System.Xml.Linq.XName.Get("li", ns.Rdf)).FirstOrDefault();
                r.Fields["XmpCreator"] = Normalizer.Clean(li?.Value ?? creatorNode.Value);
            }

            var createDateNode = xdoc.Descendants(System.Xml.Linq.XName.Get("CreateDate", ns.Xmp)).FirstOrDefault();
            if (createDateNode != null)
                r.Fields["XmpCreateDate"] = Normalizer.Clean(createDateNode.Value);

            var modifyDateNode = xdoc.Descendants(System.Xml.Linq.XName.Get("ModifyDate", ns.Xmp)).FirstOrDefault();
            if (modifyDateNode != null)
                r.Fields["XmpModifyDate"] = Normalizer.Clean(modifyDateNode.Value);

            var producerNode = xdoc.Descendants(System.Xml.Linq.XName.Get("Producer", ns.Pdf)).FirstOrDefault();
            if (producerNode != null)
                r.Fields["XmpProducer"] = Normalizer.Clean(producerNode.Value);
        }
        catch { }
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