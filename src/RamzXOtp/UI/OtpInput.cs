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

        /// <summary>True if the user changed a link's algorithm, digits or period.</summary>
        public bool Overridden { get; private set; }

        /// <summary>The settings exactly as written in the link (before any user change).</summary>
        public OtpAlgorithm LinkAlgorithm { get; private set; }
        public int LinkDigits { get; private set; }
        public int LinkPeriod { get; private set; }

        /// <param name="overrideLink">
        /// Use algorithm/digits/period even if the input is a link (the user changed them on
        /// purpose, e.g. when editing a saved entry). Otherwise the link's values win.
        /// </param>
        public static OtpInput Evaluate(string text, OtpAlgorithm algorithm, int digits, int period,
            bool overrideLink = false)
        {
            OtpInput r = new OtpInput();
            string t = (text ?? string.Empty).Trim();

            if (t.Length == 0) { r.State = OtpInputState.Empty; return r; }
            if (t.Length > OtpAuthUri.MaxLength)
            {
                r.State = OtpInputState.Invalid;
                r.Message = Strings.Get(Strings.TextTooLong);
                return r;
            }

            OtpParameters p;
            if (OtpAuthUri.LooksLikeUri(t))
            {
                try { p = OtpAuthUri.Parse(t); }
                catch (FormatException ex) { r.State = OtpInputState.Invalid; r.Message = ex.Message; return r; }
                r.FromUri = true;
                r.LinkAlgorithm = p.Algorithm;
                r.LinkDigits = p.Digits;
                r.LinkPeriod = p.Period;
                if (overrideLink && (p.Algorithm != algorithm || p.Digits != digits || p.Period != period))
                {
                    p.Algorithm = algorithm;
                    p.Digits = digits;
                    p.Period = period;
                    r.Overridden = true;
                }
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
            r.Native = KeePassOtpFields.CanStoreNatively(p, out reason);
            r.NotNativeReason = reason;

            r.State = p.Secret.Length < SecretCheck.RecommendedMinBytes ? OtpInputState.Short : OtpInputState.Ok;
            r.Message = Describe(p);
            return r;
        }

        /// <summary>
        /// True if KeePass's own OTP fields can hold it (Ctrl+T works). Otherwise it is stored as an
        /// otpauth:// link in the "otp" field (RamzX OTP and KeePassXC read it, KeePass's Ctrl+T not).
        /// </summary>
        public bool Native { get; private set; }
        public string NotNativeReason { get; private set; }

        public static string Describe(OtpParameters p)
        {
            if (p.IsSteam)
            {
                string w = string.IsNullOrEmpty(p.Account) ? p.Issuer : p.Account;
                return (string.IsNullOrEmpty(w) ? string.Empty : w + " - ") + "Steam Guard";
            }
            string who = string.IsNullOrEmpty(p.Issuer) ? p.Account
                : (string.IsNullOrEmpty(p.Account) ? p.Issuer : p.Issuer + " (" + p.Account + ")");
            string what = (p.Type == OtpType.Hotp ? "HOTP" : "TOTP") + ", " + OtpAuthUri.AlgorithmName(p.Algorithm) +
                ", " + p.Digits + (p.Type == OtpType.Totp ? ", " + p.Period + " s" : string.Empty);
            return string.IsNullOrEmpty(who) ? what : who + " - " + what;
        }
    }
}
