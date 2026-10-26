// RamzX OTP - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using KeePassLib;
using KeePassLib.Security;

using RamzXOtp.Otp;
using RamzXOtp.Qr;
using RamzXOtp.Storage;
using RamzXOtp.UI;

using Xunit;

namespace RamzXOtp.Tests
{
    /// <summary>
    /// Robustness ("fuzz") tests: thousands of random and damaged inputs for every parser.
    /// Each parser may only reject input with its documented exception; anything else
    /// (IndexOutOfRange, Overflow, OutOfMemory, ...) would surface in KeePass as a crash.
    /// Fixed seeds keep the runs reproducible.
    /// </summary>
    public class FuzzTests
    {
        private static readonly string[] Fields =
        {
            "TimeOtp-Secret", "TimeOtp-Secret-Hex", "TimeOtp-Secret-Base32", "TimeOtp-Secret-Base64",
            "TimeOtp-Length", "TimeOtp-Period", "TimeOtp-Algorithm",
            "HmacOtp-Secret", "HmacOtp-Secret-Hex", "HmacOtp-Secret-Base32", "HmacOtp-Secret-Base64", "HmacOtp-Counter",
            "otp", "TOTP Seed", "TOTP Settings"
        };

        private static readonly string[] Interesting =
        {
            "", " ", "0", "-1", "1", "5", "6", "8", "9", "10", "11", "30", "86400", "86401",
            "2147483647", "2147483648", "-2147483648", "18446744073709551615", "18446744073709551616",
            "99999999999999999999999", "1e9", "0x10", "NaN", "abc", "=", "====", "%", "&", ";", "S", "30;S", "30;6",
            "0;6", "-30;8", ";", "30;", ";8", "60;99", "HMAC-SHA-1", "HMAC-SHA-256", "HMAC-SHA-512", "MD5", "sha-1",
            "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", "MZXW6YTBOI======", "AA", "A", "1", "3132", "313", "zz",
            "MTIzNDU2Nzg5MDEyMzQ1Njc4OTA=", "!!!", "\0", "\r\n", "{CMD:/x/}", "key=", "key=&", "key=GEZDGNBV&size=-1",
            "key=GEZDGNBV&step=0", "key=GEZDGNBV&step=99999999999", "key=GEZDGNBV&type=Hotp&counter=-5",
            "key=GEZDGNBV&type=Hotp&counter=18446744073709551615", "key=GEZDGNBV&encoding=Base64",
            "key=GEZDGNBV&encoding=Hex", "key=GEZDGNBV&otpHashMode=Md5", "key==&size==",
            "otpauth://", "otpauth://totp/", "otpauth://totp/x?secret=", "otpauth://totp/x?secret=GEZDGNBV&period=0",
            "otpauth://totp/x?secret=GEZDGNBV&period=-1", "otpauth://totp/x?secret=GEZDGNBV&digits=99999999999",
            "otpauth://hotp/x?secret=GEZDGNBV&counter=18446744073709551615", "otpauth://totp/%ZZ?secret=GEZDGNBV",
            "otpauth://totp/x?secret=GEZDGNBV&encoder=steam&digits=8", "otpauth://totp/x?&&&==&secret=GEZDGNBV",
            "otpauth://totp/x?secret=GEZDGNBV&issuer=%", "otpauth://totp/a:b:c?secret=GEZDGNBV&algorithm=",
        };

        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567=&;:?/%0123456789key=size=step=-";

        private static string RandomText(Random r, int max)
        {
            StringBuilder sb = new StringBuilder();
            int n = r.Next(max);
            for (int i = 0; i < n; ++i)
            {
                int k = r.Next(10);
                if (k < 6) sb.Append(Alphabet[r.Next(Alphabet.Length)]);
                else if (k < 9) sb.Append((char)r.Next(32, 127));
                else sb.Append((char)r.Next(0, 0xD800));
            }
            return sb.ToString();
        }

        private static string Value(Random r)
        {
            int k = r.Next(4);
            if (k == 0) return RandomText(r, 60);
            if (k == 1) return Interesting[r.Next(Interesting.Length)] + RandomText(r, 4);
            return Interesting[r.Next(Interesting.Length)];
        }

