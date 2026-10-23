// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;

using KeePassLib;
using KeePassLib.Security;

using RamzXOtp.Otp;

namespace RamzXOtp.Storage
{
    /// <summary>Creates KeePass entries for imported OTP accounts.</summary>
    public static class AccountImporter
    {
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
                try { found = Equal(p.Secret, secret); }
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
                PwEntry e = new PwEntry(true, true);
                string title = !string.IsNullOrEmpty(p.Issuer) ? p.Issuer : (p.Account ?? "OTP");
                e.Strings.Set(PwDefs.TitleField, new ProtectedString(db.MemoryProtection.ProtectTitle, title));
                if (!string.IsNullOrEmpty(p.Account))
                    e.Strings.Set(PwDefs.UserNameField, new ProtectedString(db.MemoryProtection.ProtectUserName, p.Account));
                OtpStore.Write(e, p);
                group.AddEntry(e, true);
                created.Add(e);
            }
            if (created.Count > 0) db.Modified = true;
            return created;
        }

        private static bool Equal(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int d = 0;
            for (int i = 0; i < a.Length; ++i) d |= a[i] ^ b[i];
            return d == 0;
        }
    }
}
