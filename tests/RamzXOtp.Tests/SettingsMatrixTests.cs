// RamzX OTP - tests
// Copyright (c) 2026 RamzX. MIT License.

using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

using KeePassLib;

using RamzXOtp.Otp;
using RamzXOtp.Storage;
using RamzXOtp.UI;

using Xunit;

namespace RamzXOtp.Tests
{
    /// <summary>Every setting the dialog offers: algorithm x digits x period, saved and read back.</summary>
    public class SettingsMatrixTests
    {
        private const string Secret = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";

        public static IEnumerable<object[]> AllSettings()
        {
            foreach (OtpAlgorithm a in new[] { OtpAlgorithm.Sha1, OtpAlgorithm.Sha256, OtpAlgorithm.Sha512 })
                foreach (int digits in new[] { 6, 7, 8 })
                    foreach (int period in new[] { 1, 15, 30, 45, 60, 90, 120, 300, 86400 })
                        yield return new object[] { a, digits, period };
        }

        [Theory]
        [MemberData(nameof(AllSettings))]
        public void RawSecret_SaveAndReadBack_SameCodes(OtpAlgorithm a, int digits, int period)
        {
            OtpInput input = OtpInput.Evaluate(Secret, a, digits, period);
            Assert.Equal(OtpInputState.Ok, input.State);

            PwEntry e = new PwEntry(true, true);
            KeePassOtpFields.Write(e, input.Parameters);
            OtpParameters back = KeePassOtpFields.Read(e);

            Assert.Equal(a, back.Algorithm);
            Assert.Equal(digits, back.Digits);
            Assert.Equal(period, back.Period);
            foreach (long t in new[] { 59L, 1234567890L, 2000000000L })
            {
                string expected = Totp.Compute(Base32.Decode(Secret), t, period, digits, a);
                Assert.Equal(expected, back.CurrentCode(t));
                Assert.Equal(digits, expected.Length);
            }
        }

        [Theory]
        [InlineData(30)]
        [InlineData(60)]
        [InlineData(120)]
        public void Period_CodeChangesExactlyAtTheBoundary(int period)
        {
            byte[] s = Base32.Decode(Secret);
            long start = 1800000000L / period * period; // aligned to the period
            string first = Totp.Compute(s, start, period, 6, OtpAlgorithm.Sha1);
            Assert.Equal(first, Totp.Compute(s, start + period - 1, period, 6, OtpAlgorithm.Sha1));
            Assert.NotEqual(first, Totp.Compute(s, start + period, period, 6, OtpAlgorithm.Sha1));
            Assert.Equal(period, Totp.SecondsRemaining(start, period));
            Assert.Equal(1, Totp.SecondsRemaining(start + period - 1, period));
        }

        [Fact]
        public void Link_UserChangesPeriod_IsUsedAndFlagged()
        {
            string uri = "otpauth://totp/x?secret=" + Secret; // 30 s, SHA1, 6 digits by default

            OtpInput normal = OtpInput.Evaluate(uri, OtpAlgorithm.Sha256, 8, 60);
            Assert.Equal(30, normal.Parameters.Period);  // link wins unless overridden
            Assert.False(normal.Overridden);

            OtpInput changed = OtpInput.Evaluate(uri, OtpAlgorithm.Sha1, 6, 60, true);
            Assert.True(changed.Overridden);
            Assert.Equal(60, changed.Parameters.Period);
            Assert.Equal(30, changed.LinkPeriod);

            OtpInput same = OtpInput.Evaluate(uri, OtpAlgorithm.Sha1, 6, 30, true);
            Assert.False(same.Overridden); // nothing actually differs
        }

        [Fact]
        public void Dialog_EditSavedEntry_ChangePeriodTo60_IsSaved()
        {
            OtpParameters saved = OtpAuthUri.Parse("otpauth://totp/x?secret=" + Secret);
            using (AddOtpForm f = new AddOtpForm("Test", saved))
            {
                NumericUpDown period = (NumericUpDown)typeof(AddOtpForm)
                    .GetField("m_period", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(f);
                Assert.Equal(30, (int)period.Value);
                Assert.True(period.Enabled);

                period.Value = 60;
                Assert.Equal(60, f.Result.Period);

                PwEntry e = new PwEntry(true, true);
                KeePassOtpFields.Write(e, f.Result);
                Assert.Equal("60", e.Strings.ReadSafe("TimeOtp-Period"));
            }
        }

        [Fact]
        public void Dialog_SavedEntryWith60s_OpensWith60s()
        {
            OtpParameters saved = OtpAuthUri.Parse("otpauth://totp/x?secret=" + Secret + "&period=60&digits=8&algorithm=SHA256");
            using (AddOtpForm f = new AddOtpForm("Test", saved))
            {
                Assert.Equal(60, f.Result.Period);
                Assert.Equal(8, f.Result.Digits);
                Assert.Equal(OtpAlgorithm.Sha256, f.Result.Algorithm);
            }
        }
    }
}
