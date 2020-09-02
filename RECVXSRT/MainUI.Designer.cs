namespace RECVXSRT
{
    partial class MainUI
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
            this.playerHealthStatus = new System.Windows.Forms.PictureBox();
            this.inventoryPanel = new DoubleBuffered.DoubleBufferedPanel();
            this.statisticsPanel = new DoubleBuffered.DoubleBufferedPanel();
            ((System.ComponentModel.ISupportInitialize)(this.playerHealthStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // playerHealthStatus
            // 
            this.playerHealthStatus.BackColor = System.Drawing.Color.Black;
            this.playerHealthStatus.Image = global::RECVXSRT.Properties.Resources.EMPTY;
            this.playerHealthStatus.InitialImage = global::RECVXSRT.Properties.Resources.EMPTY;
            this.playerHealthStatus.Location = new System.Drawing.Point(0, 0);
            this.playerHealthStatus.Margin = new System.Windows.Forms.Padding(0);
            this.playerHealthStatus.Name = "playerHealthStatus";
            this.playerHealthStatus.Size = new System.Drawing.Size(150, 60);
            this.playerHealthStatus.TabIndex = 0;
            this.playerHealthStatus.TabStop = false;
            this.playerHealthStatus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.playerHealthStatus_MouseDown);
            // 
            // inventoryPanel
            // 
            this.inventoryPanel.BackColor = System.Drawing.Color.Black;
            this.inventoryPanel.Location = new System.Drawing.Point(150, 0);
            this.inventoryPanel.Margin = new System.Windows.Forms.Padding(0);
            this.inventoryPanel.Name = "inventoryPanel";
            this.inventoryPanel.Size = new System.Drawing.Size(210, 504);
            this.inventoryPanel.TabIndex = 3;
            this.inventoryPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.inventoryPanel_MouseDown);
            // 
            // statisticsPanel
            // 
            this.statisticsPanel.BackColor = System.Drawing.Color.Black;
            this.statisticsPanel.Location = new System.Drawing.Point(1, 60);
            this.statisticsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.statisticsPanel.Name = "statisticsPanel";
            this.statisticsPanel.Size = new System.Drawing.Size(180, 240);
            this.statisticsPanel.TabIndex = 3;
            this.statisticsPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.statisticsPanel_MouseDown);
            // 
            // MainUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(360, 504);
            this.Controls.Add(this.statisticsPanel);
            this.Controls.Add(this.playerHealthStatus);
            this.Controls.Add(this.inventoryPanel);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainUI";
            this.ShowIcon = false;
            this.Text = "RE: CVX SRT";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainUI_FormClosed);
            this.Load += new System.EventHandler(this.MainUI_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainUI_MouseDown);
            ((System.ComponentModel.ISupportInitialize)(this.playerHealthStatus)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox playerHealthStatus;
        private DoubleBuffered.DoubleBufferedPanel inventoryPanel;
        private DoubleBuffered.DoubleBufferedPanel statisticsPanel;
    }
}