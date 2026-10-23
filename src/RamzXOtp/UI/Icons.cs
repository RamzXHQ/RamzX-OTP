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
