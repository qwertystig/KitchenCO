using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace YourKitchenCo.Services.Export;

/// <summary>
/// Minimal, dependency-free PDF writer for text documents (production
/// sheets, delivery notes, invoices). Writes a plain PDF 1.4 with the three
/// standard base-14 fonts (Helvetica, Helvetica-Bold, Courier), A4 pages,
/// automatic page breaks, and a correct xref table — nothing else. It
/// exists because the app has no PDF library and adding one couldn't be
/// verified here; this exact byte layout was validated with qpdf/poppler
/// before being ported.
///
/// Text is limited to the WinAnsi (Latin-1) range: common typographic
/// characters (— – × · • …) are mapped to ASCII equivalents and anything
/// else becomes '?'.
/// </summary>
public sealed class SimplePdfWriter
{
    public enum Font { Normal, Bold, Mono }

    private readonly struct Line
    {
        public Line(string text, Font font, double size, double gapAfter)
        {
            Text = text; FontKind = font; Size = size; GapAfter = gapAfter;
        }
        public string Text { get; }
        public Font FontKind { get; }
        public double Size { get; }
        public double GapAfter { get; }
    }

    private const int PageW = 595, PageH = 842;
    private const double MarginL = 40, MarginT = 50, MarginB = 50;

    private readonly List<Line> _lines = new();

    public SimplePdfWriter Heading(string text, double size = 16, double gapAfter = 6) => Add(text, Font.Bold, size, gapAfter);
    public SimplePdfWriter SubHeading(string text, double size = 12, double gapAfter = 4) => Add(text, Font.Bold, size, gapAfter);
    public SimplePdfWriter Text(string text, double size = 10, double gapAfter = 0) => Add(text, Font.Normal, size, gapAfter);
    public SimplePdfWriter Mono(string text, double size = 9, double gapAfter = 0) => Add(text, Font.Mono, size, gapAfter);
    public SimplePdfWriter Blank(double height = 8) => Add(string.Empty, Font.Normal, height / 1.35, 0);
    public SimplePdfWriter Rule(int width = 70) => Mono(new string('-', width));

    /// <summary>Forces the next line onto a new page (no-op at the very top of a page).</summary>
    public SimplePdfWriter PageBreak() => Add(string.Empty, Font.Normal, PageBreakMarker, 0);

    private const double PageBreakMarker = -1;

    public SimplePdfWriter Add(string text, Font font, double size, double gapAfter)
    {
        _lines.Add(new Line(text ?? string.Empty, font, size, gapAfter));
        return this;
    }

    /// <summary>Adds a block of text, one line per newline — handy for feeding an existing text report straight in.</summary>
    public SimplePdfWriter MonoBlock(string block, double size = 9)
    {
        foreach (var l in (block ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            Mono(l, size);
        return this;
    }

    public byte[] Build()
    {
        var inv = CultureInfo.InvariantCulture;

        // Lay lines out into pages (same rule as the validated prototype).
        var pages = new List<List<string>>();
        var current = new List<string>();
        double y = PageH - MarginT;

        foreach (var line in _lines)
        {
            if (line.Size == PageBreakMarker)
            {
                if (current.Count > 0)
                {
                    pages.Add(current);
                    current = new List<string>();
                    y = PageH - MarginT;
                }
                continue;
            }

            var lineHeight = line.Size * 1.35;
            if (y - lineHeight < MarginB)
            {
                pages.Add(current);
                current = new List<string>();
                y = PageH - MarginT;
            }
            y -= lineHeight;
            var fontKey = line.FontKind switch { Font.Bold => "F2", Font.Mono => "F3", _ => "F1" };
            current.Add($"BT /{fontKey} {line.Size.ToString("0.##", inv)} Tf {MarginL.ToString("0.##", inv)} {y.ToString("0.0", inv)} Td ({Sanitize(line.Text)}) Tj ET");
            y -= line.GapAfter;
        }
        pages.Add(current);

        // Objects: 1 catalog, 2 pages, 3-5 fonts, then (content, page) per page.
        var objects = new List<string?>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            null,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>",
        };

        var pageIds = new List<int>();
        foreach (var page in pages)
        {
            var content = string.Join("\n", page) + "\n";
            var contentId = objects.Count + 1;
            objects.Add($"<< /Length {Latin1.GetByteCount(content)} >>\nstream\n{content}endstream");
            var pageId = objects.Count + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageW} {PageH}] /Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >> >> /Contents {contentId} 0 R >>");
            pageIds.Add(pageId);
        }

        var kids = new StringBuilder();
        foreach (var id in pageIds) kids.Append(id).Append(" 0 R ");
        objects[1] = $"<< /Type /Pages /Kids [{kids.ToString().TrimEnd()}] /Count {pageIds.Count} >>";

        using var ms = new MemoryStream();
        void Write(string s) { var b = Latin1.GetBytes(s); ms.Write(b, 0, b.Length); }

        Write("%PDF-1.4\n");
        ms.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A }, 0, 6); // binary comment line

        var offsets = new List<long>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = ms.Position;
        Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var off in offsets)
            Write($"{off.ToString("D10", inv)} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");

        return ms.ToArray();
    }

    private static readonly Encoding Latin1 = Encoding.Latin1;

    private static string Sanitize(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var raw in text)
        {
            var ch = raw switch
            {
                '—' or '–' => '-',
                '×' => 'x',
                '·' or '•' => '-',
                '‘' or '’' => '\'',
                '“' or '”' => '"',
                ' ' => ' ',
                _ => raw
            };
            if (ch == '…') { sb.Append("..."); continue; }
            if (ch < 32 || ch > 126) { sb.Append('?'); continue; }
            if (ch == '\\' || ch == '(' || ch == ')') sb.Append('\\');
            sb.Append(ch);
        }
        return sb.ToString();
    }
}
