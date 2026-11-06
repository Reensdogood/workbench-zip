using System.IO;
using System.Windows.Forms.Integration;
using Microsoft.Web.WebView2.WinForms;
using MediaState = System.Windows.Controls.MediaState;
using Stretch = System.Windows.Media.Stretch;

namespace MyZip;

internal sealed class FilePreviewForm : Form
{
    private readonly string _filePath;
    private readonly string _displayName;
    private readonly string? _tempFolder;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(29, 34, 40), Margin = new Padding(0) };
    private readonly Label _notice = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, BackColor = Color.FromArgb(29, 34, 40), Padding = new Padding(24) };
    private System.Windows.Controls.MediaElement? _media;
    private WebView2? _webView;
    private PictureBox? _picture;

    public FilePreviewForm(string filePath, string displayName, string? tempFolder = null)
    {
        _filePath = filePath;
        _displayName = displayName;
        _tempFolder = tempFolder;
        Text = $"미리보기 · {displayName}";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1080, 780);
        MinimumSize = new Size(680, 480);
        BackColor = Color.White;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var header = new Label { Text = displayName, Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(39, 58, 76), TextAlign = ContentAlignment.MiddleLeft };
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(_content, 0, 1);
        Controls.Add(layout);

        FormClosed += OnPreviewClosed;
        _content.Controls.Add(_notice);
        Shown += async (_, _) => await LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        string extension = Path.GetExtension(_filePath).ToLowerInvariant();
        try
        {
            if (PreviewContent.IsImage(_filePath))
            {
                using Image source = Image.FromFile(_filePath);
                _picture = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(29, 34, 40), SizeMode = PictureBoxSizeMode.Zoom, Image = (Image)source.Clone() };
                ShowContent(_picture);
                return;
            }

            if (PreviewContent.IsVideo(_filePath))
            {
                BuildVideoPreview();
                return;
            }

            if (PreviewContent.IsPdf(_filePath))
            {
                await BuildPdfPreviewAsync();
                return;
            }

            var text = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = extension is ".docx" or ".pptx",
                DetectUrls = false,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(36, 45, 54),
                Font = new Font(extension is ".docx" or ".xlsx" or ".pptx" ? "Segoe UI" : "Consolas", 10),
                Text = await Task.Run(() => PreviewContent.ReadDocumentText(_filePath))
            };
            if (extension == ".rtf") text.LoadFile(_filePath, RichTextBoxStreamType.RichText);
            ShowContent(text);
        }
        catch (Exception ex)
        {
            _notice.Text = $"미리보기를 열지 못했습니다.\r\n{ex.GetBaseException().Message}";
        }
    }

    private void BuildVideoPreview()
    {
        _media = new System.Windows.Controls.MediaElement
        {
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Stop,
            Stretch = Stretch.Uniform,
            ScrubbingEnabled = true
        };
        _media.MediaFailed += (_, e) => BeginInvoke(new Action(() => _notice.Text = $"이 영상 형식을 재생할 수 없습니다.\r\nWindows에 적절한 미디어 코덱이 있는지 확인하세요.\r\n{e.ErrorException?.Message}"));

        var host = new ElementHost { Dock = DockStyle.Fill, Child = _media, BackColor = Color.Black };
        var play = new Button { Text = "▶ 재생", AutoSize = true, Margin = new Padding(4) };
        var pause = new Button { Text = "Ⅱ 일시 정지", AutoSize = true, Margin = new Padding(4) };
        var stop = new Button { Text = "■ 정지", AutoSize = true, Margin = new Padding(4) };
        play.Click += (_, _) => _media?.Play();
        pause.Click += (_, _) => _media?.Pause();
        stop.Click += (_, _) => _media?.Stop();
        var player = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Black };
        player.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        player.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        player.Controls.Add(host, 0, 0);
        var controls = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6, 4, 4, 4) };
        controls.Controls.AddRange([play, pause, stop]);
        player.Controls.Add(controls, 0, 1);
        ShowContent(player);
        _media.Source = new Uri(_filePath);
        _media.Play();
    }

    private async Task BuildPdfPreviewAsync()
    {
        _webView = new WebView2 { Dock = DockStyle.Fill };
        ShowContent(_webView);
        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webView.Source = new Uri(_filePath);
        }
        catch (Exception ex)
        {
            _notice.Text = $"PDF 미리보기를 초기화하지 못했습니다. WebView2 런타임 설치 여부를 확인하세요.\r\n{ex.GetBaseException().Message}";
            _content.Controls.Remove(_webView);
            _webView.Dispose();
            _webView = null;
            _content.Controls.Add(_notice);
        }
    }

    private void ShowContent(Control control)
    {
        _content.Controls.Clear();
        _content.Controls.Add(control);
    }

    private void OnPreviewClosed(object? sender, FormClosedEventArgs e)
    {
        try { _media?.Stop(); _media?.Close(); _media = null; } catch { }
        _picture?.Image?.Dispose();
        _picture = null;
        _webView?.Dispose();
        _webView = null;
        if (_tempFolder is not null)
            try { Directory.Delete(_tempFolder, recursive: true); } catch { }
    }
}
