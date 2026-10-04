using System.Diagnostics;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.AcroForms;
using PdfMetadataAnalyzer.Models;

namespace PdfMetadataAnalyzer.Services.Extractors;

public sealed class PdfSharpExtractor
{
    private static readonly HashSet<string> StandardKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "/Title", "/Author", "/Subject", "/Keywords",
            "/Creator", "/Producer", "/CreationDate", "/ModDate", "/Trapped"
        };

    public async Task<RawResult> ExtractAsync(string path, CancellationToken ct = default)
    {
        var r = new RawResult { Tool = ExtractorTool.PDFsharp };
        var sw = Stopwatch.StartNew();

        try
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                using var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                var info = doc.Info;

                r.Fields["Title"] = Normalizer.Clean(info.Title);
                r.Fields["Author"] = Normalizer.Clean(info.Author);
                r.Fields["Subject"] = Normalizer.Clean(info.Subject);
                r.Fields["Keywords"] = Normalizer.Keywords(info.Keywords);
                r.Fields["Creator"] = Normalizer.Clean(info.Creator);
                r.Fields["Producer"] = Normalizer.Clean(info.Producer);
                r.Fields["CreationDate"] = info.CreationDate == DateTime.MinValue ? null : info.CreationDate.ToString("O");
                r.Fields["ModDate"] = info.ModificationDate == DateTime.MinValue ? null : info.ModificationDate.ToString("O");

                foreach (var key in info.Elements.Keys)
                {
                    if (StandardKeys.Contains(key)) continue;
                    var value = info.Elements[key]?.ToString()?.Trim('/', '(', ')');
                    if (!string.IsNullOrWhiteSpace(value))
                        r.Fields[$"Custom_{key.TrimStart('/')}"] = value;
                }

                r.PageCount = doc.PageCount;
                r.PdfVersion = doc.Version.ToString();
                r.IsTagged = GetIsTagged(doc);

                r.Fields["PageCount"] = r.PageCount.ToString();
                r.Fields["PdfVersion"] = r.PdfVersion;
                r.Fields["PageLayout"] = doc.PageLayout.ToString();
                r.Fields["PageMode"] = doc.PageMode.ToString();
                r.Fields["IsTagged"] = r.IsTagged.ToString();
                r.Fields["IsEncrypted"] = r.IsEncrypted.ToString();

                if (doc.PageCount > 0)
                {
                    var page0 = doc.Pages[0];
                    r.PageWidth = page0.Width.Point;
                    r.PageHeight = page0.Height.Point;
                }

                ReadAcroForm(doc, r);
                ReadBookmarks(doc, r);

                r.FieldsFound = r.Fields.Count(kv => !string.IsNullOrWhiteSpace(kv.Value));
                r.FieldsExpected = MetadataFieldCatalog.ForTool(ExtractorTool.PDFsharp).Count;

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

    private static bool GetIsTagged(PdfDocument doc)
    {
        try
        {
            var markInfo = doc.Internals.Catalog.Elements["/MarkInfo"] as PdfDictionary;
            return markInfo?.Elements["/Marked"]?.ToString() == "true";
        }
        catch { return false; }
    }

    private static void ReadAcroForm(PdfDocument doc, RawResult r)
    {
        try
        {
            var form = doc.AcroForm;
            if (form == null || form.Fields.Count == 0)
            {
                r.HasAcroForm = false;
                r.FieldCount = 0;
                return;
            }

            r.HasAcroForm = true;
            r.FieldCount = form.Fields.Count;

            r.Fields["HasAcroForm"] = "true";
            r.Fields["FieldCount"] = r.FieldCount.ToString();
        }
        catch
        {
            r.HasAcroForm = false;
            r.Fields["HasAcroForm"] = "false";
        }
    }

    private static void ReadBookmarks(PdfDocument doc, RawResult r)
    {
        try
        {
            int count = CountOutlines(doc.Outlines);
            r.BookmarkCount = count;
            r.Fields["BookmarkCount"] = count.ToString();
        }
        catch
        {
            r.Fields["BookmarkCount"] = "0";
        }
    }

    private static int CountOutlines(PdfOutlineCollection? collection)
    {
        if (collection == null || collection.Count == 0) return 0;
        int total = collection.Count;
        foreach (var item in collection)
            total += CountOutlines(item.Outlines);
        return total;
    }
}