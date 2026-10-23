// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System.Collections.Generic;
using System.Globalization;

namespace RamzXOtp.UI
{
    /// <summary>User-visible texts in English, German and Arabic.</summary>
    internal static class Strings
    {
        internal static readonly string[] Languages = { "en", "de", "ar" };

        private static Dictionary<string, string> s_current = En();
        private static string s_lang = "en";

        internal static string Language { get { return s_lang; } }
        internal static bool RightToLeft { get { return s_lang == "ar"; } }

        /// <summary>KeePass UI language if translated, otherwise the Windows display language.</summary>
        internal static void Select(string keePassIso6391)
        {
            string lang = (keePassIso6391 ?? string.Empty).Trim().ToLowerInvariant();
            if (lang.Length == 0 || lang == "en") lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            SetLanguage(lang);
        }

        internal static void SetLanguage(string lang)
        {
            switch ((lang ?? string.Empty).ToLowerInvariant())
            {
                case "de": s_current = De(); s_lang = "de"; break;
                case "ar": s_current = Ar(); s_lang = "ar"; break;
                default: s_current = En(); s_lang = "en"; break;
            }
        }

        internal static string Get(string key)
        {
            string s;
            if (s_current.TryGetValue(key, out s)) return s;
            if (En().TryGetValue(key, out s)) return s;
            return key;
        }

