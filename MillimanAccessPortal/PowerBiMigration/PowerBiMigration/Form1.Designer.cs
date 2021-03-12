
namespace PowerBiMigration
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
            this.btnGetAllDocs = new System.Windows.Forms.Button();
            this.lstContentItems = new System.Windows.Forms.ListBox();
            this.txtContentItemDetail = new System.Windows.Forms.TextBox();
            this.btnAccessSelectedContent = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnGetAllDocs
            // 
            this.btnGetAllDocs.Location = new System.Drawing.Point(12, 12);
            this.btnGetAllDocs.Name = "btnGetAllDocs";
            this.btnGetAllDocs.Size = new System.Drawing.Size(199, 23);
            this.btnGetAllDocs.TabIndex = 0;
            this.btnGetAllDocs.Text = "Get All Documents";
            this.btnGetAllDocs.UseVisualStyleBackColor = true;
            this.btnGetAllDocs.Click += new System.EventHandler(this.BtnGetAllDocs_Click);
            // 
            // lstContentItems
            // 
            this.lstContentItems.FormattingEnabled = true;
            this.lstContentItems.ItemHeight = 15;
            this.lstContentItems.Location = new System.Drawing.Point(13, 42);
            this.lstContentItems.Name = "lstContentItems";
            this.lstContentItems.Size = new System.Drawing.Size(198, 94);
            this.lstContentItems.TabIndex = 1;
            this.lstContentItems.SelectedIndexChanged += new System.EventHandler(this.LstContentItems_SelectedIndexChanged);
            // 
            // txtContentItemDetail
            // 
            this.txtContentItemDetail.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtContentItemDetail.Location = new System.Drawing.Point(217, 42);
            this.txtContentItemDetail.Multiline = true;
            this.txtContentItemDetail.Name = "txtContentItemDetail";
            this.txtContentItemDetail.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtContentItemDetail.Size = new System.Drawing.Size(571, 178);
            this.txtContentItemDetail.TabIndex = 2;
            // 
            // btnAccessSelectedContent
            // 
            this.btnAccessSelectedContent.Location = new System.Drawing.Point(12, 226);
            this.btnAccessSelectedContent.Name = "btnAccessSelectedContent";
            this.btnAccessSelectedContent.Size = new System.Drawing.Size(199, 23);
            this.btnAccessSelectedContent.TabIndex = 3;
            this.btnAccessSelectedContent.Text = "Access Selected Content";
            this.btnAccessSelectedContent.UseVisualStyleBackColor = true;
            this.btnAccessSelectedContent.Click += new System.EventHandler(this.BtnAccessSelectedContent_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnAccessSelectedContent);
            this.Controls.Add(this.txtContentItemDetail);
            this.Controls.Add(this.lstContentItems);
            this.Controls.Add(this.btnGetAllDocs);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGetAllDocs;
        private System.Windows.Forms.ListBox lstContentItems;
        private System.Windows.Forms.TextBox txtContentItemDetail;
        private System.Windows.Forms.Button btnAccessSelectedContent;
    }
}

