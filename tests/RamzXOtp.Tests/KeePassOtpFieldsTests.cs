// RamzX OTP - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Text;

using KeePassLib;
using KeePassLib.Cryptography;
using KeePassLib.Security;

using RamzXOtp.Otp;
using RamzXOtp.Storage;

using Xunit;

namespace RamzXOtp.Tests
{
    public class KeePassOtpFieldsTests
    {
        private static readonly byte[] Seed20 = Encoding.ASCII.GetBytes("12345678901234567890");

        private static OtpParameters Totp(OtpAlgorithm a, int digits, int period)
        {
            OtpParameters p = new OtpParameters();
            p.Secret = (byte[])Seed20.Clone();
            p.Algorithm = a;
            p.Digits = digits;
            p.Period = period;
            return p;
        }

        [Fact]
        public void WriteTotp_SetsNativeFields_SecretProtected()
        {
            PwEntry e = new PwEntry(true, true);
            KeePassOtpFields.Write(e, Totp(OtpAlgorithm.Sha256, 8, 60));

            Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", e.Strings.ReadSafe("TimeOtp-Secret-Base32"));
            Assert.True(e.Strings.Get("TimeOtp-Secret-Base32").IsProtected);
            Assert.Equal("8", e.Strings.ReadSafe("TimeOtp-Length"));
            Assert.Equal("60", e.Strings.ReadSafe("TimeOtp-Period"));
            Assert.Equal("HMAC-SHA-256", e.Strings.ReadSafe("TimeOtp-Algorithm"));
            Assert.True(KeePassOtpFields.HasOtp(e));
        }

        [Theory]
        [InlineData(OtpAlgorithm.Sha1, 6, 30)]
        [InlineData(OtpAlgorithm.Sha256, 7, 45)]
        [InlineData(OtpAlgorithm.Sha512, 8, 60)]
        public void WriteThenRead_RoundTrip(OtpAlgorithm a, int digits, int period)
        {
            PwEntry e = new PwEntry(true, true);
            KeePassOtpFields.Write(e, Totp(a, digits, period));

            OtpParameters r = KeePassOtpFields.Read(e);
            Assert.Equal(OtpType.Totp, r.Type);
            Assert.Equal(Seed20, r.Secret);
            Assert.Equal(a, r.Algorithm);
            Assert.Equal(digits, r.Digits);
            Assert.Equal(period, r.Period);
        }

        [Fact]
        public void Write_RemovesOtherSecretEncodings()
        {
            PwEntry e = new PwEntry(true, true);
            e.Strings.Set("TimeOtp-Secret-Hex", new ProtectedString(true, "00112233"));
            e.Strings.Set("TimeOtp-Secret", new ProtectedString(true, "old"));
            e.Strings.Set("TimeOtp-Secret-Base64", new ProtectedString(true, "b2xk"));

            KeePassOtpFields.Write(e, Totp(OtpAlgorithm.Sha1, 6, 30));

            Assert.False(e.Strings.Exists("TimeOtp-Secret-Hex"));
            Assert.False(e.Strings.Exists("TimeOtp-Secret"));
            Assert.False(e.Strings.Exists("TimeOtp-Secret-Base64"));
            Assert.Equal(Seed20, KeePassOtpFields.Read(e).Secret);
        }

        [Theory]
        [InlineData("TimeOtp-Secret-Hex", "3132333435363738393031323334353637383930")]
        [InlineData("TimeOtp-Secret-Hex", "31 32 33 34 35 36 37 38 39 30 31 32 33 34 35 36 37 38 39 30")]
        [InlineData("TimeOtp-Secret-Base64", "MTIzNDU2Nzg5MDEyMzQ1Njc4OTA=")]
        [InlineData("TimeOtp-Secret", "12345678901234567890")]
        [InlineData("TimeOtp-Secret-Base32", "gezd gnbv gy3t qojq gezd gnbv gy3t qojq")]
        public void Read_AllKeePassSecretEncodings(string field, string value)
        {
            PwEntry e = new PwEntry(true, true);
            e.Strings.Set(field, new ProtectedString(true, value));

            OtpParameters r = KeePassOtpFields.Read(e);
            Assert.Equal(Seed20, r.Secret);
            Assert.Equal(6, r.Digits);   // KeePass defaults
            Assert.Equal(30, r.Period);
            Assert.Equal(OtpAlgorithm.Sha1, r.Algorithm);
            Assert.Equal("287082", r.CurrentCode(59));
        }

        [Fact]
        public void Read_InvalidSettings_FallBackToDefaults()
        {
            PwEntry e = new PwEntry(true, true);
            e.Strings.Set("TimeOtp-Secret-Base32", new ProtectedString(true, "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ"));
            e.Strings.Set("TimeOtp-Length", new ProtectedString(false, "99"));
            e.Strings.Set("TimeOtp-Period", new ProtectedString(false, "abc"));
            e.Strings.Set("TimeOtp-Algorithm", new ProtectedString(false, "MD5"));

            OtpParameters r = KeePassOtpFields.Read(e);
            Assert.Equal(6, r.Digits);
            Assert.Equal(30, r.Period);
            Assert.Equal(OtpAlgorithm.Sha1, r.Algorithm);
        }

