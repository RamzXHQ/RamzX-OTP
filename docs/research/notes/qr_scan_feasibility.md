# QR scan "under the mouse cursor" for a KeePass 2 OTP plugin: feasibility, decoder effort, alternative import paths

Research date: 2026-10-23. Target: KeePass 2.x plugin, C#, .NET Framework 4.8, Windows 10/11, MIT, own code preferred.
Note on local evidence: some findings below come from inspecting the KeePass 2.60 install on the researcher's machine (marked "local observation"). They are not web sources and should be described as such in the report.

## 1. Screen capture from .NET Framework 4.8 (CopyFromScreen / BitBlt / Windows.Graphics.Capture), DPI, multi-monitor, protected content, permissions

### Takeaway
GDI capture (`Graphics.CopyFromScreen`, which is a BitBlt) is available in .NET Framework 4.8 with no extra dependency and no consent prompt, and it covers a small region around the cursor. The main technical risk is DPI: KeePass runs **system DPI aware**, so on a second monitor with a different scale factor the cursor and screen coordinates are virtualized. Switching the calling thread to Per-Monitor V2 for the duration of the capture (`SetThreadDpiAwarenessContext`) avoids that. Windows.Graphics.Capture is heavier (WinRT/D3D11 interop, picker or interop item creation, yellow border), and this scenario does not need it.

