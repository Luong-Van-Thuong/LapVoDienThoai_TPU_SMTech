using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TPU_Assembly_Inspection_Paddle;

namespace TPU_Assembly.Class
{
    public class Zoomable
    {
        private readonly MAINFORM MainForm;
        public Zoomable(MAINFORM mainForm)
        {
            MainForm = mainForm;
        }

        public bool IsDrawingRoi;
        public PointF RoiStartPoint;
        public RectangleF CurrentDrawingRect;


        #region 10. Zoomable Image
        public void InitializePictureBoxEvents()
        {
            PictureBox[] pbs = { MainForm.pictureBox1, MainForm.pictureBox2, MainForm.pictureBox3};

            foreach (var pb in pbs)
            {
                if (!MainForm._viewStates.ContainsKey(pb))
                {
                    MainForm._viewStates.Add(pb, new ViewState());
                }

                pb.Paint -= Universal_Paint;
                pb.MouseDown -= Universal_MouseDown;
                pb.MouseMove -= Universal_MouseMove;
                pb.MouseUp -= Universal_MouseUp;
                pb.MouseWheel -= Universal_MouseWheel;
                pb.DoubleClick -= Universal_DoubleClick;

                pb.Paint += Universal_Paint;
                pb.MouseDown += Universal_MouseDown;
                pb.MouseMove += Universal_MouseMove;
                pb.MouseUp += Universal_MouseUp;
                pb.MouseWheel += Universal_MouseWheel;
                pb.DoubleClick += Universal_DoubleClick;
            }
        }



        private void Universal_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null || !MainForm._viewStates.ContainsKey(pb)) return;

            var state = MainForm._viewStates[pb];

