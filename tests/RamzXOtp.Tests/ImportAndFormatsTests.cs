// RamzX OTP - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

using RamzXOtp.Otp;
using RamzXOtp.Qr;
using RamzXOtp.Storage;
using RamzXOtp.UI;

using Xunit;

namespace RamzXOtp.Tests
{
    /// <summary>Builds Google Authenticator export payloads (protobuf) for the tests.</summary>
    internal static class Proto
    {
        internal static void Varint(List<byte> o, ulong v)
        {
            while (v >= 0x80) { o.Add((byte)(v | 0x80)); v >>= 7; }
            o.Add((byte)v);
        }

        internal static void Key(List<byte> o, int field, int wire) { Varint(o, (ulong)(field << 3 | wire)); }

        internal static void Bytes(List<byte> o, int field, byte[] b)
        {
            Key(o, field, 2);
            Varint(o, (ulong)b.Length);
            o.AddRange(b);
        }

        internal static void Int(List<byte> o, int field, long v) { Key(o, field, 0); Varint(o, (ulong)v); }

        internal static byte[] Account(byte[] secret, string name, string issuer, int alg, int digits, int type, long counter = 0)
        {
            List<byte> a = new List<byte>();
            if (secret != null) Bytes(a, 1, secret);
            if (name != null) Bytes(a, 2, Encoding.UTF8.GetBytes(name));
            if (issuer != null) Bytes(a, 3, Encoding.UTF8.GetBytes(issuer));
            Int(a, 4, alg);
            Int(a, 5, digits);
            Int(a, 6, type);
            if (counter != 0) Int(a, 7, counter);
            return a.ToArray();
        }

        internal static string Uri(IEnumerable<byte[]> accounts, int batchSize = 1, int batchIndex = 0, int batchId = 42,
            bool urlSafe = false, bool extraFields = false)
        {
            List<byte> p = new List<byte>();
            foreach (byte[] a in accounts) Bytes(p, 1, a);
            Int(p, 2, 1);
            Int(p, 3, batchSize);
            Int(p, 4, batchIndex);
            Int(p, 5, batchId);
            if (extraFields)
            {
                Key(p, 9, 1); p.AddRange(new byte[8]); // fixed64
                Key(p, 10, 5); p.AddRange(new byte[4]); // fixed32
                Bytes(p, 11, new byte[] { 1, 2, 3 });
            }
            string b64 = Convert.ToBase64String(p.ToArray());
            if (urlSafe) b64 = b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return "otpauth-migration://offline?data=" + System.Uri.EscapeDataString(b64);
        }
    }

    public class GoogleMigrationTests
    {
        private static readonly byte[] S1 = Encoding.ASCII.GetBytes("12345678901234567890");
        private static readonly byte[] S2 = Encoding.ASCII.GetBytes("abcdefghijabcdefghij");

        [Fact]
        public void TwoAccounts_AllFieldsMapped()
        {
            string uri = Proto.Uri(new[]
            {
                Proto.Account(S1, "GitHub:alice@example.com", "GitHub", 1, 1, 2),
                Proto.Account(S2, "bob", "Acme", 2, 2, 1, 7),
            }, 3, 1, 99);

            MigrationBatch b = GoogleMigration.Parse(uri);
            Assert.Equal(3, b.BatchSize);
            Assert.Equal(1, b.BatchIndex);
            Assert.Equal(99, b.BatchId);
            Assert.Equal(2, b.Accounts.Count);

            OtpParameters a = b.Accounts[0].Parameters;
            Assert.Null(b.Accounts[0].Unsupported);
            Assert.Equal("GitHub", a.Issuer);
            Assert.Equal("alice@example.com", a.Account);
            Assert.Equal(S1, a.Secret);
            Assert.Equal(OtpType.Totp, a.Type);
            Assert.Equal(OtpAlgorithm.Sha1, a.Algorithm);
            Assert.Equal(6, a.Digits);
            Assert.Equal(30, a.Period);
            Assert.Equal("287082", a.CurrentCode(59));

            OtpParameters c = b.Accounts[1].Parameters;
            Assert.Equal("Acme", c.Issuer);
            Assert.Equal("bob", c.Account);
            Assert.Equal(OtpAlgorithm.Sha256, c.Algorithm);
            Assert.Equal(8, c.Digits);
            Assert.Equal(OtpType.Hotp, c.Type);
            Assert.Equal(7ul, c.Counter);
        }

