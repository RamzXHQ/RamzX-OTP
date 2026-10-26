// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System.Windows.Forms;

namespace RamzXOtp.UI
{
    /// <summary>Message boxes with the plugin title and right-to-left layout for Arabic.</summary>
    internal static class Ui
    {
        internal const string Title = "RamzX OTP";

        private static MessageBoxOptions Options
        {
            get { return Strings.RightToLeft ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0; }
        }

        internal static void Info(IWin32Window owner, string text)
        {
            MessageBox.Show(owner, text, Title, MessageBoxButtons.OK, MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1, Options);
        }

        internal static void Warn(IWin32Window owner, string text)
        {
            MessageBox.Show(owner, text, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1, Options);
        }

        /// <summary>Yes/No question, default No.</summary>
        internal static bool Ask(IWin32Window owner, string text)
        {
            return MessageBox.Show(owner, text, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2, Options) == DialogResult.Yes;
        }
    }
}
