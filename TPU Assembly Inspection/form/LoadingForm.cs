using System;
using System.Drawing;
using System.Windows.Forms;

namespace TPU_Assembly_Inspection
{
    public partial class LoadingForm : Form
    {
        private Form darkBackground;

        public LoadingForm(string message = "Đang xử lý, vui lòng đợi...")
        {
            InitializeComponent();
            lblMessage.Text = message;
        }

        public void UpdateMessage(string newMessage)
        {
            if (lblMessage.InvokeRequired)
            {
                lblMessage.Invoke(new Action(() => lblMessage.Text = newMessage));
            }
            else
            {
                lblMessage.Text = newMessage;
            }
        }

        public void ShowWithOverlay(Form parent)
        {
            if (parent != null)
            {
                darkBackground = new Form();
                darkBackground.StartPosition = FormStartPosition.Manual;
                darkBackground.FormBorderStyle = FormBorderStyle.None;
                darkBackground.Opacity = 0.6;
                darkBackground.BackColor = Color.Black;
                darkBackground.ShowInTaskbar = false;

                darkBackground.Location = parent.PointToScreen(Point.Empty);
                darkBackground.Size = parent.ClientSize;

                darkBackground.Show(parent);

                this.Show(darkBackground);
            }
            else
            {
                this.Show();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (darkBackground != null)
            {
                this.Owner = null;

                darkBackground.Close();
                darkBackground.Dispose();
                darkBackground = null;
            }
        }
    }
}