        [Theory]
        [InlineData(0, 6)]
        [InlineData(1, 6)]
        [InlineData(2, 8)]
        [InlineData(6, 6)]   // some exports store the plain number
        [InlineData(7, 7)]
        [InlineData(8, 8)]
        public void Digits_EnumOrPlainNumber(int stored, int expected)
        {
            MigrationBatch b = GoogleMigration.Parse(Proto.Uri(new[] { Proto.Account(S1, "x", null, 1, stored, 2) }));
            Assert.Equal(expected, b.Accounts[0].Parameters.Digits);
            Assert.Null(b.Accounts[0].Unsupported);
        }

        [Fact]
        public void Unsupported_Md5_NoSecret_BadDigits()
        {
            MigrationBatch b = GoogleMigration.Parse(Proto.Uri(new[]
            {
                Proto.Account(S1, "md5", null, 4, 1, 2),
                Proto.Account(null, "nosecret", null, 1, 1, 2),
                Proto.Account(S1, "digits", null, 1, 9, 2),
            }));
            Assert.All(b.Accounts, a => Assert.NotNull(a.Unsupported));
        }

        [Fact]
        public void NameWithoutIssuer_AndIssuerFromName()
        {
            MigrationBatch b = GoogleMigration.Parse(Proto.Uri(new[]
            {
                Proto.Account(S1, "Dropbox:me@example.com", null, 1, 1, 2),
                Proto.Account(S2, "plain-name", null, 1, 1, 2),
                Proto.Account(S2, "Other:me", "Different", 1, 1, 2),
            }));
            Assert.Equal("Dropbox", b.Accounts[0].Parameters.Issuer);
            Assert.Equal("me@example.com", b.Accounts[0].Parameters.Account);
            Assert.Null(b.Accounts[1].Parameters.Issuer);
            Assert.Equal("plain-name", b.Accounts[1].Parameters.Account);
            Assert.Equal("Other:me", b.Accounts[2].Parameters.Account); // issuer does not match: keep name
        }

        [Fact]
        public void UrlSafeBase64_AndUnknownFields()
        {
            MigrationBatch b = GoogleMigration.Parse(Proto.Uri(new[] { Proto.Account(S1, "x", "y", 1, 1, 2) },
                urlSafe: true, extraFields: true));
            Assert.Single(b.Accounts);
            Assert.Equal(S1, b.Accounts[0].Parameters.Secret);
        }

        [Theory]
        [InlineData("otpauth://totp/x?secret=ABC")]
        [InlineData("otpauth-migration://offline")]
        [InlineData("otpauth-migration://offline?data=")]
        [InlineData("otpauth-migration://offline?data=%%%%")]
        [InlineData("otpauth-migration://offline?data=CgQKAg")] // truncated protobuf
        public void Invalid_Throws(string uri)
        {
            Assert.Throws<FormatException>(() => GoogleMigration.Parse(uri));
        }

        [Fact]
        public void ExportQr_ScannedFromImage()
        {
            string uri = Proto.Uri(new[] { Proto.Account(S1, "GitHub:alice", "GitHub", 1, 1, 2) });
            using (Bitmap bmp = QrImageTests.Render(QrEncoder.Encode(uri, QrEcLevel.L), 4, 4, Color.Black, Color.White))
            {
                string read = QrDecoder.Decode(bmp);
                Assert.True(GoogleMigration.LooksLikeMigration(read));
                Assert.Equal("GitHub", GoogleMigration.Parse(read).Accounts[0].Parameters.Issuer);
            }
        }
    }

