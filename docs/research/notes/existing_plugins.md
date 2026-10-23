# Existing KeePass 2 OTP/TOTP plugins and user complaints (research for "Miftawerk OTP")

Research date: 2026-10-23. Method: GitHub REST API pulls of repo metadata, releases (with asset names and download counts) and the full issue lists (open and closed, every page) for tiuub/KeeOtp2 (55 items), KeeTrayTOTP/KeeTrayTOTP (384 items, most of them Dependabot PRs) and Rookiestyle/KeePassOTP (192 items). I also read the READMEs, the keepass.info plugin and placeholder pages, SourceForge KeePass forum threads and the Kee forum. Reddit and the SourceForge forum search could not be fetched (both blocked automated access). See Gaps.
Tags: **[body]** means I read the issue body or comments. **[title]** means I only saw the title. Dates are the issue creation dates (YYYY-MM-DD). Counts are from the API dump taken on 2026-10-23.

## Q1. KeeOtp2 (tiuub): features, storage, releases, issues by theme

### Takeaway
KeeOtp2 is an MIT plugin, shipped as PLGX only. It acts as a GUI front-end for KeePass's own `TimeOtp-*` fields, and it can scan QR codes from the screen, show QR codes, register a global hotkey, use NTP or a fixed time offset, and handle Steam. Development comes in bursts. Releases are v1.5.9 (2022-07-04), then nothing until v1.6.0 (2025-03-21), and the issues opened since then have had no reply. The recurring complaints are load/compile breakage, hotkey conflicts, unreliable QR scanning, interop with other formats (KeeOtp1, KeePassOTP, KeePassXC/K2A Steam, Google migration URIs), and crashes on unusual secrets.

