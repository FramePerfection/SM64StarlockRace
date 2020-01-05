namespace GameServerUI
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
            this.btnHost = new System.Windows.Forms.Button();
            this.lstHostedGames = new System.Windows.Forms.ListView();
            this.listPlayers = new System.Windows.Forms.ListView();
            this.lblPublicIP = new System.Windows.Forms.Label();
            this.contextGames = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextGames.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnHost
            // 
            this.btnHost.Location = new System.Drawing.Point(12, 12);
            this.btnHost.Name = "btnHost";
            this.btnHost.Size = new System.Drawing.Size(132, 23);
            this.btnHost.TabIndex = 0;
            this.btnHost.Text = "Host";
            this.btnHost.UseVisualStyleBackColor = true;
            this.btnHost.Click += new System.EventHandler(this.btnHost_Click);
            // 
            // lstHostedGames
            // 
            this.lstHostedGames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstHostedGames.Location = new System.Drawing.Point(12, 41);
            this.lstHostedGames.Name = "lstHostedGames";
            this.lstHostedGames.Size = new System.Drawing.Size(271, 97);
            this.lstHostedGames.TabIndex = 1;
            this.lstHostedGames.UseCompatibleStateImageBehavior = false;
            // 
            // listPlayers
            // 
            this.listPlayers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listPlayers.Location = new System.Drawing.Point(12, 146);
            this.listPlayers.Name = "listPlayers";
            this.listPlayers.Size = new System.Drawing.Size(271, 97);
            this.listPlayers.TabIndex = 1;
            this.listPlayers.UseCompatibleStateImageBehavior = false;
            // 
            // lblPublicIP
            // 
            this.lblPublicIP.AutoSize = true;
            this.lblPublicIP.Location = new System.Drawing.Point(150, 9);
            this.lblPublicIP.Name = "lblPublicIP";
            this.lblPublicIP.Size = new System.Drawing.Size(86, 13);
            this.lblPublicIP.TabIndex = 2;
            this.lblPublicIP.Text = "Your public IP is:";
            // 
            // contextGames
            // 
            this.contextGames.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addGameToolStripMenuItem});
            this.contextGames.Name = "contextGames";
            this.contextGames.Size = new System.Drawing.Size(131, 26);
            // 
            // addGameToolStripMenuItem
            // 
            this.addGameToolStripMenuItem.Name = "addGameToolStripMenuItem";
            this.addGameToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.addGameToolStripMenuItem.Text = "Add Game";
            this.addGameToolStripMenuItem.Click += new System.EventHandler(this.addGameToolStripMenuItem_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(295, 264);
            this.Controls.Add(this.lblPublicIP);
            this.Controls.Add(this.listPlayers);
            this.Controls.Add(this.lstHostedGames);
            this.Controls.Add(this.btnHost);
            this.Name = "Main";
            this.Text = "StarLock Public Server UI";
            this.contextGames.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnHost;
        private System.Windows.Forms.ListView lstHostedGames;
        private System.Windows.Forms.ListView listPlayers;
        private System.Windows.Forms.Label lblPublicIP;
        private System.Windows.Forms.ContextMenuStrip contextGames;
        private System.Windows.Forms.ToolStripMenuItem addGameToolStripMenuItem;
    }
}