    public class SteamGuardTests
    {
        private static readonly byte[] S = Encoding.ASCII.GetBytes("12345678901234567890");

        [Fact]
        public void FiveCharacters_FromTheSteamAlphabet_LeastSignificantFirst()
        {
            string code = SteamGuard.Compute(S, 59);
            Assert.Equal(5, code.Length);
            Assert.All(code, c => Assert.Contains(c, SteamGuard.Alphabet));

            // Independent recomputation from the 31-bit HOTP value.
            uint v = Hotp.Truncate(S, 1, OtpAlgorithm.Sha1);
            string expected = string.Empty;
            for (int i = 0; i < 5; ++i) { expected += SteamGuard.Alphabet[(int)(v % 26)]; v /= 26; }
            Assert.Equal(expected, code);
        }

        [Fact]
        public void ChangesEvery30Seconds()
        {
            Assert.Equal(SteamGuard.Compute(S, 60), SteamGuard.Compute(S, 89));
            Assert.NotEqual(SteamGuard.Compute(S, 60), SteamGuard.Compute(S, 90));
        }

        [Fact]
        public void SteamLink_RoundTrip()
        {
            OtpParameters p = OtpAuthUri.Parse("otpauth://totp/Steam:me?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Steam&encoder=steam");
            Assert.True(p.IsSteam);
            Assert.Equal(5, p.Digits);
            string uri = OtpAuthUri.Format(p);
            Assert.DoesNotContain("digits=", uri);
            Assert.Equal(p.CurrentCode(1000), OtpAuthUri.Parse(uri).CurrentCode(1000));
        }
    }

    public class OtpStoreTests
    {
        private const string B32 = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        private static readonly byte[] S = Encoding.ASCII.GetBytes("12345678901234567890");

        private static PwEntry Entry(params string[] fields)
        {
            PwEntry e = new PwEntry(true, true);
            for (int i = 0; i < fields.Length; i += 2) e.Strings.Set(fields[i], new ProtectedString(true, fields[i + 1]));
            return e;
        }

        [Theory]
        [InlineData("otp", "otpauth://totp/x?secret=" + B32 + "&digits=8&period=60&algorithm=SHA256", OtpSource.OtpUri)]
        [InlineData("otp", "key=" + B32 + "&size=8&step=60&otpHashMode=Sha256", OtpSource.KeeOtp)]
        public void Read_LegacyOtpField(string field, string value, OtpSource expected)
        {
            OtpSource s;
            OtpParameters p = OtpStore.Read(Entry(field, value), out s);
            Assert.Equal(expected, s);
            Assert.Equal(S, p.Secret);
            Assert.Equal(8, p.Digits);
            Assert.Equal(60, p.Period);
            Assert.Equal(OtpAlgorithm.Sha256, p.Algorithm);
        }

        [Fact]
        public void Read_KeeOtpEncodingsAndHotp()
        {
            Assert.Equal(S, OtpStore.ParseKeeOtp("key=" + Convert.ToBase64String(S) + "&encoding=Base64").Secret);
            Assert.Equal(S, OtpStore.ParseKeeOtp("key=3132333435363738393031323334353637383930&encoding=Hex").Secret);
            Assert.Equal(S, OtpStore.ParseKeeOtp("key=12345678901234567890&encoding=UTF8").Secret);
            OtpParameters h = OtpStore.ParseKeeOtp("key=" + B32 + "&type=Hotp&counter=5&otpHashMode=Sha512");
            Assert.Equal(OtpType.Hotp, h.Type);
            Assert.Equal(5ul, h.Counter);
            Assert.Equal(OtpAlgorithm.Sha512, h.Algorithm);
            Assert.Null(OtpStore.ParseKeeOtp("size=6"));
            Assert.Null(OtpStore.ParseKeeOtp("key=!!!"));
        }

