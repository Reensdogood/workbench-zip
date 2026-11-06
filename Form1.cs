using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        CheckBoxes = true,
        OwnerDraw = true,
        BackColor = Color.FromArgb(249, 252, 255)
    };
    private readonly Label _archiveName = new() { AutoEllipsis = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(24, 53, 78), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };
    private readonly Label _archiveInfo = new() { AutoEllipsis = true, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(71, 104, 129), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };
    private readonly TextBox _password = new() { Width = 180, UseSystemPasswordChar = true, PlaceholderText = "암호(필요한 경우)", BackColor = Color.FromArgb(251, 253, 255), ForeColor = Color.FromArgb(24, 53, 78), BorderStyle = BorderStyle.FixedSingle };
    private readonly CheckBox _showPassword = new() { Text = "표시", AutoSize = true, ForeColor = Color.FromArgb(49, 82, 107), BackColor = Color.Transparent, Margin = new Padding(4, 5, 12, 0), ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText };
    private readonly ComboBox _compressionProfile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(251, 253, 255), ForeColor = Color.FromArgb(24, 53, 78) };
    private readonly Label _status = new() { AutoEllipsis = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(47, 83, 108), BackColor = Color.Transparent };
    private readonly Label _progressText = new() { AutoSize = true, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(55, 99, 127), BackColor = Color.Transparent, Margin = new Padding(8, 6, 10, 0) };
    private readonly GlassProgressBar _progress = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000, Margin = new Padding(0, 4, 0, 0) };
    private readonly Button _openButton = ActionButton("압축 파일 열기", MenuIcon.OpenArchive, primary: true);
    private readonly Button _extractButton = ActionButton("압축 풀기 ▾", MenuIcon.Extract);
    private readonly Button _previewFileButton = ActionButton("미리보기", MenuIcon.Preview);
    private readonly Button _createButton = ActionButton("압축 만들기 ▾", MenuIcon.CompressFile);
    private readonly Button _testButton = ActionButton("압축 검사", MenuIcon.Verify);
    private readonly GlassActionButton _cancelButton = new() { Text = "취소", Image = MenuIconFactory.Create(MenuIcon.Cancel, Color.FromArgb(42, 87, 118), 16), Compact = true, Size = new Size(68, 27), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Margin = new Padding(2, 1, 2, 0) };
    private readonly ContextMenuStrip _extractMenu = new();
    private readonly Button _editButton = ActionButton("파일 편집 ▾", MenuIcon.Settings);
    private readonly ContextMenuStrip _editMenu = new();
    private readonly ContextMenuStrip _createMenu = new();
    private readonly ContextMenuStrip _itemMenu = new();

    private ArchiveSnapshot? _snapshot;
    private CancellationTokenSource? _operation;
    private string? _editWorkspace;
    private bool _editChanges;
    private bool _busy;

    public Form1(string[]? startupArgs = null)
    {
        InitializeComponent();
        BuildUi();
        if (startupArgs is { Length: > 0 })
        {
            if (startupArgs[0].Equals("--compress", StringComparison.OrdinalIgnoreCase))
                Shown += async (_, _) => await RunShellCompressAsync(startupArgs.Skip(1).ToArray());
            else if (startupArgs[0].Equals("--extract", StringComparison.OrdinalIgnoreCase))
                Shown += async (_, _) => await RunShellExtractAsync(startupArgs.Skip(1).FirstOrDefault());
            else
                Shown += async (_, _) => await OpenInputAsync(startupArgs[0]);
        }
    }

    private async Task RunShellCompressAsync(IReadOnlyList<string> paths)
    {
        var inputs = paths.Where(path => File.Exists(path) || Directory.Exists(path)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (inputs.Length == 0)
        {
            MessageBox.Show(this, "압축할 파일이나 폴더를 찾지 못했습니다.", "myzip으로 압축", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        await CreateArchiveDialogAsync(inputs);
    }

    private async Task RunShellExtractAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || !IsArchivePath(path))
        {
            MessageBox.Show(this, "압축 파일을 찾지 못했거나 지원하지 않는 형식입니다.", "myzip으로 압축 해제", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        await OpenArchiveAsync(path);
        if (_snapshot is not null && string.Equals(Path.GetFullPath(path), Path.GetFullPath(_snapshot.Path), StringComparison.OrdinalIgnoreCase))
            await ExtractSelectedAsync(ExtractMode.AutoFolder);
    }

    private void BuildUi()
    {
        Text = "myzip · 파일 압축 도구";
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? System.Drawing.SystemIcons.Application;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(880, 610);
        Size = new Size(1120, 740);
        BackColor = Color.FromArgb(233, 243, 250);
        Font = new Font("Segoe UI", 9.25f);
        _extractMenu.Renderer = new ToolStripProfessionalRenderer(new GlassToolStripColorTable());
        _extractMenu.BackColor = Color.FromArgb(244, 250, 255);
        _extractMenu.ForeColor = Color.FromArgb(31, 66, 91);
        _extractMenu.Font = new Font("Segoe UI", 9.25f);
        _extractMenu.ShowImageMargin = false;
        _editMenu.Renderer = new ToolStripProfessionalRenderer(new GlassToolStripColorTable());
        _editMenu.BackColor = Color.FromArgb(244, 250, 255);
        _editMenu.ForeColor = Color.FromArgb(31, 66, 91);
        _editMenu.Font = new Font("Segoe UI", 9.25f);
        _editMenu.ShowImageMargin = false;
        _createMenu.Renderer = new ToolStripProfessionalRenderer(new GlassToolStripColorTable());
        _createMenu.BackColor = Color.FromArgb(244, 250, 255);
        _createMenu.ForeColor = Color.FromArgb(31, 66, 91);
        _createMenu.Font = new Font("Segoe UI", 9.25f);
        _createMenu.ShowImageMargin = false;
        _itemMenu.Renderer = new ToolStripProfessionalRenderer(new GlassToolStripColorTable());
        _itemMenu.BackColor = Color.FromArgb(244, 250, 255);
        _itemMenu.ForeColor = Color.FromArgb(31, 66, 91);
        _itemMenu.Font = new Font("Segoe UI", 9.25f);
        _itemMenu.ShowImageMargin = false;
        AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        _showPassword.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !_showPassword.Checked;
        _openButton.Click += async (_, _) => await OpenDialogAsync();
        BuildExtractMenu();
        BuildEditMenu();
        BuildCreateMenu();
        BuildItemMenu();
        _extractButton.Click += (_, _) => _extractMenu.Show(_extractButton, new Point(0, _extractButton.Height));
        _editButton.Click += (_, _) => _editMenu.Show(_editButton, new Point(0, _editButton.Height));
        _createButton.Click += (_, _) => _createMenu.Show(_createButton, new Point(0, _createButton.Height));
        _previewFileButton.Click += async (_, _) => await PreviewSelectedAsync();
        _testButton.Click += async (_, _) => await TestArchiveAsync();
        _cancelButton.Click += (_, _) => _operation?.Cancel();
        _cancelButton.Enabled = false;
        _items.DoubleClick += async (_, _) => await OpenSelectedInDefaultAppAsync();
        _items.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            var hit = _items.GetItemAt(e.X, e.Y);
            if (hit is not null && !hit.Selected) { _items.SelectedItems.Clear(); hit.Selected = true; }
        };
        _items.ItemChecked += (_, _) =>
        {
            if (!_busy && _snapshot is not null && _items.CheckedItems.Count > 0)
                _status.Text = $"{_items.CheckedItems.Count:N0}개 항목 체크됨 · 압축 풀기 또는 파일 편집 작업에 적용됩니다.";
        };
        _items.SelectedIndexChanged += (_, _) => UpdateSelectionActions();
        _items.Columns.Add("이름", 500);
        _items.Columns.Add("크기", 115, HorizontalAlignment.Right);
        _items.Columns.Add("압축 크기", 115, HorizontalAlignment.Right);
        _items.Columns.Add("형식", 90);
        _items.Columns.Add("수정한 날짜", 165);
        _items.DrawColumnHeader += DrawGlassColumnHeader;
        _items.DrawItem += DrawGlassItem;
        _items.DrawSubItem += DrawGlassSubItem;
        _previewFileButton.Enabled = false;
        _items.SmallImageList = _itemIcons;
        _itemIcons.Images.Add("file", MenuIconFactory.Create(MenuIcon.File, Color.FromArgb(47, 105, 154), 18));
        _itemIcons.Images.Add("folder", MenuIconFactory.Create(MenuIcon.Folder, Color.FromArgb(209, 148, 38), 18));
        _showPassword.Image = MenuIconFactory.Create(MenuIcon.Eye, Color.FromArgb(75, 91, 106), 17);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(22), BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        Controls.Add(root);

        var headingShell = new GlassSurfacePanel { Dock = DockStyle.Fill, Tone = GlassSurfaceTone.Frost, Padding = new Padding(15, 6, 15, 6), Margin = new Padding(0, 0, 0, 5) };
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0), BackColor = Color.Transparent };
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        _archiveName.Text = "압축 파일을 열거나 새로 만들어 보세요";
        _archiveInfo.Text = "ZIP · 7Z · RAR · TAR 등 열기 · ZIP/7Z 만들기 · ALZ/EGG 해제";
        heading.Controls.Add(_archiveName, 0, 0);
        heading.Controls.Add(_archiveInfo, 0, 1);
        headingShell.Controls.Add(heading);
        root.Controls.Add(headingShell, 0, 0);

        var actionsShell = new GlassSurfacePanel { Dock = DockStyle.Fill, Tone = GlassSurfaceTone.Blue, Padding = new Padding(8, 7, 8, 9), Margin = new Padding(0, 2, 0, 8) };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), BackColor = Color.Transparent, Padding = new Padding(0), AutoScroll = true };
        actions.Controls.AddRange([_openButton, _extractButton, _previewFileButton, _testButton, Separator(), _createButton, _editButton]);
        actionsShell.Controls.Add(actions);
        root.Controls.Add(actionsShell, 0, 1);

        var optionsShell = new GlassSurfacePanel { Dock = DockStyle.Fill, Tone = GlassSurfaceTone.Frost, Padding = new Padding(11, 6, 11, 7), Margin = new Padding(0, 1, 0, 4) };
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0), BackColor = Color.Transparent };
        options.Controls.Add(new PictureBox { Image = MenuIconFactory.Create(MenuIcon.Password, Color.FromArgb(78, 95, 111), 18), Size = new Size(22, 24), SizeMode = PictureBoxSizeMode.CenterImage, Margin = new Padding(0, 4, 2, 0) });
        options.Controls.Add(new Label { Text = "암호", AutoSize = true, ForeColor = Color.FromArgb(49, 82, 107), BackColor = Color.Transparent, Margin = new Padding(0, 8, 8, 0) });
        options.Controls.Add(_password);
        options.Controls.Add(_showPassword);
        options.Controls.Add(new PictureBox { Image = MenuIconFactory.Create(MenuIcon.Settings, Color.FromArgb(62, 112, 145), 18), Size = new Size(20, 24), SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Color.Transparent, Margin = new Padding(8, 4, 0, 0) });
        options.Controls.Add(new Label { Text = "새 압축 설정", AutoSize = true, ForeColor = Color.FromArgb(49, 82, 107), BackColor = Color.Transparent, Margin = new Padding(0, 8, 8, 0) });
        _compressionProfile.Items.AddRange(["빠르게", "균형", "작은 크기"]);
        _compressionProfile.SelectedIndex = 0;
        options.Controls.Add(_compressionProfile);
        options.Controls.Add(new Label { Text = "빠르게: 속도 우선 · 균형: 일반 · 작은 크기: 시간이 더 걸림", AutoSize = true, ForeColor = Color.FromArgb(82, 117, 141), BackColor = Color.Transparent, Margin = new Padding(12, 8, 0, 0) });
        optionsShell.Controls.Add(options);
        root.Controls.Add(optionsShell, 0, 2);

        var listShell = new GlassSurfacePanel { Dock = DockStyle.Fill, Tone = GlassSurfaceTone.Frost, Padding = new Padding(6), Margin = new Padding(0, 5, 0, 8) };
        _items.BackColor = Color.FromArgb(250, 253, 255);
        _items.Font = new Font("Segoe UI", 9.25f);
        listShell.Controls.Add(_items);
        root.Controls.Add(listShell, 0, 3);

        var footerShell = new GlassSurfacePanel { Dock = DockStyle.Fill, Tone = GlassSurfaceTone.Frost, Padding = new Padding(12, 4, 12, 6), Margin = new Padding(0, 0, 0, 1) };
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0), BackColor = Color.Transparent };
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var statusRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0), BackColor = Color.Transparent };
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        statusRow.Controls.Add(_status, 0, 0);
        statusRow.Controls.Add(_progressText, 1, 0);
        statusRow.Controls.Add(_cancelButton, 2, 0);
        _status.Text = "준비됨 · 창에 압축 파일을 끌어다 놓아도 열 수 있습니다.";
        footer.Controls.Add(statusRow, 0, 0);
        footer.Controls.Add(_progress, 0, 1);
        footerShell.Controls.Add(footer);
        root.Controls.Add(footerShell, 0, 4);
        FormClosed += (_, _) => CleanupEditWorkspace();
    }

    private void DrawGlassColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        if (e.Header is null) return;
        var g = e.Graphics;
        var bounds = e.Bounds;
        using var fill = new System.Drawing.Drawing2D.LinearGradientBrush(bounds, Color.FromArgb(238, 247, 253), Color.FromArgb(220, 235, 246), System.Drawing.Drawing2D.LinearGradientMode.Vertical);
        g.FillRectangle(fill, bounds);
        using var line = new Pen(Color.FromArgb(176, 198, 216));
        g.DrawLine(line, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
        flags |= e.Header.TextAlign switch
        {
            HorizontalAlignment.Center => TextFormatFlags.HorizontalCenter,
            HorizontalAlignment.Right => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };
        var textBounds = Rectangle.Inflate(bounds, -10, -1);
        using var headerFont = new Font("Segoe UI", 9.25f, FontStyle.Bold);
        TextRenderer.DrawText(g, e.Header.Text, headerFont, textBounds, Color.FromArgb(46, 78, 102), flags);
    }

    private void DrawGlassItem(object? sender, DrawListViewItemEventArgs e)
    {
        if (_items.View != View.Details) e.DrawDefault = true;
    }

    private void DrawGlassSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        var item = e.Item;
        var subItem = e.SubItem;
        var header = e.Header;
        if (item is null || subItem is null || header is null) return;
        var g = e.Graphics;
        var bounds = e.Bounds;
        bool selected = item.Selected;
        Color rowColor = selected
            ? Color.FromArgb(204, 231, 248)
            : e.ItemIndex % 2 == 0 ? Color.FromArgb(250, 253, 255) : Color.FromArgb(244, 249, 253);
        using (var fill = new SolidBrush(rowColor)) g.FillRectangle(fill, bounds);

        if (selected && e.ColumnIndex == 0)
        {
            using var accent = new SolidBrush(Color.FromArgb(45, 149, 202));
            g.FillRectangle(accent, bounds.Left, bounds.Top + 5, 3, Math.Max(8, bounds.Height - 10));
        }

        int left = bounds.Left + 10;
        if (e.ColumnIndex == 0)
        {
            int checkSize = 14;
            var checkBounds = new Rectangle(bounds.Left + 7, bounds.Top + (bounds.Height - checkSize) / 2, checkSize, checkSize);
            ControlPaint.DrawCheckBox(g, checkBounds, item.Checked ? ButtonState.Checked : ButtonState.Normal);
            if (_items.SmallImageList is not null && item.ImageIndex >= 0)
            {
                var image = _items.SmallImageList.Images[item.ImageIndex];
                int size = 18;
                int top = bounds.Top + (bounds.Height - size) / 2;
                g.DrawImage(image, new Rectangle(bounds.Left + 29, top, size, size));
            }
            left = bounds.Left + 53;
        }

        Color textColor = selected ? Color.FromArgb(24, 58, 84) : item.ForeColor.IsEmpty ? Color.FromArgb(38, 65, 87) : item.ForeColor;
        var textBounds = Rectangle.FromLTRB(left, bounds.Top + 1, bounds.Right - 9, bounds.Bottom - 1);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
        if (header.TextAlign == HorizontalAlignment.Right) flags |= TextFormatFlags.Right;
        else if (header.TextAlign == HorizontalAlignment.Center) flags |= TextFormatFlags.HorizontalCenter;
        TextRenderer.DrawText(g, subItem.Text, item.Font, textBounds, textColor, flags);

        if (selected && e.ColumnIndex == _items.Columns.Count - 1 && _items.Focused)
            ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(bounds, -3, -3), textColor, rowColor);
    }

    private void SetGlassCaption()
    {
        try
        {
            int caption = ColorTranslator.ToWin32(Color.FromArgb(24, 75, 113));
            int text = ColorTranslator.ToWin32(Color.White);
            int border = ColorTranslator.ToWin32(Color.FromArgb(111, 180, 215));
            _ = DwmSetWindowAttribute(Handle, 33, ref caption, sizeof(int));
            _ = DwmSetWindowAttribute(Handle, 35, ref text, sizeof(int));
            _ = DwmSetWindowAttribute(Handle, 34, ref border, sizeof(int));
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int valueSize);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SetGlassCaption();
    }

    private async Task OpenDialogAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "열 압축 파일 선택",
            Filter = "압축 파일|*.zip;*.7z;*.rar;*.egg;*.alz;*.tar;*.gz;*.bz2;*.xz;*.zst;*.cab;*.arj;*.ace;*.lzh;*.arc;*.iso;*.wim;*.001|미리보기 파일|*.pdf;*.docx;*.hwpx;*.xlsx;*.pptx;*.txt;*.md;*.csv;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.mp4;*.mov;*.m4v;*.wmv;*.avi;*.mkv;*.webm;*.mp3;*.wav|모든 파일|*.*",
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
        if (IsInstaller(path) && !ConfirmInstallerExecution(path)) return;
        OpenInDefaultApp(path);
    }

    private static bool IsArchivePath(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".zip" or ".7z" or ".rar" or ".egg" or ".alz" or ".tar" or ".gz" or ".bz2" or ".xz" or ".zst" or ".cab" or ".arj" or ".ace" or ".lzh" or ".arc" or ".iso" or ".wim" or ".001";

    private bool CanOpenSelection()
    {
        if (_snapshot is null || _items.SelectedIndices.Count != 1) return false;
        var item = _snapshot.Items[_items.SelectedIndices[0]];
        return !item.IsDirectory;
    }

    private bool CanPreviewSelection()
    {
        if (!CanOpenSelection() || _snapshot is null) return false;
        return PreviewContent.CanPreview(_snapshot.Items[_items.SelectedIndices[0]].Name);
    }

    private void UpdateSelectionActions()
    {
        _previewFileButton.Enabled = !_busy && CanPreviewSelection();
        _editButton.Enabled = !_busy && _snapshot is not null;
    }

    private async Task PreviewSelectedAsync()
    {
        if (!CanPreviewSelection() || _snapshot is null) return;
        var snapshot = _snapshot;
        var item = snapshot.Items[_items.SelectedIndices[0]];
        string tempFolder = CreateOpenTempFolder();
        _operation = new CancellationTokenSource();
        var progress = new Progress<(int Done, int Total)>(value =>
        {
            _progress.Value = value.Total <= 0 ? 0 : (int)Math.Clamp(value.Done * 1000d / value.Total, 0, 1000);
            _progressText.Text = value.Total <= 0 ? string.Empty : $"{value.Done:N0} / {value.Total:N0}";
        });
        SetBusy(true, "미리보기 파일을 준비하는 중…");
        bool handedToPreview = false;
        try
        {
            await ArchiveEngineService.ExtractAsync(snapshot, [item.Index], tempFolder, _password.Text, progress, _operation.Token);
            string path = ArchiveEngineService.ResolveSafePath(tempFolder, item.Name);
            PreviewContent.EnsurePreviewSize(new FileInfo(path).Length);
            SetBusy(false, "미리보기 준비 완료");
            using var preview = new FilePreviewForm(path, item.Name, tempFolder);
            handedToPreview = true;
            preview.ShowDialog(this);
            _status.Text = "미리보기 창을 닫았습니다.";
        }
        catch (OperationCanceledException) { _status.Text = "미리보기를 취소했습니다."; }
        catch (Exception ex) { MessageBox.Show(this, FriendlyError(ex), "미리보기 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally
        {
            if (!handedToPreview) try { Directory.Delete(tempFolder, recursive: true); } catch { }
            _operation?.Dispose();
            _operation = null;
            SetBusy(false);
        }
    }

    private async Task OpenSelectedInDefaultAppAsync()
    {
        if (_snapshot is null || _items.SelectedIndices.Count != 1)
        {
            MessageBox.Show(this, "열 파일 하나를 선택하세요.", "파일 열기", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var item = _snapshot.Items[_items.SelectedIndices[0]];
        if (item.IsDirectory)
        {
            MessageBox.Show(this, "파일을 선택하세요. 폴더는 ‘선택 항목 풀기’로 임시 폴더에 풀 수 있습니다.", "파일 열기", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        bool installer = IsInstaller(item.Name);
        if (installer && !ConfirmInstallerExecution(item.Name, fromArchive: true)) return;

        string tempFolder = CreateOpenTempFolder();
        Directory.CreateDirectory(tempFolder);
        _operation = new CancellationTokenSource();
        var progress = new Progress<(int Done, int Total)>(value =>
        {
            _progress.Value = value.Total <= 0 ? 0 : (int)Math.Clamp(value.Done * 1000d / value.Total, 0, 1000);
            _progressText.Text = value.Total <= 0 ? string.Empty : $"{value.Done:N0} / {value.Total:N0}";
        });
        SetBusy(true, installer ? "설치 파일과 필요한 파일을 준비하는 중…" : "파일을 임시 폴더에 준비하는 중…");
        bool retainTemp = false;
        try
        {
            IReadOnlyCollection<int> indices = installer
                ? _snapshot.Items.Select(entry => entry.Index).ToArray()
                : [item.Index];
            await ArchiveEngineService.ExtractAsync(_snapshot, indices, tempFolder, _password.Text, progress, _operation.Token);
            string extractedPath = ArchiveEngineService.ResolveSafePath(tempFolder, item.Name);
            if (!File.Exists(extractedPath)) throw new FileNotFoundException("압축 파일에서 실행할 항목을 찾지 못했습니다.", extractedPath);
            OpenInDefaultApp(extractedPath);
            retainTemp = true;
            _status.Text = installer ? "설치 프로그램을 시작했습니다. 설치가 끝날 때까지 임시 파일을 유지합니다." : "기본 응용 프로그램에서 열었습니다. 임시 파일은 일주일 뒤 정리됩니다.";
        }
        catch (OperationCanceledException) { _status.Text = "파일 열기를 취소했습니다."; }
        catch (Exception ex) { MessageBox.Show(this, FriendlyError(ex), "파일 열기 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally
        {
            if (!retainTemp) try { Directory.Delete(tempFolder, recursive: true); } catch { }
            _operation.Dispose();
            _operation = null;
            SetBusy(false);
        }
    }

    private static string CreateOpenTempFolder()
    {
        string root = Path.Combine(Path.GetTempPath(), "myzip-open");
        Directory.CreateDirectory(root);
        try
        {
            foreach (string oldFolder in Directory.EnumerateDirectories(root))
                if (Directory.GetCreationTimeUtc(oldFolder) < DateTime.UtcNow.AddDays(-7))
                    try { Directory.Delete(oldFolder, recursive: true); } catch { }
        }
        catch { }
        string folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static bool IsExecutable(string path) => Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase);
    private static bool IsInstaller(string path) => Path.GetExtension(path).Equals(".msi", StringComparison.OrdinalIgnoreCase) || IsExecutable(path);

    private bool ConfirmInstallerExecution(string path, bool fromArchive = false) => MessageBox.Show(this,
        $"{(fromArchive ? "압축 파일의 " : string.Empty)}실행 파일을 시작합니다. 프로그램이 설치되거나 시스템을 변경할 수 있습니다. 계속할까요?\r\n\r\n{path}",
        "설치 파일 실행 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private static void OpenInDefaultApp(string path)
    {
        string extension = Path.GetExtension(path);
        if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
        {
            var installer = new ProcessStartInfo("msiexec.exe") { UseShellExecute = false };
            installer.ArgumentList.Add("/i");
            installer.ArgumentList.Add(Path.GetFullPath(path));
            Process.Start(installer);
            return;
        }
        Process.Start(new ProcessStartInfo(Path.GetFullPath(path)) { UseShellExecute = true, Verb = "open" });
    }

    private async Task OpenArchiveAsync(string path)
    {
        if (_editChanges && MessageBox.Show(this, "저장하지 않은 압축 내용 변경이 있습니다. 버리고 새 압축 파일을 열까요?", "변경 내용 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            SetBusy(true, "압축 파일을 읽는 중…");
            var snapshot = await Task.Run(() => ArchiveEngineService.Open(path, _password.Text));
            CleanupEditWorkspace();
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

    private void BuildExtractMenu()
    {
        _extractMenu.Items.Add("알아서 풀기 · 압축파일 이름 폴더").Click += async (_, _) => await ExtractSelectedAsync(ExtractMode.AutoFolder);
        _extractMenu.Items.Add("현재 폴더에 바로 풀기").Click += async (_, _) => await ExtractSelectedAsync(ExtractMode.SameFolder);
        _extractMenu.Items.Add("폴더 선택해서 풀기…").Click += async (_, _) => await ExtractSelectedAsync(ExtractMode.ChooseFolder);
    }

    private void BuildEditMenu()
    {
        _editMenu.Items.Add("선택 항목 삭제").Click += async (_, _) => await DeleteSelectedFromArchiveAsync();
        _editMenu.Items.Add("파일 추가…").Click += async (_, _) => await AddFilesToArchiveAsync();
        _editMenu.Items.Add("변경 내용 재압축…").Click += async (_, _) => await SaveArchiveChangesAsync();
    }

    private void BuildCreateMenu()
    {
        _createMenu.Items.Add("파일 압축…").Click += async (_, _) => await CreateFromFilesDialogAsync();
        _createMenu.Items.Add("폴더 압축…").Click += async (_, _) => await CreateFromFolderDialogAsync();
    }

    private void BuildItemMenu()
    {
        _itemMenu.Items.Add("미리보기").Click += async (_, _) => await PreviewSelectedAsync();
        _itemMenu.Items.Add("기본 앱에서 열기").Click += async (_, _) => await OpenSelectedInDefaultAppAsync();
        _itemMenu.Opening += (_, _) =>
        {
            bool selected = CanOpenSelection();
            _itemMenu.Items[0].Enabled = !_busy && CanPreviewSelection();
            _itemMenu.Items[1].Enabled = !_busy && selected;
        };
        _items.ContextMenuStrip = _itemMenu;
    }

    private async Task EnsureEditWorkspaceAsync()
    {
        if (_editWorkspace is not null) return;
        var snapshot = _snapshot ?? throw new InvalidOperationException("먼저 압축 파일을 여세요.");
        string workspace = CreateOpenTempFolder();
        _operation = new CancellationTokenSource();
        try
        {
            var progress = new Progress<(int Done, int Total)>(value =>
            {
                _progress.Value = value.Total <= 0 ? 0 : (int)Math.Clamp(value.Done * 1000d / value.Total, 0, 1000);
                _progressText.Text = value.Total <= 0 ? string.Empty : $"{value.Done:N0} / {value.Total:N0}";
            });
            SetBusy(true, "편집용 파일을 준비하는 중…");
            int count = await ArchiveEngineService.ExtractAsync(snapshot, snapshot.Items.Select(item => item.Index).ToArray(), workspace, _password.Text, progress, _operation.Token);
            _editWorkspace = workspace;
            _status.Text = $"편집 준비 완료 · {count:N0}개 항목을 임시 작업 폴더에 풀었습니다.";
        }
        catch
        {
            try { Directory.Delete(workspace, recursive: true); } catch { }
            throw;
        }
        finally
        {
            _operation?.Dispose();
            _operation = null;
            SetBusy(false);
        }
    }

    private async Task DeleteSelectedFromArchiveAsync()
    {
        var snapshot = _snapshot;
        if (snapshot is null) { MessageBox.Show(this, "먼저 압축 파일을 여세요."); return; }
        var selected = GetCheckedIndices().ToHashSet();
        if (selected.Count == 0) { MessageBox.Show(this, "삭제할 항목의 체크 칸을 선택하세요."); return; }
        if (MessageBox.Show(this, $"체크한 {selected.Count:N0}개 항목을 압축 내용에서 삭제할까요? 원본 압축 파일은 ‘변경 내용 재압축’을 할 때까지 바뀌지 않습니다.", "압축 내용 편집", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            await EnsureEditWorkspaceAsync();
            foreach (var item in snapshot.Items.Where(item => selected.Contains(item.Index)))
            {
                string path = ArchiveEngineService.ResolveSafePath(_editWorkspace!, item.Name);
                if (item.IsDirectory)
                {
                    if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
                }
                else if (File.Exists(path)) File.Delete(path);
            }
            _editChanges = true;
            _status.Text = $"체크한 {selected.Count:N0}개 항목을 제거했습니다. ‘파일 편집’에서 변경 내용을 재압축하세요.";
        }
        catch (Exception ex) { MessageBox.Show(this, FriendlyError(ex), "항목 삭제 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task AddFilesToArchiveAsync()
    {
        if (_snapshot is null) { MessageBox.Show(this, "먼저 압축 파일을 여세요."); return; }
        using var dialog = new OpenFileDialog { Title = "압축 파일에 추가할 파일 선택", Multiselect = true, CheckFileExists = true, Filter = "모든 파일|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (dialog.FileNames.Any(path => string.Equals(Path.GetFullPath(path), Path.GetFullPath(_snapshot.Path), StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "열려 있는 압축 파일 자체는 다시 추가할 수 없습니다.", "파일 추가", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            await EnsureEditWorkspaceAsync();
            var conflicts = dialog.FileNames.Select(path => Path.Combine(_editWorkspace!, Path.GetFileName(path))).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            bool overwrite = conflicts.Length == 0 || MessageBox.Show(this, $"같은 이름의 파일 {conflicts.Length:N0}개가 있습니다. 기존 항목을 덮어쓸까요?", "파일 추가", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
            if (!overwrite && conflicts.Length > 0) return;
            foreach (string source in dialog.FileNames)
                File.Copy(source, Path.Combine(_editWorkspace!, Path.GetFileName(source)), overwrite: true);
            _editChanges = true;
            _status.Text = $"파일 {dialog.FileNames.Length:N0}개를 추가했습니다. ‘파일 편집’에서 변경 내용을 재압축하세요.";
        }
        catch (Exception ex) { MessageBox.Show(this, FriendlyError(ex), "파일 추가 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task SaveArchiveChangesAsync()
    {
        var snapshot = _snapshot;
        if (snapshot is null) { MessageBox.Show(this, "먼저 압축 파일을 여세요."); return; }
        if (!_editChanges) { MessageBox.Show(this, "먼저 항목을 삭제하거나 파일을 추가하세요.", "변경 내용 재압축", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (snapshot.Items.Any(item => item.IsEncrypted) && MessageBox.Show(this, "새 압축 파일은 암호화되지 않습니다. 계속할까요?", "암호 보호 안내", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        using var save = new SaveFileDialog
        {
            Title = "편집한 압축 파일 저장",
            Filter = "ZIP 압축 파일 (*.zip)|*.zip|7-Zip 압축 파일 (*.7z)|*.7z",
            FilterIndex = Path.GetExtension(snapshot.Path).Equals(".7z", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
            DefaultExt = Path.GetExtension(snapshot.Path).Equals(".7z", StringComparison.OrdinalIgnoreCase) ? "7z" : "zip",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = Path.GetFileNameWithoutExtension(snapshot.Path) + "-편집"
        };
        if (save.ShowDialog(this) != DialogResult.OK) return;
        ArchiveType type = save.FilterIndex == 2 ? ArchiveType.SevenZip : ArchiveType.Zip;
        string tempOutput = Path.Combine(Path.GetDirectoryName(save.FileName)!, $".{Path.GetFileName(save.FileName)}.{Guid.NewGuid():N}.tmp");
        int level = _compressionProfile.SelectedIndex switch { 1 => 5, 2 => 9, _ => 1 };
        bool savedSuccessfully = false;
        try
        {
            await RunOperationAsync("편집한 내용을 다시 압축하는 중…", async (progress, token) =>
            {
                int count = await ArchiveEngineService.CreateFromWorkspaceAsync(_editWorkspace!, tempOutput, type, level, progress, token);
                File.Move(tempOutput, save.FileName, overwrite: true);
                savedSuccessfully = true;
                return $"{count:N0}개 파일을 재압축했습니다: {save.FileName}";
            });
            if (savedSuccessfully)
            {
                _editChanges = false;
                CleanupEditWorkspace();
                await OpenArchiveAsync(save.FileName);
            }
        }
        catch (Exception ex) { MessageBox.Show(this, FriendlyError(ex), "재압축 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally { try { if (File.Exists(tempOutput)) File.Delete(tempOutput); } catch { } }
    }

    private void CleanupEditWorkspace()
    {
        if (_editWorkspace is not null)
            try { Directory.Delete(_editWorkspace, recursive: true); } catch { }
        _editWorkspace = null;
        _editChanges = false;
    }

    private async Task ExtractSelectedAsync(ExtractMode mode)
    {
        var snapshot = _snapshot;
        if (snapshot is null) { MessageBox.Show(this, "먼저 압축 파일을 여세요."); return; }
        var indices = GetCheckedIndices();
        bool extractAll = indices.Count == 0;
        if (extractAll) indices = snapshot.Items.Select(i => i.Index).ToList();
        string archiveFolder = Path.GetDirectoryName(snapshot.Path) ?? Environment.CurrentDirectory;
        string destination = mode switch
        {
            ExtractMode.AutoFolder => Path.Combine(archiveFolder, Path.GetFileNameWithoutExtension(snapshot.Path)),
            ExtractMode.SameFolder => archiveFolder,
            ExtractMode.ChooseFolder => ChooseFolder("압축을 풀 위치를 선택하세요. 새 폴더도 만들 수 있습니다.", archiveFolder) ?? string.Empty,
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(destination)) return;
        if (File.Exists(destination)) { MessageBox.Show(this, "같은 이름의 파일이 있어 저장 폴더로 사용할 수 없습니다.", "압축 풀기", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var selectedIndices = indices.ToHashSet();
        var conflicts = new List<string>();
        try
        {
            foreach (var item in snapshot.Items.Where(item => selectedIndices.Contains(item.Index) && !item.IsDirectory))
            {
                string target = ArchiveEngineService.ResolveSafePath(destination, item.Name);
                if (string.Equals(Path.GetFullPath(target), Path.GetFullPath(snapshot.Path), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "압축 파일 자체를 덮어쓸 수 있는 항목이 포함되어 있어 풀기를 중단했습니다.", "압축 풀기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (File.Exists(target)) conflicts.Add(target);
            }
        }
        catch (Exception ex) { MessageBox.Show(this, FriendlyError(ex), "저장 경로 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        conflicts = conflicts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (conflicts.Count > 0)
        {
            string message = $"대상 폴더에 같은 이름의 파일 {conflicts.Count:N0}개가 있습니다. 기존 파일을 덮어쓸까요?";
            if (MessageBox.Show(this, message, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        }
        await RunOperationAsync("압축 해제 중…", async (progress, token) =>
        {
            int count = await ArchiveEngineService.ExtractAsync(snapshot, indices, destination, _password.Text, progress, token);
            return extractAll
                ? $"전체 항목 {count:N0}개를 풀었습니다: {destination}"
                : $"체크한 항목 {count:N0}개를 풀었습니다: {destination}";
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

    private List<int> GetCheckedIndices() => _items.CheckedItems.Cast<ListViewItem>()
        .Select(row => (int)row.Tag!)
        .ToList();

    private static string? ChooseFolder(string description, string? initialPath = null)
    {
        using var dialog = new FolderBrowserDialog { Description = description, ShowNewFolderButton = true, SelectedPath = initialPath ?? string.Empty };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }

    private enum ExtractMode { AutoFolder, SameFolder, ChooseFolder }

    private void SetBusy(bool busy, string? status = null)
    {
        _busy = busy;
        _openButton.Enabled = !busy;
        _extractButton.Enabled = !busy && _snapshot is not null;
        _previewFileButton.Enabled = !busy && CanPreviewSelection();
        _editButton.Enabled = !busy && _snapshot is not null;
        _createButton.Enabled = !busy;
        _testButton.Enabled = !busy && _snapshot is not null;
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
        var button = new GlassActionButton
        {
            Text = label,
            Image = MenuIconFactory.Create(icon, primary ? Color.White : Color.FromArgb(24, 126, 182), 28),
            Primary = primary,
            Size = new Size(148, 82),
            Margin = new Padding(4, 0, 4, 0),
            ForeColor = primary ? Color.White : Color.FromArgb(31, 66, 91),
            Font = new Font("Segoe UI", 9.1f, FontStyle.Bold),
            AccessibleName = label
        };
        return button;
    }

    private static Control Separator() => new Panel { Width = 1, Height = 35, BackColor = Color.FromArgb(158, 255, 255, 255), Margin = new Padding(8, 20, 8, 0) };

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
