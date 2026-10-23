// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;

namespace RamzXOtp.Qr
{
    /// <summary>
    /// Module grid of one QR version with its function patterns (finder, timing, alignment,
    /// format and version areas). Shared by the encoder (drawing) and the decoder (reading).
    /// Coordinates are [x = column, y = row].
    /// </summary>
    internal sealed class QrMatrix
    {
        internal readonly int Version;
        internal readonly int Size;
        internal readonly bool[,] Dark;
        internal readonly bool[,] IsFunction;

        internal QrMatrix(int version)
        {
            Version = version;
            Size = QrTables.Size(version);
            Dark = new bool[Size, Size];
            IsFunction = new bool[Size, Size];
            DrawFunctionPatterns();
        }

        private void SetFunction(int x, int y, bool dark)
        {
            Dark[x, y] = dark;
            IsFunction[x, y] = true;
        }

        private void DrawFunctionPatterns()
        {
            for (int i = 0; i < Size; ++i)
            {
                SetFunction(6, i, i % 2 == 0);
                SetFunction(i, 6, i % 2 == 0);
            }

            DrawFinder(3, 3);
            DrawFinder(Size - 4, 3);
            DrawFinder(3, Size - 4);

            int[] ap = QrTables.AlignmentPositions(Version);
            int n = ap.Length;
            for (int i = 0; i < n; ++i)
                for (int j = 0; j < n; ++j)
                {
                    bool nearFinder = (i == 0 && j == 0) || (i == 0 && j == n - 1) || (i == n - 1 && j == 0);
                    if (!nearFinder) DrawAlignment(ap[i], ap[j]);
                }

            DrawFormatBits(0); // reserves the area; real bits are drawn after masking
            DrawVersionBits();
        }

        // 9x9 area: finder (7x7) plus the light separator ring.
        private void DrawFinder(int cx, int cy)
        {
            for (int dy = -4; dy <= 4; ++dy)
                for (int dx = -4; dx <= 4; ++dx)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= Size || y >= Size) continue;
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    SetFunction(x, y, dist != 2 && dist != 4);
                }
        }

        private void DrawAlignment(int cx, int cy)
        {
            for (int dy = -2; dy <= 2; ++dy)
                for (int dx = -2; dx <= 2; ++dx)
                    SetFunction(cx + dx, cy + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
        }

        /// <summary>Draws both copies of the 15-bit format information and the dark module.</summary>
        internal void DrawFormatBits(int bits)
        {
            Point[] a = FormatPositions(Size, 0), b = FormatPositions(Size, 1);
            for (int i = 0; i < 15; ++i)
            {
                bool bit = ((bits >> i) & 1) != 0;
                SetFunction(a[i].X, a[i].Y, bit);
                SetFunction(b[i].X, b[i].Y, bit);
            }
            SetFunction(8, Size - 8, true);
        }

        private void DrawVersionBits()
        {
            if (Version < 7) return;
            int bits = QrTables.VersionBits(Version);
            Point[] a = VersionPositions(Size, 0), b = VersionPositions(Size, 1);
            for (int i = 0; i < 18; ++i)
            {
                bool bit = ((bits >> i) & 1) != 0;
                SetFunction(a[i].X, a[i].Y, bit);
                SetFunction(b[i].X, b[i].Y, bit);
            }
        }

        /// <summary>Module positions of format bit i (0 = least significant) in copy 0 or 1.</summary>
        internal static Point[] FormatPositions(int size, int copy)
        {
            Point[] p = new Point[15];
            if (copy == 0)
            {
                for (int i = 0; i <= 5; ++i) p[i] = new Point(8, i);
                p[6] = new Point(8, 7);
                p[7] = new Point(8, 8);
                p[8] = new Point(7, 8);
                for (int i = 9; i < 15; ++i) p[i] = new Point(14 - i, 8);
            }
            else
            {
                for (int i = 0; i < 8; ++i) p[i] = new Point(size - 1 - i, 8);
                for (int i = 8; i < 15; ++i) p[i] = new Point(8, size - 15 + i);
            }
            return p;
        }

        /// <summary>Module positions of version bit i in copy 0 (top right) or 1 (bottom left).</summary>
        internal static Point[] VersionPositions(int size, int copy)
        {
            Point[] p = new Point[18];
            for (int i = 0; i < 18; ++i)
            {
                int a = size - 11 + i % 3, b = i / 3;
                p[i] = copy == 0 ? new Point(a, b) : new Point(b, a);
            }
            return p;
        }

        /// <summary>Data module positions in placement order (two-column zigzag from bottom right).</summary>
        internal List<Point> DataPositions()
        {
            List<Point> list = new List<Point>(QrTables.RawDataModules(Version));
            for (int right = Size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5; // skip the vertical timing column
                bool upward = ((right + 1) & 2) == 0;
                for (int vert = 0; vert < Size; ++vert)
                    for (int j = 0; j < 2; ++j)
                    {
                        int x = right - j;
                        int y = upward ? Size - 1 - vert : vert;
                        if (!IsFunction[x, y]) list.Add(new Point(x, y));
                    }
            }
            return list;
        }

        internal void ApplyMask(int mask)
        {
            for (int y = 0; y < Size; ++y)
                for (int x = 0; x < Size; ++x)
                    if (!IsFunction[x, y] && QrTables.Mask(mask, x, y)) Dark[x, y] = !Dark[x, y];
        }
    }
}
