// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Globalization;
using System.Windows.Forms;

using KeePass;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util;
using KeePassLib;
using KeePassLib.Security;

using RamzXOtp.Integration;
using RamzXOtp.Otp;
using RamzXOtp.Storage;
using RamzXOtp.UI;

namespace RamzXOtp
{
    /// <summary>Plugin entry point. KeePass requires the class name "{Namespace}Ext".</summary>
    public sealed class RamzXOtpExt : Plugin
    {
        private IPluginHost m_host;
        private CopyGuard m_guard;

        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            m_host = host;
            Strings.Select(Program.Translation.Properties.Iso6391Code);

            // Miftawerk Unlock is looked up each time, so load order and later installs don't matter.
            m_guard = new CopyGuard(host.CustomConfig, () =>
            {
                UnlockBridge b = UnlockBridge.Find();
                if (b == null) return null;
                return msg => b.VerifyAsync(m_host.MainWindow, msg);
            }, () => DateTime.UtcNow);

            AddTopLevelCopy();
            return true;
        }

        public override void Terminate()
        {
            ContextMenuStrip cms = m_host == null ? null : m_host.MainWindow.EntryContextMenu;
            if (cms != null && m_topCopy != null)
            {
                cms.Opening -= OnEntryMenuOpening;
                cms.Items.Remove(m_topCopy);
                m_topCopy.Dispose();
            }
            m_topCopy = null;
            m_guard = null;
            m_host = null;
        }

        private ToolStripMenuItem m_topCopy;

        /// <summary>
        /// "Copy OTP code 123 456 (20 s)" directly in the entry context menu, right below KeePass's
        /// "Copy Password"; only visible for entries that have a one-time password.
        /// </summary>
        private void AddTopLevelCopy()
        {
            ContextMenuStrip cms = m_host.MainWindow.EntryContextMenu;
            if (cms == null) return;

            m_topCopy = new ToolStripMenuItem(Strings.Get(Strings.MenuCopyTopProtected));
            m_topCopy.Image = Icons.Menu(m_host.MainWindow);
            m_topCopy.Font = new System.Drawing.Font(cms.Font, System.Drawing.FontStyle.Bold);
            m_topCopy.Visible = false;
            m_topCopy.Click += OnCopyClick;

            int index = -1;
            for (int i = 0; i < cms.Items.Count; ++i)
                if (cms.Items[i].Name.IndexOf("CopyPassword", StringComparison.OrdinalIgnoreCase) >= 0) index = i + 1;
            if (index < 0) index = Math.Min(2, cms.Items.Count);
            cms.Items.Insert(index, m_topCopy);
            cms.Opening += OnEntryMenuOpening;
        }

