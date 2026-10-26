// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;

namespace RamzXOtp.Qr
{
    /// <summary>QR error correction level. Order matches the table rows below.</summary>
    public enum QrEcLevel
    {
        L = 0,
        M = 1,
        Q = 2,
        H = 3
    }

    /// <summary>
    /// Constants of the QR code symbology (versions 1-40), taken from the published QR code
    /// tables (error correction blocks, alignment pattern positions, BCH format/version info).
    /// </summary>
    internal static class QrTables
    {
        internal const int MinVersion = 1;
        internal const int MaxVersion = 40;

        // Error correction codewords per block, index [level, version] (version 0 unused).
        private static readonly byte[,] EccPerBlock =
        {
            { 0, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            { 0, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
            { 0, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            { 0, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        };

        // Number of error correction blocks, index [level, version].
        private static readonly byte[,] NumBlocks =
        {
            { 0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
            { 0, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
            { 0, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
            { 0, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 },
        };

        internal static int Size(int version)
        {
            CheckVersion(version);
            return 17 + 4 * version;
        }

        internal static int VersionForSize(int size)
        {
            if (size < 21 || size > 177 || (size - 17) % 4 != 0) return -1;
            return (size - 17) / 4;
        }

        /// <summary>Modules available for data + error correction (excluding all function patterns).</summary>
        internal static int RawDataModules(int version)
        {
            CheckVersion(version);
            int result = (16 * version + 128) * version + 64;
            if (version >= 2)
            {
                int numAlign = version / 7 + 2;
                result -= (25 * numAlign - 10) * numAlign - 55;
                if (version >= 7) result -= 36;
            }
            return result;
        }

        internal static int TotalCodewords(int version) { return RawDataModules(version) / 8; }

        internal static int EccCodewordsPerBlock(QrEcLevel level, int version) { return EccPerBlock[(int)level, version]; }

        internal static int BlockCount(QrEcLevel level, int version) { return NumBlocks[(int)level, version]; }

        internal static int DataCodewords(QrEcLevel level, int version)
        {
            return TotalCodewords(version) - EccCodewordsPerBlock(level, version) * BlockCount(level, version);
        }

        /// <summary>Centre coordinates of alignment patterns (same list for rows and columns).</summary>
        internal static int[] AlignmentPositions(int version)
        {
            CheckVersion(version);
            if (version == 1) return new int[0];

            int numAlign = version / 7 + 2;
            int step = version == 32 ? 26 : (version * 4 + numAlign * 2 + 1) / (numAlign * 2 - 2) * 2;
            int[] result = new int[numAlign];
            result[0] = 6;
            for (int i = numAlign - 1, pos = Size(version) - 7; i >= 1; --i, pos -= step) result[i] = pos;
            return result;
        }

        /// <summary>Two-bit level indicator stored in the format information.</summary>
        internal static int FormatLevelBits(QrEcLevel level)
        {
            switch (level)
            {
                case QrEcLevel.L: return 1;
                case QrEcLevel.M: return 0;
                case QrEcLevel.Q: return 3;
                default: return 2;
            }
        }

        /// <summary>15-bit format information (BCH(15,5) code, XOR mask 101010000010010).</summary>
        internal static int FormatBits(QrEcLevel level, int mask)
        {
            int data = FormatLevelBits(level) << 3 | mask;
            int rem = data;
            for (int i = 0; i < 10; ++i) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
            return (data << 10 | rem) ^ 0x5412;
        }

        /// <summary>18-bit version information (BCH(18,6) code), versions 7-40.</summary>
        internal static int VersionBits(int version)
        {
            int rem = version;
            for (int i = 0; i < 12; ++i) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
            return version << 12 | rem;
        }

        /// <summary>Data mask condition for module column x, row y.</summary>
        internal static bool Mask(int mask, int x, int y)
        {
            switch (mask)
            {
                case 0: return (x + y) % 2 == 0;
                case 1: return y % 2 == 0;
                case 2: return x % 3 == 0;
                case 3: return (x + y) % 3 == 0;
                case 4: return (x / 3 + y / 2) % 2 == 0;
                case 5: return x * y % 2 + x * y % 3 == 0;
                case 6: return (x * y % 2 + x * y % 3) % 2 == 0;
                case 7: return ((x + y) % 2 + x * y % 3) % 2 == 0;
                default: throw new ArgumentOutOfRangeException("mask");
            }
        }

        internal static int BitCount(int v)
        {
            int c = 0;
            while (v != 0) { c += v & 1; v >>= 1; }
            return c;
        }

        private static void CheckVersion(int version)
        {
            if (version < MinVersion || version > MaxVersion) throw new ArgumentOutOfRangeException("version");
        }
    }
}
