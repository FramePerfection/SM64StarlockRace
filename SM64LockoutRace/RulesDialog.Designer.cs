namespace GameServerUI
{
    partial class RulesDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RulesDialog));
            this.btnAccept = new System.Windows.Forms.Button();
            this.chkKeys = new System.Windows.Forms.CheckBox();
            this.chkSwitches = new System.Windows.Forms.CheckBox();
            this.chkCannons = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnAccept
            // 
            this.btnAccept.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAccept.Location = new System.Drawing.Point(79, 88);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(75, 23);
            this.btnAccept.TabIndex = 0;
            this.btnAccept.Text = "Accept";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.btnAccept_Click);
            // 
            // chkKeys
            // 
            this.chkKeys.AutoSize = true;
            this.chkKeys.Location = new System.Drawing.Point(13, 13);
            this.chkKeys.Name = "chkKeys";
            this.chkKeys.Size = new System.Drawing.Size(107, 17);
            this.chkKeys.TabIndex = 1;
            this.chkKeys.Text = "synchronize keys";
            this.chkKeys.UseVisualStyleBackColor = true;
            // 
            // chkSwitches
            // 
            this.chkSwitches.AutoSize = true;
            this.chkSwitches.Location = new System.Drawing.Point(13, 36);
            this.chkSwitches.Name = "chkSwitches";
            this.chkSwitches.Size = new System.Drawing.Size(147, 17);
            this.chkSwitches.TabIndex = 2;
            this.chkSwitches.Text = "synchronize cap switches";
            this.chkSwitches.UseVisualStyleBackColor = true;
            // 
            // chkCannons
            // 
            this.chkCannons.AutoSize = true;
            this.chkCannons.Checked = true;
            this.chkCannons.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCannons.Location = new System.Drawing.Point(13, 59);
            this.chkCannons.Name = "chkCannons";
            this.chkCannons.Size = new System.Drawing.Size(126, 17);
            this.chkCannons.TabIndex = 3;
            this.chkCannons.Text = "synchronize cannons";
            this.chkCannons.UseVisualStyleBackColor = true;
            // 
            // RulesDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(166, 123);
            this.Controls.Add(this.chkCannons);
            this.Controls.Add(this.chkSwitches);
            this.Controls.Add(this.chkKeys);
            this.Controls.Add(this.btnAccept);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RulesDialog";
            this.Text = "RulesDialog";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAccept;
        public System.Windows.Forms.CheckBox chkKeys;
        public System.Windows.Forms.CheckBox chkSwitches;
        public System.Windows.Forms.CheckBox chkCannons;
    }
}