        [Fact]
        public void Read_TrayTotp_NormalAndSteam()
        {
            OtpSource s;
            OtpParameters p = OtpStore.Read(Entry("TOTP Seed", B32, "TOTP Settings", "60;8"), out s);
            Assert.Equal(OtpSource.TrayTotp, s);
            Assert.Equal(60, p.Period);
            Assert.Equal(8, p.Digits);

            OtpParameters steam = OtpStore.Read(Entry("TOTP Seed", B32, "TOTP Settings", "30;S"));
            Assert.True(steam.IsSteam);
            Assert.Equal(5, steam.CurrentCode(59).Length);

            Assert.Null(OtpStore.Read(Entry("TOTP Seed", "!!!")));
            Assert.Equal(30, OtpStore.Read(Entry("TOTP Seed", B32)).Period); // no settings: defaults
        }

        [Fact]
        public void Native_HasPriority()
        {
            PwEntry e = Entry("otp", "otpauth://totp/x?secret=MZXW6YTBOI", "TimeOtp-Secret-Base32", B32);
            OtpSource s;
            Assert.Equal(S, OtpStore.Read(e, out s).Secret);
            Assert.Equal(OtpSource.Native, s);
        }

        [Fact]
        public void Write_Native_RemovesLegacyFields()
        {
            PwEntry e = Entry("otp", "key=" + B32, "TOTP Seed", B32, "TOTP Settings", "30;6");
            OtpStore.Write(e, OtpStore.Read(e));
            Assert.False(e.Strings.Exists("otp"));
            Assert.False(e.Strings.Exists("TOTP Seed"));
            Assert.False(e.Strings.Exists("TOTP Settings"));
            Assert.Equal(B32, e.Strings.ReadSafe("TimeOtp-Secret-Base32"));
        }

        [Fact]
        public void Write_Steam_GoesToOtpField_AndRemovesNative()
        {
            PwEntry e = Entry("TimeOtp-Secret-Base32", "MZXW6YTBOI", "TimeOtp-Period", "30");
            OtpParameters steam = OtpAuthUri.Parse("otpauth://totp/Steam:me?secret=" + B32 + "&encoder=steam");
            OtpStore.Write(e, steam);
            Assert.False(e.Strings.Exists("TimeOtp-Secret-Base32"));
            Assert.False(e.Strings.Exists("TimeOtp-Period"));
            Assert.True(e.Strings.Get("otp").IsProtected);
            Assert.True(OtpStore.Read(e).IsSteam);
        }

        [Fact]
        public void FindLegacy_OnlyEntriesThatNeedWork()
        {
            PwDatabase db = new PwDatabase();
            db.RootGroup = new PwGroup(true, true);
            PwEntry keeOtp = Entry("otp", "key=" + B32);
            PwEntry tray = Entry("TOTP Seed", B32);
            PwEntry xc = Entry("otp", "otpauth://totp/x?secret=" + B32);
            PwEntry native = Entry("TimeOtp-Secret-Base32", B32);
            PwEntry nativePlusLeftover = Entry("TimeOtp-Secret-Base32", B32, "TOTP Seed", B32);
            PwEntry steam = Entry("otp", "otpauth://totp/s?secret=" + B32 + "&encoder=steam");
            PwEntry none = Entry("Notes", "nothing");
            foreach (PwEntry e in new[] { keeOtp, tray, xc, native, nativePlusLeftover, steam, none }) db.RootGroup.AddEntry(e, true);

            List<PwEntry> legacy = OtpStore.FindLegacy(db);
            Assert.Contains(keeOtp, legacy);
            Assert.Contains(tray, legacy);
            Assert.Contains(xc, legacy);
            Assert.Contains(nativePlusLeftover, legacy);
            Assert.DoesNotContain(native, legacy);
            Assert.DoesNotContain(steam, legacy); // already in its best format
            Assert.DoesNotContain(none, legacy);
            Assert.Empty(OtpStore.FindLegacy(null));
        }
    }

