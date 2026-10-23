// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RamzXOtp.UI
{
    internal static class Theme
    {
        internal static readonly Color Accent = Color.FromArgb(37, 99, 235);
        internal static readonly Color Text = Color.FromArgb(17, 24, 39);
        internal static readonly Color Muted = Color.FromArgb(107, 114, 128);
        internal static readonly Color Card = Color.FromArgb(244, 247, 253);
        internal static readonly Color Border = Color.FromArgb(214, 222, 238);
        internal static readonly Color Ok = Color.FromArgb(22, 163, 74);
        internal static readonly Color Warn = Color.FromArgb(217, 119, 6);
        internal static readonly Color Error = Color.FromArgb(220, 38, 38);

        internal static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            float d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    /// <summary>Large code with a draining countdown ring; click copies (raises Click).</summary>
    internal sealed class CodeDisplay : Control
    {
        private string m_code;
        private int m_remaining, m_period = 30;
        private DateTime m_copiedUntil = DateTime.MinValue;
        private readonly Timer m_flash = new Timer();

        internal CodeDisplay()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            m_flash.Interval = 1600;
            m_flash.Tick += delegate { m_flash.Stop(); Invalidate(); };
        }

        /// <summary>Code (null = none), seconds remaining and period (0 = HOTP, no ring).</summary>
        internal void SetCode(string code, int remaining, int period)
        {
            m_code = code;
            m_remaining = remaining;
            m_period = period;
            Invalidate();
        }

        internal void ShowCopied()
        {
            m_copiedUntil = DateTime.UtcNow.AddMilliseconds(1500);
            m_flash.Stop();
            m_flash.Start();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) m_flash.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF card = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath path = Theme.RoundRect(card, Height / 6f))
            using (SolidBrush bg = new SolidBrush(Theme.Card))
            using (Pen border = new Pen(Theme.Border))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }

            bool has = !string.IsNullOrEmpty(m_code);
            string text = has ? Group(m_code) : "––– –––";
            using (Font f = new Font("Consolas", Height * 0.36f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush b = new SolidBrush(has ? Theme.Text : Theme.Border))
            {
                SizeF sz = g.MeasureString(text, f);
                g.DrawString(text, f, b, Height * 0.3f, (Height - sz.Height) / 2f);
            }

            if (has && m_period > 0) DrawRing(g);

            if (DateTime.UtcNow < m_copiedUntil) DrawCopied(g);
        }

        private void DrawRing(Graphics g)
        {
            float d = Height * 0.62f;
            RectangleF r = new RectangleF(Width - d - Height * 0.25f, (Height - d) / 2f, d, d);
            float w = Math.Max(3f, Height * 0.07f);
            Color c = m_remaining <= 5 ? Theme.Error : m_remaining <= 10 ? Theme.Warn : Theme.Ok;

            using (Pen track = new Pen(Theme.Border, w))
                g.DrawEllipse(track, r);
            using (Pen arc = new Pen(c, w))
            {
                arc.StartCap = arc.EndCap = LineCap.Round;
                float sweep = 360f * m_remaining / Math.Max(1, m_period);
                if (sweep > 0) g.DrawArc(arc, r, -90, sweep);
            }
            using (Font f = new Font(Font.FontFamily, d * 0.32f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush b = new SolidBrush(c))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(m_remaining.ToString(), f, b, r, sf);
        }

        private void DrawCopied(Graphics g)
        {
            string t = Strings.Get(Strings.Copied);
            using (Font f = new Font(Font.FontFamily, Height * 0.17f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF sz = g.MeasureString(t, f);
                RectangleF r = new RectangleF(Width - sz.Width - Height * 1.25f, Height * 0.1f, sz.Width + 14, sz.Height + 4);
                using (GraphicsPath p = Theme.RoundRect(r, r.Height / 2))
                using (SolidBrush bg = new SolidBrush(Theme.Ok))
                    g.FillPath(bg, p);
                g.DrawString(t, f, Brushes.White, r.Left + 7, r.Top + 2);
            }
        }

        private static string Group(string code)
        {
            int half = code.Length / 2;
            return code.Substring(0, half) + " " + code.Substring(half);
        }
    }

    internal enum PillKind { Neutral, Ok, Warn, Error }

    /// <summary>Rounded status message with a coloured symbol.</summary>
    internal sealed class StatusPill : Control
    {
        private PillKind m_kind;

        internal StatusPill()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        internal void Set(PillKind kind, string text)
        {
            m_kind = kind;
            Text = text;
            Height = PreferredHeight(Width);
            Invalidate();
        }

        private int Pad { get { return (int)(Font.Height * 0.6f); } }

        internal int PreferredHeight(int width)
        {
            Size s = TextRenderer.MeasureText(Text ?? string.Empty, Font, new Size(Math.Max(10, width - Pad * 3 - Font.Height), 0),
                TextFormatFlags.WordBreak);
            return s.Height + Pad * 2;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int h = PreferredHeight(Width);
            if (h != Height) Height = h;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color c = m_kind == PillKind.Ok ? Theme.Ok : m_kind == PillKind.Warn ? Theme.Warn
                : m_kind == PillKind.Error ? Theme.Error : Theme.Muted;

            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath p = Theme.RoundRect(r, Math.Min(Height / 2f, Font.Height * 0.8f)))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(28, c)))
            using (Pen border = new Pen(Color.FromArgb(90, c)))
            {
                g.FillPath(bg, p);
                g.DrawPath(border, p);
            }

            // Symbol dot with a glyph.
            float d = Font.Height * 1.05f;
            bool rtl = RightToLeft == RightToLeft.Yes;
            RectangleF dot = new RectangleF(rtl ? Width - Pad - d : Pad, Pad, d, d);
            using (SolidBrush b = new SolidBrush(c)) g.FillEllipse(b, dot);
            string glyph = m_kind == PillKind.Ok ? "✓" : m_kind == PillKind.Neutral ? "i" : "!";
            using (Font gf = new Font(Font, FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(glyph, gf, Brushes.White, dot, sf);

            Rectangle text = new Rectangle(rtl ? Pad : (int)(Pad * 2 + d), Pad, Width - Pad * 3 - (int)d, Height - Pad * 2);
            TextFormatFlags flags = TextFormatFlags.WordBreak | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : 0);
            TextRenderer.DrawText(g, Text, Font, text, Theme.Text, flags);
        }
    }
}
