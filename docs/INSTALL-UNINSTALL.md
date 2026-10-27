# RamzX KeePass plugins – install, uninstall, troubleshooting

This guide covers the three RamzX plugins for KeePass 2 on Windows. They work alone and together:

| Plugin | File in `KeePass\Plugins` | What it does |
|---|---|---|
| **RamzX Unlock** | `RamzXUnlock.dll` (up to 0.6: `MiftawerkUnlock.dll`) | Opens a database with Windows Hello (face, fingerprint or Windows Hello PIN), also over Remote Desktop. |
| **RamzX OTP** | `RamzXOtp.dll` | One-time passwords (TOTP/HOTP, Steam), QR import, Google Authenticator import. |
| **RamzX Connect** | `RamzXConnect.dll` (up to 0.1: `MiftawerkConnect.dll`) + browser extension + `ramzx-proxy.exe` | Fills logins in Chrome, Edge, Brave, Vivaldi and Firefox; passkeys; fake-site warning. |

Together: when KeePass is locked, the RamzX menu in a login field offers **Unlock KeePass**. KeePass shows its
master key prompt, RamzX Unlock asks Windows Hello, and the page is filled. OTP codes stored by RamzX OTP
(`otp` field, `otpauth://` link) are read by RamzX Connect.

> **Honest note on Windows Hello:** Windows Hello always also accepts the Windows Hello PIN. Quick unlock is
> comfort, not a second factor. Biometric data never leaves Windows; the plugins never see it.

---

## 1. Requirements

- Windows 10 or 11, **KeePass 2.61 or newer** (official build from keepass.info), .NET Framework 4.8.
- RamzX Unlock: Windows Hello set up (Settings → Accounts → Sign-in options).
- RamzX Connect: one of the browsers above; Firefox 140 or newer.

## 2. Install

### 2.1 RamzX Unlock and RamzX OTP

1. Close KeePass completely (also the tray icon: right-click → Exit).
2. Downloaded files: right-click the DLL → **Properties** → tick **Unblock** (if shown) → OK.
3. Copy the DLL into the `Plugins` folder of KeePass:
   - installed KeePass: `C:\Program Files\KeePass Password Safe 2\Plugins` (Windows asks for administrator
     rights → **Continue**),
   - portable KeePass: the `Plugins` folder next to `KeePass.exe`.
4. Start KeePass. Check: **Tools** menu shows **RamzX Unlock** / **RamzX OTP** with the RamzX logo, and
   **Tools → Plugins** lists them.
5. RamzX Unlock: open your database once with the master password; the plugin offers **Enable**. Confirm with
   Windows Hello. From then on the database opens with Windows Hello until the set number of days ends or the
   computer restarts.

**Updating from Unlock 0.6 or older:** delete `MiftawerkUnlock.dll` from the `Plugins` folder when you copy
`RamzXUnlock.dll`. Your quick-unlock setup is kept (same data folder). If the old file is still there, RamzX
Unlock warns at start.

### 2.2 RamzX Connect

1. Close KeePass. Unzip `ramzx-connect-windows-<version>.zip` into a folder of your choice.
2. Right-click `install-windows.ps1` → **Run with PowerShell** (or in PowerShell: `.\install-windows.ps1`).
   It registers the connector for Chrome, Edge, Brave, Vivaldi, Chromium and Firefox (per user, no admin
   rights) and copies `ramzx-proxy.exe` to `%LOCALAPPDATA%\RamzX\Connect`.
   - If it says *no write access to the KeePass folder*: copy `RamzXConnect.dll` into `KeePass\Plugins` as in
     2.1 (administrator rights), or run the printed command in an administrator PowerShell.
3. Install the browser extension (Chrome Web Store for Chrome/Brave/Vivaldi, Edge Add-ons, Firefox Add-ons).
4. Start KeePass and open your database. Restart the browser.
5. Click the RamzX icon in the browser → **Connect this browser** → in KeePass check the key fingerprint, give
   the connection a name → **Connect**.
6. First use on a site: KeePass asks once whether the site may use the matching entries → **Allow**
   (*Remember this decision* is ticked).

**Firefox:** if the RamzX window shows **One click to finish**, click **Allow** and confirm Firefox's question.
Firefox does not grant site access automatically when an older version is updated, and you can revoke it in
`about:addons`.

**Updating from Connect 0.1 (Miftawerk Connect):** the installer removes `MiftawerkConnect.dll` when it can.
If it prints *The old plugin … is still there*, close KeePass and delete that file as administrator.

### 2.3 Check that everything works

| Check | Expected |
|---|---|
| KeePass → Tools | RamzX Unlock, RamzX OTP, RamzX Connect, each with the RamzX logo |
| Browser → RamzX icon | logo, “KeePass in your browser · by RamzX”, no error message |
| A site with exactly one login in KeePass | filled automatically on load, nothing submitted |
| A site with several logins | click into the login field → RamzX menu → one click fills |
| KeePass locked | the RamzX menu offers **Unlock KeePass** → Windows Hello → page filled |

## 3. Uninstall

Your databases and entries are never changed by uninstalling. OTP secrets stay in the standard `otp` field
(KeePass and KeePassXC can still use them); passkeys stay in their entries.

### 3.1 Remove one plugin only

1. Close KeePass completely.
2. Delete its DLL from `KeePass\Plugins` (administrator rights for `C:\Program Files`).
3. RamzX Connect additionally: run `.\install-windows.ps1 -Uninstall` (see 3.3) and remove the extension in
   the browser.

