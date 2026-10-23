// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;

namespace RamzXOtp.Otp
{
    public enum SecretStatus
    {
        Ok,
        /// <summary>Valid, but shorter than 128 bits (RFC 4226 section 4, R6): warn, do not reject.</summary>
        Short,
        Empty,
        Invalid
    }

    /// <summary>Cleans up and validates a pasted Base32 secret.</summary>
    public static class SecretCheck
    {
        public const int RecommendedMinBytes = 16; // 128 bits

        public static SecretStatus Check(string input, out string normalized)
        {
            normalized = input == null ? string.Empty : Base32.Normalize(input);
            if (normalized.Length == 0) return SecretStatus.Empty;

            byte[] data;
            if (!Base32.TryDecode(normalized, out data) || data.Length == 0) return SecretStatus.Invalid;

            int len = data.Length;
            Array.Clear(data, 0, data.Length);
            return len < RecommendedMinBytes ? SecretStatus.Short : SecretStatus.Ok;
        }
    }
}
