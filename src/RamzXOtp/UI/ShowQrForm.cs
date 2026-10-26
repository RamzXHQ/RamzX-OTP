// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass.UI;

using RamzXOtp.Otp;
using RamzXOtp.Qr;

namespace RamzXOtp.UI
{
    /// <summary>Shows an entry's OTP as a QR code, e.g. to add it to a phone authenticator app.</summary>
    internal sealed class ShowQrForm : Form
    {
        private readonly PictureBox m_banner = new PictureBox();
        private readonly string m_title;
        private Bitmap m_qr;

        internal ShowQrForm(string entryTitle, OtpParameters p)
        {
            m_title = entryTitle;
            Text = Ui.Title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = SystemFonts.MessageBoxFont;
            BackColor = Color.White;
            if (Strings.RightToLeft) { RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; }

            m_banner.Dock = DockStyle.Top;
            m_banner.Height = Font.Height * 4;

            QrCode code = QrEncoder.Encode(OtpAuthUri.Format(p), QrEcLevel.M);
            int module = Math.Max(5, Font.Height / 2);
            m_qr = Render(code, module);
            int width = Math.Max(m_qr.Width, Font.Height * 26);

            PictureBox pic = new PictureBox();
            pic.Image = m_qr;
            pic.SizeMode = PictureBoxSizeMode.AutoSize;
            pic.Margin = new Padding((width - m_qr.Width) / 2, 0, 0, 0); // centered above the text

            Label warn = new Label();
            warn.Text = "⚠  " + Strings.Get(Strings.QrWarning);
            warn.AutoSize = true;
            warn.MaximumSize = new Size(width, 0);
            warn.ForeColor = Theme.Warn;
            warn.Margin = new Padding(0, Font.Height / 2, 0, 0);

            Button close = new Button();
            close.Text = Strings.Get(Strings.Close);
            close.DialogResult = DialogResult.Cancel;
            close.AutoSize = true;
            close.MinimumSize = new Size(Font.Height * 7, Font.Height * 2);
            close.Margin = new Padding(0, Font.Height, 0, 0);

            FlowLayoutPanel f = new FlowLayoutPanel();
            f.FlowDirection = FlowDirection.TopDown;
            f.AutoSize = true;
            f.Location = new Point(Font.Height, m_banner.Height + Font.Height);
            f.Controls.Add(pic);
            f.Controls.Add(warn);
            f.Controls.Add(close);

            Controls.Add(f);
            Controls.Add(m_banner);
            CancelButton = close;
            ClientSize = new Size(Math.Max(f.PreferredSize.Width, Font.Height * 24) + Font.Height * 2,
                m_banner.Height + f.PreferredSize.Height + Font.Height * 3);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            BannerFactory.CreateBannerEx(this, m_banner, Icons.Timer(48), Ui.Title,
                Strings.Format(Strings.ShowQrSubtitle, m_title));
        }

        internal static Bitmap Render(QrCode code, int module)
        {
            const int quiet = 4;
            int px = (code.Size + 2 * quiet) * module;
            Bitmap bmp = new Bitmap(px, px);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                for (int y = 0; y < code.Size; ++y)
                    for (int x = 0; x < code.Size; ++x)
                        if (code[x, y]) g.FillRectangle(Brushes.Black, (x + quiet) * module, (y + quiet) * module, module, module);
            }
            return bmp;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (m_qr != null) { m_qr.Dispose(); m_qr = null; }
            base.OnFormClosed(e);
        }
    }
}
