using Basler.Pylon;
using PaddleOCRSharp;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Runtime;
using System.Runtime.InteropServices;
using TPU_Assembly.Class;
using TPU_Assembly.JobSelection;


namespace TPU_Assembly_Inspection_Paddle
{

    public partial class MAINFORM : Form
    {
        public volatile bool IsRunning = false;

        private YoloInferenceEngine inferenceEngine;

        public static bool SaveImageOrigin, SaveImageOK, SaveImageNG;

        public static int SaveLogDays;

        public static float ConfidenceThreshold;

        public static string IPAddress;

        public static int Port;

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

        public List<InspectionZones> _inspectionZones = [];

        public bool IsRoiMode;

        public readonly string _configFile = "OcrZonesConfig.json";

        public readonly Dictionary<PictureBox, ViewState> _viewStates = [];

        public string currentImportpictureBox = "";

        private TCP_Server _tcpServer;

        private readonly Font font = new ("Arial", 150, FontStyle.Bold);
        private readonly Pen penBox = new (Color.Lime, 15);                   
        private readonly SolidBrush brushText = new (Color.White);  
        private readonly SolidBrush brushBg = new (Color.Lime);

        public MAINFORM()
        {
            InitializeComponent();

            SplashScreenManager.ShowSplash();

            LoadSystemSettings();

            zoomable = new Zoomable(this);

            thumbnails = new Thumbnails(this);

            MSystem.SetRichTextLogs(this.richTextLog);

            CreateFolderFileDefault.CreateSaveFolders();

            UpdateStatusCameraAndRobot();

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

            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            Panel_Home.Visible = true;
            Panel_Teaching.Visible = false;
            Panel_Settings.Visible = false;

            zoomable.LoadOcrZones();
            Start_Server();
            InitializeServer();
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

        private void OnDataReceived(string cmd)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnDataReceived(cmd)));
                return;
            }
            MSystem.InsertAndSaveLogs(cmd, Color.Blue);
            if (string.IsNullOrEmpty(cmd)) return;
            string data = cmd.ToUpper().Trim();
            if (data.Contains("TRIGGER"))
            {
                Run_Once();
            }
        }

        private async void Run_Once()
        {
            stopWatch.Restart();
            var taskCam1 = Task.Run(() => ProcessCameraAI("CAMERA1"));
            var taskCam2 = Task.Run(() => ProcessCameraAI("CAMERA2"));
            var taskCam3 = Task.Run(() => ProcessCameraAI("CAMERA3"));
            await Task.WhenAll(taskCam1, taskCam2, taskCam3);
            if (taskCam1.Result == false || taskCam2.Result == false || taskCam3.Result == false)
            {
                MSystem.InsertAndSaveLogs("CAMERA1 PROCESSING FAILED", Color.Red);
                btnResult.BackColor = Color.Red;
                btnResult.Text = "NG";
                return;
            }
            await Task.Run(() => Run_All_PictureBox_Click(null, null));
            stopWatch.Stop();
            BT_Time.Text = stopWatch.ElapsedMilliseconds.ToString();   
        }

        private bool ProcessCameraAI(string camName)
        {
            PictureBox targetPB = null;
            switch (camName)
            {
                case "CAMERA1": pictureBox1.Image?.Dispose();targetPB = pictureBox1;break;
                case "CAMERA2": pictureBox2.Image?.Dispose();targetPB = pictureBox2;break;
                case "CAMERA3":pictureBox3.Image?.Dispose();targetPB = pictureBox3;break;
            }
            try
            {
                using (Bitmap img = CameraBasler.GrabImage(false, camName))
                {
                    if (img == null) { return false; }

                    Bitmap cloned = (Bitmap)img.Clone();

                    targetPB.Invoke(new Action(() => {
                        if (targetPB.Image != null && targetPB.Image != targetPB.Tag)
                        {
                            targetPB.Image.Dispose(); 
                        }
                        UpdateCameraImage((camName == "CAMERA1") ? 1 : (camName == "CAMERA2") ? 2 : 3, cloned);
                    }));
                    return true;
                }
            }
            catch (Exception ex) 
            {
                MSystem.InsertAndSaveLogs($"Error {camName}: {ex.Message}", Color.Red);
                return false;
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
        }
        private void btnTeaching_Click(object sender, EventArgs e)
        {
            //using (FullTouchKeyboard kboard = new FullTouchKeyboard("PASSWORD", true, "1"))
            //{
            //    if (kboard.ShowDialog() == DialogResult.OK)
            //    {
            ShowPanel(Panel_Teaching);
            SetActiveMenuButton(btnTeaching);
            //    }
            //}

        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            //using (FullTouchKeyboard kboard = new FullTouchKeyboard("PASSWORD", true, "1"))
            //{
            //    if (kboard.ShowDialog() == DialogResult.OK)
            //    {
            ShowPanel(Panel_Settings);
            SetActiveMenuButton(btnSettings);
            //    }

            //}
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
            catch {}
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
            pictureBox2.Image?.Dispose();
            pictureBox2.Image = null;
            pictureBox3.Image?.Dispose();
            pictureBox3.Image = null;
            _tcpServer?.Stop();
        }

        #endregion

        #region 3. Load Models

        private void btnLoadModel_Click(object sender, EventArgs e)
        {
            if (IsRunning)
            {
                MessageBox.Show("Please STOP Before Change Models");
                return;
            }
            ButtonIsEnableLoadJob(false);
            try
            {
                using (JobSelectionForm jobSelector = new JobSelectionForm())
                {
                    if (jobSelector.ShowDialog() == DialogResult.OK)
                    {
                        string selectedJob = jobSelector.FullJobFolderPath;
                        if (!string.IsNullOrEmpty(selectedJob))
                        {
                            this.Cursor = Cursors.WaitCursor;

                            inferenceEngine?.Dispose();
                            inferenceEngine = new YoloInferenceEngine(selectedJob);
                            inferenceEngine.ConfidenceThreshold = ConfidenceThreshold;

                            try
                            {
                                InspectionZones.SetupPaddlePaths();
                                _ocrEngine = new PaddleOCREngine(null, new OCRParameter());
                            }
                            catch { MSystem.InsertAndSaveLogs("PaddleOCR Engine Init Failed!", Color.Red); }
                            this.Cursor = Cursors.Default;

                            btnLoadModel.BackColor = Color.Lime;
                            MSystem.InsertAndSaveLogs("Load Models Successfully!", Color.Green);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                btnLoadModel.BackColor = Color.Red;
                MSystem.InsertAndSaveLogs($"Load Modesl Fail: {ex}", Color.Red);
            }
            finally
            {
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

        private void UpdateStatusCameraAndRobot()
        {
            btnCamera1.BackColor = (CameraBasler.CheckConnectCam("CAMERA1")) ? Color.Lime : Color.Red;
            btnCamera2.BackColor = (CameraBasler.CheckConnectCam("CAMERA2")) ? Color.Lime : Color.Red;
            btnCamera3.BackColor = (CameraBasler.CheckConnectCam("CAMERA3")) ? Color.Lime : Color.Red;
        }

        #endregion

        #region 6. Teaching Camera
        private void Universal_GrabImage_Click(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            string cameraName = "";
            PictureBox targetPB = null;
            switch (btn.Name)
            {
                case "BT_GrapImage1": cameraName = "CAMERA1"; targetPB = pictureBox1; break;
                case "BT_GrapImage2": cameraName = "CAMERA2"; targetPB = pictureBox2; break;
                case "BT_GrapImage3": cameraName = "CAMERA3"; targetPB = pictureBox3; break;
            }

            if (targetPB != null)
            {
                using (Bitmap newImage = CameraBasler.GrabImage(true, cameraName))
                {
                    if (newImage == null) return;

                    targetPB.Image?.Dispose();
                    UpdateCameraImage(
                        (cameraName == "CAMERA1") ? 1 :
                        (cameraName == "CAMERA2") ? 2 : 3,
                        newImage);
                }
            }
        }

        private string ShowSelectionDialog()
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                Text = "Chọn PictureBox mục tiêu",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            FlowLayoutPanel panel = new FlowLayoutPanel() { Dock = DockStyle.Fill, Padding = new Padding(10) };
            string selected = "All";

            for (int i = 1; i <= 3; i++)
            {
                Button btn = new Button() { Text = i.ToString(), Width = 50, Height = 50 };
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

                pictureBox1.Image?.Dispose();
                pictureBox2.Image?.Dispose();
                pictureBox3.Image?.Dispose();

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
                    ofd.Title = "Select Images to Import";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        Bitmap bmp = LoadBitmapWithoutLocking(ofd.FileName);
                        UpdateCameraImage(1, bmp);
                        UpdateCameraImage(2, bmp);
                        UpdateCameraImage(3, bmp);
                        zoomable.FitImageToPictureBox(pictureBox1);
                        zoomable.FitImageToPictureBox(pictureBox2);
                        zoomable.FitImageToPictureBox(pictureBox3);
                    }
                }

                return;
            }
            thumbnails.SetupThumbnailUI();
            using (OpenFileDialog ofd = new OpenFileDialog())
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
            int index = 0;

            switch (btn.Name)
            {
                case "Import_Image": targetPB = pictureBox1; index = 1; break;
                case "Import_Image2": targetPB = pictureBox2; index = 2; break;
                case "Import_Image3": targetPB = pictureBox3; index = 3; break;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
                ofd.Title = $"Select Image for Camera {index}";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    targetPB.Image?.Dispose();

                    Bitmap bmp = LoadBitmapWithoutLocking(ofd.FileName);

                    UpdateCameraImage(index, bmp);

                    targetPB.Image = bmp;
                    zoomable.FitImageToPictureBox(targetPB);
                }
            }
        }

        private void BT_Folder_Img_Click(object sender, EventArgs e)
        {
            string folderPath = $@"C:\FA\TPU_Assembly_Inspection_Paddle\Images\";

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
            pictureBox2.Image?.Dispose();
            pictureBox3.Image?.Dispose();
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            pictureBox3.Image = null;

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

        #endregion

        #region 8. Counter

        private void btnClearCounter_Click(object sender, EventArgs e)
        {
            LabelOK.Text = LabelNG.Text = labelTotal.Text = "0";
            percentNG.Text = percentOK.Text = "0.0%";
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
        private void btnSelcectROI_Click(object sender, EventArgs e)
        {
            IsRoiMode = !IsRoiMode;
            if (IsRoiMode) pictureBox1.Cursor = Cursors.Cross;
            else pictureBox1.Cursor = Cursors.Default;
            ButtonIsEnableLoadJob(!IsRoiMode);
        }

        #endregion

        #region 11. Save Result Image

        private void SaveResultToDisk(Bitmap image, string baseFileName, string type)
        {
            try
            {
                bool isAllowed = false;
                if (type == "OK" && SaveImageOK) isAllowed = true;
                else if (type == "NG" && SaveImageNG) isAllowed = true;

                if (!isAllowed) return;

                string dateString = DateTime.Now.ToString("yyyy-MM-dd");
                EnsureImageDirectories(dateString, out string pathOrigin, out string pathOK, out string pathNG);

                string destFolder = "";
                switch (type)
                {
                    case "OK": destFolder = pathOK; break;
                    case "NG": destFolder = pathNG; break;
                    default: return;
                }
                string savePath = Path.Combine(destFolder, baseFileName + ".jpg");
                image?.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                MSystem.InsertAndSaveLogs($"Saved {type}: {baseFileName}.jpg", Color.Black);
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"Save Error ({type}): {ex.Message}", Color.Red);
            }
        }


        #endregion

        #region 12. Run

        public string GetResultFromZone(Bitmap fullImage, string zoneName)
        {
            var zone = _inspectionZones.Find(z => z.Name == zoneName);
            if (zone == null) return "N/A";

            try
            {
                Rectangle cropRect = zone.Rect;
                cropRect.Intersect(new Rectangle(0, 0, fullImage.Width, fullImage.Height));

                if (cropRect.Width == 0 || cropRect.Height == 0) return "Error_Size";

                string resultText = "";
                Color boxColor = Color.Lime;

                using (Bitmap roi = fullImage.Clone(cropRect, fullImage.PixelFormat))
                {
                    if (zoneName.ToUpper().Contains("OCR") || zoneName.ToUpper().Contains("TEXT"))
                    {
                        var ocrResult = _ocrEngine.DetectText(roi);
                        resultText = ocrResult != null ? ocrResult.Text.Trim() : "";
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
                MessageBox.Show("Lỗi xử lý OCR: " + ex.Message);
                return "Error: " + ex.Message;
            }
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

                    string label = $"{item.ClassName} ({item.Confidence:0.00})";
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
        private PictureBox GetPictureBox(int camId)
        {
            return camId switch
            {
                1 => pictureBox1,
                2 => pictureBox2,
                3 => pictureBox3,
                _ => null,
            };
        }
        public void UpdateCameraImage(int camId, Bitmap rawImage)
        {
            PictureBox pb = GetPictureBox(camId);
            if (pb == null) return;

            if (this.InvokeRequired)
            {
                using (Bitmap cloned = (Bitmap)rawImage.Clone())
                {
                    this.Invoke(new Action(() => UpdateCameraImage(camId, cloned)));
                }
                return;
            }

            if (pb.Tag is Bitmap oldTag)
                oldTag.Dispose();

            pb.Image?.Dispose();
            Bitmap newImage = (Bitmap)rawImage.Clone();
            pb.Tag = newImage;
            pb.Image = newImage;
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

                if (det2.Count >= 3 && det3.Count >= 3 && !string.IsNullOrEmpty(ocrResult))
                {
                    this.Invoke(new Action(() =>
                    {
                        btnResult.Text = "OK";
                        btnResult.ForeColor = Color.Black;
                        btnResult.BackColor = Color.Lime;
                    }));
                    MSystem.InsertAndSaveLogs("Result: OK", Color.Green);
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        btnResult.Text = "NG";
                        btnResult.ForeColor = Color.White;
                        btnResult.BackColor = Color.Red;
                    }));
                    MSystem.InsertAndSaveLogs("Result: NG", Color.Red);
                }

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

            Bitmap workingImage = new Bitmap(pictureBox2.Image);

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

        private void Run_Vision_CAMERA3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Chức năng này đang phát triển!", "Thông báo");
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            zoomable.FitImageToPictureBox(pictureBox1);
        }

        private void EnsureImageDirectories(string dateString, out string originalPath, out string okPath, out string ngPath)
        {
            string baseImagesPath = @"C:\FA\TPU_Assembly_Inspection_Paddle\Images";
            string dateFolderPath = Path.Combine(baseImagesPath, dateString);

            // Thư mục gốc theo ngày
            if (!Directory.Exists(dateFolderPath))
            {
                Directory.CreateDirectory(dateFolderPath);
            }

            // Thư mục con
            originalPath = Path.Combine(dateFolderPath, "Original");
            okPath = Path.Combine(dateFolderPath, "OK");
            ngPath = Path.Combine(dateFolderPath, "NG");

            if (!Directory.Exists(originalPath)) Directory.CreateDirectory(originalPath);
            if (!Directory.Exists(okPath)) Directory.CreateDirectory(okPath);
            if (!Directory.Exists(ngPath)) Directory.CreateDirectory(ngPath);
        }

        #endregion

        #region Start Engine
        private void btnStart_Click(object sender, EventArgs e)
        {


        }

        #endregion
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
    #endregion

}
