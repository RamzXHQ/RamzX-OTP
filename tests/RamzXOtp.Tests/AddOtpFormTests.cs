// RamzX OTP - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System.Reflection;
using System.Windows.Forms;

using RamzXOtp.Otp;
using RamzXOtp.UI;

using Xunit;

namespace RamzXOtp.Tests
{
    /// <summary>Dialog logic without showing the window (runs on the CI runner).</summary>
    public class AddOtpFormTests
    {
        private const string Uri = "otpauth://totp/RamzX%20Test:demo@example.com?secret=JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP&issuer=RamzX%20Test";

        private static void SetInput(AddOtpForm f, string text)
        {
            TextBox input = (TextBox)typeof(AddOtpForm).GetField("m_input", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(f);
            input.Text = text;
        }

        private static void Close(AddOtpForm f, DialogResult result)
        {
            f.DialogResult = result;
            typeof(Form).GetMethod("OnFormClosed", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(f, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });
        }

        // Regression: closing with "Save" must keep the result (the input was wiped on close
        // and the re-evaluation cleared the secret before it could be saved).
        [Fact]
        public void Save_KeepsResultAfterClosing()
        {
            using (AddOtpForm f = new AddOtpForm("Test"))
            {
                SetInput(f, Uri);
                Close(f, DialogResult.OK);

                OtpParameters p = f.Result;
                Assert.NotNull(p);
                Assert.Equal(Base32.Decode("JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP"), p.Secret);
                Assert.Equal("RamzX Test", p.Issuer);
            }
        }

        [Fact]
        public void Cancel_WipesSecret()
        {
            using (AddOtpForm f = new AddOtpForm("Test"))
            {
                SetInput(f, Uri);
                OtpParameters p = f.Result;
                Close(f, DialogResult.Cancel);
                Assert.All(p.Secret, b => Assert.Equal(0, b));
            }
        }

        [Fact]
        public void ExistingOtp_IsShownForEditing()
        {
            OtpParameters existing = OtpAuthUri.Parse(Uri);
            using (AddOtpForm f = new AddOtpForm("Test", existing))
            {
                Assert.NotNull(f.Result);
                Assert.Equal(existing.Secret, f.Result.Secret);
            }
        }

        // Regression: a Steam link (5 characters) crashed the dialog because the
        // digits field only accepts 6 to 8.
        [Theory]
        [InlineData("otpauth://totp/Steam:me?secret=JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP&issuer=Steam&encoder=steam", 5)]
        [InlineData("otpauth://totp/x?secret=JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP&digits=10", 10)]
        public void LinkOutsideDigitsRange_DoesNotCrash(string uri, int digits)
        {
            using (AddOtpForm f = new AddOtpForm("Test"))
            {
                SetInput(f, uri);
                Assert.NotNull(f.Result);
                Assert.Equal(digits, f.Result.Digits);
                Assert.Equal(digits, f.Result.CurrentCode(59).Length);
            }
        }

        [Fact]
        public void ExistingSteam_IsShownForEditing()
        {
            OtpParameters steam = OtpAuthUri.Parse("otpauth://totp/Steam:me?secret=JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP&encoder=steam");
            using (AddOtpForm f = new AddOtpForm("Test", steam))
            {
                Assert.NotNull(f.Result);
                Assert.True(f.Result.IsSteam);
            }
        }

        [Fact]
        public void InvalidInput_HasNoResult()
        {
            using (AddOtpForm f = new AddOtpForm("Test"))
            {
                SetInput(f, "not a secret!");
                Assert.Null(f.Result);
            }
        }
    }
}