        [Fact]
        public void Read_BrokenEncodings_FallThroughToNextOrNull()
        {
            PwEntry e = new PwEntry(true, true);
            e.Strings.Set("TimeOtp-Secret-Base32", new ProtectedString(true, "!!!"));
            e.Strings.Set("TimeOtp-Secret-Hex", new ProtectedString(true, "XYZ"));
            e.Strings.Set("TimeOtp-Secret-Base64", new ProtectedString(true, "%%%"));
            Assert.Null(KeePassOtpFields.Read(e));
            Assert.True(KeePassOtpFields.HasOtp(e));
        }

        [Fact]
        public void Hotp_WriteThenRead_AndMatchesKeePassOwnGenerator()
        {
            OtpParameters p = new OtpParameters();
            p.Type = OtpType.Hotp;
            p.Secret = (byte[])Seed20.Clone();
            p.Counter = 7;

            PwEntry e = new PwEntry(true, true);
            KeePassOtpFields.Write(e, p);
            Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", e.Strings.ReadSafe("HmacOtp-Secret-Base32"));
            Assert.Equal("7", e.Strings.ReadSafe("HmacOtp-Counter"));

            OtpParameters r = KeePassOtpFields.Read(e);
            Assert.Equal(OtpType.Hotp, r.Type);
            Assert.Equal(7ul, r.Counter);

            // Cross-check with KeePassLib's own HOTP implementation.
            string keePass = HmacOtp.Generate(Seed20, 7, 6, false, -1);
            Assert.Equal(keePass, r.CurrentCode(0));
            Assert.Equal("162583", keePass); // RFC 4226 Appendix D
        }

        [Fact]
        public void Read_HotpCounterInvalid_IsZero()
        {
            PwEntry e = new PwEntry(true, true);
            e.Strings.Set("HmacOtp-Secret", new ProtectedString(true, "12345678901234567890"));
            e.Strings.Set("HmacOtp-Counter", new ProtectedString(false, "x"));
            Assert.Equal(0ul, KeePassOtpFields.Read(e).Counter);
        }

        [Fact]
        public void NotStorable_Cases()
        {
            string reason;
            OtpParameters steam = Totp(OtpAlgorithm.Sha1, 6, 30); steam.IsSteam = true;
            Assert.False(KeePassOtpFields.CanStoreNatively(steam, out reason));

            Assert.False(KeePassOtpFields.CanStoreNatively(Totp(OtpAlgorithm.Sha1, 9, 30), out reason));
            Assert.NotNull(reason);

            OtpParameters hotp8 = Totp(OtpAlgorithm.Sha1, 8, 30); hotp8.Type = OtpType.Hotp;
            Assert.False(KeePassOtpFields.CanStoreNatively(hotp8, out reason));

            OtpParameters hotpSha256 = Totp(OtpAlgorithm.Sha256, 6, 30); hotpSha256.Type = OtpType.Hotp;
            Assert.False(KeePassOtpFields.CanStoreNatively(hotpSha256, out reason));

            Assert.Throws<NotSupportedException>(() => KeePassOtpFields.Write(new PwEntry(true, true), steam));
            Assert.True(KeePassOtpFields.CanStoreNatively(Totp(OtpAlgorithm.Sha512, 8, 30), out reason));
            Assert.Null(reason);
        }

        [Fact]
        public void EmptyEntry_HasNoOtp()
        {
            PwEntry e = new PwEntry(true, true);
            Assert.False(KeePassOtpFields.HasOtp(e));
            Assert.Null(KeePassOtpFields.Read(e));
            Assert.Throws<ArgumentNullException>(() => KeePassOtpFields.Read(null));
            Assert.Throws<ArgumentNullException>(() => KeePassOtpFields.Write(null, Totp(OtpAlgorithm.Sha1, 6, 30)));
        }

        [Theory]
        [InlineData("HMAC-SHA-1", OtpAlgorithm.Sha1)]
        [InlineData("hmac-sha-256", OtpAlgorithm.Sha256)]
        [InlineData(" HMAC-SHA-512 ", OtpAlgorithm.Sha512)]
        [InlineData("", OtpAlgorithm.Sha1)]
        [InlineData(null, OtpAlgorithm.Sha1)]
        public void AlgorithmField_Parsing(string value, OtpAlgorithm expected)
        {
            Assert.Equal(expected, KeePassOtpFields.ParseAlgorithmField(value));
            Assert.Equal(KeePassOtpFields.AlgorithmFieldValue(expected),
                KeePassOtpFields.AlgorithmFieldValue(KeePassOtpFields.ParseAlgorithmField(KeePassOtpFields.AlgorithmFieldValue(expected))));
        }
    }
}
