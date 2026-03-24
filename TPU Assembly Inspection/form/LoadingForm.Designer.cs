namespace TPU_Assembly_Inspection
{
    partial class LoadingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.ProgressBar progressBarLoading;
        private System.Windows.Forms.Panel mainPanel;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblMessage = new Label();
            progressBarLoading = new ProgressBar();
            mainPanel = new Panel();
            mainPanel.SuspendLayout();
            SuspendLayout();
            // 
            // lblMessage
            // 
            lblMessage.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblMessage.ForeColor = Color.FromArgb(64, 64, 64);
            lblMessage.Location = new Point(12, 29);
            lblMessage.Margin = new Padding(4, 0, 4, 0);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(439, 40);
            lblMessage.TabIndex = 0;
            lblMessage.Text = "Đang xử lý, vui lòng đợi...";
            lblMessage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // progressBarLoading
            // 
            progressBarLoading.Location = new Point(47, 87);
            progressBarLoading.Margin = new Padding(4, 3, 4, 3);
            progressBarLoading.MarqueeAnimationSpeed = 15;
            progressBarLoading.Name = "progressBarLoading";
            progressBarLoading.Size = new Size(369, 12);
            progressBarLoading.Style = ProgressBarStyle.Marquee;
            progressBarLoading.TabIndex = 1;
            // 
            // mainPanel
            // 
            mainPanel.BackColor = Color.White;
            mainPanel.Controls.Add(lblMessage);
            mainPanel.Controls.Add(progressBarLoading);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(2, 2);
            mainPanel.Margin = new Padding(4, 3, 4, 3);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(463, 134);
            mainPanel.TabIndex = 0;
            // 
            // LoadingForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(0, 122, 204);
            ClientSize = new Size(467, 138);
            Controls.Add(mainPanel);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4, 3, 4, 3);
            Name = "LoadingForm";
            Padding = new Padding(2);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "LoadingForm";
            TopMost = true;
            mainPanel.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion
    }
}