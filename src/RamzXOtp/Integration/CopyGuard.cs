// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Threading.Tasks;

using KeePass.App.Configuration;

namespace RamzXOtp.Integration
{
    public enum GuardResult
    {
        Allowed,
        Denied,
        /// <summary>Protection is on, but RamzX Unlock is not loaded.</summary>
        UnlockMissing
    }

    /// <summary>
    /// Optional protection: Windows Hello (through RamzX Unlock) before RamzX OTP copies or
    /// shows a stored code. A confirmation stays valid for a short time so several codes can
    /// be copied in a row. KeePass's own OTP commands (Ctrl+T, {TIMEOTP}) are not affected.
    /// </summary>
    internal sealed class CopyGuard
    {
        internal const string ConfigKey = "RamzXOtp.RequireHelloBeforeCopy";
        internal static readonly TimeSpan GraceTime = TimeSpan.FromSeconds(30);

        private readonly AceCustomConfig m_cfg;
        private readonly Func<Func<string, Task<bool>>> m_findVerifier; // null result = Unlock missing
        private readonly Func<DateTime> m_now;
        private DateTime m_verifiedUntil = DateTime.MinValue;

        internal CopyGuard(AceCustomConfig cfg, Func<Func<string, Task<bool>>> findVerifier, Func<DateTime> now)
        {
            if (cfg == null) throw new ArgumentNullException("cfg");
            m_cfg = cfg;
            m_findVerifier = findVerifier;
            m_now = now;
        }

        internal bool RequireHello
        {
            get { return m_cfg.GetBool(ConfigKey, false); }
            set { m_cfg.SetBool(ConfigKey, value); if (!value) m_verifiedUntil = DateTime.MinValue; }
        }

        internal bool UnlockAvailable { get { return m_findVerifier() != null; } }

        /// <summary>True if the code may be shown without asking (protection off or within grace time).</summary>
        internal bool MayShowWithoutAsking { get { return !RequireHello || m_now() < m_verifiedUntil; } }

        internal async Task<GuardResult> CheckAsync(string reason)
        {
            if (MayShowWithoutAsking) return GuardResult.Allowed;

            Func<string, Task<bool>> verify = m_findVerifier();
            if (verify == null) return GuardResult.UnlockMissing;

            if (!await verify(reason)) return GuardResult.Denied;
            m_verifiedUntil = m_now() + GraceTime;
            return GuardResult.Allowed;
        }

        /// <summary>
        /// Turns the protection off. Needs Windows Hello as well (otherwise anyone at the open
        /// KeePass could switch it off with one click), unless RamzX Unlock is not loaded:
        /// then nobody could ever switch it off again.
        /// </summary>
        internal async Task<bool> DisableAsync(string reason)
        {
            if (!RequireHello) return true;
            GuardResult g = await CheckAsync(reason);
            if (g == GuardResult.Denied) return false;
            RequireHello = false;
            return true;
        }

        /// <summary>Ends the grace time, e.g. when the database is closed or locked.</summary>
        internal void Reset() { m_verifiedUntil = DateTime.MinValue; }

        /// <summary>Turns the protection on only after one successful Windows Hello check.</summary>
        internal async Task<bool> EnableAsync(string reason)
        {
            Func<string, Task<bool>> verify = m_findVerifier();
            if (verify == null || !await verify(reason)) return false;
            RequireHello = true;
            m_verifiedUntil = m_now() + GraceTime;
            return true;
        }
    }
}
