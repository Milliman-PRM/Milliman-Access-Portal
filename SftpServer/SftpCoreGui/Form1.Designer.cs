namespace SftpCoreGui
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnStartStop = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textHash = new System.Windows.Forms.TextBox();
            this.buttonStorePassword = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonVerifyPassword = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textPassword = new System.Windows.Forms.TextBox();
            this.textKeyfilePath = new System.Windows.Forms.TextBox();
            this.buttonReportServerState = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(12, 12);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(75, 23);
            this.btnStartStop.TabIndex = 0;
            this.btnStartStop.Text = "Start";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.BtnStartStop_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.textHash);
            this.groupBox1.Controls.Add(this.buttonStorePassword);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.buttonVerifyPassword);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.textPassword);
            this.groupBox1.Location = new System.Drawing.Point(12, 273);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(952, 165);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Password Hashing";
            // 
            // textHash
            // 
            this.textHash.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textHash.Location = new System.Drawing.Point(6, 85);
            this.textHash.Name = "textHash";
            this.textHash.Size = new System.Drawing.Size(940, 23);
            this.textHash.TabIndex = 0;
            this.textHash.TextChanged += new System.EventHandler(this.textHash_TextChanged);
            // 
            // buttonStorePassword
            // 
            this.buttonStorePassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStorePassword.Location = new System.Drawing.Point(6, 136);
            this.buttonStorePassword.Name = "buttonStorePassword";
            this.buttonStorePassword.Size = new System.Drawing.Size(212, 23);
            this.buttonStorePassword.TabIndex = 2;
            this.buttonStorePassword.Text = "Hash Password";
            this.buttonStorePassword.UseVisualStyleBackColor = true;
            this.buttonStorePassword.Click += new System.EventHandler(this.ButtonHashPassword_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "Hash";
            // 
            // buttonVerifyPassword
            // 
            this.buttonVerifyPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonVerifyPassword.Location = new System.Drawing.Point(734, 136);
            this.buttonVerifyPassword.Name = "buttonVerifyPassword";
            this.buttonVerifyPassword.Size = new System.Drawing.Size(212, 23);
            this.buttonVerifyPassword.TabIndex = 2;
            this.buttonVerifyPassword.Text = "Verify Password";
            this.buttonVerifyPassword.UseVisualStyleBackColor = true;
            this.buttonVerifyPassword.Click += new System.EventHandler(this.ButtonVerifyPassword_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Password";
            // 
            // textPassword
            // 
            this.textPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textPassword.Location = new System.Drawing.Point(6, 41);
            this.textPassword.Name = "textPassword";
            this.textPassword.Size = new System.Drawing.Size(940, 23);
            this.textPassword.TabIndex = 0;
            this.textPassword.TextChanged += new System.EventHandler(this.textPassword_TextChanged);
            // 
            // textKeyfilePath
            // 
            this.textKeyfilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textKeyfilePath.Location = new System.Drawing.Point(12, 42);
            this.textKeyfilePath.Name = "textKeyfilePath";
            this.textKeyfilePath.Size = new System.Drawing.Size(946, 23);
            this.textKeyfilePath.TabIndex = 2;
            this.textKeyfilePath.Text = "C:\\Users\\tom.puckett\\Desktop\\sftpPrivateKey.OpenSSH.pem";
            // 
            // buttonReportServerState
            // 
            this.buttonReportServerState.Location = new System.Drawing.Point(12, 79);
            this.buttonReportServerState.Name = "buttonReportServerState";
            this.buttonReportServerState.Size = new System.Drawing.Size(135, 41);
            this.buttonReportServerState.TabIndex = 3;
            this.buttonReportServerState.Text = "Report SSH Key Fingerprint";
            this.buttonReportServerState.UseVisualStyleBackColor = true;
            this.buttonReportServerState.Click += new System.EventHandler(this.ButtonReportReportServerState_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(976, 450);
            this.Controls.Add(this.buttonReportServerState);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textKeyfilePath);
            this.Controls.Add(this.btnStartStop);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonVerifyPassword;
        private System.Windows.Forms.Button buttonStorePassword;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textHash;
        private System.Windows.Forms.TextBox textPassword;
        private System.Windows.Forms.TextBox textKeyfilePath;
        private System.Windows.Forms.Button buttonReportServerState;
    }
}

