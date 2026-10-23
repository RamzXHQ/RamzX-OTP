// RamzX OTP - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Text;

using RamzXOtp.Otp;

using Xunit;

namespace RamzXOtp.Tests
{
    public class OtpAuthUriTests
    {
        // Base32 of the RFC 4226/6238 test seed "12345678901234567890"
        private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        [Fact]
        public void GoogleWikiExample()
        {
            OtpParameters p = OtpAuthUri.Parse(
                "otpauth://totp/Example:alice@google.com?secret=JBSWY3DPEHPK3PXP&issuer=Example");
            Assert.Equal(OtpType.Totp, p.Type);
            Assert.Equal("Example", p.Issuer);
            Assert.Equal("alice@google.com", p.Account);
            Assert.Equal(Base32.Decode("JBSWY3DPEHPK3PXP"), p.Secret);
            Assert.Equal(OtpAlgorithm.Sha1, p.Algorithm);
            Assert.Equal(6, p.Digits);
            Assert.Equal(30, p.Period);
            Assert.False(p.IsSteam);
        }

        [Fact]
        public void EncodedLabel_AllParameters()
        {
            OtpParameters p = OtpAuthUri.Parse("otpauth://totp/ACME%20Co:john.doe@email.com?" +
                "secret=HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ&issuer=ACME%20Co&algorithm=SHA256&digits=8&period=60");
            Assert.Equal("ACME Co", p.Issuer);
            Assert.Equal("john.doe@email.com", p.Account);
            Assert.Equal(OtpAlgorithm.Sha256, p.Algorithm);
            Assert.Equal(8, p.Digits);
            Assert.Equal(60, p.Period);
        }

        [Fact]
        public void IssuerParameter_WinsOverLabel_PlusIsSpace_KeysCaseInsensitive()
        {
            OtpParameters p = OtpAuthUri.Parse("OTPAUTH://TOTP/Old+Name:bob?SECRET=jbswy3dpehpk3pxp&Issuer=New+Name&Algorithm=sha-512");
            Assert.Equal("New Name", p.Issuer);
            Assert.Equal("bob", p.Account);
            Assert.Equal(OtpAlgorithm.Sha512, p.Algorithm);
        }

        [Fact]
        public void LabelWithoutIssuer_AndSecretWithPadding()
        {
            OtpParameters p = OtpAuthUri.Parse("otpauth://totp/just-me?secret=MZXW6YTBOI%3D%3D%3D%3D%3D%3D");
            Assert.Null(p.Issuer);
            Assert.Equal("just-me", p.Account);
            Assert.Equal(Encoding.ASCII.GetBytes("foobar"), p.Secret);
        }

        [Fact]
        public void ParsedLink_GivesRfcCode()
        {
            OtpParameters p = OtpAuthUri.Parse("otpauth://totp/Test?secret=" + RfcSecret + "&digits=8");
            Assert.Equal("94287082", p.CurrentCode(59));
        }

        [Fact]
        public void Hotp_WithCounter()
        {
            OtpParameters p = OtpAuthUri.Parse("otpauth://hotp/Test?secret=" + RfcSecret + "&counter=3");
            Assert.Equal(OtpType.Hotp, p.Type);
            Assert.Equal(3ul, p.Counter);
            Assert.Equal("969429", p.CurrentCode(0)); // RFC 4226 Appendix D, counter 3
        }

        [Fact]
        public void Steam_IsDetected()
        {
            OtpParameters p = OtpAuthUri.Parse("otpauth://totp/Steam:me?secret=" + RfcSecret + "&encoder=steam");
            Assert.True(p.IsSteam);
            Assert.Throws<NotSupportedException>(() => p.CurrentCode(59));
        }

        [Theory]
        [InlineData("https://example.com/?secret=ABC")]
        [InlineData("otpauth://")]
        [InlineData("otpauth://totp")]
        [InlineData("otpauth://motp/x?secret=" + RfcSecret)]
        [InlineData("otpauth://totp/x")]
        [InlineData("otpauth://totp/x?secret=")]
        [InlineData("otpauth://totp/x?secret=NOT*BASE32")]
        [InlineData("otpauth://totp/x?secret=" + RfcSecret + "&algorithm=MD5")]
        [InlineData("otpauth://totp/x?secret=" + RfcSecret + "&digits=5")]
        [InlineData("otpauth://totp/x?secret=" + RfcSecret + "&digits=11")]
        [InlineData("otpauth://totp/x?secret=" + RfcSecret + "&digits=six")]
        [InlineData("otpauth://totp/x?secret=" + RfcSecret + "&period=0")]
        [InlineData("otpauth://hotp/x?secret=" + RfcSecret)]
        [InlineData("otpauth://hotp/x?secret=" + RfcSecret + "&counter=-1")]
        public void Invalid_IsRejected(string uri)
        {
            Assert.Throws<FormatException>(() => OtpAuthUri.Parse(uri));
        }

        [Fact]
        public void Null_IsRejected()
        {
            Assert.Throws<FormatException>(() => OtpAuthUri.Parse(null));
            Assert.False(OtpAuthUri.LooksLikeUri(null));
            Assert.True(OtpAuthUri.LooksLikeUri("  OtpAuth://totp/x"));
        }

        [Theory]
        [InlineData("otpauth://totp/ACME%20Co:john?secret=" + RfcSecret + "&issuer=ACME%20Co&algorithm=SHA512&digits=8&period=60")]
        [InlineData("otpauth://hotp/me?secret=" + RfcSecret + "&counter=42")]
        [InlineData("otpauth://totp/Steam:me?secret=" + RfcSecret + "&encoder=steam")]
        public void Format_RoundTrip(string uri)
        {
            OtpParameters a = OtpAuthUri.Parse(uri);
            string written = OtpAuthUri.Format(a);
            OtpParameters b = OtpAuthUri.Parse(written);

            Assert.DoesNotContain("=&", written.Replace("secret=", "s"));  // no Base32 padding
            Assert.Equal(a.Type, b.Type);
            Assert.Equal(a.Secret, b.Secret);
            Assert.Equal(a.Issuer, b.Issuer);
            Assert.Equal(a.Account, b.Account);
            Assert.Equal(a.Algorithm, b.Algorithm);
            Assert.Equal(a.Digits, b.Digits);
            Assert.Equal(a.Period, b.Period);
            Assert.Equal(a.Counter, b.Counter);
            Assert.Equal(a.IsSteam, b.IsSteam);
        }

        [Fact]
        public void Format_RequiresSecret()
        {
            Assert.Throws<ArgumentNullException>(() => OtpAuthUri.Format(null));
            Assert.Throws<ArgumentException>(() => OtpAuthUri.Format(new OtpParameters()));
        }

        [Fact]
        public void Clear_WipesSecret()
        {
            OtpParameters p = OtpAuthUri.Parse("otpauth://totp/x?secret=" + RfcSecret);
            p.Clear();
            Assert.All(p.Secret, b => Assert.Equal(0, b));
        }
    }
}
