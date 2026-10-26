// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace RamzXOtp.Qr
{
    public enum QrMode
    {
        Numeric = 1,
        Alphanumeric = 2,
        Byte = 4,
        Kanji = 8
    }

    /// <summary>Encodes and decodes the segment bit stream inside the data codewords.</summary>
    internal static class QrBitstream
    {
        internal const string AlphanumericChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

        internal static int CharCountBits(QrMode mode, int version)
        {
            int i = version <= 9 ? 0 : version <= 26 ? 1 : 2;
            switch (mode)
            {
                case QrMode.Numeric: return new[] { 10, 12, 14 }[i];
                case QrMode.Alphanumeric: return new[] { 9, 11, 13 }[i];
                case QrMode.Byte: return new[] { 8, 16, 16 }[i];
                case QrMode.Kanji: return new[] { 8, 10, 12 }[i];
                default: throw new ArgumentOutOfRangeException("mode");
            }
        }

        // ---------- encoding (single segment) ----------

        internal static List<bool> EncodeSegment(string text, QrMode mode, int version)
        {
            List<bool> bits = new List<bool>();
            Append(bits, (int)mode, 4);

            if (mode == QrMode.Byte)
            {
                byte[] data = Encoding.UTF8.GetBytes(text);
                Append(bits, data.Length, CharCountBits(mode, version));
                foreach (byte b in data) Append(bits, b, 8);
            }
            else if (mode == QrMode.Numeric)
            {
                foreach (char c in text) if (c < '0' || c > '9') throw new ArgumentException("Not numeric.");
                Append(bits, text.Length, CharCountBits(mode, version));
                for (int i = 0; i < text.Length; i += 3)
                {
                    int n = Math.Min(3, text.Length - i);
                    Append(bits, int.Parse(text.Substring(i, n)), n * 3 + 1);
                }
            }
            else if (mode == QrMode.Alphanumeric)
            {
                foreach (char c in text) if (AlphanumericChars.IndexOf(c) < 0) throw new ArgumentException("Not alphanumeric.");
                Append(bits, text.Length, CharCountBits(mode, version));
                for (int i = 0; i < text.Length; i += 2)
                {
                    if (i + 1 < text.Length)
                        Append(bits, AlphanumericChars.IndexOf(text[i]) * 45 + AlphanumericChars.IndexOf(text[i + 1]), 11);
                    else Append(bits, AlphanumericChars.IndexOf(text[i]), 6);
                }
            }
            else throw new NotSupportedException("Encoding mode not supported.");

            return bits;
        }

        /// <summary>Adds terminator and padding up to the data capacity, returns the data codewords.</summary>
        internal static byte[] Finish(List<bool> bits, int dataCodewords)
        {
            int capacity = dataCodewords * 8;
            if (bits.Count > capacity) throw new ArgumentException("Data too long.");
            Append(bits, 0, Math.Min(4, capacity - bits.Count));
            Append(bits, 0, (8 - bits.Count % 8) % 8);
            for (int pad = 0xEC; bits.Count < capacity; pad ^= 0xEC ^ 0x11) Append(bits, pad, 8);

            byte[] result = new byte[dataCodewords];
            for (int i = 0; i < bits.Count; ++i)
                if (bits[i]) result[i >> 3] |= (byte)(0x80 >> (i & 7));
            return result;
        }

        private static void Append(List<bool> bits, int value, int count)
        {
            for (int i = count - 1; i >= 0; --i) bits.Add(((value >> i) & 1) != 0);
        }

        // ---------- decoding ----------

        private sealed class Reader
        {
            private readonly byte[] m_data;
            private int m_pos;

            internal Reader(byte[] data) { m_data = data; }

            internal int Available { get { return m_data.Length * 8 - m_pos; } }

            internal int Read(int n)
            {
                if (n > Available) throw new QrException("Unexpected end of data.");
                int v = 0;
                for (int i = 0; i < n; ++i, ++m_pos)
                    v = (v << 1) | ((m_data[m_pos >> 3] >> (7 - (m_pos & 7))) & 1);
                return v;
            }
        }

        /// <summary>Decodes all segments into text. Byte segments are UTF-8 unless an ECI says otherwise.</summary>
        internal static string Decode(byte[] data, int version)
        {
            Reader r = new Reader(data);
            StringBuilder sb = new StringBuilder();
            Encoding byteEncoding = null; // null = UTF-8 with Latin-1 fallback

            while (r.Available >= 4)
            {
                int mode = r.Read(4);
                if (mode == 0) break; // terminator

                switch (mode)
                {
                    case 1: DecodeNumeric(r, r.Read(CharCountBits(QrMode.Numeric, version)), sb); break;
                    case 2: DecodeAlphanumeric(r, r.Read(CharCountBits(QrMode.Alphanumeric, version)), sb); break;
                    case 4:
                    {
                        int count = r.Read(CharCountBits(QrMode.Byte, version));
                        byte[] bytes = new byte[count];
                        for (int i = 0; i < count; ++i) bytes[i] = (byte)r.Read(8);
                        sb.Append(DecodeBytes(bytes, byteEncoding));
                        break;
                    }
                    case 7: byteEncoding = EciEncoding(ReadEci(r)); break;
                    case 8: DecodeKanji(r, r.Read(CharCountBits(QrMode.Kanji, version)), sb); break;
                    case 3: r.Read(16); break;  // structured append header: ignored
                    case 5: break;              // FNC1 first position
                    case 9: r.Read(8); break;   // FNC1 second position
                    default: throw new QrException("Unknown segment mode " + mode + ".");
                }
            }
            return sb.ToString();
        }

        private static void DecodeNumeric(Reader r, int count, StringBuilder sb)
        {
            while (count > 0)
            {
                int n = Math.Min(3, count);
                int v = r.Read(n * 3 + 1);
                if (v >= (n == 3 ? 1000 : n == 2 ? 100 : 10)) throw new QrException("Invalid numeric data.");
                sb.Append(v.ToString().PadLeft(n, '0'));
                count -= n;
            }
        }

        private static void DecodeAlphanumeric(Reader r, int count, StringBuilder sb)
        {
            while (count >= 2)
            {
                int v = r.Read(11);
                if (v >= 45 * 45) throw new QrException("Invalid alphanumeric data.");
                sb.Append(AlphanumericChars[v / 45]).Append(AlphanumericChars[v % 45]);
                count -= 2;
            }
            if (count == 1)
            {
                int v = r.Read(6);
                if (v >= 45) throw new QrException("Invalid alphanumeric data.");
                sb.Append(AlphanumericChars[v]);
            }
        }

        private static void DecodeKanji(Reader r, int count, StringBuilder sb)
        {
            byte[] sjis = new byte[count * 2];
            for (int i = 0; i < count; ++i)
            {
                int v = r.Read(13);
                int c = (v / 0xC0) << 8 | (v % 0xC0);
                c += c < 0x1F00 ? 0x8140 : 0xC140;
                sjis[2 * i] = (byte)(c >> 8);
                sjis[2 * i + 1] = (byte)c;
            }
            sb.Append(Encoding.GetEncoding(932).GetString(sjis));
        }

        private static int ReadEci(Reader r)
        {
            int first = r.Read(8);
            if ((first & 0x80) == 0) return first;
            if ((first & 0xC0) == 0x80) return (first & 0x3F) << 8 | r.Read(8);
            if ((first & 0xE0) == 0xC0) return (first & 0x1F) << 16 | r.Read(16);
            throw new QrException("Invalid ECI designator.");
        }

        private static Encoding EciEncoding(int eci)
        {
            switch (eci)
            {
                case 26: return new UTF8Encoding(false, true);
                case 1:
                case 3: return Encoding.GetEncoding(28591); // ISO-8859-1
                default: return null;
            }
        }

        private static string DecodeBytes(byte[] bytes, Encoding enc)
        {
            if (enc != null)
            {
                try { return enc.GetString(bytes); }
                catch (DecoderFallbackException) { throw new QrException("Invalid text for the declared character set."); }
            }
            try { return new UTF8Encoding(false, true).GetString(bytes); }
            catch (DecoderFallbackException) { return Encoding.GetEncoding(28591).GetString(bytes); }
        }
    }
}
