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
            this.textPassword = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textHash = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonStorePassword = new System.Windows.Forms.Button();
            this.buttonVerifyPassword = new System.Windows.Forms.Button();
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
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textHash);
            this.groupBox1.Controls.Add(this.buttonStorePassword);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.buttonVerifyPassword);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.textPassword);
            this.groupBox1.Location = new System.Drawing.Point(318, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(470, 236);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Password Hashing";
            // 
            // textPassword
            // 
            this.textPassword.Location = new System.Drawing.Point(6, 41);
            this.textPassword.Name = "textPassword";
            this.textPassword.Size = new System.Drawing.Size(458, 23);
            this.textPassword.TabIndex = 0;
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
            // textHash
            // 
            this.textHash.Location = new System.Drawing.Point(6, 85);
            this.textHash.Name = "textHash";
            this.textHash.Size = new System.Drawing.Size(458, 23);
            this.textHash.TabIndex = 0;
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
            // buttonStorePassword
            // 
            this.buttonStorePassword.Location = new System.Drawing.Point(6, 207);
            this.buttonStorePassword.Name = "buttonStorePassword";
            this.buttonStorePassword.Size = new System.Drawing.Size(212, 23);
            this.buttonStorePassword.TabIndex = 2;
            this.buttonStorePassword.Text = "Store Password";
            this.buttonStorePassword.UseVisualStyleBackColor = true;
            this.buttonStorePassword.Click += new System.EventHandler(this.buttonStorePassword_Click);
            // 
            // buttonVerifyPassword
            // 
            this.buttonVerifyPassword.Location = new System.Drawing.Point(252, 207);
            this.buttonVerifyPassword.Name = "buttonVerifyPassword";
            this.buttonVerifyPassword.Size = new System.Drawing.Size(212, 23);
            this.buttonVerifyPassword.TabIndex = 2;
            this.buttonVerifyPassword.Text = "Verify Password";
            this.buttonVerifyPassword.UseVisualStyleBackColor = true;
            this.buttonVerifyPassword.Click += new System.EventHandler(this.buttonVerifyPassword_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnStartStop);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

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
    }
}

