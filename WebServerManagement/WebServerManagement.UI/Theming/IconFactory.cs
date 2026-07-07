using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WebServerManagement.UI.Theming
{
    public enum AppIcon
    {
        Add,
        Edit,
        Delete,
        Start,
        Stop,
        Pause,
        Restart,
        Folder,
        Browser,
        Log,
        Reload,
        Save,
        Import,
        Export,
        Settings,
        Exit,
        Restore,
        Cancel
    }

    /// <summary>
    /// Draws small flat-style toolbar/menu icons with GDI+ so the app ships with a crisp, colored
    /// icon set without bundling image assets. Each icon is cached after first use since ToolStrip
    /// items only ever read the <see cref="Image"/> reference, never mutate it.
    /// </summary>
    public static class IconFactory
    {
        public const int CanvasSize = 20;

        private static readonly Dictionary<AppIcon, Image> Cache = new Dictionary<AppIcon, Image>();

        private static readonly Color Green = Color.FromArgb(16, 137, 62);
        private static readonly Color Red = Color.FromArgb(196, 43, 28);
        private static readonly Color Blue = Color.FromArgb(0, 103, 192);
        private static readonly Color Amber = Color.FromArgb(196, 112, 10);
        private static readonly Color Purple = Color.FromArgb(107, 63, 178);
        private static readonly Color Teal = Color.FromArgb(0, 130, 122);
        private static readonly Color FolderAmber = Color.FromArgb(255, 172, 30);
        private static readonly Color Slate = Color.FromArgb(105, 113, 125);
        private static readonly Color Gray = Color.FromArgb(128, 134, 145);

        public static Image Get(AppIcon icon)
        {
            if (Cache.TryGetValue(icon, out var cached)) return cached;

            var bitmap = Create(g =>
            {
                switch (icon)
                {
                    case AppIcon.Add: DrawAdd(g); break;
                    case AppIcon.Edit: DrawEdit(g); break;
                    case AppIcon.Delete: DrawDelete(g); break;
                    case AppIcon.Start: DrawStart(g); break;
                    case AppIcon.Stop: DrawStop(g); break;
                    case AppIcon.Pause: DrawPause(g); break;
                    case AppIcon.Restart: DrawArrowLoop(g, Purple); break;
                    case AppIcon.Folder: DrawFolder(g); break;
                    case AppIcon.Browser: DrawBrowser(g); break;
                    case AppIcon.Log: DrawLog(g); break;
                    case AppIcon.Reload: DrawArrowLoop(g, Teal); break;
                    case AppIcon.Save: DrawSave(g); break;
                    case AppIcon.Import: DrawTrayArrow(g, down: true); break;
                    case AppIcon.Export: DrawTrayArrow(g, down: false); break;
                    case AppIcon.Settings: DrawSettings(g); break;
                    case AppIcon.Exit: DrawExit(g); break;
                    case AppIcon.Restore: DrawRestore(g); break;
                    case AppIcon.Cancel: DrawCancel(g); break;
                }
            });

            Cache[icon] = bitmap;
            return bitmap;
        }

        private static Bitmap Create(System.Action<Graphics> draw)
        {
            var bitmap = new Bitmap(CanvasSize, CanvasSize, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                draw(g);
            }
            return bitmap;
        }

        private static void DrawAdd(Graphics g)
        {
            using (var pen = new Pen(Green, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, 10, 3.5f, 10, 16.5f);
                g.DrawLine(pen, 3.5f, 10, 16.5f, 10);
            }
        }

        private static void DrawEdit(Graphics g)
        {
            using (var pen = new Pen(Blue, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, 5, 15, 13, 7);
            }
            using (var brush = new SolidBrush(Blue))
            {
                g.FillPolygon(brush, new[] { new PointF(13, 7), new PointF(16, 4), new PointF(17.5f, 5.5f), new PointF(14.5f, 8.5f) });
            }
            using (var brush = new SolidBrush(Color.FromArgb(255, 205, 60)))
            {
                g.FillPolygon(brush, new[] { new PointF(3.5f, 16.5f), new PointF(5, 15), new PointF(6.4f, 16.4f), new PointF(4.4f, 17.6f) });
            }
        }

        private static void DrawDelete(Graphics g)
        {
            using (var pen = new Pen(Red, 1.6f) { LineJoin = LineJoin.Round })
            {
                g.DrawRectangle(pen, 5, 6.5f, 10, 10);
                g.DrawLine(pen, 3.5f, 6.5f, 16.5f, 6.5f);
                g.DrawLine(pen, 8, 6.5f, 8, 4);
                g.DrawLine(pen, 12, 6.5f, 12, 4);
                g.DrawLine(pen, 8, 4, 12, 4);
                g.DrawLine(pen, 7.5f, 9, 7.5f, 14.5f);
                g.DrawLine(pen, 10, 9, 10, 14.5f);
                g.DrawLine(pen, 12.5f, 9, 12.5f, 14.5f);
            }
        }

        private static void DrawStart(Graphics g)
        {
            using (var brush = new SolidBrush(Green))
            {
                g.FillPolygon(brush, new[] { new PointF(6.5f, 4.5f), new PointF(6.5f, 15.5f), new PointF(16, 10) });
            }
        }

        private static void DrawStop(Graphics g)
        {
            using (var brush = new SolidBrush(Red))
            using (var path = RoundedRect(5, 5, 10, 10, 2.4f))
            {
                g.FillPath(brush, path);
            }
        }

        private static void DrawPause(Graphics g)
        {
            using (var brush = new SolidBrush(Amber))
            {
                g.FillRectangle(brush, 6f, 4.5f, 2.8f, 11);
                g.FillRectangle(brush, 11.2f, 4.5f, 2.8f, 11);
            }
        }

        private static void DrawArrowLoop(Graphics g, Color color)
        {
            using (var pen = new Pen(color, 2.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(pen, 4, 4, 12, 12, -40, 250);
            }
            using (var brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, new[] { new PointF(16.4f, 2.8f), new PointF(19.2f, 5.6f), new PointF(14.1f, 6.6f) });
            }
        }

        private static void DrawFolder(Graphics g)
        {
            using (var brush = new SolidBrush(FolderAmber))
            {
                g.FillRectangle(brush, 3, 6.5f, 6, 2.5f);
                g.FillRectangle(brush, 3, 8, 14, 8.5f);
            }
            using (var pen = new Pen(Color.FromArgb(200, 130, 10), 1f))
            {
                g.DrawRectangle(pen, 3, 8, 14, 8.5f);
            }
        }

        private static void DrawBrowser(Graphics g)
        {
            using (var pen = new Pen(Blue, 1.4f))
            {
                g.DrawEllipse(pen, 3, 3, 14, 14);
                g.DrawLine(pen, 3, 10, 17, 10);
                g.DrawEllipse(pen, 7, 3, 6, 14);
            }
        }

        private static void DrawLog(Graphics g)
        {
            using (var pen = new Pen(Gray, 1.4f) { LineJoin = LineJoin.Round })
            {
                g.DrawRectangle(pen, 4.5f, 2.5f, 11, 15);
                g.DrawLine(pen, 6.5f, 7, 13.5f, 7);
                g.DrawLine(pen, 6.5f, 10.2f, 13.5f, 10.2f);
                g.DrawLine(pen, 6.5f, 13.4f, 11.5f, 13.4f);
            }
        }

        private static void DrawSave(Graphics g)
        {
            using (var brush = new SolidBrush(Slate))
            using (var path = RoundedRect(3, 3, 14, 14, 2f))
            {
                g.FillPath(brush, path);
            }
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, 6, 3, 8, 4.5f);
                g.FillRectangle(brush, 5.5f, 11, 9, 6);
            }
            using (var pen = new Pen(Slate, 1f))
            {
                g.DrawRectangle(pen, 5.5f, 11, 9, 6);
            }
        }

        private static void DrawTrayArrow(Graphics g, bool down)
        {
            var shaftTop = down ? 3.5f : 13.5f;
            var shaftBottom = down ? 12.5f : 4.5f;
            var arrowTip = down ? 15.5f : 3f;
            var arrowBase = down ? 11.5f : 7.5f;

            using (var pen = new Pen(Teal, 2.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, 10, shaftTop, 10, shaftBottom);
            }
            using (var brush = new SolidBrush(Teal))
            {
                g.FillPolygon(brush, new[] { new PointF(6.5f, arrowBase), new PointF(13.5f, arrowBase), new PointF(10, arrowTip) });
            }
            using (var pen = new Pen(Teal, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, 4, 16.5f, 16, 16.5f);
            }
        }

        private static void DrawSettings(Graphics g)
        {
            var state = g.Save();
            using (var brush = new SolidBrush(Gray))
            {
                g.TranslateTransform(10, 10);
                for (var i = 0; i < 8; i++)
                {
                    g.RotateTransform(45);
                    g.FillRectangle(brush, -1.1f, -8.6f, 2.2f, 3.4f);
                }
            }
            g.Restore(state);

            using (var pen = new Pen(Gray, 1.6f))
            {
                g.DrawEllipse(pen, 4, 4, 12, 12);
            }
            using (var brush = new SolidBrush(Gray))
            {
                g.FillEllipse(brush, 8, 8, 4, 4);
            }
        }

        private static void DrawExit(Graphics g)
        {
            using (var pen = new Pen(Red, 1.6f) { LineJoin = LineJoin.Round })
            {
                g.DrawRectangle(pen, 4, 3.5f, 7, 13);
            }
            using (var pen = new Pen(Red, 2.1f) { EndCap = LineCap.ArrowAnchor, StartCap = LineCap.Round })
            {
                g.DrawLine(pen, 8.5f, 10, 16.5f, 10);
            }
        }

        private static void DrawRestore(Graphics g)
        {
            using (var brush = new SolidBrush(Blue))
            {
                g.FillRectangle(brush, 3.5f, 4, 13, 3.2f);
            }
            using (var pen = new Pen(Blue, 1.4f) { LineJoin = LineJoin.Round })
            {
                g.DrawRectangle(pen, 3.5f, 4, 13, 12);
            }
        }

        private static void DrawCancel(Graphics g)
        {
            using (var pen = new Pen(Gray, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, 5.5f, 5.5f, 14.5f, 14.5f);
                g.DrawLine(pen, 14.5f, 5.5f, 5.5f, 14.5f);
            }
        }

        private static GraphicsPath RoundedRect(float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();
            var d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
