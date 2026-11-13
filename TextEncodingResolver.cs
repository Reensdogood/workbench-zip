using System.IO;
using System.Text;
using SharpCompress.Common;

namespace MyZip;

/// <summary>
/// Decodes archive names and previewable text without changing the source bytes.
/// UTF-8 remains preferred; common legacy Windows and DOS encodings are tried
/// when the bytes are not valid UTF-8 or the decoded text looks like mojibake.
/// </summary>
internal static class TextEncodingResolver
{
    private static readonly Encoding Utf8Strict = new UTF8Encoding(false, true);
    private static readonly int[] CandidateCodePages = [
        949, 932, 936, 950, 1251, 1252, 1250, 1253, 1254, 1255, 1256, 1257,
        874, 866, 850, 437
    ];

    private static readonly Encoding[] CandidateEncodings;

    static TextEncodingResolver()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        CandidateEncodings = CandidateCodePages.Select(CreateStrictEncoding).ToArray();
    }

    public static ArchiveEncoding CreateArchiveEncoding() => new()
    {
        // ZIP's traditional default is CP437. CustomDecoder applies automatic
        // detection per entry while UTF-8-flagged names are kept strictly UTF-8.
        Default = CreateStrictEncoding(437),
        UTF8 = Utf8Strict,
        CustomDecoder = DecodeArchiveName
    };

    public static string ReadTextFile(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (TryDecodeBom(bytes, out string? bomDecoded)) return bomDecoded!;

        string? utf8 = TryDecode(Utf8Strict, bytes, 0, bytes.Length);
        if (utf8 is not null)
        {
            if (LooksLikeMojibake(utf8))
            {
                string? repaired = TryRepairMojibake(utf8);
                if (repaired is not null && Score(repaired, 65001) > Score(utf8, 65001) + 2)
                    return repaired;
            }
            return utf8;
        }

        return DecodeBest(bytes, 0, bytes.Length, preferUtf8: false);
    }

    private static string DecodeArchiveName(byte[] bytes, int offset, int count, EncodingType type)
    {
        if (count <= 0) return string.Empty;

        // A ZIP UTF-8 flag is authoritative. If malformed, try recovery so a
        // mislabeled legacy archive can still display readable names.
        if (type == EncodingType.UTF8)
        {
            string? utf8 = TryDecode(Utf8Strict, bytes, offset, count);
            if (utf8 is not null) return utf8;
        }

        return DecodeBest(bytes, offset, count, preferUtf8: type != EncodingType.UTF8);
    }

    private static string DecodeBest(byte[] bytes, int offset, int count, bool preferUtf8)
    {
        string? best = null;
        int bestScore = int.MinValue;

        string? utf8 = TryDecode(Utf8Strict, bytes, offset, count);
        if (utf8 is not null)
        {
            best = utf8;
            bestScore = Score(utf8, 65001) + (preferUtf8 ? 4 : 0);
            if (!LooksLikeMojibake(utf8)) return utf8;
        }

        for (int i = 0; i < CandidateEncodings.Length; i++)
        {
            Encoding encoding = CandidateEncodings[i];
            string? candidate = TryDecode(encoding, bytes, offset, count);
            if (candidate is null) continue;

            int candidateScore = Score(candidate, encoding.CodePage);
            if (candidateScore > bestScore)
            {
                best = candidate;
                bestScore = candidateScore;
            }
        }

        if (best is not null && LooksLikeMojibake(best))
        {
            string? repaired = TryRepairMojibake(best);
            if (repaired is not null && Score(repaired, 65001) > Score(best, 65001) + 2)
                return repaired;
        }

        return best ?? Encoding.GetEncoding(437).GetString(bytes, offset, count);
    }

    private static string? TryDecode(Encoding encoding, byte[] bytes, int offset, int count)
    {
        try { return encoding.GetString(bytes, offset, count); }
        catch (DecoderFallbackException) { return null; }
    }

    private static Encoding CreateStrictEncoding(int codePage) => Encoding.GetEncoding(
        codePage,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);

    private static bool TryDecodeBom(byte[] bytes, out string? decoded)
    {
        decoded = null;
        if (bytes.AsSpan().StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF }))
            decoded = new UTF32Encoding(true, true, true).GetString(bytes, 4, bytes.Length - 4);
        else if (bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }))
            decoded = new UTF32Encoding(false, true, true).GetString(bytes, 4, bytes.Length - 4);
        else if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
            decoded = Utf8Strict.GetString(bytes, 3, bytes.Length - 3);
        else if (bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
            decoded = new UnicodeEncoding(true, true, true).GetString(bytes, 2, bytes.Length - 2);
        else if (bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
            decoded = new UnicodeEncoding(false, true, true).GetString(bytes, 2, bytes.Length - 2);
        return decoded is not null;
    }

    private static bool LooksLikeMojibake(string text)
    {
        if (text.Contains('\uFFFD')) return true;
        if (text.Contains("Ã©", StringComparison.Ordinal) || text.Contains("Ã±", StringComparison.Ordinal)
            || text.Contains("Ã¼", StringComparison.Ordinal) || text.Contains("Ã¶", StringComparison.Ordinal)
            || text.Contains("Ã¤", StringComparison.Ordinal) || text.Contains("Ã£", StringComparison.Ordinal)
            || text.Contains("â€", StringComparison.Ordinal) || text.Contains("ðŸ", StringComparison.Ordinal)
            || text.Contains("ï»¿", StringComparison.Ordinal) || text.Contains("ï¿½", StringComparison.Ordinal))
            return true;

        for (int i = 0; i + 1 < text.Length; i++)
        {
            if ((text[i] is 'ì' or 'ë' or 'ê' or 'Ã' or 'Â')
                && (text[i + 1] > 0x7F || text[i + 1] is ' ' or '\u0080' or '\u0090'))
                return true;
        }
        return false;
    }

    private static string? TryRepairMojibake(string text)
    {
        if (!LooksLikeMojibake(text)) return null;

        foreach (int codePage in new[] { 1252, 1251, 1250, 437, 850 })
        {
            try
            {
                byte[] intermediate = CreateStrictEncoding(codePage).GetBytes(text);
                string repaired = Utf8Strict.GetString(intermediate);
                if (!LooksLikeMojibake(repaired)) return repaired;
            }
            catch (EncoderFallbackException) { }
            catch (DecoderFallbackException) { }
        }
        return null;
    }

    private static int Score(string text, int codePage)
    {
        if (text.Length == 0) return 0;
        int score = codePage switch
        {
            65001 => 4,
            949 => 1,
            932 => 1,
            936 => 2,
            950 => 1,
            1251 or 1253 or 1254 or 1255 or 1256 or 1257 or 874 => 1,
            1252 => 1,
            _ => 0
        };

        foreach (char c in text)
        {
            if (c == '\uFFFD') { score -= 24; continue; }
            if (char.IsControl(c))
            {
                score -= c is '\r' or '\n' or '\t' ? 1 : 12;
                continue;
            }

            if (c is >= '\uAC00' and <= '\uD7AF' or >= '\u1100' and <= '\u11FF') score += 8; // Hangul
            else if (c is >= '\u3040' and <= '\u30FF') score += 7; // Hiragana and Katakana
            else if (c is >= '\u3400' and <= '\u9FFF') score += 5; // CJK ideographs
            else if (c is >= '\u0400' and <= '\u052F') score += 6; // Cyrillic
            else if (c is >= '\u0370' and <= '\u03FF') score += 5; // Greek
            else if (c is >= '\u0600' and <= '\u06FF') score += 5; // Arabic
            else if (char.IsLetter(c)) score += c <= 0x7F ? 2 : 2;
            else if (char.IsDigit(c)) score++;
            else if (char.IsWhiteSpace(c)) score++;
            else if (c is '_' or '-' or '.' or '/' or '\\' or '(' or ')' or '[' or ']' or '{' or '}' or '@' or '+') score++;
            else if (char.IsPunctuation(c) || char.IsSymbol(c)) score--;
        }

        if (LooksLikeMojibake(text)) score -= 16;
        return score;
    }
}
