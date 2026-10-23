// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RamzXOtp.Otp
{
    /// <summary>One account from an export, with the reason if it cannot be imported.</summary>
    public sealed class MigrationAccount
    {
        public OtpParameters Parameters;
        /// <summary>null if importable; otherwise a short English reason.</summary>
        public string Unsupported;
    }

    /// <summary>Accounts of one export QR code. Large exports are split into several QR codes.</summary>
    public sealed class MigrationBatch
    {
        public readonly List<MigrationAccount> Accounts = new List<MigrationAccount>();
        public int BatchSize = 1;
        public int BatchIndex;
        public int BatchId;
    }

    /// <summary>
    /// Reads Google Authenticator "Transfer accounts" QR codes:
    /// otpauth-migration://offline?data=BASE64 where data is a small protobuf message
    /// (field layout as documented by the community, e.g. github.com/dim13/otpauth).
    /// Only the few fields needed are read; nothing leaves the computer.
    /// </summary>
    public static class GoogleMigration
    {
        private const string Prefix = "otpauth-migration://";

        /// <summary>A QR code holds at most ~3 KB; anything much longer is not a real export.</summary>
        public const int MaxLength = 16 * 1024;

        /// <summary>Google splits large exports into a handful of QR codes.</summary>
        public const int MaxBatchSize = 100;

        public static bool LooksLikeMigration(string text)
        {
            return text != null && text.Trim().StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public static MigrationBatch Parse(string uri)
        {
            if (!LooksLikeMigration(uri)) throw new FormatException("Not a Google Authenticator export.");
            if (uri.Length > MaxLength) throw new FormatException("The export data is too long.");

            string s = uri.Trim();
            int q = s.IndexOf('?');
            if (q < 0) throw new FormatException("The export contains no data.");

            string data = null;
            foreach (string part in s.Substring(q + 1).Split('&'))
                if (part.StartsWith("data=", StringComparison.OrdinalIgnoreCase)) data = part.Substring(5);
            if (string.IsNullOrEmpty(data)) throw new FormatException("The export contains no data.");

            byte[] payload;
            try
            {
                string b64 = Uri.UnescapeDataString(data).Replace('-', '+').Replace('_', '/').Replace(' ', '+');
                while (b64.Length % 4 != 0) b64 += "=";
                payload = Convert.FromBase64String(b64);
            }
            catch (FormatException) { throw new FormatException("The export data is damaged."); }

            try { return ParsePayload(payload); }
            catch (EndOfStreamException) { throw new FormatException("The export data is damaged."); }
            finally { Array.Clear(payload, 0, payload.Length); }
        }

        private static MigrationBatch ParsePayload(byte[] payload)
        {
            MigrationBatch batch = new MigrationBatch();
            ProtoReader r = new ProtoReader(payload, 0, payload.Length);
            while (!r.End)
            {
                int field, wire;
                r.ReadKey(out field, out wire);
                switch (field)
                {
                    case 1 when wire == 2: batch.Accounts.Add(ParseAccount(r.Sub(r.ReadLength()))); break;
                    case 3 when wire == 0: batch.BatchSize = r.ReadInt(); break;
                    case 4 when wire == 0: batch.BatchIndex = r.ReadInt(); break;
                    case 5 when wire == 0: batch.BatchId = r.ReadInt(); break;
                    default: r.Skip(wire); break;
                }
            }
            if (batch.BatchSize < 1) batch.BatchSize = 1;
            if (batch.BatchSize > MaxBatchSize || batch.BatchIndex >= batch.BatchSize)
                throw new FormatException("The export data is damaged.");
            return batch;
        }

        private static MigrationAccount ParseAccount(ProtoReader r)
        {
            OtpParameters p = new OtpParameters();
            long algorithm = 1, digits = 1, type = 2, counter = 0;
            string name = null;

            while (!r.End)
            {
                int field, wire;
                r.ReadKey(out field, out wire);
                if (wire == 2 && (field == 1 || field == 2 || field == 3))
                {
                    byte[] b = r.ReadBytes(r.ReadLength());
                    if (field == 1) p.Secret = b;
                    else if (field == 2) name = Encoding.UTF8.GetString(b);
                    else p.Issuer = Encoding.UTF8.GetString(b);
                }
                else if (wire == 0 && field >= 4 && field <= 7)
                {
                    long v = (long)r.ReadVarint();
                    if (field == 4) algorithm = v;
                    else if (field == 5) digits = v;
                    else if (field == 6) type = v;
                    else counter = v;
                }
                else r.Skip(wire);
            }

            // The name is usually "Issuer:account"; keep only the account part if the issuer matches.
            if (name != null)
            {
                int colon = name.IndexOf(':');
                if (colon > 0 && (string.IsNullOrEmpty(p.Issuer) || name.Substring(0, colon).Trim() == p.Issuer))
                {
                    if (string.IsNullOrEmpty(p.Issuer)) p.Issuer = name.Substring(0, colon).Trim();
                    name = name.Substring(colon + 1).Trim();
                }
                p.Account = name;
            }

            MigrationAccount a = new MigrationAccount { Parameters = p };

            switch (algorithm)
            {
                case 0: case 1: p.Algorithm = OtpAlgorithm.Sha1; break;
                case 2: p.Algorithm = OtpAlgorithm.Sha256; break;
                case 3: p.Algorithm = OtpAlgorithm.Sha512; break;
                default: a.Unsupported = "Algorithm MD5 is not supported by KeePass."; break;
            }

            // Enum (1 = six, 2 = eight); some exports store the plain number instead.
            switch (digits)
            {
                case 0: case 1: case 6: p.Digits = 6; break;
                case 2: case 8: p.Digits = 8; break;
                case 7: p.Digits = 7; break;
                default: a.Unsupported = "Unsupported number of digits."; break;
            }

            if (type == 1) { p.Type = OtpType.Hotp; p.Counter = (ulong)Math.Max(0, counter); }
            else p.Type = OtpType.Totp;
            p.Period = Totp.DefaultPeriod; // the export format has no period field

            if (p.Secret == null || p.Secret.Length == 0) a.Unsupported = "The account has no secret.";
            return a;
        }

        /// <summary>Minimal protobuf wire-format reader (varint, length-delimited, fixed32/64).</summary>
        internal sealed class ProtoReader
        {
            private readonly byte[] m_buf;
            private int m_pos;
            private readonly int m_end;

            internal ProtoReader(byte[] buf, int start, int length)
            {
                m_buf = buf;
                m_pos = start;
                m_end = start + length;
                if (m_end > buf.Length) throw new EndOfStreamException();
            }

            internal bool End { get { return m_pos >= m_end; } }

            internal ulong ReadVarint()
            {
                ulong v = 0;
                for (int shift = 0; shift < 64; shift += 7)
                {
                    if (m_pos >= m_end) throw new EndOfStreamException();
                    byte b = m_buf[m_pos++];
                    v |= (ulong)(b & 0x7F) << shift;
                    if ((b & 0x80) == 0) return v;
                }
                throw new FormatException("Invalid varint.");
            }

            internal void ReadKey(out int field, out int wire)
            {
                ulong key = ReadVarint();
                if (key > int.MaxValue) throw new FormatException("Invalid field number.");
                field = (int)(key >> 3);
                wire = (int)(key & 7);
            }

            /// <summary>A length prefix; must fit into the remaining data (no overflow, no huge allocation).</summary>
            internal int ReadLength()
            {
                ulong n = ReadVarint();
                if (n > (ulong)(m_end - m_pos)) throw new EndOfStreamException();
                return (int)n;
            }

            /// <summary>A non-negative number that fits into an int (counts, indices, ids).</summary>
            internal int ReadInt()
            {
                ulong v = ReadVarint();
                if (v > int.MaxValue) throw new FormatException("Number out of range.");
                return (int)v;
            }

            internal byte[] ReadBytes(int n)
            {
                if (n < 0 || n > m_end - m_pos) throw new EndOfStreamException();
                byte[] b = new byte[n];
                Array.Copy(m_buf, m_pos, b, 0, n);
                m_pos += n;
                return b;
            }

            internal ProtoReader Sub(int n)
            {
                if (n < 0 || n > m_end - m_pos) throw new EndOfStreamException();
                ProtoReader r = new ProtoReader(m_buf, m_pos, n);
                m_pos += n;
                return r;
            }

            internal void Skip(int wire)
            {
                switch (wire)
                {
                    case 0: ReadVarint(); break;
                    case 1: Advance(8); break;
                    case 2: Advance(ReadLength()); break;
                    case 5: Advance(4); break;
                    default: throw new FormatException("Unsupported protobuf wire type " + wire + ".");
                }
            }

            private void Advance(int n)
            {
                if (n < 0 || n > m_end - m_pos) throw new EndOfStreamException();
                m_pos += n;
            }
        }
    }
}
