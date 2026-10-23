# OTP storage formats and interoperability for a KeePass 2 OTP plugin ("Miftawerk OTP")

Research date: 2026-10-23. Code from GPL projects (KeePassXC GPL-2/3, scito/extract_otp_secrets GPL-3.0, Aegis GPL-3.0) is only *described* here, never copied. KeeOtp2 (MIT) and dim13/otpauth (ISC) are permissively licensed. KeePass itself is GPL-2. All field names, parameter names and constants below are interface identifiers needed for interop. They are not code.

## 1. KeePass 2 built-in OTP support (up to 2.61.1)

### Takeaway
HOTP (`{HMACOTP}`) has been built in since KeePass 2.10 (2010) and TOTP (`{TIMEOTP}`) since 2.47 (Jan 2021). Both are stored as plain entry strings named `HmacOtp-*` and `TimeOtp-*`. KeePass 2.51 (May 2022) added a native "OTP Generator Settings" dialog and Copy/Show OTP commands. 2.53 added Ctrl+T / Ctrl+Shift+T, and 2.61 added copy buttons inside the dialog. No KeePass changelog or help page mentions QR-code scanning or `otpauth://` import, so KeePass natively has neither.

### Cited Findings
**Field specification (help/base/placeholders.html, section "One-Time Passwords", marked "KeePass 2.x only"):**
- `{HMACOTP}` implements HOTP per RFC 4226. The secret comes from exactly one of these fields: `HmacOtp-Secret` (UTF-8 bytes of the value), `HmacOtp-Secret-Hex`, `HmacOtp-Secret-Base32`, `HmacOtp-Secret-Base64`. — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- `HmacOtp-Counter` holds the counter in decimal. It defaults to 0, and KeePass increments it automatically every time the placeholder is evaluated. — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- `{TIMEOTP}` implements TOTP per RFC 6238. The secret comes from exactly one of these fields: `TimeOtp-Secret` (UTF-8 bytes), `TimeOtp-Secret-Hex`, `TimeOtp-Secret-Base32` (the help calls this "most common"), `TimeOtp-Secret-Base64`. — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- These TOTP fields are optional:
  - `TimeOtp-Length`: default 6, maximum 8.
  - `TimeOtp-Period`: seconds, default 30.
  - `TimeOtp-Algorithm`: one of `HMAC-SHA-1` (default), `HMAC-SHA-256`, `HMAC-SHA-512`.

  — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- There is no length or algorithm field for HOTP. Only the secret and `HmacOtp-Counter` are documented, so HOTP is effectively fixed at 6 digits / SHA-1. (Inference from the field list.) — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- The help gives an RFC 4226 test example: secret `12345678901234567890` (UTF-8) produces 755224, 287082, 359152, 969429, 338314. This is useful as a unit-test vector. — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- The help states that plugins can add non-standard OTPs (e.g. Steam). The built-in feature does not support Steam. — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- The main window has the commands "Copy HMAC-Based OTP", "Show HMAC-Based OTP", "Copy Time-Based OTP" and "Show Time-Based OTP". The OTP Generator Settings dialog validates input and shows a preview. Users can also edit the strings directly in the entry dialog's Advanced tab. — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- In the Entry dialog, the "Tools" button menu contains "OTP generator settings", which "displays a dialog for conveniently editing the one-time password generator settings of the entry". The help page does not mention QR or `otpauth`. — [KeePass Entry dialog help](https://keepass.info/help/v2/entry.html)

**Version history (exact changelog lines, keepass.info/news; the date is encoded in the URL, e.g. n220506 = 2022-05-06):**

