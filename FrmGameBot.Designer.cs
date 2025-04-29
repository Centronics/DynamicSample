namespace DynamicSample
{
    partial class FrmGameBot
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmGameBot));
            this.pbScreenField = new System.Windows.Forms.PictureBox();
            this.radField_X = new System.Windows.Forms.RadioButton();
            this.radField_O = new System.Windows.Forms.RadioButton();
            this.btnSavePosition = new System.Windows.Forms.Button();
            this.radEmptySpace = new System.Windows.Forms.RadioButton();
            this.btnClearPosition = new System.Windows.Forms.Button();
            this.radNeedClick = new System.Windows.Forms.RadioButton();
            this.btnGameStart = new System.Windows.Forms.Button();
            this.cbxProfiles = new System.Windows.Forms.ComboBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreenField)).BeginInit();
            this.SuspendLayout();
            // 
            // pbScreenField
            // 
            this.pbScreenField.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbScreenField.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.pbScreenField.Location = new System.Drawing.Point(12, 5);
            this.pbScreenField.Name = "pbScreenField";
            this.pbScreenField.Size = new System.Drawing.Size(84, 98);
            this.pbScreenField.TabIndex = 0;
            this.pbScreenField.TabStop = false;
            this.pbScreenField.MouseEnter += new System.EventHandler(this.PbScreenField_MouseEnter);
            this.pbScreenField.MouseLeave += new System.EventHandler(this.PbScreenField_MouseLeave);
            // 
            // radField_X
            // 
            this.radField_X.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radField_X.AutoSize = true;
            this.radField_X.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.radField_X.Location = new System.Drawing.Point(101, 73);
            this.radField_X.Name = "radField_X";
            this.radField_X.Size = new System.Drawing.Size(58, 29);
            this.radField_X.TabIndex = 1;
            this.radField_X.Text = "X";
            this.radField_X.UseVisualStyleBackColor = true;
            // 
            // radField_O
            // 
            this.radField_O.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radField_O.AutoSize = true;
            this.radField_O.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.radField_O.Location = new System.Drawing.Point(160, 73);
            this.radField_O.Name = "radField_O";
            this.radField_O.Size = new System.Drawing.Size(60, 29);
            this.radField_O.TabIndex = 2;
            this.radField_O.Text = "O";
            this.radField_O.UseVisualStyleBackColor = true;
            // 
            // btnSavePosition
            // 
            this.btnSavePosition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSavePosition.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnSavePosition.ForeColor = System.Drawing.Color.Green;
            this.btnSavePosition.Location = new System.Drawing.Point(163, 109);
            this.btnSavePosition.Name = "btnSavePosition";
            this.btnSavePosition.Size = new System.Drawing.Size(84, 34);
            this.btnSavePosition.TabIndex = 3;
            this.btnSavePosition.Text = "OK";
            this.btnSavePosition.UseVisualStyleBackColor = true;
            this.btnSavePosition.Click += new System.EventHandler(this.BtnSavePosition_Click);
            // 
            // radEmptySpace
            // 
            this.radEmptySpace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radEmptySpace.AutoSize = true;
            this.radEmptySpace.Location = new System.Drawing.Point(101, 38);
            this.radEmptySpace.Name = "radEmptySpace";
            this.radEmptySpace.Size = new System.Drawing.Size(156, 29);
            this.radEmptySpace.TabIndex = 4;
            this.radEmptySpace.Text = "Пуст место";
            this.radEmptySpace.UseVisualStyleBackColor = true;
            // 
            // btnClearPosition
            // 
            this.btnClearPosition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearPosition.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnClearPosition.ForeColor = System.Drawing.Color.Red;
            this.btnClearPosition.Location = new System.Drawing.Point(102, 109);
            this.btnClearPosition.Name = "btnClearPosition";
            this.btnClearPosition.Size = new System.Drawing.Size(55, 33);
            this.btnClearPosition.TabIndex = 5;
            this.btnClearPosition.Text = "X";
            this.btnClearPosition.UseVisualStyleBackColor = true;
            this.btnClearPosition.Click += new System.EventHandler(this.BtnClearPosition_Click);
            // 
            // radNeedClick
            // 
            this.radNeedClick.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radNeedClick.AutoSize = true;
            this.radNeedClick.Location = new System.Drawing.Point(101, 5);
            this.radNeedClick.Name = "radNeedClick";
            this.radNeedClick.Size = new System.Drawing.Size(136, 29);
            this.radNeedClick.TabIndex = 6;
            this.radNeedClick.Text = "Кликнуть";
            this.radNeedClick.UseVisualStyleBackColor = true;
            // 
            // btnGameStart
            // 
            this.btnGameStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGameStart.Enabled = false;
            this.btnGameStart.ForeColor = System.Drawing.Color.Blue;
            this.btnGameStart.Location = new System.Drawing.Point(12, 109);
            this.btnGameStart.Name = "btnGameStart";
            this.btnGameStart.Size = new System.Drawing.Size(84, 34);
            this.btnGameStart.TabIndex = 8;
            this.btnGameStart.Text = "Старт";
            this.btnGameStart.UseVisualStyleBackColor = true;
            this.btnGameStart.Click += new System.EventHandler(this.BtnGameStart_Click);
            // 
            // cbxProfiles
            // 
            this.cbxProfiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxProfiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxProfiles.FormattingEnabled = true;
            this.cbxProfiles.Items.AddRange(new object[] {
            "<Авто>"});
            this.cbxProfiles.Location = new System.Drawing.Point(12, 149);
            this.cbxProfiles.Name = "cbxProfiles";
            this.cbxProfiles.Size = new System.Drawing.Size(235, 33);
            this.cbxProfiles.TabIndex = 9;
            this.cbxProfiles.SelectedIndexChanged += new System.EventHandler(this.CbxProfiles_SelectedIndexChanged);
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(133, 188);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(84, 31);
            this.textBox1.TabIndex = 10;
            this.textBox1.Text = "300";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 191);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 25);
            this.label1.TabIndex = 11;
            this.label1.Text = "Выдержка:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(215, 191);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 25);
            this.label2.TabIndex = 12;
            this.label2.Text = "мс";
            // 
            // FrmGameBot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.ClientSize = new System.Drawing.Size(259, 226);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.cbxProfiles);
            this.Controls.Add(this.btnGameStart);
            this.Controls.Add(this.radNeedClick);
            this.Controls.Add(this.btnClearPosition);
            this.Controls.Add(this.radEmptySpace);
            this.Controls.Add(this.btnSavePosition);
            this.Controls.Add(this.radField_O);
            this.Controls.Add(this.radField_X);
            this.Controls.Add(this.pbScreenField);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(47, 92);
            this.Name = "FrmGameBot";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmGameBot_FormClosing);
            this.Shown += new System.EventHandler(this.FrmGameSettings_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pbScreenField)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbScreenField;
        private System.Windows.Forms.RadioButton radField_X;
        private System.Windows.Forms.RadioButton radField_O;
        private System.Windows.Forms.Button btnSavePosition;
        private System.Windows.Forms.RadioButton radEmptySpace;
        private System.Windows.Forms.Button btnClearPosition;
        private System.Windows.Forms.RadioButton radNeedClick;
        private System.Windows.Forms.Button btnGameStart;
        private System.Windows.Forms.ComboBox cbxProfiles;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}