namespace DynamicSample
{
    partial class FrmSample
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pbDraw = new System.Windows.Forms.PictureBox();
            this.tmrWork = new System.Windows.Forms.Timer(this.components);
            this.btnSaveXp = new System.Windows.Forms.Button();
            this.btnLoadXp = new System.Windows.Forms.Button();
            this.btnNewGame = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).BeginInit();
            this.SuspendLayout();
            // 
            // pbDraw
            // 
            this.pbDraw.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbDraw.Location = new System.Drawing.Point(12, 12);
            this.pbDraw.Name = "pbDraw";
            this.pbDraw.Size = new System.Drawing.Size(483, 483);
            this.pbDraw.TabIndex = 3;
            this.pbDraw.TabStop = false;
            this.pbDraw.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbDraw_MouseUp);
            // 
            // tmrWork
            // 
            this.tmrWork.Interval = 1000;
            // 
            // btnSaveXp
            // 
            this.btnSaveXp.Location = new System.Drawing.Point(12, 501);
            this.btnSaveXp.Name = "btnSaveXp";
            this.btnSaveXp.Size = new System.Drawing.Size(234, 23);
            this.btnSaveXp.TabIndex = 4;
            this.btnSaveXp.Text = "Сохранить опыт";
            this.btnSaveXp.UseVisualStyleBackColor = true;
            // 
            // btnLoadXp
            // 
            this.btnLoadXp.Location = new System.Drawing.Point(261, 501);
            this.btnLoadXp.Name = "btnLoadXp";
            this.btnLoadXp.Size = new System.Drawing.Size(234, 23);
            this.btnLoadXp.TabIndex = 5;
            this.btnLoadXp.Text = "Загрузить опыт";
            this.btnLoadXp.UseVisualStyleBackColor = true;
            // 
            // btnNewGame
            // 
            this.btnNewGame.Location = new System.Drawing.Point(12, 530);
            this.btnNewGame.Name = "btnNewGame";
            this.btnNewGame.Size = new System.Drawing.Size(483, 23);
            this.btnNewGame.TabIndex = 6;
            this.btnNewGame.Text = "Новая игра";
            this.btnNewGame.UseVisualStyleBackColor = true;
            this.btnNewGame.Click += new System.EventHandler(this.btnNewGame_Click);
            // 
            // FrmSample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(506, 558);
            this.Controls.Add(this.btnNewGame);
            this.Controls.Add(this.btnLoadXp);
            this.Controls.Add(this.btnSaveXp);
            this.Controls.Add(this.pbDraw);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(366, 150);
            this.Name = "FrmSample";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FrmSample";
            this.Shown += new System.EventHandler(this.FrmSample_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox pbDraw;
        private System.Windows.Forms.Timer tmrWork;
        private System.Windows.Forms.Button btnSaveXp;
        private System.Windows.Forms.Button btnLoadXp;
        private System.Windows.Forms.Button btnNewGame;
    }
}

