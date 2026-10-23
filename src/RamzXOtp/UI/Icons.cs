// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RamzXOtp.UI
{
    /// <summary>Plugin icon drawn in code (no image files): a blue timer with a check-style hand.</summary>
    internal static class Icons
    {
        private static readonly Color Blue = Color.FromArgb(37, 99, 235);

        /// <summary>Menu icon scaled to the DPI of the given window (16 px at 100 %).</summary>
        internal static Bitmap Menu(Control dpiSource)
        {
            float dpi = 96f;
            if (dpiSource != null)
                using (Graphics g = dpiSource.CreateGraphics()) dpi = g.DpiX;
            return Timer((int)Math.Round(16 * dpi / 96f));
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
