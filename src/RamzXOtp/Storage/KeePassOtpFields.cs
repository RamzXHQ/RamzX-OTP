// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Globalization;
using System.Text;

using KeePassLib;
using KeePassLib.Security;

using RamzXOtp.Otp;

namespace RamzXOtp.Storage
{
    /// <summary>
    /// KeePass's own OTP fields (KeePass 2.47+, see keepass.info "Placeholders"):
    /// TimeOtp-* for TOTP and HmacOtp-* for HOTP. Entries written here keep working in plain
    /// KeePass ({TIMEOTP}, Ctrl+T) and in KeePassXC 2.7.10+ even without this plugin.
    /// </summary>
    public static class KeePassOtpFields
    {
        public const string TimePrefix = "TimeOtp-";
        public const string HmacPrefix = "HmacOtp-";
        public const string TimeLength = "TimeOtp-Length";
        public const string TimePeriod = "TimeOtp-Period";
        public const string TimeAlgorithm = "TimeOtp-Algorithm";
        public const string HmacCounter = "HmacOtp-Counter";

        /// <summary>KeePass's native TOTP supports at most 8 digits.</summary>
        public const int MaxNativeTotpDigits = 8;

        private static readonly string[] SecretSuffixes = { "Secret", "Secret-Hex", "Secret-Base32", "Secret-Base64" };

        public static bool HasOtp(PwEntry e)
        {
            foreach (string s in SecretSuffixes)
                if (e.Strings.Exists(TimePrefix + s) || e.Strings.Exists(HmacPrefix + s)) return true;
            return false;
        }

        /// <summary>True if <see cref="Write"/> can store these parameters natively.</summary>
        public static bool CanStoreNatively(OtpParameters p, out string reason)
        {
            reason = null;
            if (p.IsSteam) reason = "Steam Guard tokens cannot be stored in KeePass's OTP fields.";
            else if (p.Type == OtpType.Totp && p.Digits > MaxNativeTotpDigits) reason = "KeePass supports at most 8 digits.";
            else if (p.Type == OtpType.Hotp && (p.Digits != 6 || p.Algorithm != OtpAlgorithm.Sha1))
                reason = "KeePass's HOTP supports only 6 digits with SHA-1.";
            return reason == null;
        }

        /// <summary>
        /// Writes the parameters and removes other secret encodings of the same type, so KeePass
        /// and KeePassXC cannot pick up an old secret. Callers create an entry backup first.
        /// </summary>
        public static void Write(PwEntry e, OtpParameters p)
        {
            if (e == null) throw new ArgumentNullException("e");
            string reason;
            if (!CanStoreNatively(p, out reason)) throw new NotSupportedException(reason);

            string prefix = p.Type == OtpType.Hotp ? HmacPrefix : TimePrefix;
            foreach (string s in SecretSuffixes) e.Strings.Remove(prefix + s);

            e.Strings.Set(prefix + "Secret-Base32", new ProtectedString(true, Base32.Encode(p.Secret)));

            if (p.Type == OtpType.Hotp)
            {
                e.Strings.Set(HmacCounter, new ProtectedString(false, p.Counter.ToString(CultureInfo.InvariantCulture)));
                return;
            }

            e.Strings.Set(TimeLength, new ProtectedString(false, p.Digits.ToString(CultureInfo.InvariantCulture)));
            e.Strings.Set(TimePeriod, new ProtectedString(false, p.Period.ToString(CultureInfo.InvariantCulture)));
            e.Strings.Set(TimeAlgorithm, new ProtectedString(false, AlgorithmFieldValue(p.Algorithm)));
        }

        /// <summary>Reads TOTP (preferred) or HOTP settings; null if the entry has none.</summary>
        public static OtpParameters Read(PwEntry e)
        {
            if (e == null) throw new ArgumentNullException("e");

            byte[] secret = ReadSecret(e, TimePrefix);
            if (secret != null)
            {
                OtpParameters p = new OtpParameters();
                p.Type = OtpType.Totp;
                p.Secret = secret;
                // Fewer than 6 digits is not allowed by RFC 4226 and is not computed (a damaged or
                // crafted value from a shared database falls back to the default).
                p.Digits = ReadInt(e, TimeLength, 6, Hotp.MinDigits, MaxNativeTotpDigits);
                p.Period = ReadInt(e, TimePeriod, Totp.DefaultPeriod, 1, Totp.MaxPeriod);
                p.Algorithm = ParseAlgorithmField(e.Strings.ReadSafe(TimeAlgorithm));
                return p;
            }

            secret = ReadSecret(e, HmacPrefix);
            if (secret != null)
            {
                OtpParameters p = new OtpParameters();
                p.Type = OtpType.Hotp;
                p.Secret = secret;
                ulong c;
                p.Counter = ulong.TryParse(e.Strings.ReadSafe(HmacCounter).Trim(), NumberStyles.None,
                    CultureInfo.InvariantCulture, out c) ? c : 0;
                return p;
            }
            return null;
        }

        public static string AlgorithmFieldValue(OtpAlgorithm a)
        {
            switch (a)
            {
                case OtpAlgorithm.Sha256: return "HMAC-SHA-256";
                case OtpAlgorithm.Sha512: return "HMAC-SHA-512";
                default: return "HMAC-SHA-1";
            }
        }

        public static OtpAlgorithm ParseAlgorithmField(string v)
        {
            switch ((v ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "HMAC-SHA-256": return OtpAlgorithm.Sha256;
                case "HMAC-SHA-512": return OtpAlgorithm.Sha512;
                default: return OtpAlgorithm.Sha1; // KeePass default
            }
        }

        // Same precedence for all encodings: KeePass reads exactly one; prefer Base32, then others.
        private static byte[] ReadSecret(PwEntry e, string prefix)
        {
            // Oversized values (e.g. from a crafted shared database) are ignored: they would be
            // decoded and hashed every second for the entry list column.
            string v = OtpStore.Field(e, prefix + "Secret-Base32");
            byte[] b;
            if (v.Length > 0 && Base32.TryDecode(v, out b) && b.Length > 0) return b;

            v = OtpStore.Field(e, prefix + "Secret-Hex");
            if (v.Length > 0 && TryHex(v, out b)) return b;

            v = OtpStore.Field(e, prefix + "Secret-Base64");
            if (v.Length > 0)
            {
                try { b = Convert.FromBase64String(v.Trim()); if (b.Length > 0) return b; }
                catch (FormatException) { }
            }

            v = OtpStore.Field(e, prefix + "Secret");
            if (v.Length > 0) return Encoding.UTF8.GetBytes(v);
            return null;
        }

        private static bool TryHex(string v, out byte[] data)
        {
            data = null;
            StringBuilder sb = new StringBuilder(v.Length);
            foreach (char c in v) if (!char.IsWhiteSpace(c)) sb.Append(c);
            string h = sb.ToString();
            if (h.Length == 0 || h.Length % 2 != 0) return false;
            byte[] r = new byte[h.Length / 2];
            for (int i = 0; i < r.Length; ++i)
                if (!byte.TryParse(h.Substring(2 * i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r[i]))
                    return false;
            data = r;
            return true;
        }

        private static int ReadInt(PwEntry e, string field, int def, int min, int max)
        {
            int n;
            if (int.TryParse(e.Strings.ReadSafe(field).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out n)
                && n >= min && n <= max) return n;
            return def;
        }
    }
}
