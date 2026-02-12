using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TPU_Assembly_Inspection_Paddle;

namespace TPU_Assembly.Class
{
    public class Thumbnails
    {

        private readonly MAINFORM MainForm;

        public Thumbnails(MAINFORM mainForm)
        {
            MainForm = mainForm;
        }

        #region Thumbnail Management
        public async Task AddThumbnailToPanelAsync(string filePath)
        {
            try
            {
                PictureBox pbThumb = new PictureBox();
                pbThumb.Width = 140;
                pbThumb.Height = 120;
                pbThumb.SizeMode = PictureBoxSizeMode.Zoom;
                pbThumb.BorderStyle = BorderStyle.FixedSingle;
                pbThumb.Cursor = Cursors.Hand;
                pbThumb.BackColor = Color.White;
                pbThumb.Tag = filePath;
                pbThumb.Click += Thumbnail_Click;

                Image loadedImage = await Task.Run(() =>
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        MemoryStream ms = new MemoryStream(bytes);

                        using (var originalImage = Image.FromStream(ms))
                        {
                            return new Bitmap(originalImage, new Size(90, 90));
                        }
                    }
                    catch
                    {
                        return null;
                    }
                });

                if (loadedImage == null) return;

                pbThumb.Image = loadedImage;

                if (!MainForm.IsDisposed && MainForm.flowLayoutPanelThumbnails != null && !MainForm.flowLayoutPanelThumbnails.IsDisposed)
                {
                    MainForm.flowLayoutPanelThumbnails.Controls.Add(pbThumb);
                }
            }
            catch (Exception ex)
            {
                MainForm.Invoke(new Action(() =>
                {
                    MSystem.InsertAndSaveLogs($"Lỗi load ảnh {Path.GetFileName(filePath)}: {ex.Message}", Color.Red);
                }));
            }
        }

        public void Thumbnail_Click(object sender, EventArgs e)
        {
            PictureBox clickedPb = sender as PictureBox;
            if (clickedPb == null || clickedPb == MainForm._currentSelectedThumb) return;

            MainForm.flowLayoutPanelThumbnails.SuspendLayout();

            try
            {
                if (MainForm._currentSelectedThumb != null)
                {
                    MainForm._currentSelectedThumb.BackColor = Color.White;
                    MainForm._currentSelectedThumb.BorderStyle = BorderStyle.FixedSingle;
                    MainForm._currentSelectedThumb.Invalidate();
                }

                clickedPb.BackColor = Color.LimeGreen;
                clickedPb.BorderStyle = BorderStyle.Fixed3D;

                MainForm._currentSelectedThumb = clickedPb;

                ScrollThumbnailToCenter(clickedPb);
            }
            finally
            {
                MainForm.flowLayoutPanelThumbnails.ResumeLayout(true);
            }

            MainForm._name_file = clickedPb.Tag.ToString();
            try
            {
                var PictureBoxTarget = MainForm.pictureBox1;
                switch (MainForm.currentImportpictureBox)
                {
                    case "PB1": PictureBoxTarget = MainForm.pictureBox1; break;
                    case "PB2": PictureBoxTarget = MainForm.pictureBox2; break;
                    case "PB3": PictureBoxTarget = MainForm.pictureBox3; break;
                    case "": break;
                    default: break;
                }
                PictureBoxTarget.Image?.Dispose();

                Bitmap bmp = LoadBitmapWithoutLocking(MainForm._name_file);
                PictureBoxTarget.Image = bmp;
                MainForm.zoomable.FitImageToPictureBox(PictureBoxTarget);

                MSystem.InsertAndSaveLogs($"Đã chọn ảnh: {Path.GetFileName(MainForm._name_file)}", Color.Blue);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể load ảnh vào VisionPro: " + ex.Message);
            }
        }

        public static Bitmap LoadBitmapWithoutLocking(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return new Bitmap(fs);
            }
        }

        private void ScrollThumbnailToCenter(Control targetControl)
        {
            var panel = MainForm.flowLayoutPanelThumbnails;

            int panelCenter = panel.ClientSize.Width / 2;

            int targetCenter = targetControl.Width / 2;
            int currentScrollValue = panel.HorizontalScroll.Value;
            int targetRealX = targetControl.Left + currentScrollValue;

            int newScrollX = targetRealX - panelCenter + targetCenter;
            if (newScrollX < 0) newScrollX = 0;

            panel.AutoScrollPosition = new System.Drawing.Point(newScrollX, 0);
        }

        public void btnPreThumb()
        {
            if (MainForm.flowLayoutPanelThumbnails.Controls.Count == 0) return;

            int currentIndex = GetSelectedThumbnailIndex();

            int newIndex;
            if (currentIndex == -1)
            {
                newIndex = 0;
            }
            else if (currentIndex > 0)
            {
                newIndex = currentIndex - 1;
            }
            else
            {
                return;
            }

            Control prevControl = MainForm.flowLayoutPanelThumbnails.Controls[newIndex];
            Thumbnail_Click(prevControl, EventArgs.Empty);
        }

        public void btnNextThumb()
        {
            if (MainForm.flowLayoutPanelThumbnails.Controls.Count == 0) return;

            int currentIndex = GetSelectedThumbnailIndex();

            int newIndex;
            if (currentIndex == -1)
            {
                newIndex = 0;
            }
            else if (currentIndex < MainForm.flowLayoutPanelThumbnails.Controls.Count - 1)
            {
                newIndex = currentIndex + 1;
            }
            else
            {
                return;
            }

            Control nextControl = MainForm.flowLayoutPanelThumbnails.Controls[newIndex];
            Thumbnail_Click(nextControl, EventArgs.Empty);
        }

        private int GetSelectedThumbnailIndex()
        {
            for (int i = 0; i < MainForm.flowLayoutPanelThumbnails.Controls.Count; i++)
            {
                if (MainForm.flowLayoutPanelThumbnails.Controls[i] is PictureBox pb && pb.BackColor == Color.LimeGreen)
                {
                    return i;
                }
            }
            return -1;
        }

        public void SetupThumbnailUI()
        {
            EnableDoubleBuffering(MainForm.flowLayoutPanelThumbnails);
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, control, new object[] { true });
        }

        #endregion
    }
}
