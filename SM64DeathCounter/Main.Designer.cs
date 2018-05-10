namespace SM64DeathCounter
{
    partial class Main
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.contextMain = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setToToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.topMostToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.txtCustomNumber = new System.Windows.Forms.TextBox();
            this.contextMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMain
            // 
            this.contextMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resetToolStripMenuItem,
            this.setToToolStripMenuItem,
            this.topMostToolStripMenuItem});
            this.contextMain.Name = "contextMain";
            this.contextMain.Size = new System.Drawing.Size(123, 70);
            // 
            // resetToolStripMenuItem
            // 
            this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            this.resetToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.resetToolStripMenuItem.Text = "Reset";
            this.resetToolStripMenuItem.Click += new System.EventHandler(this.resetToolStripMenuItem_Click);
            // 
            // setToToolStripMenuItem
            // 
            this.setToToolStripMenuItem.Name = "setToToolStripMenuItem";
            this.setToToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.setToToolStripMenuItem.Text = "Set to...";
            this.setToToolStripMenuItem.Click += new System.EventHandler(this.setToToolStripMenuItem_Click);
            // 
            // topMostToolStripMenuItem
            // 
            this.topMostToolStripMenuItem.Checked = true;
            this.topMostToolStripMenuItem.CheckOnClick = true;
            this.topMostToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.topMostToolStripMenuItem.Name = "topMostToolStripMenuItem";
            this.topMostToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.topMostToolStripMenuItem.Text = "TopMost";
            this.topMostToolStripMenuItem.Click += new System.EventHandler(this.topMostToolStripMenuItem_Click);
            // 
            // txtCustomNumber
            // 
            this.txtCustomNumber.Location = new System.Drawing.Point(169, 92);
            this.txtCustomNumber.Name = "txtCustomNumber";
            this.txtCustomNumber.Size = new System.Drawing.Size(100, 20);
            this.txtCustomNumber.TabIndex = 1;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(268, 112);
            this.Controls.Add(this.txtCustomNumber);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "Main";
            this.Text = "SM64 Death Counter";
            this.TopMost = true;
            this.contextMain.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMain;
        private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setToToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topMostToolStripMenuItem;
        private System.Windows.Forms.TextBox txtCustomNumber;
    }
}

