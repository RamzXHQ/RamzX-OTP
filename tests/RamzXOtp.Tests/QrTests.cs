// RamzX OTP - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

using RamzXOtp.Otp;
using RamzXOtp.Qr;

using Xunit;

namespace RamzXOtp.Tests
{
    public class QrTablesTests
    {
        [Fact]
        public void Capacity_KnownValues()
        {
            Assert.Equal(26, QrTables.TotalCodewords(1));
            Assert.Equal(134, QrTables.TotalCodewords(5));
            Assert.Equal(3706, QrTables.TotalCodewords(40));
            Assert.Equal(19, QrTables.DataCodewords(QrEcLevel.L, 1));
            Assert.Equal(16, QrTables.DataCodewords(QrEcLevel.M, 1));
            Assert.Equal(62, QrTables.DataCodewords(QrEcLevel.Q, 5));
            Assert.Equal(2956, QrTables.DataCodewords(QrEcLevel.L, 40));
            Assert.Equal(1276, QrTables.DataCodewords(QrEcLevel.H, 40));
        }

        [Fact]
        public void AlignmentPositions_KnownValues()
        {
            Assert.Empty(QrTables.AlignmentPositions(1));
            Assert.Equal(new[] { 6, 18 }, QrTables.AlignmentPositions(2));
            Assert.Equal(new[] { 6, 22, 38 }, QrTables.AlignmentPositions(7));
            Assert.Equal(new[] { 6, 34, 60, 86, 112, 138 }, QrTables.AlignmentPositions(32));
            Assert.Equal(new[] { 6, 30, 58, 86, 114, 142, 170 }, QrTables.AlignmentPositions(40));
        }

        [Fact]
        public void FormatAndVersionBits_KnownValues()
        {
            Assert.Equal(Convert.ToInt32("111011111000100", 2), QrTables.FormatBits(QrEcLevel.L, 0));
            Assert.Equal(Convert.ToInt32("101010000010010", 2), QrTables.FormatBits(QrEcLevel.M, 0));
            Assert.Equal(Convert.ToInt32("011010101011111", 2), QrTables.FormatBits(QrEcLevel.Q, 0));
            Assert.Equal(Convert.ToInt32("001011010001001", 2), QrTables.FormatBits(QrEcLevel.H, 0));
            Assert.Equal(Convert.ToInt32("000111110010010100", 2), QrTables.VersionBits(7));
        }

        [Fact]
        public void Sizes()
        {
            Assert.Equal(21, QrTables.Size(1));
            Assert.Equal(177, QrTables.Size(40));
            Assert.Equal(7, QrTables.VersionForSize(45));
            Assert.Equal(-1, QrTables.VersionForSize(22));
            Assert.Equal(-1, QrTables.VersionForSize(181));
            Assert.Throws<ArgumentOutOfRangeException>(() => QrTables.Size(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => QrTables.Mask(8, 0, 0));
        }
    }

    public class ReedSolomonTests
    {
        // "HELLO WORLD", version 1-M (worked example of the QR encoding process)
        private static readonly byte[] HelloData = { 32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17 };
        private static readonly byte[] HelloEcc = { 196, 35, 39, 119, 235, 215, 231, 226, 93, 23 };

        [Fact]
        public void Gf256_Arithmetic()
        {
            for (int a = 1; a < 256; ++a)
            {
                Assert.Equal(a, Gf256.Div(Gf256.Mul(a, 7), 7));
                Assert.Equal(1, Gf256.Mul(a, Gf256.Div(1, a)));
            }
            Assert.Equal(0, Gf256.Mul(0, 5));
            Assert.Equal(1, Gf256.Pow(255));
            Assert.Equal(Gf256.Pow(254), Gf256.Pow(-1));
            Assert.Throws<DivideByZeroException>(() => Gf256.Div(1, 0));
        }

