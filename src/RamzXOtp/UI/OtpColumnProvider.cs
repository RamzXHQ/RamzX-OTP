// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;

using KeePass.UI;
using KeePassLib;

using RamzXOtp.Integration;
using RamzXOtp.Otp;
using RamzXOtp.Storage;

namespace RamzXOtp.UI
{
    /// <summary>
    /// Optional entry list column "RamzX OTP" with the current code and remaining seconds
    /// (View → Configure Columns). A click on the cell copies the code.
    /// </summary>
    internal sealed class OtpColumnProvider : ColumnProvider
    {
        internal const string ColumnName = "RamzX OTP";

        private readonly Func<CopyGuard> m_guard;
        private readonly Action<PwEntry> m_copy;

        internal OtpColumnProvider(Func<CopyGuard> guard, Action<PwEntry> copy)
        {
            m_guard = guard;
            m_copy = copy;
        }

        public override string[] ColumnNames { get { return new[] { ColumnName }; } }

        public override string GetCellData(string strColumnName, PwEntry pe)
        {
            if (strColumnName != ColumnName || pe == null) return string.Empty;
            return CellText(pe, m_guard());
        }

        internal static string CellText(PwEntry pe, CopyGuard guard)
        {
            OtpParameters p = OtpStore.Read(pe);
            if (p == null) return string.Empty;
            try
            {
                if (guard != null && !guard.MayShowWithoutAsking) return "••• •••  🔒";
                if (p.Type == OtpType.Hotp) return "HOTP";
                long now = Totp.UnixNow();
                string code = p.CurrentCode(now);
                return RamzXOtpExt.Group(code) + "   " + Totp.SecondsRemaining(now, p.Period) + " s";
            }
            finally { p.Clear(); }
        }

        public override bool SupportsCellAction(string strColumnName) { return strColumnName == ColumnName; }

        public override void PerformCellAction(string strColumnName, PwEntry pe)
        {
            if (strColumnName == ColumnName && pe != null) m_copy(pe);
        }
    }
}