        internal static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key), args);
        }

        internal static Dictionary<string, string> Table(string lang)
        {
            switch (lang)
            {
                case "de": return De();
                case "ar": return Ar();
                default: return En();
            }
        }

        internal const string MenuAdd = "MenuAdd";
        internal const string MenuCopy = "MenuCopy";
        internal const string MenuCopyNone = "MenuCopyNone";
        internal const string CodeCopied = "CodeCopied";
        internal const string FormTitle = "FormTitle";
        internal const string InputLabel = "InputLabel";
        internal const string ShowSecret = "ShowSecret";
        internal const string Algorithm = "Algorithm";
        internal const string Digits = "Digits";
        internal const string Period = "Period";
        internal const string StatusEmpty = "StatusEmpty";
        internal const string StatusInvalid = "StatusInvalid";
        internal const string StatusShort = "StatusShort";
        internal const string StatusOk = "StatusOk";
        internal const string StatusNotNative = "StatusNotNative";
        internal const string PreviewLabel = "PreviewLabel";
        internal const string Remaining = "Remaining";
        internal const string SameDbNote = "SameDbNote";
        internal const string ConfirmReplace = "ConfirmReplace";
        internal const string Saved = "Saved";
        internal const string SelectOneEntry = "SelectOneEntry";
        internal const string ScanScreen = "ScanScreen";
        internal const string FromClipboard = "FromClipboard";
        internal const string FromFile = "FromFile";
        internal const string ScanHint = "ScanHint";
        internal const string QrNotFound = "QrNotFound";
        internal const string QrNotOtp = "QrNotOtp";
        internal const string QrMigration = "QrMigration";
        internal const string NoImage = "NoImage";
        internal const string ImportCaption = "ImportCaption";
        internal const string SettingsCaption = "SettingsCaption";
        internal const string BannerSubtitle = "BannerSubtitle";
        internal const string MenuRequireHello = "MenuRequireHello";
        internal const string MenuRequireHelloMissing = "MenuRequireHelloMissing";
        internal const string RequireHelloNote = "RequireHelloNote";
        internal const string MenuCopyProtected = "MenuCopyProtected";
        internal const string HelloCopyReason = "HelloCopyReason";
        internal const string HelloEnableReason = "HelloEnableReason";
        internal const string HelloNotEnabled = "HelloNotEnabled";
        internal const string HelloDenied = "HelloDenied";
        internal const string UnlockMissing = "UnlockMissing";
        internal const string Copy = "Copy";
        internal const string Copied = "Copied";
        internal const string Save = "Save";
        internal const string Cancel = "Cancel";

        private static Dictionary<string, string> En()
        {
            return new Dictionary<string, string>
            {
                { MenuAdd, "Add or replace one-time password..." },
                { MenuCopy, "Copy code   {0}" },
                { MenuCopyNone, "Copy code (no one-time password in this entry)" },
                { CodeCopied, "One-time password copied to the clipboard." },
                { FormTitle, "RamzX OTP - {0}" },
                { InputLabel, "2. ...or paste the otpauth:// link or the secret key (Base32)" },
                { ShowSecret, "Show" },
                { Algorithm, "Algorithm:" },
                { Digits, "Digits:" },
                { Period, "Period (seconds):" },
                { StatusEmpty, "Paste a link or a secret key." },
                { StatusInvalid, "Not valid: {0}" },
                { StatusShort, "Warning: this key is shorter than 128 bits. It works, but the service uses a weak key." },
                { StatusOk, "Recognized: {0}" },
                { StatusNotNative, "Cannot be saved yet: {0}" },
                { PreviewLabel, "Current code:" },
                { Remaining, "{0} s" },
                { SameDbNote, "Note: an OTP secret stored in the same database as the password is not a real second factor." },
                { ConfirmReplace, "This entry already has a one-time password. Replace it?\r\n\r\nThe old values stay in the entry history." },
                { Saved, "One-time password saved.\r\n\r\nKeePass shows the code with Ctrl+Shift+T and copies it with Ctrl+T. Auto-Type: {TIMEOTP}" },
                { SelectOneEntry, "Please select exactly one entry." },
                { ScanScreen, "Scan QR from screen" },
                { FromClipboard, "From clipboard" },
                { FromFile, "Open image..." },
                { ScanHint, "Click on the QR code  ·  Esc = cancel" },
                { QrNotFound, "No QR code found.\r\n\r\nTip: zoom in so the QR code is larger, or copy it as an image (Win+Shift+S) and use \"From clipboard\"." },
                { QrNotOtp, "This QR code is not an otpauth:// link for one-time passwords." },
                { QrMigration, "This is a Google Authenticator export with several accounts. Importing it will come in a later version." },
                { NoImage, "The clipboard contains neither an image nor an otpauth:// link." },
                { ImportCaption, "1. Get the QR code" },
                { SettingsCaption, "Settings (only for a pasted secret key)" },
                { BannerSubtitle, "One-time password for: {0}" },
                { MenuRequireHello, "Windows Hello before copying (Miftawerk Unlock)" },
                { MenuRequireHelloMissing, "Windows Hello before copying (requires Miftawerk Unlock)" },
                { RequireHelloNote, "Applies to RamzX OTP. KeePass's own commands (Ctrl+T, {TIMEOTP}) are not affected." },
                { MenuCopyProtected, "Copy code (after Windows Hello)" },
                { HelloCopyReason, "RamzX OTP: copy the one-time password for \"{0}\"" },
                { HelloEnableReason, "RamzX OTP: turn on Windows Hello protection for one-time passwords" },
                { HelloNotEnabled, "Windows Hello was not confirmed. The protection stays off." },
                { HelloDenied, "Windows Hello was not confirmed. The code was not copied." },
                { UnlockMissing, "This protection needs the Miftawerk Unlock plugin, which is not loaded. The code was not copied.\r\n\r\nYou can switch the protection off in the RamzX OTP menu." },
                { Copy, "Copy" },                { Copied, "Copied ✓" },
                { Save, "Save" },
                { Cancel, "Cancel" },
            };
        }

        private static Dictionary<string, string> De()
        {
            return new Dictionary<string, string>
            {
                { MenuAdd, "Einmalpasswort hinzufügen oder ersetzen..." },
                { MenuCopy, "Code kopieren   {0}" },
                { MenuCopyNone, "Code kopieren (kein Einmalpasswort in diesem Eintrag)" },
                { CodeCopied, "Einmalpasswort in die Zwischenablage kopiert." },
                { FormTitle, "RamzX OTP - {0}" },
                { InputLabel, "2. ...oder otpauth://-Link bzw. geheimen Schlüssel (Base32) einfügen" },
                { ShowSecret, "Anzeigen" },
                { Algorithm, "Algorithmus:" },
                { Digits, "Stellen:" },
                { Period, "Gültigkeit (Sekunden):" },
                { StatusEmpty, "Link oder geheimen Schlüssel einfügen." },
                { StatusInvalid, "Ungültig: {0}" },
                { StatusShort, "Warnung: Dieser Schlüssel ist kürzer als 128 Bit. Er funktioniert, aber der Dienst verwendet einen schwachen Schlüssel." },
                { StatusOk, "Erkannt: {0}" },
                { StatusNotNative, "Kann noch nicht gespeichert werden: {0}" },
                { PreviewLabel, "Aktueller Code:" },
                { Remaining, "{0} s" },
                { SameDbNote, "Hinweis: Ein OTP-Schlüssel in derselben Datenbank wie das Passwort ist kein echter zweiter Faktor." },
                { ConfirmReplace, "Dieser Eintrag hat bereits ein Einmalpasswort. Ersetzen?\r\n\r\nDie alten Werte bleiben in der Eintragshistorie." },
                { Saved, "Einmalpasswort gespeichert.\r\n\r\nKeePass zeigt den Code mit Strg+Umschalt+T und kopiert ihn mit Strg+T. Auto-Type: {TIMEOTP}" },
                { SelectOneEntry, "Bitte genau einen Eintrag auswählen." },
                { ScanScreen, "QR vom Bildschirm scannen" },
                { FromClipboard, "Aus Zwischenablage" },
                { FromFile, "Bild öffnen..." },
                { ScanHint, "Auf den QR-Code klicken  ·  Esc = Abbrechen" },
                { QrNotFound, "Kein QR-Code gefunden.\r\n\r\nTipp: Vergrößern Sie die Ansicht, damit der QR-Code größer ist, oder kopieren Sie ihn als Bild (Win+Umschalt+S) und nutzen Sie \"Aus Zwischenablage\"." },
                { QrNotOtp, "Dieser QR-Code ist kein otpauth://-Link für Einmalpasswörter." },
                { QrMigration, "Das ist ein Google-Authenticator-Export mit mehreren Konten. Der Import folgt in einer späteren Version." },
                { NoImage, "Die Zwischenablage enthält weder ein Bild noch einen otpauth://-Link." },
                { ImportCaption, "1. QR-Code holen" },
                { SettingsCaption, "Einstellungen (nur bei eingefügtem geheimen Schlüssel)" },
                { BannerSubtitle, "Einmalpasswort für: {0}" },
                { MenuRequireHello, "Vor dem Kopieren Windows Hello verlangen (Miftawerk Unlock)" },
                { MenuRequireHelloMissing, "Vor dem Kopieren Windows Hello verlangen (benötigt Miftawerk Unlock)" },
                { RequireHelloNote, "Gilt für RamzX OTP. KeePass-eigene Befehle (Strg+T, {TIMEOTP}) sind nicht betroffen." },
                { MenuCopyProtected, "Code kopieren (nach Windows Hello)" },
                { HelloCopyReason, "RamzX OTP: Einmalpasswort für \"{0}\" kopieren" },
                { HelloEnableReason, "RamzX OTP: Windows-Hello-Schutz für Einmalpasswörter einschalten" },
                { HelloNotEnabled, "Windows Hello wurde nicht bestätigt. Der Schutz bleibt aus." },
                { HelloDenied, "Windows Hello wurde nicht bestätigt. Der Code wurde nicht kopiert." },
                { UnlockMissing, "Dieser Schutz benötigt das Plugin Miftawerk Unlock, das nicht geladen ist. Der Code wurde nicht kopiert.\r\n\r\nSie können den Schutz im RamzX-OTP-Menü ausschalten." },
                { Copy, "Kopieren" },                { Copied, "Kopiert ✓" },
                { Save, "Speichern" },
                { Cancel, "Abbrechen" },
            };
        }

        private static Dictionary<string, string> Ar()
        {
            return new Dictionary<string, string>
            {
                { MenuAdd, "إضافة أو استبدال رمز لمرة واحدة..." },
                { MenuCopy, "نسخ الرمز   {0}" },
                { MenuCopyNone, "نسخ الرمز (لا يوجد رمز في هذا الإدخال)" },
                { CodeCopied, "تم نسخ الرمز إلى الحافظة." },
                { FormTitle, "RamzX OTP - {0}" },
                { InputLabel, "2. ...أو الصق رابط otpauth:// أو المفتاح السري (Base32)" },
                { ShowSecret, "إظهار" },
                { Algorithm, "الخوارزمية:" },
                { Digits, "عدد الأرقام:" },
                { Period, "المدة (بالثواني):" },
                { StatusEmpty, "الصق رابطاً أو مفتاحاً سرياً." },
                { StatusInvalid, "غير صالح: {0}" },
                { StatusShort, "تنبيه: هذا المفتاح أقصر من 128 بت. سيعمل، لكن الخدمة تستخدم مفتاحاً ضعيفاً." },
                { StatusOk, "تم التعرّف: {0}" },
                { StatusNotNative, "لا يمكن حفظه بعد: {0}" },
                { PreviewLabel, "الرمز الحالي:" },
                { Remaining, "{0} ث" },
                { SameDbNote, "ملاحظة: حفظ مفتاح الرمز في نفس قاعدة كلمة السر ليس عامل تحقق ثانياً حقيقياً." },
                { ConfirmReplace, "هذا الإدخال فيه رمز لمرة واحدة. هل تريد استبداله؟\r\n\r\nتبقى القيم القديمة في سجل الإدخال." },
                { Saved, "تم حفظ الرمز.\r\n\r\nيعرض KeePass الرمز بـ Ctrl+Shift+T وينسخه بـ Ctrl+T. الكتابة التلقائية: {TIMEOTP}" },
                { SelectOneEntry, "اختر إدخالاً واحداً فقط من فضلك." },
                { ScanScreen, "مسح QR من الشاشة" },
                { FromClipboard, "من الحافظة" },
                { FromFile, "فتح صورة..." },
                { ScanHint, "اضغط على رمز QR  ·  Esc للإلغاء" },
                { QrNotFound, "لم يُعثر على رمز QR.\r\n\r\nنصيحة: كبّر العرض ليصبح الرمز أكبر، أو انسخه كصورة (Win+Shift+S) واستخدم \"من الحافظة\"." },
                { QrNotOtp, "رمز QR هذا ليس رابط otpauth:// لرموز المرة الواحدة." },
                { QrMigration, "هذا تصدير من Google Authenticator فيه عدة حسابات. استيراده سيأتي في إصدار لاحق." },
                { NoImage, "الحافظة لا تحتوي على صورة ولا على رابط otpauth://." },
                { ImportCaption, "1. احصل على رمز QR" },
                { SettingsCaption, "الإعدادات (فقط عند لصق المفتاح السري)" },
                { BannerSubtitle, "رمز لمرة واحدة لـ: {0}" },
                { MenuRequireHello, "طلب Windows Hello قبل النسخ (Miftawerk Unlock)" },
                { MenuRequireHelloMissing, "طلب Windows Hello قبل النسخ (يتطلب Miftawerk Unlock)" },
                { RequireHelloNote, "ينطبق على RamzX OTP. أوامر KeePass الخاصة (Ctrl+T، {TIMEOTP}) لا تتأثر." },
                { MenuCopyProtected, "نسخ الرمز (بعد Windows Hello)" },
                { HelloCopyReason, "RamzX OTP: نسخ الرمز لـ \"{0}\"" },
                { HelloEnableReason, "RamzX OTP: تفعيل حماية Windows Hello لرموز المرة الواحدة" },
                { HelloNotEnabled, "لم يتم تأكيد Windows Hello. تبقى الحماية مطفأة." },
                { HelloDenied, "لم يتم تأكيد Windows Hello. لم يُنسخ الرمز." },
                { UnlockMissing, "هذه الحماية تتطلب إضافة Miftawerk Unlock، وهي غير محمّلة. لم يُنسخ الرمز.\r\n\r\nيمكنك إطفاء الحماية من قائمة RamzX OTP." },
                { Copy, "نسخ" },                { Copied, "تم النسخ ✓" },
                { Save, "حفظ" },
                { Cancel, "إلغاء" },
            };
        }
    }
}
