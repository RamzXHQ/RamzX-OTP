// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;

namespace RamzXOtp.Otp
{
    /// <summary>
    /// Time-based one-time password (RFC 6238). All times are Unix seconds in UTC,
    /// so time zones and daylight saving time cannot affect the code.
    /// </summary>
    public static class Totp
    {
        public const int DefaultPeriod = 30;
        public const int MaxPeriod = 86400;

        public static string Compute(byte[] secret, long unixSeconds, int period, int digits,
            OtpAlgorithm algorithm)
        {
            return Hotp.Compute(secret, Counter(unixSeconds, period), digits, algorithm);
        }

        public static string ComputeNow(byte[] secret, int period, int digits, OtpAlgorithm algorithm)
        {
            return Compute(secret, UnixNow(), period, digits, algorithm);
        }

        /// <summary>T = floor((unixSeconds - T0) / period) with T0 = 0.</summary>
        public static ulong Counter(long unixSeconds, int period)
        {
            if (period < 1 || period > MaxPeriod) throw new ArgumentOutOfRangeException("period");
            if (unixSeconds < 0) throw new ArgumentOutOfRangeException("unixSeconds");
            return (ulong)(unixSeconds / period);
        }

        /// <summary>Seconds until the current code changes (1..period).</summary>
        public static int SecondsRemaining(long unixSeconds, int period)
        {
            if (period < 1 || period > MaxPeriod) throw new ArgumentOutOfRangeException("period");
            if (unixSeconds < 0) throw new ArgumentOutOfRangeException("unixSeconds");
            return period - (int)(unixSeconds % period);
        }

        public static long UnixNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
