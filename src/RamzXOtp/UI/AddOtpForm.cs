// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass;
using KeePass.Forms;
using KeePass.UI;
using KeePass.Util;

using RamzXOtp.Otp;

namespace RamzXOtp.UI
{
    /// <summary>
    /// Add a one-time password: scan the QR code on screen, take it from the clipboard or a file,
    /// or paste the link/secret; the live code card shows the result before saving.
    /// </summary>
    internal sealed class AddOtpForm : Form
    {
        private readonly string m_entryTitle;
        private readonly PictureBox m_banner = new PictureBox();
        private readonly TextBox m_input = new TextBox();
        private readonly CheckBox m_show = new CheckBox();
        private readonly ComboBox m_algorithm = new ComboBox();
        private readonly NumericUpDown m_digits = new NumericUpDown();
        private readonly NumericUpDown m_period = new NumericUpDown();
        private readonly GroupBox m_settings = new GroupBox();
        private readonly StatusPill m_status = new StatusPill();
        private readonly CodeDisplay m_code = new CodeDisplay();
        private readonly Button m_copy = new Button();
        private readonly Button m_save = new Button();
        private readonly Timer m_timer = new Timer();
        private bool m_updating;
        private bool m_overrideLink; // user changed the settings of a link on purpose
        private OtpInput m_result;

        /// <summary>The parameters to save (valid after DialogResult.OK).</summary>
        internal OtpParameters Result { get { return m_result == null ? null : m_result.Parameters; } }

        /// <param name="existing">Current OTP of the entry (shown for editing), or null.</param>
        internal AddOtpForm(string entryTitle, OtpParameters existing = null)
            : this(entryTitle)
        {
            if (existing != null && existing.Secret != null)
                m_input.Text = OtpAuthUri.Format(existing);
        }

