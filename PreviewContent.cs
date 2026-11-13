using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MyZip;

internal static partial class PreviewContent
{
    private const long MaxPreviewBytes = 8L * 1024 * 1024 * 1024;

    public static bool CanPreview(string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tif" or ".tiff" or ".txt" or ".md" or ".csv" or ".tsv" or ".log" or ".json" or ".xml" or ".ini" or ".yaml" or ".yml" or ".html" or ".htm" or ".rtf" or ".pdf" or ".docx" or ".hwpx" or ".xlsx" or ".pptx" or ".mp4" or ".mov" or ".m4v" or ".wmv" or ".avi" or ".mpg" or ".mpeg" or ".mkv" or ".webm" or ".wav" or ".mp3";
    }

    public static bool IsVideo(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() is ".mp4" or ".mov" or ".m4v" or ".wmv" or ".avi" or ".mpg" or ".mpeg" or ".mkv" or ".webm" or ".wav" or ".mp3";

    public static bool IsImage(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tif" or ".tiff";

    public static bool IsPdf(string fileName) => Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public static void EnsurePreviewSize(long size)
    {
        if (size < 0 || size > MaxPreviewBytes)
            throw new InvalidDataException("미리보기는 8GB 이하 파일만 지원합니다. 압축을 풀어 기본 앱에서 확인해 주세요.");
    }

    public static string ReadDocumentText(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".docx") return ReadDocx(path);
        if (extension == ".hwpx") return ReadHwpx(path);
        if (extension == ".xlsx") return ReadXlsx(path);
        if (extension == ".pptx") return ReadPptx(path);

        var info = new FileInfo(path);
        if (info.Length > 16 * 1024 * 1024)
            throw new InvalidDataException("텍스트 미리보기는 16MB 이하 파일만 지원합니다.");
        return TextEncodingResolver.ReadTextFile(path);
    }

    private static string ReadDocx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidDataException("Word 문서 본문을 찾지 못했습니다.");
        XDocument xml = LoadSafeXml(entry);
        return string.Join(Environment.NewLine, xml.Descendants().Where(e => e.Name.LocalName == "p")
            .Select(p => string.Concat(p.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value)))
            .Where(line => line.Length > 0));
    }

    private static string ReadHwpx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var sections = archive.Entries
            .Where(entry => Regex.IsMatch(entry.FullName, @"^Contents/section\d+\.xml$", RegexOptions.IgnoreCase))
            .OrderBy(entry => SectionNumber().Match(entry.FullName).Groups[1].Value, NaturalNumberComparer.Instance)
            .ToList();
        if (sections.Count == 0)
            throw new InvalidDataException("HWPX 문서 본문을 찾지 못했습니다. 파일이 손상됐거나 지원되지 않는 변형일 수 있습니다.");

        var output = new StringBuilder();
        foreach (var section in sections)
        {
            XDocument xml = LoadSafeXml(section);
            foreach (var paragraph in xml.Descendants().Where(element => element.Name.LocalName == "p"))
            {
                string text = string.Concat(paragraph.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value));
                if (text.Length > 0) output.AppendLine(text);
            }
            output.AppendLine();
        }
        return output.ToString();
    }

    private static string ReadXlsx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var sharedStrings = new List<string>();
        ZipArchiveEntry? sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedEntry is not null)
        {
            XDocument sharedXml = LoadSafeXml(sharedEntry);
            sharedStrings.AddRange(sharedXml.Descendants().Where(e => e.Name.LocalName == "si")
                .Select(si => string.Concat(si.Descendants().Where(e => e.Name.LocalName == "t").Select(t => t.Value))));
        }

        var sheetEntry = archive.Entries.Where(e => Regex.IsMatch(e.FullName, @"^xl/worksheets/sheet\d+\.xml$", RegexOptions.IgnoreCase))
            .OrderBy(e => SheetNumber().Match(e.FullName).Groups[1].Value, NaturalNumberComparer.Instance).FirstOrDefault();
        if (sheetEntry is null) throw new InvalidDataException("Excel 시트를 찾지 못했습니다.");
        XDocument sheet = LoadSafeXml(sheetEntry);
        return string.Join(Environment.NewLine, sheet.Descendants().Where(e => e.Name.LocalName == "row").Select(row =>
            string.Join("\t", row.Elements().Where(e => e.Name.LocalName == "c").Select(cell =>
            {
                string? value = cell.Elements().FirstOrDefault(e => e.Name.LocalName == "v")?.Value;
                if (cell.Attribute("t")?.Value == "s" && int.TryParse(value, out int index) && index >= 0 && index < sharedStrings.Count)
                    return sharedStrings[index];
                if (cell.Attribute("t")?.Value == "inlineStr")
                    return string.Concat(cell.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value));
                return value ?? string.Empty;
            }))));
    }

    private static string ReadPptx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var slides = archive.Entries.Where(e => Regex.IsMatch(e.FullName, @"^ppt/slides/slide\d+\.xml$", RegexOptions.IgnoreCase))
            .OrderBy(e => SlideNumber().Match(e.FullName).Groups[1].Value, NaturalNumberComparer.Instance).ToList();
        var output = new StringBuilder();
        for (int i = 0; i < slides.Count; i++)
        {
            XDocument slide = LoadSafeXml(slides[i]);
            string text = string.Join(Environment.NewLine, slide.Descendants().Where(e => e.Name.LocalName == "p")
                .Select(p => string.Concat(p.Descendants().Where(e => e.Name.LocalName == "t").Select(t => t.Value)))
                .Where(line => line.Length > 0));
            if (text.Length > 0) output.AppendLine($"슬라이드 {i + 1}").AppendLine(text).AppendLine();
        }
        return output.ToString();
    }

    private static XDocument LoadSafeXml(ZipArchiveEntry entry)
    {
        if (entry.Length > 16 * 1024 * 1024)
            throw new InvalidDataException("문서 미리보기 데이터가 16MB 제한을 초과했습니다.");
        using var stream = entry.Open();
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings
        {
            DtdProcessing = System.Xml.DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 16 * 1024 * 1024
        });
        return XDocument.Load(reader);
    }

    [GeneratedRegex(@"(?:sheet|slide)(\d+)\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex SheetNumber();

    [GeneratedRegex(@"(?:sheet|slide)(\d+)\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex SlideNumber();

    [GeneratedRegex(@"section(\d+)\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex SectionNumber();

    private sealed class NaturalNumberComparer : IComparer<string>
    {
        public static NaturalNumberComparer Instance { get; } = new();
        public int Compare(string? x, string? y)
        {
            long.TryParse(x, out long left);
            long.TryParse(y, out long right);
            return left.CompareTo(right);
        }
    }
}