### Cited Findings
**Identity and status**
- Repo created 2021-01-20, last push 2025-03-21, 164 stars, 13 forks, 16 open items, license MIT, not archived — [GitHub API repo](https://api.github.com/repos/tiuub/KeeOtp2)
- Releases: v1.6.0 2025-03-21 (KeeOtp2.plgx, 17,188 downloads); v1.5.9 2022-07-04 (28,296); v1.5.8.1 2021-11-30; v1.5.8 2021-11-21; v1.5.7 2021-09-24; v1.5.6.1 2021-06-22. **Every release asset is a single `.plgx`. No DLL, no signature, no hash file** — [releases](https://github.com/tiuub/KeeOtp2/releases)
- Also published on Chocolatey (`keepass-plugin-keeotp2`). A user asked on 2025-11-11 for the Chocolatey package to be updated (it lags behind) and got no reply — [#63](https://github.com/tiuub/KeeOtp2/issues/63) [body]
- The maintainer's activity is bursty. In 2022-02 he said he would answer late because of exams ([#40 comments](https://github.com/tiuub/KeeOtp2/issues/40)). In 2022-03 a user asked "Is this repo still active yet?" ([#44](https://github.com/tiuub/KeeOtp2/issues/44)) [body]. On 2025-03-21 he wrote that he had finished his thesis and released 1.6.0 ([#55 comments](https://github.com/tiuub/KeeOtp2/issues/55)) [body]. Issues #63, #65 and #66 (2025-11 to 2026-05) have 0 comments.
- keepass.info describes it as giving "a form to display one-time passwords". It stores secrets "in a normalized format" that is fully compatible with the built-in OTP. It derives from KeeOtp(1), which is discontinued — [keepass.info plugins](https://keepass.info/plugins.html)

**Features (README, own wording)**
- Configure TOTP and Show TOTP from the entry context menu under a KeeOtp2 submenu. It can migrate KeeOtp(1) secrets (the `otp` field) to the built-in format, either one entry at a time or in bulk from Tools > KeeOtp2 > Settings — [README](https://github.com/tiuub/KeeOtp2)
- A global auto-type hotkey (default Ctrl+Alt+T) that can be changed. It supports the `{TIMEOTP}` placeholder (recommended) and the legacy `{TOTP}` from KeeOtp1 (deprecated) — [README](https://github.com/tiuub/KeeOtp2)
- A "Global Time" setting with three modes: system time, a fixed offset in seconds, or a custom NTP server. An "Incorrect?" troubleshooting button pings an NTP server and recommends a fix if the clock is off by more than 5 s — [README](https://github.com/tiuub/KeeOtp2)
- It can share a configuration as an `otpauth://` URI or show it as a QR code ("Show QR" with "Copy URI") — [README](https://github.com/tiuub/KeeOtp2)
- It scans QR codes from the screen. Evidence: the bug reports about scanning only on the last monitor ([#37](https://github.com/tiuub/KeeOtp2/issues/37)) and about failures that were fixed by hiding the window before the screenshot ([#40](https://github.com/tiuub/KeeOtp2/issues/40)) [body]. I found no evidence of clipboard-image QR import.
- Third-party dependencies: KeeOtp(1) code (MIT), Otp.NET (MIT), Yort.Ntp.Portable (MIT), ZXing.Net (Apache-2.0), NHotkey and NHotkey.WindowsForms (Apache-2.0). Tests use xUnit. Icons are Material Design icons (Apache-2.0) — [README dependency table](https://github.com/tiuub/KeeOtp2)
- Steam support was added in 2021-06 as an "output transform" ([PR #24](https://github.com/tiuub/KeeOtp2/pull/24), [#20](https://github.com/tiuub/KeeOtp2/issues/20)). Version 1.6.0 added Steam compatibility with KeePassXC and Keepass2Android, which read an `otpauth://` URI in the `otp` field ([#55](https://github.com/tiuub/KeeOtp2/issues/55)) [body]

**Storage format**
- It writes KeePass's built-in fields. One report mentions `TimeOtp-Secret-Base32` being created ([#16](https://github.com/tiuub/KeeOtp2/issues/16)) [body]. It still reads the legacy KeeOtp1 `otp` string, which has the form `key=…&size=…` (Steam: `key=xxx&type=Steam&size=5`). A user pointed out that this differs from the `otpauth://` format used by KeePassXC and K2A and that `size` is non-standard (the standard parameter is `digits`) — [#55](https://github.com/tiuub/KeeOtp2/issues/55) [body]

**Issues grouped by theme** (48 issues + 7 PRs. Open as of 2026-10-23: #66, #65, #63, #53, #46, #45, #41, #30 plus PRs #61, #58)
1. **Load/compile breakage after KeePass, .NET or Mono changes: 6.**
   - v1.5.0 failed to compile on some PCs because of x86/x64 library issues; 17 comments; fixed in 1.5.1 ([#13](https://github.com/tiuub/KeeOtp2/issues/13), 2021-03-05) [body]
   - "method not found" errors ([#15](https://github.com/tiuub/KeeOtp2/issues/15), 2021-03-11) [title]
   - `OtpSharp.Totp.ComputeTotp()` method not found when pressing the global auto-type hotkey ([#17](https://github.com/tiuub/KeeOtp2/issues/17), 2021-05-21) [body]
   - Does not load on KeePass 2.48.1 portable under Mono on Ubuntu ([#29](https://github.com/tiuub/KeeOtp2/issues/29), 2021-06-22) [body]
   - Hotkey already registered, so KeePass reports "plugin incompatible" ([#42](https://github.com/tiuub/KeeOtp2/issues/42), 2022-02-22) [body]
   - `MissingMethodException` in `ZXing.IBarcodeWriter.Write` when clicking Show QR ([#56](https://github.com/tiuub/KeeOtp2/issues/56), 2024-09-17) [body]
2. **Hotkeys and shortcuts: 8.**
   - Request for an OTP-only auto-type hotkey ([#2](https://github.com/tiuub/KeeOtp2/issues/2), 2021-01)
   - Cannot assign a hotkey to TIMEOTP ([#19](https://github.com/tiuub/KeeOtp2/issues/19), 2021-05)
   - Ctrl+T stopped copying ([#21](https://github.com/tiuub/KeeOtp2/issues/21), [#26](https://github.com/tiuub/KeeOtp2/issues/26), 2021-06)
   - Ctrl+A broken ([#27](https://github.com/tiuub/KeeOtp2/issues/27))
   - Local shortcut conflicts with another plugin ([#34](https://github.com/tiuub/KeeOtp2/issues/34), 2021-11)
   - "Hotkey is registered" at startup ([#42](https://github.com/tiuub/KeeOtp2/issues/42))
   - Exception when the hotkey is set to none ([#43](https://github.com/tiuub/KeeOtp2/issues/43), 2022-03) [titles; #42 body]
3. **QR scanning: 3.**
   - Only scans the last of several monitors; fixed in 1.5.8.1 ([#37](https://github.com/tiuub/KeeOtp2/issues/37), 2021-11-30) [body]
   - Scanning succeeded only about 40% of the time because the plugin's own message box covered the QR code. Fixed on 2022-07-04 by adding a delay so the window is invisible when the screenshot is taken ([#40](https://github.com/tiuub/KeeOtp2/issues/40), 2021-12-23) [body+comments]
   - Show-QR crash ([#56](https://github.com/tiuub/KeeOtp2/issues/56), 2024) [body]
4. **Non-standard and parameter support: 6.**
   - Steam requested ([#20](https://github.com/tiuub/KeeOtp2/issues/20), 2021-05)
   - "Copy TOTP" gives a wrong value for Steam; only the Show TOTP window works ([#35](https://github.com/tiuub/KeeOtp2/issues/35), 2021-11) [body]
   - 7-digit codes ([#25](https://github.com/tiuub/KeeOtp2/issues/25))
   - Only 7 of 8 digits displayed ([#1](https://github.com/tiuub/KeeOtp2/issues/1))
   - Periods other than 30 s not recognized ([#22](https://github.com/tiuub/KeeOtp2/issues/22))
   - HOTP requested, still open ([#46](https://github.com/tiuub/KeeOtp2/issues/46), 2022-09-20) [title/body short]
5. **Format interop and migration: about 11.**
   - KeeOtp1 migration errors ([#4](https://github.com/tiuub/KeeOtp2/issues/4), [#11](https://github.com/tiuub/KeeOtp2/issues/11)) and storage compatibility questions ([#7](https://github.com/tiuub/KeeOtp2/issues/7))
   - Lowercase secrets not handled ([#8](https://github.com/tiuub/KeeOtp2/issues/8))
   - Manual edits of the key or URI not saved ([#14](https://github.com/tiuub/KeeOtp2/issues/14))
   - An `otp` field written by KeePassOTP stops the secret being detected, so the setup dialog keeps reappearing; renaming the field breaks KeePassOTP on another PC ([#16](https://github.com/tiuub/KeeOtp2/issues/16), 2021-05) [body]
   - Broken entries from 1.5.6 ([#28](https://github.com/tiuub/KeeOtp2/issues/28))
   - Crash if an `otp` key already exists, still open ([#41](https://github.com/tiuub/KeeOtp2/issues/41), 2022-01) [body]
   - Steam format incompatible with KeePassXC and K2A ([#55](https://github.com/tiuub/KeeOtp2/issues/55), 2024-07, fixed in 1.6.0) [body]
   - Google Authenticator `otpauth-migration://offline?data=` support, still open ([#53](https://github.com/tiuub/KeeOtp2/issues/53), 2024-03-26) [body]
   - Percent-encoded padding (`%3D`) in a secret breaks 1.6.0; the fix PR #61 is still open ([#61](https://github.com/tiuub/KeeOtp2/pull/61), 2025-03-21) [body]
   - "Two TOTP per entry" for services such as Huawei that ask for 2 codes ([#51](https://github.com/tiuub/KeeOtp2/issues/51), 2023-11, closed 2025-03) [body]
6. **Crashes on use: 2 open.**
   - Copy TOTP throws `ArgumentException: Destination array was not long enough` inside Otp.NET `InMemoryKey..ctor`; the user says it happens frequently ([#66](https://github.com/tiuub/KeeOtp2/issues/66), 2026-05-11, 0 replies) [body]
   - Crash if an `otp` key exists ([#41](https://github.com/tiuub/KeeOtp2/issues/41))
7. **Time accuracy: 2.**
   - A work laptop was 87 s off and policy blocked changing the clock. This led to the manual offset and NTP feature ([#10](https://github.com/tiuub/KeeOtp2/issues/10), 2021-03) [body]
   - Wrong code; the user suspects the time zone (UTC+1) ([#64](https://github.com/tiuub/KeeOtp2/issues/64), 2026-03-15) [body]
8. **UI, theming and window behaviour: 6.**
   - Dialogs hidden under KeePass when "Always on top" is enabled ([#5](https://github.com/tiuub/KeeOtp2/issues/5), 2021-02) [body]
   - Settings window has layout problems on Windows 8.1, open, body is mostly screenshots ([#30](https://github.com/tiuub/KeeOtp2/issues/30), 2021-07) [body]
   - Almost nothing readable with the KeeTheme dark theme ([#32](https://github.com/tiuub/KeeOtp2/issues/32), 2021-09) [body]
   - Last-modification column not updated ([#31](https://github.com/tiuub/KeeOtp2/issues/31))
   - Community PR to improve the UI, add settings, add KeeTrayTOTP compatibility and add a context-menu Show-QR item, still open ([PR #58](https://github.com/tiuub/KeeOtp2/pull/58), 2024-11-26) [body]
   - Request for a toolbar button to copy the TOTP ([#65](https://github.com/tiuub/KeeOtp2/issues/65), 2026-04-27) [body]
9. **Performance: 1 open.** 100% CPU and a hung background process with KeePass 2.39.1 (Pleasant Password Server edition) on several machines ([#45](https://github.com/tiuub/KeeOtp2/issues/45), 2022-04-14) [body]
10. **Packaging and Chocolatey: 3.** [#33](https://github.com/tiuub/KeeOtp2/issues/33) (2021-10, install error, closed 2024-01), [#36](https://github.com/tiuub/KeeOtp2/issues/36), [#63](https://github.com/tiuub/KeeOtp2/issues/63)

### Inferences
- #42 and #56 suggest two failure modes. A global hotkey that is already taken should degrade gracefully, not make the plugin "incompatible". Bundling third-party assemblies such as ZXing inside a PLGX can clash at runtime, possibly with another plugin's copy, though that is not confirmed in the issue.
- #66 points to missing validation of secret length or encoding before the key reaches the OTP library. A from-scratch implementation should validate and normalize Base32 (case, spaces, padding, `%3D`) and report errors instead of crashing.
- #40 shows that screen-capture QR scanning must hide the plugin's own UI before capturing and must cover all monitors (#37).

### Gaps
- I did not find whether KeeOtp2 reads a QR image from the clipboard or from a file. The README covers only showing a QR code. Scanning is evidenced only by the issues.
- I did not verify a high-DPI-specific issue. #30 might be DPI-related, but the body is mostly screenshots.
- I found no Smart App Control complaint in the KeeOtp2 tracker.

## Q2. Other plugins: KeeOtp (original), Tray TOTP / KeeTrayTOTP, KeePassOTP, TwoFactorQRCodeReader, OtpKeyProv, built-in OTP

### Takeaway
keepass.info lists four OTP-generation plugins: KeePassOTP (Rookiestyle, GPL-3.0), KeeOtp2 (MIT), KeeTrayTOTP (GPL-3.0) and TwoFactorQRCodeReader. The original KeeOtp and Tray TOTP are discontinued, and OtpKeyProv is only a key provider for unlocking the database. KeePassOTP is the most actively maintained and has the most features. KeeTrayTOTP is in maintenance-only mode. "KPOTP" is KeePassOTP's placeholder, not a separate plugin.

### Cited Findings
**Built-in KeePass OTP (the baseline every plugin builds on)**
- KeePass 2.47 added the `{TIMEOTP}` placeholder (RFC 6238) — [KeePass 2.47 news](https://keepass.info/news/n210109_2.47.html). The URL date suggests 2021-01-09; the page text says only "released today".
- Built-in fields:
  - HOTP: one of `HmacOtp-Secret`, `-Hex`, `-Base32` or `-Base64`, plus `HmacOtp-Counter`
  - TOTP: one of `TimeOtp-Secret`, `-Hex`, `-Base32` or `-Base64`, plus `TimeOtp-Length` (default 6, max 8), `TimeOtp-Period` (default 30) and `TimeOtp-Algorithm` (SHA-1 default, SHA-256, SHA-512)
  - There is an "OTP Generator Settings" dialog with a preview. The page sends users to plugins for non-standard OTPs such as Steam and does not mention QR codes — [KeePass placeholders help](https://keepass.info/help/base/placeholders.html)
- A 2025 SourceForge thread notes that the native OTP settings cover setup "except via QR code". Users there pair TwoFactorQRCodeReader with the built-in generator — [SF thread 2025-06](https://sourceforge.net/p/keepass/discussion/329221/thread/763ff4fd2e/)

**KeeOtp (original, "KeeOtp(1)")**
- Author Devin Martin. Last version 1.3.9, last updated 2015-09-29. It stores TOTP secrets in the database and is compatible with Google 2-Step and AWS MFA (RFC 6238). It ships as `KeeOtp.plgx` — [Chocolatey keepass-plugin-keeotp](https://community.chocolatey.org/packages/keepass-plugin-keeotp)
- The original Bitbucket site is gone. tiuub re-uploaded the source as KeeOtpMirror, and it is MIT-licensed per KeeOtp2's dependency table — [KeeOtp2 README](https://github.com/tiuub/KeeOtp2); [KeeOtpMirror](https://github.com/tiuub/KeeOtpMirror)
- keepass.info calls it discontinued — [keepass.info plugins](https://keepass.info/plugins.html)
- Storage: an `otp` string field of the form `key=…&size=…`, with the `{TOTP}` placeholder — [KeeOtp2 #55](https://github.com/tiuub/KeeOtp2/issues/55), [KeeOtp2 README](https://github.com/tiuub/KeeOtp2)
- Note: the brief named "daveshaw/KeeOtp" as the original. The GitHub API returned 404 for that repo, and every source names Devin Martin as the original author.

**KeeTrayTOTP (KeeTrayTOTP/KeeTrayTOTP; formerly victor-rds/KeeTrayTOTP, which now redirects)**
- Created 2015-09-08, last push 2026-08-14 (Dependabot), 510 stars, 46 forks, 24 open items, GPL-3.0 — [GitHub API](https://api.github.com/repos/KeeTrayTOTP/KeeTrayTOTP)
- It is a fork of Tray TOTP by Morphlin on SourceForge, created to add Steam support. It needs KeePass 2.31 and .NET 4.5. Docs explain how to extract Steam secrets via Steam Desktop Authenticator, a rooted Android phone or an iOS backup. It is on Chocolatey — [README](https://github.com/KeeTrayTOTP/KeeTrayTOTP)
- keepass.info says it shows TOTPs from the KeePass tray icon, adds a list column, supports auto-type, and works with Google, Dropbox and Steam. Tray TOTP itself is discontinued — [keepass.info plugins](https://keepass.info/plugins.html)
- Releases:
  - Last tagged release v0.108.0, 2020-09-12, `KeeTrayTOTP.plgx` with 41,578 downloads
  - A rolling "latest" pre-release from 2026-05-28 contains `KeeTrayTOTP.plgx` and `hashes.txt`
  - PLGX only, no signature — [releases](https://github.com/KeeTrayTOTP/KeeTrayTOTP/releases)
- Maintenance: on 2023-12-13 the maintainer wrote that he took over around 2015, no longer uses KeePass, will do minor bug fixes, and cannot support new functionality — [#325](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/325) [body]
- Storage: two non-standard string fields, `TOTP Seed` and `TOTP Settings` (for example "30;6"). Users asked to switch to a standard `otpauth://` Key URI instead; the issue is still open with 14 comments — [#110](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/110) (2020-04) [body]; [#147](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/147) [title]; [KeePassOTP #184](https://github.com/Rookiestyle/KeePassOTP/issues/184) [body]
- KeeTrayTOTP complaints (about 95 non-PR issues):
  - **Wrong codes:**
    - 32-character seeds give wrong results, still open ([#243](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/243), 2021-08) [body]
    - The setup UI does not expose SHA-256/512 even though the code supports them, still open ([#207](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/207), 2020-12) [body]
    - SHA512 question, open ([#377](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/377), 2026-04) [body]
    - A Windows 11 wrong-code report turned out to be a wrong system clock ([#311](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/311), 2023-05) [body]
    - Padding `=` errors ([#130](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/130)) [title]
  - **Performance:**
    - Performance hit from the column on large databases, open ([#27](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/27), 2018) [body]
    - System freeze under Mono on a Raspberry Pi when combined with KeePassOTP, open ([#305](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/305), 2023) [body]
    - Possible sync problems on SMB or Nextcloud ([#327](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/327), 2025-04) [body]
  - **Display:**
    - TOTP column does not count down until a copy ([#328](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/328), 2025-08) [body]
    - Counter not ticking ([#192](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/192)) [title]
    - Ampersand in a title shown as an accelerator underline, open ([#232](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/232)) [title]
    - Dialogs compressed and unreadable, 13 comments ([#8](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/8), 2016) [title]
    - Tray menu on multi-monitor systems, 14 comments ([#160](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/160), 2020) [title]
    - Popup position after switching databases ([#61](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/61)) [title]
  - **Auto-type:** the `{TOTP}` output includes the existing clipboard content, open ([#236](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/236), 2021-07) [body]
  - **QR:** drag-and-drop and "Read from Screen" do not work on KeePass 2.61.1 ([#382](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/382), 2026-06-09) [body]. The user mentions the "KeePassOTP" message "OTP: Error", so the report may mix up the two plugins.
  - **Load breakage:**
    - 0.97-Beta does not load on KeePass 2.42, 14 comments ([#32](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/32), 2019-05) [title]
    - Fails on Windows 7 ([#124](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/124)) [title]
    - Mono problems ([#5](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/5)) [title]
  - **Open feature requests:**
    - Global auto-type shortcut for TOTP ([#35](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/35))
    - Register an `otpauth://` protocol handler ([#144](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/144))
    - Drag a QR code onto an entry ([#143](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/143))
    - A window showing all TOTP codes ([#134](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/134))
    - Integrate setup into the Edit Entry dialog ([#116](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/116))
    - Streamline the setup wizard ([#34](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/34))
    - More flexible time settings ([#231](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/231))
    - Fix the TLS issue with time sync ([#145](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/145))
    - Hide TOTP for expired entries ([#194](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/194))
    - Translations ([#196](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/196))
    - Yandex ([#197](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/197), closed 2026-08)
    - Battle.net, 22 comments ([#3](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/3))
    - Twitch ([#48](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/48))
    - 7 digits ([#40](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/40))
    - Period up to 180 s ([#50](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/50))
    - Color-coded tray entries ([#176](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/176))
    - Hash file for integrity checks ([#187](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/187))
    - [titles unless noted]
  - **Other:** a help window opens on every KeePass start ([#18](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/18), [#42](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/42), [#195](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/195)) [titles]
- A 2019 SourceForge thread reported that Tray TOTP had stopped working with newer KeePass versions — [SF search result summary](https://sourceforge.net/p/keepass/discussion/329220/thread/4625bdee46/) (search snippet only, not read)

**KeePassOTP (Rookiestyle) — keepass.info lists it first**
- Created 2020-06-11, last push 2026-09-18, 519 stars, 32 forks, 0 open issues, GPL-3.0 — [GitHub API](https://api.github.com/repos/Rookiestyle/KeePassOTP)
- Releases are frequent: v1.12 2026-02-10 (KeePassOTP.plgx, 27,252 downloads), v1.11 2025-11-29, v1.10.2 2025-05-17, v1.10 2025-03-07, 1.9 2025-01-18, v1.8 2024-09-23. PLGX only — [releases](https://github.com/Rookiestyle/KeePassOTP/releases)
- Features (README, own wording):
  - TOTP and HOTP, plus Steam and Yandex (Yandex.Key below version 3 only, because v3 changed a closed algorithm)
  - Drag and drop a QR image onto the setup form, and a screen-capture button that finds a QR code anywhere on screen
  - Paste an `otpauth://` URI
  - The setup form shows both the current and the next OTP
  - `{KPOTP}` placeholder, configurable and working inside and outside KeePass
  - A column that shows OTPs or flags entries where 2FA is possible; it downloads the 2fa.directory list at startup, which can be turned off
  - Copy or auto-type from the tray icon
  - Export QR codes for apps such as Google Authenticator and andOTP
  - Translation XML files
  - Storage either inside the entry or in a separate embedded OTP database
  - Requires KeePass 2.42 and .NET 4.0
  - [README](https://github.com/Rookiestyle/KeePassOTP); [keepass.info](https://keepass.info/plugins.html)
- Storage and interop: it can keep OTP data in an `otp` entry field that Keepass2Android can read. Data in the separate embedded database is not visible to mobile apps — [SF thread 2025-06](https://sourceforge.net/p/keepass/discussion/329221/thread/763ff4fd2e/); [SF thread 2024-02](https://sourceforge.net/p/keepass/discussion/329221/thread/d67d6aabf2/)
- KeePassOTP complaint themes (192 items, all closed):
  - **Separate OTP database friction:**
    - Asked for the OTP database password every time ([SF 2024](https://sourceforge.net/p/keepass/discussion/329221/thread/d67d6aabf2/))
    - Still prompted after unticking auto-open ([#176](https://github.com/Rookiestyle/KeePassOTP/issues/176), 2025-09) [title]
    - Prompted for a password on every save even with the separate DB disabled, wontfix ([#188](https://github.com/Rookiestyle/KeePassOTP/issues/188), 2026-06) [body]
    - "Copy OTP" should work while the OTP DB is locked ([#97](https://github.com/Rookiestyle/KeePassOTP/issues/97)) [body]
    - OTP database password not accepted ([#161](https://github.com/Rookiestyle/KeePassOTP/issues/161)) [title]
    - Accidental deletion of the OTP DB possible ([#127](https://github.com/Rookiestyle/KeePassOTP/issues/127)) [title]
  - **Database marked modified on every open or save:** [#130](https://github.com/Rookiestyle/KeePassOTP/issues/130) (2023-02) [body], [#166](https://github.com/Rookiestyle/KeePassOTP/issues/166) (2025-02, after migrating from KeeTrayTOTP) [body], [#174](https://github.com/Rookiestyle/KeePassOTP/issues/174) (2025-08, "always asked to save") [title]
  - **Google Authenticator import:**
    - [#22](https://github.com/Rookiestyle/KeePassOTP/issues/22) (2020-10, 13 comments) [body]
    - [#90](https://github.com/Rookiestyle/KeePassOTP/issues/90) (2022-02) [title]
    - Bulk import of several seeds into new entries, wontfix ([#108](https://github.com/Rookiestyle/KeePassOTP/issues/108), 2022-07) [body]
    - Cannot import a Google export image ([#179](https://github.com/Rookiestyle/KeePassOTP/issues/179), 2025-11) [body]
  - **Proprietary or unsupported issuers:**
    - Yandex ([#11](https://github.com/Rookiestyle/KeePassOTP/issues/11), [#25](https://github.com/Rookiestyle/KeePassOTP/issues/25), [#112](https://github.com/Rookiestyle/KeePassOTP/issues/112) broke after Yandex Key v3, 11 comments)
    - Microsoft 365 QR not supported, wontfix ([#133](https://github.com/Rookiestyle/KeePassOTP/issues/133), 2023-03) [body], [#151](https://github.com/Rookiestyle/KeePassOTP/issues/151)
    - Steam ([#19](https://github.com/Rookiestyle/KeePassOTP/issues/19), [#21](https://github.com/Rookiestyle/KeePassOTP/issues/21))
    - Wrong codes for Yahoo, Oracle Cloud, Protonmail and GitHub ([#142](https://github.com/Rookiestyle/KeePassOTP/issues/142), [#178](https://github.com/Rookiestyle/KeePassOTP/issues/178), [#140](https://github.com/Rookiestyle/KeePassOTP/issues/140), [#104](https://github.com/Rookiestyle/KeePassOTP/issues/104)) [titles]
  - **Hotkeys:**
    - Ctrl+T registered globally, so it breaks browser new-tab ([#15](https://github.com/Rookiestyle/KeePassOTP/issues/15) wontfix, [#32](https://github.com/Rookiestyle/KeePassOTP/issues/32)) [body #15]
    - Hotkeys fail on Russian keyboard layouts ([#110](https://github.com/Rookiestyle/KeePassOTP/issues/110))
    - Fails on Instagram ([#113](https://github.com/Rookiestyle/KeePassOTP/issues/113))
    - Auto-type types only 1 digit ([#152](https://github.com/Rookiestyle/KeePassOTP/issues/152), 2024-05)
    - [titles]
  - **Crashes and load errors:**
    - "Incompatible with the current KeePass version", 15 comments ([#31](https://github.com/Rookiestyle/KeePassOTP/issues/31), 2021-01)
    - Crash on startup ([#78](https://github.com/Rookiestyle/KeePassOTP/issues/78))
    - Missing Newtonsoft.Json on Manjaro ([#79](https://github.com/Rookiestyle/KeePassOTP/issues/79), 2021-11)
    - Crash at startup without a network ([#81](https://github.com/Rookiestyle/KeePassOTP/issues/81))
    - NullReferenceException and ArgumentException ([#171](https://github.com/Rookiestyle/KeePassOTP/issues/171), [#172](https://github.com/Rookiestyle/KeePassOTP/issues/172), 2025-05)
    - Wrong behaviour on the secure desktop ([#85](https://github.com/Rookiestyle/KeePassOTP/issues/85))
    - [titles]
  - **Network and privacy:**
    - Request to delay the network call ([#55](https://github.com/Rookiestyle/KeePassOTP/issues/55))
    - A DNS lookup for every URL entry ([#134](https://github.com/Rookiestyle/KeePassOTP/issues/134), 2023-04)
    - The time-correction feature mis-parses the HTTP Date header, giving a wrong offset ([#187](https://github.com/Rookiestyle/KeePassOTP/issues/187), 2026-04)
    - [titles]
  - **Display and security:**
    - Show OTP values only for the selected row because showing all of them "feels insecure", wontfix ([#186](https://github.com/Rookiestyle/KeePassOTP/issues/186), 2026-02) [body]
    - A column for time remaining ([#190](https://github.com/Rookiestyle/KeePassOTP/issues/190), 2026-06, done) [body]
    - Show the code without revealing anything else ([#180](https://github.com/Rookiestyle/KeePassOTP/issues/180)) [title]
    - Setup form overflows its window ([#63](https://github.com/Rookiestyle/KeePassOTP/issues/63), 2021-08) [title]
    - Tray submenu appears on the wrong screen with 2 monitors ([#132](https://github.com/Rookiestyle/KeePassOTP/issues/132), 2023-03) [title]
  - **Migration requests:** from KeeTrayTOTP ([#184](https://github.com/Rookiestyle/KeePassOTP/issues/184), 2026-01, implemented) [body], KeePassXC ([#177](https://github.com/Rookiestyle/KeePassOTP/issues/177), 2025-10) [title], export ([#124](https://github.com/Rookiestyle/KeePassOTP/issues/124), [#136](https://github.com/Rookiestyle/KeePassOTP/issues/136)) [titles]
  - **Other requests:**
    - Camera (WIA) QR capture ([#121](https://github.com/Rookiestyle/KeePassOTP/issues/121)) [body]
    - Copy password and OTP combined ([#125](https://github.com/Rookiestyle/KeePassOTP/issues/125), wontfix) [body]
    - Recovery-code storage ([#68](https://github.com/Rookiestyle/KeePassOTP/issues/68), [#120](https://github.com/Rookiestyle/KeePassOTP/issues/120), [#144](https://github.com/Rookiestyle/KeePassOTP/issues/144)) [titles]
    - Confirm before deleting an OTP ([#143](https://github.com/Rookiestyle/KeePassOTP/issues/143))
    - Winget package ([#163](https://github.com/Rookiestyle/KeePassOTP/issues/163))
    - Authy import, wontfix ([#13](https://github.com/Rookiestyle/KeePassOTP/issues/13))

**TwoFactorQRCodeReader (Alex Vallat)**
- Announced 2021-01-09 on the SourceForge forum and requires KeePass 2.47+. It reads a QR code, fills the built-in `TimeOtp-Secret-Base32` or `HmacOtp-*` fields, and leaves generation to KeePass. Distributed through a SourceForge project, with update checks via SourceForgeUpdateChecker. No license or format is stated in the thread — [SF thread](https://sourceforge.net/p/keepass/discussion/329220/thread/041ecdb8cb/)
- keepass.info says it reads a QR code shown on screen or in the clipboard, or an OTP URL — [keepass.info plugins](https://keepass.info/plugins.html)
- In 2024 a user still recommended it as the QR front-end for built-in OTP ([SF thread 2024-02](https://sourceforge.net/p/keepass/discussion/329221/thread/d67d6aabf2/)) and again in 2025 ([SF thread 2025-06](https://sourceforge.net/p/keepass/discussion/329221/thread/763ff4fd2e/))

**OtpKeyProv (Dominik Reichl)**
- A **key provider**, not a TOTP generator. Once a database is protected with it, you must enter one-time passwords from an OATH HOTP (RFC 4226) token to open the database. v2.7 is for KeePass 2.54+. keepass.info points users who want KeePass to generate OTPs to the built-in placeholders and to KeePassOTP, KeeOtp2, KeeTrayTOTP and TwoFactorQRCodeReader instead — [keepass.info plugins](https://keepass.info/plugins.html)

### Inferences
- Storage formats are fragmented across tools:
  - built-in `TimeOtp-*` fields (KeeOtp2, TwoFactorQRCodeReader)
  - `otp` field holding `otpauth://` (KeePassOTP in-entry mode, KeePassXC, K2A)
  - legacy `otp` field holding `key=…` (KeeOtp1)
  - `TOTP Seed` + `TOTP Settings` (KeeTrayTOTP)
  - a hidden embedded database (KeePassOTP separate mode)
  
  Many complaints come from this fragmentation: wrong codes on mobile, the "otp field already exists" crash, migration requests. A new plugin that reads all of them and writes standard formats (built-in fields and/or `otpauth://` in `otp`) addresses a documented pain point.
- Every plugin that registers a global hotkey has hotkey complaints: conflicts, Ctrl+T captured system-wide, keyboard layouts.

### Gaps
- I could not confirm TwoFactorQRCodeReader's license, latest version or distribution format (DLL or PLGX). The SourceForge project page was not fetched.
- I did not read the full Tray TOTP (SourceForge) history.
- The ~45 KeePassOTP issue bodies not listed above were not read; their themes are inferred from titles.

## Q3. Delivery: DLL or PLGX, signing, Windows 11 Smart App Control

### Takeaway
All three GitHub plugins (KeeOtp2, KeeTrayTOTP, KeePassOTP) ship **only as PLGX**. None ships an Authenticode-signed DLL; KeeTrayTOTP's rolling pre-release adds only a `hashes.txt`. With PLGX, KeePass compiles a DLL into the local plugin cache, so that DLL cannot be signed by the author. In 2026, a Windows 11 Smart App Control block of a PLGX plugin (KeePassRPC) was reported, and the only workaround found was turning Smart App Control off.

### Cited Findings
- KeeOtp2 release assets: `KeeOtp2.plgx` only, every version — [releases](https://github.com/tiuub/KeeOtp2/releases). Build instructions use `makeplgx.bat` — [README](https://github.com/tiuub/KeeOtp2)
- KeeTrayTOTP release assets: `KeeTrayTOTP.plgx`. The "latest" pre-release from 2026-05-28 adds `hashes.txt` — [releases](https://github.com/KeeTrayTOTP/KeeTrayTOTP/releases). Users asked for hash-file integrity verification in 2020 ([#187](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/187)) [title]
- KeePassOTP release assets: `KeePassOTP.plgx` only — [releases](https://github.com/Rookiestyle/KeePassOTP/releases). It can be updated through Rookiestyle's EarlyUpdateCheck plugin — [README](https://github.com/Rookiestyle/KeePassOTP)
- A KeeOtp2 load error shows the compiled DLL being loaded from `%LOCALAPPDATA%\KeePass\PluginCache\<random>\KeeOtp2.dll` ([#42](https://github.com/tiuub/KeeOtp2/issues/42)) [body]. PLGX compile failures have also been reported on specific Windows or .NET setups ([#13](https://github.com/tiuub/KeeOtp2/issues/13)) [body]
- Smart App Control. On 2026-01-27 a user reported that Windows 11 Smart App Control blocks the KeePassRPC plugin and that disabling SAC was the only workaround. On 2026-02-02 the developer replied:
  - The DLL is built from the PLGX on the user's system, so it cannot be signed.
  - A per-KeePass-release prebuilt DLL might be possible, but he cannot commit to it.
  - He does not know whether the missing signature is really the cause.
  
  On 2026-02-03 the user said the block stopped after an apparent Windows update — [Kee forum](https://forum.kee.pm/t/keepassrpc-blocked-by-windows-11-smart-app-control-any-solution/5094)
- Microsoft says Smart App Control blocks files that cannot be verified or are not properly signed — [Microsoft Support](https://support.microsoft.com/en-us/topic/smart-app-control-has-blocked-part-of-this-app-0729fff1-48bf-4b25-aa97-632fe55ccca2) (seen as a search summary only)
- An older thread describes a workaround for PLGX blocking: copy the compiled DLL out of `%LocalAppData%\KeePass\PluginCache` into the Plugins folder, and redo it after each release — [Devolutions forum](https://forum.devolutions.net/topics/1748/keepass-plugin-broken-on-version-217) (search summary only)
- Security background: Quarkslab has written about abusing the KeePass plugin cache for post-exploitation — [Quarkslab](https://blog.quarkslab.com/post-exploitation-abusing-the-keepass-plugin-cache.html) (title only, not read)

### Inferences
- A DLL built against the KeePass 2.x API and Authenticode-signed (for example through SignPath Foundation, which the Miftawerk Unlock project already plans to use) would set the plugin apart in distribution. No OTP plugin found ships one. The trade-off is that a plain DLL is less tolerant of KeePass API changes than a PLGX, and the KeeOtp2/KeeTrayTOTP history shows load breakage after KeePass updates.
- Bundling third-party libraries such as ZXing, Otp.NET, NHotkey or Newtonsoft.Json has caused MissingMethod and missing-assembly errors (KeeOtp2 #17 and #56, KeePassOTP #79). A from-scratch implementation with minimal dependencies reduces that risk.

### Gaps
- I found no report specifically about Smart App Control blocking KeeOtp2, KeePassOTP or KeeTrayTOTP. The only documented case is KeePassRPC.
- I did not verify whether any of these authors has ever offered a signed build outside GitHub.

## Q4. Repeated user requests (cross-plugin feature demand)

### Takeaway
The same requests keep coming back across trackers:
- Import from Google Authenticator migration QR codes, in bulk
- Reliable screen and multi-monitor QR capture
- Standard `otpauth://` interop with mobile apps and KeePassXC
- Steam
- HOTP and SHA-256/512
- Non-default digits and periods
- A remaining-seconds display
- Non-conflicting, configurable hotkeys
- A toolbar or context copy button
- Time-offset/NTP correction
- Not exposing every code in the list
- Avoiding needless database modification

### Cited Findings
- **Google Authenticator `otpauth-migration://` import:**
  - KeeOtp2 #53 (2024-03, open) [body], which references KeePassOTP #22
  - KeePassOTP #22 (2020) [body], #90 (2022) [title], #108 bulk import wontfix (2022) [body], #179 (2025) [body]
  - [KeeOtp2 #53](https://github.com/tiuub/KeeOtp2/issues/53); [KeePassOTP #108](https://github.com/Rookiestyle/KeePassOTP/issues/108); [KeePassOTP #179](https://github.com/Rookiestyle/KeePassOTP/issues/179)
- **Authy import:** asked for and declined, wontfix — [KeePassOTP #13](https://github.com/Rookiestyle/KeePassOTP/issues/13) [title]. Authy desktop being discontinued drove users to KeePass in 2024 — [SF thread 2024-02](https://sourceforge.net/p/keepass/discussion/329221/thread/d67d6aabf2/)
- **QR capture:**
  - From screen: KeeOtp2 #37 and #40, KeePassOTP #54 [title], KeeTrayTOTP #382
  - Drag and drop: KeeTrayTOTP #143, KeePassOTP #28 [title]
  - Camera: KeePassOTP #121
  - Some QR codes cannot be read directly, so users resort to online decoders — [SF thread 2024-02](https://sourceforge.net/p/keepass/discussion/329221/thread/d67d6aabf2/)
  - [KeeOtp2 #40](https://github.com/tiuub/KeeOtp2/issues/40); [KeeTrayTOTP #382](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/382)
- **Interop and standard storage:**
  - KeeTrayTOTP #110 asks for Key URI storage (open since 2020)
  - KeeOtp2 #55: KeePassXC/K2A Steam compatibility
  - KeeOtp2 #16: conflict with KeePassOTP's `otp` field
  - SF 2025: K2A showed a different code from KeePassOTP because the entry held several formats
  - [KeeTrayTOTP #110](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/110); [KeeOtp2 #55](https://github.com/tiuub/KeeOtp2/issues/55); [SF 2025-06](https://sourceforge.net/p/keepass/discussion/329221/thread/763ff4fd2e/)
- **Migration between plugins:** KeeTrayTOTP to KeePassOTP (#184, 2026), KeePassXC to KeePassOTP (#177), KeeOtp1 to KeeOtp2 (#4, #7, #11), and KeeTrayTOTP compatibility in KeeOtp2 (PR #58) — [KeePassOTP #184](https://github.com/Rookiestyle/KeePassOTP/issues/184); [KeeOtp2 PR #58](https://github.com/tiuub/KeeOtp2/pull/58)
- **Steam Guard:** KeeOtp2 #20 and #35; KeePassOTP #19 and #21; KeeTrayTOTP's reason for existing. Also Yandex (KeeTrayTOTP #197; KeePassOTP #11, #25, #112), Battle.net (KeeTrayTOTP #3, 22 comments) and Microsoft 365 (KeePassOTP #133) — [KeeOtp2 #35](https://github.com/tiuub/KeeOtp2/issues/35); [KeeTrayTOTP #3](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/3)
- **HOTP:** KeeOtp2 #46 (open since 2022-09) — [link](https://github.com/tiuub/KeeOtp2/issues/46). **Algorithms:** KeeTrayTOTP #207 and #377 (SHA-256/512) — [link](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/207)
- **Custom digits and periods:** 7 digits (KeeOtp2 #25, KeeTrayTOTP #40), 8 digits displayed wrongly (KeeOtp2 #1), non-30 s periods (KeeOtp2 #22), periods of 60–180 s (KeeTrayTOTP #19, #50). Built-in OTP caps length at 8 — [placeholders](https://keepass.info/help/base/placeholders.html)
- **Multiple OTPs per entry:** Huawei needs 2 codes — [KeeOtp2 #51](https://github.com/tiuub/KeeOtp2/issues/51) [body]
- **Remaining seconds or countdown:** KeePassOTP #190 asks for a time-remaining column (2026); KeeTrayTOTP #328 and #192 report a countdown that does not tick — [KeePassOTP #190](https://github.com/Rookiestyle/KeePassOTP/issues/190); [KeeTrayTOTP #328](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues/328)
- **Copy and hotkeys:**
  - Toolbar copy button (KeeOtp2 #65, 2026)
  - Context-menu copy (KeePassOTP #116; KeeTrayTOTP #156)
  - Configurable copy hotkey (KeePassOTP #80)
  - Global auto-type hotkey (KeeTrayTOTP #35; KeeOtp2 #2)
  - Ctrl+T conflicts (KeePassOTP #15 and #32; KeeOtp2 #21 and #26)
  - [KeeOtp2 #65](https://github.com/tiuub/KeeOtp2/issues/65); [KeePassOTP #15](https://github.com/Rookiestyle/KeePassOTP/issues/15)
- **Tray display:** KeeTrayTOTP's core feature, with requests for colors (#176), icons (#140) and a "Show TOTP" item (#186). KeePassOTP added tray copy and auto-type and was asked to make tray entries removable (#175) — [KeeTrayTOTP issues](https://github.com/KeeTrayTOTP/KeeTrayTOTP/issues?q=is%3Aissue)
- **Time correction:** KeeOtp2 #10 (corporate clock locked, 87 s off); KeeTrayTOTP #31, #145 and #231; KeePassOTP #187 (Date-header parse bug, 2026). Wrong codes caused by the system clock: KeeTrayTOTP #311 — [KeeOtp2 #10](https://github.com/tiuub/KeeOtp2/issues/10)
- **Privacy of displayed codes:** KeePassOTP #186 asks to show the code only for the selected entry (wontfix) and #180 asks to show the code without revealing other data — [KeePassOTP #186](https://github.com/Rookiestyle/KeePassOTP/issues/186)
- **No needless database modification or save prompts:** KeePassOTP #130, #166, #174 and #188; KeeOtp2 #31 (modification time) — [KeePassOTP #188](https://github.com/Rookiestyle/KeePassOTP/issues/188)
- **Recovery or backup codes stored alongside the OTP:** KeePassOTP #68, #120 and #144 — [KeePassOTP #68](https://github.com/Rookiestyle/KeePassOTP/issues/68) [title]
- **Export:** KeePassOTP #124 and #136; the Google Authenticator export dialog (#100, #102) — [KeePassOTP #136](https://github.com/Rookiestyle/KeePassOTP/issues/136) [title]
- **Theming and dark mode:** KeeOtp2 #32 (KeeTheme) — [link](https://github.com/tiuub/KeeOtp2/issues/32)
- **Packaging:** Chocolatey freshness (KeeOtp2 #63), Winget (KeePassOTP #163) — [KeePassOTP #163](https://github.com/Rookiestyle/KeePassOTP/issues/163)

### Inferences
- These needs appear in the trackers but are still unmet or declined in at least one major plugin:
  - Google migration bulk import that creates entries (declined in KeePassOTP, open in KeeOtp2)
  - HOTP in KeeOtp2
  - SHA-256/512 in the KeeTrayTOTP UI
  - Selected-row-only code display (declined in KeePassOTP)
  - A signed-DLL distribution (no plugin offers one)
  - Multiple OTPs per entry
- No 2FAS or Aegis import request appeared in the three trackers I scanned. Demand is concentrated on Google Authenticator, with some for Authy, KeePassXC and KeeTrayTOTP.

### Gaps
- I found no explicit requests for Aegis or 2FAS import, or for Yandex beyond the KeePassOTP and KeeTrayTOTP mentions, in the scanned trackers.

## Q5. Community sentiment outside GitHub (Reddit, SourceForge, Stack Exchange)

### Takeaway
Reddit (www and old.reddit, including the JSON endpoints) and SourceForge's own forum search blocked automated access. SourceForge threads found through web search show:
- Users combining the built-in generator with TwoFactorQRCodeReader or KeePassOTP
- Confusion about where OTP data lives and how it syncs to mobile apps
- Friction with KeePassOTP's separate database
- QR codes that will not scan

### Cited Findings
- **2025-06:** a KeePassOTP plus Keepass2Android user got different codes on each device. The entry held several OTP formats (old `TOTP Seed`/`TOTP Settings` alongside the newer fields). Rookiestyle explained that in-entry storage is readable by K2A. It was resolved by deleting the old fields and setting OTP up again — [SF thread](https://sourceforge.net/p/keepass/discussion/329221/thread/763ff4fd2e/)
- **2024-02/03:** Authy desktop users moving to KeePass were pointed to TwoFactorQRCodeReader plus the built-in generator, or to KeePassOTP. Complaints in the thread:
  - KeePassOTP's DB password prompt on every open until auto-open was turned off
  - Mobile apps cannot read KeePassOTP's embedded DB
  - Some QR codes are unreadable and need online decoders
  - [SF thread](https://sourceforge.net/p/keepass/discussion/329221/thread/d67d6aabf2/)
- **2021-01:** the TwoFactorQRCodeReader announcement received praise from Dominik Reichl, who added it to the plugins page — [SF thread](https://sourceforge.net/p/keepass/discussion/329220/thread/041ecdb8cb/)
- **Other SF threads, search titles only, not read:**
  - "Option to save QR-Codes" — [link](https://sourceforge.net/p/keepass/discussion/329220/thread/b0dab19795/)
  - "how to use QR code for TOFP" — [link](https://sourceforge.net/p/keepass/discussion/329220/thread/826463b0ae/)
  - "KeepassOTP and Facebook QR code" — [link](https://sourceforge.net/p/keepass/discussion/329220/thread/2b7c39efc0/)
  - "New plugin: KeePassOTP" — [link](https://sourceforge.net/p/keepass/discussion/329220/thread/5d3d91c0da/)
  - "Implement TOTP in KeePass" — [link](https://sourceforge.net/p/keepass/discussion/329220/thread/4625bdee46/)
- KeePassium's docs say that on desktop KeePass you need the KeePassOTP plugin for TOTP — [KeePassium article](https://keepassium.com/articles/how-to-setup-totp/) (search summary only)

### Inferences
- Users are often unsure whether KeePass, the plugin, or the mobile app generates the code, and which field matters. Clear on-screen indication of the storage format, plus a "normalize / clean up legacy fields" function, would address the confusion seen on SourceForge.

### Gaps
- Reddit r/KeePass and Stack Exchange content was not retrievable (blocked or no results), so there is no Reddit evidence here. One r/KeePass thread about embedding TOTP into the password field is known only by URL (reddit.com/r/KeePass/comments/tzzv25), via a Bitwarden community post.
- I did not read the "New plugin: KeePassOTP" SF thread (long, with 100+ replies possible); it may contain more complaints.
