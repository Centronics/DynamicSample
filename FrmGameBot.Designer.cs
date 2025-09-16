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
            this.radNeedClick = new System.Windows.Forms.RadioButton();
            this.btnGameStart = new System.Windows.Forms.Button();
            this.cbxProfiles = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreenField)).BeginInit();
            this.SuspendLayout();
            // 
            // pbScreenField
            // 
            this.pbScreenField.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbScreenField.BackColor = System.Drawing.SystemColors.Window;
            this.pbScreenField.Location = new System.Drawing.Point(13, 6);
            this.pbScreenField.Name = "pbScreenField";
            this.pbScreenField.Size = new System.Drawing.Size(84, 98);
            this.pbScreenField.TabIndex = 0;
            this.pbScreenField.TabStop = false;
            // 
            // radField_X
            // 
            this.radField_X.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radField_X.AutoSize = true;
            this.radField_X.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.radField_X.Location = new System.Drawing.Point(102, 74);
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
            this.radField_O.Location = new System.Drawing.Point(161, 74);
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
            this.btnSavePosition.Location = new System.Drawing.Point(102, 110);
            this.btnSavePosition.Name = "btnSavePosition";
            this.btnSavePosition.Size = new System.Drawing.Size(146, 34);
            this.btnSavePosition.TabIndex = 3;
            this.btnSavePosition.Text = "Записать";
            this.btnSavePosition.UseVisualStyleBackColor = true;
            this.btnSavePosition.Click += new System.EventHandler(this.BtnSavePosition_Click);
            // 
            // radEmptySpace
            // 
            this.radEmptySpace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radEmptySpace.AutoSize = true;
            this.radEmptySpace.Location = new System.Drawing.Point(102, 39);
            this.radEmptySpace.Name = "radEmptySpace";
            this.radEmptySpace.Size = new System.Drawing.Size(156, 29);
            this.radEmptySpace.TabIndex = 4;
            this.radEmptySpace.Text = "Пуст место";
            this.radEmptySpace.UseVisualStyleBackColor = true;
            // 
            // radNeedClick
            // 
            this.radNeedClick.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radNeedClick.AutoSize = true;
            this.radNeedClick.Checked = true;
            this.radNeedClick.Location = new System.Drawing.Point(102, 6);
            this.radNeedClick.Name = "radNeedClick";
            this.radNeedClick.Size = new System.Drawing.Size(136, 29);
            this.radNeedClick.TabIndex = 6;
            this.radNeedClick.TabStop = true;
            this.radNeedClick.Text = "Кликнуть";
            this.radNeedClick.UseVisualStyleBackColor = true;
            // 
            // btnGameStart
            // 
            this.btnGameStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGameStart.Enabled = false;
            this.btnGameStart.ForeColor = System.Drawing.Color.MediumBlue;
            this.btnGameStart.Location = new System.Drawing.Point(13, 110);
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
            "<Новый>"});
            this.cbxProfiles.Location = new System.Drawing.Point(13, 150);
            this.cbxProfiles.Name = "cbxProfiles";
            this.cbxProfiles.Size = new System.Drawing.Size(235, 33);
            this.cbxProfiles.TabIndex = 9;
            this.cbxProfiles.SelectedIndexChanged += new System.EventHandler(this.CbxProfiles_SelectedIndexChanged);
            // 
            // FrmGameBot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.ClientSize = new System.Drawing.Size(259, 193);
            this.Controls.Add(this.cbxProfiles);
            this.Controls.Add(this.btnGameStart);
            this.Controls.Add(this.radNeedClick);
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
        private System.Windows.Forms.RadioButton radNeedClick;
        private System.Windows.Forms.Button btnGameStart;
        private System.Windows.Forms.ComboBox cbxProfiles;
    }
}