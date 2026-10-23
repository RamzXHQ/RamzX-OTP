// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

using KeePass;
using KeePass.App.Configuration;
using KeePass.Forms;
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
        private static readonly System.Diagnostics.Stopwatch s_clock = System.Diagnostics.Stopwatch.StartNew();

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
            }, () => DateTime.MinValue + s_clock.Elapsed); // monotonic: changing the system clock cannot extend the grace time

            // Closing or locking the database ends the Windows Hello grace time.
            m_host.MainWindow.FileClosed += OnFileClosed;

            AddTopLevelCopy();
            RegisterColumn();
            return true;
        }

        private void OnFileClosed(object sender, FileClosedEventArgs e)
        {
            if (m_guard != null) m_guard.Reset();
        }

        public override void Terminate()
        {
            if (m_host != null)
            {
                m_host.MainWindow.FileClosed -= OnFileClosed;
                UnregisterColumn();
            }
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
            OtpParameters p = null;
            try
            {
                p = e == null ? null : OtpStore.Read(e);
                if (p == null) return null;
                if (!m_guard.MayShowWithoutAsking) return string.Empty;
                long now = Totp.UnixNow();
                return p.Type == OtpType.Totp
                    ? Group(p.CurrentCode(now)) + "  (" + Strings.Format(Strings.Remaining, Totp.SecondsRemaining(now, p.Period)) + ")"
                    : "HOTP";
            }
            catch (Exception) { return null; } // a damaged entry must not break the context menu
            finally { if (p != null) p.Clear(); }
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

            ToolStripMenuItem showQr = new ToolStripMenuItem(Strings.Get(Strings.MenuShowQr));
            showQr.Click += OnShowQrClick;
            root.DropDownItems.Add(showQr);

            root.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem import = new ToolStripMenuItem(Strings.Get(Strings.MenuImport));
            import.Click += OnImportClick;
            root.DropDownItems.Add(import);

            ToolStripMenuItem migrate = new ToolStripMenuItem(Strings.Get(Strings.MenuMigrate));
            migrate.Click += OnMigrateClick;
            root.DropDownItems.Add(migrate);

            root.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem protect = new ToolStripMenuItem(Strings.Get(Strings.MenuRequireHello));
            protect.ToolTipText = Strings.Get(Strings.RequireHelloNote);
            protect.Click += OnProtectClick;
            root.DropDownItems.Add(protect);

            root.DropDownOpening += delegate
            {
                UpdateCopyItem(copy);
                UpdateProtectItem(protect);
                showQr.Enabled = copy.Enabled;
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
            try
            {
                if (m_guard.RequireHello)
                {
                    if (!await m_guard.DisableAsync(Strings.Get(Strings.HelloDisableReason)))
                        m_host.MainWindow.SetStatusEx(Strings.Get(Strings.HelloDenied));
                    return;
                }
                if (!await m_guard.EnableAsync(Strings.Get(Strings.HelloEnableReason)))
                    Ui.Info(m_host.MainWindow, Strings.Get(Strings.HelloNotEnabled));
            }
            catch (Exception ex) { Ui.Warn(m_host.MainWindow, ex.Message); } // async void: never let it reach KeePass
        }

        /// <summary>
        /// After waiting for Windows Hello the database may have been closed, locked or switched:
        /// only go on if the entry still belongs to the open database.
        /// </summary>
        private bool StillOpen(PwEntry entry)
        {
            PwDatabase db = m_host == null ? null : m_host.Database;
            return db != null && db.IsOpen && db.RootGroup != null && db.RootGroup.FindEntry(entry.Uuid, true) == entry;
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

        private void OnCopyClick(object sender, EventArgs e)
        {
            CopyEntry(SelectedEntry());
        }

        private async void CopyEntry(PwEntry entry)
        {
            try { await CopyEntryAsync(entry); }
            catch (Exception ex) { Ui.Warn(m_host.MainWindow, ex.Message); } // async void: never let it reach KeePass
        }

        private async System.Threading.Tasks.Task CopyEntryAsync(PwEntry entry)
        {
            if (entry == null || !OtpStore.HasOtp(entry)) return;

            GuardResult g = await m_guard.CheckAsync(Strings.Format(Strings.HelloCopyReason,
                entry.Strings.ReadSafe(PwDefs.TitleField)));
            if (g == GuardResult.Denied) { m_host.MainWindow.SetStatusEx(Strings.Get(Strings.HelloDenied)); return; }
            if (g == GuardResult.UnlockMissing) { Ui.Warn(m_host.MainWindow, Strings.Get(Strings.UnlockMissing)); return; }
            if (!StillOpen(entry)) return;

            OtpParameters p = OtpStore.Read(entry);
            if (p == null) return;
            try
            {
                if (p.Type == OtpType.Hotp && p.Counter == ulong.MaxValue)
                {
                    // The counter would wrap around to 0 and old codes would be issued again.
                    Ui.Warn(m_host.MainWindow, Strings.Get(Strings.HotpExhausted));
                    return;
                }
                string code = p.CurrentCode(Totp.UnixNow());
                if (p.Type == OtpType.Hotp)
                {
                    // Every HOTP code may be used once: advance the counter like KeePass does.
                    p.Counter += 1;
                    OtpStore.Write(entry, p);
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

        private async void OnAddClick(object sender, EventArgs e)
        {
            try { await AddAsync(); }
            catch (Exception ex) { Ui.Warn(m_host.MainWindow, ex.Message); } // async void: never let it reach KeePass
        }

        private async System.Threading.Tasks.Task AddAsync()
        {
            Form owner = m_host.MainWindow;
            PwEntry entry = SelectedEntry();
            if (entry == null)
            {
                Ui.Info(owner, Strings.Get(Strings.SelectOneEntry));
                return;
            }

            // The dialog shows an existing secret and its live code: same protection as copying.
            if (OtpStore.HasOtp(entry))
            {
                GuardResult g = await m_guard.CheckAsync(Strings.Format(Strings.HelloCopyReason,
                    entry.Strings.ReadSafe(PwDefs.TitleField)));
                if (g == GuardResult.Denied) { m_host.MainWindow.SetStatusEx(Strings.Get(Strings.HelloDenied)); return; }
                if (g == GuardResult.UnlockMissing) { Ui.Warn(owner, Strings.Get(Strings.UnlockMissing)); return; }
                if (!StillOpen(entry)) return;
            }
            PwDatabase db = m_host.Database;

            // An existing OTP is shown in the dialog (with its live code); saving keeps the old
            // values in the entry history, so no extra confirmation is needed.
            OtpParameters existing = OtpStore.Read(entry);
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
                OtpStore.Write(entry, p);
                entry.Touch(true);
                entry.MaintainBackups(db);
            }
            finally { p.Clear(); }

            m_host.MainWindow.UpdateUI(false, null, false, null, true, null, true);
            Ui.Info(owner, Strings.Get(Strings.Saved));
        }

        private async void OnShowQrClick(object sender, EventArgs e)
        {
            try { await ShowQrAsync(); }
            catch (Exception ex) { Ui.Warn(m_host.MainWindow, ex.Message); } // async void: never let it reach KeePass
        }

        private async System.Threading.Tasks.Task ShowQrAsync()
        {
            PwEntry entry = SelectedEntry();
            if (entry == null || !OtpStore.HasOtp(entry)) { Ui.Info(m_host.MainWindow, Strings.Get(Strings.SelectOneEntry)); return; }

            string title = entry.Strings.ReadSafe(PwDefs.TitleField);
            GuardResult g = await m_guard.CheckAsync(Strings.Format(Strings.HelloCopyReason, title));
            if (g == GuardResult.Denied) { m_host.MainWindow.SetStatusEx(Strings.Get(Strings.HelloDenied)); return; }
            if (g == GuardResult.UnlockMissing) { Ui.Warn(m_host.MainWindow, Strings.Get(Strings.UnlockMissing)); return; }
            if (!StillOpen(entry)) return;

            OtpParameters p = OtpStore.Read(entry);
            if (p == null) return;
            try
            {
                // Label for the phone app: entry title and user name.
                if (string.IsNullOrEmpty(p.Issuer)) p.Issuer = title;
                if (string.IsNullOrEmpty(p.Account)) p.Account = entry.Strings.ReadSafe(PwDefs.UserNameField);
                using (ShowQrForm f = new ShowQrForm(title, p)) UIUtil.ShowDialogAndDestroy(f);
            }
            finally { p.Clear(); }
        }

        private void OnImportClick(object sender, EventArgs e)
        {
            try { Import(); }
            catch (Exception ex) { Ui.Warn(m_host.MainWindow, ex.Message); } // never let it reach KeePass
        }

        private void Import()
        {
            PwDatabase db = m_host.Database;
            if (db == null || !db.IsOpen) { Ui.Info(m_host.MainWindow, Strings.Get(Strings.OpenDatabaseFirst)); return; }

            PwGroup group = m_host.MainWindow.GetSelectedGroup() ?? db.RootGroup;
            List<OtpParameters> selected;
            using (ImportForm f = new ImportForm(db, group.Name))
            {
                if (UIUtil.ShowDialogAndDestroy(f) != DialogResult.OK || f.Selected == null) return;
                selected = f.Selected;
            }
            try
            {
                List<PwEntry> created = AccountImporter.CreateEntries(db, group, selected);
                m_host.MainWindow.UpdateUI(false, null, true, group, true, null, true);
                Ui.Info(m_host.MainWindow, Strings.Format(Strings.ImportDone, created.Count, group.Name));
            }
            finally { foreach (OtpParameters p in selected) p.Clear(); }
        }

        private void OnMigrateClick(object sender, EventArgs e)
        {
            try { Migrate(); }
            catch (Exception ex) { Ui.Warn(m_host.MainWindow, ex.Message); } // never let it reach KeePass
        }

        private void Migrate()
        {
            PwDatabase db = m_host.Database;
            if (db == null || !db.IsOpen) { Ui.Info(m_host.MainWindow, Strings.Get(Strings.OpenDatabaseFirst)); return; }

            List<PwEntry> legacy = OtpStore.FindLegacy(db);
            if (legacy.Count == 0) { Ui.Info(m_host.MainWindow, Strings.Get(Strings.MigrateNothing)); return; }
            if (!Ui.Ask(m_host.MainWindow, Strings.Format(Strings.MigrateAsk, legacy.Count))) return;

            int done = 0;
            foreach (PwEntry entry in legacy)
            {
                OtpParameters p = OtpStore.Read(entry);
                if (p == null) continue;
                try
                {
                    entry.CreateBackup(db); // the old fields stay in the entry history
                    OtpStore.Write(entry, p);
                    entry.Touch(true);
                    entry.MaintainBackups(db);
                    ++done;
                }
                finally { p.Clear(); }
            }
            m_host.MainWindow.UpdateUI(false, null, false, null, true, null, true);
            Ui.Info(m_host.MainWindow, Strings.Format(Strings.MigrateDone, done));
        }

        // ----- live OTP column -----

        private OtpColumnProvider m_column;
        private Timer m_columnTimer;

        private void RegisterColumn()
        {
            m_column = new OtpColumnProvider(() => m_guard, CopyEntry);
            m_host.ColumnProviderPool.Add(m_column);
            m_columnTimer = new Timer();
            m_columnTimer.Interval = 1000;
            m_columnTimer.Tick += delegate { RefreshColumn(); };
            m_columnTimer.Start();
        }

        private void UnregisterColumn()
        {
            if (m_columnTimer != null) { m_columnTimer.Stop(); m_columnTimer.Dispose(); m_columnTimer = null; }
            if (m_column != null) { m_host.ColumnProviderPool.Remove(m_column); m_column = null; }
        }

        // Refreshes the list once per second, only while the column is shown and nothing is open over it.
        private void RefreshColumn()
        {
            MainForm mf = m_host.MainWindow;
            if (mf == null || !mf.Visible || mf.WindowState == FormWindowState.Minimized) return;
            if (m_host.Database == null || !m_host.Database.IsOpen) return;
            if (!ColumnShown() || GlobalWindowManager.WindowCount > 0) return;
            if (mf.EntryContextMenu != null && mf.EntryContextMenu.Visible) return;
            mf.RefreshEntriesList();
        }

        private static bool ColumnShown()
        {
            foreach (AceColumn c in Program.Config.MainWindow.EntryListColumns)
                if (c.Type == AceColumnType.PluginExt && c.CustomName == OtpColumnProvider.ColumnName) return true;
            return false;
        }

        internal static string Group(string code)
        {
            int half = code.Length / 2;
            return code.Substring(0, half) + " " + code.Substring(half);
        }
    }
}
