// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

namespace RamzXOtp.Otp
{
    /// <summary>HMAC hash function used by HOTP/TOTP (RFC 4226 / RFC 6238).</summary>
    public enum OtpAlgorithm
    {
        Sha1,
        Sha256,
        Sha512
    }
}
