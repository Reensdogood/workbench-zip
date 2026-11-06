using System.IO;
using System.Runtime.InteropServices;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.SevenZip;
using SharpCompress.Writers.Zip;

namespace MyZip;

internal static class ArchiveEngineService
{
    private const uint ErrorSuccess = 1;
    private const uint ErrorSkip = 2;
    private const uint ErrorCancel = 3;
    private const uint ErrorWrongPassword = 9;
    private const uint EventExtract = 1;
    private const uint EventProgress = 2;
    private const uint EventQueryPassword = 7;
    private const uint EventError = 8;
    private const uint EventTest = 9;
    private const int MaxPath = 260;

    public static ArchiveSnapshot Open(string path, string? password)
    {
        path = Path.GetFullPath(path);
        if (HasAlzEggExtension(path))
        {
            EnsureUnEggModuleAvailable();
            using var native = new UnEggSession(path, password, null, null);
            return new ArchiveSnapshot(path, ArchiveEngine.EstsoftUnEgg, native.ReadItems(), false, Path.GetExtension(path).TrimStart('.').ToUpperInvariant());
        }

        using var archive = OpenSharpArchive(path, password);
        var items = archive.Entries.Select((entry, index) => new ArchiveItem(
            index,
            entry.Key ?? string.Empty,
            entry.Size,
            entry.CompressedSize,
            entry.LastModifiedTime,
            entry.IsDirectory,
            entry.IsEncrypted)).ToList();
        return new ArchiveSnapshot(path, ArchiveEngine.SharpCompress, items, archive.IsSolid, archive.Type.ToString());
    }

