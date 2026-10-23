// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;

using RamzXOtp.Otp;
using RamzXOtp.Storage;

namespace RamzXOtp.UI
{
    public enum OtpInputState
    {
        Empty,
        Invalid,
        NotStorable,
        Short,
        Ok
    }

    /// <summary>
    /// Turns what the user pasted (otpauth:// link or Base32 secret) plus the manual settings
    /// into OtpParameters. Kept free of WinForms so it can be unit-tested.
    /// </summary>
    public sealed class OtpInput
    {
        public OtpInputState State { get; private set; }
        public string Message { get; private set; }
        public OtpParameters Parameters { get; private set; }

        /// <summary>True if the input was a link: the caller shows its settings in the controls.</summary>
        public bool FromUri { get; private set; }

        public static OtpInput Evaluate(string text, OtpAlgorithm algorithm, int digits, int period)
        {
            OtpInput r = new OtpInput();
            string t = (text ?? string.Empty).Trim();

            if (t.Length == 0) { r.State = OtpInputState.Empty; return r; }

            OtpParameters p;
            if (OtpAuthUri.LooksLikeUri(t))
            {
                try { p = OtpAuthUri.Parse(t); }
                catch (FormatException ex) { r.State = OtpInputState.Invalid; r.Message = ex.Message; return r; }
                r.FromUri = true;
            }
            else
            {
                string normalized;
                SecretStatus s = SecretCheck.Check(t, out normalized);
                if (s == SecretStatus.Empty || s == SecretStatus.Invalid)
                {
                    r.State = OtpInputState.Invalid;
                    r.Message = "The secret is not valid Base32.";
                    return r;
                }
                p = new OtpParameters();
                p.Secret = Base32.Decode(normalized);
                p.Algorithm = algorithm;
                p.Digits = digits;
                p.Period = period;
            }

            r.Parameters = p;
            string reason;
            if (!KeePassOtpFields.CanStoreNatively(p, out reason))
            {
                r.State = OtpInputState.NotStorable;
                r.Message = reason;
                return r;
            }

            r.State = p.Secret.Length < SecretCheck.RecommendedMinBytes ? OtpInputState.Short : OtpInputState.Ok;
            r.Message = Describe(p);
            return r;
        }

        public static string Describe(OtpParameters p)
        {
            string who = string.IsNullOrEmpty(p.Issuer) ? p.Account
                : (string.IsNullOrEmpty(p.Account) ? p.Issuer : p.Issuer + " (" + p.Account + ")");
            string what = (p.Type == OtpType.Hotp ? "HOTP" : "TOTP") + ", " + OtpAuthUri.AlgorithmName(p.Algorithm) +
                ", " + p.Digits + (p.Type == OtpType.Totp ? ", " + p.Period + " s" : string.Empty);
            return string.IsNullOrEmpty(who) ? what : who + " - " + what;
        }
    }
}
