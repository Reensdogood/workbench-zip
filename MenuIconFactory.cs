namespace MyZip;

internal enum MenuIcon
{
    OpenArchive,
    Extract,
    Verify,
    CompressFile,
    CompressFolder,
    Cancel,
    Password,
    Eye,
    Settings,
    File,
    Folder
}

internal static class MenuIconFactory
{
    public static Bitmap Create(MenuIcon kind, Color color, int size = 22)
    {
        var bitmap = new Bitmap(size, size);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        float scale = size / 24f;
        g.ScaleTransform(scale, scale);
        using var pen = new Pen(color, 1.8f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round };

        switch (kind)
        {
            case MenuIcon.OpenArchive:
                g.DrawLines(pen, [new PointF(3, 8), new PointF(3, 5), new PointF(10, 5), new PointF(12, 8), new PointF(21, 8), new PointF(21, 19), new PointF(3, 19), new PointF(3, 8)]);
                g.DrawLine(pen, 3, 10, 21, 10);
                g.DrawLine(pen, 12, 11, 12, 17);
                g.DrawLine(pen, 12, 11, 14, 12.5f);
                g.DrawLine(pen, 12, 11, 10, 12.5f);
                break;
            case MenuIcon.Extract:
                g.DrawRectangle(pen, 5, 3, 14, 12);
                g.DrawLine(pen, 12, 5, 12, 15);
                g.DrawLine(pen, 8, 11, 12, 15);
                g.DrawLine(pen, 16, 11, 12, 15);
                g.DrawLine(pen, 3, 20, 21, 20);
                g.DrawLine(pen, 3, 17, 3, 20);
                g.DrawLine(pen, 21, 17, 21, 20);
                break;
            case MenuIcon.Verify:
                g.DrawPolygon(pen, [new PointF(12, 2), new PointF(20, 5), new PointF(19, 13), new PointF(12, 21), new PointF(5, 13), new PointF(4, 5), new PointF(12, 2)]);
                g.DrawLine(pen, 8, 11, 11, 14);
                g.DrawLine(pen, 11, 14, 16, 8);
                break;
            case MenuIcon.CompressFile:
                g.DrawLines(pen, [new PointF(6, 2), new PointF(14, 2), new PointF(19, 7), new PointF(19, 21), new PointF(6, 21), new PointF(6, 2)]);
                g.DrawLine(pen, 14, 2, 14, 7);
                g.DrawLine(pen, 14, 7, 19, 7);
                g.DrawLine(pen, 12.5f, 10, 12.5f, 18);
                g.DrawLine(pen, 9.5f, 13, 15.5f, 13);
                break;
            case MenuIcon.CompressFolder:
                g.DrawLines(pen, [new PointF(3, 7), new PointF(3, 5), new PointF(10, 5), new PointF(12, 8), new PointF(21, 8), new PointF(21, 19), new PointF(3, 19), new PointF(3, 7)]);
                g.DrawLine(pen, 12, 10, 12, 17);
                g.DrawLine(pen, 9, 13.5f, 15, 13.5f);
                break;
            case MenuIcon.Cancel:
                g.DrawLine(pen, 6, 6, 18, 18);
                g.DrawLine(pen, 18, 6, 6, 18);
                break;
            case MenuIcon.Password:
                g.DrawArc(pen, 7, 3, 10, 11, 180, 180);
                g.DrawRectangle(pen, 4, 10, 16, 11);
                g.DrawEllipse(pen, 11, 14, 2, 2);
                g.DrawLine(pen, 12, 16, 12, 18);
                break;
            case MenuIcon.Eye:
                g.DrawBezier(pen, new PointF(2, 12), new PointF(6, 5), new PointF(18, 5), new PointF(22, 12));
                g.DrawBezier(pen, new PointF(2, 12), new PointF(6, 19), new PointF(18, 19), new PointF(22, 12));
                g.DrawEllipse(pen, 9, 9, 6, 6);
                break;
            case MenuIcon.Settings:
                g.DrawEllipse(pen, 5, 5, 14, 14);
                g.DrawEllipse(pen, 9, 9, 6, 6);
                g.DrawLine(pen, 12, 2, 12, 5);
                g.DrawLine(pen, 12, 19, 12, 22);
                g.DrawLine(pen, 2, 12, 5, 12);
                g.DrawLine(pen, 19, 12, 22, 12);
                g.DrawLine(pen, 5, 5, 7, 7);
                g.DrawLine(pen, 17, 17, 19, 19);
                g.DrawLine(pen, 17, 7, 19, 5);
                g.DrawLine(pen, 5, 19, 7, 17);
                break;
            case MenuIcon.File:
                g.DrawLines(pen, [new PointF(6, 2), new PointF(14, 2), new PointF(19, 7), new PointF(19, 21), new PointF(6, 21), new PointF(6, 2)]);
                g.DrawLine(pen, 14, 2, 14, 7);
                g.DrawLine(pen, 14, 7, 19, 7);
                g.DrawLine(pen, 9, 12, 16, 12);
                g.DrawLine(pen, 9, 16, 16, 16);
                break;
            case MenuIcon.Folder:
                g.DrawLines(pen, [new PointF(3, 6), new PointF(3, 4), new PointF(10, 4), new PointF(12, 7), new PointF(21, 7), new PointF(21, 19), new PointF(3, 19), new PointF(3, 6)]);
                break;
        }

        return bitmap;
    }
}
