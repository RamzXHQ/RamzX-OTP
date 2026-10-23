// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass;
using KeePass.Forms;
using KeePass.Util;

using RamzXOtp.Otp;

namespace RamzXOtp.UI
{
    /// <summary>Paste an otpauth:// link or a Base32 secret, see the live code, save.</summary>
    internal sealed class AddOtpForm : Form
    {
        private readonly TextBox m_input = new TextBox();
        private readonly CheckBox m_show = new CheckBox();
        private readonly ComboBox m_algorithm = new ComboBox();
        private readonly NumericUpDown m_digits = new NumericUpDown();
        private readonly NumericUpDown m_period = new NumericUpDown();
        private readonly Label m_status = new Label();
        private readonly Label m_code = new Label();
        private readonly Label m_remaining = new Label();
        private readonly Button m_save = new Button();
        private readonly Button m_copy = new Button();
        private readonly Timer m_timer = new Timer();
        private bool m_updating;
        private ulong m_lastCounter = ulong.MaxValue;
        private OtpInput m_result;

        /// <summary>The parameters to save (valid after DialogResult.OK).</summary>
        internal OtpParameters Result { get { return m_result == null ? null : m_result.Parameters; } }

        internal AddOtpForm(string entryTitle)
        {
            Text = Strings.Format(Strings.FormTitle, entryTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);
            if (Strings.RightToLeft) { RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; }

            TableLayoutPanel t = new TableLayoutPanel();
            t.AutoSize = true;
            t.ColumnCount = 2;
            t.Dock = DockStyle.Fill;
            int row = 0;

            Label lblInput = AutoLabel(Strings.Get(Strings.InputLabel));
            t.Controls.Add(lblInput, 0, row); t.SetColumnSpan(lblInput, 2); ++row;

            m_input.Width = 420;
            m_input.UseSystemPasswordChar = true;
            m_input.TextChanged += delegate { Evaluate(); };
            t.Controls.Add(m_input, 0, row);
            m_show.Text = Strings.Get(Strings.ShowSecret);
            m_show.AutoSize = true;
            m_show.CheckedChanged += delegate { m_input.UseSystemPasswordChar = !m_show.Checked; };
            t.Controls.Add(m_show, 1, row); ++row;

            FlowLayoutPanel import = new FlowLayoutPanel();
            import.AutoSize = true;
            import.Margin = new Padding(0, 4, 0, 6);
            import.Controls.Add(ImportButton(Strings.ScanScreen, ScanScreen, true));
            import.Controls.Add(ImportButton(Strings.FromClipboard, FromClipboard, false));
            import.Controls.Add(ImportButton(Strings.FromFile, FromFile, false));
            t.Controls.Add(import, 0, row); t.SetColumnSpan(import, 2); ++row;

            m_algorithm.DropDownStyle = ComboBoxStyle.DropDownList;
            m_algorithm.Items.AddRange(new object[] { "SHA-1", "SHA-256", "SHA-512" });
            m_algorithm.SelectedIndex = 0;
            m_algorithm.SelectedIndexChanged += delegate { Evaluate(); };
            AddRow(t, ref row, Strings.Get(Strings.Algorithm), m_algorithm);

            m_digits.Minimum = Hotp.MinDigits;
            m_digits.Maximum = 8;
            m_digits.Value = 6;
            m_digits.ValueChanged += delegate { Evaluate(); };
            AddRow(t, ref row, Strings.Get(Strings.Digits), m_digits);

            m_period.Minimum = 1;
            m_period.Maximum = Totp.MaxPeriod;
            m_period.Value = Totp.DefaultPeriod;
            m_period.ValueChanged += delegate { Evaluate(); };
            AddRow(t, ref row, Strings.Get(Strings.Period), m_period);

            m_status.AutoSize = true;
            m_status.MaximumSize = new Size(520, 0);
            m_status.Margin = new Padding(0, 10, 0, 0);
            t.Controls.Add(m_status, 0, row); t.SetColumnSpan(m_status, 2); ++row;

            FlowLayoutPanel codeRow = new FlowLayoutPanel();
            codeRow.AutoSize = true;
            codeRow.Margin = new Padding(0, 8, 0, 0);
            codeRow.Controls.Add(AutoLabel(Strings.Get(Strings.PreviewLabel)));
            m_code.AutoSize = true;
            m_code.Font = new Font(FontFamily.GenericMonospace, 18f, FontStyle.Bold);
            codeRow.Controls.Add(m_code);
            m_remaining.AutoSize = true;
            m_remaining.Margin = new Padding(8, 10, 0, 0);
            codeRow.Controls.Add(m_remaining);
            m_copy.Text = Strings.Get(Strings.Copy);
            m_copy.AutoSize = true;
            m_copy.Margin = new Padding(12, 4, 0, 0);
            m_copy.Click += delegate { CopyCode(); };
            codeRow.Controls.Add(m_copy);
            m_code.Cursor = Cursors.Hand;
            m_code.Click += delegate { CopyCode(); };
            t.Controls.Add(codeRow, 0, row); t.SetColumnSpan(codeRow, 2); ++row;

            Label note = AutoLabel(Strings.Get(Strings.SameDbNote));
            note.MaximumSize = new Size(520, 0);
            note.ForeColor = SystemColors.GrayText;
            note.Margin = new Padding(0, 10, 0, 0);
            t.Controls.Add(note, 0, row); t.SetColumnSpan(note, 2); ++row;

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, 12, 0, 0);
            Button cancel = new Button();
            cancel.Text = Strings.Get(Strings.Cancel);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.AutoSize = true;
            m_save.Text = Strings.Get(Strings.Save);
            m_save.DialogResult = DialogResult.OK;
            m_save.AutoSize = true;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(m_save);
            t.Controls.Add(buttons, 0, row); t.SetColumnSpan(buttons, 2);