| Version (date) | OTP-related change |
|---|---|
| 2.10 (2010-03-05) | Added `{HMACOTP}` (RFC 4226). Secret = UTF-8 of `HmacOtp-Secret`, counter in `HmacOtp-Counter` — [2.10 notes](https://keepass.info/news/n100305_2.10.html) |
| 2.27 (2014-07-06) | `{HMACOTP}`: added `HmacOtp-Secret-Hex`, `-Base32`, `-Base64` — [2.27 notes](https://keepass.info/news/n140706_2.27.html) |
| 2.35 (2017-01-09) | Evaluating `{HMACOTP}` now updates the entry's last-modification time — [2.35 notes](https://keepass.info/news/n170109_2.35.html) |
| **2.47 (2021-01-09)** | **Added `{TIMEOTP}` placeholder (RFC 6238)** — [2.47 notes](https://keepass.info/news/n210109_2.47.html) |
| 2.48 (2021-05-07) | `{HMACOTP}`/`{TIMEOTP}` automatically add Base32 padding when needed. Improved error handling — [2.48 notes](https://keepass.info/news/n210507_2.48.html) |
| **2.51 (2022-05-06)** | **Added the dialog for editing HOTP/TOTP generator settings ("OTP Generator Settings" commands in the entry dialog and in the main window's "Edit Entry (Quick)" menu). Added entry commands Copy/Show HMAC-Based OTP and Copy/Show Time-Based OTP ("Other Data" menu). `HmacOtp-`/`TimeOtp-` fields are no longer shown in the entry string copy menu** — [2.51 notes](https://keepass.info/news/n220506_2.51.html) |
| 2.52 (2022-09-09) | Bitwarden JSON import converts TOTP settings automatically — [2.52 notes](https://keepass.info/news/n220909_2.52.html) |
| 2.53 (2023-01-09) | `{HMACOTP}`/`{TIMEOTP}` added to the "Perform Auto-Type" menu. Ctrl+T = Copy Time-Based OTP, Ctrl+Shift+T = Show Time-Based OTP — [2.53 notes](https://keepass.info/news/n230109_2.53.html) |
| 2.58 (2025-03-04) | Ctrl+T / Ctrl+Shift+T are ignored if the selected entry has no TOTP parameters — [2.58 notes](https://keepass.info/news/n250304_2.58.html) |
| 2.60 (2025-11-02) | Bitwarden JSON import: a `totp` value consisting only of Base32 characters is treated as a TOTP secret. The Show HOTP/TOTP commands use a larger font — [2.60 notes](https://keepass.info/news/n251102_2.60.html) |
| 2.61 (2026-03-04) | OTP settings dialog: whitespace is stripped automatically when pasting Hex/Base32/Base64 secrets. New buttons copy the current OTP. The dialog can show settings of history entries. `{TIMEOTP}` is reclassified as "non-active" (gets replaced in more contexts). Bugfix: editing OTP settings no longer sets the repeated password to the password — [2.61 notes](https://keepass.info/news/n260304_2.61.html) |
| 2.61.1 | No OTP-related changes (focus/dialog/auto-type fixes only). 2.61.1 is the current download — [2.61 notes, "Changes from 2.61 to 2.61.1"](https://keepass.info/news/n260304_2.61.html); [KeePass download page](https://keepass.info/download.html) |

- A grep of all 2.x release-note pages from 2.08 to 2.61 for "otp", "one-time" and "QR code" returned no QR- or otpauth-related entries. — [KeePass news index](https://keepass.info/news/news_all.html)

### Inferences
- The field set has been stable since 2.47/2.51. A plugin that writes `TimeOtp-*`/`HmacOtp-*` is readable by the KeePass 2.51+ native dialog, Copy/Show commands and Ctrl+T, even if the plugin is uninstalled. This is the strongest no-lock-in property available.
- What a plugin can add over KeePass 2.61.1: QR scanning/import, `otpauth://` paste/export, a live code column or countdown in the entry list, Steam, Google migration import, and bulk migration.
- KeePass increments `HmacOtp-Counter` and touches the entry on every `{HMACOTP}` evaluation. A plugin that generates HOTP must increment the same field so both stay in sync.

### Gaps
- No primary source gives the exact date `TimeOtp-Length/Period/Algorithm` became available. The 2.47 notes mention only the placeholder. They were presumably introduced together with `{TIMEOTP}` in 2.47, but this is unconfirmed.
- No screenshot or primary documentation of the 2.51+ dialog's controls was found (e.g. whether it can paste an `otpauth://` URI). The changelog and help never mention it, so I assume it cannot.
- No official statement was found on how KeePass handles lowercase or space-separated Base32 beyond the 2.48 padding and 2.61 whitespace-on-paste notes.

## 2. KeePassXC: `otp` attribute, legacy fields, Steam, reading KeePass 2 fields

### Takeaway
KeePassXC stores TOTP in a single string attribute `otp` that holds an `otpauth://totp/...` URI (its default). It still reads and writes the legacy pair `TOTP Seed` + `TOTP Settings`, and reads KeeOtp's `key=...&size=...&step=...` query format. Since **2.7.10 (2025-03-02)** it also *reads* KeePass 2 fields, but only `TimeOtp-Secret-Base32` (+ `-Algorithm`, `-Length`, `-Period`). It does not read `-Hex`, `-Base64`, plain `TimeOtp-Secret` or any `HmacOtp-*`, and it never writes `TimeOtp-*`. Steam is expressed as `encoder=steam` in the URI or `S` in the legacy settings.

### Cited Findings
Behavior below is described from KeePassXC source `src/core/Totp.h`, `src/core/Totp.cpp` and `src/core/Entry.cpp` on the `develop` branch, read 2026-10-23 (GPL, described only). The source path is now `src/core/Totp.cpp`. The older `src/totp/totp.cpp` path no longer exists.
- **Attribute names (constants in Totp.h):** `otp`, `TOTP Seed`, `TOTP Settings`. The KeePass 2 names it reads are `TimeOtp-Secret-Base32`, `TimeOtp-Algorithm`, `TimeOtp-Length`, `TimeOtp-Period`. The defaults are step 30, digits 6, SHA-1, and the default storage format is the otpauth URL. Steam digits = 5, Steam short name = `S`. — [KeePassXC Totp.h](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.h)
- **Read priority (Entry::updateTotp):**
  1. If `TOTP Settings` exists, KeePassXC parses it together with `TOTP Seed`.
  2. Otherwise, if `otp` exists, it parses that.
  3. Otherwise, if `TimeOtp-Secret-Base32` exists, it builds settings from the four KeePass 2 fields.
  4. Otherwise the entry has no TOTP.

  Consequence: if an entry has both `otp` and `TimeOtp-*`, **KeePassXC uses `otp`**. — [KeePassXC Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp)
- **Write (Entry::setTotp):** KeePassXC deletes `otp`, `TOTP Seed` and `TOTP Settings`, then writes either `otp` (URI or KeeOtp format, protected) or the legacy `TOTP Seed` (protected) + `TOTP Settings`. It never writes or deletes `TimeOtp-*`, so a KeePassXC edit of an entry with KeePass 2 fields leaves stale `TimeOtp-*` fields behind. — [KeePassXC Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp)
- **otpauth parsing:** KeePassXC reads `secret`, `digits`, `period`, `encoder` and `algorithm` from the query string. It ignores `issuer` and the label for generation, and it does not process `hotp`/`counter` at all (TOTP only). Digits are clamped to 1–10 and step to 1–86400 s. — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp)
- **Algorithm names are case-insensitive:** `SHA256`/`HMAC-SHA-256` and `SHA512`/`HMAC-SHA-512` are recognized, and anything else falls back to SHA-1. So KeePass-style values work in both the URI and `TimeOtp-Algorithm`. — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp)
- **URI it writes:** `otpauth://totp/<title>:<username>?secret=<base32>&period=<n>&digits=<n>&issuer=<title>`. The title falls back to "KeePassXC" and the username to "none". `&encoder=steam` is appended for Steam. `&algorithm=SHA256|SHA512` is appended only when the algorithm isn't SHA-1. — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp)
- **Legacy `TOTP Settings` format:** `<step>;<digits>`, or `<step>;S` for Steam. The secret is in `TOTP Seed`. — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp)
- **KeeOtp compatibility:** If the `otp` value is not an otpauth URI but has a `key` query item, KeePassXC parses `key`, `size` (digits), `step` and `otpHashMode`, and preserves that format when it writes back. — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp)
- **Secrets are always treated as Base32,** after a sanitize step. — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp)
- **Steam encoder:** alphabet `23456789BCDFGHJKMNPQRTVWXY` (26 characters), 5 characters, step 30, flagged as "reverse". The truncated HOTP integer is repeatedly reduced modulo 26, and the least-significant symbol is emitted first (left to right). — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp)
- **KeePassXC changelog milestones:**
  - 2.2.0 (2017-06-23): "Added TOTP support".
  - 2.3.0 (2018-02-27): "Add support for Steam one-time passwords".
  - 2.4.0 (2019-03-19): "TOTP QR Code Generator".
  - 2.5.0 (2019-10-26): "Add support for RFC6238-compliant TOTP hashes" (SHA-256/512).
  - 2.5.2 (2020-01-04): "OPVault: Use 'otp' attribute".
  - 2.7.5 (2023-05-14): "Increase max TOTP step to 24 hours".
  - **2.7.10 (2025-03-02): "Support KeePass2 TOTP settings [#11229]"**.
  - **2.7.12 (2026-03-10): "Support TIMEOTP autotype and entry placeholder [#13117]"**.
  - Latest listed release: 2.8.0-beta1 (2026-09-23).

  — [KeePassXC CHANGELOG.md](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md)

### Inferences
- To be readable by KeePassXC ≥ 2.7.10 through the built-in fields, the secret **must be in `TimeOtp-Secret-Base32`**, not `-Hex`/`-Base64`/UTF-8. Older KeePassXC (< 2.7.10, e.g. LTS distro packages) reads only `otp` / `TOTP Seed`+`TOTP Settings`.
- KeePassXC does not read HOTP in any format. HOTP entries are KeePass-only (and plugin-only).
- Writing both `otp` and `TimeOtp-*` risks divergence. KeePassXC prefers `otp` while KeePass's native dialog edits only `TimeOtp-*`. A dual-write strategy must treat one format as the source of truth and rewrite the other on every change, or warn about the mismatch.

### Gaps
- Not verified whether KeePassXC 2.8.0-beta1 changed the read priority or added `-Hex`/`-Base64` support. Only `develop` was inspected, and it shows Base32 only.
- No KeePassXC user-guide text on the TimeOtp import was fetched. The behavior above comes from source and the changelog.

## 3. otpauth:// Key URI format (Google Authenticator wiki)

### Takeaway
The de facto standard is `otpauth://TYPE/LABEL?PARAMETERS`, where TYPE is `totp` or `hotp` and LABEL is `Issuer:account`. Parameters are `secret` (required, Base32 without padding), `issuer` (strongly recommended), `algorithm` (SHA1/SHA256/SHA512), `digits` (6/8), `counter` (required for hotp) and `period` (default 30). The spec lives on an archived wiki (repo archived 2021-04-06, page last edited 2018-11-26). Real implementations diverge.

### Cited Findings
- Structure: `otpauth://TYPE/LABEL?PARAMETERS`, with TYPE `hotp` or `totp`. LABEL is `accountname` or `issuer:accountname`. The colon may be literal or `%3A`, optional `%20` spaces may precede the account name, and neither part may contain a colon. The wiki recommends using both the label prefix and the `issuer` parameter, which should match. — [Key Uri Format wiki](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)
- `secret`: required. Base32 per RFC 3548, and padding should be omitted. — [Key Uri Format wiki](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)
- `issuer`: strongly recommended, URL-encoded per RFC 3986. Older Google Authenticator versions ignore it and use the label prefix. — [Key Uri Format wiki](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)
- `algorithm`: SHA1 (default) / SHA256 / SHA512. The wiki says Google Authenticator "currently" ignores it. — [Key Uri Format wiki](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)
- `digits`: 6 (default) or 8. The wiki says the Android and BlackBerry apps ignore it. — [Key Uri Format wiki](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)
- `counter`: required for hotp (initial counter). `period`: totp only, default 30. The wiki says Google Authenticator ignores `period`. — [Key Uri Format wiki](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)
- Repository archived by its owner on 2021-04-06. The page was last edited 2018-11-26. — [Key Uri Format wiki](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)
- Known non-standard extensions:
  - `encoder=steam` (KeePassXC and KeeOtp2). — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp); KeeOtp2 `OtpAuthUtils.cs` comment: "inofficial for Steam support" — [KeeOtp2 OtpAuthUtils.cs](https://github.com/tiuub/KeeOtp2/blob/master/KeeOtp2/OtpAuthUtils.cs)
  - KeeOtp2 also accepts `size` as an unofficial alias for `digits` when reading. — [KeeOtp2 OtpAuthUtils.cs](https://github.com/tiuub/KeeOtp2/blob/master/KeeOtp2/OtpAuthUtils.cs)
  - Bitwarden accepts `digits` 1–10 and `period` > 0. — [Bitwarden Integrated Authenticator](https://bitwarden.com/help/integrated-authenticator/)
- KeePassXC ignores `hotp`/`counter` and accepts 1–10 digits. — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp)
- KeePass built-in allows a maximum of 8 digits. — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)

### Inferences (incompatibility checklist for the parser/writer)
- Accept a lowercase scheme/type and lowercase `algorithm` values (e.g. Bitwarden's `sha256`). Accept `HMAC-SHA-256` too, since KeePassXC accepts it.
- Strip spaces and `=` padding and uppercase the secret before Base32 decoding. When writing, emit unpadded uppercase Base32.
- Percent-decode the label. Split on the first `:` (or `%3A`). If `issuer` and the label prefix conflict, prefer `issuer`.
- Reject or warn when `digits` > 8 and the target is KeePass built-in fields (KeePass max 8). Accept up to 10 for the `otp` URI.
- Treat `period` != 30, `digits` != 6 and non-SHA1 algorithms as "may not work in Google Authenticator", per the (dated) wiki.
- `hotp` URIs can be mapped only to `HmacOtp-*` (KeePass) and are not usable in KeePassXC.

### Gaps
- No current (2024–2026) primary Google statement was found on whether today's Google Authenticator still ignores `algorithm`/`digits`/`period`. The wiki's notes date from 2018 or earlier.
- There is no IETF RFC for `otpauth://`. Note the existence of draft efforts, but none were verified here.

## 4. Google Authenticator export (`otpauth-migration://offline?data=`) and other app export formats

### Takeaway
Google Authenticator's "Transfer accounts → Export accounts" produces one or more QR codes. Each encodes `otpauth-migration://offline?data=<Base64 of a protobuf MigrationPayload, percent-encoded>`, and large exports are split into batches. The protobuf has no period field, so period is implicitly 30 s. Aegis has a fully documented JSON vault format (v3 content). 2FAS's `.2fas` JSON is documented only by community examples. Bitwarden stores a `totp` string (Base32, `otpauth://` URI, or `steam://`).

### Cited Findings
**Google Authenticator migration (community reverse-engineered, no official spec):**
- dim13/otpauth (ISC licence, last push 2026-07-30) converts `otpauth-migration://offline?data=...` transfer links into plain otpauth links. It obtains the link via "⋮ → Transfer accounts → Export accounts" and supports batch info (`-info`). — [dim13/otpauth README](https://github.com/dim13/otpauth)
- dim13's proto3 `Payload`:
  - `repeated OtpParameters otp_parameters = 1`, `int32 version = 2`, `int32 batch_size = 3`, `int32 batch_index = 4`, `int32 batch_id = 5`.
  - `OtpParameters`: `bytes secret = 1` (raw bytes, not Base32), `string name = 2`, `string issuer = 3`, `Algorithm algorithm = 4`, `DigitCount digits = 5`, `OtpType type = 6`, `uint64 counter = 7`, `string unique_id = 8`.
  - Enums: Algorithm 0 unspecified, 1 SHA1, 2 SHA256, 3 SHA512, 4 MD5. DigitCount 0 unspecified, 1 = six, 2 = eight. OtpType 0 unspecified, 1 HOTP, 2 TOTP.
  - There is no period field.

  — [dim13/otpauth migration.proto](https://github.com/dim13/otpauth/blob/master/migration/migration.proto)
- scito/extract_otp_secrets (GPL-3.0, actively pushed 2026-10-21) uses an older, smaller proto: the same field numbers 1–7, `digits` as `int32` and only SHA1 in the Algorithm enum. Its comment says the proto was copied from Aegis's `google_auth.proto`. Its README states the `data` parameter is a Base64-encoded proto3 message. — [scito google_auth.proto](https://github.com/scito/extract_otp_secrets/blob/master/src/google_auth.proto); [scito README](https://github.com/scito/extract_otp_secrets)
- The two community protos differ on `digits` (enum 1→6, 2→8 vs. plain int32) and on the algorithm enum range. Both decode the same varint on the wire. The safe interpretation is 1→6, 2→8, 0→6, while also accepting a literal 6 or 8. — [dim13 proto](https://github.com/dim13/otpauth/blob/master/migration/migration.proto) vs. [scito proto](https://github.com/scito/extract_otp_secrets/blob/master/src/google_auth.proto)

**Aegis (documented vault format):**
- The top-level file is `{version: 1, header: {...}, db: ...}`. `db` is either a JSON object (plain export) or a Base64 string encrypted with AES-256-GCM (key slots: raw/password/biometric). — [Aegis vault.md](https://github.com/beemdevelopment/Aegis/blob/master/docs/vault.md)
- The content is `{version: 3, entries: [], groups: []}`.
  - Each entry has `type` (`totp`|`hotp`|`steam`|`motp`|`yandex`), `uuid`, `name`, `issuer`, `note`, `icon*`, `favorite`, `info` and `groups`.
  - `info` contains `secret` (Base32), `algo` (`SHA1`/`SHA256`/`SHA512`), `digits`, `period` (TOTP) or `counter` (HOTP).
  - Steam fixes algo SHA1, period 30, digits 5. MOTP is MD5/10 s/6 digits plus a `pin`. Yandex is SHA256/30 s.

  — [Aegis vault.md](https://github.com/beemdevelopment/Aegis/blob/master/docs/vault.md)

**2FAS:**
- `.2fas` backups are JSON, encrypted by default, with an unencrypted export option. A community sample shows a top-level `services` array. Each item has `name` and `secret`, plus an `otp` object with account, issuer, tokenType, algorithm, period, digits, counter and a link. There is no official schema. — [romanzipp blog](https://romanzipp.com/blog/convert-2fas-backup-to-plain-text-links); [fileinfo .2fas](https://fileinfo.com/extension/2fas)

**Bitwarden:**
- The authenticator key field accepts a plain secret or `steam://<secret>` for Steam Guard. Parameters are set via `otpauth://totp/...` with `algorithm` (sha1/sha256/sha512), `digits` (1–10, default 6) and `period` (> 0, default 30). — [Bitwarden Integrated Authenticator](https://bitwarden.com/help/integrated-authenticator/)
- KeePass's Bitwarden JSON importer converts TOTP settings since 2.52 and accepts bare Base32 `totp` values since 2.60. — [2.52 notes](https://keepass.info/news/n220909_2.52.html); [2.60 notes](https://keepass.info/news/n251102_2.60.html)

### Inferences
- The highest-value import for Google Authenticator users is `otpauth-migration`. It requires a protobuf decoder (or a small hand-written varint parser, since the schema is tiny), Base64 with percent-decoding, a merge of several batch QRs (`batch_id` / `batch_index` / `batch_size`), raw secret bytes re-encoded to Base32, and period = 30.
- An Aegis plain JSON import is straightforward and well documented. Encrypted Aegis vaults need scrypt + AES-GCM, which is probably out of scope for v1.
- Export should be `otpauth://` URIs (text list plus QR), which every app above can import. Writing Google migration QR is optional.

### Gaps
- No official Google documentation exists for `otpauth-migration`. Batch-size limits (how many accounts per QR) were not confirmed by a primary source.
- The 2FAS schema is unofficial. The twofas GitHub repositories were not checked.
- How Google Authenticator encodes a non-30 s period on export (it can't, per the proto) was not tested.

## 5. Steam Guard TOTP variant

### Takeaway
Steam Guard is TOTP with HMAC-SHA1, a 30 s step and 5 output characters. The usual decimal truncation is replaced by base-26 encoding into the alphabet `23456789BCDFGHJKMNPQRTVWXY`, with the least-significant symbol first. No official Valve specification exists. The interchange representations are `otpauth://...&encoder=steam` (KeePassXC, KeeOtp2), `TOTP Settings = 30;S` (KeePassXC legacy), Aegis `type: "steam"` and Bitwarden `steam://<secret>`. KeePass built-in fields cannot express it.

### Cited Findings
- Aegis states that no specification is available for Steam's OTP algorithm and that it is "essentially the same as TOTP" with a different final encoding step. Fixed values: algo SHA1, period 30, digits 5. — [Aegis vault.md](https://github.com/beemdevelopment/Aegis/blob/master/docs/vault.md)
- KeePassXC's Steam encoder uses the alphabet `23456789BCDFGHJKMNPQRTVWXY`, 5 digits, step 30, and a "reverse" flag. The 31-bit truncated value is taken modulo 26 repeatedly, and each remainder indexes the alphabet, filling positions left to right. The URI name is `steam` and the legacy short name is `S`. — [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp); [KeePassXC Totp.h](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.h)
- KeeOtp2 writes `encoder=steam` into URIs for Steam entries and reads the KeeOtp1 `encoder` parameter. — [KeeOtp2 OtpAuthUtils.cs](https://github.com/tiuub/KeeOtp2/blob/master/KeeOtp2/OtpAuthUtils.cs)
- Bitwarden uses a `steam://` prefix on the secret. — [Bitwarden Integrated Authenticator](https://bitwarden.com/help/integrated-authenticator/)
- KeePass built-in: "Plugins can add non-standard OTPs (e.g. Steam)". — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)

### Inferences
- Steam entries must be stored in the `otp` field (otpauth URI with `encoder=steam`). Do not also write `TimeOtp-*`: KeePass would generate a wrong 6-digit decimal code from the same secret.
- Test vectors: no official vectors exist. Cross-check against KeePassXC/Aegis output for a known secret and timestamp (an implementation test, not a copy).

### Gaps
- No primary Valve source. The Steam shared secret is usually extracted from the Steam mobile app or SDA `maFile` as Base64 (`shared_secret`). This is commonly known but was not sourced here, so the plugin should accept Base64 for Steam input. Unverified.

## 6. Recommendation: what to read, what to write, primary storage

### Takeaway
**Primary storage should be KeePass 2 built-in fields:** `TimeOtp-Secret-Base32` + `TimeOtp-Length/Period/Algorithm` for TOTP, and `HmacOtp-Secret-Base32` + `HmacOtp-Counter` for HOTP. This makes entries work in KeePass 2.51+ with no plugin, and in KeePassXC ≥ 2.7.10 for TOTP. Use the `otp` attribute (otpauth URI) only for what built-in fields cannot express (Steam, >8 digits), or as an opt-in mirror for older KeePassXC and mobile clients. Read every format listed below.

### Cited Findings
- Built-in fields and allowed values. — [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html)
- KeePass native UI for these fields since 2.51, Ctrl+T since 2.53. — [2.51 notes](https://keepass.info/news/n220506_2.51.html); [2.53 notes](https://keepass.info/news/n230109_2.53.html)
- KeePassXC reads `TimeOtp-Secret-Base32` (only Base32) since 2.7.10, at lower priority than `otp` and the legacy fields. — [KeePassXC Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp); [KeePassXC CHANGELOG](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md)
- KeeOtp2 (MIT, latest v1.6.0 published 2025-03-21, not archived) supports two storage targets:
  - the string field `otp` in KeeOtp1 query format, with parameters `key`, `type`, `step`, `size`, `encoding`, `counter`, `otpHashMode`, `encoder`;
  - KeePass built-in `TimeOtp-*`/`HmacOtp-*` with all four secret encodings (`-Secret`, `-Secret-Hex`, `-Secret-Base32`, `-Secret-Base64`) and `HMAC-SHA-1/256/512`.

  It offers "Migrate to built-in", recommends `{TIMEOTP}`, and treats `{TOTP}` as deprecated. — [KeeOtp2 README](https://github.com/tiuub/KeeOtp2); [KeeOtp2 OtpAuthUtils.cs](https://github.com/tiuub/KeeOtp2/blob/master/KeeOtp2/OtpAuthUtils.cs); [GitHub API release data](https://api.github.com/repos/tiuub/KeeOtp2/releases/latest)

### Compatibility matrix (R = reads, W = writes, — = no)

| Storage format (entry strings) | KeePass 2.61.1 built-in | KeePassXC 2.7.10+ | KeePassXC < 2.7.10 | KeeOtp2 1.6.0 | Miftawerk OTP (proposed) |
|---|---|---|---|---|---|
| `TimeOtp-Secret-Base32` + Length/Period/Algorithm | R/W (2.47+, dialog 2.51+) | R only | — | R/W | **R/W (primary)** |
| `TimeOtp-Secret-Hex` / `-Base64` / `TimeOtp-Secret` (UTF-8) | R/W | — | — | R/W | R. W only when the user picks it (warn: not KeePassXC-readable) |
| `HmacOtp-Secret*` + `HmacOtp-Counter` | R/W (2.10+, Base32/Hex/Base64 2.27+) | — | — | R/W | R/W (primary for HOTP) |
| `otp` = `otpauth://totp/...` | — | R/W (default) | R/W | R/W | R. W for Steam/>8 digits, optional mirror |
| `otp` = `otpauth://...&encoder=steam` | — | R/W | R/W (2.3.0+) | R/W | R/W (only Steam path) |
| `otp` = `otpauth://hotp/...` | — | — (ignored) | — | R/W | R. Convert to `HmacOtp-*` |
| `otp` = KeeOtp1 `key=..&size=..&step=..` | — | R/W (keeps format) | R/W | R/W | R. Offer migration |
| `TOTP Seed` + `TOTP Settings` (`step;digits` / `step;S`) | — | R/W | R/W | — (not found in source) | R. Offer migration |
| Placeholder `{TIMEOTP}` | yes (2.47+) | yes (2.7.12+) | — | yes | must not override the built-in one |
| Placeholder `{HMACOTP}` | yes (2.10+) | — | — | — (not verified) | built-in handles it |
| Import: `otpauth-migration://` (Google) | — | — (not found) | — | — (not found) | R (recommended) |
| Import: Aegis JSON (plain) | — | — (not verified) | — | — | R (nice-to-have) |
| Import: Bitwarden JSON `totp` | R (importer, 2.52/2.60) | — (not verified) | — | — | not needed (KeePass does it) |
| QR scan / QR display | — | QR display (2.4.0+) | QR display | QR scan from screen + display | R (scan) / W (display) |

Sources for the matrix: [KeePass Placeholders help](https://keepass.info/help/base/placeholders.html), KeePass release notes ([2.10](https://keepass.info/news/n100305_2.10.html), [2.27](https://keepass.info/news/n140706_2.27.html), [2.47](https://keepass.info/news/n210109_2.47.html), [2.51](https://keepass.info/news/n220506_2.51.html), [2.52](https://keepass.info/news/n220909_2.52.html), [2.60](https://keepass.info/news/n251102_2.60.html)), [KeePassXC Totp.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Totp.cpp), [KeePassXC Entry.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Entry.cpp), [KeePassXC CHANGELOG](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md), [KeeOtp2 OtpAuthUtils.cs](https://github.com/tiuub/KeeOtp2/blob/master/KeeOtp2/OtpAuthUtils.cs), [KeeOtp2 strings (Scan QR Code)](https://github.com/tiuub/KeeOtp2/blob/master/KeeOtp2/KeeOtp2Statics.cs).

### Inferences (recommended design rules)
1. **Write TOTP/HOTP as built-in fields, with the secret in `-Base32`**, normalized to uppercase and unpadded. KeePass 2.48+ auto-pads, and KeePassXC sanitizes. Write `TimeOtp-Length/Period/Algorithm` only when they differ from the defaults, or always (both are harmless). Use the exact algorithm strings `HMAC-SHA-1/256/512`. This gives zero lock-in: uninstalling the plugin leaves KeePass's native Copy/Show/Ctrl+T/`{TIMEOTP}` working, and KeePassXC ≥ 2.7.10 reads the same entry.
2. **Do not register a `{TIMEOTP}`/`{HMACOTP}` handler.** KeePass owns them. If a new placeholder is wanted (e.g. for Steam), use a distinct name, and document `{TIMEOTP}` as the portable one.
3. **Steam and unusual parameters (> 8 digits)** go to `otp` = otpauth URI with `encoder=steam`, matching what KeePassXC and KeeOtp2 write. Never add `TimeOtp-*` to such entries.
4. **Optional "KeePassXC/mobile mirror" setting (default off):** also write `otp` = otpauth URI. Because KeePassXC prefers `otp` over `TimeOtp-*`, the plugin must regenerate `otp` whenever built-in fields change. It should also detect a mismatch on open: built-in fields edited by the native KeePass dialog leave `otp` stale. Default-off avoids that divergence.
5. **Read everything:** built-in (all four encodings), `otp` (otpauth totp/hotp, KeeOtp1 query), `TOTP Seed`/`TOTP Settings` (including `S`), plus imports of `otpauth://` text/QR, `otpauth-migration://` (multi-batch) and Aegis plain JSON. Detection order should mirror reality: if both built-in and `otp` exist and disagree, show a warning and let the user choose.
6. **One-click migration** (per entry and bulk): from KeeOtp1/KeeOtp2 `otp`, KeePassXC legacy, or `otp` URIs to built-in fields, keeping the old fields in entry history (KeePass history), not silently deleting them.
7. **Export:** a per-entry otpauth URI and QR (for moving to phone apps) and a bulk otpauth URI list. All of the listed apps (Aegis, 2FAS, Bitwarden, KeePassXC, Google Authenticator for basic params) accept otpauth URIs.
8. **Validation guardrails:**
   - Digits 6–8 for built-in (KeePass max 8).
   - Period ≥ 1. Warn above 86400, which is KeePassXC's clamp.
   - Warn that HOTP is not supported in KeePassXC.
   - Warn that Hex/Base64/UTF-8 secrets are not readable by KeePassXC.
   - Use the RFC 4226 test vector from the KeePass help in unit tests.

### Gaps
- Keepass2Android, KeePassDX and Strongbox read/write behavior for `TimeOtp-*` vs `otp` was not verified. The Keepass2Android docs page moved to the wiki and was not fetched: [Keepass2Android docs stub](https://github.com/PhilippC/keepass2android/blob/master/docs/Generating-TOTPs.md). This matters for deciding whether the `otp` mirror should default to on. Recommend a follow-up check.
- Whether KeeOtp2 reads KeePassXC's `TOTP Seed`/`TOTP Settings` was not confirmed. Those names were not seen in the extracted constants.
- KeeTrayTOTP storage format (historically `TOTP Seed`/`TOTP Settings`, which KeePassXC's legacy format is believed to mirror) was not researched with a primary source.
