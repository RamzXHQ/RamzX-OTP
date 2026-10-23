// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using KeePassLib;
using KeePassLib.Security;

using RamzXOtp.Otp;

namespace RamzXOtp.Storage
{
    /// <summary>Where an entry's OTP came from.</summary>
    public enum OtpSource
    {
        None,
        /// <summary>KeePass's own TimeOtp-*/HmacOtp-* fields (preferred).</summary>
        Native,
        /// <summary>"otp" field with an otpauth:// link (KeePassXC, KeePassOTP; Steam).</summary>
        OtpUri,
        /// <summary>"otp" field in KeeOtp format (key=...&amp;size=...&amp;step=...).</summary>
        KeeOtp,
        /// <summary>KeeTrayTOTP fields "TOTP Seed" + "TOTP Settings".</summary>
        TrayTotp
    }

    /// <summary>
    /// Reads every common OTP format and writes the one with the widest support:
    /// KeePass's native fields; only what they cannot hold (Steam, more than 8 digits) goes into
    /// the "otp" field as an otpauth:// link (the format KeePassXC uses).
    /// </summary>
    public static class OtpStore
    {
        public const string OtpField = "otp";
        public const string TraySeed = "TOTP Seed";
        public const string TraySettings = "TOTP Settings";

        public static bool HasOtp(PwEntry e)
        {
            return KeePassOtpFields.HasOtp(e) || e.Strings.Exists(OtpField) || e.Strings.Exists(TraySeed);
        }

        /// <summary>Longest OTP field value that is read; real ones are well below 1 KB.</summary>
        public const int MaxFieldLength = 4096;

        /// <summary>
        /// Native fields first, then the legacy formats. Null if none can be read.
        /// Never throws for damaged or crafted field values (entries may come from a shared
        /// database, and this runs every second for the entry list column).
        /// </summary>
        public static OtpParameters Read(PwEntry e, out OtpSource source)
        {
            if (e == null) throw new ArgumentNullException("e");
            source = OtpSource.None;

            OtpParameters p = Try(() => KeePassOtpFields.Read(e));
            if (p != null) { source = OtpSource.Native; return p; }

            string otp = Field(e, OtpField).Trim();
            if (OtpAuthUri.LooksLikeUri(otp))
            {
                p = Try(() => OtpAuthUri.Parse(otp));
                if (p != null) { source = OtpSource.OtpUri; return p; }
            }
            else if (otp.IndexOf("key=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                p = Try(() => ParseKeeOtp(otp));
                if (p != null) { source = OtpSource.KeeOtp; return p; }
            }

            string seed = Field(e, TraySeed);
            if (seed.Length > 0)
            {
                string settings = Field(e, TraySettings);
                p = Try(() => ParseTrayTotp(seed, settings));
                if (p != null) { source = OtpSource.TrayTotp; return p; }
            }
            return null;
        }

        /// <summary>Field value, or "" if it is missing or longer than MaxFieldLength.</summary>
        internal static string Field(PwEntry e, string name)
        {
            string v = e.Strings.ReadSafe(name);
            return v.Length > MaxFieldLength ? string.Empty : v;
        }

        private static OtpParameters Try(Func<OtpParameters> read)
        {
            try { return read(); }
            catch (Exception) { return null; } // unreadable = no OTP; never a crash
        }

        public static OtpParameters Read(PwEntry e)
        {
            OtpSource s;
            return Read(e, out s);
        }

        /// <summary>
        /// Writes the parameters (native where possible) and removes all other OTP formats of
        /// the entry, so no app can pick up an outdated secret. Callers create a backup first.
        /// </summary>
        public static void Write(PwEntry e, OtpParameters p)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (p == null) throw new ArgumentNullException("p");

            string reason;
            RemoveLegacy(e);
            if (KeePassOtpFields.CanStoreNatively(p, out reason))
            {
                KeePassOtpFields.Write(e, p);
                return;
            }

            RemoveNative(e);
            e.Strings.Set(OtpField, new ProtectedString(true, OtpAuthUri.Format(p)));
        }

        /// <summary>Entries that still use a legacy format (candidates for migration).</summary>
        public static List<PwEntry> FindLegacy(PwDatabase db)
        {
            List<PwEntry> list = new List<PwEntry>();
            if (db == null || db.RootGroup == null) return list;
            db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, e =>
            {
                OtpSource s;
                OtpParameters p = Read(e, out s);
                if (p == null) return true;
                bool needsWork = s != OtpSource.Native
                    ? KeePassOtpFields.CanStoreNatively(p, out _) || s != OtpSource.OtpUri
                    : e.Strings.Exists(OtpField) || e.Strings.Exists(TraySeed);
                p.Clear();
                if (needsWork) list.Add(e);
                return true;
            });
            return list;
        }