            AcceptButton = m_save;
            CancelButton = cancel;
            Controls.Add(t);

            m_timer.Interval = 1000;
            m_timer.Tick += delegate { UpdatePreview(); };
            m_timer.Start();
            Evaluate();
        }

        private static Label AutoLabel(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Margin = new Padding(0, 6, 8, 0);
            return l;
        }

        private Button ImportButton(string key, Action action, bool primary)
        {
            Button b = new Button();
            b.Text = Strings.Get(key);
            b.AutoSize = true;
            if (primary) b.Font = new Font(b.Font, FontStyle.Bold);
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

        private static void AddRow(TableLayoutPanel t, ref int row, string label, Control c)
        {
            t.Controls.Add(AutoLabel(label), 0, row);
            t.Controls.Add(c, 1, row);
            ++row;
        }

        private void Evaluate()
        {
            if (m_updating) return;

            if (m_result != null && m_result.Parameters != null) m_result.Parameters.Clear();

            m_result = OtpInput.Evaluate(m_input.Text, (OtpAlgorithm)m_algorithm.SelectedIndex,
                (int)m_digits.Value, (int)m_period.Value);

            if (m_result.FromUri && m_result.Parameters != null)
            {
                // Show the link's settings; the link itself is the source of truth.
                OtpParameters p = m_result.Parameters;
                m_updating = true;
                m_algorithm.SelectedIndex = (int)p.Algorithm;
                if (p.Digits <= m_digits.Maximum) m_digits.Value = p.Digits;
                m_period.Value = p.Period;
                m_updating = false;
            }
            m_algorithm.Enabled = m_digits.Enabled = m_period.Enabled = !m_result.FromUri;

            switch (m_result.State)
            {
                case OtpInputState.Empty:
                    SetStatus(Strings.Get(Strings.StatusEmpty), SystemColors.ControlText); break;
                case OtpInputState.Invalid:
                    SetStatus(Strings.Format(Strings.StatusInvalid, m_result.Message), Color.Firebrick); break;
                case OtpInputState.NotStorable:
                    SetStatus(Strings.Format(Strings.StatusNotNative, m_result.Message), Color.Firebrick); break;
                case OtpInputState.Short:
                    SetStatus(Strings.Format(Strings.StatusOk, m_result.Message) + "\r\n" +
                        Strings.Get(Strings.StatusShort), Color.DarkOrange); break;
                default:
                    SetStatus(Strings.Format(Strings.StatusOk, m_result.Message), Color.DarkGreen); break;
            }

            m_save.Enabled = m_result.State == OtpInputState.Ok || m_result.State == OtpInputState.Short;
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
                m_copy.Text = Strings.Get(Strings.Copied);
            }
        }

        private void SetStatus(string text, Color color)
        {
            m_status.Text = text;
            m_status.ForeColor = color;
        }

        private void UpdatePreview()
        {
            m_copy.Enabled = m_save.Enabled;
            if (!m_save.Enabled || m_result == null || m_result.Parameters == null)
            {
                m_code.Text = "------";
                m_remaining.Text = string.Empty;
                return;
            }
            if (m_lastCounter != Totp.Counter(Totp.UnixNow(), Math.Max(1, m_result.Parameters.Period)))
            {
                m_lastCounter = Totp.Counter(Totp.UnixNow(), Math.Max(1, m_result.Parameters.Period));
                m_copy.Text = Strings.Get(Strings.Copy); // new code: "copied" no longer true
            }

            OtpParameters p = m_result.Parameters;
            long now = Totp.UnixNow();
            string code = p.CurrentCode(now);
            int half = code.Length / 2;
            m_code.Text = code.Substring(0, half) + " " + code.Substring(half);
            m_remaining.Text = p.Type == OtpType.Totp
                ? Strings.Format(Strings.Remaining, Totp.SecondsRemaining(now, p.Period)) : string.Empty;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            m_timer.Stop();
            m_timer.Dispose();
            m_input.Text = string.Empty;
            if (DialogResult != DialogResult.OK && m_result != null && m_result.Parameters != null)
                m_result.Parameters.Clear();
            base.OnFormClosed(e);
        }
    }
}
