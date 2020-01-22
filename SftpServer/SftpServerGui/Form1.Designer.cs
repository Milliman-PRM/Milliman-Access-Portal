namespace SftpServerGui
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
            this.btnStartStop = new System.Windows.Forms.Button();
            this.openPrivateKeyFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.txtKeyPath = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(13, 13);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(75, 23);
            this.btnStartStop.TabIndex = 0;
            this.btnStartStop.Text = "Start";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(95, 13);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(131, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Find Private Key File";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtKeyPath
            // 
            this.txtKeyPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtKeyPath.Location = new System.Drawing.Point(232, 15);
            this.txtKeyPath.Name = "txtKeyPath";
            this.txtKeyPath.Size = new System.Drawing.Size(556, 20);
            this.txtKeyPath.TabIndex = 2;
            this.txtKeyPath.Text = "C:\\Users\\tom.puckett\\Desktop\\sftpPrivateKey.OpenSSH.pem";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtKeyPath);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnStartStop);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.OpenFileDialog openPrivateKeyFileDialog;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox txtKeyPath;
    }
}

