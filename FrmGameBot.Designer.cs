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
            this.radClickAfter = new System.Windows.Forms.RadioButton();
            this.radFirstClick = new System.Windows.Forms.RadioButton();
            this.btnGameStart = new System.Windows.Forms.Button();
            this.cbxProfiles = new System.Windows.Forms.ComboBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreenField)).BeginInit();
            this.SuspendLayout();
            // 
            // pbScreenField
            // 
            this.pbScreenField.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbScreenField.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.pbScreenField.Location = new System.Drawing.Point(12, 9);
            this.pbScreenField.Name = "pbScreenField";
            this.pbScreenField.Size = new System.Drawing.Size(84, 132);
            this.pbScreenField.TabIndex = 0;
            this.pbScreenField.TabStop = false;
            this.pbScreenField.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PictureBox1_MouseClick);
            this.pbScreenField.MouseEnter += new System.EventHandler(this.PbScreenField_MouseEnter);
            this.pbScreenField.MouseLeave += new System.EventHandler(this.PbScreenField_MouseLeave);
            // 
            // radField_X
            // 
            this.radField_X.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radField_X.AutoSize = true;
            this.radField_X.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.radField_X.Location = new System.Drawing.Point(101, 112);
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
            this.radField_O.Location = new System.Drawing.Point(160, 112);
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
            this.btnSavePosition.Location = new System.Drawing.Point(162, 147);
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
            this.radEmptySpace.Location = new System.Drawing.Point(101, 79);
            this.radEmptySpace.Name = "radEmptySpace";
            this.radEmptySpace.Size = new System.Drawing.Size(162, 29);
            this.radEmptySpace.TabIndex = 4;
            this.radEmptySpace.Text = "Пуст. место";
            this.radEmptySpace.UseVisualStyleBackColor = true;
            // 
            // btnClearPosition
            // 
            this.btnClearPosition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearPosition.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnClearPosition.ForeColor = System.Drawing.Color.Red;
            this.btnClearPosition.Location = new System.Drawing.Point(101, 147);
            this.btnClearPosition.Name = "btnClearPosition";
            this.btnClearPosition.Size = new System.Drawing.Size(55, 33);
            this.btnClearPosition.TabIndex = 5;
            this.btnClearPosition.Text = "X";
            this.btnClearPosition.UseVisualStyleBackColor = true;
            this.btnClearPosition.Click += new System.EventHandler(this.BtnClearPosition_Click);
            // 
            // radClickAfter
            // 
            this.radClickAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radClickAfter.AutoSize = true;
            this.radClickAfter.Location = new System.Drawing.Point(101, 44);
            this.radClickAfter.Name = "radClickAfter";
            this.radClickAfter.Size = new System.Drawing.Size(157, 29);
            this.radClickAfter.TabIndex = 6;
            this.radClickAfter.Text = "Клик после";
            this.radClickAfter.UseVisualStyleBackColor = true;
            // 
            // radFirstClick
            // 
            this.radFirstClick.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radFirstClick.AutoSize = true;
            this.radFirstClick.Checked = true;
            this.radFirstClick.Location = new System.Drawing.Point(101, 9);
            this.radFirstClick.Name = "radFirstClick";
            this.radFirstClick.Size = new System.Drawing.Size(155, 29);
            this.radFirstClick.TabIndex = 7;
            this.radFirstClick.TabStop = true;
            this.radFirstClick.Text = "Клик сразу";
            this.radFirstClick.UseVisualStyleBackColor = true;
            // 
            // btnGameStart
            // 
            this.btnGameStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGameStart.Enabled = false;
            this.btnGameStart.ForeColor = System.Drawing.Color.Blue;
            this.btnGameStart.Location = new System.Drawing.Point(11, 147);
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
            this.cbxProfiles.FormattingEnabled = true;
            this.cbxProfiles.Items.AddRange(new object[] {
            "<Авто>"});
            this.cbxProfiles.Location = new System.Drawing.Point(11, 187);
            this.cbxProfiles.Name = "cbxProfiles";
            this.cbxProfiles.Size = new System.Drawing.Size(235, 33);
            this.cbxProfiles.TabIndex = 9;
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(132, 226);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(84, 31);
            this.textBox1.TabIndex = 10;
            this.textBox1.Text = "300";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 229);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 25);
            this.label1.TabIndex = 11;
            this.label1.Text = "Выдержка:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(214, 229);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 25);
            this.label2.TabIndex = 12;
            this.label2.Text = "мс";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(16, 257);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(110, 43);
            this.button1.TabIndex = 13;
            this.button1.Text = "Выигрыш";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(137, 257);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(110, 43);
            this.button2.TabIndex = 14;
            this.button2.Text = "Проигрыш";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // FrmGameBot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.ClientSize = new System.Drawing.Size(259, 385);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.cbxProfiles);
            this.Controls.Add(this.btnGameStart);
            this.Controls.Add(this.radFirstClick);
            this.Controls.Add(this.radClickAfter);
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
        private System.Windows.Forms.RadioButton radClickAfter;
        private System.Windows.Forms.RadioButton radFirstClick;
        private System.Windows.Forms.Button btnGameStart;
        private System.Windows.Forms.ComboBox cbxProfiles;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}