### 3.2 Remove everything, keep nothing behind (privacy)

Do these steps **before** deleting the DLLs, while KeePass and the plugins still run:

**RamzX Unlock**
1. **Tools → RamzX Unlock → Forget all** – deletes every quick-unlock record
   (`%LOCALAPPDATA%\SeeColors\MiftawerkUnlock\records`).
2. **Tools → RamzX Unlock → Delete self-test key** – deletes the plugin's Windows Hello key (if a self-test
   was run).
3. Windows **Settings → Accounts → Passkeys**: delete the passkeys for **unlock.ramzx.de**.
4. Close KeePass, delete `RamzXUnlock.dll` (and `MiftawerkUnlock.dll` if present), then delete the folder
   `%LOCALAPPDATA%\SeeColors\MiftawerkUnlock`.

**RamzX OTP**
1. Close KeePass and delete `RamzXOtp.dll`. The plugin stores no files of its own; one setting
   (`RamzXOtp.RequireHelloBeforeCopy`) stays in `KeePass.config.xml` and is ignored without the plugin.

**RamzX Connect**
1. In each browser: Extensions → RamzX Connect → **Remove**.
2. Close all browsers and KeePass, then in PowerShell in the unzipped folder:
   `.\install-windows.ps1 -Uninstall`
   It removes the browser registrations, `%LOCALAPPDATA%\RamzX\Connect` and the KeePass plugin (or prints the
   administrator command).
3. Optional, per database: **Database → Database Settings → Plugin data** – delete the keys starting with
   `KPXC_BROWSER_` (browser pairings) and `MiftawerkConnect.` (site decisions, passkey settings). Site decisions
   per entry: entry context menu **RamzX Connect → Forget site decisions** before uninstalling.

### 3.3 What `install-windows.ps1 -Uninstall` does

| Removed | Where |
|---|---|
| Native messaging registrations `de.ramzx.connect` and `de.seecolors.miftawerk` | `HKCU\Software\{Google\Chrome, Microsoft\Edge, BraveSoftware\Brave-Browser, Vivaldi, Chromium, Mozilla}\NativeMessagingHosts` |
| Connector folder with `ramzx-proxy.exe` and the manifests | `%LOCALAPPDATA%\RamzX\Connect` |
| KeePass plugin `RamzXConnect.dll` / `MiftawerkConnect.dll` | `KeePass\Plugins` (needs administrator rights in `C:\Program Files`) |

### 3.4 Uninstalling KeePass itself

The KeePass uninstaller may leave plugin DLLs in `C:\Program Files\KeePass Password Safe 2\Plugins`. Delete
that folder afterwards, then follow 3.2 for the data folders.

## 4. Problems and fixes

| Symptom | Cause | Fix |
|---|---|---|
| Plugin missing in the Tools menu | DLL in the wrong folder; file blocked after download; KeePass too old | Copy into `KeePass\Plugins`; Properties → **Unblock**; update KeePass to 2.61+ |
| KeePass: *plugin incompatible* | KeePass older than the plugin needs | Update KeePass |
| Plugin blocked by **Smart App Control** (Windows 11) | the build is not code-signed | Use a signed release when available. Smart App Control cannot be switched back on once it is off – we do not recommend turning it off. |
| Windows Hello asks twice / two RamzX Unlock menus | old `MiftawerkUnlock.dll` still in `Plugins` | Close KeePass, delete `MiftawerkUnlock.dll` |
| Master key prompt has *Key file/provider: Miftawerk Unlock (Windows Hello)* ticked and the password box unticked | remembered by KeePass after quick unlock (Unlock up to 0.6) | Unlock 0.7 removes it at start. Older: untick the key provider, tick *Master password* |
| No Windows Hello prompt over Remote Desktop | WebAuthn redirection off; Windows Hello not set up on the computer you sit at | In the RDP connection allow WebAuthn redirection (`redirectwebauthn:i:1`); set up Windows Hello locally |
| RamzX Unlock asks to enable on a computer where it is already enabled | the database was opened with the master password/key file instead of Windows Hello | Click **Not now**; quick unlock stays active |
| Browser: *No connection to KeePass* | KeePass not running; RamzX Connect plugin missing; connector not installed; browser not restarted | Start KeePass; check Tools menu; run `install-windows.ps1`; restart the browser |
| Browser: *Database is locked* | KeePass workspace locked | **Unlock KeePass** in the RamzX window or in the login field menu |
| Nothing filled on a site | several logins (choose in the menu); site not allowed in KeePass (*Deny* was remembered); look-alike/fake site; sign-up form | Click into the field and choose; entry context menu → **RamzX Connect → Forget site decisions**; check the address |
| Fake-site warning on a real site | the site is new and resembles one of your sites | Only if you are sure: RamzX window → **Trust this site** (two clicks) |
| Firefox: RamzX menu shows but nothing is filled | site access not granted | RamzX window → **Allow** |
| `install-windows.ps1` cannot run (*running scripts is disabled*) | PowerShell execution policy | Right-click the file → Properties → **Unblock**, then run again; or in PowerShell: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| `-Uninstall` cannot delete `%LOCALAPPDATA%\RamzX\Connect` | a browser still runs the proxy | Close all browsers, run `-Uninstall` again |
| A plugin DLL cannot be deleted | KeePass still running (tray) or no administrator rights | KeePass tray icon → Exit; delete as administrator |

## 5. Support

support@ramzx.de · https://ramzx.de
