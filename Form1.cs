using System.IO;
using SharpCompress.Common;

namespace MyZip;

public partial class Form1 : Form
{
    private readonly ImageList _itemIcons = new() { ImageSize = new Size(18, 18), ColorDepth = ColorDepth.Depth32Bit };
    private readonly ListView _items = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = true,
        HideSelection = false,
        BorderStyle = BorderStyle.None,
        CheckBoxes = false
    };
    private readonly Label _archiveName = new() { AutoEllipsis = true, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(30, 48, 67), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _archiveInfo = new() { AutoEllipsis = true, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(95, 112, 128), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly TextBox _password = new() { Width = 180, UseSystemPasswordChar = true, PlaceholderText = "암호(필요한 경우)" };
    private readonly CheckBox _showPassword = new() { Text = "표시", AutoSize = true, ForeColor = Color.FromArgb(75, 91, 106), Margin = new Padding(4, 5, 12, 0), ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText };
    private readonly ComboBox _compressionProfile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly Label _status = new() { AutoEllipsis = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(78, 95, 111) };
    private readonly Label _progressText = new() { AutoSize = true, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(78, 95, 111), Margin = new Padding(8, 6, 10, 0) };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000, Style = ProgressBarStyle.Continuous };
    private readonly Button _openButton = ActionButton("압축 파일 열기", MenuIcon.OpenArchive, primary: true);
    private readonly Button _extractButton = ActionButton("선택 항목 풀기", MenuIcon.Extract);
    private readonly Button _previewButton = ActionButton("미리보기", MenuIcon.Preview);
    private readonly Button _createFilesButton = ActionButton("파일 압축", MenuIcon.CompressFile);
    private readonly Button _createFolderButton = ActionButton("폴더 압축", MenuIcon.CompressFolder);
    private readonly Button _testButton = ActionButton("압축 검사", MenuIcon.Verify);
    private readonly Button _cancelButton = ActionButton("취소", MenuIcon.Cancel);

    private ArchiveSnapshot? _snapshot;
    private CancellationTokenSource? _operation;
    private bool _busy;

    public Form1(string[]? startupArgs = null)
    {
        InitializeComponent();
        BuildUi();
        if (startupArgs?.FirstOrDefault() is { Length: > 0 } startupPath)
            Shown += async (_, _) => await OpenInputAsync(startupPath);
    }

    private void BuildUi()
    {
        Text = "myzip · 파일 압축 도구";
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? System.Drawing.SystemIcons.Application;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(880, 610);
        Size = new Size(1120, 740);
        BackColor = Color.FromArgb(246, 248, 250);
        Font = new Font("Segoe UI", 9.5f);
        AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        _showPassword.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !_showPassword.Checked;
        _openButton.Click += async (_, _) => await OpenDialogAsync();
        _extractButton.Click += async (_, _) => await ExtractSelectedAsync();
        _previewButton.Click += async (_, _) => await PreviewSelectedAsync();
        _createFilesButton.Click += async (_, _) => await CreateFromFilesDialogAsync();
        _createFolderButton.Click += async (_, _) => await CreateFromFolderDialogAsync();
        _testButton.Click += async (_, _) => await TestArchiveAsync();
        _cancelButton.Click += (_, _) => _operation?.Cancel();
        _cancelButton.Enabled = false;
        _items.DoubleClick += async (_, _) =>
        {
            if (CanPreviewSelection()) await PreviewSelectedAsync();
            else await ExtractSelectedAsync();
        };
        _items.SelectedIndexChanged += (_, _) => _previewButton.Enabled = !_busy && CanPreviewSelection();
        _items.Columns.Add("이름", 500);
        _items.Columns.Add("크기", 115, HorizontalAlignment.Right);
        _items.Columns.Add("압축 크기", 115, HorizontalAlignment.Right);
        _items.Columns.Add("형식", 90);
        _items.Columns.Add("수정한 날짜", 165);
        _previewButton.Enabled = false;
        _items.SmallImageList = _itemIcons;
        _itemIcons.Images.Add("file", MenuIconFactory.Create(MenuIcon.File, Color.FromArgb(47, 105, 154), 18));
        _itemIcons.Images.Add("folder", MenuIconFactory.Create(MenuIcon.Folder, Color.FromArgb(209, 148, 38), 18));
        _showPassword.Image = MenuIconFactory.Create(MenuIcon.Eye, Color.FromArgb(75, 91, 106), 17);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(20), BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        Controls.Add(root);

        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        _archiveName.Text = "압축 파일을 열거나 새로 만들어 보세요";
        _archiveInfo.Text = "ZIP · 7Z · RAR · TAR 등 열기 · ZIP/7Z 만들기 · ALZ/EGG 해제";
        heading.Controls.Add(_archiveName, 0, 0);
        heading.Controls.Add(_archiveInfo, 0, 1);
        root.Controls.Add(heading, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 10), BackColor = Color.FromArgb(240, 246, 250), Padding = new Padding(8, 7, 8, 7), AutoScroll = true };
        actions.Controls.AddRange([_openButton, _extractButton, _previewButton, _testButton, Separator(), _createFilesButton, _createFolderButton]);
        root.Controls.Add(actions, 0, 1);

        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0, 5, 0, 0) };
        options.Controls.Add(new PictureBox { Image = MenuIconFactory.Create(MenuIcon.Password, Color.FromArgb(78, 95, 111), 18), Size = new Size(22, 24), SizeMode = PictureBoxSizeMode.CenterImage, Margin = new Padding(0, 4, 2, 0) });
        options.Controls.Add(new Label { Text = "암호", AutoSize = true, ForeColor = Color.FromArgb(78, 95, 111), Margin = new Padding(0, 8, 8, 0) });
        options.Controls.Add(_password);
        options.Controls.Add(_showPassword);
        options.Controls.Add(new PictureBox { Image = MenuIconFactory.Create(MenuIcon.Settings, Color.FromArgb(78, 95, 111), 18), Size = new Size(20, 24), SizeMode = PictureBoxSizeMode.CenterImage, Margin = new Padding(8, 4, 0, 0) });
        options.Controls.Add(new Label { Text = "새 압축 설정", AutoSize = true, ForeColor = Color.FromArgb(78, 95, 111), Margin = new Padding(0, 8, 8, 0) });
        _compressionProfile.Items.AddRange(["빠르게", "균형", "작은 크기"]);
        _compressionProfile.SelectedIndex = 0;
        options.Controls.Add(_compressionProfile);
        options.Controls.Add(new Label { Text = "빠르게: 속도 우선 · 균형: 일반 · 작은 크기: 시간이 더 걸림", AutoSize = true, ForeColor = Color.FromArgb(111, 125, 138), Margin = new Padding(12, 8, 0, 0) });
        root.Controls.Add(options, 0, 2);

        var listShell = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1), Margin = new Padding(0, 4, 0, 8) };
        _items.BackColor = Color.White;
        _items.Font = new Font("Segoe UI", 9.5f);
        listShell.Controls.Add(_items);
        root.Controls.Add(listShell, 0, 3);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var statusRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        statusRow.Controls.Add(_status, 0, 0);
        statusRow.Controls.Add(_progressText, 1, 0);
        statusRow.Controls.Add(_cancelButton, 2, 0);
        _status.Text = "준비됨 · 창에 압축 파일을 끌어다 놓아도 열 수 있습니다.";
        footer.Controls.Add(statusRow, 0, 0);
        footer.Controls.Add(_progress, 0, 1);
        root.Controls.Add(footer, 0, 4);
    }

    private async Task OpenDialogAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "열 압축 파일 선택",
            Filter = "압축 파일|*.zip;*.7z;*.rar;*.egg;*.alz;*.tar;*.gz;*.bz2;*.xz;*.zst;*.cab;*.arj;*.ace;*.lzh;*.arc;*.iso;*.wim;*.001|미리보기 파일|*.pdf;*.docx;*.xlsx;*.pptx;*.txt;*.md;*.csv;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.mp4;*.mov;*.m4v;*.wmv;*.avi;*.mkv;*.webm;*.mp3;*.wav|모든 파일|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) await OpenInputAsync(dialog.FileName);
    }

    private async Task OpenInputAsync(string path)
    {
        if (!File.Exists(path))
        {
            MessageBox.Show(this, $"전달받은 파일을 찾을 수 없습니다.\r\n{path}", "파일 열기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (IsArchivePath(path)) { await OpenArchiveAsync(path); return; }
        if (PreviewContent.CanPreview(path)) { ShowStandalonePreview(path); return; }
        MessageBox.Show(this, "지원하는 압축 파일이나 미리보기 가능한 문서·사진·영상 파일을 선택하세요.", "지원하지 않는 파일", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static bool IsArchivePath(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".zip" or ".7z" or ".rar" or ".egg" or ".alz" or ".tar" or ".gz" or ".bz2" or ".xz" or ".zst" or ".cab" or ".arj" or ".ace" or ".lzh" or ".arc" or ".iso" or ".wim" or ".001";

    private bool CanPreviewSelection()
    {
        if (_snapshot is null || _items.SelectedIndices.Count != 1) return false;
        var item = _snapshot.Items[_items.SelectedIndices[0]];
        return !item.IsDirectory && PreviewContent.CanPreview(item.Name);
    }

    private async Task PreviewSelectedAsync()
    {
        if (_snapshot is null || _items.SelectedIndices.Count != 1)
        {
            MessageBox.Show(this, "미리볼 파일 하나를 선택하세요.", "미리보기", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var item = _snapshot.Items[_items.SelectedIndices[0]];
        if (item.IsDirectory || !PreviewContent.CanPreview(item.Name))
        {
            MessageBox.Show(this, "이 항목은 미리보기를 지원하지 않습니다.", "미리보기", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try { PreviewContent.EnsurePreviewSize(item.Size); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "미리보기", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        string tempFolder = Path.Combine(Path.GetTempPath(), "myzip-preview", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        _operation = new CancellationTokenSource();
        var progress = new Progress<(int Done, int Total)>(value =>
        {
            _progress.Value = value.Total <= 0 ? 0 : (int)Math.Clamp(value.Done * 1000d / value.Total, 0, 1000);
            _progressText.Text = value.Total <= 0 ? string.Empty : $"{value.Done:N0} / {value.Total:N0}";
        });
        SetBusy(true, "미리보기 파일을 준비하는 중…");
        bool previewOwnsTemp = false;
        try
        {
            await ArchiveEngineService.ExtractAsync(_snapshot, [item.Index], tempFolder, _password.Text, progress, _operation.Token);
            string extractedPath = ArchiveEngineService.ResolveSafePath(tempFolder, item.Name);
            var preview = new FilePreviewForm(extractedPath, item.Name, tempFolder);
            previewOwnsTemp = true;
            preview.ShowDialog(this);
        }
        catch (OperationCanceledException) { _status.Text = "미리보기를 취소했습니다."; }
        catch (Exception ex) { MessageBox.Show(this, FriendlyError(ex), "미리보기 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally
        {
            if (!previewOwnsTemp) try { Directory.Delete(tempFolder, recursive: true); } catch { }
            _operation.Dispose();
            _operation = null;
            SetBusy(false);
        }
    }

    private void ShowStandalonePreview(string path)
    {
        try
        {
            PreviewContent.EnsurePreviewSize(new FileInfo(path).Length);
            using var preview = new FilePreviewForm(path, Path.GetFileName(path));
            preview.ShowDialog(this);
        }
        catch (Exception ex) { MessageBox.Show(this, FriendlyError(ex), "미리보기 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task OpenArchiveAsync(string path)
    {
        try
        {
            SetBusy(true, "압축 파일을 읽는 중…");
            var snapshot = await Task.Run(() => ArchiveEngineService.Open(path, _password.Text));
            _snapshot = snapshot;
            PopulateItems(snapshot);
            _archiveName.Text = Path.GetFileName(path);
            string encrypted = snapshot.Items.Any(i => i.IsEncrypted) ? " · 암호 보호" : string.Empty;
            string solid = snapshot.IsSolid ? " · 솔리드" : string.Empty;
            _archiveInfo.Text = $"{snapshot.Format ?? Path.GetExtension(path).TrimStart('.').ToUpperInvariant()} · {snapshot.Items.Count:N0}개 항목{encrypted}{solid}";
            _status.Text = "열기 완료 · 풀 항목을 선택하거나 압축 검사를 실행하세요.";
        }
        catch (Exception ex)
        {
            _status.Text = "압축 파일을 열지 못했습니다.";
            MessageBox.Show(this, FriendlyError(ex), "열기 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { SetBusy(false); }
    }

    private async Task ExtractSelectedAsync()
    {
        if (_snapshot is null) { MessageBox.Show(this, "먼저 압축 파일을 여세요."); return; }
        var indices = _items.SelectedIndices.Cast<int>().ToList();
        if (indices.Count == 0) indices = _snapshot.Items.Select(i => i.Index).ToList();
        string? baseFolder = ChooseFolder("압축을 풀 저장 위치 선택");
        if (baseFolder is null) return;
        string destination = Path.Combine(baseFolder, Path.GetFileNameWithoutExtension(_snapshot.Path) + "_압축풀기");
        if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
        {
            var choice = MessageBox.Show(this, "같은 이름의 폴더가 이미 있고 내용이 있습니다. 기존 파일을 덮어쓸 수 있습니다. 계속할까요?", "저장 위치 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (choice != DialogResult.Yes) return;
        }
        await RunOperationAsync("압축 해제 중…", async (progress, token) =>
        {
            int count = await ArchiveEngineService.ExtractAsync(_snapshot, indices, destination, _password.Text, progress, token);
            return $"{count:N0}개 항목을 풀었습니다: {destination}";
        });
    }

    private async Task TestArchiveAsync()
    {
        if (_snapshot is null) { MessageBox.Show(this, "먼저 압축 파일을 여세요."); return; }
        await RunOperationAsync("압축 데이터를 검사 중…", async (progress, token) =>
        {
            int count = await ArchiveEngineService.TestAsync(_snapshot, _password.Text, progress, token);
            return $"검사 완료 · {count:N0}개 파일의 데이터 확인";
        });
    }

    private async Task CreateFromFilesDialogAsync()
    {
        using var files = new OpenFileDialog { Title = "압축할 파일 선택", Multiselect = true, CheckFileExists = true, Filter = "모든 파일|*.*" };
        if (files.ShowDialog(this) != DialogResult.OK) return;
        await CreateArchiveDialogAsync(files.FileNames);
    }

    private async Task CreateFromFolderDialogAsync()
    {
        using var folder = new FolderBrowserDialog { Description = "압축할 폴더 선택", ShowNewFolderButton = false };
        if (folder.ShowDialog(this) != DialogResult.OK) return;
        await CreateArchiveDialogAsync([folder.SelectedPath]);
    }

    private async Task CreateArchiveDialogAsync(IReadOnlyList<string> sources)
    {
        using var save = new SaveFileDialog
        {
            Title = "새 압축 파일 저장",
            Filter = "ZIP 압축 파일 (*.zip)|*.zip|7-Zip 압축 파일 (*.7z)|*.7z",
            FilterIndex = 1,
            DefaultExt = "zip",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = Path.GetFileNameWithoutExtension(sources[0])
        };
        if (save.ShowDialog(this) != DialogResult.OK) return;
        var type = save.FilterIndex == 2 ? ArchiveType.SevenZip : ArchiveType.Zip;
        int level = _compressionProfile.SelectedIndex switch { 1 => 5, 2 => 9, _ => 1 };
        await RunOperationAsync($"{type} 압축 중…", async (progress, token) =>
        {
            await ArchiveEngineService.CreateAsync(sources, save.FileName, type, level, progress, token);
            return $"{type} 압축 완료: {save.FileName}";
        });
        if (File.Exists(save.FileName)) await OpenArchiveAsync(save.FileName);
    }

    private async Task RunOperationAsync(string title, Func<IProgress<(int Done, int Total)>, CancellationToken, Task<string>> operation)
    {
        if (_busy) return;
        _operation = new CancellationTokenSource();
        var progress = new Progress<(int Done, int Total)>(value =>
        {
            _progress.Value = value.Total <= 0 ? 0 : (int)Math.Clamp(value.Done * 1000d / value.Total, 0, 1000);
            _progressText.Text = value.Total <= 0 ? string.Empty : $"{value.Done:N0} / {value.Total:N0}";
        });
        SetBusy(true, title);
        try
        {
            string result = await operation(progress, _operation.Token);
            _status.Text = result;
            if (result.StartsWith("검사 완료", StringComparison.Ordinal) || result.StartsWith("ZIP 압축 완료", StringComparison.Ordinal) || result.StartsWith("SevenZip 압축 완료", StringComparison.Ordinal))
                MessageBox.Show(this, result, "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException) { _status.Text = "작업을 취소했습니다."; }
        catch (Exception ex)
        {
            _status.Text = "작업을 완료하지 못했습니다.";
            MessageBox.Show(this, FriendlyError(ex), "작업 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _operation.Dispose(); _operation = null;
            SetBusy(false);
        }
    }

    private void PopulateItems(ArchiveSnapshot snapshot)
    {
        _items.BeginUpdate();
        _items.Items.Clear();
        foreach (var entry in snapshot.Items)
        {
            var row = new ListViewItem(entry.Name) { Tag = entry.Index, ImageKey = entry.IsDirectory ? "folder" : "file" };
            row.SubItems.Add(entry.IsDirectory ? "—" : FormatSize(entry.Size));
            row.SubItems.Add(entry.IsDirectory ? "—" : FormatSize(entry.PackedSize));
            row.SubItems.Add(entry.IsEncrypted ? "암호화" : entry.IsDirectory ? "폴더" : Path.GetExtension(entry.Name).TrimStart('.').ToUpperInvariant());
            row.SubItems.Add(entry.Modified?.ToString("yyyy-MM-dd HH:mm") ?? "—");
            if (entry.IsEncrypted) row.ForeColor = Color.FromArgb(76, 104, 139);
            _items.Items.Add(row);
        }
        _items.EndUpdate();
    }

    private static string? ChooseFolder(string description)
    {
        using var dialog = new FolderBrowserDialog { Description = description, ShowNewFolderButton = true };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _busy = busy;
        _openButton.Enabled = !busy;
        _extractButton.Enabled = !busy && _snapshot is not null;
        _previewButton.Enabled = !busy && CanPreviewSelection();
        _testButton.Enabled = !busy && _snapshot is not null;
        _createFilesButton.Enabled = !busy;
        _createFolderButton.Enabled = !busy;
        _password.Enabled = !busy;
        _compressionProfile.Enabled = !busy;
        _cancelButton.Enabled = busy;
        if (status is not null) _status.Text = status;
        if (!busy)
        {
            _progress.Value = 0;
            _progressText.Text = string.Empty;
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;

    private async void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) await OpenInputAsync(files[0]);
    }

    private static Button ActionButton(string label, MenuIcon icon, bool primary = false)
    {
        var button = new Button
        {
            Text = label,
            Image = MenuIconFactory.Create(icon, primary ? Color.White : Color.FromArgb(29, 119, 178), 34),
            ImageAlign = ContentAlignment.TopCenter,
            TextImageRelation = TextImageRelation.ImageAboveText,
            TextAlign = ContentAlignment.BottomCenter,
            AutoSize = false,
            Size = new Size(148, 78),
            Margin = new Padding(4, 0, 4, 0),
            Padding = new Padding(5, 5, 5, 5),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(26, 119, 176) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(39, 58, 76),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(207, 220, 230);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(21, 103, 155) : Color.FromArgb(229, 243, 251);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(17, 88, 135) : Color.FromArgb(214, 234, 246);
        return button;
    }

    private static Control Separator() => new Panel { Width = 1, Height = 30, BackColor = Color.FromArgb(221, 228, 234), Margin = new Padding(8, 2, 8, 0) };

    private static string FormatSize(long bytes)
    {
        if (bytes < 0) return "—";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return unit == 0 ? $"{value:N0} {units[unit]}" : $"{value:N1} {units[unit]}";
    }

    private static string FriendlyError(Exception ex)
    {
        string message = ex.GetBaseException().Message;
        if (message.Contains("password", StringComparison.OrdinalIgnoreCase) || message.Contains("암호", StringComparison.OrdinalIgnoreCase))
            return message + "\n\n상단 암호 칸에 암호를 입력한 뒤 다시 시도하세요.";
        if (message.Contains("UnEGG64.dll", StringComparison.OrdinalIgnoreCase))
            return "ALZ/EGG 해제 모듈을 불러오지 못했습니다. 프로그램 폴더에 포함된 UnEGG64.dll과 라이선스 파일이 있는지 확인하세요.\n\n" + message;
        return message;
    }
}
