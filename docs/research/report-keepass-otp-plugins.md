> Note: written before the project was renamed. "Miftawerk OTP" in this report = **RamzX OTP**.

# Build Miftawerk OTP around KeePass's native fields

*Research report for SeeColors iT, 2026-10-23. Sources: four research-note files (existing plugins, storage formats, QR-scan feasibility, security/UX) compiled the same day. All effort figures are estimates, not measurements.*

Miftawerk OTP should not compete with KeePass as a second OTP generator. It should be a **signed, dependency-free .NET 4.8 DLL that writes only KeePass's own `TimeOtp-*` / `HmacOtp-*` fields** and adds the things KeePass 2.61.1 still lacks: QR import (point-and-click on screen, clipboard image, image file), forgiving `otpauth://` paste, bulk Google Authenticator migration import, and one-click clean-up of the four legacy plugin formats that cause wrong codes and crashes today. KeePass has generated TOTP natively since 2.47, has shipped an OTP settings dialog plus Copy/Show commands since 2.51 and Ctrl+T since 2.53, yet **no KeePass changelog from 2.08 to 2.61 mentions QR or `otpauth` import** ([KeePass news index](https://keepass.info/news/news_all.html)). That gap is the product. The three GitHub OTP plugins (KeeOtp2, KeeTrayTOTP, KeePassOTP) **ship only as PLGX** ([KeeOtp2 releases](https://github.com/tiuub/KeeOtp2/releases), [KeeTrayTOTP releases](https://github.com/KeeTrayTOTP/KeeTrayTOTP/releases), [KeePassOTP releases](https://github.com/Rookiestyle/KeePassOTP/releases)). Two of them have largely stalled: KeeOtp2 has had no maintainer reply since its 2025-03 release, and KeeTrayTOTP's maintainer announced minor-fixes-only mode in 2023. The best-maintained one, KeePassOTP, is GPL-3.0 and downloads a 2fa.directory list at startup unless the user turns that off. Building on native fields also guarantees the property users most need from a small open-source project: if Miftawerk OTP is uninstalled or abandoned, every entry keeps working in KeePass and, for Base32 TOTP, in KeePassXC 2.7.10+. The MVP is estimated at **26–41 developer-days**. The riskiest parts are mixed-DPI screen capture and a from-scratch QR decoder, and both can be retired early with small spikes.

## Four plugins, one recurring list of complaints

