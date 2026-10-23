// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Windows.Forms;

using KeePass;
using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;

using RamzXOtp.Otp;
using RamzXOtp.Storage;
using RamzXOtp.UI;

namespace RamzXOtp
{
    /// <summary>Plugin entry point. KeePass requires the class name "{Namespace}Ext".</summary>
    public sealed class RamzXOtpExt : Plugin
    {
        private IPluginHost m_host;

        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            m_host = host;
            Strings.Select(Program.Translation.Properties.Iso6391Code);
            return true;
        }

        public override void Terminate()
        {
            m_host = null;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t != PluginMenuType.Entry && t != PluginMenuType.Main) return null;

            ToolStripMenuItem item = new ToolStripMenuItem(Strings.Get(Strings.MenuAdd));
            item.Click += OnAddClick;
            return item;
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            Form owner = m_host.MainWindow;
            PwDatabase db = m_host.Database;
            PwEntry[] sel = m_host.MainWindow.GetSelectedEntries();
            if (db == null || !db.IsOpen || sel == null || sel.Length != 1)
            {
                Ui.Info(owner, Strings.Get(Strings.SelectOneEntry));
                return;
            }

            PwEntry entry = sel[0];
            if (KeePassOtpFields.HasOtp(entry) && !Ui.Ask(owner, Strings.Get(Strings.ConfirmReplace))) return;

            OtpParameters p;
            using (AddOtpForm f = new AddOtpForm(entry.Strings.ReadSafe(PwDefs.TitleField)))
            {
                if (UIUtil.ShowDialogAndDestroy(f) != DialogResult.OK) return;
                p = f.Result;
            }
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
    }
}
