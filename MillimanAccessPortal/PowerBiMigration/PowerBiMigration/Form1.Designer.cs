
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lstTargetWorkspaces = new System.Windows.Forms.ListBox();
            this.btnListTargetWorkspaces = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnGetAllDocs
            // 
            this.btnGetAllDocs.Location = new System.Drawing.Point(6, 22);
            this.btnGetAllDocs.Name = "btnGetAllDocs";
            this.btnGetAllDocs.Size = new System.Drawing.Size(199, 23);
            this.btnGetAllDocs.TabIndex = 0;
            this.btnGetAllDocs.Text = "Get All Documents";
            this.btnGetAllDocs.UseVisualStyleBackColor = true;
            this.btnGetAllDocs.Click += new System.EventHandler(this.BtnGetAllSourceDocs_Click);
            // 
            // lstContentItems
            // 
            this.lstContentItems.FormattingEnabled = true;
            this.lstContentItems.ItemHeight = 15;
            this.lstContentItems.Location = new System.Drawing.Point(6, 51);
            this.lstContentItems.Name = "lstContentItems";
            this.lstContentItems.Size = new System.Drawing.Size(199, 184);
            this.lstContentItems.TabIndex = 1;
            this.lstContentItems.SelectedIndexChanged += new System.EventHandler(this.LstContentItems_SelectedIndexChanged);
            // 
            // txtContentItemDetail
            // 
            this.txtContentItemDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtContentItemDetail.Location = new System.Drawing.Point(211, 22);
            this.txtContentItemDetail.Multiline = true;
            this.txtContentItemDetail.Name = "txtContentItemDetail";
            this.txtContentItemDetail.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtContentItemDetail.Size = new System.Drawing.Size(559, 214);
            this.txtContentItemDetail.TabIndex = 2;
            // 
            // btnAccessSelectedContent
            // 
            this.btnAccessSelectedContent.Location = new System.Drawing.Point(6, 242);
            this.btnAccessSelectedContent.Name = "btnAccessSelectedContent";
            this.btnAccessSelectedContent.Size = new System.Drawing.Size(199, 23);
            this.btnAccessSelectedContent.TabIndex = 3;
            this.btnAccessSelectedContent.Text = "Access Selected Content";
            this.btnAccessSelectedContent.UseVisualStyleBackColor = true;
            this.btnAccessSelectedContent.Click += new System.EventHandler(this.BtnAccessSelectedContent_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer1.Size = new System.Drawing.Size(776, 667);
            this.splitContainer1.SplitterDistance = 333;
            this.splitContainer1.TabIndex = 4;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnAccessSelectedContent);
            this.groupBox2.Controls.Add(this.txtContentItemDetail);
            this.groupBox2.Controls.Add(this.btnGetAllDocs);
            this.groupBox2.Controls.Add(this.lstContentItems);
            this.groupBox2.Location = new System.Drawing.Point(3, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(770, 327);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "groupBox2";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lstTargetWorkspaces);
            this.groupBox1.Controls.Add(this.btnListTargetWorkspaces);
            this.groupBox1.Location = new System.Drawing.Point(4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(769, 323);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Target Capacity";
            // 
            // lstTargetWorkspaces
            // 
            this.lstTargetWorkspaces.FormattingEnabled = true;
            this.lstTargetWorkspaces.ItemHeight = 15;
            this.lstTargetWorkspaces.Location = new System.Drawing.Point(7, 53);
            this.lstTargetWorkspaces.Name = "lstTargetWorkspaces";
            this.lstTargetWorkspaces.Size = new System.Drawing.Size(197, 94);
            this.lstTargetWorkspaces.TabIndex = 1;
            // 
            // btnListTargetWorkspaces
            // 
            this.btnListTargetWorkspaces.Location = new System.Drawing.Point(7, 23);
            this.btnListTargetWorkspaces.Name = "btnListTargetWorkspaces";
            this.btnListTargetWorkspaces.Size = new System.Drawing.Size(197, 23);
            this.btnListTargetWorkspaces.TabIndex = 0;
            this.btnListTargetWorkspaces.Text = "List Workspaces";
            this.btnListTargetWorkspaces.UseVisualStyleBackColor = true;
            this.btnListTargetWorkspaces.Click += new System.EventHandler(this.BtnListTargetWorkspaces_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 691);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnGetAllDocs;
        private System.Windows.Forms.ListBox lstContentItems;
        private System.Windows.Forms.TextBox txtContentItemDetail;
        private System.Windows.Forms.Button btnAccessSelectedContent;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox lstTargetWorkspaces;
        private System.Windows.Forms.Button btnListTargetWorkspaces;
    }
}

