using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace TPU_Assembly_Inspection_Paddle
{
    public partial class frmSplash : Form
    {
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblStatus;
        private Panel pnlProgressContainer;
        private Panel pnlProgress;
        private System.Windows.Forms.Timer tmrAnimation;

        private Color colorBackground = Color.FromArgb(45, 45, 48);
        private Color colorAccent = Color.FromArgb(0, 122, 204);
        private Color colorTextMain = Color.White;
        private Color colorTextSub = Color.FromArgb(160, 160, 160);

        public frmSplash()
        {
            InitializeComponent();
            SetupCustomUI();

            this.Opacity = 0;
            tmrAnimation = new System.Windows.Forms.Timer();
            tmrAnimation.Interval = 20;
            tmrAnimation.Tick += TmrAnimation_Tick;
            tmrAnimation.Start();
            this.ShowInTaskbar = false;
        }

        private void SetupCustomUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(600, 350);
            this.BackColor = colorBackground;
            this.TopMost = true;
            this.DoubleBuffered = true;

            lblTitle = new Label();
            lblTitle.Text = "TPU Assembly Inspection";
            lblTitle.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblTitle.ForeColor = colorTextMain;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(50, 80);
            this.Controls.Add(lblTitle);

            lblSubtitle = new Label();
            lblSubtitle.Text = "Automated Inspection System v1.0";
            lblSubtitle.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            lblSubtitle.ForeColor = colorAccent;
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(55, 125);
            this.Controls.Add(lblSubtitle);

            lblStatus = new Label();
            lblStatus.Text = "Initializing core modules...";
            lblStatus.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            lblStatus.ForeColor = colorTextSub;
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(50, 270);
            this.Controls.Add(lblStatus);

            pnlProgressContainer = new Panel();
            pnlProgressContainer.Size = new Size(500, 4);
            pnlProgressContainer.Location = new Point(50, 295);
            pnlProgressContainer.BackColor = Color.FromArgb(60, 60, 60);
            this.Controls.Add(pnlProgressContainer);

            pnlProgress = new Panel();
            pnlProgress.Size = new Size(50, 4);
            pnlProgress.Location = new Point(0, 0);
            pnlProgress.BackColor = colorAccent;
            pnlProgressContainer.Controls.Add(pnlProgress);

            this.Paint += FrmSplash_Paint;
        }

        private void TmrAnimation_Tick(object sender, EventArgs e)
        {
            if (this.Opacity < 1)
            {
                this.Opacity += 0.05;
            }

            pnlProgress.Width += 10;
            if (pnlProgress.Width >= pnlProgressContainer.Width)
            {
                pnlProgress.Width = pnlProgress.Width;
            }
        }

        private void FrmSplash_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(colorAccent, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }

    public static class SplashScreenManager
    {
        private static Thread _splashThread;
        private static frmSplash _splashForm;
        private static ManualResetEvent _resetSplashCreated;

        public static void ShowSplash()
        {
            if (_splashThread != null) return;

            _resetSplashCreated = new ManualResetEvent(false);

            _splashThread = new Thread(() =>
            {
                _splashForm = new frmSplash();

                _splashForm.Load += (s, e) => _resetSplashCreated.Set();

                Application.Run(_splashForm);
            })
            {
                IsBackground = true
            };
            _splashThread.SetApartmentState(ApartmentState.STA);
            _splashThread.Start();
        }

        public static void CloseSplash()
        {
            if (_splashThread == null || _resetSplashCreated == null) return;

            _resetSplashCreated.WaitOne(2000);

            if (_splashForm == null || _splashForm.IsDisposed) return;

            try
            {
                if (_splashForm.InvokeRequired)
                {
                    _splashForm.Invoke(new MethodInvoker(() =>
                    {
                        CloseFormSafe();
                    }));
                }
                else
                {
                    CloseFormSafe();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi đóng Splash: " + ex.Message);
            }
            finally
            {
                _splashThread = null;
                _splashForm = null;
                _resetSplashCreated.Dispose();
                _resetSplashCreated = null;
            }
        }

        private static void CloseFormSafe()
        {
            if (_splashForm != null && !_splashForm.IsDisposed)
            {
                _splashForm.Close();
                _splashForm.Dispose();
            }
        }
    }
}