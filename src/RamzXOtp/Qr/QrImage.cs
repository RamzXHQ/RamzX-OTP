// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RamzXOtp.Qr
{
    /// <summary>Grey values 0 (black) .. 255 (white), row-major.</summary>
    internal sealed class LuminanceImage
    {
        internal readonly int Width;
        internal readonly int Height;
        internal readonly byte[] L;

        internal LuminanceImage(int width, int height, byte[] lum)
        {
            if (width <= 0 || height <= 0 || lum == null || lum.Length != width * height)
                throw new ArgumentException("Invalid image.");
            Width = width;
            Height = height;
            L = lum;
        }

        /// <summary>Reads any GDI+ bitmap. Transparent pixels count as white (clipboard PNGs).</summary>
        internal static LuminanceImage FromBitmap(Bitmap bmp)
        {
            if (bmp == null) throw new ArgumentNullException("bmp");
            int w = bmp.Width, h = bmp.Height;
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte[] row = new byte[w * 4];
                byte[] lum = new byte[w * h];
                for (int y = 0; y < h; ++y)
                {
                    Marshal.Copy(IntPtr.Add(bd.Scan0, y * bd.Stride), row, 0, row.Length);
                    for (int x = 0; x < w; ++x)
                    {
                        int b = row[4 * x], g = row[4 * x + 1], r = row[4 * x + 2], a = row[4 * x + 3];
                        int v = (r * 299 + g * 587 + b * 114) / 1000;
                        lum[y * w + x] = (byte)((v * a + 255 * (255 - a)) / 255);
                    }
                }
                Array.Clear(row, 0, row.Length);
                return new LuminanceImage(w, h, lum);
            }
            finally { bmp.UnlockBits(bd); }
        }
    }

    /// <summary>Black/white image used by the detector.</summary>
    internal sealed class BitImage
    {
        internal readonly int Width;
        internal readonly int Height;
        private readonly bool[] m_black;

        internal BitImage(int width, int height, bool[] black)
        {
            Width = width;
            Height = height;
            m_black = black;
        }

        internal bool this[int x, int y] { get { return m_black[y * Width + x]; } }

        internal BitImage Inverted()
        {
            bool[] inv = new bool[m_black.Length];
            for (int i = 0; i < inv.Length; ++i) inv[i] = !m_black[i];
            return new BitImage(Width, Height, inv);
        }
    }

    /// <summary>Turns grey values into black/white with several strategies.</summary>
    internal static class Binarizer
    {
        /// <summary>Normal and inverted (light-on-dark) versions of global and local thresholds.</summary>
        internal static IEnumerable<BitImage> All(LuminanceImage img)
        {
            BitImage global = Global(img);
            yield return global;
            BitImage local = Local(img);
            yield return local;
            yield return global.Inverted();
            yield return local.Inverted();
        }

        /// <summary>Otsu's method: threshold that best separates the two grey value groups.</summary>
        internal static BitImage Global(LuminanceImage img)
        {
            int[] hist = new int[256];
            foreach (byte v in img.L) ++hist[v];

            long total = img.L.Length, sumAll = 0;
            for (int i = 0; i < 256; ++i) sumAll += (long)i * hist[i];

            long wB = 0, sumB = 0;
            double best = -1;
            int threshold = 127;
            for (int t = 0; t < 256; ++t)
            {
                wB += hist[t];
                if (wB == 0) continue;
                long wF = total - wB;
                if (wF == 0) break;
                sumB += (long)t * hist[t];
                double mB = (double)sumB / wB, mF = (double)(sumAll - sumB) / wF;
                double between = (double)wB * wF * (mB - mF) * (mB - mF);
                if (between > best) { best = between; threshold = t; }
            }

            bool[] black = new bool[img.L.Length];
            for (int i = 0; i < black.Length; ++i) black[i] = img.L[i] <= threshold;
            return new BitImage(img.Width, img.Height, black);
        }

        /// <summary>Local mean threshold (integral image), for uneven backgrounds.</summary>
        internal static BitImage Local(LuminanceImage img)
        {
            int w = img.Width, h = img.Height;
            long[] integral = new long[(w + 1) * (h + 1)];
            for (int y = 0; y < h; ++y)
            {
                long rowSum = 0;
                for (int x = 0; x < w; ++x)
                {
                    rowSum += img.L[y * w + x];
                    integral[(y + 1) * (w + 1) + x + 1] = integral[y * (w + 1) + x + 1] + rowSum;
                }
            }

            int r = Math.Max(8, Math.Min(w, h) / 12);
            bool[] black = new bool[w * h];
            for (int y = 0; y < h; ++y)
            {
                int y0 = Math.Max(0, y - r), y1 = Math.Min(h, y + r + 1);
                for (int x = 0; x < w; ++x)
                {
                    int x0 = Math.Max(0, x - r), x1 = Math.Min(w, x + r + 1);
                    long sum = integral[y1 * (w + 1) + x1] - integral[y0 * (w + 1) + x1]
                        - integral[y1 * (w + 1) + x0] + integral[y0 * (w + 1) + x0];
                    long count = (long)(x1 - x0) * (y1 - y0);
                    int v = img.L[y * w + x];
                    black[y * w + x] = (long)(v + 8) * count < sum; // darker than the local mean
                }
            }
            return new BitImage(w, h, black);
        }
    }
}
