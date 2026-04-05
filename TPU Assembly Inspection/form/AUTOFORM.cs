using Lighting_ALT;         
using PaddleOCRSharp;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO.Compression;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TPU_Assembly.Class;
using TPU_Assembly.JobSelection;
using TPU_Assembly_Inspection;
using TPU_Assembly_Inspection.Properties;

namespace TPU_Assembly_Inspection_Paddle
{
    public partial class MAINFORM : Form
    {
        public volatile bool IsRunning = false;

        private YoloInferenceEngine inferenceEngine;

        public static bool SaveImageOrigin, SaveImageOK, SaveImageNG;

        public static int SaveLogDays, Port;

        public static bool AutoLoadModel;

        public static float ConfidenceThreshold;

        public static string IPAddress;

        public bool isLiveOn = false;

        public System.Windows.Forms.Timer liveTimer;

        public Zoomable zoomable;

        public Thumbnails thumbnails;

        public string currentLoadedJobName;

        public string lastLoadedJobFilePath;

        public PictureBox _currentSelectedThumb;

        private PerformanceCounter cpuCounter;

        private System.Windows.Forms.Timer timerPerformance;

        public string _name_file;

        private PaddleOCREngine _ocrEngine;

        private readonly Stopwatch stopWatch = new();
        private readonly Stopwatch stopWatch_Run = new();

        public List<InspectionZones> _inspectionZones = [];

        public bool IsRoiMode;

        public readonly string _configFile = "OcrZonesConfig.json";

        public readonly Dictionary<PictureBox, ViewState> _viewStates = [];

        public bool showROI = true;

        public string currentImportpictureBox = "";

        private TCP_Server _tcpServer;

        private readonly Font font = new("Arial", 150, FontStyle.Bold);
        private readonly Pen penBox = new(Color.Lime, 15);
        private readonly SolidBrush brushText = new(Color.White);
        private readonly SolidBrush brushBg = new(Color.Lime);

        public static Dictionary<string, CameraConfig> _cameraDict;

        private LightingSerialALT myLighting;


        public int TotalCount = 0;
        public int OKCount = 0;
        public int NGCount = 0;


        private static readonly object _lock = new object();

        public MAINFORM()
        {
            InitializeComponent();

            SplashScreenManager.ShowSplash();

            MSystem.SetRichTextLogs(this.richTextLog);

            _cameraDict = new Dictionary<string, CameraConfig>
            {
                {
                    "CAMERA1", new CameraConfig {
                        Name = "CAMERA1",
                        CameraInterface = BaslerCam.CAMERA1,
                        TargetPictureBox = pictureBox1
                    }
                },
                {
                    "CAMERA2", new CameraConfig {
                        Name = "CAMERA2",
                        CameraInterface = BaslerCam.CAMERA2,
                        TargetPictureBox = pictureBox2
                    }
                },
                {
                    "CAMERA3", new CameraConfig {
                        Name = "CAMERA3",
                        CameraInterface = BaslerCam.CAMERA3,
                        TargetPictureBox = pictureBox3
                    }
                }
            };

            LoadSystemSettings();

            zoomable = new Zoomable(this);

            thumbnails = new Thumbnails(this);

            InitializeLighting();

            CreateFolderFileDefault.CreateSaveFolders();

            UpdateStatusCamera();

            InitializePerformanceMonitor();

            zoomable.InitializePictureBoxEvents();
        }

        protected override void OnShown(EventArgs e)
        {
            SplashScreenManager.CloseSplash();
            base.OnShown(e);
            this.TopMost = true;
            this.TopMost = false;
        }

        private void MAINFORM_Load(object sender, EventArgs e)
        {

            numericGamma.Minimum = 0;
            numericGamma.Maximum = 99999999;

            numericGain.Minimum = 0;
            numericGain.Maximum = 99999999;
            
            numericExposure_Time.Minimum = 0;
            numericExposure_Time.Maximum = 99999999;


            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            Panel_Home.Visible = true;
            Panel_Teaching.Visible = false;
            Panel_Settings.Visible = false;

            zoomable.LoadOcrZones();
            Start_Server();
            AutoDeleteOldLogs();
            Menu_Strip();
            if (AutoLoadModel)
            {
                TaskAutoLoadModel();
            }
        }

        #region Open Server TCP/IP

        public void InitializeServer()
        {
            string ip = IPAddress;
            int port = Port;

            _tcpServer = new TCP_Server(ip, port);
            _tcpServer.OnClientConnected += OnClientConnected;
            _tcpServer.OnDataReceived += OnDataReceived;
            _tcpServer.OnClientDisconnected += OnClientDisconnected;
            _tcpServer.OnError += (msg) => MSystem.InsertAndSaveLogs(msg, Color.Red);
        }

        public void Start_Server()
        {
            if (_tcpServer == null) InitializeServer();

            btnRobot.BackColor = Color.Red;
            bool isStarted = _tcpServer.Start();

            if (isStarted)
            {
                MSystem.InsertAndSaveLogs($"Server opened: {_tcpServer.ServerIP}:{_tcpServer.ServerPort}", Color.Black);
            }
            else
            {
                MSystem.InsertAndSaveLogs("Server open failed!", Color.Red);
            }
        }

