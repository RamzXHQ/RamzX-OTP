// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;

namespace RamzXOtp.Qr
{
    /// <summary>
    /// Splits data into error correction blocks and interleaves them (encoder), and the reverse
    /// with Reed-Solomon correction (decoder). Blocks: the first ones are one data byte shorter.
    /// </summary>
    internal static class QrCodewords
    {
        internal static byte[] AddEccAndInterleave(byte[] data, int version, QrEcLevel level)
        {
            int numBlocks = QrTables.BlockCount(level, version);
            int ecc = QrTables.EccCodewordsPerBlock(level, version);
            int total = QrTables.TotalCodewords(version);
            if (data.Length != QrTables.DataCodewords(level, version)) throw new ArgumentException("Wrong data length.");

            int numShort = numBlocks - total % numBlocks;
            int shortData = total / numBlocks - ecc;

            byte[][] dataBlocks = new byte[numBlocks][];
            byte[][] eccBlocks = new byte[numBlocks][];
            for (int i = 0, k = 0; i < numBlocks; ++i)
            {
                int len = shortData + (i < numShort ? 0 : 1);
                dataBlocks[i] = new byte[len];
                Array.Copy(data, k, dataBlocks[i], 0, len);
                k += len;
                eccBlocks[i] = ReedSolomon.Encode(dataBlocks[i], ecc);
            }

            byte[] result = new byte[total];
            int pos = 0;
            for (int i = 0; i <= shortData; ++i)
                for (int j = 0; j < numBlocks; ++j)
                    if (i < dataBlocks[j].Length) result[pos++] = dataBlocks[j][i];
            for (int i = 0; i < ecc; ++i)
                for (int j = 0; j < numBlocks; ++j)
                    result[pos++] = eccBlocks[j][i];
            return result;
        }

        /// <summary>Deinterleaves and corrects; throws QrException if a block is uncorrectable.</summary>
        internal static byte[] CorrectAndExtract(byte[] codewords, int version, QrEcLevel level)
        {
            int numBlocks = QrTables.BlockCount(level, version);
            int ecc = QrTables.EccCodewordsPerBlock(level, version);
            int total = QrTables.TotalCodewords(version);
            if (codewords.Length != total) throw new ArgumentException("Wrong codeword count.");

            int numShort = numBlocks - total % numBlocks;
            int shortData = total / numBlocks - ecc;

            byte[][] blocks = new byte[numBlocks][];
            for (int j = 0; j < numBlocks; ++j) blocks[j] = new byte[shortData + (j < numShort ? 0 : 1) + ecc];

            int pos = 0;
            for (int i = 0; i <= shortData; ++i)
                for (int j = 0; j < numBlocks; ++j)
                {
                    int dataLen = blocks[j].Length - ecc;
                    if (i < dataLen) blocks[j][i] = codewords[pos++];
                }
            for (int i = 0; i < ecc; ++i)
                for (int j = 0; j < numBlocks; ++j)
                    blocks[j][blocks[j].Length - ecc + i] = codewords[pos++];

            byte[] data = new byte[QrTables.DataCodewords(level, version)];
            int k = 0;
            foreach (byte[] b in blocks)
            {
                if (!ReedSolomon.Correct(b, ecc)) throw new QrException("Too many errors in an error correction block.");
                Array.Copy(b, 0, data, k, b.Length - ecc);
                k += b.Length - ecc;
            }
            return data;
        }
    }

    /// <summary>A QR code could not be decoded.</summary>
    public sealed class QrException : Exception
    {
        public QrException(string message) : base(message) { }
    }
}
