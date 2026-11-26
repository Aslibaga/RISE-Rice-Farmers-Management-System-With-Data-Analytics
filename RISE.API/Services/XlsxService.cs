using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace RISE.API.Services;

/// <summary>
/// Minimal OOXML spreadsheet reader/writer built on the .NET base class library.
/// No third-party package is required, and no network access is needed at build time.
/// </summary>
public static class XlsxService
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Relationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";

    public enum CellKind { Text, Number, Date, Integer }

    public sealed class Column
    {
        public string Header { get; init; } = "";
        public CellKind Kind { get; init; } = CellKind.Text;
        public double Width { get; init; } = 18;
        public bool ReadOnly { get; init; }
    }

    public sealed class Sheet
    {
        public string Name { get; init; } = "Sheet";
        public IReadOnlyList<Column> Columns { get; init; } = Array.Empty<Column>();
        public IReadOnlyList<object?[]> Rows { get; init; } = Array.Empty<object?[]>();
        public string? Summary { get; init; }
    }

    public static byte[] Write(IReadOnlyList<Sheet> sheets, string title, string? subtitle = null)
    {
        if (sheets.Count == 0) throw new ArgumentException("At least one worksheet is required.", nameof(sheets));

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypes(sheets.Count));
            WriteEntry(archive, "_rels/.rels", RootRelationships());
            WriteEntry(archive, "docProps/core.xml", CoreProperties(title, subtitle));
            WriteEntry(archive, "docProps/app.xml", AppProperties(sheets));
            WriteEntry(archive, "xl/workbook.xml", Workbook(sheets));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships(sheets.Count));
            WriteEntry(archive, "xl/styles.xml", Styles());
            for (var index = 0; index < sheets.Count; index++)
                WriteEntry(archive, $"xl/worksheets/sheet{index + 1}.xml", Worksheet(sheets[index]));
        }
        return stream.ToArray();
    }

    public static IReadOnlyList<Dictionary<string, string>> Read(Stream stream, string? sheetName = null)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = ReadSharedStrings(archive);
        var targets = ReadSheetTargets(archive);
        var target = sheetName != null && targets.TryGetValue(sheetName, out var named)
            ? named
            : targets.Values.FirstOrDefault();
        if (target is null) return Array.Empty<Dictionary<string, string>>();

        var entry = archive.GetEntry(target) ?? throw new InvalidDataException("The workbook sheet could not be found.");
        using var sheetStream = entry.Open();
        var document = XDocument.Load(sheetStream);
        var rows = document.Descendants(XName.Get("row", Main)).ToList();
        if (rows.Count == 0) return Array.Empty<Dictionary<string, string>>();

        var headers = ReadRow(rows[0], sharedStrings).ToList();
        var result = new List<Dictionary<string, string>>();
        foreach (var row in rows.Skip(1))
        {
            var values = ReadRow(row, sharedStrings).ToList();
            if (values.All(value => string.IsNullOrWhiteSpace(value))) continue;
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Count; index++)
                record[headers[index]] = index < values.Count ? values[index] : "";
            result.Add(record);
        }
        return result;
    }

    public static IReadOnlyList<string> SheetNames(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        return ReadSheetTargets(archive).Keys.ToList();
    }

    private static IEnumerable<string> ReadRow(XElement row, IReadOnlyList<string> sharedStrings)
    {
        foreach (var cell in row.Elements(XName.Get("c", Main)))
        {
            var type = (string?)cell.Attribute("t") ?? "";
            var valueElement = cell.Element(XName.Get("v", Main));
            var inline = cell.Element(XName.Get("is", Main));
            if (type == "inlineStr" && inline is not null)
            {
                yield return string.Concat(inline.Descendants(XName.Get("t", Main)).Select(node => node.Value));
                continue;
            }
            var raw = valueElement?.Value ?? "";
            if (type == "s" && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex))
                yield return sharedIndex >= 0 && sharedIndex < sharedStrings.Count ? sharedStrings[sharedIndex] : "";
            else if (type == "b")
                yield return raw == "1" ? "TRUE" : "FALSE";
            else
                yield return raw;
        }
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return Array.Empty<string>();
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document.Descendants(XName.Get("si", Main))
            .Select(node => string.Concat(node.Descendants(XName.Get("t", Main)).Select(text => text.Value)))
            .ToList();
    }

    private static Dictionary<string, string> ReadSheetTargets(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml");
        var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (workbookEntry is null || relsEntry is null) return new Dictionary<string, string>();

        using var workbookStream = workbookEntry.Open();
        using var relsStream = relsEntry.Open();
        var workbook = XDocument.Load(workbookStream);
        var rels = XDocument.Load(relsStream);
        var map = rels.Descendants(XName.Get("Relationship", Relationships))
            .Where(node => (string?)node.Attribute("Type") == OfficeRelationships + "/worksheet")
            .ToDictionary(
                node => (string)node.Attribute("Id")!,
                node =>
                {
                    var target = (string)node.Attribute("Target")!;
                    return target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? target : "xl/" + target;
                });

        var result = new Dictionary<string, string>();
        foreach (var sheet in workbook.Descendants(XName.Get("sheet", Main)))
        {
            var id = (string?)sheet.Attribute(XName.Get("id", OfficeRelationships));
            if (id is not null && map.TryGetValue(id, out var target))
                result[(string?)sheet.Attribute("name") ?? "Sheet"] = target;
        }
        return result;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Xml(XElement element) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + element.ToString(SaveOptions.DisableFormatting);

    private static string ContentTypes(int count)
    {
        var overrides = new List<XElement>();
        for (var index = 1; index <= count; index++)
            overrides.Add(new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"),
                new XAttribute("PartName", $"/xl/worksheets/sheet{index}.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
        overrides.Add(new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"),
            new XAttribute("PartName", "/xl/workbook.xml"),
            new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")));
        overrides.Add(new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"),
            new XAttribute("PartName", "/xl/styles.xml"),
            new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")));
        overrides.Add(new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"),
            new XAttribute("PartName", "/docProps/core.xml"),
            new XAttribute("ContentType", "application/vnd.openxmlformats-package.core-properties+xml")));
        overrides.Add(new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"),
            new XAttribute("PartName", "/docProps/app.xml"),
            new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.extended-properties+xml")));
        return Xml(new XElement(XName.Get("Types", "http://schemas.openxmlformats.org/package/2006/content-types"),
            new XElement(XName.Get("Default", "http://schemas.openxmlformats.org/package/2006/content-types"),
                new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(XName.Get("Default", "http://schemas.openxmlformats.org/package/2006/content-types"),
                new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
            overrides));
    }

    private static string RootRelationships() => Xml(new XElement(XName.Get("Relationships", Relationships),
        new XElement(XName.Get("Relationship", Relationships), new XAttribute("Id", "rId1"),
            new XAttribute("Type", OfficeRelationships + "/officeDocument"), new XAttribute("Target", "xl/workbook.xml")),
        new XElement(XName.Get("Relationship", Relationships), new XAttribute("Id", "rId2"),
            new XAttribute("Type", PackageRelationships), new XAttribute("Target", "docProps/core.xml")),
        new XElement(XName.Get("Relationship", Relationships), new XAttribute("Id", "rId3"),
            new XAttribute("Type", OfficeRelationships + "/extended-properties"), new XAttribute("Target", "docProps/app.xml"))));

    private static string Workbook(IReadOnlyList<Sheet> sheets)
    {
        var nodes = sheets.Select((sheet, index) => new XElement(XName.Get("sheet", Main),
            new XAttribute("name", SafeSheetName(sheet.Name, index)),
            new XAttribute("sheetId", index + 1),
            new XAttribute(XName.Get("id", OfficeRelationships), $"rId{index + 1}")));
        return Xml(new XElement(XName.Get("workbook", Main),
            new XAttribute(XNamespace.Xmlns + "r", OfficeRelationships),
            new XElement(XName.Get("bookViews", Main), new XElement(XName.Get("workbookView", Main))),
            new XElement(XName.Get("sheets", Main), nodes)));
    }

    private static string WorkbookRelationships(int count)
    {
        var nodes = Enumerable.Range(1, count).Select(index => new XElement(XName.Get("Relationship", Relationships),
            new XAttribute("Id", $"rId{index}"),
            new XAttribute("Type", OfficeRelationships + "/worksheet"),
            new XAttribute("Target", $"worksheets/sheet{index}.xml"))).ToList();
        nodes.Add(new XElement(XName.Get("Relationship", Relationships), new XAttribute("Id", $"rId{count + 1}"),
            new XAttribute("Type", OfficeRelationships + "/styles"), new XAttribute("Target", "styles.xml")));
        return Xml(new XElement(XName.Get("Relationships", Relationships), nodes));
    }

    private static string CoreProperties(string title, string? subtitle)
    {
        XNamespace cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
        XNamespace dc = "http://purl.org/dc/elements/1.1/";
        XNamespace dcterms = "http://purl.org/dc/terms/";
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        return Xml(new XElement(cp + "coreProperties",
            new XAttribute(XNamespace.Xmlns + "cp", cp),
            new XAttribute(XNamespace.Xmlns + "dc", dc),
            new XAttribute(XNamespace.Xmlns + "dcterms", dcterms),
            new XAttribute(XNamespace.Xmlns + "xsi", xsi),
            new XElement(dc + "title", title),
            new XElement(dc + "subject", subtitle ?? "RISE system-generated report"),
            new XElement(dc + "creator", "RISE Rice Farmer Assistance System"),
            new XElement(dcterms + "created",
                new XAttribute(xsi + "type", "dcterms:W3CDTF"),
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))));
    }

    private static string AppProperties(IReadOnlyList<Sheet> sheets)
    {
        XNamespace extended = "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties";
        XNamespace vt = "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes";
        return Xml(new XElement(extended + "Properties",
            new XAttribute(XNamespace.Xmlns + "vt", vt),
            new XElement(extended + "Application", "RISE"),
            new XElement(extended + "TitlesOfParts",
                new XElement(vt + "vector",
                    new XAttribute("size", sheets.Count), new XAttribute("baseType", "lpstr"),
                    sheets.Select(sheet => new XElement(vt + "lpstr", sheet.Name))))));
    }

    private static string Styles()
    {
        var fonts = new XElement(XName.Get("fonts", Main), new XAttribute("count", 2),
            Font(false, "000000"), Font(true, "FFFFFF"));
        var fills = new XElement(XName.Get("fills", Main), new XAttribute("count", 4),
            new XElement(XName.Get("fill", Main), new XElement(XName.Get("patternFill", Main), new XAttribute("patternType", "none"))),
            new XElement(XName.Get("fill", Main), new XElement(XName.Get("patternFill", Main), new XAttribute("patternType", "gray125"))),
            SolidFill("1B5E3F"), SolidFill("EEF5EF"));
        var borders = new XElement(XName.Get("borders", Main), new XAttribute("count", 2),
            new XElement(XName.Get("border", Main), new XElement(XName.Get("left", Main)), new XElement(XName.Get("right", Main)),
                new XElement(XName.Get("top", Main)), new XElement(XName.Get("bottom", Main)), new XElement(XName.Get("diagonal", Main))),
            new XElement(XName.Get("border", Main),
                new XElement(XName.Get("left", Main), new XAttribute("style", "thin"), Color("B7C9B8")),
                new XElement(XName.Get("right", Main), new XAttribute("style", "thin"), Color("B7C9B8")),
                new XElement(XName.Get("top", Main), new XAttribute("style", "thin"), Color("B7C9B8")),
                new XElement(XName.Get("bottom", Main), new XAttribute("style", "thin"), Color("B7C9B8")),
                new XElement(XName.Get("diagonal", Main))));
        var formats = new XElement(XName.Get("numFmts", Main), new XAttribute("count", 3),
            new XElement(XName.Get("numFmt", Main), new XAttribute("numFmtId", 164), new XAttribute("formatCode", "yyyy-mm-dd")),
            new XElement(XName.Get("numFmt", Main), new XAttribute("numFmtId", 165), new XAttribute("formatCode", "#,##0.00")),
            new XElement(XName.Get("numFmt", Main), new XAttribute("numFmtId", 166), new XAttribute("formatCode", "0")));

        // 0 default, 1 header, 2 normal, 3 alternate, 4 date, 5 alternate date, 6 number, 7 alternate number, 8 summary, 9 integer, 10 alternate integer
        var xfs = new XElement(XName.Get("cellXfs", Main), new XAttribute("count", 11),
            Xf(0, 0, 0, 0, null),
            Xf(1, 1, 2, 1, "center"),
            Xf(0, 0, 0, 1, null),
            Xf(0, 0, 3, 1, null),
            Xf(164, 0, 0, 1, null),
            Xf(164, 0, 3, 1, null),
            Xf(165, 0, 0, 1, null),
            Xf(165, 0, 3, 1, null),
            Xf(0, 1, 2, 1, "left"),
            Xf(166, 0, 0, 1, "center"),
            Xf(166, 0, 3, 1, "center"));
        return Xml(new XElement(XName.Get("styleSheet", Main), formats, fonts, fills, borders,
            new XElement(XName.Get("cellStyleXfs", Main), new XAttribute("count", 1), Xf(0, 0, 0, 0, null)), xfs,
            new XElement(XName.Get("cellStyles", Main), new XAttribute("count", 1),
                new XElement(XName.Get("cellStyle", Main), new XAttribute("name", "Normal"),
                    new XAttribute("xfId", 0), new XAttribute("builtinId", 0)))));
    }

    private static XElement Font(bool bold, string color) => new(XName.Get("font", Main),
        new XElement(XName.Get("sz", Main), new XAttribute("val", 11)),
        new XElement(XName.Get("color", Main), new XAttribute("rgb", "FF" + color)),
        new XElement(XName.Get("name", Main), new XAttribute("val", "Calibri")),
        bold ? new XElement(XName.Get("b", Main)) : null);

    private static XElement SolidFill(string color) => new(XName.Get("fill", Main),
        new XElement(XName.Get("patternFill", Main), new XAttribute("patternType", "solid"),
            new XElement(XName.Get("fgColor", Main), new XAttribute("rgb", "FF" + color)),
            new XElement(XName.Get("bgColor", Main), new XAttribute("indexed", 64))));

    private static XElement Color(string rgb) => new(XName.Get("color", Main), new XAttribute("rgb", "FF" + rgb));

    private static XElement Xf(int numberFormat, int font, int fill, int border, string? alignment)
    {
        var node = new XElement(XName.Get("xf", Main),
            new XAttribute("numFmtId", numberFormat), new XAttribute("fontId", font),
            new XAttribute("fillId", fill), new XAttribute("borderId", border), new XAttribute("xfId", 0),
            new XAttribute("applyFont", 1), new XAttribute("applyFill", 1), new XAttribute("applyBorder", 1),
            new XAttribute("applyAlignment", 1), new XAttribute("applyNumberFormat", 1));
        if (alignment is not null)
            node.Add(new XElement(XName.Get("alignment", Main), new XAttribute("horizontal", alignment), new XAttribute("vertical", "center")));
        return node;
    }

    private static string Worksheet(Sheet sheet)
    {
        var columns = new XElement(XName.Get("cols", Main));
        for (var index = 0; index < sheet.Columns.Count; index++)
            columns.Add(new XElement(XName.Get("col", Main),
                new XAttribute("min", index + 1), new XAttribute("max", index + 1),
                new XAttribute("width", sheet.Columns[index].Width.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("customWidth", 1)));

        var data = new XElement(XName.Get("sheetData", Main));
        data.Add(Row(1, true, sheet.Columns.Select(column => (object)column.Header).ToArray(), sheet.Columns.Select(_ => CellKind.Text).ToArray(), null));

        var rowIndex = 2;
        foreach (var values in sheet.Rows)
        {
            data.Add(Row(rowIndex++, false, values, sheet.Columns.Select(column => column.Kind).ToArray(), null));
        }
        if (!string.IsNullOrWhiteSpace(sheet.Summary))
            data.Add(Row(rowIndex, false, new object[] { sheet.Summary }, new[] { CellKind.Text }, 8));

        var lastColumn = ColumnName(Math.Max(1, sheet.Columns.Count));
        var lastRow = sheet.Rows.Count + 1 + (string.IsNullOrWhiteSpace(sheet.Summary) ? 0 : 1);
        var worksheet = new XElement(XName.Get("worksheet", Main),
            new XElement(XName.Get("dimension", Main), new XAttribute("ref", $"A1:{lastColumn}{Math.Max(1, lastRow)}")),
            new XElement(XName.Get("sheetViews", Main),
                new XElement(XName.Get("sheetView", Main), new XAttribute("workbookViewId", 0),
                    new XElement(XName.Get("pane", Main), new XAttribute("ySplit", 1),
                        new XAttribute("topLeftCell", "A2"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(XName.Get("sheetFormatPr", Main), new XAttribute("defaultRowHeight", 15)),
            columns, data,
            new XElement(XName.Get("autoFilter", Main), new XAttribute("ref", $"A1:{lastColumn}{Math.Max(1, sheet.Rows.Count + 1)}")),
            new XElement(XName.Get("pageMargins", Main),
                new XAttribute("left", 0.7), new XAttribute("right", 0.7),
                new XAttribute("top", 0.75), new XAttribute("bottom", 0.75),
                new XAttribute("header", 0.3), new XAttribute("footer", 0.3)));
        return Xml(worksheet);
    }

    private static XElement Row(int index, bool header, IReadOnlyList<object?> values, IReadOnlyList<CellKind> kinds, int? forcedStyle)
    {
        var row = new XElement(XName.Get("row", Main), new XAttribute("r", index));
        var alternate = index % 2 == 0;
        for (var column = 0; column < values.Count; column++)
        {
            var kind = column < kinds.Count ? kinds[column] : CellKind.Text;
            var style = header ? 1 : forcedStyle ?? kind switch
            {
                CellKind.Date => alternate ? 5 : 4,
                CellKind.Number => alternate ? 7 : 6,
                CellKind.Integer => alternate ? 10 : 9,
                _ => alternate ? 3 : 2
            };
            var cell = new XElement(XName.Get("c", Main), new XAttribute("r", $"{ColumnName(column + 1)}{index}"), new XAttribute("s", style));
            var value = values[column];
            if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                row.Add(cell);
                continue;
            }
            if ((kind == CellKind.Number || kind == CellKind.Integer) && TryNumber(value, out var number))
                cell.Add(new XElement(XName.Get("v", Main),
                    number.ToString(kind == CellKind.Integer ? "0" : "0.########", CultureInfo.InvariantCulture)));
            else if (kind == CellKind.Date && TryDate(value, out var date))
                cell.Add(new XElement(XName.Get("v", Main), (date - new DateTime(1899, 12, 30)).TotalDays.ToString("0.########", CultureInfo.InvariantCulture)));
            else
                cell.Add(new XAttribute("t", "inlineStr"),
                    new XElement(XName.Get("is", Main), new XElement(XName.Get("t", Main),
                        new XAttribute(XNamespace.Xml + "space", "preserve"), value.ToString())));
            row.Add(cell);
        }
        return row;
    }

    private static bool TryNumber(object? value, out double number)
    {
        number = 0;
        if (value is null) return false;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        text = text.Replace(",", "");
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out number);
    }

    private static bool TryDate(object? value, out DateTime date)
    {
        date = default;
        if (value is DateTime dateTime) { date = dateTime; return true; }
        if (value is DateOnly dateOnly) { date = dateOnly.ToDateTime(TimeOnly.MinValue); return true; }
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static string ColumnName(int index)
    {
        var result = "";
        while (index > 0)
        {
            index--;
            result = (char)('A' + index % 26) + result;
            index /= 26;
        }
        return result;
    }

    private static string SafeSheetName(string name, int index)
    {
        var invalid = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var clean = new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        if (clean.Length > 31) clean = clean[..31];
        return string.IsNullOrWhiteSpace(clean) ? $"Sheet{index + 1}" : clean;
    }
}
