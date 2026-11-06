using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MyZip;

internal sealed record GitHubUpdateInfo(Version Version, string Tag, string ReleaseUrl, string Notes, Uri InstallerUri, Uri ChecksumUri);

internal static partial class GitHubUpdateService
{
    private const string LatestReleaseApi = "https://api.github.com/repos/Reensdogood/workbench-zip/releases/latest";
    private const string InstallerName = "myzip-setup-win-x64.exe";
    private const string ChecksumName = InstallerName + ".sha256";
    private const long MaximumInstallerBytes = 160L * 1024 * 1024;
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("myzip-updater", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    public static async Task<GitHubUpdateInfo?> CheckAsync(Version currentVersion, CancellationToken cancellationToken)
    {
        using var response = await Client.GetAsync(LatestReleaseApi, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        string tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var latestVersion) || latestVersion <= currentVersion) return null;

        Uri? installer = null;
        Uri? checksum = null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? string.Empty;
            string url = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var assetUri) || !IsAllowedReleaseAsset(assetUri)) continue;
            if (name.Equals(InstallerName, StringComparison.Ordinal)) installer = assetUri;
            else if (name.Equals(ChecksumName, StringComparison.Ordinal)) checksum = assetUri;
        }
        if (installer is null || checksum is null)
            throw new InvalidDataException("GitHub Release에 설치 파일 또는 SHA-256 체크섬이 없습니다.");

        string notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? string.Empty : string.Empty;
        string releaseUrl = root.TryGetProperty("html_url", out var html) ? html.GetString() ?? string.Empty : string.Empty;
        return new GitHubUpdateInfo(latestVersion, tag, releaseUrl, notes, installer, checksum);
    }

    public static async Task<string> DownloadAndVerifyInstallerAsync(GitHubUpdateInfo update, CancellationToken cancellationToken)
    {
        byte[] installerBytes = await DownloadBoundedAsync(update.InstallerUri, MaximumInstallerBytes, cancellationToken);
        if (installerBytes.LongLength is 0 or > MaximumInstallerBytes)
            throw new InvalidDataException("GitHub에서 받은 설치 파일 크기가 올바르지 않습니다.");

        string checksumText = await Client.GetStringAsync(update.ChecksumUri, cancellationToken);
        var match = ChecksumPattern().Match(checksumText.Trim());
        if (!match.Success) throw new InvalidDataException("설치 파일의 SHA-256 체크섬을 읽지 못했습니다.");
        byte[] expected = Convert.FromHexString(match.Groups[1].Value);
        byte[] actual = SHA256.HashData(installerBytes);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            throw new CryptographicException("설치 파일의 SHA-256 체크섬이 일치하지 않습니다. 업데이트를 실행하지 않았습니다.");

        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "myzip", "updates");
        Directory.CreateDirectory(folder);
        string destination = Path.Combine(folder, $"myzip-setup-{update.Version}.exe");
        await File.WriteAllBytesAsync(destination, installerBytes, cancellationToken);
        return destination;
    }

    private static bool IsAllowedReleaseAsset(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.StartsWith("/Reensdogood/workbench-zip/releases/download/", StringComparison.OrdinalIgnoreCase);

    private static async Task<byte[]> DownloadBoundedAsync(Uri uri, long maximumBytes, CancellationToken cancellationToken)
    {
        if (!IsAllowedReleaseAsset(uri)) throw new InvalidDataException("업데이트 파일 주소가 GitHub Release 주소가 아닙니다.");
        using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long contentLength && (contentLength <= 0 || contentLength > maximumBytes))
            throw new InvalidDataException("업데이트 파일 크기가 허용 범위를 벗어났습니다.");

        await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        byte[] buffer = new byte[64 * 1024];
        while (true)
        {
            int read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            if (output.Length + read > maximumBytes) throw new InvalidDataException("업데이트 파일 크기가 허용 범위를 초과했습니다.");
            output.Write(buffer, 0, read);
        }
        if (output.Length == 0) throw new InvalidDataException("GitHub에서 빈 설치 파일을 받았습니다.");
        return output.ToArray();
    }

    [GeneratedRegex(@"\A\s*([0-9a-fA-F]{64})(?:\s+\*?[^\r\n]+)?\s*\z", RegexOptions.CultureInvariant)]
    private static partial Regex ChecksumPattern();
}
