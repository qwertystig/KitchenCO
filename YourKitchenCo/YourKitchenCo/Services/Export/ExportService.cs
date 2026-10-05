using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace YourKitchenCo.Services.Export;

/// <summary>
/// "Download" on a phone means: write the file to the app's cache folder and
/// hand it to the system share sheet, which is where Save to Files / Drive /
/// email / print / WhatsApp all live. Same mechanism the invoice and kitchen
/// sheets already use for text, just with a file instead.
/// </summary>
public static class ExportService
{
    public static async Task ShareFileAsync(string fileName, byte[] bytes, string title)
    {
        var safeName = SanitizeFileName(fileName);
        var path = Path.Combine(FileSystem.CacheDirectory, safeName);
        await File.WriteAllBytesAsync(path, bytes);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(path)
        });
    }

    public static Task SharePdfAsync(string fileName, SimplePdfWriter pdf, string title) =>
        ShareFileAsync(EnsureExtension(fileName, ".pdf"), pdf.Build(), title);

    public static Task ShareXlsxAsync(string fileName, SimpleXlsxWriter xlsx, string title) =>
        ShareFileAsync(EnsureExtension(fileName, ".xlsx"), xlsx.Build(), title);

    /// <summary>"Production Sheet 2026-10-02" style stamp for file names.</summary>
    public static string DateStamp(DateOnly date) => date.ToString("yyyy-MM-dd");
    public static string NowStamp() => DateTime.Now.ToString("yyyy-MM-dd_HHmm");

    private static string EnsureExtension(string name, string ext) =>
        name.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? name : name + ext;

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }
}
