// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;

namespace RamzXOtp.Otp
{
    public enum OtpType
    {
        Totp,
        Hotp
    }

    /// <summary>Everything needed to generate codes for one account.</summary>
    public sealed class OtpParameters
    {
        public OtpType Type = OtpType.Totp;
        public byte[] Secret;
        public OtpAlgorithm Algorithm = OtpAlgorithm.Sha1;
        public int Digits = 6;
        public int Period = Totp.DefaultPeriod;
        public ulong Counter;

        /// <summary>Service name, e.g. "GitHub" (informational only).</summary>
        public string Issuer;

        /// <summary>Account name, e.g. "user@example.com" (informational only).</summary>
        public string Account;

        /// <summary>Steam Guard tokens (encoder=steam) use a different output format.</summary>
        public bool IsSteam;

        /// <summary>Current code for TOTP, code for the stored counter for HOTP.</summary>
        public string CurrentCode(long unixSeconds)
        {
            if (IsSteam) throw new NotSupportedException("Steam Guard codes are not supported yet.");
            return Type == OtpType.Hotp
                ? Hotp.Compute(Secret, Counter, Digits, Algorithm)
                : Totp.Compute(Secret, unixSeconds, Period, Digits, Algorithm);
        }

        public void Clear()
        {
            if (Secret != null) Array.Clear(Secret, 0, Secret.Length);
        }
    }
}