        private void OnClientConnected(string clientIP)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(OnClientConnected), clientIP);
                return;
            }
            btnRobot.BackColor = Color.Lime;
            MSystem.InsertAndSaveLogs($"Client Connected: {clientIP}", Color.Black);
        }

        private void OnClientDisconnected()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(OnClientDisconnected));
                return;
            }
            btnRobot.BackColor = Color.Red;
            MSystem.InsertAndSaveLogs($"Client is Disconncted", Color.Red);
        }
        private static Random _rand = new();

        private void OnDataReceived(string cmd)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnDataReceived(cmd)));
                return;
            }

            MSystem.InsertAndSaveLogs(cmd, Color.Blue);
            if (string.IsNullOrEmpty(cmd)) return;

            string data = cmd.Trim();

            //// xử lý data nhận được
            if (data.Contains("TRIGGER"))
            {
                if (!myLighting.MutilChannelON(myLighting.Brightness))
                {
                    MSystem.InsertAndSaveLogs("ERROR ON LIGHT", Color.Red);
                    _tcpServer.Send("GRAB_ERROR");
                    return;
                }
                Thread.Sleep(50);
                Run_Once();
            }

            //if (data.Contains("TRIGGER"))
            //{
            //    int r = _rand.Next(0, 6);

            //    string response = r switch
            //    {
            //        0 => "OK:A37GF12#6HT",
            //        1 => "NG_OCR:A26GF12#6HT",
            //        2 => "NG_TPU:A37GF12#6HT",
            //        3 => "NG_ALL:A26GF12#6HT",
            //        4 => "GRAB_ERROR",
            //        5 => "UNKNOWN_ERROR",
            //        _ => "UNKNOWN_ERROR"
            //    };

            //    _tcpServer.Send(response);
            //}
        }

        private async void Run_Once()
        {
            string OcrText = "";
            CameraProcessResult res1 = null;
            CameraProcessResult res2 = null;
            CameraProcessResult res3 = null;
            try
            {
                stopWatch_Run.Restart();

                if (inferenceEngine == null)
                {
                    MessageBox.Show("Inference Engine chưa được khởi tạo!");
                    return;
                }

                var taskCam1 = ProcessCameraAndAIAsync("CAMERA1");
                var taskCam2 = ProcessCameraAndAIAsync("CAMERA2");
                var taskCam3 = ProcessCameraAndAIAsync("CAMERA3");

                await Task.WhenAll(taskCam1, taskCam2, taskCam3);

                res1 = taskCam1.Result;
                res2 = taskCam2.Result;
                res3 = taskCam3.Result;

                if (res1.Status != "OK" || res2.Status != "OK" || res3.Status != "OK")
                {
                    string errorToSend = new[] { res1.Status, res2.Status, res3.Status }.FirstOrDefault(r => r != "OK");
                    _tcpServer.Send(errorToSend ?? "GRAB_ERROR");
                    MSystem.InsertAndSaveLogs("CAMERA PROCESSING FAILED", Color.Red);

                    this.Invoke(new Action(() =>
                    {
                        btnResult.BackColor = Color.Red;
                        btnResult.Text = "NG";
                    }));
                    return;
                }

                bool isCam2OK = res2.Detections.Count >= 3;
                bool isCam3OK = res3.Detections.Count >= 5;
                bool isOCROK = !string.IsNullOrEmpty(res1.OcrText);

                OcrText = res1.OcrText;

                List<string> errorCams = [];
                if (!isCam2OK) errorCams.Add("Camera 2");
                if (!isCam3OK) errorCams.Add("Camera 3");
                if (!isOCROK) errorCams.Add("Camera OCR");

                bool isAllOK = errorCams.Count == 0;
                string resultText = isAllOK ? "OK" : "NG";
                Color backColor = isAllOK ? Color.Lime : Color.Red;
                Color foreColor = isAllOK ? Color.Black : Color.White;

                float tongDienTichLoi = res2.Detections.Where(d => d.ClassName == "2").Sum(d => d.Area);

                if (isAllOK) _tcpServer.Send("OK:" + OcrText);
                else if (!isOCROK && errorCams.Count >= 2) _tcpServer.Send("NG_ALL:" + OcrText);
                else if (!isOCROK && errorCams.Count < 2) _tcpServer.Send("NG_OCR:" + OcrText);
                else if ((isOCROK && errorCams.Count > 0) || tongDienTichLoi < 200000) _tcpServer.Send("NG_TPU:" + OcrText);


                stopWatch_Run.Stop();
                this.Invoke(new Action(() =>
                {
                    BT_Time.Text = stopWatch_Run.ElapsedMilliseconds.ToString() + " ms";
                }));

                string logCamName = isAllOK ? "All Camera" : string.Join(" + ", errorCams);

                this.Invoke(new Action(() =>
                {
                    btnResult.Text = resultText;
                    btnResult.ForeColor = foreColor;
                    btnResult.BackColor = backColor;
                    btnOCR.Text = isOCROK ? res1.OcrText : "N/A";

                    UpdatePictureBoxWithClone(pictureBox1, res1.RawImage);
                    UpdatePictureBoxSafe(pictureBox2, res2.RawImage, res2.Detections);
                    UpdatePictureBoxSafe(pictureBox3, res3.RawImage, res3.Detections);

                    if (SaveImageOK || SaveImageNG)
                    {
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        SaveResultToDisk((Bitmap)pictureBox1.Image, $"OCR_{timestamp}", resultText);
                        SaveResultToDisk((Bitmap)pictureBox2.Image, $"Picture2_{timestamp}", resultText);
                        SaveResultToDisk((Bitmap)pictureBox3.Image, $"Picture3_{timestamp}", resultText);
                    }
                }));

                MSystem.InsertAndSaveLogs($"Result: {resultText}", isAllOK ? Color.Green : Color.Red);
                TotalCount++;
                UpdateResult(logCamName, isAllOK, res1.OcrText, "Complete");
                AutoDeleteOldLogs();

            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"Lỗi hệ thống trong Run Once: {ex.Message}", Color.Red);
                _tcpServer.Send("GRAB_ERROR");
                this.Invoke(new Action(() =>
                {
                    btnResult.BackColor = Color.Red;
                    btnResult.Text = "GRAB_ERROR";
                }));
            }
            finally
            {
                if (!myLighting.MutilChannelOFF())
                {
                    MSystem.InsertAndSaveLogs("ERROR OFF LIGHT", Color.Red);
                }
                res1?.RawImage?.Dispose();
                res2?.RawImage?.Dispose();
                res3?.RawImage?.Dispose();

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        private async Task<CameraProcessResult> ProcessCameraAndAIAsync(string camName)
        {
            return await Task.Run(() =>
            {
                var result = new CameraProcessResult();

                if (!_cameraDict.TryGetValue(camName, out var cameraConfig))
                {
                    MSystem.InsertAndSaveLogs($"Error: {camName} not in dictionary", Color.Red);
                    return result;
                }
                try
                {
                    Bitmap grabbedImg = CameraBasler.GrabImage(camName);                   
                    if (grabbedImg == null)
                    {
                        result.Status = "GRAB_ERROR";
                        return result;
                    }

                    if (camName == "CAMERA1")
                    {
                        grabbedImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }
                    result.RawImage = grabbedImg;

                    if (camName == "CAMERA1")
                    {
                        result.OcrText = GetResultFromZone(grabbedImg, "OCR");
                    }
                    else
                    {
                        result.Detections = inferenceEngine.RunInference(grabbedImg);
                    }

                    result.Status = "OK";
                }
                catch (Exception ex)
                {
                    MSystem.InsertAndSaveLogs($"Error {camName}: {ex.Message}", Color.Red);
                    result.RawImage?.Dispose();
                }
                return result;
            });
        }

        private void UpdatePictureBoxWithClone(PictureBox pb, Bitmap newImage)
        {
            if (pb == null || newImage == null) return;

            Image oldImg = pb.Image;
            pb.Image = (Bitmap)newImage.Clone();

            oldImg?.Dispose();

            zoomable.FitImageToPictureBox(pb);
        }

        public void UpdatePictureBoxSafe(PictureBox pb, Bitmap source, List<YoloInferenceEngine.Detection> detections)
        {
            if (detections == null) return;
            Bitmap newDrawnImage = DrawBoundingBoxes(source, detections);
            Image oldImage = pb.Image;
            pb.Image = newDrawnImage;
            if (oldImage != null && oldImage != pb.Tag)
            {
                oldImage.Dispose();
            }
            zoomable.FitImageToPictureBox(pb);
        }
        public Bitmap DrawBoundingBoxes(Bitmap originalImage, List<YoloInferenceEngine.Detection> detections)
        {
            Bitmap drawnImage = new Bitmap(originalImage.Width, originalImage.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(drawnImage))
            {
                g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                foreach (var item in detections)
                {
                    g.DrawRectangle(penBox, item.X, item.Y, item.Width, item.Height);

                    string label = $"{item.ClassName} ({item.Confidence:0.00}) ({item.Area})";
                    SizeF textSize = g.MeasureString(label, font);

                    float textX = item.X;
                    float textY = item.Y - textSize.Height;
                    if (textY < 0) textY = item.Y;

                    g.FillRectangle(brushBg, textX, textY, textSize.Width, textSize.Height);

                    g.DrawString(label, font, brushText, textX, textY);
                }
            }

            return drawnImage;
        }

        public string GetResultFromZone(Bitmap fullImage, string zoneName)
        {
            var zone = _inspectionZones.Find(z => z.Name == zoneName);
            if (zone == null) return "";

            try
            {
                Rectangle cropRect = zone.Rect;
                cropRect.Intersect(new Rectangle(0, 0, fullImage.Width, fullImage.Height));

                if (cropRect.Width == 0 || cropRect.Height == 0) return "";

                string resultText = "";
                Color boxColor = Color.Lime;

                using (Bitmap roi = fullImage.Clone(cropRect, fullImage.PixelFormat))
                {
                    if (zoneName.ToUpper().Contains("OCR") || zoneName.ToUpper().Contains("TEXT"))
                    {
                        if(_ocrEngine == null)
                        {
                            MessageBox.Show("Inference Engine chưa được khởi tạo!");
                            return "";
                        }

                        var ocrResult = _ocrEngine.DetectText(roi);
                        resultText = ocrResult != null ? ocrResult.Text.Trim() : "";

                        if (!string.IsNullOrEmpty(resultText))
                        {
                            resultText = Regex.Replace(resultText, @"[^a-zA-Z0-9#\-\><%\s]", "");

                            int index37 = resultText.IndexOf("37");
                            int index26 = resultText.IndexOf("26");

                            int targetIndex = -1;

                            if (index37 >= 0 && index26 >= 0)
                                targetIndex = Math.Min(index37, index26);
                            else if (index37 >= 0)
                                targetIndex = index37;
                            else if (index26 >= 0)
                                targetIndex = index26;

                            if (targetIndex >= 0)
                            {
                                if (targetIndex > 0 && resultText[targetIndex - 1] == 'A')
                                {
                                    resultText = resultText.Substring(targetIndex - 1);
                                }
                                else
                                {
                                    resultText = "A" + resultText.Substring(targetIndex);
                                }
                            }

                            if (!string.IsNullOrEmpty(resultText))
                            {
                                int indexLastBracket = resultText.LastIndexOf('<');
                                if (indexLastBracket >= 17)
                                {
                                    resultText = resultText.Substring(0, indexLastBracket + 1);
                                }
                            }
                        }

                    }
                }

                using (Graphics g = Graphics.FromImage(fullImage))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawRectangle(penBox, cropRect);

                    if (!string.IsNullOrEmpty(resultText))
                    {
                        string label = $"{zoneName}: {resultText}";

                        SizeF textSize = g.MeasureString(label, font);
                        float labelY = cropRect.Y - textSize.Height;
                        if (labelY < 0) labelY = cropRect.Y;

                        g.FillRectangle(brushBg, cropRect.X, labelY, textSize.Width, textSize.Height);
                        g.DrawString(label, font, brushText, cropRect.X, labelY);
                    }
                }

                return resultText;
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs("Lỗi xử lý OCR: " + ex.Message, Color.Red);
                return "";
            }
        }

        #endregion

        #region Lighting
        private void InitializeLighting()
        {
            myLighting = new LightingSerialALT();

            if (myLighting.IsConnected())
            {
                MSystem.InsertAndSaveLogs($"[Lighting] Đã kết nối thành công", Color.Green);
            }
        }

        private void btnLightingOn_Click(object sender, EventArgs e)
        {
            if (myLighting.IsConnected())
            {
                if (!myLighting.MutilChannelON(myLighting.Brightness)) MSystem.InsertAndSaveLogs("ERROR ON Light", Color.Red);
            }
        }

        private void btnLightingOff_Click(object sender, EventArgs e)
        {
            if (myLighting.IsConnected())
            {
                if (!myLighting.MutilChannelOFF()) MSystem.InsertAndSaveLogs("ERROR OFF Light", Color.Red);
            }
        }

        #endregion

        #region 0. Nén và giải nén models tránh trường hợp lỗi
        private async void btnExtract_Click(object sender, EventArgs e)
        {
            string modelsPath = Path.Combine(Application.StartupPath, "models");
            string zipFilePath = Path.Combine(modelsPath, "best.onnx.zip");

            if (!File.Exists(zipFilePath))
            {
                MSystem.InsertAndSaveLogs("Không tìm thấy file: " + zipFilePath, Color.Red);
                return;
            }

            ToggleUI(false);

            var progressReporter = new Progress<double>(percent =>
            {
                progressBar1.Value = (int)percent;
            });

            try
            {
                await Task.Run(() => ExtractZipWithProgress(zipFilePath, modelsPath, progressReporter));
                MSystem.InsertAndSaveLogs("Giải nén thành công!", Color.Green);
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs("Lỗi: " + ex.Message, Color.Red);
            }
            finally
            {
                ToggleUI(true);
            }
        }

        private async void btnAddtoArchive_Click(object sender, EventArgs e)
        {
            string sourceFilePath = Path.Combine(JobSelectionForm.JobFolderPath, "best.onnx");
            string zipDestPath = sourceFilePath + ".zip";

            if (!File.Exists(sourceFilePath))
            {
                MSystem.InsertAndSaveLogs("Không tìm thấy file gốc: " + sourceFilePath, Color.Red);
                return;
            }

            ToggleUI(false);

            var progressReporter = new Progress<double>(percent =>
            {
                progressBar1.Value = (int)percent;
            });

            try
            {
                await Task.Run(() => CompressFileWithProgress(sourceFilePath, zipDestPath, progressReporter));
                MSystem.InsertAndSaveLogs("Nén file thành công!", Color.Green);
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs("Lỗi: " + ex.Message, Color.Red);
            }
            finally
            {
                ToggleUI(true);
            }
        }

        private void ExtractZipWithProgress(string zipPath, string destDir, IProgress<double> progress)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                long totalBytes = archive.Entries.Sum(e => e.Length);
                long currentBytes = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    string destPath = Path.Combine(destDir, entry.FullName);

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                    using (Stream zipStream = entry.Open())
                    using (FileStream fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[81920];
                        int bytesRead;
                        while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            currentBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                double percentage = (double)currentBytes / totalBytes * 100;
                                progress.Report(percentage);
                            }
                        }
                    }
                }
            }
        }
        private void CompressFileWithProgress(string sourceFile, string zipPath, IProgress<double> progress)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);

            using (FileStream fsOut = new FileStream(zipPath, FileMode.Create))
            using (ZipArchive zip = new ZipArchive(fsOut, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = zip.CreateEntry(Path.GetFileName(sourceFile), CompressionLevel.Optimal);

                FileInfo fi = new FileInfo(sourceFile);
                long totalBytes = fi.Length;
                long currentBytes = 0;

                using (Stream entryStream = entry.Open())
                using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        entryStream.Write(buffer, 0, bytesRead);
                        currentBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            double percentage = (double)currentBytes / totalBytes * 100;
                            progress.Report(percentage);
                        }
                    }
                }
            }
        }
        private void ToggleUI(bool isEnabled)
        {
            btnExtract.Enabled = isEnabled;
            btnAddtoArchive.Enabled = isEnabled;
            Cursor = isEnabled ? Cursors.Default : Cursors.WaitCursor;
        }
        #endregion

        #region 1. Menu Nav

        private void btnHome_Click(object sender, EventArgs e)
        {
            ShowPanel(Panel_Home);
            SetActiveMenuButton(btnHome);
            Panel_Home.BringToFront();
        }
        private void btnTeaching_Click(object sender, EventArgs e)
        {
            ShowPanel(Panel_Teaching);
            SetActiveMenuButton(btnTeaching);
            Panel_Teaching.BringToFront();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (FullTouchKeyboard kboard = new FullTouchKeyboard("PASSWORD", true, "1"))
            {
                if (kboard.ShowDialog() == DialogResult.OK)
                {
                    ShowPanel(Panel_Settings);
                    SetActiveMenuButton(btnSettings);
                    Panel_Settings.BringToFront();
                }

            }
        }


        private void ShowPanel(Panel panel)
        {
            if (panel.Visible == true) return;
            Panel_Home.Visible = false;
            Panel_Teaching.Visible = false;
            Panel_Settings.Visible = false;
            panel.Visible = true;
        }

        private void SetActiveMenuButton(Button activeButton)
        {
            foreach (Control ctrl in Panel_Menu.Controls)
            {
                if (ctrl is Button btn && btn != btnStart && btn != btnStop)
                {
                    btn.BackColor = (btn == activeButton)
                        ? Color.DodgerBlue
                        : Color.DarkBlue;
                }
            }
        }
        #endregion

        #region 2. Hide and Exit
        private void BT_Hide_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;

        }

        private void BT_Exit_Click(object sender, EventArgs e)
        {
            if (IsRunning == false)
            {
                DialogResult result = MessageBox.Show("DO YOU WANT EXIT PROGRAM?", "Yes", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    CleanupResources();

                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("PLEASE STOP PROGRAM");
            }
        }

        private static void SafeDisposeCamera(ref ICameraInterface cam)
        {
            if (cam == null) return;
            try
            {
                cam.DestroyCamera();
            }
            catch { }
            finally
            {
                cam = null;
            }
        }
        private void CleanupResources()
        {
            if (isLiveOn)
            {
                liveTimer?.Stop();
                isLiveOn = false;
            }

            SafeDisposeCamera(ref BaslerCam.CAMERA1);
            SafeDisposeCamera(ref BaslerCam.CAMERA2);
            SafeDisposeCamera(ref BaslerCam.CAMERA3);

            timerPerformance?.Stop();
            timerPerformance?.Dispose();
            liveTimer?.Dispose();

            inferenceEngine?.Dispose();
            inferenceEngine = null;

            _ocrEngine?.Dispose();
            cpuCounter?.Dispose();
            cpuCounter = null;

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;
            pictureBox3.Image?.Dispose();
            pictureBox3.Image = null;
            pictureBox2.Image?.Dispose();
            pictureBox2.Image = null;
            _tcpServer?.Stop();

            myLighting?.Dispose();
        }

        #endregion

        #region 3. Load Models

        private async void btnLoadModel_Click(object sender, EventArgs e)
        {
            if (IsRunning)
            {
                MessageBox.Show("Please STOP Before Change Models");
                return;
            }

            ButtonIsEnableLoadJob(false);

            try
            {
                using (JobSelectionForm jobSelector = new())
                {
                    if (jobSelector.ShowDialog() == DialogResult.OK)
                    {
                        string selectedJob = jobSelector.FullJobFolderPath;
                        if (!string.IsNullOrEmpty(selectedJob))
                        {
                            LoadingForm loadingForm = new LoadingForm("Đang tải Model, vui lòng đợi...");
                            loadingForm.Owner = this;
                            loadingForm.ShowWithOverlay(this);

                            try
                            {
                                await Task.Run(() =>
                                {
                                    inferenceEngine?.Dispose();
                                    inferenceEngine = new YoloInferenceEngine(selectedJob);

                                    try
                                    {
                                        if (_ocrEngine == null)
                                        {
                                            OCRParameter ocrParam = new()
                                            {
                                                det_db_thresh = 0.5f,
                                                det_db_box_thresh = 0.5f
                                            };
                                            InspectionZones.SetupPaddlePaths();
                                            _ocrEngine = new PaddleOCREngine(null, ocrParam);
                                        }
                                    }
                                    catch
                                    {
                                        MSystem.InsertAndSaveLogs("PaddleOCR Engine Init Failed!", Color.Red);
                                    }
                                });

                                btnLoadModel.BackColor = Color.Lime;
                                MSystem.InsertAndSaveLogs("Load Models Successfully!", Color.Green);
                            }
                            finally
                            {
                                loadingForm.Close();
                                loadingForm.Dispose();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                btnLoadModel.BackColor = Color.Red;
                MSystem.InsertAndSaveLogs($"Load Models Fail: {ex}", Color.Red);
            }
            finally
            {
                if (inferenceEngine != null)
                {
                    btnStart_Click(null, null);
                }
                ButtonIsEnableLoadJob(true);
            }
        }

        private async void TaskAutoLoadModel()
        {
            string modelFolderPath = Path.Combine(Application.StartupPath, "models");
            string modelFilePath = Path.Combine(modelFolderPath, "best.onnx");

            if (!File.Exists(modelFilePath))
            {
                MSystem.InsertAndSaveLogs($"Không tìm thấy file model tại: {modelFilePath}", Color.Red);
                return;
            }
            try
            {
                ButtonIsEnableLoadJob(false);
                this.Cursor = Cursors.WaitCursor;

                LoadingForm loadingForm = new LoadingForm("Đang tải Model, vui lòng đợi...");
                loadingForm.Owner = this;
                loadingForm.ShowWithOverlay(this);

                try
                {
                    await Task.Run(() =>
                    {
                        inferenceEngine?.Dispose();
                        inferenceEngine = new YoloInferenceEngine(modelFilePath);

                        try
                        {
                            if (_ocrEngine == null)
                            {
                                OCRParameter ocrParam = new()
                                {
                                    det_db_thresh = 0.5f,
                                    det_db_box_thresh = 0.5f
                                };
                                InspectionZones.SetupPaddlePaths();
                                _ocrEngine = new PaddleOCREngine(null, ocrParam);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Invoke(new Action(() =>
                            {
                                MSystem.InsertAndSaveLogs($"PaddleOCR Engine Init Failed! {ex.Message}", Color.Red);
                            }));
                        }
                    });

                    btnLoadModel.BackColor = Color.Lime;
                    MSystem.InsertAndSaveLogs("Auto Load Models Successfully!", Color.Green);
                }
                finally
                {
                    loadingForm.Close();
                    loadingForm.Dispose();
                }
            }
            catch (Exception ex)
            {
                btnLoadModel.BackColor = Color.Red;
                MSystem.InsertAndSaveLogs($"Auto Load Models Fail: {ex.Message}", Color.Red);
            }
            finally
            {
                if (inferenceEngine != null)
                {
                    btnStart_Click(null, null);
                }
                ButtonIsEnableLoadJob(true);
                this.Cursor = Cursors.Default;
            }
        }

        internal void ButtonIsEnableLoadJob(bool isTrue)
        {
            DisableButtonsInContainer(this, isTrue);
        }

        private void DisableButtonsInContainer(Control parent, bool isEnabled)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.Enabled = isEnabled;
                }
                if (ctrl.HasChildren)
                {
                    DisableButtonsInContainer(ctrl, isEnabled);
                }
            }
        }

        #endregion

        #region 4. Save Configuration
        private void LoadSystemSettings()
        {
            ConfigurationSystem.ReloadSystemSettings();
            propertyGridSettings.SelectedObject = new VisionSettingsWrapper();
        }

        private void propertyGridSettings_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            try
            {
                ConfigurationSystem.SaveSystemSetting();
                MSystem.InsertAndSaveLogs("User changed setting: " + e.ChangedItem.Label, Color.Blue);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ConfigurationSystem.ReloadSystemSettings();
            }
        }

        #endregion

        #region 5. Check Connection Camera

        private void UpdateCameraStatusUI(string camName, bool isConnected)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateCameraStatusUI(camName, isConnected)));
                return;
            }

            Color statusColor = isConnected ? Color.Lime : Color.Red;

            if (camName == "CAMERA1") btnCamera1.BackColor = statusColor;
            else if (camName == "CAMERA2") btnCamera2.BackColor = statusColor;
            else if (camName == "CAMERA3") btnCamera3.BackColor = statusColor;

        }

        private void UpdateStatusCamera()
        {
            btnCamera1.BackColor = (CameraBasler.CheckConnectCam("CAMERA1")) ? Color.Lime : Color.Red;
            btnCamera2.BackColor = (CameraBasler.CheckConnectCam("CAMERA2")) ? Color.Lime : Color.Red;
            btnCamera3.BackColor = (CameraBasler.CheckConnectCam("CAMERA3")) ? Color.Lime : Color.Red;
            foreach (var cam in MAINFORM._cameraDict.Values)
            {
                if (cam.CameraInterface is BaslerCam baslerCam)
                {
                    baslerCam.ConnectionStatusChangedEvent += UpdateCameraStatusUI;
                }
            }

            if (CameraBasler.CheckConnectCam("CAMERA1"))
            {
                comboBoxCamera.Items.Add("CAMERA1");
            }
            if (CameraBasler.CheckConnectCam("CAMERA2"))
            {
                comboBoxCamera.Items.Add("CAMERA2");
            }
            if (CameraBasler.CheckConnectCam("CAMERA3"))
            {
                comboBoxCamera.Items.Add("CAMERA3");
            }
        }

        private void btnCamera1_Click(object sender, EventArgs e)
        {
            if (!CameraBasler.CheckConnectCam("CAMERA1")) CameraBasler.ReOpenCamera("CAMERA1");
        }

        private void btnCamera2_Click(object sender, EventArgs e)
        {
            if (!CameraBasler.CheckConnectCam("CAMERA2")) CameraBasler.ReOpenCamera("CAMERA2");
        }

        private void btnCamera3_Click(object sender, EventArgs e)
        {
            if (!CameraBasler.CheckConnectCam("CAMERA3")) CameraBasler.ReOpenCamera("CAMERA3");
        }

        #endregion

        #region 6. Teaching Camera
        private void Universal_GrabImage_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is not Button btn) return;
                string cameraName = btn.Name switch
                {
                    "BT_GrapImage1" => "CAMERA1",
                    "BT_GrapImage2" => "CAMERA2",
                    "BT_GrapImage3" => "CAMERA3",
                    _ => ""
                };

                if (!_cameraDict.TryGetValue(cameraName, out var cameraConfig)) return;

                PictureBox targetPB = cameraConfig.TargetPictureBox;

                if (!myLighting.MutilChannelON(myLighting.Brightness))
                {
                    MSystem.InsertAndSaveLogs("ERROR ON LIGHT", Color.Red);
                    //return;
                }

                Thread.Sleep(50);
                stopWatch.Restart();
                Bitmap newImage = CameraBasler.GrabImage(cameraName); // chụp manual
                stopWatch.Stop();
                BT_Time.Text = stopWatch.ElapsedMilliseconds.ToString() + " ms"; // đoạn này trả về 200ms

                if (newImage == null) return;

                if(cameraName == "CAMERA1") newImage.RotateFlip(RotateFlipType.Rotate270FlipNone);

                UpdateCameraImage(cameraName, newImage);


            }
            finally
            {
                if (!myLighting.MutilChannelOFF())
                {
                    MSystem.InsertAndSaveLogs("ERROR OFF LIGHT", Color.Red);
                }
            }
        }

        private string ShowSelectionDialog()
        {
            Form prompt = new()
            {
                Width = 300,
                Height = 150,
                Text = "Chọn PictureBox mục tiêu",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            FlowLayoutPanel panel = new() { Dock = DockStyle.Fill, Padding = new Padding(10) };
            string selected = "All";

            for (int i = 1; i <= 3; i++)
            {
                Button btn = new() { Text = i.ToString(), Width = 50, Height = 50 };
                int index = i;
                btn.Click += (s, e) => { selected = "PB" + index; prompt.Close(); };
                panel.Controls.Add(btn);
            }

            prompt.Controls.Add(panel);
            prompt.ShowDialog();
            return selected;
        }

        private async void Import_Multi_Image_Click(object sender, EventArgs e)
        {
            currentImportpictureBox = ShowSelectionDialog();

            if (currentImportpictureBox == "All")
            {
                currentImportpictureBox = "PB1";

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
                    ofd.Title = "Select Images to Import";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        Bitmap originalBmp = LoadBitmapWithoutLocking(ofd.FileName);

                        UpdateCameraImage("CAMERA1", new Bitmap(originalBmp));
                        UpdateCameraImage("CAMERA2", new Bitmap(originalBmp));
                        UpdateCameraImage("CAMERA3", new Bitmap(originalBmp));

                        originalBmp.Dispose();

                        zoomable.FitImageToPictureBox(pictureBox1);
                        zoomable.FitImageToPictureBox(pictureBox2);
                        zoomable.FitImageToPictureBox(pictureBox3);
                    }
                }

                return;
            }
            thumbnails.SetupThumbnailUI();
            using (OpenFileDialog ofd = new())
            {
                ofd.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
                ofd.Multiselect = true;
                ofd.Title = "Select Images to Import";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    ClearThumbnails(null, null);

                    foreach (string filePath in ofd.FileNames)
                    {
                        await thumbnails.AddThumbnailToPanelAsync(filePath);
                    }
                    btnNextThumb_Click(null, null);
                }
            }
        }

        public static Bitmap LoadBitmapWithoutLocking(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return new Bitmap(fs);
            }
        }

        private void Universal_Import_Click(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            PictureBox targetPB = null;
            string camName = "";

            switch (btn.Name)
            {
                case "Import_Image": targetPB = pictureBox1; camName = "CAMERA1"; break;
                case "Import_Image2": targetPB = pictureBox2; camName = "CAMERA2"; break;
                case "Import_Image3": targetPB = pictureBox3; camName = "CAMERA3"; break;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
                ofd.Title = $"Select Image for Camera {camName}";

                if (ofd.ShowDialog() == DialogResult.OK)
                {

                    Bitmap bmp = LoadBitmapWithoutLocking(ofd.FileName);
                    UpdateCameraImage(camName, bmp);
                    zoomable.FitImageToPictureBox(targetPB);
                }
            }
        }

        private void BT_Folder_Img_Click(object sender, EventArgs e)
        {
            string dateString = DateTime.Now.ToString("ddMMyyyy");
            string folderPath = $@"C:\FA\TPU_Assembly_Inspection_Paddle\Images\{dateString}";

            if (!Directory.Exists(folderPath))
            {
                CreateFolderFileDefault.CreateSaveFolders();
            }
            Process.Start("explorer.exe", folderPath);
        }
        #endregion

        #region Thumbnail Management
        private void btnPreThumb_Click(object sender, EventArgs e)
        {
            thumbnails.btnPreThumb();
        }
        private void btnNextThumb_Click(object sender, EventArgs e)
        {
            thumbnails.btnNextThumb();
        }

        public void ClearThumbnails(object sender, EventArgs e)
        {
            pictureBox1.Image?.Dispose();
            pictureBox3.Image?.Dispose();
            pictureBox2.Image?.Dispose();
            pictureBox1.Image = null;
            pictureBox3.Image = null;
            pictureBox2.Image = null;

            foreach (Control ctrl in flowLayoutPanelThumbnails.Controls)
            {
                if (ctrl is PictureBox pb)
                {
                    pb.Image?.Dispose();
                    pb.Dispose();
                }
            }
            flowLayoutPanelThumbnails.Controls.Clear();
            GC.Collect();
        }

        #endregion

        #region 7. MenuStrip
        private void Universal_Clear_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is not ToolStripMenuItem btn) return;
                PictureBox targetPB = null;
                switch (btn.Name)
                {
                    case "Clear_Image": targetPB = pictureBox1; break;
                    case "Clear_Image2": targetPB = pictureBox2; break;
                    case "Clear_Image3": targetPB = pictureBox3; break;
                }

                if (targetPB != null)
                {
                    targetPB.Image?.Dispose();
                    targetPB.Image = null;
                }
            }
            finally
            {
                GC.Collect();
            }
        }

        private void Universal_FitImage_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is not ToolStripMenuItem btn) return;
                PictureBox targetPB = null;
                switch (btn.Name)
                {
                    case "Fit_Image": targetPB = pictureBox1; break;
                    case "Fit_Image2": targetPB = pictureBox2; break;
                    case "Fit_Image3": targetPB = pictureBox3; break;
                }

                if (targetPB != null)
                {
                    if (targetPB.Image != null) { zoomable.FitImageToPictureBox(targetPB); }
                }
            }
            finally
            {
                GC.Collect();
            }
        }


        private void listROIstrip_Click(object sender, EventArgs e)
        {
            zoomable?.ShowROIManagerDialog();
        }

        private void showROIstrip_Click(object sender, EventArgs e)
        {
            showROI = !showROI;
            zoomable.FitImageToPictureBox(pictureBox1);
            Menu_Strip();
        }

        private void Menu_Strip()
        {
            showROIstrip.Image = showROI ? Resources.Tick : null;
        }

        #endregion

        #region 8. Counter

        private void btnClearCounter_Click(object sender, EventArgs e)
        {
            TotalCount = 0;
            OKCount = 0;
            NGCount = 0;

            LabelOK.Text = LabelNG.Text = labelTotal.Text = "0";
            percentNG.Text = percentOK.Text = "0.0%";

            MSystem.InsertAndSaveLogs("Reset bộ đếm", Color.Blue);
        }
        #endregion

        #region 9. Perfomance
        private void InitializePerformanceMonitor()
        {
            try
            {
                if (timerPerformance == null)
                {
                    timerPerformance = new System.Windows.Forms.Timer
                    {
                        Interval = 1000
                    };
                    timerPerformance.Tick += TimerPerformance_Tick;
                }

                string currentProcessName = Process.GetCurrentProcess().ProcessName;

                cpuCounter = new PerformanceCounter("Process", "% Processor Time", currentProcessName);

                timerPerformance.Start();
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs("Không thể khởi tạo Performance Monitor: " + ex.Message, Color.Red);
            }
        }

        private void TimerPerformance_Tick(object sender, EventArgs e)
        {
            try
            {
                Process currentProc = Process.GetCurrentProcess();
                currentProc.Refresh();

                double ramUsageMB = currentProc.PrivateMemorySize64 / (1024.0 * 1024.0);

                lblMonitorRAM.Text = $"RAM: {ramUsageMB:F1} MB";

                if (ramUsageMB > 1500) lblMonitorRAM.ForeColor = Color.Red;
                else lblMonitorRAM.ForeColor = Color.Black;

                float cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;

                lblMonitorCPU.Text = $"CPU: {cpuUsage:F1} %";

                if (cpuUsage > 80) lblMonitorCPU.ForeColor = Color.Red;
                else lblMonitorCPU.ForeColor = Color.Black;
            }
            catch
            {
            }
        }
        #endregion

        #region 10. Select ROI
        public void btnSelcectROI_Click(object sender, EventArgs e)
        {
            IsRoiMode = !IsRoiMode;
            if (IsRoiMode) pictureBox1.Cursor = Cursors.Cross;
            else pictureBox1.Cursor = Cursors.Default;
            ButtonIsEnableLoadJob(!IsRoiMode);
        }

        #endregion

        #region 11. Save Result Image & Logs

        private static void SaveResultToDisk(Bitmap image, string baseFileName, string type)
        {
            try
            {
                bool isAllowed = false;
                if (type == "OK" && SaveImageOK) isAllowed = true;
                else if (type == "NG" && SaveImageNG) isAllowed = true;

                if (!isAllowed) return;

                EnsureImageDirectories(out string pathOrigin, out string pathOK, out string pathNG);

                string destFolder = "";
                switch (type)
                {
                    case "OK": destFolder = pathOK; break;
                    case "NG": destFolder = pathNG; break;
                    default: return;
                }
                string savePath = Path.Combine(destFolder, baseFileName + ".jpg");
                image?.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);


            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"Save Error ({type}): {ex.Message}", Color.Red);
            }
        }

        private void AutoDeleteOldLogs()
        {
            Task.Run(() =>
            {
                try
                {
                    string baseImagesPath = @"C:\FA\TPU_Assembly_Inspection_Paddle\Images";
                    if (!Directory.Exists(baseImagesPath)) return;

                    string[] directories = Directory.GetDirectories(baseImagesPath);
                    DateTime thresholdDate = DateTime.Now.AddDays(-SaveLogDays);

                    foreach (string dirPath in directories)
                    {
                        string folderName = Path.GetFileName(dirPath);

                        if (DateTime.TryParseExact(folderName, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime folderDate))
                        {
                            if (folderDate < thresholdDate)
                            {
                                try
                                {
                                    Directory.Delete(dirPath, true);
                                    Debug.WriteLine($"Đã xóa thư mục cũ: {folderName}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Không thể xóa {folderName}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi thực hiện dọn dẹp tự động: " + ex.Message);
                }
            });
        }

        private void SaveProductionLog(string cameraName, bool isOK, string OCR, string note = "")
        {
            try
            {
                string logDir = Path.Combine(@"C:\FA\TPU_Assembly_Inspection_Paddle", "ProductionLogs");
                Directory.CreateDirectory(logDir);

                // Tên file theo định dạng: Log_20231025.csv
                string fileName = $"Log_{DateTime.Now:yyyyMMdd}.csv";
                string filePath = Path.Combine(logDir, fileName);

                if (!File.Exists(filePath))
                {
                    File.AppendAllText(filePath, "Time,Camera,Result,OCR,Note\n");
                }

                string resultStr = isOK ? "OK" : "NG";
                string logLine = $"{DateTime.Now:HH:mm:ss},{cameraName},{resultStr},{OCR},{note}\n";

                File.AppendAllText(filePath, logLine);
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"Lỗi khi lưu Log sản xuất: {ex.Message}", Color.Red);
            }
        }

        private void UpdateResult(string camName, bool isOK, string OCR, string note)
        {
            if (isOK) OKCount++;
            else NGCount++;

            this.Invoke(new Action(() =>
            {
                labelTotal.Text = TotalCount.ToString();
                LabelOK.Text = OKCount.ToString();
                LabelNG.Text = NGCount.ToString();

                percentOK.Text = TotalCount > 0 ? $"{(OKCount * 100.0 / TotalCount):0.00} %" : "0.0 %";
                percentNG.Text = TotalCount > 0 ? $"{(NGCount * 100.0 / TotalCount):0.00} %" : "0.0 %";
            }));

            SaveProductionLog(camName, isOK, OCR, note);

        }


        #endregion

        #region 12. Run

        private PictureBox GetPictureBox(string camName)
        {
            return camName switch
            {
                "CAMERA1" => pictureBox1,
                "CAMERA2" => pictureBox2,
                "CAMERA3" => pictureBox3,
                _ => null,
            };
        }

        public void UpdateCameraImage(string camName, Bitmap rawImage)
        {
            PictureBox pb = GetPictureBox(camName);
            if (pb == null || rawImage == null) return;

            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => UpdateCameraImage(camName, rawImage)));
                return;
            }

            pb.Image?.Dispose();
            pb.Image = null;

            if (pb.Tag is Bitmap oldTag)
            {
                oldTag.Dispose();
                pb.Tag = null;
            }

            pb.Image = new Bitmap(rawImage);
            pb.Tag = rawImage;
            zoomable.FitImageToPictureBox(pb);
        }


        private async void Run_All_PictureBox_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null || pictureBox2.Image == null ||
                pictureBox3.Image == null) return;

            if (inferenceEngine == null)
            {
                MessageBox.Show("Inference Engine chưa được khởi tạo!");
                return;
            }
            static Bitmap GetCleanImage(PictureBox pb)
            {
                if (pb.Tag is Bitmap cleanBmp) return new Bitmap(cleanBmp);
                if (pb.Image != null) return new Bitmap(pb.Image);
                return null;
            }
            Bitmap bm1 = GetCleanImage(pictureBox1);
            Bitmap bm2 = GetCleanImage(pictureBox2);
            Bitmap bm3 = GetCleanImage(pictureBox3);

            string ocrResult = "";
            List<YoloInferenceEngine.Detection> det2 = null;
            List<YoloInferenceEngine.Detection> det3 = null;


            try
            {
                stopWatch.Restart();

                var t1 = Task.Run(() => ocrResult = GetResultFromZone(bm1, "OCR"));
                var t2 = Task.Run(() => det2 = inferenceEngine.RunInference(bm2));
                var t3 = Task.Run(() => det3 = inferenceEngine.RunInference(bm3));

                await Task.WhenAll(t1, t2, t3);

                stopWatch.Stop();
                BT_Time.Text = stopWatch.ElapsedMilliseconds.ToString() + " ms";

                this.Invoke(new Action(() =>
                {
                    btnOCR.Text = (!string.IsNullOrEmpty(ocrResult)) ? ocrResult : "N/A";
                    Image oldImg = pictureBox1.Image;
                    pictureBox1.Image = (Bitmap)bm1.Clone();
                    if (oldImg != null && oldImg != pictureBox1.Tag)
                    {
                        oldImg.Dispose();
                    }
                }));

                UpdatePictureBoxSafe(pictureBox2, bm2, det2);
                UpdatePictureBoxSafe(pictureBox3, bm3, det3);

                bool isCam2OK = det2.Count >= 3;
                bool isCam3OK = det3.Count >= 5;
                bool isOCROK = !string.IsNullOrEmpty(ocrResult);

                List<string> errorCams = [];
                if (!isCam2OK) errorCams.Add("Camera 2");
                if (!isCam3OK) errorCams.Add("Camera 3");
                if (!isOCROK) errorCams.Add("Camera OCR");

                bool isAllOK = errorCams.Count == 0;
                string resultText = isAllOK ? "OK" : "NG";
                Color backColor = isAllOK ? Color.Lime : Color.Red;
                Color foreColor = isAllOK ? Color.Black : Color.White;

                float tongDienTichLoi = det2.Where(d => d.ClassName == "2").Sum(d => d.Area);

                if (isAllOK)
                {
                    _tcpServer.Send("OK");
                }
                else if (!isOCROK && errorCams.Count >= 2)
                {
                    _tcpServer.Send("NG_ALL");
                }
                else if (!isOCROK && errorCams.Count < 2)
                {
                    _tcpServer.Send("NG_OCR");
                }
                else if ((isOCROK && errorCams.Count > 0) || tongDienTichLoi < 200000)
                {
                    _tcpServer.Send("NG_TPU");
                }

                string logCamName = isAllOK ? "All Camera" : string.Join(" + ", errorCams);

                this.Invoke(new Action(() =>
                {
                    btnResult.Text = resultText;
                    btnResult.ForeColor = foreColor;
                    btnResult.BackColor = backColor;
                }));

                MSystem.InsertAndSaveLogs($"Result: {resultText}", isAllOK ? Color.Green : Color.Red);
                TotalCount++; // tăng số lượng sản phẩm
                UpdateResult(logCamName, isAllOK, ocrResult, "Complete");

                if (SaveImageOK || SaveImageNG)
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string baseName_OCR = $"OCR_{timestamp}";
                    string baseName_Picture2 = $"Picture2_{timestamp}";
                    string baseName_Picture3 = $"Picture3_{timestamp}";
                    SaveResultToDisk((Bitmap)pictureBox1.Image, baseName_OCR, resultText);      // lưu 3 ảnh
                    SaveResultToDisk((Bitmap)pictureBox2.Image, baseName_Picture2, resultText); // lưu 3 ảnh
                    SaveResultToDisk((Bitmap)pictureBox3.Image, baseName_Picture3, resultText); // lưu 3 ảnh
                }
                AutoDeleteOldLogs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xử lý: " + ex.Message);
            }
            finally
            {
                bm1?.Dispose();
                bm2?.Dispose();
                bm3?.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            }
        }
        private async void Run_Vision_CAMERA1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null) return;

            Bitmap workingImage = new(pictureBox1.Image);

            try
            {
                ButtonIsEnableLoadJob(false);

                string fileName = !string.IsNullOrEmpty(_name_file) ? Path.GetFileNameWithoutExtension(_name_file) : "LiveImage";
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseName = $"{timestamp}_{fileName}";

                stopWatch.Restart();

                var resultData = await Task.Run(() =>
                {
                    string result = GetResultFromZone(workingImage, "OCR");
                    return new { Result = result };
                });

                stopWatch.Stop();
                BT_Time.Text = stopWatch.ElapsedMilliseconds.ToString() + " ms";
                btnOCR.Text = (!string.IsNullOrEmpty(resultData.Result)) ? resultData.Result : "N/A";

                var oldImage = pictureBox1.Image;

                pictureBox1.Image = workingImage;
                if (oldImage != null && oldImage != workingImage)
                {
                    oldImage.Dispose();
                }

                string status = !string.IsNullOrEmpty(resultData.Result) ? "OK" : "NG";

                SaveResultToDisk(workingImage, baseName, status);
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"Error Process: {ex.Message}", Color.Red);
                workingImage?.Dispose();
            }
            finally
            {
                ButtonIsEnableLoadJob(true);
            }
        }
        private async void Run_Vision_CAMERA2_Click(object sender, EventArgs e)
        {
            if (pictureBox2.Image == null) return;
            if (inferenceEngine == null)
            {
                MessageBox.Show("Chưa Load Model.");
                return;
            }

            Bitmap workingImage = new(pictureBox2.Image);

            try
            {
                ButtonIsEnableLoadJob(false);
                string fileName = !string.IsNullOrEmpty(_name_file) ? Path.GetFileNameWithoutExtension(_name_file) : "LiveImage";
                string baseName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}";

                stopWatch.Restart();

                var resultData = await Task.Run(() =>
                {
                    var detections = inferenceEngine.RunInference(workingImage);

                    Bitmap drawnImage = DrawBoundingBoxes(workingImage, detections);

                    return new { Detections = detections, ResultImage = drawnImage };
                });

                stopWatch.Stop();
                BT_Time.Text = stopWatch.ElapsedMilliseconds.ToString() + " ms";

                var oldImage = pictureBox2.Image;

                pictureBox2.Image = resultData.ResultImage;

                oldImage?.Dispose();

                workingImage.Dispose();

                string status = (resultData.Detections.Count >= 3) ? "OK" : "NG";

                SaveResultToDisk((Bitmap)pictureBox2.Image, baseName, status);

            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"Error Process Cam2: {ex.Message}", Color.Red);
                workingImage?.Dispose();
            }
            finally
            {
                ButtonIsEnableLoadJob(true);
            }
        }

        private async void Run_Vision_CAMERA3_Click(object sender, EventArgs e)
        {
            if (pictureBox3.Image == null) return;

            Bitmap workingImage = new(pictureBox3.Image);

            try
            {
                ButtonIsEnableLoadJob(false);
                string fileName = !string.IsNullOrEmpty(_name_file) ? Path.GetFileNameWithoutExtension(_name_file) : "LiveImage";
                string baseName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}";

                stopWatch.Restart();

                var resultData = await Task.Run(() =>
                {
                    var detections = inferenceEngine.RunInference(workingImage);

                    Bitmap drawnImage = DrawBoundingBoxes(workingImage, detections);

                    return new { Detections = detections, ResultImage = drawnImage };
                });

                stopWatch.Stop();
                BT_Time.Text = stopWatch.ElapsedMilliseconds.ToString() + " ms";

                var oldImage = pictureBox3.Image;

                pictureBox3.Image = resultData.ResultImage;

                oldImage?.Dispose();

                workingImage.Dispose();

                string status = (resultData.Detections.Count >= 5) ? "OK" : "NG";

                SaveResultToDisk((Bitmap)pictureBox3.Image, baseName, status);

            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"Error Process Cam2: {ex.Message}", Color.Red);
                workingImage?.Dispose();
            }
            finally
            {
                ButtonIsEnableLoadJob(true);
            }
        }

        private static void EnsureImageDirectories(out string originPath, out string okPath, out string ngPath)
        {
            string dateString = DateTime.Now.ToString("ddMMyyyy");
            string baseImagesPath = @"C:\FA\TPU_Assembly_Inspection_Paddle\Images";
            string dateFolderPath = Path.Combine(baseImagesPath, dateString);

            if (!Directory.Exists(dateFolderPath))
            {
                Directory.CreateDirectory(dateFolderPath);
            }

            originPath = Path.Combine(dateFolderPath, "Origin");
            okPath = Path.Combine(dateFolderPath, "OK");
            ngPath = Path.Combine(dateFolderPath, "NG");

            Directory.CreateDirectory(originPath);
            Directory.CreateDirectory(okPath);
            Directory.CreateDirectory(ngPath);
        }

        #endregion

        #region 13. Start Engine
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (inferenceEngine == null)
            {
                MSystem.InsertAndSaveLogs("Inference Engine chưa được khởi tạo!", Color.Red);
                return;
            }
            IsRunning = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnStart.BackColor = Color.Lime;
            btnStop.BackColor = Color.White;

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            IsRunning = false;
            btnStop.Enabled = false;
            btnStart.Enabled = true;
            btnStop.BackColor = Color.Red;
            btnStart.BackColor = Color.White;
        }
        #endregion

        #region 14. Camera Settings
        private void comboBoxCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string selectedCam = comboBoxCamera.SelectedItem.ToString();
                numericExposure_Time.Value = (decimal)CameraBasler.GetExposureTime(selectedCam);
                numericGain.Value = (decimal)CameraBasler.GetGain(selectedCam);
                numericGamma.Value = (decimal)CameraBasler.GetGamma(selectedCam);
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs("Lỗi khi lấy parameter camera: " + ex.ToString(), Color.Red);
            }
        }

        private void numericExposure_Time_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (CameraBasler.SetExposureTime(comboBoxCamera.SelectedItem.ToString(), (double)numericExposure_Time.Value))
                {
                    MSystem.InsertAndSaveLogs($"Set Exposure Time {comboBoxCamera.SelectedItem}: {(double)numericExposure_Time.Value}", Color.Green);
                    return;
                }
                MSystem.InsertAndSaveLogs($"Failed to set Exposure Time for {comboBoxCamera.SelectedItem}", Color.Red);
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
            }
        }

        private void numericGain_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (CameraBasler.SetGain(comboBoxCamera.SelectedItem.ToString(), (double)numericGain.Value))
                {
                    MSystem.InsertAndSaveLogs($"Set Gain {comboBoxCamera.SelectedItem}: {(double)numericGain.Value}", Color.Green);
                    return;
                }
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
            }
        }

        private void numericGamma_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (CameraBasler.SetGamma(comboBoxCamera.SelectedItem.ToString(), (double)numericGamma.Value))
                {
                    MSystem.InsertAndSaveLogs($"Set Gamma {comboBoxCamera.SelectedItem}: {(double)numericGamma.Value}", Color.Green);
                }
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
            }
        }
        private void btnSave_Parameter_Click(object sender, EventArgs e)
        {
            if (comboBoxCamera.SelectedItem == null) return;
            try
            {
                if (CameraBasler.UserSetSave(comboBoxCamera.SelectedItem.ToString()))
                {
                    NotificationDialog.Show($"Parameters for {comboBoxCamera.SelectedItem} saved successfully!");
                    return;
                }
                NotificationDialog.Show($"Failed to save parameters for {comboBoxCamera.SelectedItem}. Please check the camera connection and try again.");
            }
            catch { }

        }

        #endregion

        private void Panel_Header_Paint(object sender, PaintEventArgs e)
        {

        }
    }

    #region Inspection Zones Class
    [Serializable]
    public class InspectionZones
    {
        public string Name { get; set; }
        public Rectangle Rect { get; set; }


        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);
        public static void SetupPaddlePaths()
        {
            string baseDir = AppContext.BaseDirectory;
            string paddleLibPath = Path.Combine(baseDir, "dll");
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", paddleLibPath + ";" + pathEnv);
            SetDllDirectory(paddleLibPath);
        }
    }

    public class CameraConfig
    {
        public string Name { get; set; }
        public ICameraInterface CameraInterface { get; set; }
        public PictureBox TargetPictureBox { get; set; }
    }

    public class CameraProcessResult
    {
        public Bitmap RawImage { get; set; }
        public string Status { get; set; } = "UNKNOWN_ERROR";
        public string OcrText { get; set; } = "";
        public List<YoloInferenceEngine.Detection> Detections { get; set; } = new();
    }
    #endregion

}
