namespace GDUploaderDemo
{
    partial class GDUploaderDemo
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
            this.fullUploaderButton = new System.Windows.Forms.Button();
            this.uploadDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.gDPath = new System.Windows.Forms.TextBox();
            this.statusBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // fullUploaderButton
            // 
            this.fullUploaderButton.Location = new System.Drawing.Point(317, 12);
            this.fullUploaderButton.Name = "fullUploaderButton";
            this.fullUploaderButton.Size = new System.Drawing.Size(75, 23);
            this.fullUploaderButton.TabIndex = 0;
            this.fullUploaderButton.Text = "Full Upload";
            this.fullUploaderButton.UseVisualStyleBackColor = true;
            this.fullUploaderButton.Click += new System.EventHandler(this.fullUploaderButton_Click);
            // 
            // statusBar
            // 
            this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusBar.Location = new System.Drawing.Point(0, 67);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(422, 22);
            this.statusBar.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Google Drive Folder:";
            // 
            // gDPath
            // 
            this.gDPath.Location = new System.Drawing.Point(116, 15);
            this.gDPath.Name = "gDPath";
            this.gDPath.Size = new System.Drawing.Size(177, 20);
            this.gDPath.TabIndex = 4;
            // 
            // GDUploaderDemo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(422, 89);
            this.Controls.Add(this.gDPath);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.fullUploaderButton);
            this.Name = "GDUploaderDemo";
            this.Text = "GDUploaderDemo";
            this.statusBar.ResumeLayout(false);
            this.statusBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button fullUploaderButton;
        private System.Windows.Forms.FolderBrowserDialog uploadDialog;
        private System.Windows.Forms.StatusStrip statusBar;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox gDPath;
    }
}