        [Fact]
        public void HelloWorld_DataAndEccCodewords()
        {
            byte[] data = QrBitstream.Finish(QrBitstream.EncodeSegment("HELLO WORLD", QrMode.Alphanumeric, 1), 16);
            Assert.Equal(HelloData, data);
            Assert.Equal(HelloEcc, ReedSolomon.Encode(HelloData, 10));
        }

        [Fact]
        public void Correct_UpToHalfTheEccBytes()
        {
            Random rnd = new Random(7);
            foreach (int ecc in new[] { 7, 10, 16, 22, 30 })
                for (int trial = 0; trial < 40; ++trial)
                {
                    byte[] data = new byte[rnd.Next(1, 120)];
                    rnd.NextBytes(data);
                    byte[] block = new byte[data.Length + ecc];
                    Array.Copy(data, block, data.Length);
                    Array.Copy(ReedSolomon.Encode(data, ecc), 0, block, data.Length, ecc);
                    byte[] good = (byte[])block.Clone();

                    int errors = rnd.Next(0, ecc / 2 + 1);
                    for (int e = 0; e < errors; ++e) block[rnd.Next(block.Length)] ^= (byte)rnd.Next(1, 256);

                    Assert.True(ReedSolomon.Correct(block, ecc));
                    Assert.Equal(good, block);
                }
        }

        [Fact]
        public void Correct_InvalidArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ReedSolomon.Correct(new byte[5], 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ReedSolomon.Correct(new byte[5], 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => ReedSolomon.Generator(0));
        }
    }

    public class QrGridRoundTripTests
    {
        private const string Uri = "otpauth://totp/ACME%20Co:john.doe@email.com?secret=HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ&issuer=ACME%20Co&algorithm=SHA1&digits=6&period=30";

        internal static bool[,] Grid(QrCode c)
        {
            bool[,] g = new bool[c.Size, c.Size];
            for (int y = 0; y < c.Size; ++y)
                for (int x = 0; x < c.Size; ++x) g[x, y] = c[x, y];
            return g;
        }

        [Fact]
        public void AllVersionsAndLevels()
        {
            foreach (QrEcLevel level in new[] { QrEcLevel.L, QrEcLevel.M, QrEcLevel.Q, QrEcLevel.H })
                for (int v = 1; v <= 40; ++v)
                {
                    // Fill the symbol completely: byte capacity = (data bits - mode - count) / 8.
                    int capacity = (QrTables.DataCodewords(level, v) * 8 - 4 - QrBitstream.CharCountBits(QrMode.Byte, v)) / 8;
                    StringBuilder sb = new StringBuilder();
                    while (sb.Length < capacity) sb.Append(Uri);
                    string text = sb.ToString(0, capacity);

                    QrCode c = QrEncoder.Encode(text, level, v % 8, v);
                    Assert.Equal(v, c.Version);
                    Assert.Equal(text, QrDecoder.DecodeGrid(Grid(c)));
                }
        }

        [Fact]
        public void AllMasks_AndAutomaticMask()
        {
            for (int mask = -1; mask < 8; ++mask)
            {
                QrCode c = QrEncoder.Encode(Uri, QrEcLevel.M, mask);
                if (mask >= 0) Assert.Equal(mask, c.Mask);
                Assert.Equal(Uri, QrDecoder.DecodeGrid(Grid(c)));
            }
        }

        [Theory]
        [InlineData("0123456789012345", QrMode.Numeric)]
        [InlineData("12", QrMode.Numeric)]
        [InlineData("HELLO WORLD $%*+-./:", QrMode.Alphanumeric)]
        [InlineData("A", QrMode.Alphanumeric)]
        [InlineData("Grüße ✓ مرحبا", QrMode.Byte)]
        public void Modes(string text, QrMode mode)
        {
            QrCode c = QrEncoder.Encode(text, QrEcLevel.Q, -1, 1, mode);
            Assert.Equal(text, QrDecoder.DecodeGrid(Grid(c)));
        }

