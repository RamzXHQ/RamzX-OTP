// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RamzXOtp.Otp
{
    /// <summary>
    /// Reads and writes Key URIs as shown in 2FA setup QR codes:
    /// otpauth://TYPE/LABEL?secret=...&amp;issuer=...&amp;algorithm=...&amp;digits=...&amp;period=...&amp;counter=...
    /// (format documented in the Google Authenticator wiki "Key Uri Format").
    /// </summary>
    public static class OtpAuthUri
    {
        private const string Scheme = "otpauth://";

        public static bool LooksLikeUri(string text)
        {
            return text != null && text.Trim().StartsWith(Scheme, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Throws FormatException with a short English reason on invalid input.</summary>
        public static OtpParameters Parse(string uri)
        {
            if (!LooksLikeUri(uri)) throw new FormatException("Not an otpauth:// link.");
            string s = uri.Trim().Substring(Scheme.Length);

            int slash = s.IndexOf('/');
            if (slash <= 0) throw new FormatException("Missing OTP type.");
            string type = s.Substring(0, slash).ToLowerInvariant();
            string rest = s.Substring(slash + 1);

            OtpParameters p = new OtpParameters();
            if (type == "totp") p.Type = OtpType.Totp;
            else if (type == "hotp") p.Type = OtpType.Hotp;
            else throw new FormatException("Unknown OTP type '" + type + "'.");

            int q = rest.IndexOf('?');
            string label = q < 0 ? rest : rest.Substring(0, q);
            Dictionary<string, string> args = ParseQuery(q < 0 ? string.Empty : rest.Substring(q + 1));

            label = DecodeText(label);
            int colon = label.IndexOf(':');
            if (colon >= 0)
            {
                p.Issuer = label.Substring(0, colon).Trim();
                p.Account = label.Substring(colon + 1).Trim();
            }
            else p.Account = label.Trim();

            string v;
            if (args.TryGetValue("issuer", out v) && v.Trim().Length > 0) p.Issuer = DecodeText(v).Trim();

            if (!args.TryGetValue("secret", out v) || Base32.Normalize(Uri.UnescapeDataString(v)).Length == 0)
                throw new FormatException("The link contains no secret.");
            try { p.Secret = Base32.Decode(Uri.UnescapeDataString(v)); }
            catch (FormatException) { throw new FormatException("The secret is not valid Base32."); }
            if (p.Secret.Length == 0) throw new FormatException("The link contains no secret.");

            if (args.TryGetValue("algorithm", out v)) p.Algorithm = ParseAlgorithm(v);
            bool steam = args.TryGetValue("encoder", out v) && v.Equals("steam", StringComparison.OrdinalIgnoreCase);
            if (args.TryGetValue("digits", out v) && !steam) p.Digits = ParseInt(v, Hotp.MinDigits, Hotp.MaxDigits, "digits");
            if (args.TryGetValue("period", out v)) p.Period = ParseInt(v, 1, Totp.MaxPeriod, "period");

            if (p.Type == OtpType.Hotp)
            {
                if (!args.TryGetValue("counter", out v)) throw new FormatException("HOTP link without counter.");
                ulong c;
                if (!ulong.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out c))
                    throw new FormatException("Invalid counter.");
                p.Counter = c;
            }

            if (args.TryGetValue("encoder", out v) && v.Equals("steam", StringComparison.OrdinalIgnoreCase))
            {
                p.IsSteam = true;
                p.Digits = SteamGuard.Length;
            }

            return p;
        }

        /// <summary>Writes a Key URI (secret without padding).</summary>
        public static string Format(OtpParameters p)
        {
            if (p == null) throw new ArgumentNullException("p");
            if (p.Secret == null || p.Secret.Length == 0) throw new ArgumentException("No secret.", "p");

            string label = Uri.EscapeDataString(p.Account ?? string.Empty);
            if (!string.IsNullOrEmpty(p.Issuer)) label = Uri.EscapeDataString(p.Issuer) + ":" + label;

            StringBuilder sb = new StringBuilder(Scheme);
            sb.Append(p.Type == OtpType.Hotp ? "hotp/" : "totp/").Append(label);
            sb.Append("?secret=").Append(Base32.Encode(p.Secret));
            if (!string.IsNullOrEmpty(p.Issuer)) sb.Append("&issuer=").Append(Uri.EscapeDataString(p.Issuer));
            sb.Append("&algorithm=").Append(AlgorithmName(p.Algorithm));
            if (!p.IsSteam) sb.Append("&digits=").Append(p.Digits.ToString(CultureInfo.InvariantCulture));
            if (p.Type == OtpType.Hotp) sb.Append("&counter=").Append(p.Counter.ToString(CultureInfo.InvariantCulture));
            else sb.Append("&period=").Append(p.Period.ToString(CultureInfo.InvariantCulture));
            if (p.IsSteam) sb.Append("&encoder=steam");
            return sb.ToString();
        }

        public static string AlgorithmName(OtpAlgorithm a)
        {
            switch (a)
            {
                case OtpAlgorithm.Sha256: return "SHA256";
                case OtpAlgorithm.Sha512: return "SHA512";
                default: return "SHA1";
            }
        }

        private static OtpAlgorithm ParseAlgorithm(string v)
        {
            switch (v.Trim().Replace("-", string.Empty).ToUpperInvariant())
            {
                case "SHA1": return OtpAlgorithm.Sha1;
                case "SHA256": return OtpAlgorithm.Sha256;
                case "SHA512": return OtpAlgorithm.Sha512;
                default: throw new FormatException("Unsupported algorithm '" + v + "'.");
            }
        }

        private static int ParseInt(string v, int min, int max, string name)
        {
            int n;
            if (!int.TryParse(v.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out n) || n < min || n > max)
                throw new FormatException("Invalid " + name + " '" + v + "'.");
            return n;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in query.Split('&'))
            {
                if (part.Length == 0) continue;
                int eq = part.IndexOf('=');
                string key = eq < 0 ? part : part.Substring(0, eq);
                string val = eq < 0 ? string.Empty : part.Substring(eq + 1);
                key = Uri.UnescapeDataString(key);
                if (!d.ContainsKey(key)) d[key] = val; // first occurrence wins
            }
            return d;
        }

        // Labels and issuers: percent-encoding, and '+' as space (common in generated links).
        private static string DecodeText(string s)
        {
            return Uri.UnescapeDataString(s.Replace('+', ' '));
        }
    }
}