    public class AccountImporterTests
    {
        [Fact]
        public void CreateEntries_AndDetectExisting()
        {
            PwDatabase db = new PwDatabase();
            db.RootGroup = new PwGroup(true, true);
            PwGroup g = new PwGroup(true, true, "2FA", PwIcon.Key);
            db.RootGroup.AddGroup(g, true);

            OtpParameters a = OtpAuthUri.Parse("otpauth://totp/GitHub:alice?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=GitHub");
            OtpParameters b = OtpAuthUri.Parse("otpauth://totp/noissuer?secret=MZXW6YTBOI");
            Assert.False(AccountImporter.Exists(db, a.Secret));

            List<PwEntry> created = AccountImporter.CreateEntries(db, g, new[] { a, b });
            Assert.Equal(2, created.Count);
            Assert.Equal(2u, g.GetEntriesCount(false));
            Assert.Equal("GitHub", created[0].Strings.ReadSafe(PwDefs.TitleField));
            Assert.Equal("alice", created[0].Strings.ReadSafe(PwDefs.UserNameField));
            Assert.Equal("noissuer", created[1].Strings.ReadSafe(PwDefs.TitleField));
            Assert.True(db.Modified);

            Assert.True(AccountImporter.Exists(db, a.Secret));
            Assert.False(AccountImporter.Exists(db, new byte[] { 1, 2, 3 }));
            Assert.Throws<ArgumentNullException>(() => AccountImporter.CreateEntries(null, g, new OtpParameters[0]));
        }
    }

    public class ColumnAndQrViewTests
    {
        private const string Uri = "otpauth://totp/Test:me?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Test";

        [Fact]
        public void ColumnText_ShowsCode_OrNothing()
        {
            PwEntry e = new PwEntry(true, true);
            Assert.Equal(string.Empty, OtpColumnProvider.CellText(e, null));

            OtpStore.Write(e, OtpAuthUri.Parse(Uri));
            string cell = OtpColumnProvider.CellText(e, null);
            Assert.Matches(@"^\d{3} \d{3}   \d+ s$", cell);
        }

        [Fact]
        public void ShowQr_RenderedCodeIsReadable()
        {
            OtpParameters p = OtpAuthUri.Parse(Uri);
            using (Bitmap bmp = ShowQrForm.Render(QrEncoder.Encode(OtpAuthUri.Format(p)), 4))
            {
                OtpParameters back = OtpAuthUri.Parse(QrDecoder.Decode(bmp));
                Assert.Equal(p.Secret, back.Secret);
                Assert.Equal("Test", back.Issuer);
            }
        }

        [Fact]
        public void ImportForm_CollectsAccountsFromAnExport()
        {
            PwDatabase db = new PwDatabase();
            db.RootGroup = new PwGroup(true, true);
            string export = Proto.Uri(new[]
            {
                Proto.Account(Encoding.ASCII.GetBytes("12345678901234567890"), "GitHub:alice", "GitHub", 1, 1, 2),
                Proto.Account(Encoding.ASCII.GetBytes("abcdefghijabcdefghij"), "md5", null, 4, 1, 2),
            }, 2, 0, 5);

            using (ImportForm f = new ImportForm(db, "Root"))
            {
                f.Add(export);
                f.Add(export); // same QR twice: ignored
                ListView list = (ListView)typeof(ImportForm).GetField("m_list", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(f);
                Assert.Equal(2, list.Items.Count);
                Assert.True(list.Items[0].Checked);   // importable
                Assert.False(list.Items[1].Checked);  // MD5 unsupported

                typeof(ImportForm).GetMethod("CollectSelection", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(f, null);
                Assert.Single(f.Selected);
                Assert.Equal("GitHub", f.Selected[0].Issuer);
            }
        }
    }
}
