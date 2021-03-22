
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
            this.grpDatabase = new System.Windows.Forms.GroupBox();
            this.lstPowerBiReports = new System.Windows.Forms.ListBox();
            this.lblPowerBiReports = new System.Windows.Forms.Label();
            this.lstPbiWorkspaces = new System.Windows.Forms.ListBox();
            this.lblPbiWorkspaces = new System.Windows.Forms.Label();
            this.lstClients = new System.Windows.Forms.ListBox();
            this.lblClients = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblContentItems = new System.Windows.Forms.Label();
            this.lstContentItems = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtStorageFolder = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.btnTest = new System.Windows.Forms.Button();
            this.btnExportAll = new System.Windows.Forms.Button();
            this.grpDatabase.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnGetAllInventory
            // 
            this.btnGetAllInventory.Location = new System.Drawing.Point(6, 22);
            this.btnGetAllInventory.Name = "btnGetAllInventory";
            this.btnGetAllInventory.Size = new System.Drawing.Size(105, 23);
            this.btnGetAllInventory.TabIndex = 0;
            this.btnGetAllInventory.Text = "Get All Inventory";
            this.btnGetAllInventory.UseVisualStyleBackColor = true;
            this.btnGetAllInventory.Click += new System.EventHandler(this.BtnGetAllInventory_Click);
            // 
            // grpDatabase
            // 
            this.grpDatabase.Controls.Add(this.lstPowerBiReports);
            this.grpDatabase.Controls.Add(this.lblPowerBiReports);
            this.grpDatabase.Controls.Add(this.lstPbiWorkspaces);
            this.grpDatabase.Controls.Add(this.lblPbiWorkspaces);
            this.grpDatabase.Controls.Add(this.lstClients);
            this.grpDatabase.Controls.Add(this.lblClients);
            this.grpDatabase.Controls.Add(this.label2);
            this.grpDatabase.Controls.Add(this.lblContentItems);
            this.grpDatabase.Controls.Add(this.lstContentItems);
            this.grpDatabase.Controls.Add(this.btnGetAllInventory);
            this.grpDatabase.Location = new System.Drawing.Point(12, 42);
            this.grpDatabase.Name = "grpDatabase";
            this.grpDatabase.Size = new System.Drawing.Size(1040, 217);
            this.grpDatabase.TabIndex = 4;
            this.grpDatabase.TabStop = false;
            this.grpDatabase.Text = "Database";
            // 
            // lstPowerBiReports
            // 
            this.lstPowerBiReports.FormattingEnabled = true;
            this.lstPowerBiReports.ItemHeight = 15;
            this.lstPowerBiReports.Location = new System.Drawing.Point(806, 47);
            this.lstPowerBiReports.Name = "lstPowerBiReports";
            this.lstPowerBiReports.Size = new System.Drawing.Size(217, 154);
            this.lstPowerBiReports.TabIndex = 9;
            // 
            // lblPowerBiReports
            // 
            this.lblPowerBiReports.AutoSize = true;
            this.lblPowerBiReports.Location = new System.Drawing.Point(806, 22);
            this.lblPowerBiReports.Name = "lblPowerBiReports";
            this.lblPowerBiReports.Size = new System.Drawing.Size(96, 15);
            this.lblPowerBiReports.TabIndex = 8;
            this.lblPowerBiReports.Text = "Power BI Reports";
            // 
            // lstPbiWorkspaces
            // 
            this.lstPbiWorkspaces.FormattingEnabled = true;
            this.lstPbiWorkspaces.ItemHeight = 15;
            this.lstPbiWorkspaces.Location = new System.Drawing.Point(578, 47);
            this.lstPbiWorkspaces.Name = "lstPbiWorkspaces";
            this.lstPbiWorkspaces.Size = new System.Drawing.Size(217, 154);
            this.lstPbiWorkspaces.TabIndex = 7;
            this.lstPbiWorkspaces.SelectedIndexChanged += new System.EventHandler(this.LstPbiWorkspaces_SelectedIndexChanged);
            // 
            // lblPbiWorkspaces
            // 
            this.lblPbiWorkspaces.AutoSize = true;
            this.lblPbiWorkspaces.Location = new System.Drawing.Point(578, 22);
            this.lblPbiWorkspaces.Name = "lblPbiWorkspaces";
            this.lblPbiWorkspaces.Size = new System.Drawing.Size(119, 15);
            this.lblPbiWorkspaces.TabIndex = 6;
            this.lblPbiWorkspaces.Text = "Power BI Workspaces";
            // 
            // lstClients
            // 
            this.lstClients.FormattingEnabled = true;
            this.lstClients.ItemHeight = 15;
            this.lstClients.Location = new System.Drawing.Point(117, 47);
            this.lstClients.Name = "lstClients";
            this.lstClients.ScrollAlwaysVisible = true;
            this.lstClients.Size = new System.Drawing.Size(217, 154);
            this.lstClients.TabIndex = 5;
            this.lstClients.SelectedIndexChanged += new System.EventHandler(this.LstClients_SelectedIndexChanged);
            // 
            // lblClients
            // 
            this.lblClients.AutoSize = true;
            this.lblClients.Location = new System.Drawing.Point(117, 22);
            this.lblClients.Name = "lblClients";
            this.lblClients.Size = new System.Drawing.Size(102, 15);
            this.lblClients.TabIndex = 4;
            this.lblClients.Text = "Database - Clients";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(360, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 15);
            this.label2.TabIndex = 3;
            // 
            // lblContentItems
            // 
            this.lblContentItems.AutoSize = true;
            this.lblContentItems.Location = new System.Drawing.Point(345, 22);
            this.lblContentItems.Name = "lblContentItems";
            this.lblContentItems.Size = new System.Drawing.Size(141, 15);
            this.lblContentItems.TabIndex = 2;
            this.lblContentItems.Text = "Database - Content Items";
            // 
            // lstContentItems
            // 
            this.lstContentItems.FormattingEnabled = true;
            this.lstContentItems.ItemHeight = 15;
            this.lstContentItems.Location = new System.Drawing.Point(345, 47);
            this.lstContentItems.Name = "lstContentItems";
            this.lstContentItems.Size = new System.Drawing.Size(217, 154);
            this.lstContentItems.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Location = new System.Drawing.Point(12, 556);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1329, 123);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Target Capacity";
            // 
            // txtStorageFolder
            // 
            this.txtStorageFolder.Location = new System.Drawing.Point(12, 13);
            this.txtStorageFolder.Name = "txtStorageFolder";
            this.txtStorageFolder.Size = new System.Drawing.Size(1040, 23);
            this.txtStorageFolder.TabIndex = 5;
            this.txtStorageFolder.MouseClick += new System.Windows.Forms.MouseEventHandler(this.TxtStorageFolder_MouseClick);
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(318, 337);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(75, 23);
            this.btnTest.TabIndex = 6;
            this.btnTest.Text = "Test";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.BtnTest_Click);
            // 
            // btnExportAll
            // 
            this.btnExportAll.Location = new System.Drawing.Point(12, 265);
            this.btnExportAll.Name = "btnExportAll";
            this.btnExportAll.Size = new System.Drawing.Size(111, 23);
            this.btnExportAll.TabIndex = 7;
            this.btnExportAll.Text = "Export All";
            this.btnExportAll.UseVisualStyleBackColor = true;
            this.btnExportAll.Click += new System.EventHandler(this.BtnExportAll_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1064, 691);
            this.Controls.Add(this.btnExportAll);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.txtStorageFolder);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.grpDatabase);
            this.Name = "Form1";
            this.Text = "Form1";
            this.grpDatabase.ResumeLayout(false);
            this.grpDatabase.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGetAllInventory;
        private System.Windows.Forms.GroupBox grpDatabase;
        private System.Windows.Forms.GroupBox groupBox1;
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
    }
}