            if (e.Button == MouseButtons.Left)
            {
                // Logic riêng cho PB1 khi đang vẽ ROI
                if (pb == MainForm.pictureBox1 && MainForm.IsRoiMode)
                {
                    IsDrawingRoi = true;
                    RoiStartPoint = ToImageCoordinates(pb, e.Location); // Cần sửa hàm này một chút
                    CurrentDrawingRect = new RectangleF(RoiStartPoint, new SizeF(0, 0));
                }
                else // Logic chung: Pan (kéo ảnh)
                {
                    state.IsDragging = true;
                    state.LastMousePos = e.Location;
                    pb.Cursor = Cursors.Hand;
                }
            }
        }

        private void Universal_MouseWheel(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null || !MainForm._viewStates.ContainsKey(pb)) return;

            var state = MainForm._viewStates[pb];
            float oldScale = state.ZoomScale;

            // Tính toán tỷ lệ zoom
            if (e.Delta > 0) state.ZoomScale *= 1.25f; // Tăng 25%
            else state.ZoomScale *= 0.8f; // Giảm 20%

            // Giới hạn zoom
            if (state.ZoomScale < 0.01f) state.ZoomScale = 0.01f;
            if (state.ZoomScale > 50.0f) state.ZoomScale = 50.0f;

            // Zoom tại vị trí con trỏ chuột (Math logic)
            float scaleFactor = state.ZoomScale / oldScale;
            state.ImagePos = new PointF(
                e.X - (e.X - state.ImagePos.X) * scaleFactor,
                e.Y - (e.Y - state.ImagePos.Y) * scaleFactor
            );

            pb.Invalidate(); // Vẽ lại
        }

        private void Universal_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null || pb.Image == null || !MainForm._viewStates.ContainsKey(pb)) return;

            var state = MainForm._viewStates[pb];

            // Cấu hình đồ họa cho mượt và nét
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.Clear(pb.BackColor);

            using (Matrix matrix = new Matrix())
            {
                // Áp dụng phép dịch chuyển và zoom
                matrix.Translate(state.ImagePos.X, state.ImagePos.Y);
                matrix.Scale(state.ZoomScale, state.ZoomScale);
                e.Graphics.Transform = matrix;

                // 1. Vẽ Ảnh gốc
                e.Graphics.DrawImage(pb.Image, 0, 0);

                // 2. Chỉ vẽ ROI/Text nếu là pictureBox1
                if (pb == MainForm.pictureBox1)
                {
                    float currentZoom = state.ZoomScale;

                    // Vẽ các vùng đã lưu
                    foreach (var zone in MainForm._inspectionZones)
                    {
                        using (Pen pen = new Pen(Color.Lime, 2 / currentZoom))
                        {
                            e.Graphics.DrawRectangle(pen, zone.Rect);
                        }
                        // Điều chỉnh font size theo zoom để chữ không bị quá to/nhỏ
                        using (Brush brush = new SolidBrush(Color.Yellow))
                        using (Font font = new Font("Arial", 14 / currentZoom, FontStyle.Bold))
                        {
                            e.Graphics.DrawString(zone.Name, font, brush, zone.Rect.X, zone.Rect.Y - (20 / currentZoom));
                        }
                    }

                    // Vẽ vùng đang kéo chuột (nếu có)
                    if (CurrentDrawingRect.Width > 0 && CurrentDrawingRect.Height > 0)
                    {
                        using (Pen pen = new Pen(Color.Red, 2 / currentZoom))
                        {
                            pen.DashStyle = DashStyle.Dash;
                            e.Graphics.DrawRectangle(pen, CurrentDrawingRect.X, CurrentDrawingRect.Y,CurrentDrawingRect.Width,CurrentDrawingRect.Height);
                        }
                    }
                }
            }
        }

        private void Universal_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null || !MainForm._viewStates.ContainsKey(pb)) return;

            var state = MainForm._viewStates[pb];

            // Case 1: Đang vẽ ROI trên PB1
            if (pb == MainForm.pictureBox1 && MainForm.IsRoiMode && IsDrawingRoi)
            {
                PointF currentPoint = ToImageCoordinates(pb, e.Location);

                float x = Math.Min(RoiStartPoint.X, currentPoint.X);
                float y = Math.Min(RoiStartPoint.Y, currentPoint.Y);
                float w = Math.Abs(RoiStartPoint.X - currentPoint.X);
                float h = Math.Abs(RoiStartPoint.Y - currentPoint.Y);

                CurrentDrawingRect = new RectangleF(x, y, w, h);
                pb.Invalidate();
            }
            // Case 2: Đang Pan ảnh (Kéo thả) - Áp dụng cho cả 4 PB
            else if (state.IsDragging)
            {
                int deltaX = e.X - state.LastMousePos.X;
                int deltaY = e.Y - state.LastMousePos.Y;

                state.ImagePos = new PointF(state.ImagePos.X + deltaX, state.ImagePos.Y + deltaY);
                state.LastMousePos = e.Location;

                pb.Invalidate();
            }
        }
        private void Universal_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null || !MainForm._viewStates.ContainsKey(pb)) return;

            var state = MainForm._viewStates[pb];

            // Case 1: Kết thúc vẽ ROI trên PB1
            if (pb == MainForm.pictureBox1 && IsDrawingRoi)
            {
                IsDrawingRoi = false;

                // Logic lưu vùng OCR cũ giữ nguyên
                int finalX = (int)CurrentDrawingRect.X;
                int finalY = (int)CurrentDrawingRect.Y;
                int finalW = (int)CurrentDrawingRect.Width;
                int finalH = (int)CurrentDrawingRect.Height;

                CurrentDrawingRect = RectangleF.Empty;

                if (finalW > 10 && finalH > 10)
                {
                    string name = ShowInputDialog("Nhập tên vùng:", "Lưu Vùng OCR");
                    if (!string.IsNullOrEmpty(name))
                    {
                        MainForm._inspectionZones.RemoveAll(z => z.Name == name);
                        MainForm._inspectionZones.Add(new InspectionZones
                        {
                            Name = name,
                            Rect = new Rectangle(finalX, finalY, finalW, finalH)
                        });
                        SaveOcrZones();
                    }
                }
                pb.Invalidate();
            }

            // Case 2: Kết thúc Pan ảnh
            if (state.IsDragging)
            {
                state.IsDragging = false;
                if (pb == MainForm.pictureBox1 && MainForm.IsRoiMode) pb.Cursor = Cursors.Cross;
                else pb.Cursor = Cursors.Default;
            }
        }
        private PointF ToImageCoordinates(PictureBox pb, System.Drawing.Point mousePoint)
        {
            if (!MainForm._viewStates.ContainsKey(pb)) return new PointF(0, 0);
            var state = MainForm._viewStates[pb];

            float imageX = (mousePoint.X - state.ImagePos.X) / state.ZoomScale;
            float imageY = (mousePoint.Y - state.ImagePos.Y) / state.ZoomScale;
            return new PointF(imageX, imageY);
        }
        public void FitImageToPictureBox(PictureBox pb)
        {
            if (pb.Image == null || !MainForm._viewStates.ContainsKey(pb)) return;

            var state = MainForm._viewStates[pb];

            float ratioX = (float)pb.Width / pb.Image.Width;
            float ratioY = (float)pb.Height / pb.Image.Height;

            state.ZoomScale = Math.Min(ratioX, ratioY); // Fit vừa khung

            float imgWidth = pb.Image.Width * state.ZoomScale;
            float imgHeight = pb.Image.Height * state.ZoomScale;

            // Căn giữa
            state.ImagePos = new PointF((pb.Width - imgWidth) / 2, (pb.Height - imgHeight) / 2);

            pb.Invalidate();
        }

        // Sự kiện Double Click chung
        private void Universal_DoubleClick(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb != null) FitImageToPictureBox(pb);
        }



        private string ShowInputDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, Width = 300 };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 280 };
            Button confirmation = new Button() { Text = "Save", Left = 220, Width = 80, Top = 90, DialogResult = DialogResult.OK };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        private void SaveOcrZones()
        {
            try
            {
                string json = JsonConvert.SerializeObject(MainForm._inspectionZones, Formatting.Indented);
                File.WriteAllText(MainForm._configFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu cấu hình: " + ex.Message);
            }
        }

        public void LoadOcrZones()
        {
            try
            {
                if (File.Exists(MainForm._configFile))
                {
                    string json = File.ReadAllText(MainForm._configFile);
                    var loadedZones = JsonConvert.DeserializeObject<List<InspectionZones>>(json);

                    if (loadedZones != null)
                    {
                        MainForm._inspectionZones = loadedZones;
                        MainForm.pictureBox1.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đọc cấu hình: " + ex.Message);
            }
        }

        #endregion
    }
}
