using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TPU_Assembly.JobSelection
{
    public partial class JobSelectionForm : Form
    {
        public string SelectedJobName { get; private set; }
        public static string JobFolderPath = Path.Combine(Application.StartupPath, "models");

        public string FullJobFolderPath { get; private set; }

        // Khai báo Controls
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelJobs;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Panel panelHeader;

        private Panel selectedJobCard = null;

        private readonly Color AccentColor = Color.FromArgb(0, 120, 215);
        private readonly Color CardDefaultColor = Color.FromArgb(245, 245, 245);
        private readonly Color CardSelectedColor = Color.FromArgb(170, 210, 255);

        public JobSelectionForm()
        {
            InitializeJobSelectionComponent();
            SelectedJobName = null;
            this.Text = "Chọn Vision Job";
            this.StartPosition = FormStartPosition.CenterScreen;

            LoadJobsToGrid();

            this.btnSelect.Click += BtnSelect_Click;
            this.btnCancel.DialogResult = DialogResult.Cancel;

            if (flowLayoutPanelJobs.Controls.Count > 0)
            {
                SelectJobCard(flowLayoutPanelJobs.Controls[0] as Panel);
            }
        }

        private void InitializeJobSelectionComponent()
        {
            Font buttonFont = new Font("Segoe UI", 10F, FontStyle.Bold);

            this.flowLayoutPanelJobs = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSelect = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.panelHeader = new System.Windows.Forms.Panel();

            Label lblTitle = new Label();
            lblTitle.Text = "QUẢN LÝ VÀ CHỌN VISION JOB";
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(15, 10);
            lblTitle.AutoSize = true;

            this.SuspendLayout();

            this.BackColor = Color.White;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Size = new Size(800, 580);
            this.MinimumSize = new Size(800, 580);
            this.MaximumSize = new Size(800, 580);


            this.panelHeader.BackColor = Color.DarkBlue;
            this.panelHeader.Dock = DockStyle.Top;
            this.panelHeader.Height = 45;
            this.panelHeader.Controls.Add(lblTitle);

            this.panelBottom.Controls.Add(this.btnCancel);
            this.panelBottom.Controls.Add(this.btnSelect);
            this.panelBottom.Dock = DockStyle.Bottom;
            this.panelBottom.Height = 80;
            this.panelBottom.BackColor = Color.LightGray;

            this.btnSelect.Text = "LOAD JOB";
            this.btnSelect.Location = new Point(this.Width - 190, 15);
            this.btnSelect.Size = new Size(160, 50);
            this.btnSelect.Font = buttonFont;
            this.btnSelect.FlatStyle = FlatStyle.Flat;
            this.btnSelect.BackColor = Color.DarkBlue;
            this.btnSelect.ForeColor = Color.White;
            this.btnSelect.FlatAppearance.BorderSize = 0;

            this.btnCancel.Text = "HỦY";
            this.btnCancel.Location = new Point(this.Width - 360, 15);
            this.btnCancel.Size = new Size(160, 50);
            this.btnCancel.Font = buttonFont;
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.BackColor = Color.WhiteSmoke;
            this.btnCancel.ForeColor = Color.Black;
            this.btnCancel.FlatAppearance.BorderColor = Color.DarkGray;
            this.btnCancel.FlatAppearance.BorderSize = 1;

            this.flowLayoutPanelJobs.Dock = DockStyle.Fill;
            this.flowLayoutPanelJobs.AutoScroll = true;
            this.flowLayoutPanelJobs.BackColor = Color.WhiteSmoke;
            this.flowLayoutPanelJobs.Padding = new Padding(10);
            this.flowLayoutPanelJobs.FlowDirection = FlowDirection.LeftToRight;


            this.Controls.Add(this.flowLayoutPanelJobs);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelBottom);

            this.flowLayoutPanelJobs.BringToFront();

            this.ResumeLayout(false);
        }

        // --- HÀM TẠO CARD (BLOCK) CHO TỪNG JOB (ĐÃ SỬA) ---
        private Panel CreateJobCard(string modelName, string dateModified)
        {
            // Cấu hình kích thước và kiểu dáng Card (Kích thước được tối ưu)
            Panel card = new Panel
            {
                Width = 240, // Tăng chiều rộng
                Height = 140, // Tăng chiều cao
                Margin = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = CardDefaultColor,
                Cursor = Cursors.Hand,
                Tag = modelName
            };

            // Label Tên Model (Đã tăng chiều cao để cho phép xuống dòng)
            Label lblName = new Label
            {
                Text = modelName,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(5, 15),
                ForeColor = AccentColor,
                AutoSize = false,
                Width = card.Width - 10,
                Height = 40, // Tăng chiều cao lên 40px (đủ cho 2 dòng text)
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Label Đường gạch ngang (Vị trí đã điều chỉnh)
            Panel separator = new Panel
            {
                BackColor = Color.LightGray,
                Location = new Point(10, 60),
                Size = new Size(card.Width - 20, 1)
            };

            // Label Ngày Cập nhật (Vị trí đã điều chỉnh)
            Label lblDate = new Label
            {
                Text = $"Cập nhật: {dateModified}",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Location = new Point(10, 75),
                ForeColor = Color.DimGray,
                AutoSize = false,
                Width = card.Width - 20,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Thêm Controls vào Card
            card.Controls.Add(lblName);
            card.Controls.Add(separator);
            card.Controls.Add(lblDate);

            // Thêm sự kiện Click cho Card
            card.Click += JobCard_Click;
            lblName.Click += JobCard_Click;
            lblDate.Click += JobCard_Click;

            return card;
        }

        private void LoadJobsToGrid()
        {
            flowLayoutPanelJobs.Controls.Clear();


            if (!Directory.Exists(JobFolderPath))
            {
                Directory.CreateDirectory(JobFolderPath);
            }
            

            try
            {
                var jobData = Directory.GetFiles(JobFolderPath, "*.onnx")
                    .Select(filePath => new
                    {
                        ModelName = Path.GetFileNameWithoutExtension(filePath),
                        DateModified = File.GetLastWriteTime(filePath).ToString("dd/MM/yyyy HH:mm:ss"),
                    })
                    .OrderBy(j => j.ModelName)
                    .ToList();

                // Tạo và thêm Card vào FlowLayoutPanel
                foreach (var job in jobData)
                {
                    Panel card = CreateJobCard(job.ModelName, job.DateModified);
                    flowLayoutPanelJobs.Controls.Add(card);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách Job: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- LOGIC CHỌN CARD (BLOCK) (Giữ nguyên) ---
        private void JobCard_Click(object sender, EventArgs e)
        {
            // Tìm Panel gốc (Card) được click
            Panel clickedCard = sender as Panel;
            if (clickedCard == null)
            {
                // Nếu click vào Label, tìm Panel cha (Card)
                Control parent = (sender as Control).Parent;
                clickedCard = parent as Panel;
            }

            if (clickedCard != null)
            {
                SelectJobCard(clickedCard);
            }
        }

        private void SelectJobCard(Panel card)
        {
            if (card == null) return;

            if (selectedJobCard != null)
            {
                // Bỏ chọn Card cũ
                selectedJobCard.BackColor = CardDefaultColor;
            }

            // Chọn Card mới
            selectedJobCard = card;
            selectedJobCard.BackColor = CardSelectedColor;
            SelectedJobName = card.Tag.ToString();
            FullJobFolderPath = Path.Combine(JobFolderPath, SelectedJobName +".onnx");
        }


        private void BtnSelect_Click(object sender, EventArgs e)
        {
            if (selectedJobCard != null && !string.IsNullOrEmpty(SelectedJobName))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một Job từ danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}