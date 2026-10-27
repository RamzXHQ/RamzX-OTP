// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RamzXOtp.UI
{
    /// <summary>Icons drawn in code (no image files): the RamzX logo and small action symbols.</summary>
    internal static class Icons
    {
        private static readonly Color Blue = Color.FromArgb(37, 99, 235);
        private static readonly Color Cyan = Color.FromArgb(41, 182, 246);
        private static readonly Color Navy = Color.FromArgb(11, 18, 32);

        internal const string Website = "https://ramzx.de";

        private static int MenuSize(Control dpiSource)
        {
            float dpi = 96f;
            if (dpiSource != null)
                using (Graphics g = dpiSource.CreateGraphics()) dpi = g.DpiX;
            return (int)Math.Round(16 * dpi / 96f);
        }

        /// <summary>RamzX logo in menu size (16 px at 100 %, DPI-aware).</summary>
        internal static Bitmap Menu(Control dpiSource) { return Logo(MenuSize(dpiSource)); }

        /// <summary>Timer symbol in menu size, for the "copy OTP code" item.</summary>
        internal static Bitmap MenuTimer(Control dpiSource) { return Timer(MenuSize(dpiSource)); }

        // The "X" of the RamzX logo: two chevrons on a 460 x 460 grid (from the RamzX avatar).
        private static readonly PointF[] LeftMark =
        {
            new PointF(140, 175), new PointF(180, 175), new PointF(218, 230),
            new PointF(152, 322), new PointF(112, 322), new PointF(178, 230)
        };
        private static readonly PointF[] RightMark =
        {
            new PointF(310, 138), new PointF(348, 138), new PointF(282, 230),
            new PointF(320, 285), new PointF(282, 285), new PointF(242, 230)
        };

        /// <summary>RamzX app icon: rounded navy square with the blue "X".</summary>
        internal static Bitmap Logo(int size)
        {
            size = Math.Max(16, size);
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath p = Theme.RoundRect(new RectangleF(0, 0, size - 1, size - 1), size * 0.22f))
                using (SolidBrush b = new SolidBrush(Navy))
                    g.FillPath(b, p);

                float pad = size * 0.2f, box = size - 2 * pad;
                const float minX = 112, maxX = 348, minY = 138, maxY = 322;
                float s = Math.Min(box / (maxX - minX), box / (maxY - minY));
                float ox = pad + (box - (maxX - minX) * s) / 2f - minX * s;
                float oy = pad + (box - (maxY - minY) * s) / 2f - minY * s;
                using (LinearGradientBrush lb = new LinearGradientBrush(
                    new PointF(ox + minX * s, oy + minY * s), new PointF(ox + maxX * s, oy + maxY * s), Cyan, Blue))
                {
                    foreach (PointF[] mark in new[] { LeftMark, RightMark })
                    {
                        PointF[] pts = new PointF[mark.Length];
                        for (int i = 0; i < pts.Length; ++i) pts[i] = new PointF(ox + mark[i].X * s, oy + mark[i].Y * s);
                        g.FillPolygon(lb, pts);
                    }
                }
            }
            return bmp;
        }

        private delegate void Painter(Graphics g, float s, Pen p, Brush b);

        private static Bitmap Draw(int size, Painter paint)
        {
            size = Math.Max(16, size);
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            using (Pen p = new Pen(Blue, Math.Max(1.5f, size / 10f)))
            using (SolidBrush b = new SolidBrush(Blue))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                p.StartCap = p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                paint(g, size / 16f, p, b);
            }
            return bmp;
        }

        /// <summary>Crosshair over a small QR: "scan from screen".</summary>
        internal static Bitmap Crosshair(int size)
        {
            return Draw(size, (g, s, p, b) =>
            {
                g.DrawEllipse(p, 2.5f * s, 2.5f * s, 11f * s, 11f * s);
                g.DrawLine(p, 8f * s, 0.8f * s, 8f * s, 5f * s);
                g.DrawLine(p, 8f * s, 11f * s, 8f * s, 15.2f * s);
                g.DrawLine(p, 0.8f * s, 8f * s, 5f * s, 8f * s);
                g.DrawLine(p, 11f * s, 8f * s, 15.2f * s, 8f * s);
                g.FillRectangle(b, 6.8f * s, 6.8f * s, 2.4f * s, 2.4f * s);
            });
        }

        internal static Bitmap Clipboard(int size)
        {
            return Draw(size, (g, s, p, b) =>
            {
                g.DrawRectangle(p, 3f * s, 2.5f * s, 10f * s, 12.5f * s);
                g.FillRectangle(b, 5.5f * s, 1f * s, 5f * s, 3f * s);
                g.DrawLine(p, 5.5f * s, 7.5f * s, 10.5f * s, 7.5f * s);
                g.DrawLine(p, 5.5f * s, 10.5f * s, 9f * s, 10.5f * s);
            });
        }

        internal static Bitmap Picture(int size)
        {
            return Draw(size, (g, s, p, b) =>
            {
                g.DrawRectangle(p, 1.5f * s, 3f * s, 13f * s, 10f * s);
                g.FillEllipse(b, 4f * s, 5f * s, 3f * s, 3f * s);
                g.DrawLines(p, new[] { new PointF(2.5f * s, 12f * s), new PointF(6.5f * s, 8.5f * s),
                    new PointF(9f * s, 10.5f * s), new PointF(11f * s, 8.5f * s), new PointF(14f * s, 11.5f * s) });
            });
        }

        internal static Bitmap CopyIcon(int size)
        {
            return Draw(size, (g, s, p, b) =>
            {
                g.DrawRectangle(p, 5f * s, 5f * s, 9f * s, 9.5f * s);
                g.DrawLines(p, new[] { new PointF(3f * s, 11f * s), new PointF(2f * s, 11f * s),
                    new PointF(2f * s, 2f * s), new PointF(10.5f * s, 2f * s), new PointF(10.5f * s, 3f * s) });
            });
        }

        internal static Bitmap Timer(int size)
        {
            size = Math.Max(16, size);
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                float s = size / 16f;
                float stroke = Math.Max(1.5f, 1.6f * s);

                // Crown of the stopwatch.
                using (SolidBrush b = new SolidBrush(Blue))
                    g.FillRectangle(b, 6.5f * s, 0.5f * s, 3f * s, 2f * s);

                RectangleF dial = new RectangleF(1.5f * s, 2.5f * s, 13f * s, 13f * s);
                using (SolidBrush white = new SolidBrush(Color.White))
                    g.FillEllipse(white, dial);
                using (Pen p = new Pen(Blue, stroke))
                    g.DrawEllipse(p, dial);

                // Elapsed segment (like a countdown) and the hand.
                using (SolidBrush seg = new SolidBrush(Color.FromArgb(120, Blue)))
                    g.FillPie(seg, 3.5f * s, 4.5f * s, 9f * s, 9f * s, -90, 110);
                using (Pen hand = new Pen(Color.FromArgb(17, 24, 39), stroke))
                {
                    hand.StartCap = hand.EndCap = LineCap.Round;
                    g.DrawLine(hand, 8f * s, 9f * s, 8f * s, 5f * s);
                    g.DrawLine(hand, 8f * s, 9f * s, 10.8f * s, 10.6f * s);
                }
            }
            return bmp;
        }
    }
}