        private static void RemoveLegacy(PwEntry e)
        {
            e.Strings.Remove(OtpField);
            e.Strings.Remove(TraySeed);
            e.Strings.Remove(TraySettings);
        }

        private static void RemoveNative(PwEntry e)
        {
            foreach (string k in e.Strings.GetKeys())
                if (k.StartsWith(KeePassOtpFields.TimePrefix, StringComparison.Ordinal) ||
                    k.StartsWith(KeePassOtpFields.HmacPrefix, StringComparison.Ordinal))
                    e.Strings.Remove(k);
        }

        /// <summary>KeeOtp: key=SECRET&amp;size=6&amp;step=30&amp;type=Totp&amp;counter=0&amp;otpHashMode=Sha1&amp;encoding=Base32</summary>
        internal static OtpParameters ParseKeeOtp(string s)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in s.Split('&'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0) d[part.Substring(0, eq).Trim()] = Uri.UnescapeDataString(part.Substring(eq + 1).Trim());
            }

            string key;
            if (!d.TryGetValue("key", out key) || key.Length == 0) return null;

            OtpParameters p = new OtpParameters();
            string v;
            string encoding = d.TryGetValue("encoding", out v) ? v.ToLowerInvariant() : "base32";
            try
            {
                if (encoding == "base64") p.Secret = Convert.FromBase64String(key);
                else if (encoding == "hex") p.Secret = Hex(key); // throws FormatException/ArgumentException; caught in Read
                else if (encoding == "utf8") p.Secret = Encoding.UTF8.GetBytes(key);
                else p.Secret = Base32.Decode(key);
            }
            catch (FormatException) { return null; }
            catch (ArgumentException) { return null; } // Convert.ToByte("-1", 16)
            if (p.Secret == null || p.Secret.Length == 0) return null;

            int n;
            if (d.TryGetValue("size", out v) && int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out n) &&
                n >= Hotp.MinDigits && n <= Hotp.MaxDigits) p.Digits = n;
            if (d.TryGetValue("step", out v) && int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out n) &&
                n >= 1 && n <= Totp.MaxPeriod) p.Period = n;
            if (d.TryGetValue("otpHashMode", out v))
            {
                if (v.Equals("Sha256", StringComparison.OrdinalIgnoreCase)) p.Algorithm = OtpAlgorithm.Sha256;
                else if (v.Equals("Sha512", StringComparison.OrdinalIgnoreCase)) p.Algorithm = OtpAlgorithm.Sha512;
            }
            if (d.TryGetValue("type", out v) && v.Equals("Hotp", StringComparison.OrdinalIgnoreCase))
            {
                p.Type = OtpType.Hotp;
                ulong c;
                if (d.TryGetValue("counter", out v) && ulong.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out c))
                    p.Counter = c;
            }
            return p;
        }

        /// <summary>KeeTrayTOTP: seed in Base32, settings "period;digits" or "period;S" for Steam.</summary>
        internal static OtpParameters ParseTrayTotp(string seed, string settings)
        {
            byte[] secret;
            if (!Base32.TryDecode(seed, out secret) || secret.Length == 0) return null;

            OtpParameters p = new OtpParameters();
            p.Secret = secret;
            string[] parts = (settings ?? string.Empty).Split(';');
            int n;
            if (parts.Length > 0 && int.TryParse(parts[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out n) &&
                n >= 1 && n <= Totp.MaxPeriod) p.Period = n;
            if (parts.Length > 1)
            {
                string d = parts[1].Trim();
                if (d.Equals("S", StringComparison.OrdinalIgnoreCase)) { p.IsSteam = true; p.Digits = SteamGuard.Length; }
                else if (int.TryParse(d, NumberStyles.None, CultureInfo.InvariantCulture, out n) &&
                    n >= Hotp.MinDigits && n <= Hotp.MaxDigits) p.Digits = n;
            }
            return p;
        }

        private static byte[] Hex(string s)
        {
            s = s.Replace(" ", string.Empty);
            if (s.Length % 2 != 0) throw new FormatException();
            byte[] b = new byte[s.Length / 2];
            for (int i = 0; i < b.Length; ++i) b[i] = Convert.ToByte(s.Substring(2 * i, 2), 16);
            return b;
        }
    }
}