        private void OnEntryMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m_topCopy == null) return;
            string info = CopyInfo(SelectedEntry());
            m_topCopy.Visible = info != null;
            if (info == null) return;
            m_topCopy.Text = info.Length == 0 ? Strings.Get(Strings.MenuCopyTopProtected)
                : Strings.Format(Strings.MenuCopyTop, info);
        }

        /// <summary>null = no OTP; "" = protected (do not show); otherwise "123 456  (20 s)".</summary>
        private string CopyInfo(PwEntry e)
        {
            OtpParameters p = e == null ? null : KeePassOtpFields.Read(e);
            if (p == null) return null;
            try
            {
                if (!m_guard.MayShowWithoutAsking) return string.Empty;
                long now = Totp.UnixNow();
                return p.Type == OtpType.Totp
                    ? Group(p.CurrentCode(now)) + "  (" + Strings.Format(Strings.Remaining, Totp.SecondsRemaining(now, p.Period)) + ")"
                    : "HOTP";
            }
            finally { p.Clear(); }
        }

        /// <summary>
        /// Entry context menu and Tools menu: "RamzX OTP" with "Copy code 123 456 (23 s)" - the
        /// current code is shown in the menu itself - and "Add or replace...".
        /// </summary>
        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t != PluginMenuType.Entry && t != PluginMenuType.Main) return null;

            ToolStripMenuItem root = new ToolStripMenuItem(Ui.Title);
            root.Image = Icons.Menu(m_host.MainWindow);

            ToolStripMenuItem copy = new ToolStripMenuItem(Strings.Get(Strings.MenuCopyNone));
            copy.Click += OnCopyClick;
            root.DropDownItems.Add(copy);

            ToolStripMenuItem add = new ToolStripMenuItem(Strings.Get(Strings.MenuAdd));
            add.Click += OnAddClick;
            root.DropDownItems.Add(add);

            root.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem protect = new ToolStripMenuItem(Strings.Get(Strings.MenuRequireHello));
            protect.ToolTipText = Strings.Get(Strings.RequireHelloNote);
            protect.Click += OnProtectClick;
            root.DropDownItems.Add(protect);

            root.DropDownOpening += delegate
            {
                UpdateCopyItem(copy);
                UpdateProtectItem(protect);
            };
            return root;
        }

        private void UpdateProtectItem(ToolStripMenuItem protect)
        {
            bool available = m_guard.UnlockAvailable;
            protect.Checked = m_guard.RequireHello;
            protect.Text = Strings.Get(available ? Strings.MenuRequireHello : Strings.MenuRequireHelloMissing);
            protect.Enabled = available || protect.Checked; // always allow switching it off
        }

        private async void OnProtectClick(object sender, EventArgs e)
        {
            if (m_guard.RequireHello)
            {
                m_guard.RequireHello = false;
                return;
            }
            if (!await m_guard.EnableAsync(Strings.Get(Strings.HelloEnableReason)))
                Ui.Info(m_host.MainWindow, Strings.Get(Strings.HelloNotEnabled));
        }

        private PwEntry SelectedEntry()
        {
            PwDatabase db = m_host.Database;
            PwEntry[] sel = m_host.MainWindow.GetSelectedEntries();
            if (db == null || !db.IsOpen || sel == null || sel.Length != 1) return null;
            return sel[0];
        }

        private void UpdateCopyItem(ToolStripMenuItem copy)
        {
            string info = CopyInfo(SelectedEntry());
            copy.Enabled = info != null;
            if (info == null) copy.Text = Strings.Get(Strings.MenuCopyNone);
            else if (info.Length == 0) copy.Text = Strings.Get(Strings.MenuCopyProtected); // never show before Hello
            else copy.Text = Strings.Format(Strings.MenuCopy, info);
        }

        private async void OnCopyClick(object sender, EventArgs e)
        {
            PwEntry entry = SelectedEntry();
            if (entry == null || !KeePassOtpFields.HasOtp(entry)) return;

            GuardResult g = await m_guard.CheckAsync(Strings.Format(Strings.HelloCopyReason,
                entry.Strings.ReadSafe(PwDefs.TitleField)));
            if (g == GuardResult.Denied) { m_host.MainWindow.SetStatusEx(Strings.Get(Strings.HelloDenied)); return; }
            if (g == GuardResult.UnlockMissing) { Ui.Warn(m_host.MainWindow, Strings.Get(Strings.UnlockMissing)); return; }

            OtpParameters p = KeePassOtpFields.Read(entry);
            if (p == null) return;
            try
            {
                string code = p.CurrentCode(Totp.UnixNow());
                if (p.Type == OtpType.Hotp)
                {
                    // Every HOTP code may be used once: advance the counter like KeePass does.
                    entry.Strings.Set(KeePassOtpFields.HmacCounter,
                        new ProtectedString(false, (p.Counter + 1).ToString(CultureInfo.InvariantCulture)));
                    entry.Touch(true);
                    m_host.MainWindow.UpdateUI(false, null, false, null, false, null, true);
                }

                if (ClipboardUtil.Copy(code, false, true, entry, m_host.Database, m_host.MainWindow.Handle))
                {
                    m_host.MainWindow.StartClipboardCountdown();
                    m_host.MainWindow.SetStatusEx(Strings.Get(Strings.CodeCopied));
                }
            }
            finally { p.Clear(); }
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            Form owner = m_host.MainWindow;
            PwDatabase db = m_host.Database;
            PwEntry entry = SelectedEntry();
            if (entry == null)
            {
                Ui.Info(owner, Strings.Get(Strings.SelectOneEntry));
                return;
            }

            // An existing OTP is shown in the dialog (with its live code); saving keeps the old
            // values in the entry history, so no extra confirmation is needed.
            OtpParameters existing = KeePassOtpFields.Read(entry);
            OtpParameters p;
            try
            {
                using (AddOtpForm f = new AddOtpForm(entry.Strings.ReadSafe(PwDefs.TitleField), existing))
                {
                    if (UIUtil.ShowDialogAndDestroy(f) != DialogResult.OK) return;
                    p = f.Result;
                }
            }
            finally { if (existing != null) existing.Clear(); }
            if (p == null) return;

            try
            {
                entry.CreateBackup(db);   // old values stay in the entry history
                KeePassOtpFields.Write(entry, p);
                entry.Touch(true);
                entry.MaintainBackups(db);
            }
            finally { p.Clear(); }

            m_host.MainWindow.UpdateUI(false, null, false, null, true, null, true);
            Ui.Info(owner, Strings.Get(Strings.Saved));
        }

        internal static string Group(string code)
        {
            int half = code.Length / 2;
            return code.Substring(0, half) + " " + code.Substring(half);
        }
    }
}
