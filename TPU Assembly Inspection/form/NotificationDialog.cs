using System;
using System.Drawing;
using System.Windows.Forms;

namespace TPU_Assembly_Inspection
{
    public partial class NotificationDialog : Form
    {
        private Panel panelHeader;
        private Panel panelBottom;
        private Label lblTitle;
        private Label lblMessage;
        private Label lblIcon;
        private Button btnOk;

        public NotificationDialog()
        {
            InitializeComponent(); 
        }

        public NotificationDialog(string title, string message, bool isSuccess = true)
        {
            InitializeComponent();
            SetupCustomUI(title, message, isSuccess); 
        }

        private void SetupCustomUI(string title, string message, bool isSuccess)
        {
            Font buttonFont = new Font("Segoe UI", 10F, FontStyle.Bold);

            this.panelHeader = new Panel();
            this.panelBottom = new Panel();
            this.lblTitle = new Label();
            this.lblMessage = new Label();
            this.lblIcon = new Label();
            this.btnOk = new Button();

            this.SuspendLayout();

            this.BackColor = Color.White;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Size = new Size(450, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = title;


            this.lblTitle.Text = title.ToUpper();
            this.lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.White;
            this.lblTitle.Location = new Point(15, 10);
            this.lblTitle.AutoSize = true;

            this.panelBottom.Dock = DockStyle.Bottom;

            this.btnOk.Text = "OK";
            this.btnOk.Size = new Size(160, 50);
            this.btnOk.Location = new Point((this.Width - 160) / 2 - 8, 15);
            this.btnOk.Font = buttonFont;
            this.btnOk.FlatStyle = FlatStyle.Flat;
            this.btnOk.BackColor = Color.DarkBlue;
            this.btnOk.ForeColor = Color.White;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.DialogResult = DialogResult.OK;
            this.btnOk.Cursor = Cursors.Hand;
            this.panelBottom.Controls.Add(btnOk);

            this.lblIcon.Text = isSuccess ? "✔" : "❌";
            this.lblIcon.Font = new Font("Segoe UI", 42F, FontStyle.Bold);

            this.lblIcon.ForeColor = isSuccess ? Color.MediumSeaGreen : Color.Crimson;
            this.lblIcon.Dock = DockStyle.Top;
            this.lblIcon.Height = 80;
            this.lblIcon.TextAlign = ContentAlignment.BottomCenter;

            this.lblMessage.Text = message;
            this.lblMessage.Font = new Font("Segoe UI", 11.5F, FontStyle.Regular);
            this.lblMessage.ForeColor = Color.Black;
            this.lblMessage.Dock = DockStyle.Fill;
            this.lblMessage.TextAlign = ContentAlignment.MiddleCenter;
            this.lblMessage.Padding = new Padding(20, 0, 20, 10);

            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.lblIcon);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelBottom);

            this.AcceptButton = this.btnOk;

            this.ResumeLayout(false);
        }

        public static void Show(string message, string title = "THÔNG BÁO", bool isSuccess = true)
        {
            using (NotificationDialog dialog = new(title, message, isSuccess))
            {
                dialog.ShowDialog();
            }
        }
    }
}