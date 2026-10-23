// RamzX OTP - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Text;

using RamzXOtp.Otp;

using Xunit;

namespace RamzXOtp.Tests
{
    public class HotpTests
    {
        private static readonly byte[] Rfc4226Secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // RFC 4226, Appendix D
        [Theory]
        [InlineData(0ul, "755224")]
        [InlineData(1ul, "287082")]
        [InlineData(2ul, "359152")]
        [InlineData(3ul, "969429")]
        [InlineData(4ul, "338314")]
        [InlineData(5ul, "254676")]
        [InlineData(6ul, "287922")]
        [InlineData(7ul, "162583")]
        [InlineData(8ul, "399871")]
        [InlineData(9ul, "520489")]
        public void Rfc4226_AppendixD(ulong counter, string expected)
        {
            Assert.Equal(expected, Hotp.Compute(Rfc4226Secret, counter, 6, OtpAlgorithm.Sha1));
        }

        [Theory]
        [InlineData(5)]
        [InlineData(11)]
        public void Digits_OutOfRange_Throw(int digits)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Hotp.Compute(Rfc4226Secret, 0, digits, OtpAlgorithm.Sha1));
        }

        [Fact]
        public void TenDigits_IsTheFull31BitValue()
        {
            uint full = Hotp.Truncate(Rfc4226Secret, 0, OtpAlgorithm.Sha1);
            Assert.Equal(full.ToString().PadLeft(10, '0'), Hotp.Compute(Rfc4226Secret, 0, 10, OtpAlgorithm.Sha1));
            Assert.True(full <= int.MaxValue);
        }

        [Fact]
        public void EmptyOrNullSecret_Throws()
        {
            Assert.Throws<ArgumentException>(() => Hotp.Compute(new byte[0], 0, 6, OtpAlgorithm.Sha1));
            Assert.Throws<ArgumentNullException>(() => Hotp.Compute(null, 0, 6, OtpAlgorithm.Sha1));
        }

        [Fact]
        public void UnknownAlgorithm_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Hotp.Compute(Rfc4226Secret, 0, 6, (OtpAlgorithm)99));
        }
    }

    public class TotpTests
    {
        // RFC 6238 Appendix B uses a different seed length per hash (Erratum 2866).
        private static readonly byte[] Seed20 = Encoding.ASCII.GetBytes("12345678901234567890");
        private static readonly byte[] Seed32 = Encoding.ASCII.GetBytes("12345678901234567890123456789012");
        private static readonly byte[] Seed64 = Encoding.ASCII.GetBytes(
            "1234567890123456789012345678901234567890123456789012345678901234");

        // RFC 6238, Appendix B (8 digits, 30-second period)
        [Theory]
        [InlineData(59L, "94287082", "46119246", "90693936")]
        [InlineData(1111111109L, "07081804", "68084774", "25091201")]
        [InlineData(1111111111L, "14050471", "67062674", "99943326")]
        [InlineData(1234567890L, "89005924", "91819424", "93441116")]
        [InlineData(2000000000L, "69279037", "90698825", "38618901")]
        [InlineData(20000000000L, "65353130", "77737706", "47863826")]
        public void Rfc6238_AppendixB(long time, string sha1, string sha256, string sha512)
        {
            Assert.Equal(sha1, Totp.Compute(Seed20, time, 30, 8, OtpAlgorithm.Sha1));
            Assert.Equal(sha256, Totp.Compute(Seed32, time, 30, 8, OtpAlgorithm.Sha256));
            Assert.Equal(sha512, Totp.Compute(Seed64, time, 30, 8, OtpAlgorithm.Sha512));
        }

        [Fact]
        public void Erratum2866_ShortSeedGivesDifferentCodes()
        {
            // Using the 20-byte seed for SHA-256/512 does NOT reproduce Appendix B.
            Assert.Equal("32247374", Totp.Compute(Seed20, 59, 30, 8, OtpAlgorithm.Sha256));
            Assert.Equal("69342147", Totp.Compute(Seed20, 59, 30, 8, OtpAlgorithm.Sha512));
        }

        [Fact]
        public void Year2286AndBeyond_Works()
        {
            // Far beyond 32-bit time (2038) - counter is 64-bit.
            Assert.Equal(8, Totp.Compute(Seed20, 99999999999L, 30, 8, OtpAlgorithm.Sha1).Length);
        }

        [Fact]
        public void Counter_AndRemainingSeconds()
        {
            Assert.Equal(1ul, Totp.Counter(59, 30));
            Assert.Equal(2ul, Totp.Counter(60, 30));
            Assert.Equal(1ul, Totp.Counter(60, 60));
            Assert.Equal(1, Totp.SecondsRemaining(59, 30));
            Assert.Equal(30, Totp.SecondsRemaining(60, 30));
            Assert.Equal(15, Totp.SecondsRemaining(75, 30));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-30)]
        [InlineData(86401)]
        public void InvalidPeriod_Throws(int period)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Totp.Counter(100, period));
            Assert.Throws<ArgumentOutOfRangeException>(() => Totp.SecondsRemaining(100, period));
        }

        [Fact]
        public void NegativeTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Totp.Counter(-1, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() => Totp.SecondsRemaining(-1, 30));
        }

        [Fact]
        public void ComputeNow_MatchesComputeWithCurrentTime()
        {
            long before = Totp.UnixNow();
            string now = Totp.ComputeNow(Seed20, 30, 6, OtpAlgorithm.Sha1);
            long after = Totp.UnixNow();
            Assert.True(now == Totp.Compute(Seed20, before, 30, 6, OtpAlgorithm.Sha1) ||
                        now == Totp.Compute(Seed20, after, 30, 6, OtpAlgorithm.Sha1));
            Assert.InRange(before, 1700000000L, 99999999999L); // sane clock, UTC epoch seconds
        }
    }

    public class Base32Tests
    {
        // RFC 4648, section 10
        [Theory]
        [InlineData("", "")]
        [InlineData("f", "MY======")]
        [InlineData("fo", "MZXQ====")]
        [InlineData("foo", "MZXW6===")]
        [InlineData("foob", "MZXW6YQ=")]
        [InlineData("fooba", "MZXW6YTB")]
        [InlineData("foobar", "MZXW6YTBOI======")]
        public void Rfc4648_Vectors(string plain, string encoded)
        {
            byte[] data = Encoding.ASCII.GetBytes(plain);
            Assert.Equal(encoded, Base32.Encode(data, true));
            Assert.Equal(encoded.TrimEnd('='), Base32.Encode(data));
            Assert.Equal(data, Base32.Decode(encoded));
            Assert.Equal(data, Base32.Decode(encoded.TrimEnd('=')));
        }

        [Fact]
        public void Decode_AcceptsMessyInput()
        {
            byte[] expected = Encoding.ASCII.GetBytes("foobar");
            Assert.Equal(expected, Base32.Decode("mzxw 6ytb oi"));
            Assert.Equal(expected, Base32.Decode("MZXW-6YTB-OI======"));
            Assert.Equal(expected, Base32.Decode(" mzxw6ytboi\r\n"));
            Assert.Equal(expected, Base32.Decode("MzXw6YtBoI"));
        }

        [Theory]
        [InlineData("MZXW6YT1")]   // '1' is not in the alphabet
        [InlineData("MZXW6YT8")]   // '8' neither
        [InlineData("M")]          // impossible length (1 mod 8)
        [InlineData("MZX")]        // impossible length (3 mod 8)
        [InlineData("MZXW6Y")]     // impossible length (6 mod 8)
        [InlineData("MZXW?YTB")]
        public void Decode_RejectsInvalid(string input)
        {
            Assert.Throws<FormatException>(() => Base32.Decode(input));
            byte[] data;
            Assert.False(Base32.TryDecode(input, out data));
            Assert.Null(data);
        }

        [Fact]
        public void RoundTrip_RandomData()
        {
            Random rnd = new Random(42);
            for (int len = 0; len < 70; ++len)
            {
                byte[] data = new byte[len];
                rnd.NextBytes(data);
                Assert.Equal(data, Base32.Decode(Base32.Encode(data)));
            }
        }

        [Fact]
        public void NullInput_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Base32.Encode(null));
            Assert.Throws<ArgumentNullException>(() => Base32.Normalize(null));
        }
    }

    public class SecretCheckTests
    {
        [Fact]
        public void Classifies_Secrets()
        {
            string n;
            Assert.Equal(SecretStatus.Ok, SecretCheck.Check("JBSW Y3DP EHPK 3PXP JBSW Y3DP EHPK 3PXP", out n));
            Assert.Equal("JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP", n);
            Assert.Equal(SecretStatus.Short, SecretCheck.Check("jbswy3dpehpk3pxp", out n)); // 10 bytes
            Assert.Equal(SecretStatus.Empty, SecretCheck.Check("   ", out n));
            Assert.Equal(SecretStatus.Empty, SecretCheck.Check(null, out n));
            Assert.Equal(SecretStatus.Invalid, SecretCheck.Check("not base32!", out n));
        }
    }
}
