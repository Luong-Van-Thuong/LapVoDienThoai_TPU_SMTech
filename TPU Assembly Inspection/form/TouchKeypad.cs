using System;
using System.Drawing;
using System.Windows.Forms;

namespace TPU_Assembly.Class
{
    public class FullTouchKeyboard : Form
    {
        private TextBox txtDisplay;
        public string InputValue { get; private set; } = "";
        private bool _isPasswordMode;

        private const int BTN_SIZE = 65; 
        private const int GAP = 6;      

        private Color COLOR_BG = Color.FromArgb(45, 45, 48);
        private Color COLOR_BTN_NORMAL = Color.WhiteSmoke;
        private Color COLOR_BTN_PRESS = Color.FromArgb(200, 200, 200); 
        private Color COLOR_ENTER = Color.DodgerBlue;
        private Color COLOR_CLEAR = Color.IndianRed;
        private string _correctPassword;
        public FullTouchKeyboard(string title = "INPUT DATA", bool isPassword = true, string correctPass = null)
        {
            _isPasswordMode = isPassword;
            _correctPassword = correctPass;

            this.Text = title;
            this.KeyPreview = true;
            this.Size = new Size(860, 495);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = COLOR_BG;
            this.ControlBox = false; 

            txtDisplay = new TextBox()
            {
                Location = new Point(20, 20),
                Width = 680,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                PasswordChar = _isPasswordMode ? '●' : '\0',
                MaxLength = 50,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            this.Controls.Add(txtDisplay);

            Button btnClear = CreateButton("CLR", 710, 20, 100, 48, COLOR_CLEAR);
            btnClear.Click += (s, e) => txtDisplay.Text = "";
            this.Controls.Add(btnClear);

            int startY = 90;
            int startX = 25;

            string[] row1 = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            AddRow(row1, startX, startY);

            string[] row2 = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
            AddRow(row2, startX + 35, startY + BTN_SIZE + GAP);

            string[] row3 = { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
            AddRow(row3, startX + 55, startY + (BTN_SIZE + GAP) * 2);

            string[] row4 = { "Z", "X", "C", "V", "B", "N", "M" };
            AddRow(row4, startX + 90, startY + (BTN_SIZE + GAP) * 3);


            Button btnBack = CreateButton("⌫", startX + 10 * (BTN_SIZE + GAP) + 10, startY, 90, BTN_SIZE, Color.LightGray);
            btnBack.Click += BtnBack_Click;
            this.Controls.Add(btnBack);

            Button btnEnter = CreateButton("ENTER", 680, 380, 130, 70, COLOR_ENTER);
            btnEnter.ForeColor = Color.White;
            btnEnter.Click += BtnEnter_Click;
            this.Controls.Add(btnEnter);
            this.AcceptButton = btnEnter;

            Button btnCancel = CreateButton("CANCEL", 25, 380, 130, 70, Color.DimGray);
            btnCancel.ForeColor = Color.White;
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);


            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnEnter_Click(btnEnter, EventArgs.Empty);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            };
        }

        private void AddRow(string[] keys, int x, int y)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                Button btn = CreateButton(keys[i], x + i * (BTN_SIZE + GAP), y, BTN_SIZE, BTN_SIZE, COLOR_BTN_NORMAL);

                btn.Click += (s, e) => txtDisplay.AppendText(((Button)s).Text);

                this.Controls.Add(btn);
            }
        }

        private Button CreateButton(string text, int x, int y, int w, int h, Color bg)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(w, h);
            btn.BackColor = bg;
            btn.ForeColor = (bg == COLOR_BTN_NORMAL || bg == Color.LightGray || bg == Color.Gainsboro) ? Color.Black : Color.White;

            btn.Font = new Font("Segoe UI", 16, FontStyle.Bold);

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.TabStop = false; 

            btn.MouseDown += (s, e) =>
            {
                if (bg == COLOR_BTN_NORMAL) btn.BackColor = Color.Orange; 
                else btn.BackColor = ControlPaint.Dark(bg);
            };

            btn.MouseUp += (s, e) =>
            {
                btn.BackColor = bg;
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = bg;
            };

            return btn;
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (txtDisplay.Text.Length > 0)
            {
                txtDisplay.Text = txtDisplay.Text.Substring(0, txtDisplay.Text.Length - 1);
            }
            txtDisplay.Focus();
        }

        private void BtnEnter_Click(object sender, EventArgs e)
        {
            if (_correctPassword != null && txtDisplay.Text != _correctPassword)
            {
                MessageBox.Show("Password is Wrong", "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtDisplay.Text = ""; 
                txtDisplay.Focus();
                return;
            }
            InputValue = txtDisplay.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}