        [Fact]
        public void DamagedSymbol_IsCorrected()
        {
            QrCode c = QrEncoder.Encode(Uri, QrEcLevel.H);
            bool[,] g = Grid(c);
            int n = c.Size;
            // A smudge of 4x4 modules in the data area (bottom right, outside function patterns).
            for (int y = n - 12; y < n - 8; ++y)
                for (int x = n - 4; x < n; ++x) g[x, y] = !g[x, y];
            Assert.Equal(Uri, QrDecoder.DecodeGrid(g));
        }

        [Fact]
        public void TooDamaged_Throws()
        {
            QrCode c = QrEncoder.Encode(Uri, QrEcLevel.L);
            bool[,] g = Grid(c);
            for (int y = 9; y < c.Size - 9; ++y)
                for (int x = 9; x < c.Size; ++x) g[x, y] = (x * 7 + y * 13) % 3 == 0;
            Assert.Throws<QrException>(() => QrDecoder.DecodeGrid(g));
        }

        [Fact]
        public void InvalidGrid_Throws()
        {
            Assert.Throws<QrException>(() => QrDecoder.DecodeGrid(new bool[22, 22]));
            Assert.Throws<QrException>(() => QrDecoder.DecodeGrid(new bool[21, 21])); // no format info
        }

        [Fact]
        public void Encoder_Limits()
        {
            Assert.Throws<ArgumentNullException>(() => QrEncoder.Encode(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => QrEncoder.Encode("x", QrEcLevel.M, 8));
            Assert.Throws<ArgumentException>(() => QrEncoder.Encode(new string('x', 3000), QrEcLevel.H));
            Assert.Throws<ArgumentException>(() => QrEncoder.Encode("abc", QrEcLevel.M, -1, 1, QrMode.Numeric));
            Assert.Throws<ArgumentException>(() => QrEncoder.Encode("abc", QrEcLevel.M, -1, 1, QrMode.Alphanumeric));
            Assert.Throws<NotSupportedException>(() => QrEncoder.Encode("abc", QrEcLevel.M, -1, 1, QrMode.Kanji));
        }
    }

    /// <summary>Decoding from rendered images, as from a screenshot.</summary>
    public class QrImageTests
    {
        private const string Uri = "otpauth://totp/Example:alice@google.com?secret=JBSWY3DPEHPK3PXP&issuer=Example";

        internal static Bitmap Render(QrCode c, int scale, int quiet, Color dark, Color light)
        {
            int px = (c.Size + 2 * quiet) * scale;
            Bitmap bmp = new Bitmap(px, px);
            using (Graphics g = Graphics.FromImage(bmp))
            using (SolidBrush b = new SolidBrush(dark))
            {
                g.Clear(light);
                for (int y = 0; y < c.Size; ++y)
                    for (int x = 0; x < c.Size; ++x)
                        if (c[x, y]) g.FillRectangle(b, (x + quiet) * scale, (y + quiet) * scale, scale, scale);
            }
            return bmp;
        }

        private static Bitmap Render(QrCode c, int scale, int quiet = 4)
        {
            return Render(c, scale, quiet, Color.Black, Color.White);
        }

        // Smooth resize like browser zoom or Windows display scaling.
        private static Bitmap Resize(Bitmap src, double factor)
        {
            int w = (int)Math.Round(src.Width * factor), h = (int)Math.Round(src.Height * factor);
            Bitmap dst = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, 0, 0, w, h);
            }
            return dst;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(9)]
        public void IntegerScales(int scale)
        {
            using (Bitmap bmp = Render(QrEncoder.Encode(Uri), scale))
                Assert.Equal(Uri, QrDecoder.Decode(bmp));
        }

        [Theory]
        [InlineData(0.25)]   // 2.5 px per module
        [InlineData(0.375)]  // 3.75 px per module
        [InlineData(0.43)]
        [InlineData(0.61)]
        public void FractionalScales_WithSmoothing(double factor)
        {
            using (Bitmap big = Render(QrEncoder.Encode(Uri), 10))
            using (Bitmap bmp = Resize(big, factor))
                Assert.Equal(Uri, QrDecoder.Decode(bmp));
        }

