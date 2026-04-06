namespace TPU_Assembly_Inspection.form
{
    partial class CreateProductModelForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            label1 = new Label();
            tbProductModelName = new TextBox();
            btnOK = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(12, 41);
            label1.Name = "label1";
            label1.Size = new Size(109, 46);
            label1.TabIndex = 0;
            label1.Text = "Name";
            // 
            // tbProductModelName
            // 
            tbProductModelName.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbProductModelName.Location = new Point(127, 36);
            tbProductModelName.Name = "tbProductModelName";
            tbProductModelName.Size = new Size(630, 51);
            tbProductModelName.TabIndex = 1;
            tbProductModelName.TextAlign = HorizontalAlignment.Center;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(636, 100);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(121, 61);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // CreateProductModelForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 173);
            Controls.Add(btnOK);
            Controls.Add(tbProductModelName);
            Controls.Add(label1);
            Name = "CreateProductModelForm";
            Text = "Create Product Model";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox tbProductModelName;
        private Button btnOK;
    }
}