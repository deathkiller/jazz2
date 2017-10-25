namespace Editor
{
    partial class MainWindow
    {
        /// <summary>
        /// Vyžaduje se proměnná návrháře.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Uvolněte všechny používané prostředky.
        /// </summary>
        /// <param name="disposing">hodnota true, když by se měl spravovaný prostředek odstranit; jinak false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Kód generovaný Návrhářem Windows Form

        /// <summary>
        /// Metoda vyžadovaná pro podporu Návrháře - neupravovat
        /// obsah této metody v editoru kódu.
        /// </summary>
        private void InitializeComponent()
        {
            this.camView2 = new Editor.CamView.CamView();
            this.SuspendLayout();
            // 
            // camView2
            // 
            this.camView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.camView2.Location = new System.Drawing.Point(12, 12);
            this.camView2.Name = "camView2";
            this.camView2.Size = new System.Drawing.Size(672, 393);
            this.camView2.TabIndex = 1;
            this.camView2.Text = "camView2";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(696, 417);
            this.Controls.Add(this.camView2);
            this.Name = "MainWindow";
            this.ResumeLayout(false);

        }

        #endregion

        private CamView.CamView camView2;
    }
}

