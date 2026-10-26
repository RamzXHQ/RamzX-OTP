// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using KeePass.UI;
using KeePassLib;

using RamzXOtp.Otp;
using RamzXOtp.Storage;

namespace RamzXOtp.UI
{
    /// <summary>
    /// Import from Google Authenticator ("Transfer accounts" QR codes). Several QR codes of one
    /// export can be scanned one after another; the user picks which accounts to create.
    /// </summary>
    internal sealed class ImportForm : Form
    {
        private readonly PwDatabase m_db;
        private readonly string m_groupName;
        private readonly PictureBox m_banner = new PictureBox();
        private readonly ListView m_list = new ListView();
        private readonly StatusPill m_status = new StatusPill();
        private readonly Button m_import = new Button();
        private readonly List<OtpParameters> m_accounts = new List<OtpParameters>();
        private readonly HashSet<string> m_seenBatches = new HashSet<string>();
        private int m_expectedBatches;

        /// <summary>Accounts the user checked (valid after DialogResult.OK).</summary>
        internal List<OtpParameters> Selected { get; private set; }

        internal ImportForm(PwDatabase db, string groupName)
        {
            m_db = db;
            m_groupName = groupName;
            Text = Ui.Title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = SystemFonts.MessageBoxFont;
            BackColor = Color.White;
            if (Strings.RightToLeft) { RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; }

            int w = Font.Height * 36;
            m_banner.Dock = DockStyle.Top;
            m_banner.Height = Font.Height * 4;

            FlowLayoutPanel f = new FlowLayoutPanel();
            f.FlowDirection = FlowDirection.TopDown;
            f.AutoSize = true;
            f.Location = new Point(Font.Height, m_banner.Height + Font.Height);

            Label how = new Label();
            how.Text = Strings.Get(Strings.ImportHowTo);
            how.AutoSize = true;
            how.MaximumSize = new Size(w, 0);
            f.Controls.Add(how);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.Margin = new Padding(0, Font.Height / 2, 0, Font.Height / 2);
            buttons.Controls.Add(SourceButton(Strings.ScanScreen, Icons.Crosshair, ScanScreen, true));
            buttons.Controls.Add(SourceButton(Strings.FromClipboard, Icons.Clipboard, FromClipboard, false));
            buttons.Controls.Add(SourceButton(Strings.FromFile, Icons.Picture, FromFile, false));
            f.Controls.Add(buttons);

            m_list.View = View.Details;
            m_list.CheckBoxes = true;
            m_list.FullRowSelect = true;
            m_list.Size = new Size(w, Font.Height * 14);
            m_list.Columns.Add(Strings.Get(Strings.ColService), w * 30 / 100);
            m_list.Columns.Add(Strings.Get(Strings.ColAccount), w * 34 / 100);
            m_list.Columns.Add(Strings.Get(Strings.ColType), w * 12 / 100);
            m_list.Columns.Add(Strings.Get(Strings.ColStatus), w * 22 / 100);
            m_list.ItemChecked += delegate { UpdateButton(); };
            f.Controls.Add(m_list);

            m_status.Width = w;
            m_status.Font = Font;
            m_status.Margin = new Padding(0, Font.Height / 2, 0, 0);
            f.Controls.Add(m_status);

            FlowLayoutPanel ok = new FlowLayoutPanel();
            ok.FlowDirection = FlowDirection.RightToLeft;
            ok.AutoSize = true;
            ok.Width = w;
            ok.Margin = new Padding(0, Font.Height, 0, 0);
            Button cancel = new Button();
            cancel.Text = Strings.Get(Strings.Cancel);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.AutoSize = true;
            cancel.MinimumSize = new Size(Font.Height * 7, Font.Height * 2);
            m_import.DialogResult = DialogResult.OK;
            m_import.AutoSize = true;
            m_import.MinimumSize = cancel.MinimumSize;
            m_import.Font = new Font(Font, FontStyle.Bold);
            m_import.Click += delegate { CollectSelection(); };
            ok.Controls.Add(cancel);
            ok.Controls.Add(m_import);
            f.Controls.Add(ok);

            Controls.Add(f);
            Controls.Add(m_banner);
            AcceptButton = m_import;
            CancelButton = cancel;
            ClientSize = new Size(w + Font.Height * 2, m_banner.Height + f.PreferredSize.Height + Font.Height * 4);

            m_status.Set(PillKind.Neutral, Strings.Get(Strings.ImportStart));
            UpdateButton();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            BannerFactory.CreateBannerEx(this, m_banner, Icons.Timer(48), Strings.Get(Strings.ImportTitle),
                Strings.Format(Strings.ImportSubtitle, m_groupName));
        }

