// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Text;

using KeePassLib;
using KeePassLib.Security;

using RamzXOtp.Otp;

namespace RamzXOtp.Storage
{
    /// <summary>Creates KeePass entries for imported OTP accounts.</summary>
    public static class AccountImporter
    {
        /// <summary>Longest service or account name taken over from an import.</summary>
        public const int MaxNameLength = 200;

        /// <summary>True if an entry in the database already has an OTP with this secret.</summary>
        public static bool Exists(PwDatabase db, byte[] secret)
        {
            if (db == null || db.RootGroup == null || secret == null) return false;
            bool found = false;
            db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, e =>
            {
                if (!OtpStore.HasOtp(e)) return true;
                OtpParameters p = OtpStore.Read(e);
                if (p == null) return true;
                try { found = SameSecret(p.Secret, secret); }
                finally { p.Clear(); }
                return !found; // stop when found
            });
            return found;
        }

        /// <summary>Title = issuer (or account), user name = account. Returns the new entries.</summary>
        public static List<PwEntry> CreateEntries(PwDatabase db, PwGroup group, IEnumerable<OtpParameters> accounts)
        {
            if (db == null) throw new ArgumentNullException("db");
            if (group == null) throw new ArgumentNullException("group");

            List<PwEntry> created = new List<PwEntry>();
            foreach (OtpParameters p in accounts)
            {
                // Names come from a QR code, i.e. from outside: make them plain text first.
                p.Issuer = SafeName(p.Issuer);
                p.Account = SafeName(p.Account);

                PwEntry e = new PwEntry(true, true);
                string title = p.Issuer ?? p.Account ?? "OTP";
                e.Strings.Set(PwDefs.TitleField, new ProtectedString(db.MemoryProtection.ProtectTitle, title));
                if (p.Account != null)
                    e.Strings.Set(PwDefs.UserNameField, new ProtectedString(db.MemoryProtection.ProtectUserName, p.Account));
                OtpStore.Write(e, p);
                group.AddEntry(e, true);
                created.Add(e);
            }
            if (created.Count > 0) db.Modified = true;
            return created;
        }

        /// <summary>
        /// Plain text for a title or user name: no control characters, at most MaxNameLength
        /// characters, and no curly braces. KeePass treats {...} in field values as placeholders
        /// (e.g. {CMD:...} during auto-type), so a crafted QR code must not be able to plant one.
        /// </summary>
        public static string SafeName(string s)
        {
            if (s == null) return null;
            StringBuilder sb = new StringBuilder(Math.Min(s.Length, MaxNameLength));
            foreach (char c in s)
            {
                if (sb.Length >= MaxNameLength || (sb.Length == MaxNameLength - 1 && char.IsHighSurrogate(c))) break;
                if (c == '{') sb.Append('(');
                else if (c == '}') sb.Append(')');
                else if (char.IsControl(c)) sb.Append(' ');
                else sb.Append(c);
            }
            string r = sb.ToString().Trim();
            return r.Length == 0 ? null : r;
        }

        /// <summary>Compares two secrets in constant time.</summary>
        public static bool SameSecret(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int d = 0;
            for (int i = 0; i < a.Length; ++i) d |= a[i] ^ b[i];
            return d == 0;
        }
    }
}
