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
        internal const string ImageTooLarge = "ImageTooLarge";
        internal const string TextTooLong = "TextTooLong";
        internal const string HelloDisableReason = "HelloDisableReason";
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
        internal const string MenuCopyTop = "MenuCopyTop";
        internal const string MenuCopyTopProtected = "MenuCopyTopProtected";
        internal const string StatusOverride = "StatusOverride";
        internal const string StatusNotNativeOk = "StatusNotNativeOk";
        internal const string QrWarning = "QrWarning";
        internal const string Close = "Close";
        internal const string ShowQrSubtitle = "ShowQrSubtitle";
        internal const string ImportHowTo = "ImportHowTo";
        internal const string ColService = "ColService";
        internal const string ColAccount = "ColAccount";
        internal const string ColType = "ColType";
        internal const string ColStatus = "ColStatus";
        internal const string ImportStart = "ImportStart";
        internal const string ImportTitle = "ImportTitle";
        internal const string ImportSubtitle = "ImportSubtitle";
        internal const string ImportSameQr = "ImportSameQr";
        internal const string ImportMoreQr = "ImportMoreQr";
        internal const string ImportReady = "ImportReady";
        internal const string ImportButton = "ImportButton";
        internal const string ImportUnsupported = "ImportUnsupported";
        internal const string ImportExists = "ImportExists";
        internal const string ImportNew = "ImportNew";
        internal const string MenuShowQr = "MenuShowQr";
        internal const string MenuImport = "MenuImport";
        internal const string MenuMigrate = "MenuMigrate";
        internal const string OpenDatabaseFirst = "OpenDatabaseFirst";
        internal const string ImportDone = "ImportDone";
        internal const string MigrateNothing = "MigrateNothing";
        internal const string MigrateAsk = "MigrateAsk";
        internal const string MigrateDone = "MigrateDone";
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
                { QrMigration, "This is a Google Authenticator export with several accounts. Use \"RamzX OTP → Import from Google Authenticator...\" for it." },
                { NoImage, "The clipboard contains neither an image nor an otpauth:// link." },
                { ImageTooLarge, "The image is too large. Please use a smaller image or a screenshot of the QR code." },
                { TextTooLong, "The text is too long to be a QR code link." },
                { HelloDisableReason, "Turn off Windows Hello before copying OTP codes" },
                { ImportCaption, "1. Get the QR code" },
                { SettingsCaption, "Settings" },
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
                { MenuCopyTop, "Copy OTP code   {0}" },
                { MenuCopyTopProtected, "Copy OTP code (after Windows Hello)" },
                { StatusOverride, "Changed by you: {0}. The QR code / link says: {1}. The service must use the same values, otherwise the codes will not match." },
                { StatusNotNativeOk, "Recognized: {0}. Will be saved as an otpauth:// link in the field \"otp\": RamzX OTP and KeePassXC show it, KeePass's own Ctrl+T does not." },
                { QrWarning, "Anyone who sees or photographs this QR code can create your codes. Only show it when you are alone." },
                { Close, "Close" },
                { ShowQrSubtitle, "Scan with your phone's authenticator app: {0}" },
                { ImportHowTo, "In Google Authenticator on your phone: menu → \"Transfer accounts\" → \"Export accounts\". Then scan the QR code(s) shown on the phone, e.g. from a photo or a screenshot." },
                { ColService, "Service" },
                { ColAccount, "Account" },
                { ColType, "Type" },
                { ColStatus, "Status" },
                { ImportStart, "Scan the first export QR code." },
                { ImportTitle, "Import from Google Authenticator" },
                { ImportSubtitle, "New entries are created in the group \"{0}\"" },
                { ImportSameQr, "This QR code was already scanned." },
                { ImportMoreQr, "QR code {0} of {1} read. Please scan the next one." },
                { ImportReady, "{0} account(s) found. Choose which ones to import." },
                { ImportButton, "Import ({0})" },
                { ImportUnsupported, "not supported" },
                { ImportExists, "already in KeePass" },
                { ImportNew, "new" },
                { MenuShowQr, "Show as QR code (for the phone)..." },
                { MenuImport, "Import from Google Authenticator..." },
                { MenuMigrate, "Convert old OTP formats (KeeOtp, KeeTrayTOTP, KeePassOTP, KeePassXC)..." },
                { OpenDatabaseFirst, "Please open a database first." },
                { ImportDone, "{0} entry/entries created in \"{1}\". Please save the database (Ctrl+S)." },
                { MigrateNothing, "No entries with old OTP formats were found." },
                { MigrateAsk, "{0} entry/entries use an old OTP format. Convert them to KeePass's own OTP fields?\r\n\r\nThe old fields stay in each entry's history." },
                { MigrateDone, "{0} entry/entries converted. Please save the database (Ctrl+S)." },
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
                { QrMigration, "Das ist ein Google-Authenticator-Export mit mehreren Konten. Nutzen Sie dafür \"RamzX OTP → Aus Google Authenticator importieren...\"." },
                { NoImage, "Die Zwischenablage enthält weder ein Bild noch einen otpauth://-Link." },
                { ImageTooLarge, "Das Bild ist zu groß. Bitte ein kleineres Bild oder einen Screenshot des QR-Codes verwenden." },
                { TextTooLong, "Der Text ist zu lang für einen QR-Code-Link." },
                { HelloDisableReason, "Windows Hello vor dem Kopieren von OTP-Codes ausschalten" },
                { ImportCaption, "1. QR-Code holen" },
                { SettingsCaption, "Einstellungen" },
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
                { MenuCopyTop, "OTP-Code kopieren   {0}" },
                { MenuCopyTopProtected, "OTP-Code kopieren (nach Windows Hello)" },
                { StatusOverride, "Von Ihnen geändert: {0}. Der QR-Code / Link sagt: {1}. Der Dienst muss dieselben Werte verwenden, sonst passen die Codes nicht." },
                { StatusNotNativeOk, "Erkannt: {0}. Wird als otpauth://-Link im Feld \"otp\" gespeichert: RamzX OTP und KeePassXC zeigen ihn, KeePass' eigenes Strg+T nicht." },
                { QrWarning, "Wer diesen QR-Code sieht oder fotografiert, kann Ihre Codes erzeugen. Zeigen Sie ihn nur, wenn Sie allein sind." },
                { Close, "Schließen" },
                { ShowQrSubtitle, "Mit der Authenticator-App des Handys scannen: {0}" },
                { ImportHowTo, "In Google Authenticator auf dem Handy: Menü → \"Konten übertragen\" → \"Konten exportieren\". Dann den/die angezeigten QR-Code(s) scannen, z. B. von einem Foto oder Screenshot." },
                { ColService, "Dienst" },
                { ColAccount, "Konto" },
                { ColType, "Typ" },
                { ColStatus, "Status" },
                { ImportStart, "Scannen Sie den ersten Export-QR-Code." },
                { ImportTitle, "Aus Google Authenticator importieren" },
                { ImportSubtitle, "Neue Einträge werden in der Gruppe \"{0}\" angelegt" },
                { ImportSameQr, "Dieser QR-Code wurde bereits gescannt." },
                { ImportMoreQr, "QR-Code {0} von {1} gelesen. Bitte den nächsten scannen." },
                { ImportReady, "{0} Konto/Konten gefunden. Wählen Sie, welche importiert werden." },
                { ImportButton, "Importieren ({0})" },
                { ImportUnsupported, "nicht unterstützt" },
                { ImportExists, "schon in KeePass" },
                { ImportNew, "neu" },
                { MenuShowQr, "Als QR-Code anzeigen (für das Handy)..." },
                { MenuImport, "Aus Google Authenticator importieren..." },
                { MenuMigrate, "Alte OTP-Formate umwandeln (KeeOtp, KeeTrayTOTP, KeePassOTP, KeePassXC)..." },
                { OpenDatabaseFirst, "Bitte zuerst eine Datenbank öffnen." },
                { ImportDone, "{0} Eintrag/Einträge in \"{1}\" angelegt. Bitte die Datenbank speichern (Strg+S)." },
                { MigrateNothing, "Keine Einträge mit alten OTP-Formaten gefunden." },
                { MigrateAsk, "{0} Eintrag/Einträge verwenden ein altes OTP-Format. In KeePass' eigene OTP-Felder umwandeln?\r\n\r\nDie alten Felder bleiben in der Historie jedes Eintrags." },
                { MigrateDone, "{0} Eintrag/Einträge umgewandelt. Bitte die Datenbank speichern (Strg+S)." },
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
                { QrMigration, "هذا تصدير من Google Authenticator فيه عدة حسابات. استخدم لذلك \"RamzX OTP ← استيراد من Google Authenticator...\"." },
                { NoImage, "الحافظة لا تحتوي على صورة ولا على رابط otpauth://." },
                { ImageTooLarge, "الصورة كبيرة جداً. استخدم صورة أصغر أو لقطة شاشة لرمز QR." },
                { TextTooLong, "النص أطول من أن يكون رابط رمز QR." },
                { HelloDisableReason, "إيقاف طلب Windows Hello قبل نسخ رموز OTP" },
                { ImportCaption, "1. احصل على رمز QR" },
                { SettingsCaption, "الإعدادات" },
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
                { MenuCopyTop, "نسخ رمز OTP   {0}" },
                { MenuCopyTopProtected, "نسخ رمز OTP (بعد Windows Hello)" },
                { StatusOverride, "غيّرته أنت: {0}. رمز QR / الرابط يقول: {1}. يجب أن تستخدم الخدمة نفس القيم، وإلا لن تتطابق الرموز." },
                { StatusNotNativeOk, "تم التعرّف: {0}. سيُحفظ كرابط otpauth:// في الحقل \"otp\": يعرضه RamzX OTP وKeePassXC، أما Ctrl+T في KeePass فلا." },
                { QrWarning, "كل من يرى رمز QR هذا أو يصوّره يستطيع توليد رموزك. اعرضه فقط وأنت وحدك." },
                { Close, "إغلاق" },
                { ShowQrSubtitle, "امسحه بتطبيق المصادقة في هاتفك: {0}" },
                { ImportHowTo, "في Google Authenticator على الهاتف: القائمة ← \"نقل الحسابات\" ← \"تصدير الحسابات\". ثم امسح رمز (رموز) QR الظاهرة، مثلاً من صورة أو لقطة شاشة." },
                { ColService, "الخدمة" },
                { ColAccount, "الحساب" },
                { ColType, "النوع" },
                { ColStatus, "الحالة" },
                { ImportStart, "امسح أول رمز QR للتصدير." },
                { ImportTitle, "استيراد من Google Authenticator" },
                { ImportSubtitle, "تُنشأ الإدخالات الجديدة في المجموعة \"{0}\"" },
                { ImportSameQr, "تم مسح رمز QR هذا من قبل." },
                { ImportMoreQr, "تمت قراءة رمز QR {0} من {1}. امسح التالي من فضلك." },
                { ImportReady, "تم العثور على {0} حساب. اختر ما تريد استيراده." },
                { ImportButton, "استيراد ({0})" },
                { ImportUnsupported, "غير مدعوم" },
                { ImportExists, "موجود في KeePass" },
                { ImportNew, "جديد" },
                { MenuShowQr, "عرض كرمز QR (للهاتف)..." },
                { MenuImport, "استيراد من Google Authenticator..." },
                { MenuMigrate, "تحويل صيغ OTP القديمة (KeeOtp وKeeTrayTOTP وKeePassOTP وKeePassXC)..." },
                { OpenDatabaseFirst, "افتح قاعدة بيانات أولاً من فضلك." },
                { ImportDone, "تم إنشاء {0} إدخال في \"{1}\". احفظ قاعدة البيانات من فضلك (Ctrl+S)." },
                { MigrateNothing, "لم يُعثر على إدخالات بصيغ OTP قديمة." },
                { MigrateAsk, "{0} إدخال يستخدم صيغة OTP قديمة. تحويلها إلى حقول OTP الخاصة بـ KeePass؟\r\n\r\nتبقى الحقول القديمة في سجل كل إدخال." },
                { MigrateDone, "تم تحويل {0} إدخال. احفظ قاعدة البيانات من فضلك (Ctrl+S)." },
                { Copy, "نسخ" },                { Copied, "تم النسخ ✓" },
                { Save, "حفظ" },
                { Cancel, "إلغاء" },
            };
        }
    }
}
