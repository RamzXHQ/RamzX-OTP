// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Globalization;
using System.Security.Cryptography;

namespace RamzXOtp.Otp
{
    /// <summary>
    /// HMAC-based one-time password, implemented from the text of RFC 4226
    /// (section 5.3, dynamic truncation). TOTP (RFC 6238) is HOTP with a time counter.
    /// </summary>
    public static class Hotp
    {
        public const int MinDigits = 6;
        public const int MaxDigits = 10;

        /// <summary>Returns the code as a zero-padded decimal string.</summary>
        public static string Compute(byte[] secret, ulong counter, int digits, OtpAlgorithm algorithm)
        {
            if (digits < MinDigits || digits > MaxDigits)
                throw new ArgumentOutOfRangeException("digits");

            uint binary = Truncate(secret, counter, algorithm);
            ulong code = binary % Pow10(digits);
            return code.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
        }

        /// <summary>
        /// HMAC over the 8-byte big-endian counter, then RFC 4226 dynamic truncation
        /// to a 31-bit integer.
        /// </summary>
        public static uint Truncate(byte[] secret, ulong counter, OtpAlgorithm algorithm)
        {
            if (secret == null) throw new ArgumentNullException("secret");
            if (secret.Length == 0) throw new ArgumentException("Secret is empty.", "secret");

            byte[] msg = new byte[8];
            for (int i = 7; i >= 0; --i)
            {
                msg[i] = (byte)(counter & 0xFF);
                counter >>= 8;
            }

            byte[] hash;
            using (HMAC h = CreateHmac(algorithm, secret)) hash = h.ComputeHash(msg);
            try
            {
                int offset = hash[hash.Length - 1] & 0x0F;
                return ((uint)(hash[offset] & 0x7F) << 24)
                    | ((uint)hash[offset + 1] << 16)
                    | ((uint)hash[offset + 2] << 8)
                    | hash[offset + 3];
            }
            finally { Array.Clear(hash, 0, hash.Length); }
        }

        private static HMAC CreateHmac(OtpAlgorithm algorithm, byte[] key)
        {
            switch (algorithm)
            {
                case OtpAlgorithm.Sha1: return new HMACSHA1(key);
                case OtpAlgorithm.Sha256: return new HMACSHA256(key);
                case OtpAlgorithm.Sha512: return new HMACSHA512(key);
                default: throw new ArgumentOutOfRangeException("algorithm");
            }
        }

        private static ulong Pow10(int n)
        {
            ulong r = 1;
            for (int i = 0; i < n; ++i) r *= 10;
            return r;
        }
    }
}
