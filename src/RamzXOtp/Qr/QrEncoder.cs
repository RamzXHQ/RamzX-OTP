// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;

namespace RamzXOtp.Qr
{
    /// <summary>A finished QR symbol (dark = true).</summary>
    public sealed class QrCode
    {
        private readonly bool[,] m_modules;

        public int Version { get; private set; }
        public int Size { get; private set; }
        public QrEcLevel Level { get; private set; }
        public int Mask { get; private set; }

        internal QrCode(QrMatrix m, QrEcLevel level, int mask)
        {
            Version = m.Version;
            Size = m.Size;
            Level = level;
            Mask = mask;
            m_modules = (bool[,])m.Dark.Clone();
        }

        /// <summary>Module at column x, row y.</summary>
        public bool this[int x, int y] { get { return m_modules[x, y]; } }
    }

    /// <summary>
    /// QR code generator (used for "show as QR" and to test the decoder). Single segment,
    /// smallest version that fits, mask chosen by the standard penalty rules unless given.
    /// </summary>
    public static class QrEncoder
    {
        public static QrCode Encode(string text, QrEcLevel level = QrEcLevel.M, int mask = -1,
            int minVersion = 1, QrMode mode = QrMode.Byte)
        {
            if (text == null) throw new ArgumentNullException("text");
            if (mask < -1 || mask > 7) throw new ArgumentOutOfRangeException("mask");

            for (int version = Math.Max(1, minVersion); version <= QrTables.MaxVersion; ++version)
            {
                List<bool> bits = QrBitstream.EncodeSegment(text, mode, version);
                int dataCodewords = QrTables.DataCodewords(level, version);
                if (bits.Count > dataCodewords * 8) continue;

                byte[] data = QrBitstream.Finish(bits, dataCodewords);
                byte[] all = QrCodewords.AddEccAndInterleave(data, version, level);

                QrMatrix m = new QrMatrix(version);
                List<Point> pos = m.DataPositions();
                for (int i = 0; i < all.Length * 8; ++i)
                    m.Dark[pos[i].X, pos[i].Y] = ((all[i >> 3] >> (7 - (i & 7))) & 1) != 0;

                if (mask < 0) mask = ChooseMask(m, level);
                m.ApplyMask(mask);
                m.DrawFormatBits(QrTables.FormatBits(level, mask));
                return new QrCode(m, level, mask);
            }
            throw new ArgumentException("Text too long for a QR code.");
        }

        private static int ChooseMask(QrMatrix m, QrEcLevel level)
        {
            int best = 0, bestScore = int.MaxValue;
            for (int k = 0; k < 8; ++k)
            {
                m.ApplyMask(k);
                m.DrawFormatBits(QrTables.FormatBits(level, k));
                int score = Penalty(m);
                if (score < bestScore) { bestScore = score; best = k; }
                m.ApplyMask(k); // undo (XOR)
            }
            return best;
        }

        // Penalty rules N1-N4 of the QR specification.
        internal static int Penalty(QrMatrix m)
        {
            int n = m.Size, score = 0, dark = 0;
            for (int pass = 0; pass < 2; ++pass)
                for (int a = 0; a < n; ++a)
                {
                    int run = 0;
                    bool prev = false;
                    int pattern = 0;
                    for (int b = 0; b < n; ++b)
                    {
                        bool v = pass == 0 ? m.Dark[b, a] : m.Dark[a, b];
                        if (b > 0 && v == prev) ++run;
                        else { if (run >= 5) score += run - 2; run = 1; }
                        prev = v;

                        // N3: 1:1:3:1:1 finder-like pattern with 4 light modules on one side.
                        pattern = ((pattern << 1) | (v ? 1 : 0)) & 0x7FF;
                        if (b >= 10 && (pattern == 0x5D0 || pattern == 0x05D)) score += 40;
                    }
                    if (run >= 5) score += run - 2;
                }

            for (int y = 0; y < n; ++y)
                for (int x = 0; x < n; ++x)
                {
                    if (m.Dark[x, y]) ++dark;
                    if (x + 1 < n && y + 1 < n)
                    {
                        bool c = m.Dark[x, y];
                        if (c == m.Dark[x + 1, y] && c == m.Dark[x, y + 1] && c == m.Dark[x + 1, y + 1]) score += 3;
                    }
                }

            int total = n * n;
            int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
            score += Math.Max(0, k) * 10;
            return score;
        }
    }
}
