
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
            this.btnGetAllInventory = new System.Windows.Forms.Button();
            this.radioTarget = new System.Windows.Forms.RadioButton();
            this.radioSource = new System.Windows.Forms.RadioButton();
            this.lstPowerBiReports = new System.Windows.Forms.ListBox();
            this.lblPowerBiReports = new System.Windows.Forms.Label();
            this.lstPbiWorkspaces = new System.Windows.Forms.ListBox();
            this.lblPbiWorkspaces = new System.Windows.Forms.Label();
            this.lstClients = new System.Windows.Forms.ListBox();
            this.lblClients = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblContentItems = new System.Windows.Forms.Label();
            this.lstContentItems = new System.Windows.Forms.ListBox();
            this.txtStorageFolder = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.btnTest = new System.Windows.Forms.Button();
            this.btnExportAll = new System.Windows.Forms.Button();
            this.chkWriteFiles = new System.Windows.Forms.CheckBox();
            this.chkImportToTarget = new System.Windows.Forms.CheckBox();
            this.chkUpdateDatabase = new System.Windows.Forms.CheckBox();
            this.btnExportSelectedClient = new System.Windows.Forms.Button();
            this.txtReportDetails = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnGetAllInventory
            // 
            this.btnGetAllInventory.Location = new System.Drawing.Point(12, 142);
            this.btnGetAllInventory.Name = "btnGetAllInventory";
            this.btnGetAllInventory.Size = new System.Drawing.Size(105, 23);
            this.btnGetAllInventory.TabIndex = 0;
            this.btnGetAllInventory.Text = "Get All Inventory";
            this.btnGetAllInventory.UseVisualStyleBackColor = true;
            this.btnGetAllInventory.Click += new System.EventHandler(this.BtnGetAllInventory_Click);
            // 
            // radioTarget
            // 
            this.radioTarget.AutoSize = true;
            this.radioTarget.Location = new System.Drawing.Point(12, 97);
            this.radioTarget.Name = "radioTarget";
            this.radioTarget.Size = new System.Drawing.Size(57, 19);
            this.radioTarget.TabIndex = 11;
            this.radioTarget.Text = "Target";
            this.radioTarget.UseVisualStyleBackColor = true;
            this.radioTarget.CheckedChanged += new System.EventHandler(this.RadioTarget_CheckedChanged);
            // 
            // radioSource
            // 
            this.radioSource.AutoSize = true;
            this.radioSource.Checked = true;
            this.radioSource.Location = new System.Drawing.Point(12, 71);
            this.radioSource.Name = "radioSource";
            this.radioSource.Size = new System.Drawing.Size(61, 19);
            this.radioSource.TabIndex = 10;
            this.radioSource.TabStop = true;
            this.radioSource.Text = "Source";
            this.radioSource.UseVisualStyleBackColor = true;
            this.radioSource.CheckedChanged += new System.EventHandler(this.RadioSource_CheckedChanged);
            // 
            // lstPowerBiReports
            // 
            this.lstPowerBiReports.FormattingEnabled = true;
            this.lstPowerBiReports.ItemHeight = 15;
            this.lstPowerBiReports.Location = new System.Drawing.Point(583, 323);
            this.lstPowerBiReports.Name = "lstPowerBiReports";
            this.lstPowerBiReports.Size = new System.Drawing.Size(469, 94);
            this.lstPowerBiReports.TabIndex = 9;
            this.lstPowerBiReports.SelectedIndexChanged += new System.EventHandler(this.LstPowerBiReports_SelectedIndexChanged);
            // 
            // lblPowerBiReports
            // 
            this.lblPowerBiReports.AutoSize = true;
            this.lblPowerBiReports.Location = new System.Drawing.Point(583, 305);
            this.lblPowerBiReports.Name = "lblPowerBiReports";
            this.lblPowerBiReports.Size = new System.Drawing.Size(96, 15);
            this.lblPowerBiReports.TabIndex = 8;
            this.lblPowerBiReports.Text = "Power BI Reports";
            // 
            // lstPbiWorkspaces
            // 
            this.lstPbiWorkspaces.FormattingEnabled = true;
            this.lstPbiWorkspaces.ItemHeight = 15;
            this.lstPbiWorkspaces.Location = new System.Drawing.Point(583, 66);
            this.lstPbiWorkspaces.Name = "lstPbiWorkspaces";
            this.lstPbiWorkspaces.Size = new System.Drawing.Size(469, 229);
            this.lstPbiWorkspaces.TabIndex = 7;
            this.lstPbiWorkspaces.SelectedIndexChanged += new System.EventHandler(this.LstPbiWorkspaces_SelectedIndexChanged);
            // 
            // lblPbiWorkspaces
            // 
            this.lblPbiWorkspaces.AutoSize = true;
            this.lblPbiWorkspaces.Location = new System.Drawing.Point(583, 41);
            this.lblPbiWorkspaces.Name = "lblPbiWorkspaces";
            this.lblPbiWorkspaces.Size = new System.Drawing.Size(119, 15);
            this.lblPbiWorkspaces.TabIndex = 6;
            this.lblPbiWorkspaces.Text = "Power BI Workspaces";
            // 
            // lstClients
            // 
            this.lstClients.FormattingEnabled = true;
            this.lstClients.ItemHeight = 15;
            this.lstClients.Location = new System.Drawing.Point(122, 66);
            this.lstClients.Name = "lstClients";
            this.lstClients.Size = new System.Drawing.Size(455, 229);
            this.lstClients.TabIndex = 5;
            this.lstClients.SelectedIndexChanged += new System.EventHandler(this.LstClients_SelectedIndexChanged);
            // 
            // lblClients
            // 
            this.lblClients.AutoSize = true;
            this.lblClients.Location = new System.Drawing.Point(122, 41);
            this.lblClients.Name = "lblClients";
            this.lblClients.Size = new System.Drawing.Size(102, 15);
            this.lblClients.TabIndex = 4;
            this.lblClients.Text = "Database - Clients";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(365, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 15);
            this.label2.TabIndex = 3;
            // 
            // lblContentItems
            // 
            this.lblContentItems.AutoSize = true;
            this.lblContentItems.Location = new System.Drawing.Point(122, 305);
            this.lblContentItems.Name = "lblContentItems";
            this.lblContentItems.Size = new System.Drawing.Size(141, 15);
            this.lblContentItems.TabIndex = 2;
            this.lblContentItems.Text = "Database - Content Items";
            // 
            // lstContentItems
            // 
            this.lstContentItems.FormattingEnabled = true;
            this.lstContentItems.ItemHeight = 15;
            this.lstContentItems.Location = new System.Drawing.Point(122, 323);
            this.lstContentItems.Name = "lstContentItems";
            this.lstContentItems.Size = new System.Drawing.Size(455, 94);
            this.lstContentItems.TabIndex = 1;
            // 
            // txtStorageFolder
            // 
            this.txtStorageFolder.Location = new System.Drawing.Point(122, 13);
            this.txtStorageFolder.Name = "txtStorageFolder";
            this.txtStorageFolder.Size = new System.Drawing.Size(930, 23);
            this.txtStorageFolder.TabIndex = 5;
            this.txtStorageFolder.MouseClick += new System.Windows.Forms.MouseEventHandler(this.TxtStorageFolder_MouseClick);
            // 
            // btnTest
            // 
            this.btnTest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnTest.Location = new System.Drawing.Point(426, 478);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(75, 23);
            this.btnTest.TabIndex = 6;
            this.btnTest.Text = "Test";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.BtnTest_Click);
            // 
            // btnExportAll
            // 
            this.btnExportAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExportAll.Location = new System.Drawing.Point(175, 499);
            this.btnExportAll.Name = "btnExportAll";
            this.btnExportAll.Size = new System.Drawing.Size(159, 23);
            this.btnExportAll.TabIndex = 7;
            this.btnExportAll.Text = "Export All";
            this.btnExportAll.UseVisualStyleBackColor = true;
            this.btnExportAll.Click += new System.EventHandler(this.BtnExportAll_Click);
            // 
            // chkWriteFiles
            // 
            this.chkWriteFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkWriteFiles.AutoSize = true;
            this.chkWriteFiles.Checked = true;
            this.chkWriteFiles.CheckState = System.Windows.Forms.CheckState.Indeterminate;
            this.chkWriteFiles.Location = new System.Drawing.Point(12, 453);
            this.chkWriteFiles.Name = "chkWriteFiles";
            this.chkWriteFiles.Size = new System.Drawing.Size(80, 19);
            this.chkWriteFiles.TabIndex = 8;
            this.chkWriteFiles.Text = "Write Files";
            this.chkWriteFiles.UseVisualStyleBackColor = true;
            this.chkWriteFiles.CheckStateChanged += new System.EventHandler(this.ChkWriteFiles_CheckStateChanged);
            // 
            // chkImportToTarget
            // 
            this.chkImportToTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkImportToTarget.AutoSize = true;
            this.chkImportToTarget.Checked = true;
            this.chkImportToTarget.CheckState = System.Windows.Forms.CheckState.Indeterminate;
            this.chkImportToTarget.Location = new System.Drawing.Point(12, 478);
            this.chkImportToTarget.Name = "chkImportToTarget";
            this.chkImportToTarget.Size = new System.Drawing.Size(112, 19);
            this.chkImportToTarget.TabIndex = 9;
            this.chkImportToTarget.Text = "Import To Target";
            this.chkImportToTarget.UseVisualStyleBackColor = true;
            this.chkImportToTarget.CheckStateChanged += new System.EventHandler(this.ChkImportToTarget_CheckStateChanged);
            this.chkImportToTarget.EnabledChanged += new System.EventHandler(this.ChkImportToTarget_EnabledChanged);
            // 
            // chkUpdateDatabase
            // 
            this.chkUpdateDatabase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkUpdateDatabase.AutoSize = true;
            this.chkUpdateDatabase.Location = new System.Drawing.Point(13, 503);
            this.chkUpdateDatabase.Name = "chkUpdateDatabase";
            this.chkUpdateDatabase.Size = new System.Drawing.Size(115, 19);
            this.chkUpdateDatabase.TabIndex = 10;
            this.chkUpdateDatabase.Text = "Update Database";
            this.chkUpdateDatabase.UseVisualStyleBackColor = true;
            // 
            // btnExportSelectedClient
            // 
            this.btnExportSelectedClient.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExportSelectedClient.Location = new System.Drawing.Point(175, 470);
            this.btnExportSelectedClient.Name = "btnExportSelectedClient";
            this.btnExportSelectedClient.Size = new System.Drawing.Size(159, 23);
            this.btnExportSelectedClient.TabIndex = 11;
            this.btnExportSelectedClient.Text = "Export Selected Client";
            this.btnExportSelectedClient.UseVisualStyleBackColor = true;
            this.btnExportSelectedClient.Click += new System.EventHandler(this.BtnExportSelectedClient_Click);
            // 
            // txtReportDetails
            // 
            this.txtReportDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtReportDetails.Location = new System.Drawing.Point(583, 424);
            this.txtReportDetails.Multiline = true;
            this.txtReportDetails.Name = "txtReportDetails";
            this.txtReportDetails.Size = new System.Drawing.Size(471, 103);
            this.txtReportDetails.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 15);
            this.label1.TabIndex = 13;
            this.label1.Text = "Temporary Folder";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1066, 539);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtReportDetails);
            this.Controls.Add(this.radioTarget);
            this.Controls.Add(this.radioSource);
            this.Controls.Add(this.btnExportSelectedClient);
            this.Controls.Add(this.lstPowerBiReports);
            this.Controls.Add(this.chkUpdateDatabase);
            this.Controls.Add(this.lblPowerBiReports);
            this.Controls.Add(this.chkImportToTarget);
            this.Controls.Add(this.lstPbiWorkspaces);
            this.Controls.Add(this.chkWriteFiles);
            this.Controls.Add(this.lblPbiWorkspaces);
            this.Controls.Add(this.btnExportAll);
            this.Controls.Add(this.lstClients);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.lblClients);
            this.Controls.Add(this.txtStorageFolder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblContentItems);
            this.Controls.Add(this.btnGetAllInventory);
            this.Controls.Add(this.lstContentItems);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGetAllInventory;
        private System.Windows.Forms.TextBox txtStorageFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ListBox lstClients;
        private System.Windows.Forms.Label lblClients;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblContentItems;
        private System.Windows.Forms.ListBox lstContentItems;
        private System.Windows.Forms.ListBox lstPbiWorkspaces;
        private System.Windows.Forms.Label lblPbiWorkspaces;
        private System.Windows.Forms.ListBox lstPowerBiReports;
        private System.Windows.Forms.Label lblPowerBiReports;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button btnExportAll;
        private System.Windows.Forms.CheckBox chkWriteFiles;
        private System.Windows.Forms.CheckBox chkImportToTarget;
        private System.Windows.Forms.CheckBox chkUpdateDatabase;
        private System.Windows.Forms.RadioButton radioTarget;
        private System.Windows.Forms.RadioButton radioSource;
        private System.Windows.Forms.Button btnExportSelectedClient;
        private System.Windows.Forms.TextBox txtReportDetails;
        private System.Windows.Forms.Label label1;
    }
}