### Cited Findings
- `Graphics.CopyFromScreen` "Performs a bit-block transfer of color data, corresponding to a rectangle of pixels, from the screen to the drawing surface of the Graphics". It is available in .NET Framework 2.0 through 4.8.1 and throws `Win32Exception` if the operation fails. The docs say nothing about DPI. — [Microsoft Learn: Graphics.CopyFromScreen](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen?view=netframework-4.8)
- DPI modes: under Unaware, "All displays are 96 DPI". Under System, all displays have the DPI of the primary display at sign-in. PMv1 and PMv2 apps see "all displays in physical pixels"; PMv2 means "The application seeing the raw pixels of each display". — [Microsoft Learn: High DPI Desktop Application Development](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
- Virtualization: "when a System DPI-aware thread is interacting with a display at a different DPI than was in use when the current user's session was started, Windows will DPI-scale some API calls into the coordinate space..." Microsoft also says "it can be difficult to know which API calls can return virtualized values ... this information is not currently sufficiently documented by Microsoft." — [Microsoft Learn: High DPI Desktop Application Development](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
- Since Windows 10 1607, DPI awareness can be set per thread or per top-level window with `SetThreadDpiAwarenessContext`. Microsoft says to "restore the old context when you're done to avoid causing incorrect behavior elsewhere in your application". — [Microsoft Learn: High DPI Desktop Application Development](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
- PMv2 requires Windows 10 1703 (Creators Update) or later. The `dpiAwareness` manifest element arrived in 1607. — [Microsoft Learn: Setting the default DPI awareness for a process](https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process)
- Local observation: the KeePass 2.60 `KeePass.exe` manifest has no `dpiAware`/`dpiAwareness` element. The binary references `SetProcessDPIAware` and `SetProcessDpiAwareness`, so KeePass sets awareness at runtime. `KeePass.exe.config` contains `EnableWindowsFormsHighDpiAutoResizing=true`. Querying the running KeePass 2.60 process with `GetProcessDpiAwareness` returned **1 = PROCESS_SYSTEM_DPI_AWARE**. — local inspection of `C:\Program Files\KeePass Password Safe 2\` (no URL)
- KeePass 2.28 release notes list "enhanced high DPI support". — [KeePass 2.28 news](https://keepass.info/news/n141008_2.28.html)
- Protected content: `SetWindowDisplayAffinity` with `WDA_MONITOR` means "Everywhere else, the window appears with no content". `WDA_EXCLUDEFROMCAPTURE` (Windows 10 2004+) means "the window does not appear at all" in captures. Microsoft says this is "unlike a security feature or an implementation of DRM" and works only under DWM composition. — [Microsoft Learn: SetWindowDisplayAffinity](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity)
- Windows.Graphics.Capture: "developers invoke secure system UI for end users to pick the display or application window to be captured, and a yellow notification border is drawn by the system around the actively captured item." It uses a `Direct3D11CaptureFramePool` (D3D11 device) and frames arrive asynchronously. On HDR systems the pixel format may differ from BGRA8. — [Microsoft Learn: Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)
- Removing the WGC border requires `GraphicsCaptureAccess.RequestAccessAsync(Borderless)`, which shows a consent dialog, plus the `graphicsCaptureWithoutBorder` package-manifest capability. — [Microsoft Learn: GraphicsCaptureAccessKind](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscaptureaccesskind?view=winrt-26100); [Microsoft Learn: GraphicsCaptureSession.IsBorderRequired](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturesession.isborderrequired)
- A third-party guide says the Windows privacy page "Let Windows apps take screenshots of various windows or displays" applies to packaged apps, and that desktop programs may not appear in that list. — [macmyths guide](https://macmyths.com/how-to-control-screenshot-and-screen-recording-permissions-in-windows-11/) (secondary source, low authority)

### Inferences
- Recommended capture path: on hotkey or click, call `SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)`, then `GetCursorPos` (physical pixels), then BitBlt/CopyFromScreen a fixed physical-pixel square (for example 600x600, clamped to the monitor from `MonitorFromPoint`/`GetMonitorInfo`), then restore the previous context. This keeps cursor and capture coordinates in one physical space on mixed-DPI setups. Without the switch, a system-aware KeePass on a 150% secondary monitor would receive virtualized coordinates and could capture the wrong area. Microsoft does not document exactly which APIs virtualize, so this must be tested on a real mixed-DPI rig.
- The function needs a P/Invoke with an OS-version guard. It exists only on Windows 10 1607+, which is fine for the Windows 10/11 target.
- Browser 2FA pages are ordinary DWM-composed content and are not normally display-affinity protected. Black or missing captures should be rare, but they are possible for some secure apps and DRM video. The UI needs an "unable to capture" message.
- GDI capture needs no user consent and shows no yellow border. Microsoft documents both only for WGC, so this is an inference from their absence for GDI (see Gaps).
- Windows.Graphics.Capture from net48 would need WinRT projections (Microsoft.Windows.SDK.Contracts or hand-written COM interop) and D3D11 interop. That is substantial code or dependencies for no benefit in a single-snapshot use case.

### Gaps
- Found no Microsoft statement on whether GDI BitBlt capture triggers any Windows 11 privacy indicator or appears in privacy settings. I believe it does not, but did not verify this with a source.
- Did not find KeePass developer documentation describing its DPI mode. The "system aware" finding is a local runtime measurement of 2.60 only.
- Did not verify whether hardware-DRM video (PlayReady SL3000) shows as black under BitBlt on all GPUs. This is not relevant for QR codes in web pages.

## 2. UX patterns and what existing tools do

### Takeaway
Two patterns exist. (a) "Snip" style: the user drags a rectangle over a dimmed or frozen screen. The Authenticator browser extension does this, and the Win+Shift+S then clipboard workaround works the same way. (b) Automatic full-screen or region scan: 1Password-style "scan from screen". Vendors report the automatic variant sometimes fails to find the code. A hover-plus-hotkey design is a variant of (b) restricted to a window around the cursor, so it should be paired with a snip fallback.

### Cited Findings
- 1Password: users reported "scan QR code from screen" doing nothing. A 1Password employee said the scanner "sometimes is unable to find the edge of the QR code" and suggested Win+Shift+S, then "scan from clipboard". — [1Password community](https://1password.community/discussion/comment/516633)
- Devolutions (RDM): a "scan QR code from my screen" failure in which the main window minimizes for a second and then reappears without scanning. — [Devolutions forum](https://forum.devolutions.net/topics/31800/one-time-password--scan-qr-code-from-my-screen-does-not-work)
- Authenticator browser extension (authenticator.cc): press the scan icon, then select the page area containing the QR code. Reviewers describe "click and drag a box around the qr code". — [UZH math wiki: In browser authenticator](https://wiki.math.uzh.ch/public/MFA/In_browser_authenticator); [Mozilla add-on reviews](https://addons.mozilla.org/sv-SE/firefox/addon/auth-helper/reviews/2193739/)
- A UI Vision forum note says that browser zoom changes often break image recognition, an example of how sensitive scaling is. — [UI Vision forum](https://forum.ui.vision/t/how-to-scan-qr-code-using-chrome-s-extension/14653)
- KeeOtp2 (MIT): the README lists *showing* a QR code and copying the `otpauth://` URI for sharing, plus ZXing.Net as a dependency (Apache-2.0). It does **not** list scanning or importing a QR from screen, file or clipboard, or Google Authenticator migration import. — [KeeOtp2 README](https://raw.githubusercontent.com/tiuub/KeeOtp2/master/README.md); [KeeOtp2 GitHub](https://github.com/tiuub/KeeOtp2)
- Other desktop screen-QR tools build on ZXing: OSQRS "takes a partial screenshot and parses the QR-code ... uses ZXing.Net". barcodrod.io decodes from Snipping Tool, image file, clipboard or webcam using ZXing.Net. — [OSQRS GitHub](https://github.com/itmgithub/osqrs); search summary of [barcodrod.io (Microsoft Store)](https://apps.microsoft.com/detail/9phmxnx36szz?hl=en-US)

### Inferences
- A proposed flow that addresses the reported failure modes: hotkey → capture about 600x600 physical px around the cursor → try decode. On failure, open a fullscreen frozen-screenshot overlay where the user drags a rectangle (snip) and decode that. Always keep "Paste image from clipboard" as a third route, since the 1Password workaround shows users already rely on Win+Shift+S.
- The hover approach helps when the page has several QR codes or images: the region near the cursor narrows the search. It still needs a decoder that locates the QR code within the region (finder-pattern search). A "decode exactly the clicked pixel grid" shortcut is not enough.
- With a global hotkey, the KeePass window is not in the way. With a "click inside KeePass" UX, KeePass must minimize or hide first, which adds timing issues like the Devolutions report. The hotkey is cleaner.

### Gaps
- Could not confirm from primary sources whether ShareX uses ZXing.Net for its "QR code (decode from screen)" tool. The GitHub file path guessed was 404, and the search produced no confirmation.
- 2FAS desktop/browser extension scan UX: not researched (search budget).
- Could not confirm the current KeeOtp2 release version or date from the README.

## 3. Writing an own QR decoder (no third-party code): components, size, references, simplifications for screen QR codes

### Takeaway
A full decoder needs: binarization, finder-pattern detection, module-size and grid estimation, alignment patterns (v2+), format info (BCH 15,5) and version info (BCH 18,6, v7+), unmasking, zigzag codeword extraction, de-interleaving of blocks, Reed–Solomon decoding over GF(256) (syndromes, Berlekamp–Massey, Chien or brute-force search, Forney), and mode parsing (numeric, alphanumeric, byte, ECI, optionally kanji). Screen QR codes are axis-aligned, sharp and unrotated with integer or near-integer module size, which removes perspective transform and most of the robustness work. A screen-only decoder is realistic at roughly 1,000–1,800 lines of C# plus tests (inference). Free references cover encoding, RS decoding and format/version BCH. ISO/IEC 18004 is not strictly required, but test vectors are needed.

### Cited Findings
- The Thonky QR tutorial is "designed to teach programmers how to encode information in a QR code". It covers data encoding, error correction coding, structure, module placement, masking and format/version information, with appendices: capacity tables, EC codewords/block info, GF(256) log/antilog table, generator polynomials, mask patterns, format/version string tables, alphanumeric table, alignment pattern locations. It does not cover decoding. — [Thonky QR Code Tutorial](https://www.thonky.com/qr-code-tutorial/)
- Nayuki's "Creating a QR Code step by step" walks through segments/modes, version/capacity, padding, RS ECC and interleaving (the RS math is omitted), function patterns, zigzag placement, masking and penalty scoring. It is encoding-only and links to external decoding resources. — [Nayuki: Creating a QR Code step by step](https://www.nayuki.io/page/creating-a-qr-code-step-by-step)
- Wikiversity "Reed–Solomon codes for coders" uses QR codes as its running example. It covers unmasking and format info, BCH decoding of format info (with Python code), syndrome calculation, Berlekamp–Massey for the error locator, Chien search (mentioned, brute force used instead), Forney for error values, and erasures. — [Wikiversity: Reed–Solomon codes for coders](https://en.wikiversity.org/wiki/Reed%E2%80%93Solomon_codes_for_coders)
- quirc, a C QR decoder library, describes itself as "small and easily embeddable" with no dependencies beyond standard C and a footprint of one byte per image pixel plus a few kB per decoder. It shows the decoder can be compact. No line count was found. — [quirc README (Debian sources)](https://sources.debian.org/src/quirc/1.2-1/README.md)
- The 1Password scanner's reported failure ("unable to find the edge of the QR code") is an example of detection-stage fragility in production decoders. — [1Password community](https://1password.community/discussion/comment/516633)

### Inferences
- **What screen-QR assumptions allow** (inference): no perspective or rotation, so the grid is found from the three finder patterns by axis-aligned module-size estimation. A sampler can read the center of each module, and alignment patterns serve only as a sanity check. Global or Otsu thresholding on grayscale is usually enough (high contrast, no lighting gradient). The risks are browser zoom and DPI scaling producing non-integer module sizes (for example 3.75 px per module at 125%) and antialiased edges, so sampling must use sub-pixel module centers derived from finder-pattern centers, not integer stepping. Dark mode or inverted QR codes (light on dark) exist on some sites, so the binarizer should try both polarities.
- **otpauth URIs are small**: a typical `otpauth://totp/Issuer:user@example.com?secret=<32 base32>&issuer=Issuer` is about 80–150 bytes, so versions 4–10 in byte mode. Supporting v1–40 costs almost nothing extra (table data), but testing should focus on v2–15. Google Authenticator migration QR codes are larger (multiple accounts in base64 protobuf) and can reach high versions, so v7+ version-info decoding is needed.
- **Rough C# size estimate** (inference, no source): tables (capacity, EC blocks, alignment positions for v1–40) about 250 lines; GF(256) arithmetic and RS decoder about 250; format/version BCH decoding about 80; binarizer and finder-pattern search about 300–450; grid sampling plus function-pattern mask about 200; unmask, zigzag, de-interleave about 150; bitstream parser (numeric/alnum/byte/ECI/kanji-reject) about 150. Total about 1,400–1,750 lines including error handling, plus a test corpus. Effort for an experienced developer (inference): about 5–10 working days to a robust screen-only decoder with tests. Most risk sits in the detector, not in RS.
- **Test strategy without third-party runtime code**: generate test QR codes with a self-written encoder or an existing generator used only as a build-time or test-time tool (not shipped). Render them at module sizes 1–8 px, fractional scales (1.25, 1.5, 1.75), with antialiasing, inverted colors, quiet-zone violations, and placed among other page content. Compare against known payloads.
- **ISO/IEC 18004**: the free sources above cover all the tables and algorithms needed. The paid standard would mainly help with edge cases (ECI details, structured append, Micro QR). For otpauth payloads, ECI and structured append can be rejected or ignored, so the paid standard is not strictly needed. This is an inference; no source compared coverage.
- Security: the decoded string is untrusted input. Strict parsing of `otpauth://` and `otpauth-migration://`, length limits, and refusing unknown schemes are needed.

### Gaps
- No authoritative line counts for quirc, jsQR or ZXing's QR decoder were retrieved, so the C# size estimate is an inference.
- License of the Wikiversity article not confirmed in the fetched text. Wikiversity content is typically CC BY-SA, which matters only if code is copied. Concepts are free to reimplement.
- Did not obtain the ISO/IEC 18004:2024 price or table of contents.

## 4. Built-in Windows APIs that decode QR from a still image

### Takeaway
No built-in Windows or WinRT API for decoding a QR code from a still bitmap turned up in desktop-usable form. `Windows.Devices.PointOfService` barcode scanning targets scanner devices and camera streams, and `Windows.Media.Ocr` is text-only. Desktop QR-from-image tools all use ZXing.Net or commercial SDKs.

### Cited Findings
- A search for a built-in WinRT still-image barcode API returned only third-party options: Dynamsoft Barcode Reader (commercial), DataSymbol.BarcodeReader (commercial WinRT component), and barcodrod.io (which uses ZXing.Net). — [Dynamsoft UWP article](https://www.dynamsoft.com/codepool/uwp-barcode-reader-winrt-javascript.html); [DataSymbol.BarcodeReader NuGet](https://feed.nuget.org/packages/DataSymbol.BarcodeReader); [barcodrod.io Store page](https://apps.microsoft.com/detail/9phmxnx36szz?hl=en-US)

### Inferences
- Even if a WinRT API existed, calling it from net48 needs WinRT projection support (Microsoft.Windows.SDK.Contracts NuGet), which adds dependencies and version coupling. That conflicts with the "own code" goal.

### Gaps
- The Microsoft Learn page on the camera barcode scanner returned 404 at the guessed URL. I could not confirm from Microsoft whether the Windows 10 1803+ software camera barcode decoder can accept still images. Based on prior knowledge (unverified this session), it works only through the `BarcodeScanner` device model with a camera, not on arbitrary bitmaps.

## 5. ZXing.Net: license, size, net48 support, release cadence, implications for "own code" and Smart App Control

### Takeaway
ZXing.Net 0.16.11 (2025-10-25) is Apache-2.0, ships an explicit net48 build with no dependencies, and is widely used (54.1M downloads). It is the low-effort option, but it adds a third-party DLL, Apache-2.0 attribution duties, and (if not merged) an extra unsigned or third-party-signed binary next to the plugin.

### Cited Findings
- Latest ZXing.Net is 0.16.11, released 10/25/2025. Earlier: 0.16.10 (1/26/2025), 0.16.9 (2/23/2023), 0.16.8 (2/6/2022). License Apache-2.0. The package includes explicit net48, net47 and net461 builds, has no dependencies for .NET Framework targets, is 9.96 MB as a nupkg (all targets), and has 54.1M total downloads. — [NuGet: ZXing.Net](https://www.nuget.org/packages/ZXing.Net)
- KeeOtp2 (MIT) already ships ZXing.Net as a dependency, so the Apache-2.0 + MIT combination has precedent in the KeePass plugin ecosystem. — [KeeOtp2 README](https://raw.githubusercontent.com/tiuub/KeeOtp2/master/README.md)
- Apache-2.0 section 4 requires redistributions to include a copy of the license and to keep attribution/NOTICE notices. — [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)

### Inferences
- The net48 `zxing.dll` alone is far smaller than the 9.96 MB nupkg (which bundles many targets). The exact DLL size was not measured; see Gaps.
- To keep a single plugin file: ILMerge/ILRepack or embedding the DLL as a resource with an `AssemblyResolve` loader. Either way, the shipped binary contains third-party code, so it is no longer "100% own code", and the MIT release must include the Apache-2.0 license text.
- Smart App Control / code signing: a merged single assembly signed under the project's own signing (for example SignPath Foundation, which this project already references in its README) avoids an extra separately-signed or unsigned DLL. Whether SAC blocks an unsigned managed DLL loaded by an already-trusted KeePass.exe was not researched here (see Gaps). Other notes in this research set may cover it.
- Using only ZXing's QR decoder subset (by copying source) is possible under Apache-2.0 with attribution, but then the project maintains forked third-party code. This is neither "own code" nor a clean dependency.

### Gaps
- No measurement of net48 `zxing.dll` size, and the ZXing.Net GitHub repo activity was not checked beyond NuGet release dates.
- Did not research how Smart App Control treats unsigned managed DLLs loaded into a signed .NET Framework host. No source collected.

## 6. Alternative import paths (clipboard image, screenshot file, pasted otpauth text, Google Authenticator migration QR)

### Takeaway
All alternative paths reuse the same decoder and parser. Clipboard image import (fed by Win+Shift+S) is the fallback other vendors already recommend. Pasted `otpauth://` text needs no decoder. Google Authenticator export QR codes use `otpauth-migration://offline?data=<base64 protobuf>` with a community-reverse-engineered schema. A hand-written protobuf varint/length-delimited reader is small, so no protobuf dependency is needed.

### Cited Findings
- 1Password's support workaround for failed screen scans is Win+Shift+S, then "scan from clipboard". — [1Password community](https://1password.community/discussion/comment/516633)
- Google Authenticator export QR codes contain `otpauth-migration://offline?data=...`, where `data` is a base64-encoded proto3 message. — [extract_otp_secret_keys README](https://git.blob42.xyz/Archives/extract_otp_secret_keys/raw/commit/10fefacd2d85c08d6385e4f64985e46af2d0aff1/README.md)
- Schema (community-documented): payload with repeated `otp_parameters`, `version`, `batch_size`, `batch_index`, `batch_id`. Each entry has secret (bytes), name, issuer, algorithm (SHA1/SHA256/SHA512/MD5), digits (6/8), type (HOTP/TOTP), counter. Google has published no official spec. — [DJStompZone/otp_export](https://github.com/DJStompZone/otp_export)
- Aegis (an open-source Android authenticator) added import of Google Authenticator export QR codes using generated protobuf-lite classes. — [Aegis commit](https://git.morozoff.pro/aiden/Aegis/commit/56bde0e19b51568a7050f6cb56085a1bb38c5a9e)
- Large exports are split across multiple QR codes (`batch_size` / `batch_index` fields). — [DJStompZone/otp_export](https://github.com/DJStompZone/otp_export)

### Inferences
- Effort (inference): clipboard image (`Clipboard.GetImage()` / `ContainsImage`) about 0.5 day. Open image file (`OpenFileDialog` + `Bitmap`) about 0.5 day. Paste `otpauth://` text plus a strict parser about 1 day including tests. Migration parser (base64url handling, varint/length-delimited protobuf reader, about 150–250 lines) plus a multi-account preview dialog with per-account entry creation: about 2–3 days.
- Migration secrets arrive as raw bytes and must be Base32-encoded for KeePass's built-in `TimeOtp-Secret-Base32` field or stored as `TimeOtp-Secret-Hex`/`-Base64`. The KeePass built-in OTP field names should be checked in other research notes.
- The file-open path could pick up a screenshot from disk that the user should then delete. The UI could remind the user, but the plugin must never delete user files itself.

### Gaps
- Did not verify whether newer Google Authenticator versions (2024–2026) changed the migration format, for example by adding encryption or new fields. The sources found are community and older.

## 7. Privacy and security of a capture feature

### Takeaway
Implement capture only on an explicit user action (hotkey or menu). Keep pixels in memory only, decode in-process with no network, and dispose and zero buffers immediately. GDI capture leaves no OS-level trace, so the plugin's own discipline is the only control.

### Cited Findings
- WGC shows a yellow border and requires picker-based consent, while GDI capture APIs document no such consent mechanism. — [Microsoft Learn: Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture); [Microsoft Learn: Graphics.CopyFromScreen](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen?view=netframework-4.8)
- `WDA_EXCLUDEFROMCAPTURE` lets an app hide its own window from capture. It is not a security guarantee. — [Microsoft Learn: SetWindowDisplayAffinity](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity)
- Users note that a QR shown once on a 2FA setup page cannot be retrieved later, so the import must be reliable or the secret must be shown as text. — [UZH math wiki](https://wiki.math.uzh.ch/public/MFA/In_browser_authenticator)

### Inferences
- Concrete rules: (1) capture only after hotkey or menu click, never on a timer; (2) capture the smallest region possible (around the cursor or the user's snip); (3) never write bitmaps to disk, temp or the clipboard; (4) after decoding, lock the bitmap, `Array.Clear` the grayscale/binary buffers, and `Dispose` the `Bitmap`/`Graphics`. Managed buffers may still be copied by the GC, so this is best effort. (5) Hold decoded secrets in KeePass `ProtectedString` as soon as possible; (6) no network calls; (7) the snip overlay can apply `WDA_EXCLUDEFROMCAPTURE` to its own window so other capture tools do not record the frozen screenshot. This is optional and not a guarantee. (8) Show a confirmation dialog with issuer/account (secret masked) before saving.
- The global hotkey must be configurable and conflict-checked. KeeOtp2's default auto-type hotkey Ctrl+Alt+T is one existing binding to avoid ([KeeOtp2 README](https://raw.githubusercontent.com/tiuub/KeeOtp2/master/README.md)).

### Gaps
- No source found on whether Windows 11 logs or surfaces GDI screen reads in any privacy dashboard (assumed no).

## Overall effort estimate (inference, summarizing sections 1–7)
- Capture around cursor (PMv2 thread switch, monitor clamp, hotkey, error UI): 2–3 days, including mixed-DPI testing.
- Snip fallback overlay (per-monitor frozen screenshot, drag rectangle, multi-monitor): 2–3 days.
- Own QR decoder, screen-optimized, v1–40, RS, modes: 5–10 days, plus building a test corpus.
- Clipboard, file and text import: 1–2 days. Google migration import: 2–3 days.
- Total own-code path: about 12–21 developer days. With ZXing.Net instead of an own decoder, the decoder line item shrinks to about 1 day, plus license, merge and signing work (about 1 day).
