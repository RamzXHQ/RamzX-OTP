# RamzX OTP

**One-time passwords (TOTP / HOTP) for KeePass 2 – with QR code import straight from your screen.**
Free and open source (MIT). Work in progress.

[![Build](https://github.com/SeeColors/RamzX-otp/actions/workflows/build.yml/badge.svg)](https://github.com/SeeColors/RamzX-otp/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Status: early development](https://img.shields.io/badge/Status-early%20development-orange)

> **Status: early development.** Nothing to download yet.

## Planned for 1.0

- **Scan a QR code from the screen:** click it (or drag a box around it) on any monitor
- Import from the clipboard, an image file or a pasted `otpauth://` link
- Bulk import from Google Authenticator export QR codes
- Migration of old plugin formats (KeeOtp, KeeTrayTOTP, KeePassOTP fields)
- **No lock-in:** codes are stored in KeePass's own OTP fields (`TimeOtp-*` / `HmacOtp-*`),
  so they keep working in KeePass 2 and KeePassXC even without this plugin
- Shipped as a ready DLL (not PLGX), no third-party libraries, no network access, no telemetry

## Done so far

- OTP engine written from the RFC texts: HOTP (RFC 4226), TOTP (RFC 6238) with SHA-1/256/512,
  6–10 digits, any period; Base32 (RFC 4648) with clean-up of pasted secrets
- Automated tests against the official RFC test vectors

## Honest security note

Storing the OTP secret in the same database as the password is convenient, but it is no longer
a real second factor: whoever gets your database and master password gets both.
For accounts that matter most, consider a separate database or a separate authenticator.

## Research behind the design

[Analysis of existing KeePass OTP plugins, user complaints, formats and QR scanning](docs/research/report-keepass-otp-plugins.md)

## License

MIT – see [LICENSE](LICENSE). © 2026 SeeColors iT.
KeePass is a project by Dominik Reichl; RamzX OTP is an independent plugin.