        internal AddOtpForm(string entryTitle)
        {
            m_entryTitle = entryTitle;
            Text = Strings.Format(Strings.FormTitle, entryTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            Font = SystemFonts.MessageBoxFont;
            BackColor = Color.White;
            if (Strings.RightToLeft) { RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; }

            int w = Font.Height * 34; // content width, scales with the font / DPI

            m_banner.Dock = DockStyle.Top;
            m_banner.Height = Font.Height * 4;

            TableLayoutPanel t = new TableLayoutPanel();
            t.ColumnCount = 1;
            t.AutoSize = true;
            t.Location = new Point(Font.Height, m_banner.Height + Font.Height);
            t.Padding = new Padding(0);

            // 1. Import row: the main ways to get the secret in.
            Label lblImport = Caption(Strings.Get(Strings.ImportCaption));
            t.Controls.Add(lblImport);
            FlowLayoutPanel import = new FlowLayoutPanel();
            import.AutoSize = true;
            import.Margin = new Padding(0, 2, 0, Font.Height / 2);
            import.Controls.Add(ImportButton(Strings.ScanScreen, Icons.Crosshair, ScanScreen, true));
            import.Controls.Add(ImportButton(Strings.FromClipboard, Icons.Clipboard, FromClipboard, false));
            import.Controls.Add(ImportButton(Strings.FromFile, Icons.Picture, FromFile, false));
            t.Controls.Add(import);

            // 2. Manual input.
            t.Controls.Add(Caption(Strings.Get(Strings.InputLabel)));
            TableLayoutPanel inputRow = new TableLayoutPanel();
            inputRow.ColumnCount = 2;
            inputRow.AutoSize = true;
            inputRow.Margin = new Padding(0, 2, 0, Font.Height / 2);
            m_input.Width = w - Font.Height * 7;
            m_input.UseSystemPasswordChar = true;
            m_input.TextChanged += delegate { if (!m_updating) m_overrideLink = false; Evaluate(); };
            UIUtil.SetCueBanner(m_input, "otpauth://totp/...   /   JBSW Y3DP EHPK 3PXP");
            m_show.Text = Strings.Get(Strings.ShowSecret);
            m_show.AutoSize = true;
            m_show.Margin = new Padding(Font.Height / 2, 3, 0, 0);
            m_show.CheckedChanged += delegate { m_input.UseSystemPasswordChar = !m_show.Checked; };
            inputRow.Controls.Add(m_input, 0, 0);
            inputRow.Controls.Add(m_show, 1, 0);
            t.Controls.Add(inputRow);

            // 3. Settings (only used for a pasted secret; a link brings its own).
            m_settings.Text = Strings.Get(Strings.SettingsCaption);
            m_settings.Width = w;
            m_settings.Height = Font.Height * 4;
            m_settings.Margin = new Padding(0, 0, 0, Font.Height / 2);
            FlowLayoutPanel s = new FlowLayoutPanel();
            s.Dock = DockStyle.Fill;
            s.Padding = new Padding(Font.Height / 2, Font.Height / 3, 0, 0);
            m_algorithm.DropDownStyle = ComboBoxStyle.DropDownList;
            m_algorithm.Items.AddRange(new object[] { "SHA-1", "SHA-256", "SHA-512" });
            m_algorithm.SelectedIndex = 0;
            m_algorithm.Width = Font.Height * 6;
            m_algorithm.SelectedIndexChanged += delegate { SettingChanged(); };
            m_digits.Minimum = Hotp.MinDigits;
            m_digits.Maximum = 8;
            m_digits.Value = 6;
            m_digits.Width = Font.Height * 3;
            m_digits.ValueChanged += delegate { SettingChanged(); };
            m_period.Minimum = 1;
            m_period.Maximum = Totp.MaxPeriod;
            m_period.Value = Totp.DefaultPeriod;
            m_period.Width = Font.Height * 4;
            m_period.ValueChanged += delegate { SettingChanged(); };
            s.Controls.Add(Inline(Strings.Get(Strings.Algorithm))); s.Controls.Add(m_algorithm);
            s.Controls.Add(Inline(Strings.Get(Strings.Digits))); s.Controls.Add(m_digits);
            s.Controls.Add(Inline(Strings.Get(Strings.Period))); s.Controls.Add(m_period);
            m_settings.Controls.Add(s);
            t.Controls.Add(m_settings);

            // 4. Status and live code.
            m_status.Width = w;
            m_status.Font = Font;
            m_status.Margin = new Padding(0, 0, 0, Font.Height / 2);
            t.Controls.Add(m_status);

            TableLayoutPanel codeRow = new TableLayoutPanel();
            codeRow.ColumnCount = 2;
            codeRow.AutoSize = true;
            m_code.Size = new Size(w - Font.Height * 8, Font.Height * 5);
            m_code.Font = Font;
            m_code.Click += delegate { CopyCode(); };
            m_copy.Text = Strings.Get(Strings.Copy);
            m_copy.Size = new Size(Font.Height * 7, Font.Height * 5);
            m_copy.Margin = new Padding(Font.Height / 2, 3, 0, 3);
            m_copy.Image = Icons.CopyIcon(Font.Height + 4);
            m_copy.TextImageRelation = TextImageRelation.ImageAboveText;
            m_copy.Click += delegate { CopyCode(); };
            codeRow.Controls.Add(m_code, 0, 0);
            codeRow.Controls.Add(m_copy, 1, 0);
            t.Controls.Add(codeRow);

            // 5. Honest note.
            Label note = new Label();
            note.Text = "ⓘ  " + Strings.Get(Strings.SameDbNote);
            note.AutoSize = true;
            note.MaximumSize = new Size(w, 0);
            note.ForeColor = Theme.Muted;
            note.Margin = new Padding(0, Font.Height / 2, 0, 0);
            t.Controls.Add(note);

            // 6. Buttons.
            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.AutoSize = true;
            buttons.Width = w;
            buttons.Margin = new Padding(0, Font.Height, 0, 0);
            Button cancel = new Button();
            cancel.Text = Strings.Get(Strings.Cancel);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.MinimumSize = new Size(Font.Height * 7, Font.Height * 2);
            cancel.AutoSize = true;
            m_save.Text = Strings.Get(Strings.Save);
            m_save.DialogResult = DialogResult.OK;
            m_save.MinimumSize = cancel.MinimumSize;
            m_save.AutoSize = true;
            m_save.Font = new Font(Font, FontStyle.Bold);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(m_save);
            t.Controls.Add(buttons);

            Controls.Add(t);
            Controls.Add(m_banner);
            ClientSize = new Size(w + Font.Height * 2, m_banner.Height + t.PreferredSize.Height + Font.Height * 4);

            AcceptButton = m_save;
            CancelButton = cancel;

            m_timer.Interval = 1000;
            m_timer.Tick += delegate { UpdatePreview(); };
            m_timer.Start();
            Evaluate();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Same banner style as KeePass's own dialogs.
            BannerFactory.CreateBannerEx(this, m_banner, Icons.Logo(48), Ui.Title,
                Strings.Format(Strings.BannerSubtitle, m_entryTitle));
            ClientSize = new Size(ClientSize.Width, m_banner.Height + Controls[0].PreferredSize.Height + Font.Height * 4);
            m_input.Focus();
        }

        private Label Caption(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Font = new Font(Font, FontStyle.Bold);
            l.ForeColor = Theme.Text;
            l.Margin = new Padding(0, 0, 0, 2);
            return l;
        }

        private Label Inline(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Margin = new Padding(Font.Height / 2, 6, 2, 0);
            return l;
        }

        private Button ImportButton(string key, Func<int, Bitmap> icon, Action action, bool primary)
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
            if (!cancelled) UseDecoded(text);
        }

        private void FromClipboard()
        {
            bool hadImage;
            string text = ScreenScanner.FromClipboard(out hadImage);
            if (hadImage) { UseDecoded(text); return; }

            string clip = Clipboard.ContainsText() ? Clipboard.GetText() : null;
            if (clip != null && OtpAuthUri.LooksLikeUri(clip)) { SetInput(clip.Trim()); return; }
            Ui.Info(this, Strings.Get(Strings.NoImage));
        }