keepass.info lists four OTP-generating plugins: KeePassOTP, KeeOtp2, KeeTrayTOTP and TwoFactorQRCodeReader. The original KeeOtp and Tray TOTP are marked discontinued, and OtpKeyProv is only a key provider for opening the database ([keepass.info plugins](https://keepass.info/plugins.html)).

**KeeOtp2** (MIT, 164 stars) is a front-end for KeePass's `TimeOtp-*` fields. It offers:

- screen QR scanning, QR display and `otpauth://` sharing;
- a global auto-type hotkey (Ctrl+Alt+T);
- NTP or fixed-offset time;
- Steam.

It bundles Otp.NET, ZXing.Net, NHotkey and Yort.Ntp ([README](https://github.com/tiuub/KeeOtp2)). The README does not advertise screen scanning, but bug reports [#37](https://github.com/tiuub/KeeOtp2/issues/37) and [#40](https://github.com/tiuub/KeeOtp2/issues/40) show that it exists. Releases came in bursts: v1.5.9 in 2022, then **v1.6.0 on 2025-03-21**. Issues opened since then, including a frequent crash on Copy TOTP ([#66](https://github.com/tiuub/KeeOtp2/issues/66), 2026-05), have had no reply.

**KeeTrayTOTP** (GPL-3.0, 510 stars) stores secrets in the non-standard fields `TOTP Seed` and `TOTP Settings`. Its maintainer wrote in 2023 that he no longer uses KeePass and will do only minor fixes ([#325](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/325)).

**KeePassOTP** (GPL-3.0, 519 stars, v1.12 on 2026-02-10) is the most complete and most actively maintained plugin ([GitHub](https://github.com/Rookiestyle/KeePassOTP)). It supports:

- TOTP, HOTP, Steam and Yandex;
- QR import by drag-and-drop or screen capture;
- `otpauth://` paste and QR export;
- a tray menu and a `{KPOTP}` placeholder;
- storage either in the entry or in a separate embedded OTP database.

It also downloads the 2fa.directory list at startup, which can be switched off ([README](https://github.com/Rookiestyle/KeePassOTP)).

**TwoFactorQRCodeReader** is the closest conceptual match to the proposed design. It reads a QR code from the screen or clipboard, fills the built-in fields and leaves generation to KeePass ([SF announcement](https://sourceforge.net/p/keepass/discussion/329220/thread/041ecdb8cb/)). Users still recommended it in 2025 ([SF 2025-06](https://sourceforge.net/p/keepass/discussion/329221/thread/763ff4fd2e/)). Its license and binary format could not be confirmed. It checks for updates over the network through SourceForgeUpdateChecker.

The complaints below come from full issue-tracker pulls of the three GitHub plugins (KeeOtp2 55 items, KeeTrayTOTP 384, KeePassOTP 192) and a title-based classification of 208 KeePassXC TOTP issues. KeeOtp2 counts are a complete classification. KeeTrayTOTP and KeePassOTP counts are the issues the researchers cited per theme, so they are lower bounds. KeePassXC counts are approximate.

| Complaint theme | KeeOtp2 | KeeTrayTOTP | KeePassOTP | KeePassXC (approx.) | Representative evidence |
|---|---|---|---|---|---|
| Wrong codes (mostly clock or parameters) | 2 | 8 | 5 | ~37 + ~9 time/TZ | [KeeOtp2 #10](https://github.com/tiuub/KeeOtp2/issues/10) (87 s off, clock locked by policy); [KeeTrayTOTP #311](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/311); [KeePassXC #8300](https://github.com/keepassxreboot/keepassxc/issues/8300) ("99.9%" clock) |
| Secret parsing (spaces, case, padding, `%3D`) | 3 | 2 | – | ~15 | [KeeOtp2 #61](https://github.com/tiuub/KeeOtp2/pull/61), [#66](https://github.com/tiuub/KeeOtp2/issues/66); [KeePassXC #3754](https://github.com/keepassxreboot/keepassxc/issues/3754) |
| Format interop and migration between plugins | ~11 | 2 | 4 | ~13 | [KeeOtp2 #16](https://github.com/tiuub/KeeOtp2/issues/16), [#41](https://github.com/tiuub/KeeOtp2/issues/41); [KeeTrayTOTP #110](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/110); [KeePassOTP #184](https://github.com/Rookiestyle/KeePassOTP/issues/184) |
| Google Authenticator export import | 1 (open) | – | 4 (bulk = wontfix) | – | [KeeOtp2 #53](https://github.com/tiuub/KeeOtp2/issues/53); [KeePassOTP #108](https://github.com/Rookiestyle/KeePassOTP/issues/108), [#179](https://github.com/Rookiestyle/KeePassOTP/issues/179) |
| QR scanning / import reliability | 3 | 2 | 3 | ~13 | [KeeOtp2 #37](https://github.com/tiuub/KeeOtp2/issues/37) (last monitor only), [#40](https://github.com/tiuub/KeeOtp2/issues/40) (~40% success); [KeePassXC #4825](https://github.com/keepassxreboot/keepassxc/issues/4825) (52 comments) |
| Hotkey conflicts | 8 | 1 | 5 | ~13 auto-type | [KeeOtp2 #42](https://github.com/tiuub/KeeOtp2/issues/42) (taken hotkey → "incompatible"); [KeePassOTP #15](https://github.com/Rookiestyle/KeePassOTP/issues/15) (global Ctrl+T) |
| Load/compile breakage and crashes | 8 | 3 | 7 | – | [KeeOtp2 #13](https://github.com/tiuub/KeeOtp2/issues/13), [#56](https://github.com/tiuub/KeeOtp2/issues/56); [KeePassOTP #79](https://github.com/Rookiestyle/KeePassOTP/issues/79) (missing Newtonsoft.Json), [#81](https://github.com/Rookiestyle/KeePassOTP/issues/81) (crash without network) |
| Parameters: HOTP, SHA-256/512, 7/8 digits, periods, Steam etc. | 6 | ≥6 | 7 | ~6 + ~9 | [KeeOtp2 #46](https://github.com/tiuub/KeeOtp2/issues/46) (HOTP, open); [KeeTrayTOTP #207](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/207) |
| UI, display, countdown, theming | 6 | 6 | 5 | ~26 | [KeeOtp2 #32](https://github.com/tiuub/KeeOtp2/issues/32) (dark theme); [KeeTrayTOTP #328](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/328); [KeePassOTP #186](https://github.com/Rookiestyle/KeePassOTP/issues/186) (show only selected row, wontfix) |
| Needless "database modified" / separate-DB password prompts | 1 | – | 9 | – | [KeePassOTP #188](https://github.com/Rookiestyle/KeePassOTP/issues/188), [#130](https://github.com/Rookiestyle/KeePassOTP/issues/130); [SF 2024](https://sourceforge.net/p/keepass/discussion/329221/thread/d67d6aabf2/) |
| Network and privacy | – | 1 (TLS time sync) | 3 | – | [KeePassOTP #55](https://github.com/Rookiestyle/KeePassOTP/issues/55), [#134](https://github.com/Rookiestyle/KeePassOTP/issues/134) (DNS lookup per URL), [#187](https://github.com/Rookiestyle/KeePassOTP/issues/187) |
| Performance | 1 | 3 | – | – | [KeeOtp2 #45](https://github.com/tiuub/KeeOtp2/issues/45) (100% CPU); [KeeTrayTOTP #27](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/27) |
| Lock/session (OTP window survives lock) | – | – | – | ~9 | [KeePassXC #923](https://github.com/keepassxreboot/keepassxc/issues/923), [#4436](https://github.com/keepassxreboot/keepassxc/issues/4436) |
| Entry edits lost while configuring OTP | – | – | – | ~10 | [KeePassXC #10217](https://github.com/keepassxreboot/keepassxc/issues/10217), [#3142](https://github.com/keepassxreboot/keepassxc/issues/3142) |
| Packaging / integrity | 3 | 1 (hash file) | 1 (winget) | – | [KeeOtp2 #63](https://github.com/tiuub/KeeOtp2/issues/63); [KeeTrayTOTP #187](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/187) |

Three patterns stand out in this table. First, most "wrong code" reports are not algorithm bugs. They come from the system clock, from strict parsing of secrets with spaces or padding, and from entries that contain two competing formats. A 2025 Keepass2Android user got different codes on each device for exactly that reason ([SF 2025-06](https://sourceforge.net/p/keepass/discussion/329221/thread/763ff4fd2e/)). Second, every plugin that registers a global hotkey collects hotkey complaints. Third, crashes cluster around bundled third-party assemblies (`MissingMethodException` in ZXing in KeeOtp2 #56, a missing Newtonsoft.Json in KeePassOTP #79), which supports a no-dependency build.

On delivery, the notes found no OTP plugin that ships a DLL or an Authenticode signature. KeeTrayTOTP's rolling pre-release adds only a `hashes.txt`. PLGX plugins are compiled into `%LOCALAPPDATA%\KeePass\PluginCache` on the user's machine ([KeeOtp2 #42](https://github.com/tiuub/KeeOtp2/issues/42)), so their authors cannot sign them. A 2026 report of Windows 11 Smart App Control blocking a PLGX plugin (KeePassRPC) had disabling SAC as its only workaround. The KeePassRPC developer could not say whether the missing signature was the cause ([Kee forum](https://forum.kee.pm/t/keepassrpc-blocked-by-windows-11-smart-app-control-any-solution/5094)). This matches SeeColors iT's own observation that KeeOtp2 and ColoredPassword are blocked on the target PC, which is project context and not a published source.

## KeePass's own TimeOtp fields are the only lock-in-free primary format

KeePass documents its native OTP storage as plain entry strings ([Placeholders help](https://keepass.info/help/base/placeholders.html)).

TOTP uses:

- exactly one of `TimeOtp-Secret`, `-Hex`, `-Base32` or `-Base64`;
- `TimeOtp-Length` (default 6, **maximum 8**);
- `TimeOtp-Period` (default 30);
- `TimeOtp-Algorithm` (`HMAC-SHA-1`, `HMAC-SHA-256` or `HMAC-SHA-512`).

HOTP uses one `HmacOtp-Secret*` field plus `HmacOtp-Counter`, which KeePass increments every time `{HMACOTP}` is evaluated. There are no HOTP length or algorithm fields. Since 2.51 KeePass edits these fields in its own dialog and offers Copy/Show OTP ([2.51 notes](https://keepass.info/news/n220506_2.51.html)). Since 2.53, Ctrl+T copies the TOTP ([2.53 notes](https://keepass.info/news/n230109_2.53.html)). 2.61 strips whitespace when a secret is pasted and adds copy buttons ([2.61 notes](https://keepass.info/news/n260304_2.61.html)).

KeePassXC changes the calculation. It stores TOTP in an `otp` attribute holding an `otpauth://` URI and still reads the legacy `TOTP Seed` + `TOTP Settings` pair and KeeOtp1's `key=…&size=…` format. Since **2.7.10 (2025-03-02)** it also reads KeePass's `TimeOtp-Secret-Base32` plus Length/Period/Algorithm. It reads only the Base32 variant, never HOTP, and only when no `otp` or legacy field is present ([Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp), [CHANGELOG](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md)). Since 2.7.12 it also understands `{TIMEOTP}` in auto-type. KeePassXC never writes or deletes `TimeOtp-*` fields, so an entry edited in both tools can end up holding two disagreeing secrets.

The design rules follow from these facts. On write, the plugin should always put the secret in `TimeOtp-Secret-Base32`, normalized to uppercase and unpadded, and write the parameter fields with the exact `HMAC-SHA-*` spellings. That entry then works in KeePass 2.51+ without the plugin and in KeePassXC 2.7.10+. HOTP goes to `HmacOtp-Secret-Base32` + `HmacOtp-Counter`. KeePass does the generation, so the counter has only one owner.

Steam cannot be expressed in native fields, and writing `TimeOtp-*` for a Steam secret would make KeePass produce a wrong 6-digit decimal code. Steam therefore goes only into `otp` as `otpauth://totp/…&encoder=steam`, the form KeePassXC and KeeOtp2 already use. Steam is HMAC-SHA1 with a 30 s period, reduced to 5 symbols from the alphabet `23456789BCDFGHJKMNPQRTVWXY` with the least significant symbol first ([Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp), [Aegis vault.md](https://github.com/beemdevelopment/Aegis/blob/master/docs/vault.md)).

Miftawerk OTP must not register `{TIMEOTP}` or `{HMACOTP}`, because KeePass owns them. A distinct placeholder is justified only for Steam.

On read, the plugin should accept every format it may meet. When formats disagree it should show a warning and let the user choose instead of picking one silently, because silent precedence is how the K2A divergence and KeeOtp2's "`otp` already exists" crash ([#41](https://github.com/tiuub/KeeOtp2/issues/41)) happened.

| Format | KeePass 2.61.1 | KeePassXC ≥ 2.7.10 | Miftawerk OTP |
|---|---|---|---|
| `TimeOtp-Secret-Base32` + Length/Period/Algorithm | read/write | read only | **read/write (primary TOTP)** |
| `TimeOtp-Secret` / `-Hex` / `-Base64` | read/write | — | read; offer conversion to Base32 |
| `HmacOtp-Secret*` + `HmacOtp-Counter` | read/write | — | **read/write (primary HOTP)** |
| `otp` = `otpauth://totp/…` | — | read/write (default) | read; optional mirror (off by default, later) |
| `otp` = `otpauth://…&encoder=steam` | — | read/write | read/write (Steam only, later) |
| `otp` = `otpauth://hotp/…` | — | ignored | read → convert to `HmacOtp-*` |
| `otp` = KeeOtp1 `key=…&size=…&step=…` | — | read/write | read → migrate |
| `TOTP Seed` + `TOTP Settings` (`30;6`, `30;S`) | — | read/write | read → migrate |
| Google `otpauth-migration://offline?data=` | — | — | import (multi-batch) |
| Aegis plain JSON (v3 content) | — | not verified | import (later) |

Sources: [Placeholders help](https://keepass.info/help/base/placeholders.html), [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp), [Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp), [KeeOtp2 OtpAuthUtils.cs](https://github.com/tiuub/KeeOtp2/blob/master/KeeOtp2/OtpAuthUtils.cs).

The `otpauth://` format itself is defined only by an archived Google wiki page (repo archived 2021, page last edited 2018). The required parameter is `secret` (Base32 without padding). The optional ones are `issuer`, `algorithm`, `digits` (6/8), `period`, and `counter` for HOTP. Issuer and account are separated by `:` or `%3A`, and neither may contain a colon ([Key Uri Format](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)). Implementations differ in the details:

- Bitwarden accepts lowercase `sha256` and 1–10 digits ([Bitwarden](https://bitwarden.com/help/integrated-authenticator/)).
- KeePassXC clamps digits to 1–10 and accepts `HMAC-SHA-256`.
- KeePass caps native length at 8.

The parser should therefore be liberal (any case, both algorithm spellings, percent-decoded labels, `issuer` preferred over the label prefix). It should reject more than 8 digits only when the target is the native fields.

Google Authenticator's export is `otpauth-migration://offline?data=<Base64 protobuf>`. The schema is community reverse-engineered. Its fields are repeated OTP parameters (raw secret bytes, name, issuer, algorithm enum, digit enum 1→6 / 2→8, type, counter) plus `batch_size`/`batch_index`/`batch_id` for multi-QR exports, and **there is no period field**, so the period is always 30 s ([dim13 migration.proto](https://github.com/dim13/otpauth/blob/master/migration/migration.proto)). The schema is small enough for a hand-written 150–250-line varint reader with no protobuf dependency.

Code from KeePassXC (GPL), Aegis (GPL), scito/extract_otp_secrets (GPL), KeePassOTP and KeeTrayTOTP (GPL-3.0) may be studied for behaviour only. Field names and constants are interface facts, but no code may be copied into an MIT project.

## Scanning under the cursor is feasible with GDI, a DPI switch and a screen-only decoder

Capture is the easy half. `Graphics.CopyFromScreen` is a BitBlt available in .NET Framework 4.8 with no dependency ([Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen?view=netframework-4.8)). Unlike Windows.Graphics.Capture, it needs no consent picker and draws no yellow border ([Screen capture docs](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)). Windows.Graphics.Capture would also add WinRT and D3D11 interop for no benefit in a single-snapshot use case.

The real hazard is DPI. A local measurement on the researcher's machine showed KeePass 2.60 running as **system-DPI-aware**. That is not a published source and must be re-checked on 2.61.1. Microsoft warns that system-aware threads receive virtualized coordinates on monitors with a different scale factor, and that which APIs are affected "is not currently sufficiently documented" ([High-DPI guide](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)). The remedy is to switch only the capturing thread to Per-Monitor-V2 with `SetThreadDpiAwarenessContext` (Windows 10 1703+), read the cursor position and capture in physical pixels, then restore the previous context as Microsoft instructs.

The UX evidence argues against a scanner that runs blind. KeeOtp2 scanned only the last of several monitors ([#37](https://github.com/tiuub/KeeOtp2/issues/37)). It succeeded about 40% of the time because its own message box covered the QR code, until a delay hid the window before the screenshot ([#40](https://github.com/tiuub/KeeOtp2/issues/40)). 1Password's screen scanner "sometimes is unable to find the edge of the QR code", and its staff tell users to snip with Win+Shift+S and scan from the clipboard instead ([1Password community](https://1password.community/discussion/comment/516633)). Devolutions shows a minimize-and-nothing-happens failure ([Devolutions forum](https://forum.devolutions.net/topics/31800/one-time-password--scan-qr-code-from-my-screen-does-not-work)).

The recommended design is therefore a **pick mode rather than a global hotkey**:

1. The user clicks "Scan QR on screen" in the setup dialog.
2. All KeePass windows hide.
3. A translucent, non-frozen overlay covers every monitor with a crosshair cursor.
4. The user either clicks on the QR code, which captures a roughly 600×600 physical-pixel square around the click, or drags a rectangle, which captures exactly that area.
5. The overlay hides before capture, and capture waits for the compositor.
6. Esc cancels.

This puts the user's chosen area under the cursor, captures only that region rather than the whole desktop, and needs no system-wide hotkey. If the decoder fails, the same dialog offers "Paste image from clipboard", "Open image file" and "Paste `otpauth://` text". A true hover-plus-hotkey mode can come later as an opt-in. It must be configurable and must degrade gracefully when its key is taken, because KeeOtp2 #42 shows a taken hotkey turning into "plugin incompatible".

The decoder is the hard half. No usable built-in Windows API decodes a QR code from a still bitmap. The only options found were commercial SDKs and ZXing.Net. **ZXing.Net 0.16.11** is Apache-2.0, has a dependency-free net48 build and 54.1 M downloads ([NuGet](https://www.nuget.org/packages/ZXing.Net)), and would reduce decoder work to about a day. However, it breaks the "100% from scratch" goal. It also requires shipping Apache-2.0 notices ([Apache-2.0 §4](https://www.apache.org/licenses/LICENSE-2.0)). Finally, it means either a second binary next to the signed plugin or an ILMerge step, and bundled ZXing is exactly where KeeOtp2 #56 crashed.

An own decoder is realistic because screen QR codes are axis-aligned, sharp and unrotated. That removes perspective correction and most of the robustness work that camera decoders need. The components are:

- binarization (try both polarities, for dark-mode sites);
- finder-pattern search;
- sub-pixel module sampling, because 125% zoom gives module sizes like 3.75 px;
- format and version BCH decoding;
- unmasking and zigzag extraction;
- block de-interleaving;
- Reed–Solomon decoding over GF(256);
- numeric, alphanumeric and byte modes.

Free references cover all of it: [Thonky](https://www.thonky.com/qr-code-tutorial/) and [Nayuki](https://www.nayuki.io/page/creating-a-qr-code-step-by-step) for structure and tables, [Wikiversity](https://en.wikiversity.org/wiki/Reed%E2%80%93Solomon_codes_for_coders) for RS and BCH decoding. The research estimate is **about 1,400–1,750 lines of C# and 5–10 developer-days**, with most of the risk in detection, not in Reed–Solomon. Versions up to 40 must be supported, because Google migration exports with many accounts reach high versions.

The test corpus is the leverage point. It should contain synthetic codes rendered at 1–8 px per module and at 125/150/175% scale, with antialiasing, inverted colours and cluttered backgrounds, plus real screenshots. In the MVP these can be generated by a test-only tool that never ships. A later own QR encoder, needed anyway for QR export, can then replace that tool.

Capture is also a privacy feature, so it needs explicit discipline. The plugin should:

- capture only on an explicit user action and only the chosen region;
- keep pixels in memory, never on disk, in temp files or on the clipboard;
- clear grayscale and binary buffers and dispose bitmaps immediately (best effort under the GC);
- move the decoded secret into a protected string at once;
- show a confirmation with issuer and account and a masked secret before saving;
- make no network calls.

Decoded text is untrusted input, so strict scheme checks and length limits apply. `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` can keep the overlay out of other recorders, but Microsoft says it is not a security feature ([SetWindowDisplayAffinity](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity)).

## Correct codes depend more on the clock and parser than the HMAC

The algorithm fits on a page, and its official vectors double as acceptance tests. RFC 4226 Appendix D gives ten HOTP codes for the ASCII secret `12345678901234567890`, counters 0–9: `755224, 287082, 359152, 969429, 338314, 254676, 287922, 162583, 399871, 520489` ([RFC 4226](https://www.rfc-editor.org/rfc/rfc4226)). RFC 6238 Appendix B gives 18 eight-digit TOTP codes for SHA-1/256/512 at six timestamps, for example `94287082 / 46119246 / 90693936` at t = 59 ([RFC 6238](https://www.rfc-editor.org/rfc/rfc6238)).

There is a trap in those vectors. **Verified Erratum 2866** confirms that the SHA-256 and SHA-512 vectors reproduce only with 32-byte and 64-byte seeds, not the 20-byte secret the appendix text implies ([RFC 6238 errata](https://errata.rfc-editor.org/rfc6238)). With the 20-byte seed, the researchers computed `32247374` / `69342147` at t = 59. That is a useful negative-control test. The t = 20,000,000,000 vector checks the 64-bit counter. `07081804` checks that leading zeros survive, so codes must be strings and never integers. The engine should be written from the RFC prose, because the RFC's Java reference code is under Simplified BSD and copying it would bring that licence text along.

Time is the dominant failure. KeePassXC's maintainer attributes "99.9%" of wrong-code reports to the system clock ([#8300](https://github.com/keepassxreboot/keepassxc/issues/8300)). Seven of eight sampled wrong-code threads ended with "sync your computer time". KeePassXC still has open time-zone and DST bugs ([#10802](https://github.com/keepassxreboot/keepassxc/issues/10802), [#9975](https://github.com/keepassxreboot/keepassxc/issues/9975)), and KeeOtp2 has an unanswered UTC+1 report ([#64](https://github.com/tiuub/KeeOtp2/issues/64)). Computing T only from `DateTimeOffset.UtcNow.ToUnixTimeSeconds()` makes that bug class structurally impossible.

A clock *check* belongs in the product, but as an opt-in action. NTP requests are unauthenticated and fingerprintable ([IETF NTP data-minimization draft](https://datatracker.ietf.org/doc/html/draft-ietf-ntp-data-minimization)). KeePassOTP's HTTP-Date-based correction mis-parsed headers in 2026 ([#187](https://github.com/Rookiestyle/KeePassOTP/issues/187)). The default should be system time with zero network traffic, plus a local "codes wrong? check Windows time sync" hint.

A clock *offset* has a catch. It changes only the codes the plugin shows, while KeePass's native `{TIMEOTP}` and Ctrl+T keep using system time, because KeePass's help says "the date and the time of your system must be correct" ([Placeholders help](https://keepass.info/help/base/placeholders.html)). An offset therefore creates two different "current" codes. That is acceptable only as a clearly labelled later option for locked-down corporate PCs like the one in KeeOtp2 #10.

Input validation decides the long tail of support load. Real secrets arrive in many shapes:

- with spaces and in lowercase (Tutanota style);
- without padding (Amazon) or with too much;
- with newlines;
- percent-encoded (`%3D`);
- as a full `otpauth://` URI pasted into the secret field.

Strict decoders turned each of these into bug reports ([KeePassXC #3754](https://github.com/keepassxreboot/keepassxc/issues/3754), [#3825](https://github.com/keepassxreboot/keepassxc/issues/3825), [#9978](https://github.com/keepassxreboot/keepassxc/issues/9978); [KeeOtp2 #61](https://github.com/tiuub/KeeOtp2/pull/61)). In KeeOtp2 #66 the bad input reached Otp.NET and crashed it.

The rule set is:

- **Normalize:** trim; remove whitespace and hyphens; uppercase; strip `=` and re-pad internally; parse a URI when the input starts with `otpauth://`.
- **Reject, with a specific message and without losing the user's input:** characters outside A–Z2–7, impossible Base32 lengths, empty secrets, unsupported digits or periods, unknown algorithms.
- **Warn but allow:** secrets under RFC 4226's 128-bit minimum. Users cannot change a service's secret, and the common example `JBSWY3DPEHPK3PXP` is only 80 bits.

A live preview of the current and next code, with the seconds remaining, lets users compare against the service before saving.

KeePass's memory protection covers fields stored as protected strings, but its documentation admits that plaintext copies appear on display, clipboard transfer and placeholder replacement ([KeePass Security](https://keepass.info/help/base/security.html)). The plugin's job is not to add more copies:

- Keep the secret field protected.
- Decode to a byte array only for the HMAC call, then zero it.
- Never place secrets in exceptions or logs. Proton Authenticator leaked full TOTP secrets into its debug logs in 2025 ([BleepingComputer](https://www.bleepingcomputer.com/news/security/proton-fixes-authenticator-bug-leaking-totp-secrets-in-logs/)).
- Never copy the secret or URI to the clipboard by default.

Copies of *codes* should go through KeePass's own clipboard path. Since 2.44 that path excludes data from Windows clipboard history and cloud sync by default ([2.44 notes](https://keepass.info/news/n200120_2.44.html), [Clipboard formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats)) and applies the user's auto-clear setting. The exact KeePass helper API still needs confirming in code. Codes are copied without the display space ([KeePassXC #12039](https://github.com/keepassxreboot/keepassxc/issues/12039)). Every OTP window or timer must close on database lock and must not reset the auto-lock timer, which is a lesson from about nine KeePassXC lock/session issues ([#923](https://github.com/keepassxreboot/keepassxc/issues/923), [#4436](https://github.com/keepassxreboot/keepassxc/issues/4436)).

Auto-type needs no new mechanism. Documenting `{TIMEOTP}` gives a sequence that works in KeePass and in KeePassXC 2.7.12+, and KeePass resolves it at typing time.

The same-database trade-off deserves one honest, non-alarmist note. KeePassXC's guide warns that storing TOTP alongside the password "will eliminate the advantages of two-factor authentication" ([KeePassXC docs](https://raw.githubusercontent.com/keepassxreboot/keepassxc/develop/docs/topics/DatabaseOperations.adoc)). 1Password argues that one-time codes still defeat replay of a captured password, while conceding that "you lack a true second factor when both secrets originate from the same device" ([1Password 2023](https://1password.com/blog/1password-2fa-passwords-codes-together)).

The note should be shown once at first setup and repeated in the docs. Proposed wording: *"If this database is stolen and unlocked, the attacker gets both the password and the one-time codes for this account. Two-factor protection then only helps against attacks that don't involve this database, such as a leaked or phished password. For critical accounts (your e-mail, and the storage or sync account holding this database), consider keeping the OTP secret in a separate database with a different master key, or on another device."* The docs should also say that auto-opening that second database from the first removes the separation ([KeePassXC #4491](https://github.com/keepassxreboot/keepassxc/issues/4491)).

## Thirty-nine requirements traced to evidence

"MVP" means required for v1.0. "Later" means v1.1 or after.

| # | Requirement | Scope | Evidence |
|---|---|---|---|
| R1 | HOTP/TOTP engine written from the RFC text; passes all 10 RFC 4226 App. D and 18 RFC 6238 App. B vectors with per-algorithm seeds, plus the Erratum 2866 negative control | MVP | [RFC 4226](https://www.rfc-editor.org/rfc/rfc4226), [RFC 6238](https://www.rfc-editor.org/rfc/rfc6238), [errata](https://errata.rfc-editor.org/rfc6238) |
| R2 | 64-bit time counter; offset taken from the last HMAC byte for 20-, 32- and 64-byte digests | MVP | [RFC 6238 §4](https://www.rfc-editor.org/rfc/rfc6238) |
| R3 | Codes are zero-padded strings; 6–8 digits for native fields, 1–10 accepted when reading `otp` | MVP | [Placeholders help](https://keepass.info/help/base/placeholders.html), [Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp) |
| R4 | Time from the UTC Unix epoch only, never local time | MVP | [KeePassXC #10802](https://github.com/keepassxreboot/keepassxc/issues/10802), [KeeOtp2 #64](https://github.com/tiuub/KeeOtp2/issues/64) |
| R5 | Write TOTP to `TimeOtp-Secret-Base32` + Length/Period/Algorithm (exact `HMAC-SHA-*` strings); write HOTP to `HmacOtp-Secret-Base32` + Counter | MVP | [Placeholders help](https://keepass.info/help/base/placeholders.html), [Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp) |
| R6 | Secret fields stored as protected strings; secret bytes zeroed after use; no secrets in logs or exceptions | MVP | [KeePass Security](https://keepass.info/help/base/security.html), [Proton log leak](https://www.bleepingcomputer.com/news/security/proton-fixes-authenticator-bug-leaking-totp-secrets-in-logs/) |
| R7 | Read all formats: native (4 encodings), `otp` otpauth TOTP/HOTP, KeeOtp1 `key=`, `TOTP Seed`/`TOTP Settings` | MVP | [Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp), [KeeTrayTOTP #110](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/110) |
| R8 | Detect entries with several formats that disagree; show both codes and let the user choose | MVP | [SF 2025-06](https://sourceforge.net/p/keepass/discussion/329221/thread/763ff4fd2e/), [KeeOtp2 #16](https://github.com/tiuub/KeeOtp2/issues/16), [#41](https://github.com/tiuub/KeeOtp2/issues/41) |
| R9 | Per-entry and bulk migration of legacy formats to native fields, keeping old fields in KeePass entry history | MVP | [KeePassOTP #184](https://github.com/Rookiestyle/KeePassOTP/issues/184), [#177](https://github.com/Rookiestyle/KeePassOTP/issues/177), [KeeOtp2 PR #58](https://github.com/tiuub/KeeOtp2/pull/58) |
| R10 | Never register `{TIMEOTP}`/`{HMACOTP}`; document `{TIMEOTP}` for auto-type; leave HOTP generation and counter to KeePass | MVP | [Placeholders help](https://keepass.info/help/base/placeholders.html), [KeePassXC CHANGELOG 2.7.12](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md) |
| R11 | Forgiving secret normalization; specific rejections that keep the dialog's input; warning below 128 bits | MVP | [KeePassXC #3754](https://github.com/keepassxreboot/keepassxc/issues/3754), [#3142](https://github.com/keepassxreboot/keepassxc/issues/3142), [KeeOtp2 #66](https://github.com/tiuub/KeeOtp2/issues/66) |
| R12 | `otpauth://` accepted wherever a secret is: liberal case and algorithm spellings, percent-decoded label, `issuer` preferred; strict scheme and length limits | MVP | [Key Uri Format](https://github.com/google/google-authenticator/wiki/Key-Uri-Format), [KeePassXC #9978](https://github.com/keepassxreboot/keepassxc/issues/9978), [#11731](https://github.com/keepassxreboot/keepassxc/issues/11731) |
| R13 | SHA-1/256/512, 6/7/8 digits and any positive period selectable in the UI | MVP | [KeeTrayTOTP #207](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/207), [KeeOtp2 #25](https://github.com/tiuub/KeeOtp2/issues/25), [#22](https://github.com/tiuub/KeeOtp2/issues/22) |
| R14 | HOTP setup from `otpauth://hotp` (with counter) | MVP | [KeeOtp2 #46](https://github.com/tiuub/KeeOtp2/issues/46), [KeePassXC #4371](https://github.com/keepassxreboot/keepassxc/issues/4371) |
| R15 | Setup dialog with live current and next code and seconds remaining | MVP | [KeePassXC #4461](https://github.com/keepassxreboot/keepassxc/issues/4461), [#11340](https://github.com/keepassxreboot/keepassxc/issues/11340) |
| R16 | Configuring OTP never discards unsaved entry edits | MVP | [KeePassXC #10217](https://github.com/keepassxreboot/keepassxc/issues/10217), [#6838](https://github.com/keepassxreboot/keepassxc/issues/6838) |
| R17 | The database is marked modified only when OTP data actually changes | MVP | [KeePassOTP #188](https://github.com/Rookiestyle/KeePassOTP/issues/188), [#130](https://github.com/Rookiestyle/KeePassOTP/issues/130), [KeeOtp2 #31](https://github.com/tiuub/KeeOtp2/issues/31) |
| R18 | Codes copied through KeePass's clipboard path (auto-clear, history and cloud exclusion), without spaces | MVP | [2.44 notes](https://keepass.info/news/n200120_2.44.html), [KeePassXC #12039](https://github.com/keepassxreboot/keepassxc/issues/12039) |
| R19 | The secret or URI is never copied to the clipboard by default | MVP | [KeePass Security](https://keepass.info/help/base/security.html) |
| R20 | All plugin windows and timers close on lock and do not reset the auto-lock timer | MVP | [KeePassXC #923](https://github.com/keepassxreboot/keepassxc/issues/923), [#4436](https://github.com/keepassxreboot/keepassxc/issues/4436) |
| R21 | Screen pick mode: hide own windows, overlay on all monitors, click captures a region around the point or drag captures a rectangle, Esc cancels | MVP | [KeeOtp2 #37](https://github.com/tiuub/KeeOtp2/issues/37), [#40](https://github.com/tiuub/KeeOtp2/issues/40), [1Password](https://1password.community/discussion/comment/516633) |
| R22 | Capture in Per-Monitor-V2 thread context, clamped to the monitor, context restored afterwards | MVP | [High-DPI guide](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows) |
| R23 | Fallback imports from clipboard image and image file | MVP | [1Password workaround](https://1password.community/discussion/comment/516633) |
| R24 | Own QR decoder: v1–40, numeric/alphanumeric/byte modes, both polarities, fractional module sizes, RS error correction | MVP | [Thonky](https://www.thonky.com/qr-code-tutorial/), [Wikiversity RS](https://en.wikiversity.org/wiki/Reed%E2%80%93Solomon_codes_for_coders) |
| R25 | Pixel hygiene: memory only, buffers cleared, explicit action only, smallest region | MVP | [SetWindowDisplayAffinity](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity), [WGC consent model](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture) |
| R26 | Confirmation before saving: issuer, account, parameters, masked secret | MVP | [UZH wiki](https://wiki.math.uzh.ch/public/MFA/In_browser_authenticator) |
| R27 | Google `otpauth-migration` import: multi-batch, preview list, create or attach entries, period fixed at 30 s | MVP | [KeeOtp2 #53](https://github.com/tiuub/KeeOtp2/issues/53), [KeePassOTP #108](https://github.com/Rookiestyle/KeePassOTP/issues/108), [dim13 proto](https://github.com/dim13/otpauth/blob/master/migration/migration.proto) |
| R28 | No telemetry and no network calls by default; any network feature opt-in with a privacy notice | MVP | [KeePassOTP README](https://github.com/Rookiestyle/KeePassOTP), [#81](https://github.com/Rookiestyle/KeePassOTP/issues/81), [#134](https://github.com/Rookiestyle/KeePassOTP/issues/134) |
| R29 | No global hotkey registered by default | MVP | [KeeOtp2 #42](https://github.com/tiuub/KeeOtp2/issues/42), [KeePassOTP #15](https://github.com/Rookiestyle/KeePassOTP/issues/15) |
| R30 | One-time, factual same-database 2FA note; no superlatives anywhere in UI or docs | MVP | [KeePassXC docs](https://raw.githubusercontent.com/keepassxreboot/keepassxc/develop/docs/topics/DatabaseOperations.adoc), [1Password](https://1password.com/blog/1password-2fa-passwords-codes-together) |
| R31 | Single DLL, no third-party runtime assemblies, SHA-256 hashes published, Authenticode-signed once SignPath approves | MVP | [KeeOtp2 #56](https://github.com/tiuub/KeeOtp2/issues/56), [KeePassOTP #79](https://github.com/Rookiestyle/KeePassOTP/issues/79), [Kee forum SAC](https://forum.kee.pm/t/keepassrpc-blocked-by-windows-11-smart-app-control-any-solution/5094) |
| R32 | en/de/ar UI including right-to-left layout; readable under dark themes | MVP | [KeeOtp2 #32](https://github.com/tiuub/KeeOtp2/issues/32), [KeeTrayTOTP #196](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/196) |
| R33 | Steam: `otp` with `encoder=steam`, never native fields; Base64 `shared_secret` input; distinct placeholder | Later | [Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp), [KeeOtp2 #35](https://github.com/tiuub/KeeOtp2/issues/35) |
| R34 | Entry-list code/countdown column, selected row only by default, single shared timer | Later | [KeePassOTP #186](https://github.com/Rookiestyle/KeePassOTP/issues/186), [#190](https://github.com/Rookiestyle/KeePassOTP/issues/190), [KeeTrayTOTP #27](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/27) |
| R35 | Opt-in one-shot clock check and a clearly labelled plugin-only offset | Later | [KeeOtp2 #10](https://github.com/tiuub/KeeOtp2/issues/10), [NTP draft](https://datatracker.ietf.org/doc/html/draft-ietf-ntp-data-minimization) |
| R36 | Export: per-entry `otpauth://` and QR (own encoder, rendered in memory), bulk URI list | Later | [KeePassOTP #124](https://github.com/Rookiestyle/KeePassOTP/issues/124), [#136](https://github.com/Rookiestyle/KeePassOTP/issues/136) |
| R37 | Optional `otp` mirror for older KeePassXC and mobile apps (off by default, regenerated on change, mismatch detection) | Later | [Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp) |
| R38 | Aegis plain-JSON import; drag-and-drop QR; optional hover-plus-hotkey scan | Later | [Aegis vault.md](https://github.com/beemdevelopment/Aegis/blob/master/docs/vault.md), [KeeTrayTOTP #143](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/143) |
| R39 | Investigate multiple OTPs per entry (native fields allow only one) | Later | [KeeOtp2 #51](https://github.com/tiuub/KeeOtp2/issues/51) |

## A 26-to-41-day MVP that leaves display to KeePass itself

The biggest scope saver is that KeePass 2.60+ already shows, copies and auto-types native TOTP and HOTP (Show/Copy since 2.51, Ctrl+T since 2.53, larger font since 2.60). Miftawerk Unlock already requires KeePass 2.60+, and Miftawerk OTP should use the same minimum. The v1.0 MVP can therefore be **an input, migration and validation plugin**. Users keep generating and copying codes with KeePass's own commands, and a countdown column, Steam and export arrive later.

The table below lists the **estimated** effort. These are not measurements. They are derived from the research notes' own estimates for capture, decoder and imports, plus judgement for the rest. Full-time, they correspond to about 5–8 weeks.

| Work package (MVP) | Estimate, developer-days |
|---|---|
| Repo skeleton reused from Miftawerk Unlock (net48, xUnit, SHA-pinned CI, en/de/ar resources, privacy policy) | 1–2 |
| OTP engine, Base32 normalizer, RFC vectors | 2–3 |
| Format readers/writers, conflict detection, migration with history, tests | 4–6 |
| `otpauth://` parser/writer with tests | 1 |
| Setup dialog: preview, validation, confirmation, same-DB note, RTL | 4–6 |
| Own QR decoder, screen-optimized, plus test corpus | 5–10 |
| Pick/snip overlay, multi-monitor, Per-Monitor-V2 capture, mixed-DPI testing | 4–6 |
| Clipboard-image and file import | 1 |
| Google migration import with batch merge and preview | 2–3 |
| Docs, release packaging, hashes, signing paperwork | 2–3 |
| **MVP total** | **26–41** |

The later scope is estimated at **about 14–21 additional developer-days**:

| Later work package | Estimate, developer-days |
|---|---|
| Own QR encoder and export | 3–5 |
| Steam | 1–2 |
| Countdown column | 3–4 |
| Opt-in clock check and offset | 2–3 |
| `otp` mirror | 2 |
| Aegis import, drag-and-drop and optional hotkey | 3–5 |

Using ZXing.Net instead of an own decoder would cut roughly 4–9 days from the MVP. It would cost the from-scratch property and add a dependency of the kind that has crashed other plugins, so the notes do not support that trade for this project.

The evidence-backed differences the project can state at release, each phrased with a date and verifiable, are the following. They avoid "best", "strongest", "first" and "only" in line with UWG §5.

- It ships as a DLL rather than a PLGX. The three GitHub OTP plugins publish only `.plgx` as of 2026-10-23.
- It makes no network connections unless the user enables a feature that needs one.
- It is MIT-licensed. KeePassOTP and KeeTrayTOTP are GPL-3.0.
- It writes only KeePass's own fields, so entries keep working without the plugin.
- It imports Google Authenticator exports in bulk. This is declined in KeePassOTP #108 and open in KeeOtp2 #53.

## Seven risks, most retired by small early spikes

| Risk | Why it is real | Mitigation |
|---|---|---|
| Mixed-DPI capture hits the wrong area | KeePass measured system-DPI-aware (local, 2.60); Microsoft says virtualization is under-documented ([guide](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)) | 1–2-day spike on a 100% + 150% two-monitor setup before building UI; Per-Monitor-V2 thread switch |
| Own decoder misses real-world codes | Production scanners fail at detection ([1Password](https://1password.community/discussion/comment/516633)); online decoders needed for some codes ([SF 2024](https://sourceforge.net/p/keepass/discussion/329221/thread/d67d6aabf2/)) | Corpus at fractional zoom and inverted colours; drag-rectangle, clipboard, file and text fallbacks always available |
| Smart App Control or reputation blocks the DLL | SAC blocks unverified or unsigned files ([Microsoft](https://support.microsoft.com/en-us/topic/smart-app-control-has-blocked-part-of-this-app-0729fff1-48bf-4b25-aa97-632fe55ccca2)); effect on a managed DLL loaded by signed KeePass is unresearched; Miftawerk Unlock's signing is still pending | Test the existing Miftawerk Unlock DLL on the SAC-enabled PC now; apply to SignPath Foundation (free) early; publish hashes |
| Prebuilt DLL breaks on a future KeePass API change | PLGX plugins already broke after KeePass updates ([KeeOtp2 #13](https://github.com/tiuub/KeeOtp2/issues/13), [KeeTrayTOTP #32](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/32)) | Use only stable public KeePass API; CI builds against 2.60 and latest; catch init failures without crashing KeePass |
| Two formats drift apart | KeePassXC prefers `otp` and never updates `TimeOtp-*` ([Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp)) | Native fields as single source of truth; mirror off by default; mismatch detection (R8) |
| Google migration format changes | No official spec; community schemas already differ on `digits` ([dim13](https://github.com/dim13/otpauth/blob/master/migration/migration.proto) vs [scito](https://github.com/scito/extract_otp_secrets/blob/master/src/google_auth.proto)) | Tolerant varint reader, accept 1/2 and 6/8, preview before creating entries |
| Licence or legal contamination | GPL references (KeePassXC, KeePassOTP, KeeTrayTOTP, Aegis); BSD RFC code; UWG claims | Study behaviour only, write from specs; RFC prose not Java; dated, factual comparison claims |

The notes leave three unknowns open. First, whether Keepass2Android, KeePassDX and Strongbox read `TimeOtp-*` fields was not verified; this decides when the `otp` mirror becomes urgent. Second, it is unconfirmed whether current Google Authenticator still ignores `algorithm`, `digits` and `period`, since the wiki is from 2018. Third, the exact name of KeePass's clipboard helper API needs checking in the KeePass source.

## Recommendation and first step

**Recommendation:** Build Miftawerk OTP v1.0 as a dependency-free, MIT, Authenticode-signed .NET 4.8 DLL for KeePass 2.60+. It treats KeePass's native `TimeOtp-*` / `HmacOtp-*` fields as the single source of truth. Its features are QR import (screen pick mode with click-or-drag, clipboard image, image file), forgiving `otpauth://` paste, bulk Google Authenticator migration import, and legacy-format migration with conflict detection, all on a from-scratch RFC engine and QR decoder with no network access. It leaves code display, copying and auto-type to KeePass's own commands, and adds Steam, a countdown column and export in v1.1.

**First concrete step:** Create the `SeeColors/miftawerk-otp` repository from Miftawerk Unlock's skeleton (net48 project, xUnit, SHA-pinned GitHub Actions, en/de/ar resources). Land one pull request that contains only the OTP engine and Base32 normalizer, written from the RFC prose, with the 28 RFC test vectors, the Erratum 2866 negative control, the 64-bit timestamp case and the normalization cases from R11 as xUnit tests. The estimate is 2–3 days. It has no UI and no risk, and every later feature depends on it.

## Conclusion

The research changes the framing of the project. The opportunity is not a better code generator, because KeePass has had a correct one since 2.47. The real opportunity is the edges around generation: getting secrets in (QR, Google exports), cleaning up four incompatible legacy formats, and validating input so that "wrong code" turns into a clear message instead of a support ticket. That is a smaller, more testable product than the existing plugins, and it shifts most user-visible behaviour onto KeePass's own maintained code.

The same choice also protects users from the maintenance pattern seen across this ecosystem: two of the three GitHub plugins have stalled, and one maintainer no longer uses KeePass. Because Miftawerk OTP stores nothing that KeePass cannot read alone, its users are never stranded, even if the project itself pauses.
