// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;

namespace RamzXOtp.Qr
{
    /// <summary>
    /// Reads a QR code from an image (screenshot, clipboard, file). Written from the public
    /// QR code description (finder/timing/alignment patterns, BCH format info, Reed-Solomon
    /// over GF(256), segment modes); no third-party code.
    /// </summary>
    public static class QrDecoder
    {
        /// <summary>Text of the first QR code found, or null.</summary>
        public static string Decode(Bitmap bitmap)
        {
            return Decode(LuminanceImage.FromBitmap(bitmap));
        }

        internal static string Decode(LuminanceImage lum)
        {
            foreach (BitImage img in Binarizer.All(lum))
            {
                string s = DecodeFromBits(img);
                if (s != null) return s;
            }
            return null;
        }

        internal static string DecodeFromBits(BitImage img)
        {
            List<FinderPattern> finders = QrDetector.FindFinders(img);
            if (finders.Count < 3) return null;

            int attempts = 0;
            foreach (FinderPattern[] t in QrDetector.Triples(finders))
            {
                if (++attempts > 30) break;
                foreach (int dim in QrDetector.Dimensions(t))
                {
                    bool[,] grid = QrDetector.Sample(img, t, dim);
                    if (grid == null) continue;
                    try { return DecodeGrid(grid); }
                    catch (QrException) { }
                }
            }
            return null;
        }

        /// <summary>Decodes a sampled module grid [x, y]. Throws QrException on failure.</summary>
        public static string DecodeGrid(bool[,] grid)
        {
            int size = grid.GetLength(0);
            int version = QrTables.VersionForSize(size);
            if (version < 0 || grid.GetLength(1) != size) throw new QrException("Invalid symbol size.");

            QrEcLevel level;
            int mask;
            ReadFormat(grid, size, out level, out mask);
            if (version >= 7) CheckVersion(grid, size, version);

            QrMatrix layout = new QrMatrix(version);
            List<Point> pos = layout.DataPositions();
            byte[] codewords = new byte[QrTables.TotalCodewords(version)];
            for (int i = 0; i < codewords.Length * 8; ++i)
            {
                Point p = pos[i];
                if (grid[p.X, p.Y] ^ QrTables.Mask(mask, p.X, p.Y))
                    codewords[i >> 3] |= (byte)(0x80 >> (i & 7));
            }

            byte[] data = QrCodewords.CorrectAndExtract(codewords, version, level);
            return QrBitstream.Decode(data, version);
        }

        private static void ReadFormat(bool[,] grid, int size, out QrEcLevel level, out int mask)
        {
            int a = ReadBits(grid, QrMatrix.FormatPositions(size, 0));
            int b = ReadBits(grid, QrMatrix.FormatPositions(size, 1));

            int bestDist = int.MaxValue;
            level = QrEcLevel.M;
            mask = 0;
            foreach (QrEcLevel l in new[] { QrEcLevel.L, QrEcLevel.M, QrEcLevel.Q, QrEcLevel.H })
                for (int m = 0; m < 8; ++m)
                {
                    int f = QrTables.FormatBits(l, m);
                    int d = Math.Min(QrTables.BitCount(a ^ f), QrTables.BitCount(b ^ f));
                    if (d < bestDist) { bestDist = d; level = l; mask = m; }
                }
            if (bestDist > 3) throw new QrException("Format information unreadable.");
        }

        private static void CheckVersion(bool[,] grid, int size, int version)
        {
            int a = ReadBits(grid, QrMatrix.VersionPositions(size, 0));
            int b = ReadBits(grid, QrMatrix.VersionPositions(size, 1));
            int v = QrTables.VersionBits(version);
            if (Math.Min(QrTables.BitCount(a ^ v), QrTables.BitCount(b ^ v)) > 3)
                throw new QrException("Version information does not match the symbol size.");
        }

        private static int ReadBits(bool[,] grid, Point[] positions)
        {
            int v = 0;
            for (int i = 0; i < positions.Length; ++i)
                if (grid[positions[i].X, positions[i].Y]) v |= 1 << i;
            return v;
        }
    }
}