        private void FromFile()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|*.*|*.*";
                if (d.ShowDialog(this) != DialogResult.OK) return;
                UseDecoded(ScreenScanner.FromFile(d.FileName));
            }
        }

        private void UseDecoded(string text)
        {
            if (text == null) { Ui.Info(this, Strings.Get(Strings.QrNotFound)); return; }
            if (text.StartsWith("otpauth-migration://", StringComparison.OrdinalIgnoreCase))
            {
                Ui.Info(this, Strings.Get(Strings.QrMigration));
                return;
            }
            if (!OtpAuthUri.LooksLikeUri(text)) { Ui.Info(this, Strings.Get(Strings.QrNotOtp)); return; }
            SetInput(text);
        }

        private void SetInput(string text)
        {
            m_input.Text = text;
            m_input.SelectionStart = 0;
        }

        private void SettingChanged()
        {
            if (m_updating) return;
            if (m_result != null && m_result.FromUri) m_overrideLink = true;
            Evaluate();
        }

        private void Evaluate()
        {
            if (m_updating) return;

            if (m_result != null && m_result.Parameters != null) m_result.Parameters.Clear();

            m_result = OtpInput.Evaluate(m_input.Text, (OtpAlgorithm)m_algorithm.SelectedIndex,
                (int)m_digits.Value, (int)m_period.Value, m_overrideLink);

            if (m_result.FromUri && m_result.Parameters != null && !m_overrideLink)
            {
                // A new link: show its settings. The user may still change them afterwards.
                OtpParameters p = m_result.Parameters;
                m_updating = true;
                m_algorithm.SelectedIndex = (int)p.Algorithm;
                // Steam (5 characters) and 9/10 digits are outside the field's range: keep the field.
                if (p.Digits >= m_digits.Minimum && p.Digits <= m_digits.Maximum) m_digits.Value = p.Digits;
                m_period.Value = p.Period;
                m_updating = false;
            }

            if (m_result.State == OtpInputState.Ok && !m_result.Native)
            {
                m_status.Set(PillKind.Warn, Strings.Format(Strings.StatusNotNativeOk, m_result.Message));
            }
            else if (m_result.Overridden && m_result.State == OtpInputState.Ok)
            {
                m_status.Set(PillKind.Warn, Strings.Format(Strings.StatusOverride, m_result.Message,
                    OtpAuthUri.AlgorithmName(m_result.LinkAlgorithm) + ", " + m_result.LinkDigits + ", " + m_result.LinkPeriod + " s"));
            }
            else switch (m_result.State)
            {
                case OtpInputState.Empty:
                    m_status.Set(PillKind.Neutral, Strings.Get(Strings.StatusEmpty)); break;
                case OtpInputState.Invalid:
                    m_status.Set(PillKind.Error, Strings.Format(Strings.StatusInvalid, m_result.Message)); break;
                case OtpInputState.NotStorable:
                    m_status.Set(PillKind.Error, Strings.Format(Strings.StatusNotNative, m_result.Message)); break;
                case OtpInputState.Short:
                    m_status.Set(PillKind.Warn, Strings.Format(Strings.StatusOk, m_result.Message) + "\r\n" +
                        Strings.Get(Strings.StatusShort)); break;
                default:
                    m_status.Set(PillKind.Ok, Strings.Format(Strings.StatusOk, m_result.Message)); break;
            }

            m_save.Enabled = m_result.State == OtpInputState.Ok || m_result.State == OtpInputState.Short;
            if (IsHandleCreated && Controls.Count > 1)
                ClientSize = new Size(ClientSize.Width, m_banner.Height + Controls[0].PreferredSize.Height + Font.Height * 4);
            UpdatePreview();
        }

        /// <summary>
        /// Copies the current code through KeePass's clipboard helper, so KeePass's settings apply
        /// (auto-clear countdown, exclusion from Windows clipboard history and cloud clipboard).
        /// </summary>
        private void CopyCode()
        {
            if (!m_save.Enabled || m_result == null || m_result.Parameters == null) return;
            string code = m_result.Parameters.CurrentCode(Totp.UnixNow());
            // bIsEntryInfo = true: treated as sensitive entry data by KeePass.
            if (ClipboardUtil.Copy(code, false, true, null, null, Handle))
            {
                MainForm mf = Program.MainForm;
                if (mf != null) mf.StartClipboardCountdown();
                m_code.ShowCopied();
            }
        }

        private void UpdatePreview()
        {
            m_copy.Enabled = m_save.Enabled;
            if (!m_save.Enabled || m_result == null || m_result.Parameters == null)
            {
                m_code.SetCode(null, 0, 0);
                return;
            }

            OtpParameters p = m_result.Parameters;
            long now = Totp.UnixNow();
            m_code.SetCode(p.CurrentCode(now),
                p.Type == OtpType.Totp ? Totp.SecondsRemaining(now, p.Period) : 0,
                p.Type == OtpType.Totp ? p.Period : 0);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            m_timer.Stop();
            m_timer.Dispose();
            // Wipe the visible secret without re-evaluating: Evaluate() would clear the result
            // that the caller is about to save.
            m_updating = true;
            m_input.Text = string.Empty;
            if (DialogResult != DialogResult.OK && m_result != null && m_result.Parameters != null)
                m_result.Parameters.Clear();
            base.OnFormClosed(e);
        }
    }
}
