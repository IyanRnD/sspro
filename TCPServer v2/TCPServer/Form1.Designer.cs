namespace TCPServer
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.Log = new System.Windows.Forms.TextBox();
            this.bwProcess = new System.ComponentModel.BackgroundWorker();
            this.button1 = new System.Windows.Forms.Button();
            this.ChAutoScroll = new System.Windows.Forms.CheckBox();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            // 
            // Log
            // 
            this.Log.Location = new System.Drawing.Point(6, 11);
            this.Log.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Log.Multiline = true;
            this.Log.Name = "Log";
            this.Log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.Log.Size = new System.Drawing.Size(586, 393);
            this.Log.TabIndex = 1;
            this.Log.TextChanged += new System.EventHandler(this.Log_TextChanged);
            // 
            // bwProcess
            // 
            this.bwProcess.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bwProcess_DoWork);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(597, 445);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "ClearLog";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // ChAutoScroll
            // 
            this.ChAutoScroll.AutoSize = true;
            this.ChAutoScroll.Checked = true;
            this.ChAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChAutoScroll.Location = new System.Drawing.Point(597, 422);
            this.ChAutoScroll.Name = "ChAutoScroll";
            this.ChAutoScroll.Size = new System.Drawing.Size(74, 17);
            this.ChAutoScroll.TabIndex = 6;
            this.ChAutoScroll.Text = "AutoScroll";
            this.ChAutoScroll.UseVisualStyleBackColor = true;
            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "ATG Service";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(681, 482);
            this.Controls.Add(this.ChAutoScroll);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Log);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Server";
            this.TopMost = true;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox Log;
        private System.ComponentModel.BackgroundWorker bwProcess;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox ChAutoScroll;
        private System.Windows.Forms.NotifyIcon notifyIcon;
    }
}