        [Fact]
        public void EntryFields_FromASharedDatabase_NeverThrow()
        {
            Random r = new Random(4711);
            PwDatabase db = new PwDatabase();
            db.RootGroup = new PwGroup(true, true);
            long[] times = { 0, 59, 1111111109, Totp.UnixNow(), 20000000000, long.MaxValue / 2 };

            for (int i = 0; i < 6000; ++i)
            {
                PwEntry e = new PwEntry(true, true);
                int fields = 1 + r.Next(5);
                for (int f = 0; f < fields; ++f)
                    e.Strings.Set(Fields[r.Next(Fields.Length)], new ProtectedString(r.Next(2) == 0, Value(r)));
                db.RootGroup.AddEntry(e, true);

                OtpParameters p = OtpStore.Read(e);                 // must not throw
                OtpColumnProvider.CellText(e, null);                 // runs every second in the entry list
                if (p == null) continue;

                foreach (long t in times) Assert.NotNull(p.CurrentCode(t));
                if (p.Type == OtpType.Totp) Totp.SecondsRemaining(Totp.UnixNow(), p.Period);

                PwEntry copy = new PwEntry(true, true);
                OtpStore.Write(copy, p);                             // must not throw either
                OtpParameters back = OtpStore.Read(copy);
                Assert.NotNull(back);
                Assert.Equal(p.Secret, back.Secret);
            }

            OtpStore.FindLegacy(db);
            AccountImporter.Exists(db, new byte[] { 1, 2, 3 });
        }

        [Fact]
        public void OtpAuthLinks_OnlyFormatException()
        {
            Random r = new Random(1234);
            string valid = "otpauth://totp/ACME%20Co:john@example.com?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=ACME%20Co&algorithm=SHA256&digits=8&period=60";
            for (int i = 0; i < 20000; ++i)
            {
                string s;
                switch (r.Next(3))
                {
                    case 0: s = "otpauth://" + RandomText(r, 120); break;
                    case 1: s = Mutate(r, valid); break;
                    default: s = Interesting[r.Next(Interesting.Length)] + RandomText(r, 30); break;
                }
                try
                {
                    OtpParameters p = OtpAuthUri.Parse(s);
                    Assert.NotNull(p.CurrentCode(59));
                    OtpAuthUri.Parse(OtpAuthUri.Format(p));          // whatever was accepted can be written back
                }
                catch (FormatException) { }

                OtpInput.Evaluate(s, OtpAlgorithm.Sha1, 6, 30);      // dialog logic: never throws
            }
        }

        [Fact]
        public void GoogleExports_OnlyFormatException()
        {
            Random r = new Random(99);
            List<byte> good = new List<byte>();
            Proto.Bytes(good, 1, Proto.Account(Encoding.ASCII.GetBytes("12345678901234567890"), "GitHub:alice", "GitHub", 1, 1, 2));
            Proto.Bytes(good, 1, Proto.Account(Encoding.ASCII.GetBytes("abcdefghijabcdefghij"), "bob", "Acme", 2, 2, 1, 7));
            Proto.Int(good, 3, 1);

            for (int i = 0; i < 20000; ++i)
            {
                byte[] payload;
                if (r.Next(2) == 0)
                {
                    payload = new byte[r.Next(80)];
                    r.NextBytes(payload);
                }
                else
                {
                    payload = good.ToArray();
                    int flips = 1 + r.Next(4);
                    for (int k = 0; k < flips; ++k) payload[r.Next(payload.Length)] = (byte)r.Next(256);
                    if (r.Next(4) == 0) Array.Resize(ref payload, r.Next(payload.Length));
                }
                string uri = "otpauth-migration://offline?data=" + Uri.EscapeDataString(Convert.ToBase64String(payload));
                try
                {
                    foreach (MigrationAccount a in GoogleMigration.Parse(uri).Accounts)
                        if (a.Unsupported == null) Assert.NotNull(a.Parameters.CurrentCode(59));
                }
                catch (FormatException) { }
            }
        }

        [Fact]
        public void QrGrids_OnlyQrException()
        {
            Random r = new Random(7);
            QrCode code = QrEncoder.Encode("otpauth://totp/Test:me?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Test");
            for (int i = 0; i < 3000; ++i)
            {
                bool[,] grid;
                if (r.Next(2) == 0)
                {
                    int size = 21 + 4 * r.Next(40);
                    grid = new bool[size, size];
                    for (int y = 0; y < size; ++y) for (int x = 0; x < size; ++x) grid[x, y] = r.Next(2) == 0;
                }
                else
                {
                    grid = new bool[code.Size, code.Size];
                    for (int y = 0; y < code.Size; ++y) for (int x = 0; x < code.Size; ++x) grid[x, y] = code[x, y];
                    int flips = r.Next(150);
                    for (int k = 0; k < flips; ++k) { int x = r.Next(code.Size), y = r.Next(code.Size); grid[x, y] = !grid[x, y]; }
                }
                try { QrDecoder.DecodeGrid(grid); }
                catch (QrException) { }
            }
            Assert.Throws<QrException>(() => QrDecoder.DecodeGrid(new bool[20, 20]));
            Assert.Throws<QrException>(() => QrDecoder.DecodeGrid(new bool[21, 25]));
        }

