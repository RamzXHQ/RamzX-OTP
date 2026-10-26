// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System.Text;

namespace RamzXOtp.Otp
{
    /// <summary>
    /// Steam Guard codes: TOTP (SHA-1, 30 s) whose 31-bit value is written as 5 characters of
    /// the alphabet 23456789BCDFGHJKMNPQRTVWXY, least significant first.
    /// </summary>
    public static class SteamGuard
    {
        public const string Alphabet = "23456789BCDFGHJKMNPQRTVWXY";
        public const int Length = 5;

        public static string Compute(byte[] secret, long unixSeconds)
        {
            uint v = Hotp.Truncate(secret, Totp.Counter(unixSeconds, Totp.DefaultPeriod), OtpAlgorithm.Sha1);
            StringBuilder sb = new StringBuilder(Length);
            for (int i = 0; i < Length; ++i)
            {
                sb.Append(Alphabet[(int)(v % (uint)Alphabet.Length)]);
                v /= (uint)Alphabet.Length;
            }
            return sb.ToString();
        }
    }
}