        [Fact]
        public void LightOnDark_AndColored()
        {
            QrCode c = QrEncoder.Encode(Uri);
            using (Bitmap inv = Render(c, 4, 4, Color.White, Color.Black))
                Assert.Equal(Uri, QrDecoder.Decode(inv));
            using (Bitmap col = Render(c, 4, 4, Color.FromArgb(20, 40, 120), Color.FromArgb(255, 250, 210)))
                Assert.Equal(Uri, QrDecoder.Decode(col));
        }

        [Theory]
        [InlineData(RotateFlipType.Rotate90FlipNone)]
        [InlineData(RotateFlipType.Rotate180FlipNone)]
        [InlineData(RotateFlipType.Rotate270FlipNone)]
        public void Rotated(RotateFlipType rot)
        {
            using (Bitmap bmp = Render(QrEncoder.Encode(Uri), 4))
            {
                bmp.RotateFlip(rot);
                Assert.Equal(Uri, QrDecoder.Decode(bmp));
            }
        }

        [Fact]
        public void SmallQuietZone_OnBusyPage()
        {
            QrCode c = QrEncoder.Encode(Uri, QrEcLevel.M);
            using (Bitmap qr = Render(c, 3, 1))
            using (Bitmap page = new Bitmap(900, 600))
            using (Graphics g = Graphics.FromImage(page))
            using (Font f = new Font(FontFamily.GenericSansSerif, 14f))
            {
                g.Clear(Color.FromArgb(245, 245, 245));
                Random rnd = new Random(3);
                for (int i = 0; i < 60; ++i)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256))))
                        g.FillRectangle(b, rnd.Next(900), rnd.Next(600), rnd.Next(5, 60), rnd.Next(5, 30));
                    g.DrawString("Scan this QR code with your authenticator app", f, Brushes.Black, rnd.Next(500), rnd.Next(580));
                }
                g.FillRectangle(Brushes.White, 410, 190, qr.Width + 6, qr.Height + 6);
                g.DrawImage(qr, 413, 193);
                Assert.Equal(Uri, QrDecoder.Decode(page));
            }
        }

        [Fact]
        public void LargeVersion()
        {
            string text = Uri + "&x=" + new string('a', 400);
            QrCode c = QrEncoder.Encode(text, QrEcLevel.M);
            Assert.True(c.Version >= 10);
            using (Bitmap bmp = Render(c, 3))
                Assert.Equal(text, QrDecoder.Decode(bmp));
        }

        [Fact]
        public void TransparentBackground_CountsAsWhite()
        {
            QrCode c = QrEncoder.Encode(Uri);
            using (Bitmap bmp = Render(c, 4, 4, Color.Black, Color.Transparent))
                Assert.Equal(Uri, QrDecoder.Decode(bmp));
        }

        [Fact]
        public void NoQrCode_ReturnsNull()
        {
            using (Bitmap bmp = new Bitmap(300, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font f = new Font(FontFamily.GenericSansSerif, 18f))
            {
                g.Clear(Color.White);
                g.DrawString("No QR code here ■ □ ▪", f, Brushes.Black, 10, 80);
                Assert.Null(QrDecoder.Decode(bmp));
            }
            Assert.Throws<ArgumentNullException>(() => QrDecoder.Decode((Bitmap)null));
        }

        [Fact]
        public void EndToEnd_QrToOtpCode()
        {
            string uri = "otpauth://totp/Test?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&digits=8";
            using (Bitmap bmp = Render(QrEncoder.Encode(uri), 3))
            {
                OtpParameters p = OtpAuthUri.Parse(QrDecoder.Decode(bmp));
                Assert.Equal("94287082", p.CurrentCode(59)); // RFC 6238 Appendix B
            }
        }
    }
}
