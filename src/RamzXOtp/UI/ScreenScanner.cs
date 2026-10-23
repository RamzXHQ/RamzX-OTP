// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using RamzXOtp.Qr;

namespace RamzXOtp.UI
{
    /// <summary>
    /// "Click on the QR code": shows an almost invisible overlay on every monitor, waits for one
    /// click, hides itself and reads the screen around the click (then the whole monitor).
    /// Screen pixels are only read after the user's click, kept in memory and never saved.
    /// </summary>
    internal static class ScreenScanner
    {
        private const int RegionSize = 1000; // physical pixels around the cursor

        /// <summary>Text of the QR code the user clicked on; null if cancelled or none found.</summary>
        internal static string PickAndDecode(Form owner, out bool cancelled)
        {
            cancelled = false;
            Point? click = WaitForClick();
            if (click == null) { cancelled = true; return null; }

            // Let the overlay disappear from the screen before reading it.
            Application.DoEvents();
            Thread.Sleep(150);

            using (new PerMonitorDpiScope())
            {
                NativeMethods.POINT p;
                NativeMethods.GetCursorPos(out p);

                string s = DecodeRect(new Rectangle(p.X - RegionSize / 2, p.Y - RegionSize / 2, RegionSize, RegionSize));
                if (s != null) return s;

                IntPtr mon = NativeMethods.MonitorFromPoint(p, 2 /* MONITOR_DEFAULTTONEAREST */);
                NativeMethods.MONITORINFO mi = new NativeMethods.MONITORINFO();
                mi.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO));
                if (NativeMethods.GetMonitorInfo(mon, ref mi))
                    return DecodeRect(Rectangle.FromLTRB(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Right, mi.rcMonitor.Bottom));
            }
            return null;
        }

        private static string DecodeRect(Rectangle r)
        {
            Rectangle virt = VirtualScreenPhysical();
            r.Intersect(virt);
            if (r.Width < 21 || r.Height < 21) return null;

            using (Bitmap bmp = new Bitmap(r.Width, r.Height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                    g.CopyFromScreen(r.Left, r.Top, 0, 0, r.Size, CopyPixelOperation.SourceCopy);
                return QrDecoder.Decode(bmp);
            }
        }

        private static Rectangle VirtualScreenPhysical()
        {
            return new Rectangle(NativeMethods.GetSystemMetrics(76), NativeMethods.GetSystemMetrics(77),
                NativeMethods.GetSystemMetrics(78), NativeMethods.GetSystemMetrics(79));
        }

        // One overlay per monitor; returns after the first click (null on Esc).
        private static Point? WaitForClick()
        {
            Point? result = null;
            bool done = false;
            Form[] overlays = new Form[Screen.AllScreens.Length];
            Form hint = CreateHint();

            for (int i = 0; i < overlays.Length; ++i)
            {
                Form f = new Form();
                f.FormBorderStyle = FormBorderStyle.None;
                f.StartPosition = FormStartPosition.Manual;
                f.Bounds = Screen.AllScreens[i].Bounds;
                f.TopMost = true;
                f.ShowInTaskbar = false;
                f.BackColor = Color.Black;
                f.Opacity = 0.01;           // invisible, but receives the click
                f.Cursor = Cursors.Cross;
                f.KeyPreview = true;
                f.MouseDown += (s, e) => { result = Cursor.Position; done = true; };
                f.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) done = true; };
                f.Deactivate += (s, e) => { };
                overlays[i] = f;
                f.Show();
            }
            hint.Show();
            overlays[0].Activate();

            while (!done && Array.Exists(overlays, f => f.Visible))
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }

            foreach (Form f in overlays) { f.Hide(); f.Dispose(); }
            hint.Hide();
            hint.Dispose();
            return result;
        }

        private static Form CreateHint()
        {
            Form h = new Form();
            h.FormBorderStyle = FormBorderStyle.None;
            h.StartPosition = FormStartPosition.Manual;
            h.TopMost = true;
            h.ShowInTaskbar = false;
            h.BackColor = Color.FromArgb(32, 32, 32);
            h.AutoSize = true;
            h.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            h.Padding = new Padding(14, 8, 14, 8);
            if (Strings.RightToLeft) { h.RightToLeft = RightToLeft.Yes; h.RightToLeftLayout = true; }

            Label l = new Label();
            l.AutoSize = true;
            l.ForeColor = Color.White;
            l.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f, FontStyle.Bold);
            l.Text = Strings.Get(Strings.ScanHint);
            h.Controls.Add(l);

            Rectangle wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            h.Location = new Point(wa.Left + wa.Width / 2 - 200, wa.Top + 12);
            return h;
        }

        /// <summary>Reads clipboard: image → QR text, or plain text (e.g. a copied otpauth:// link).</summary>
        internal static string FromClipboard(out bool hadImage)
        {
            hadImage = false;
            if (Clipboard.ContainsImage())
            {
                hadImage = true;
                using (Image img = Clipboard.GetImage())
                    return Decode(img);
            }
            return null;
        }

        /// <summary>Larger files or images are refused (protection against decompression bombs).</summary>
        internal const long MaxFileBytes = 50L * 1024 * 1024;
        internal const long MaxPixels = 60L * 1000 * 1000;

        internal static string FromFile(string path)
        {
            if (new FileInfo(path).Length > MaxFileBytes) throw new InvalidDataException(Strings.Get(Strings.ImageTooLarge));
            using (FileStream fs = File.OpenRead(path))
            using (Image img = Image.FromStream(fs, false, false)) // reads the header only; pixels are decoded later
                return Decode(img);
        }

        internal static string Decode(Image img)
        {
            if (img == null) return null;
            if ((long)img.Width * img.Height > MaxPixels) throw new InvalidDataException(Strings.Get(Strings.ImageTooLarge));
            using (Bitmap bmp = new Bitmap(img))
                return QrDecoder.Decode(bmp);
        }

        /// <summary>
        /// Temporarily makes this thread per-monitor DPI aware so cursor and screen coordinates
        /// are physical pixels on all monitors (Windows 10 1607+; no-op on older systems).
        /// </summary>
        private sealed class PerMonitorDpiScope : IDisposable
        {
            private readonly IntPtr m_old;

            internal PerMonitorDpiScope()
            {
                try { m_old = NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4)); }
                catch (EntryPointNotFoundException) { m_old = IntPtr.Zero; }
            }

            public void Dispose()
            {
                if (m_old == IntPtr.Zero) return;
                try { NativeMethods.SetThreadDpiAwarenessContext(m_old); }
                catch (EntryPointNotFoundException) { }
            }
        }

        private static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct POINT { internal int X, Y; }

            [StructLayout(LayoutKind.Sequential)]
            internal struct RECT { internal int Left, Top, Right, Bottom; }

            [StructLayout(LayoutKind.Sequential)]
            internal struct MONITORINFO
            {
                internal int cbSize;
                internal RECT rcMonitor;
                internal RECT rcWork;
                internal uint dwFlags;
            }

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetCursorPos(out POINT p);

            [DllImport("user32.dll")]
            internal static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO mi);

            [DllImport("user32.dll")]
            internal static extern int GetSystemMetrics(int index);

            [DllImport("user32.dll")]
            internal static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        }
    }
}