        [Fact]
        public void Images_NeverThrow()
        {
            Random r = new Random(3);
            QrCode code = QrEncoder.Encode("otpauth://totp/Test:me?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Test");
            for (int i = 0; i < 60; ++i)
            {
                using (Bitmap bmp = i % 2 == 0 ? new Bitmap(1 + r.Next(300), 1 + r.Next(300)) : ShowQrForm.Render(code, 2 + r.Next(4)))
                {
                    int noise = i % 2 == 0 ? bmp.Width * bmp.Height : r.Next(bmp.Width * bmp.Height / 8);
                    for (int k = 0; k < noise; ++k)
                    {
                        int v = r.Next(2) * 255;
                        bmp.SetPixel(r.Next(bmp.Width), r.Next(bmp.Height), Color.FromArgb(v, v, v));
                    }
                    QrDecoder.Decode(bmp); // text or null, never an exception
                }
            }
            using (Bitmap one = new Bitmap(1, 1)) Assert.Null(QrDecoder.Decode(one));
        }

        // Findings of the independent security review, one regression test each.

        [Fact]
        public void KeeOtpHexWithMinus_IsIgnored_NotThrown()
        {
            PwEntry e = new PwEntry(true, true);
            e.Strings.Set("otp", new ProtectedString(true, "key=-1&encoding=hex"));
            Assert.Null(OtpStore.Read(e));
            Assert.Null(OtpStore.ParseKeeOtp("key=-1&encoding=hex"));
        }

        [Fact]
        public void OversizedSecretFields_AreIgnored()
        {
            string huge = new string('A', OtpStore.MaxFieldLength + 8);
            foreach (string field in new[] { "TimeOtp-Secret-Base32", "TimeOtp-Secret", "HmacOtp-Secret-Hex", "otp", "TOTP Seed" })
            {
                PwEntry e = new PwEntry(true, true);
                e.Strings.Set(field, new ProtectedString(true, field == "otp" ? "key=" + huge : huge));
                Assert.Null(OtpStore.Read(e));
            }
        }

        [Fact]
        public void FinderFlood_IsAbandonedQuickly()
        {
            // An image tiled with finder-like squares made the search quadratic (minutes).
            using (Bitmap bmp = new Bitmap(800, 800))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    for (int y = 0; y + 7 <= 800; y += 8)
                        for (int x = 0; x + 7 <= 800; x += 8)
                        {
                            g.FillRectangle(Brushes.Black, x, y, 7, 7);
                            g.FillRectangle(Brushes.White, x + 1, y + 1, 5, 5);
                            g.FillRectangle(Brushes.Black, x + 2, y + 2, 3, 3);
                        }
                }
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                Assert.Null(QrDecoder.Decode(bmp));
                Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), "took " + sw.Elapsed);
            }
        }

        [Fact]
        public void Eci26WithInvalidUtf8_IsAQrException()
        {
            // ECI 26 (UTF-8), byte mode, 1 byte 0xFF (not valid UTF-8), terminator.
            byte[] data = { 0x71, 0xA4, 0x01, 0xFF, 0x00, 0xEC, 0x11, 0xEC, 0x11 };
            Assert.Throws<QrException>(() => QrBitstream.Decode(data, 1));
        }

        private static string Mutate(Random r, string s)
        {
            StringBuilder sb = new StringBuilder(s);
            int n = 1 + r.Next(5);
            for (int i = 0; i < n && sb.Length > 0; ++i)
            {
                int pos = r.Next(sb.Length);
                switch (r.Next(3))
                {
                    case 0: sb.Remove(pos, 1); break;
                    case 1: sb.Insert(pos, "=&%?:/0-"[r.Next(8)]); break;
                    default: sb[pos] = (char)r.Next(32, 127); break;
                }
            }
            return sb.ToString();
        }
    }
}
