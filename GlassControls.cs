using System.Drawing.Drawing2D;

namespace MyZip;

internal enum GlassSurfaceTone
{
    Frost,
    Blue
}

internal sealed class GlassToolStripColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Color.FromArgb(244, 250, 255);
    public override Color MenuBorder => Color.FromArgb(157, 187, 207);
    public override Color MenuItemBorder => Color.FromArgb(116, 174, 207);
    public override Color MenuItemSelected => Color.FromArgb(211, 234, 248);
    public override Color ImageMarginGradientBegin => Color.FromArgb(233, 244, 251);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(233, 244, 251);
    public override Color ImageMarginGradientEnd => Color.FromArgb(233, 244, 251);
}

internal sealed class GlassSurfacePanel : Panel
{
    public GlassSurfaceTone Tone { get; set; }

    public GlassSurfacePanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) InvokePaintBackground(Parent, e);
        else e.Graphics.Clear(Color.FromArgb(235, 244, 250));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var bounds = new RectangleF(1, 1, Width - 3, Height - 6);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        using var shadowPath = Rounded(bounds with { Y = bounds.Y + 4 }, 15);
        using (var shadow = new SolidBrush(Color.FromArgb(20, 32, 61, 87))) g.FillPath(shadow, shadowPath);
        using var path = Rounded(bounds, 15);
        Color top = Tone == GlassSurfaceTone.Blue ? Color.FromArgb(249, 253, 255) : Color.FromArgb(255, 255, 255);
        Color bottom = Tone == GlassSurfaceTone.Blue ? Color.FromArgb(218, 235, 247) : Color.FromArgb(231, 242, 250);
        using (var fill = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical)) g.FillPath(fill, path);

        var gloss = new RectangleF(bounds.X + 2, bounds.Y + 1, bounds.Width - 4, Math.Max(8, bounds.Height * .32f));
        using var glossPath = Rounded(gloss, 13);
        using (var shine = new LinearGradientBrush(gloss, Color.FromArgb(115, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical)) g.FillPath(shine, glossPath);
        using var edge = new Pen(Color.FromArgb(205, 255, 255, 255), 1f);
        g.DrawPath(edge, path);
        using var lowerEdge = new Pen(Color.FromArgb(110, 149, 182, 205), 1f);
        g.DrawLine(lowerEdge, bounds.Left + 16, bounds.Bottom - 1, bounds.Right - 16, bounds.Bottom - 1);
    }

    internal static GraphicsPath Rounded(RectangleF bounds, float radius)
    {
        float d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class GlassActionButton : Control
{
    private bool _hovered;
    private bool _pressed;
    private Image? _image;

    public bool Primary { get; set; }
    public bool Compact { get; set; }
    public Image? Image
    {
        get => _image;
        set { _image = value; Invalidate(); }
    }

    public GlassActionButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Color.FromArgb(231, 243, 250);
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Paint an opaque toolbar-matching surface. Transparent child painting in
        // WinForms can expose the uninitialized window surface as black corners.
        e.Graphics.Clear(BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var rect = new RectangleF(2, _pressed ? 3 : 1, Width - 5, Height - 7);
        if (rect.Width <= 0 || rect.Height <= 0) return;
        using var path = GlassSurfacePanel.Rounded(rect, Compact ? 9 : 13);

        if (!_pressed)
        {
            using var shadowPath = GlassSurfacePanel.Rounded(rect with { Y = rect.Y + 4 }, Compact ? 9 : 13);
            using var shadow = new SolidBrush(Enabled ? Color.FromArgb(30, 30, 65, 94) : Color.FromArgb(8, 30, 65, 94));
            g.FillPath(shadow, shadowPath);
        }

        Color top = Primary ? Color.FromArgb(62, 174, 225) : Color.FromArgb(255, 255, 255);
        Color bottom = Primary ? Color.FromArgb(22, 117, 177) : Color.FromArgb(222, 238, 249);
        if (_hovered && Enabled)
        {
            top = Primary ? Color.FromArgb(82, 190, 236) : Color.FromArgb(255, 255, 255);
            bottom = Primary ? Color.FromArgb(31, 139, 199) : Color.FromArgb(232, 246, 254);
        }
        if (_pressed)
        {
            top = Primary ? Color.FromArgb(23, 119, 178) : Color.FromArgb(208, 229, 243);
            bottom = Primary ? Color.FromArgb(14, 92, 147) : Color.FromArgb(220, 238, 249);
        }
        if (!Enabled)
        {
            top = Color.FromArgb(239, 245, 249);
            bottom = Color.FromArgb(224, 234, 241);
        }
        using (var fill = new LinearGradientBrush(rect, top, bottom, LinearGradientMode.Vertical)) g.FillPath(fill, path);

        var gloss = new RectangleF(rect.X + 2, rect.Y + 1, rect.Width - 4, Math.Max(6, rect.Height * .35f));
        using var glossPath = GlassSurfacePanel.Rounded(gloss, Compact ? 8 : 11);
        using (var shine = new LinearGradientBrush(gloss, Primary ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(180, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical)) g.FillPath(shine, glossPath);

        using (var border = new Pen(Primary ? Color.FromArgb(145, 244, 252, 255) : Color.FromArgb(195, 255, 255, 255), 1f)) g.DrawPath(border, path);
        using (var lower = new Pen(Primary ? Color.FromArgb(90, 10, 67, 112) : Color.FromArgb(100, 105, 149, 180), 1f))
            g.DrawLine(lower, rect.Left + 12, rect.Bottom - 1, rect.Right - 12, rect.Bottom - 1);

        if (Focused && ShowFocusCues)
        {
            var focus = RectangleF.Inflate(rect, -5, -5);
            using var focusPath = GlassSurfacePanel.Rounded(focus, Compact ? 5 : 8);
            using var focusPen = new Pen(Primary ? Color.White : Color.FromArgb(27, 119, 179), 1.5f) { DashPattern = [2, 2] };
            g.DrawPath(focusPen, focusPath);
        }

        Color ink = !Enabled ? Color.FromArgb(132, 151, 166) : Primary ? Color.White : Color.FromArgb(31, 66, 91);
        if (Compact)
        {
            int iconSize = 16;
            int gap = Image is null ? 0 : 6;
            int textWidth = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            int total = iconSize + gap + textWidth;
            int x = Math.Max(4, (Width - total) / 2);
            if (Image is not null) g.DrawImage(Image, new Rectangle(x, (Height - iconSize) / 2, iconSize, iconSize));
            var textRect = new Rectangle(x + iconSize + gap, 1, Math.Max(0, Width - x - iconSize - gap - 4), Height - 4);
            TextRenderer.DrawText(g, Text, Font, textRect, ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            return;
        }

        if (Image is not null)
        {
            int iconSize = Math.Min(28, Math.Max(20, Width / 5));
            var icon = new Rectangle((Width - iconSize) / 2, (int)rect.Y + 8, iconSize, iconSize);
            using var iconShadow = new SolidBrush(Primary ? Color.FromArgb(35, 0, 31, 63) : Color.FromArgb(25, 40, 80, 110));
            var tile = new RectangleF(icon.X - 6, icon.Y - 3, icon.Width + 12, icon.Height + 7);
            using var tilePath = GlassSurfacePanel.Rounded(tile, 8);
            g.FillPath(iconShadow, tilePath);
            g.DrawImage(Image, icon);
        }

        var labelRect = new Rectangle(7, (int)rect.Y + 43, Width - 14, Math.Max(18, (int)rect.Height - 47));
        float fontSize = Font.Size;
        while (fontSize > 8)
        {
            using var candidate = new Font(Font.FontFamily, fontSize, Font.Style, GraphicsUnit.Point);
            if (TextRenderer.MeasureText(Text, candidate, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width <= labelRect.Width - 2) break;
            fontSize -= .25f;
        }
        using var labelFont = new Font(Font.FontFamily, fontSize, Font.Style, GraphicsUnit.Point);
        TextRenderer.DrawText(g, Text, labelFont, labelRect, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Space or Keys.Enter || base.IsInputKey(keyData);
    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; if (!Capture) _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Enabled) { Focus(); _pressed = true; Capture = true; Invalidate(); }
        base.OnMouseDown(e);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        bool invokeClick = e.Button == MouseButtons.Left && _pressed && ClientRectangle.Contains(e.Location) && Enabled;
        _pressed = false; Capture = false; Invalidate();
        base.OnMouseUp(e);
        if (invokeClick) OnClick(EventArgs.Empty);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && !_pressed) { _pressed = true; Invalidate(); e.Handled = true; e.SuppressKeyPress = true; }
        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        bool invokeClick = _pressed && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && Enabled;
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { _pressed = false; Invalidate(); e.Handled = true; e.SuppressKeyPress = true; }
        base.OnKeyUp(e);
        if (invokeClick) OnClick(EventArgs.Empty);
    }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
}

internal sealed class GlassProgressBar : Control
{
    private int _value;
    public int Minimum { get; set; }
    public int Maximum { get; set; } = 1000;
    public int Value
    {
        get => _value;
        set { _value = Math.Clamp(value, Minimum, Math.Max(Minimum, Maximum)); Invalidate(); }
    }

    public GlassProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Height = 12;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) InvokePaintBackground(Parent, e);
        else e.Graphics.Clear(Color.FromArgb(235, 244, 250));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new RectangleF(1, 1, Width - 3, Height - 4);
        if (track.Width < 2 || track.Height < 2) return;
        using var trackPath = GlassSurfacePanel.Rounded(track, track.Height / 2);
        using (var trackBrush = new LinearGradientBrush(track, Color.FromArgb(215, 230, 241), Color.FromArgb(237, 246, 251), LinearGradientMode.Vertical)) g.FillPath(trackBrush, trackPath);
        using (var trackPen = new Pen(Color.FromArgb(180, 255, 255, 255))) g.DrawPath(trackPen, trackPath);
        if (_value <= Minimum || Maximum <= Minimum) return;
        float amount = Math.Clamp((_value - Minimum) / (float)(Maximum - Minimum), 0, 1);
        var fill = new RectangleF(track.X + 1, track.Y + 1, Math.Max(3, (track.Width - 2) * amount), track.Height - 2);
        using var fillPath = GlassSurfacePanel.Rounded(fill, fill.Height / 2);
        using (var fillBrush = new LinearGradientBrush(fill, Color.FromArgb(84, 196, 239), Color.FromArgb(20, 119, 183), LinearGradientMode.Vertical)) g.FillPath(fillBrush, fillPath);
        using var gloss = new Pen(Color.FromArgb(120, 255, 255, 255), 1);
        g.DrawLine(gloss, fill.Left + 6, fill.Top + 1, Math.Max(fill.Left + 6, fill.Right - 6), fill.Top + 1);
    }
}
