<div align="center">

# RamzX OTP

### Einmalpasswörter (TOTP / HOTP / Steam Guard) für KeePass 2 – QR-Code direkt vom Bildschirm scannen

[English](../README.md) · **Deutsch** · [العربية](README.ar.md)

Kostenlos und Open Source · Made in Germany von **[RamzX](https://ramzx.de)**

</div>

---

**RamzX OTP** ist ein kostenloses Open-Source-Plugin für **KeePass 2**, das Ihren Einträgen
**Zwei-Faktor-Codes** (TOTP und HOTP, wie bei Google Authenticator, Microsoft Authenticator und den
meisten Websites) hinzufügt. Klicken Sie auf den QR-Code, den eine Website anzeigt – das Plugin liest
ihn direkt vom Bildschirm, ohne Handy und ohne Abtippen.

> **Status: Alpha.** Bitte behalten Sie beim Testen eine Sicherung Ihrer Datenbank.

## Funktionen

- **QR-Code vom Bildschirm scannen** (jeder Monitor), aus der Zwischenablage, aus einer Bilddatei oder als eingefügter `otpauth://`-Link
- **Import aus Google Authenticator** („Konten übertragen“-Export, auch mehrere QR-Codes)
- **Alte Plugin-Formate umwandeln:** KeeOtp, KeeTrayTOTP, KeePassOTP, KeePassXC → KeePass' eigene OTP-Felder (alte Werte bleiben in der Historie)
- **Live-Spalte** in der Eintragsliste mit Code und Restsekunden; Klick kopiert
- **„OTP-Code kopieren 123 456 (20 s)“** direkt im Kontextmenü; KeePass leert die Zwischenablage automatisch
- **Als QR-Code anzeigen**, um ein Konto aufs Handy zu übertragen
- **Steam Guard** (5 Zeichen)
- **Windows Hello vor dem Kopieren** (optional, mit [RamzX Unlock](https://github.com/RamzXHQ/RamzX-Unlock))
- **Kein Lock-in:** Speicherung in KeePass' eigenen OTP-Feldern (`TimeOtp-*` / `HmacOtp-*`) – Strg+T und `{TIMEOTP}` funktionieren auch ohne Plugin
- **Englisch, Deutsch, Arabisch** · **kein Netzwerk, keine Telemetrie**, 100 % eigener Code

## Installation

1. KeePass schließen.
2. `RamzXOtp.dll` unter [Releases](https://github.com/RamzXHQ/RamzX-OTP/releases) herunterladen und SHA-256 mit `SHA256SUMS.txt` prüfen.
3. In den Ordner `Plugins` von KeePass 2.60+ kopieren und KeePass starten.

> Windows 11 *Smart App Control* blockiert nicht signierte DLLs. Unsere Releases sind noch nicht signiert.

## Sicherheit – ehrlich

- **Gleiche Datenbank = kein echter zweiter Faktor.** Wer Datenbank und Master-Passwort hat, hat beides.
- **Windows Hello** akzeptiert immer auch die PIN – Komfortschutz, kein zweiter Faktor.
- Sicherheitslücken bitte vertraulich an **support@ramzx.de**.

## Datenschutz (DSGVO)

Kein Netzwerkzugriff, keine Konten, keine Telemetrie. Bildschirmpixel werden erst nach Ihrem Klick
gelesen, nur im Arbeitsspeicher gehalten und nie gespeichert.

MIT-Lizenz · © 2026 RamzX · Fragen: **support@ramzx.de**
KeePass ist ein Projekt von Dominik Reichl; RamzX OTP ist ein unabhängiges Plugin.
