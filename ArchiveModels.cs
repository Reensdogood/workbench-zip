namespace MyZip;

internal enum ArchiveEngine
{
    SharpCompress,
    EstsoftUnEgg
}

internal sealed record ArchiveItem(
    int Index,
    string Name,
    long Size,
    long PackedSize,
    DateTime? Modified,
    bool IsDirectory,
    bool IsEncrypted);

internal sealed record ArchiveSnapshot(
    string Path,
    ArchiveEngine Engine,
    IReadOnlyList<ArchiveItem> Items,
    bool IsSolid,
    string? Format);
