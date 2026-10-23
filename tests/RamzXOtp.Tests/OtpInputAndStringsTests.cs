// RamzX OTP - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Linq;
using System.Text.RegularExpressions;

using RamzXOtp.Otp;
using RamzXOtp.UI;

using Xunit;

namespace RamzXOtp.Tests
{
    public class OtpInputTests
    {
        private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        [Fact]
        public void Empty_AndInvalid()
        {
            Assert.Equal(OtpInputState.Empty, OtpInput.Evaluate("  ", OtpAlgorithm.Sha1, 6, 30).State);
            Assert.Equal(OtpInputState.Empty, OtpInput.Evaluate(null, OtpAlgorithm.Sha1, 6, 30).State);
            Assert.Equal(OtpInputState.Invalid, OtpInput.Evaluate("hello world!", OtpAlgorithm.Sha1, 6, 30).State);
            Assert.Equal(OtpInputState.Invalid, OtpInput.Evaluate("otpauth://totp/x", OtpAlgorithm.Sha1, 6, 30).State);
        }

        [Fact]
        public void RawSecret_UsesManualSettings()
        {
            OtpInput r = OtpInput.Evaluate("gezd gnbv gy3t qojq gezd gnbv gy3t qojq", OtpAlgorithm.Sha256, 8, 60);
            Assert.Equal(OtpInputState.Ok, r.State);
            Assert.False(r.FromUri);
            Assert.Equal(OtpAlgorithm.Sha256, r.Parameters.Algorithm);
            Assert.Equal(8, r.Parameters.Digits);
            Assert.Equal(60, r.Parameters.Period);
        }

        [Fact]
        public void Link_OverridesManualSettings()
        {
            OtpInput r = OtpInput.Evaluate("otpauth://totp/GitHub:me?secret=" + RfcSecret + "&digits=8",
                OtpAlgorithm.Sha512, 6, 90);
            Assert.Equal(OtpInputState.Ok, r.State);
            Assert.True(r.FromUri);
            Assert.Equal(OtpAlgorithm.Sha1, r.Parameters.Algorithm);
            Assert.Equal(8, r.Parameters.Digits);
            Assert.Equal(30, r.Parameters.Period);
            Assert.Equal("GitHub (me) - TOTP, SHA1, 8, 30 s", r.Message);
        }

        [Fact]
        public void ShortSecret_IsAWarningNotAnError()
        {
            OtpInput r = OtpInput.Evaluate("JBSWY3DPEHPK3PXP", OtpAlgorithm.Sha1, 6, 30);
            Assert.Equal(OtpInputState.Short, r.State);
            Assert.NotNull(r.Parameters);
        }

        [Fact]
        public void Steam_AndTooManyDigits_AreNotStorableYet()
        {
            Assert.Equal(OtpInputState.NotStorable, OtpInput.Evaluate(
                "otpauth://totp/Steam:me?secret=" + RfcSecret + "&encoder=steam", OtpAlgorithm.Sha1, 6, 30).State);
            Assert.Equal(OtpInputState.NotStorable, OtpInput.Evaluate(
                "otpauth://totp/x?secret=" + RfcSecret + "&digits=10", OtpAlgorithm.Sha1, 6, 30).State);
        }

        [Fact]
        public void Describe_Variants()
        {
            OtpParameters p = new OtpParameters();
            p.Secret = new byte[20];
            Assert.Equal("TOTP, SHA1, 6, 30 s", OtpInput.Describe(p));
            p.Issuer = "Acme";
            Assert.Equal("Acme - TOTP, SHA1, 6, 30 s", OtpInput.Describe(p));
            p.Type = OtpType.Hotp; p.Issuer = null; p.Account = "bob";
            Assert.Equal("bob - HOTP, SHA1, 6", OtpInput.Describe(p));
        }
    }

    public class StringsTests : IDisposable
    {
        public void Dispose() { Strings.SetLanguage("en"); }

        [Fact]
        public void AllLanguages_HaveAllKeys_WithSamePlaceholders()
        {
            var en = Strings.Table("en");
            foreach (string lang in Strings.Languages)
            {
                var t = Strings.Table(lang);
                Assert.Equal(en.Keys.OrderBy(k => k), t.Keys.OrderBy(k => k));
                foreach (string key in en.Keys)
                {
                    Assert.False(string.IsNullOrWhiteSpace(t[key]), lang + "/" + key);
                    Assert.Equal(Placeholders(en[key]), Placeholders(t[key]));
                }
            }
        }

        private static string[] Placeholders(string s)
        {
            return Regex.Matches(s, @"\{[A-Z0-9]+\}").Cast<Match>().Select(m => m.Value).Distinct().OrderBy(x => x).ToArray();
        }

        [Fact]
        public void SameDatabaseNotice_InEveryLanguage()
        {
            foreach (string lang in Strings.Languages)
                Assert.False(string.IsNullOrWhiteSpace(Strings.Table(lang)[Strings.SameDbNote]));
        }

        [Theory]
        [InlineData("de", "de", false)]
        [InlineData("AR", "ar", true)]
        [InlineData("fr", "en", false)]
        [InlineData(null, "en", false)]
        public void SetLanguage(string requested, string expected, bool rtl)
        {
            Strings.SetLanguage(requested);
            Assert.Equal(expected, Strings.Language);
            Assert.Equal(rtl, Strings.RightToLeft);
            Assert.Equal(Strings.Table(expected)[Strings.Save], Strings.Get(Strings.Save));
            Assert.Equal("NoSuchKey", Strings.Get("NoSuchKey"));
            Strings.Select("de");
            Assert.Equal("de", Strings.Language);
        }

        [Fact]
        public void Format_InsertsArguments()
        {
            Strings.SetLanguage("en");
            Assert.Equal("RamzX OTP - GitHub", Strings.Format(Strings.FormTitle, "GitHub"));
        }
    }
}
