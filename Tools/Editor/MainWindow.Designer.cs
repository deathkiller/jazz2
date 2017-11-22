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
            this.tileMapLayersListView1 = new Editor.UI.Controls.TileMapLayersListView();
            this.camView2 = new Editor.UI.CamView.CamView();
            this.tileSetView1 = new Editor.UI.Controls.TileSetView();
            this.SuspendLayout();
            // 
            // tileMapLayersListView1
            // 
            this.tileMapLayersListView1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tileMapLayersListView1.Location = new System.Drawing.Point(397, 12);
            this.tileMapLayersListView1.Name = "tileMapLayersListView1";
            this.tileMapLayersListView1.Size = new System.Drawing.Size(287, 131);
            this.tileMapLayersListView1.TabIndex = 2;
            this.tileMapLayersListView1.Text = "tileMapLayersListView1";
            // 
            // camView2
            // 
            this.camView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.camView2.Location = new System.Drawing.Point(12, 12);
            this.camView2.Name = "camView2";
            this.camView2.Size = new System.Drawing.Size(379, 393);
            this.camView2.TabIndex = 1;
            this.camView2.Text = "camView2";
            // 
            // tileSetView1
            // 
            this.tileSetView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tileSetView1.Location = new System.Drawing.Point(397, 149);
            this.tileSetView1.Name = "tileSetView1";
            this.tileSetView1.Size = new System.Drawing.Size(287, 256);
            this.tileSetView1.TabIndex = 3;
            this.tileSetView1.Text = "tileSetView1";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(696, 417);
            this.Controls.Add(this.tileSetView1);
            this.Controls.Add(this.tileMapLayersListView1);
            this.Controls.Add(this.camView2);
            this.Name = "MainWindow";
            this.ResumeLayout(false);

        }

        #endregion

        private Editor.UI.CamView.CamView camView2;
        private UI.Controls.TileMapLayersListView tileMapLayersListView1;
        private UI.Controls.TileSetView tileSetView1;
    }
}