        private Button SourceButton(string key, Func<int, Bitmap> icon, Action action, bool primary)
        {
            Button b = new Button();
            b.Text = Strings.Get(key);
            b.AutoSize = true;
            b.Padding = new Padding(4, 2, 4, 2);
            b.MinimumSize = new Size(0, Font.Height * 2 + 4);
            b.Image = icon(Font.Height + 2);
            b.TextImageRelation = TextImageRelation.ImageBeforeText;
            if (primary) b.Font = new Font(Font, FontStyle.Bold);
            b.Click += delegate
            {
                try { action(); }
                catch (Exception ex) { Ui.Warn(this, ex.Message); }
            };
            return b;
        }

        private void ScanScreen()
        {
            bool cancelled;
            string text;
            Hide();
            try { text = ScreenScanner.PickAndDecode(this, out cancelled); }
            finally { Show(); Activate(); }
            if (!cancelled) Add(text);
        }

        private void FromClipboard()
        {
            bool hadImage;
            string text = ScreenScanner.FromClipboard(out hadImage);
            if (!hadImage && Clipboard.ContainsText()) text = Clipboard.GetText();
            Add(text);
        }

        private void FromFile()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|*.*|*.*";
                d.Multiselect = true;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                foreach (string file in d.FileNames) Add(ScreenScanner.FromFile(file));
            }
        }

        /// <summary>Adds the accounts of one export QR code (or a single otpauth:// link).</summary>
        internal void Add(string text)
        {
            if (string.IsNullOrEmpty(text)) { m_status.Set(PillKind.Error, Strings.Get(Strings.QrNotFound)); return; }
            if (text.Length > GoogleMigration.MaxLength) { m_status.Set(PillKind.Error, Strings.Get(Strings.TextTooLong)); return; }

            if (OtpAuthUri.LooksLikeUri(text))
            {
                try { AddAccount(OtpAuthUri.Parse(text), null); }
                catch (FormatException ex) { m_status.Set(PillKind.Error, ex.Message); return; }
                UpdateStatus();
                return;
            }
            if (!GoogleMigration.LooksLikeMigration(text)) { m_status.Set(PillKind.Error, Strings.Get(Strings.QrNotOtp)); return; }

            MigrationBatch batch;
            try { batch = GoogleMigration.Parse(text); }
            catch (FormatException ex) { m_status.Set(PillKind.Error, ex.Message); return; }

            string key = batch.BatchId + "/" + batch.BatchIndex;
            if (!m_seenBatches.Add(key)) { m_status.Set(PillKind.Warn, Strings.Get(Strings.ImportSameQr)); return; }
            m_expectedBatches = Math.Max(m_expectedBatches, batch.BatchSize);

            foreach (MigrationAccount a in batch.Accounts) AddAccount(a.Parameters, a.Unsupported);
            UpdateStatus();
        }

        private void AddAccount(OtpParameters p, string unsupported)
        {
            bool exists = unsupported == null && AccountImporter.Exists(m_db, p.Secret);
            foreach (OtpParameters known in m_accounts)
                if (AccountImporter.SameSecret(known.Secret, p.Secret)) exists = true;

            ListViewItem item = new ListViewItem(string.IsNullOrEmpty(p.Issuer) ? "-" : p.Issuer);
            item.SubItems.Add(p.Account ?? string.Empty);
            item.SubItems.Add(p.IsSteam ? "Steam" : (p.Type == OtpType.Hotp ? "HOTP" : "TOTP"));
            item.SubItems.Add(unsupported != null ? Strings.Get(Strings.ImportUnsupported)
                : exists ? Strings.Get(Strings.ImportExists) : Strings.Get(Strings.ImportNew));
            item.ForeColor = unsupported != null ? Theme.Muted : exists ? Theme.Warn : Theme.Text;
            item.ToolTipText = unsupported;
            item.Tag = unsupported == null ? p : null;
            item.Checked = unsupported == null && !exists;
            m_list.Items.Add(item);
            m_accounts.Add(p);
        }

        private void UpdateStatus()
        {
            if (m_expectedBatches > 1 && m_seenBatches.Count < m_expectedBatches)
                m_status.Set(PillKind.Warn, Strings.Format(Strings.ImportMoreQr, m_seenBatches.Count, m_expectedBatches));
            else
                m_status.Set(PillKind.Ok, Strings.Format(Strings.ImportReady, m_list.Items.Count));
            UpdateButton();
        }

        private void UpdateButton()
        {
            int n = 0;
            foreach (ListViewItem i in m_list.Items) if (i.Checked && i.Tag != null) ++n;
            m_import.Text = Strings.Format(Strings.ImportButton, n);
            m_import.Enabled = n > 0;
        }

        private void CollectSelection()
        {
            Selected = new List<OtpParameters>();
            foreach (ListViewItem i in m_list.Items)
                if (i.Checked && i.Tag is OtpParameters) Selected.Add((OtpParameters)i.Tag);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Wipe everything that was not handed over for import.
            foreach (OtpParameters p in m_accounts)
                if (Selected == null || DialogResult != DialogResult.OK || !Selected.Contains(p)) p.Clear();
            base.OnFormClosed(e);
        }
    }
}
