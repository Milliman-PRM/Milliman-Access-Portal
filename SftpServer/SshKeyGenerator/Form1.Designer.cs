namespace SshKeyGenerator
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
            this.btnGenerate = new System.Windows.Forms.Button();
            this.lblExpirationYears = new System.Windows.Forms.Label();
            this.upDownExpirationYears = new System.Windows.Forms.NumericUpDown();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.lblSubject = new System.Windows.Forms.Label();
            this.lblSerialNumber = new System.Windows.Forms.Label();
            this.txtSubject = new System.Windows.Forms.TextBox();
            this.upDownSerialNumber = new System.Windows.Forms.NumericUpDown();
            this.radioLF = new System.Windows.Forms.RadioButton();
            this.radioCRLF = new System.Windows.Forms.RadioButton();
            this.btnCopyToClipboard = new System.Windows.Forms.Button();
            this.chkTranslateLineFeed = new System.Windows.Forms.CheckBox();
            this.grpLineFeedHandling = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.upDownExpirationYears)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownSerialNumber)).BeginInit();
            this.grpLineFeedHandling.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnGenerate
            // 
            this.btnGenerate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGenerate.FlatAppearance.BorderSize = 2;
            this.btnGenerate.Location = new System.Drawing.Point(12, 107);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(688, 23);
            this.btnGenerate.TabIndex = 0;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // lblExpirationYears
            // 
            this.lblExpirationYears.AutoSize = true;
            this.lblExpirationYears.Location = new System.Drawing.Point(12, 17);
            this.lblExpirationYears.Name = "lblExpirationYears";
            this.lblExpirationYears.Size = new System.Drawing.Size(98, 15);
            this.lblExpirationYears.TabIndex = 1;
            this.lblExpirationYears.Text = "Expiration (Years)";
            // 
            // upDownExpirationYears
            // 
            this.upDownExpirationYears.AutoSize = true;
            this.upDownExpirationYears.Location = new System.Drawing.Point(132, 15);
            this.upDownExpirationYears.Name = "upDownExpirationYears";
            this.upDownExpirationYears.Size = new System.Drawing.Size(47, 23);
            this.upDownExpirationYears.TabIndex = 2;
            this.upDownExpirationYears.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.Location = new System.Drawing.Point(12, 228);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.Size = new System.Drawing.Size(688, 415);
            this.txtOutput.TabIndex = 3;
            // 
            // lblSubject
            // 
            this.lblSubject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSubject.AutoSize = true;
            this.lblSubject.Location = new System.Drawing.Point(12, 83);
            this.lblSubject.Name = "lblSubject";
            this.lblSubject.Size = new System.Drawing.Size(49, 15);
            this.lblSubject.TabIndex = 4;
            this.lblSubject.Text = "Subject:";
            // 
            // lblSerialNumber
            // 
            this.lblSerialNumber.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSerialNumber.AutoSize = true;
            this.lblSerialNumber.Location = new System.Drawing.Point(12, 48);
            this.lblSerialNumber.Name = "lblSerialNumber";
            this.lblSerialNumber.Size = new System.Drawing.Size(85, 15);
            this.lblSerialNumber.TabIndex = 4;
            this.lblSerialNumber.Text = "Serial Number:";
            // 
            // txtSubject
            // 
            this.txtSubject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSubject.Location = new System.Drawing.Point(67, 78);
            this.txtSubject.Name = "txtSubject";
            this.txtSubject.Size = new System.Drawing.Size(633, 23);
            this.txtSubject.TabIndex = 5;
            this.txtSubject.Text = "CN=sftp.map.milliman.com";
            // 
            // upDownSerialNumber
            // 
            this.upDownSerialNumber.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownSerialNumber.Location = new System.Drawing.Point(103, 46);
            this.upDownSerialNumber.Name = "upDownSerialNumber";
            this.upDownSerialNumber.Size = new System.Drawing.Size(76, 23);
            this.upDownSerialNumber.TabIndex = 6;
            this.upDownSerialNumber.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // radioLF
            // 
            this.radioLF.AutoSize = true;
            this.radioLF.Checked = true;
            this.radioLF.Location = new System.Drawing.Point(103, 25);
            this.radioLF.Name = "radioLF";
            this.radioLF.Size = new System.Drawing.Size(37, 19);
            this.radioLF.TabIndex = 7;
            this.radioLF.TabStop = true;
            this.radioLF.Text = "\\n";
            this.radioLF.UseVisualStyleBackColor = true;
            // 
            // radioCRLF
            // 
            this.radioCRLF.AutoSize = true;
            this.radioCRLF.Location = new System.Drawing.Point(103, 50);
            this.radioCRLF.Name = "radioCRLF";
            this.radioCRLF.Size = new System.Drawing.Size(46, 19);
            this.radioCRLF.TabIndex = 8;
            this.radioCRLF.Text = "\\r\\n";
            this.radioCRLF.UseVisualStyleBackColor = true;
            // 
            // btnCopyToClipboard
            // 
            this.btnCopyToClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopyToClipboard.Location = new System.Drawing.Point(173, 199);
            this.btnCopyToClipboard.Name = "btnCopyToClipboard";
            this.btnCopyToClipboard.Size = new System.Drawing.Size(527, 23);
            this.btnCopyToClipboard.TabIndex = 0;
            this.btnCopyToClipboard.Text = "Copy Top Clipboard";
            this.btnCopyToClipboard.UseVisualStyleBackColor = true;
            this.btnCopyToClipboard.Click += new System.EventHandler(this.btnCopyToClipboard_Click);
            // 
            // chkTranslateLineFeed
            // 
            this.chkTranslateLineFeed.AutoSize = true;
            this.chkTranslateLineFeed.Location = new System.Drawing.Point(6, 23);
            this.chkTranslateLineFeed.Name = "chkTranslateLineFeed";
            this.chkTranslateLineFeed.Size = new System.Drawing.Size(61, 19);
            this.chkTranslateLineFeed.TabIndex = 9;
            this.chkTranslateLineFeed.Text = "Enable";
            this.chkTranslateLineFeed.UseVisualStyleBackColor = true;
            this.chkTranslateLineFeed.CheckedChanged += new System.EventHandler(this.chkTranslateLineFeed_CheckedChanged);
            // 
            // grpLineFeedHandling
            // 
            this.grpLineFeedHandling.Controls.Add(this.radioLF);
            this.grpLineFeedHandling.Controls.Add(this.chkTranslateLineFeed);
            this.grpLineFeedHandling.Controls.Add(this.radioCRLF);
            this.grpLineFeedHandling.Location = new System.Drawing.Point(12, 147);
            this.grpLineFeedHandling.Name = "grpLineFeedHandling";
            this.grpLineFeedHandling.Size = new System.Drawing.Size(155, 75);
            this.grpLineFeedHandling.TabIndex = 10;
            this.grpLineFeedHandling.TabStop = false;
            this.grpLineFeedHandling.Text = "Line Feeds to Text";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(712, 655);
            this.Controls.Add(this.grpLineFeedHandling);
            this.Controls.Add(this.btnCopyToClipboard);
            this.Controls.Add(this.upDownSerialNumber);
            this.Controls.Add(this.txtSubject);
            this.Controls.Add(this.lblSerialNumber);
            this.Controls.Add(this.lblSubject);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.upDownExpirationYears);
            this.Controls.Add(this.lblExpirationYears);
            this.Controls.Add(this.btnGenerate);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.upDownExpirationYears)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownSerialNumber)).EndInit();
            this.grpLineFeedHandling.ResumeLayout(false);
            this.grpLineFeedHandling.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Label lblExpirationYears;
        private System.Windows.Forms.NumericUpDown upDownExpirationYears;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Label lblSubject;
        private System.Windows.Forms.Label lblSerialNumber;
        private System.Windows.Forms.TextBox txtSubject;
        private System.Windows.Forms.NumericUpDown upDownSerialNumber;
        private System.Windows.Forms.RadioButton radioLF;
        private System.Windows.Forms.RadioButton radioCRLF;
        private System.Windows.Forms.Button btnCopyToClipboard;
        private System.Windows.Forms.CheckBox chkTranslateLineFeed;
        private System.Windows.Forms.GroupBox grpLineFeedHandling;
    }
}

