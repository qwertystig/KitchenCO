using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace YourKitchenCo.Services.Export;

/// <summary>
/// Minimal, dependency-free .xlsx writer: a real Office Open XML workbook
/// (one or more sheets, bold header rows, numeric cells kept numeric,
/// column widths) built with the BCL's ZipArchive — no NuGet package. The
/// exact part layout was validated with openpyxl and LibreOffice before
/// being ported here. Strings are written inline (no shared-strings part)
/// to keep it to five small XML parts.
/// </summary>
public sealed class SimpleXlsxWriter
{
    public sealed class Sheet
    {
        public Sheet(string name) { Name = name; }
        public string Name { get; }
        public List<(object?[] Cells, bool Bold)> Rows { get; } = new();
        public double[]? ColumnWidths { get; set; }

        public Sheet Header(params object?[] cells) { Rows.Add((cells, true)); return this; }
        public Sheet Row(params object?[] cells) { Rows.Add((cells, false)); return this; }
        public Sheet Blank() { Rows.Add((Array.Empty<object?>(), false)); return this; }
        public Sheet Widths(params double[] widths) { ColumnWidths = widths; return this; }
    }

    private readonly List<Sheet> _sheets = new();

    public Sheet AddSheet(string name)
    {
        // Excel sheet-name rules: max 31 chars, none of : \ / ? * [ ]
        var clean = new StringBuilder();
        foreach (var ch in name)
            clean.Append(":\\/?*[]".IndexOf(ch) >= 0 ? ' ' : ch);
        var s = clean.ToString().Trim();
        if (s.Length > 31) s = s[..31];
        if (s.Length == 0) s = $"Sheet{_sheets.Count + 1}";
        var sheet = new Sheet(s);
        _sheets.Add(sheet);
        return sheet;
    }

    public byte[] Build()
    {
        if (_sheets.Count == 0) AddSheet("Sheet1");

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var n = _sheets.Count;

            var contentTypes = new StringBuilder();
            contentTypes.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (var i = 0; i < n; i++)
                contentTypes.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            contentTypes.Append("</Types>");
            AddEntry(zip, "[Content_Types].xml", contentTypes.ToString());

            AddEntry(zip, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");

            var workbook = new StringBuilder();
            workbook.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
            for (var i = 0; i < n; i++)
                workbook.Append($"<sheet name=\"{Esc(_sheets[i].Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
            workbook.Append("</sheets></workbook>");
            AddEntry(zip, "xl/workbook.xml", workbook.ToString());

            var rels = new StringBuilder();
            rels.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (var i = 0; i < n; i++)
                rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
            rels.Append($"<Relationship Id=\"rId{n + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            AddEntry(zip, "xl/_rels/workbook.xml.rels", rels.ToString());

            AddEntry(zip, "xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");

            for (var i = 0; i < n; i++)
                AddEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", SheetXml(_sheets[i]));
        }

        return ms.ToArray();
    }

    private static string SheetXml(Sheet sheet)
    {
        var inv = CultureInfo.InvariantCulture;
        var x = new StringBuilder();
        x.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

        if (sheet.ColumnWidths is { Length: > 0 })
        {
            x.Append("<cols>");
            for (var i = 0; i < sheet.ColumnWidths.Length; i++)
                x.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{sheet.ColumnWidths[i].ToString("0.##", inv)}\" customWidth=\"1\"/>");
            x.Append("</cols>");
        }

        x.Append("<sheetData>");
        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var (cells, bold) = sheet.Rows[r];
            x.Append($"<row r=\"{r + 1}\">");
            for (var c = 0; c < cells.Length; c++)
            {
                var value = cells[c];
                if (value is null) continue;
                var cellRef = $"{ColumnName(c + 1)}{r + 1}";
                var style = bold ? " s=\"1\"" : string.Empty;

                switch (value)
                {
                    case int or long or short or byte:
                        x.Append($"<c r=\"{cellRef}\"{style}><v>{Convert.ToInt64(value, inv)}</v></c>");
                        break;
                    case decimal dec:
                        x.Append($"<c r=\"{cellRef}\"{style}><v>{dec.ToString(inv)}</v></c>");
                        break;
                    case double or float:
                        x.Append($"<c r=\"{cellRef}\"{style}><v>{Convert.ToDouble(value, inv).ToString("R", inv)}</v></c>");
                        break;
                    default:
                        x.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"{style}><is><t xml:space=\"preserve\">{Esc(Convert.ToString(value, inv) ?? string.Empty)}</t></is></c>");
                        break;
                }
            }
            x.Append("</row>");
        }
        x.Append("</sheetData></worksheet>");
        return x.ToString();
    }

    private static void AddEntry(ZipArchive zip, string path, string xml)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = new UTF8Encoding(false).GetBytes(xml);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string ColumnName(int index)
    {
        var s = string.Empty;
        while (index > 0)
        {
            index--;
            s = (char)('A' + index % 26) + s;
            index /= 26;
        }
        return s;
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
