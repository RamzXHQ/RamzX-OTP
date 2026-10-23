// RamzX OTP - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System.Drawing;

using RamzXOtp.UI;

using Xunit;

namespace RamzXOtp.Tests
{
    public class UiHelperTests
    {
        [Theory]
        [InlineData(8, 16)]   // never smaller than 16 px
        [InlineData(16, 16)]
        [InlineData(24, 24)]
        [InlineData(32, 32)]
        public void Icon_IsDrawnAtRequestedSize(int requested, int expected)
        {
            using (Bitmap b = Icons.Timer(requested))
            {
                Assert.Equal(expected, b.Width);
                Assert.Equal(expected, b.Height);
                Assert.NotEqual(0, b.GetPixel(expected / 2, expected / 2).A); // dial is opaque
                Assert.Equal(0, b.GetPixel(0, 0).A);                          // corner transparent
            }
        }

        [Theory]
        [InlineData("123456", "123 456")]
        [InlineData("12345678", "1234 5678")]
        [InlineData("1234567", "123 4567")]
        public void CodeGrouping(string code, string expected)
        {
            Assert.Equal(expected, RamzXOtpExt.Group(code));
        }
    }
}