    public static Task<int> ExtractAsync(
        ArchiveSnapshot snapshot,
        IReadOnlyCollection<int> selectedIndices,
        string destination,
        string? password,
        IProgress<(int Done, int Total)>? progress,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        destination = Path.GetFullPath(destination);
        Directory.CreateDirectory(destination);
        if (snapshot.Engine == ArchiveEngine.EstsoftUnEgg)
        {
            EnsureUnEggModuleAvailable();
            using var native = new UnEggSession(snapshot.Path, password, destination, selectedIndices.ToHashSet());
            native.Extract(progress, cancellationToken);
            return selectedIndices.Count;
        }

        using var archive = OpenSharpArchive(snapshot.Path, password);
        var wanted = selectedIndices.ToHashSet();
        int total = wanted.Count;
        int done = 0;
        foreach (var pair in archive.Entries.Select((entry, index) => (entry, index)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!wanted.Contains(pair.index)) continue;
            var entry = pair.entry;
            if (!entry.IsDirectory)
            {
                if (!string.IsNullOrEmpty(entry.LinkTarget))
                    throw new InvalidDataException($"압축 파일 안의 링크 항목은 안전을 위해 건너뜁니다: {entry.Key}");
                string target = ResolveSafePath(destination, entry.Key ?? string.Empty);
                EnsureNotArchiveSource(snapshot, target);
                EnsureNoReparsePoint(destination, target);
                string? parent = Path.GetDirectoryName(target);
                if (parent is not null) Directory.CreateDirectory(parent);
                using Stream input = entry.OpenEntryStream();
                using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
                CopyWithCancellation(input, output, cancellationToken);
            }
            else
            {
                string target = ResolveSafePath(destination, entry.Key ?? string.Empty);
                EnsureNoReparsePoint(destination, target);
                Directory.CreateDirectory(target);
            }
            done++;
            progress?.Report((done, total));
        }
        return done;
    }, cancellationToken);

    public static Task<int> TestAsync(
        ArchiveSnapshot snapshot,
        string? password,
        IProgress<(int Done, int Total)>? progress,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        if (snapshot.Engine == ArchiveEngine.EstsoftUnEgg)
        {
            EnsureUnEggModuleAvailable();
            using var native = new UnEggSession(snapshot.Path, password, null, null);
            native.Test(progress, cancellationToken);
            return snapshot.Items.Count;
        }

        using var archive = OpenSharpArchive(snapshot.Path, password);
        var files = archive.Entries.Where(e => !e.IsDirectory).ToList();
        int done = 0;
        foreach (var entry in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using Stream stream = entry.OpenEntryStream();
            DrainWithCancellation(stream, cancellationToken);
            if (!entry.IsComplete) throw new InvalidDataException($"항목이 끝까지 검증되지 않았습니다: {entry.Key}");
            progress?.Report((++done, files.Count));
        }
        return done;
    }, cancellationToken);

    public static Task CreateAsync(
        IReadOnlyList<string> sourcePaths,
        string outputPath,
        ArchiveType format,
        int compressionLevel,
        IProgress<(int Done, int Total)>? progress,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        if (sourcePaths.Count == 0) throw new InvalidOperationException("압축할 파일이나 폴더를 선택하세요.");
        outputPath = Path.GetFullPath(outputPath);
        string outputFullPath = outputPath;
        if (sourcePaths.Any(p => string.Equals(Path.GetFullPath(p), outputFullPath, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("출력 압축 파일을 입력 항목으로 선택할 수 없습니다.");

        var files = new List<(string Source, string Entry, DateTime Modified)>();
        foreach (string input in sourcePaths.Select(Path.GetFullPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Directory.Exists(input))
            {
                string rootName = new DirectoryInfo(input).Name;
                foreach (string file in Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string relative = Path.GetRelativePath(input, file).Replace(Path.DirectorySeparatorChar, '/');
                    files.Add((file, $"{rootName}/{relative}", File.GetLastWriteTime(file)));
                }
            }
            else if (File.Exists(input))
                files.Add((input, Path.GetFileName(input), File.GetLastWriteTime(input)));
        }

        if (files.Count == 0) throw new InvalidOperationException("선택한 위치에서 압축할 파일을 찾지 못했습니다.");
        if (files.Any(f => string.Equals(Path.GetFullPath(f.Source), outputFullPath, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("출력 압축 파일을 자기 자신 안에 포함할 수 없습니다.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputFullPath)!);
        using Stream destination = new FileStream(outputFullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
        IWriter writer = format == ArchiveType.SevenZip
            ? WriterFactory.OpenWriter(destination, ArchiveType.SevenZip, new SevenZipWriterOptions(CompressionType.LZMA2) { CompressionLevel = compressionLevel, LeaveStreamOpen = true })
            : WriterFactory.OpenWriter(destination, ArchiveType.Zip, new ZipWriterOptions(CompressionType.Deflate) { CompressionLevel = compressionLevel, UseZip64 = true, LeaveStreamOpen = true });
        using (writer)
        {
            int done = 0;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using Stream input = new FileStream(file.Source, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
                writer.Write(file.Entry, input, file.Modified);
                progress?.Report((++done, files.Count));
            }
        }
        return files.Count;
    }, cancellationToken);

    public static Task<int> CreateFromWorkspaceAsync(
        string workspace,
        string outputPath,
        ArchiveType format,
        int compressionLevel,
        IProgress<(int Done, int Total)>? progress,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        workspace = Path.GetFullPath(workspace);
        outputPath = Path.GetFullPath(outputPath);
        string outputFullPath = outputPath;
        var files = Directory.EnumerateFiles(workspace, "*", SearchOption.AllDirectories)
            .Select(file => (Source: file, Entry: Path.GetRelativePath(workspace, file).Replace(Path.DirectorySeparatorChar, '/'), Modified: File.GetLastWriteTime(file)))
            .Where(file => !string.Equals(Path.GetFullPath(file.Source), outputFullPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (files.Count == 0) throw new InvalidOperationException("압축할 파일이 없습니다. 파일을 추가한 뒤 저장하세요.");
        Directory.CreateDirectory(Path.GetDirectoryName(outputFullPath)!);
        using Stream destination = new FileStream(outputFullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
        IWriter writer = format == ArchiveType.SevenZip
            ? WriterFactory.OpenWriter(destination, ArchiveType.SevenZip, new SevenZipWriterOptions(CompressionType.LZMA2) { CompressionLevel = compressionLevel, LeaveStreamOpen = true })
            : WriterFactory.OpenWriter(destination, ArchiveType.Zip, new ZipWriterOptions(CompressionType.Deflate) { CompressionLevel = compressionLevel, UseZip64 = true, LeaveStreamOpen = true });
        using (writer)
        {
            int done = 0;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using Stream input = new FileStream(file.Source, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
                writer.Write(file.Entry, input, file.Modified);
                progress?.Report((++done, files.Count));
            }
        }
        return files.Count;
    }, cancellationToken);

    public static bool IsAlzEgg(string path)
    {
        if (HasAlzEggExtension(path)) return true;
        try
        {
            using var session = new UnEggSession(path, null, null, null, validateOnly: true);
            return true;
        }
        catch { return false; }
    }

    public static string ResolveSafePath(string root, string archiveName)
    {
        if (string.IsNullOrWhiteSpace(archiveName)) throw new InvalidDataException("압축 항목의 파일명이 비어 있습니다.");
        string normalized = archiveName.Replace('\\', '/');
        if (normalized.StartsWith('/') || Path.IsPathRooted(normalized) || normalized.Contains(':'))
            throw new InvalidDataException($"압축 파일에 절대 경로 항목이 있습니다: {archiveName}");
        string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(part => part is "." or ".."))
            throw new InvalidDataException($"압축 파일에 안전하지 않은 경로 항목이 있습니다: {archiveName}");
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullTarget = Path.GetFullPath(Path.Combine(fullRoot, Path.Combine(parts)));
        string prefix = fullRoot + Path.DirectorySeparatorChar;
        if (!fullTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"압축 파일 경로가 저장 폴더 밖을 가리킵니다: {archiveName}");
        return fullTarget;
    }

    private static void EnsureNotArchiveSource(ArchiveSnapshot snapshot, string target)
    {
        if (string.Equals(Path.GetFullPath(target), Path.GetFullPath(snapshot.Path), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("압축 파일 자체를 덮어쓸 수 있는 항목은 풀 수 없습니다.");
    }

    private static bool HasAlzEggExtension(string path) => Path.GetExtension(path).Equals(".egg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".alz", StringComparison.OrdinalIgnoreCase);

    private static void EnsureUnEggModuleAvailable()
    {
        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "UnEGG64.dll")))
            throw new NotSupportedException("ALZ/EGG 지원 모듈이 없습니다. 공식 UnEGG64.dll을 별도로 준비해 앱 폴더에 두세요. 이 모듈은 프로젝트와 별도 라이선스가 적용됩니다.");
    }

    private static IArchive OpenSharpArchive(string path, string? password) => ArchiveFactory.OpenArchive(path, new ReaderOptions
    {
        Password = string.IsNullOrWhiteSpace(password) ? null : password,
        LeaveStreamOpen = false
    });

    private static void EnsureNoReparsePoint(string root, string target)
    {
        string current = Path.GetDirectoryName(target) ?? root;
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        while (current.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"저장 위치에 바로가기/링크 폴더가 있어 중단했습니다: {current}");
            if (string.Equals(current, fullRoot, StringComparison.OrdinalIgnoreCase)) break;
            current = Path.GetDirectoryName(current) ?? fullRoot;
        }
    }

    private static void CopyWithCancellation(Stream input, Stream output, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1024 * 1024];
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.Write(buffer, 0, read);
        }
    }

    private static void DrainWithCancellation(Stream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1024 * 1024];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) cancellationToken.ThrowIfCancellationRequested();
    }

    private sealed class UnEggSession : IDisposable
    {
        private readonly string _archivePath;
        private readonly NativeEventProc _callback;
        private readonly string? _destination;
        private readonly HashSet<int>? _selected;
        private readonly string? _password;
        private IProgress<(int Done, int Total)>? _progress;
        private nuint _handle;
        private bool _opened;
        private int _filesDone;
        private int _totalFiles;
        private CancellationToken _cancellationToken;
        private string? _callbackError;

        public UnEggSession(
            string archivePath,
            string? password,
            string? destination,
            HashSet<int>? selected,
            bool validateOnly = false,
            IProgress<(int Done, int Total)>? progress = null)
        {
            _callback = OnNativeEvent;
            _archivePath = Path.GetFullPath(archivePath);
            _password = password;
            _destination = destination is null ? null : Path.GetFullPath(destination);
            _selected = selected;
            uint type = 0;
            uint valid = NativeIsValidArchive(archivePath, out type);
            if (valid != ErrorSuccess || type is not (1 or 2))
                throw new InvalidDataException("공식 ALZ/EGG 해제 엔진이 이 파일을 인식하지 못했습니다.");
            if (validateOnly) return;
            uint created = NativeCreateEgg(out _handle, _callback, 0);
            if (created != ErrorSuccess || _handle == 0) throw new InvalidOperationException($"ALZ/EGG 엔진 초기화 실패 (코드 {created}).");
            uint opened = NativeOpenArchive(_handle, archivePath);
            if (opened != ErrorSuccess)
            {
                NativeDestroyEgg(ref _handle);
                throw MakeNativeError("압축 파일을 열지 못했습니다", opened);
            }
            _opened = true;
        }

        public List<ArchiveItem> ReadItems()
        {
            if (_handle == 0) throw new InvalidOperationException("압축 핸들이 없습니다.");
            uint result = NativeGetFileCount(_handle, out nuint count);
            if (result != ErrorSuccess) throw MakeNativeError("항목 목록을 읽지 못했습니다", result);
            if (count > 1_000_000) throw new InvalidDataException("항목 수가 안전 제한을 초과했습니다.");
            var items = new List<ArchiveItem>((int)count);
            for (nuint i = 0; i < count; i++)
            {
                result = NativeGetFileHeader(_handle, i, out NativeFileHeader header);
                if (result != ErrorSuccess) throw MakeNativeError($"항목 {i + 1} 정보를 읽지 못했습니다", result);
                string name = Marshal.PtrToStringUni(header.FileName) ?? string.Empty;
                DateTime? modified = null;
                if (header.LastModified > 0 && header.LastModified < 4_102_444_800)
                {
                    try { modified = DateTimeOffset.FromUnixTimeSeconds(header.LastModified).LocalDateTime; }
                    catch (ArgumentOutOfRangeException) { }
                }
                items.Add(new ArchiveItem((int)i, name, ClampSize(header.UnpackedSize), ClampSize(header.PackedSize), modified, name.EndsWith('/') || name.EndsWith('\\'), header.Encrypted != 0));
            }
            return items;
        }

        public void Extract(IProgress<(int Done, int Total)>? progress, CancellationToken cancellationToken)
        {
            if (_destination is null || _selected is null) throw new InvalidOperationException("추출 작업의 저장 위치가 지정되지 않았습니다.");
            _progress = progress;
            _cancellationToken = cancellationToken;
            _totalFiles = _selected.Count;
            uint result = NativeExtract(_handle);
            if (_callbackError is not null) throw new InvalidDataException(_callbackError);
            if (result != ErrorSuccess) throw MakeNativeError("ALZ/EGG 압축 해제에 실패했습니다", result);
        }

        public void Test(IProgress<(int Done, int Total)>? progress, CancellationToken cancellationToken)
        {
            _progress = progress;
            _cancellationToken = cancellationToken;
            _totalFiles = int.MaxValue;
            uint result = NativeTest(_handle);
            if (_callbackError is not null) throw new InvalidDataException(_callbackError);
            if (result != ErrorSuccess) throw MakeNativeError("ALZ/EGG 파일 검증에 실패했습니다", result);
        }

        private uint OnNativeEvent(nint user, uint code, nint param)
        {
            try
            {
                if (_cancellationToken.IsCancellationRequested) return ErrorCancel;
                switch (code)
                {
                    case EventExtract:
                    {
                        long nativeIndex = Marshal.ReadInt64(param, 0);
                        int index = checked((int)nativeIndex);
                        if (_selected is not null && !_selected.Contains(index)) return ErrorSkip;
                        string archiveName = Marshal.PtrToStringUni(Marshal.ReadIntPtr(param, IntPtr.Size)) ?? string.Empty;
                        if (_destination is null) return ErrorCancel;
                        string target;
                        try { target = ResolveSafePath(_destination, archiveName); }
                        catch (InvalidDataException ex) { _callbackError = ex.Message; return ErrorCancel; }
                        if (string.Equals(Path.GetFullPath(target), _archivePath, StringComparison.OrdinalIgnoreCase))
                        {
                            _callbackError = "압축 파일 자체를 덮어쓸 수 있는 항목은 풀 수 없습니다.";
                            return ErrorCancel;
                        }
                        if (target.Length >= MaxPath - 1)
                        {
                            _callbackError = $"경로 길이 제한(MAX_PATH)을 넘는 항목은 풀 수 없습니다: {archiveName}";
                            return ErrorCancel;
                        }
                        EnsureNoReparsePoint(_destination, target);
                        WriteFixedUnicode(param, IntPtr.Size * 2, MaxPath, target);
                        _progress?.Report((Math.Min(++_filesDone, _totalFiles), _totalFiles));
                        return ErrorSuccess;
                    }
                    case EventProgress:
                    {
                        short total = Marshal.ReadInt16(param, 0);
                        short current = Marshal.ReadInt16(param, sizeof(short));
                        if (total > 0 && _totalFiles == int.MaxValue)
                            _progress?.Report((Math.Clamp((int)current, 0, total), total));
                        return ErrorSuccess;
                    }
                    case EventQueryPassword:
                    {
                        if (string.IsNullOrEmpty(_password))
                        {
                            _callbackError = "이 압축 파일은 암호가 필요합니다. 암호를 입력한 뒤 다시 시도하세요.";
                            return ErrorCancel;
                        }
                        if (_password.Length >= 1024)
                        {
                            _callbackError = "암호가 너무 깁니다.";
                            return ErrorCancel;
                        }
                        WriteFixedUnicode(param, IntPtr.Size * 2, 1024, _password);
                        return ErrorSuccess;
                    }
                    case EventTest:
                        return ErrorSuccess;
                    case EventError:
                    {
                        uint codeValue = unchecked((uint)Marshal.ReadInt32(param, IntPtr.Size));
                        if (codeValue == ErrorWrongPassword) _callbackError = "암호가 올바르지 않습니다.";
                        return ErrorSuccess;
                    }
                }
                return ErrorSuccess;
            }
            catch (OperationCanceledException) { return ErrorCancel; }
            catch (Exception ex)
            {
                _callbackError = ex.Message;
                return ErrorCancel;
            }
        }

        public void Dispose()
        {
            if (_handle == 0) return;
            if (_opened) NativeCloseArchive(_handle);
            NativeDestroyEgg(ref _handle);
            GC.KeepAlive(_callback);
        }

        private static long ClampSize(ulong size) => size > long.MaxValue ? long.MaxValue : (long)size;

        private static void WriteFixedUnicode(nint structure, int offset, int capacity, string value)
        {
            nint buffer = IntPtr.Add(structure, offset);
            for (int i = 0; i < capacity; i++) Marshal.WriteInt16(buffer, i * sizeof(char), 0);
            for (int i = 0; i < value.Length; i++) Marshal.WriteInt16(buffer, i * sizeof(char), value[i]);
        }

        private static Exception MakeNativeError(string task, uint code) => code switch
        {
            ErrorWrongPassword => new InvalidDataException($"{task}: 암호가 올바르지 않습니다."),
            7 => new InvalidDataException($"{task}: 압축 파일 구조가 손상되었거나 지원되지 않습니다."),
            8 => new InvalidDataException($"{task}: 지원하지 않는 압축 알고리즘입니다."),
            _ => new InvalidDataException($"{task} (오류 코드 {code}).")
        };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint NativeEventProc(nint user, uint code, nint param);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeFileHeader
        {
            public nuint Size;
            public nint FileName;
            public nint Comment;
            public ulong PackedSize;
            public ulong UnpackedSize;
            public uint Attribute;
            public uint Crc;
            public uint Method;
            public long LastModified;
            public int Encrypted;
        }

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_CreateEgg", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeCreateEgg(out nuint handle, NativeEventProc eventProc, nint user);

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_DestroyEgg", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeDestroyEgg(ref nuint handle);

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_IsValidArchive", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeIsValidArchive(string fileName, out uint type);

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_OpenArchive", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeOpenArchive(nuint handle, string fileName);

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_CloseArchive", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeCloseArchive(nuint handle);

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_GetFileCount", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeGetFileCount(nuint handle, out nuint count);

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_GetFileHeader", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeGetFileHeader(nuint handle, nuint index, out NativeFileHeader header);

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_Extract", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeExtract(nuint handle);

        [DllImport("UnEGG64.dll", EntryPoint = "EGG_Test", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern uint NativeTest(nuint handle);
    }
}
