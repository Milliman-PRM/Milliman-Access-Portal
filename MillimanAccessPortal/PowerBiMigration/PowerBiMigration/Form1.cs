using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PowerBiLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PowerBiMigration
{
    public partial class Form1 : Form
    {
        private string _mapConnectionString = null;
        private DbContextOptions<ApplicationDbContext> _dbOptions = null;
        private PowerBiConfig _sourcePbiConfig = null;
        private PowerBiConfig _targetPbiConfig = null;
        private Dictionary<string, string> _groupMap = new Dictionary<string,string>();
        List<ProcessedItem> _processedItems = new List<ProcessedItem>();

        public Form1()
        {
            InitializeComponent();

            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appSettings.json", false);
            configBuilder.AddUserSecrets<Form1>(true);
            IConfigurationRoot appConfig = configBuilder.Build();

            lstClients.DisplayMember = "Name";
            lstContentItems.DisplayMember = "ContentName";
            lstPbiWorkspaces.DisplayMember = "GroupName";
            lstPowerBiReports.DisplayMember = "ReportName";

            // Initial CheckState value is CheckState.Indeterminate because I want the following to fire a CheckStateChanged event
            chkWriteFiles.CheckState = appConfig.GetValue("WriteFiles", false)
                ? CheckState.Checked
                : CheckState.Unchecked;

            _sourcePbiConfig = new PowerBiConfig
            {
                PbiGrantType = appConfig.GetValue<string>("SourcePbiGrantType"),
                PbiAuthenticationScope = appConfig.GetValue<string>("SourcePbiAuthenticationScope"),
                PbiAzureADClientId = appConfig.GetValue<string>("SourcePbiAzureADClientID"),
                PbiAzureADClientSecret = appConfig.GetValue<string>("SourcePbiAzureADClientSecret"),
                PbiAzureADUsername = appConfig.GetValue<string>("SourcePbiAzureADUsername"),
                PbiAzureADPassword = appConfig.GetValue<string>("SourcePbiAzureADPassword"),
                PbiTenantId = appConfig.GetValue<string>("SourcePbiTenantId"),
            };

            _targetPbiConfig = new PowerBiConfig
            {
                PbiGrantType = appConfig.GetValue<string>("TargetPbiGrantType"),
                PbiAuthenticationScope = appConfig.GetValue<string>("TargetPbiAuthenticationScope"),
                PbiAzureADClientId = appConfig.GetValue<string>("TargetPbiAzureADClientID"),
                PbiAzureADClientSecret = appConfig.GetValue<string>("TargetPbiAzureADClientSecret"),
                PbiAzureADUsername = appConfig.GetValue<string>("TargetPbiAzureADUsername"),
                PbiAzureADPassword = appConfig.GetValue<string>("TargetPbiAzureADPassword"),
                PbiTenantId = appConfig.GetValue<string>("TargetPbiTenantId"),
                PbiCapacityId = appConfig.GetValue<string>("TargetPbiCapacityId"),
            };

            _mapConnectionString = appConfig.GetConnectionString("MapDbConnection");
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_mapConnectionString).Options;
        }

        //private async void BtnGetAllSourceDocs_Click(object sender, EventArgs e)
        //{
        //    using (var db = new ApplicationDbContext(_dbOptions))
        //    {
        //        ContentType powerBiContentType = db.ContentType.Single(t => t.TypeEnum == ContentTypeEnum.PowerBi);

        //        List<RootContentItem> pbiContentItems = db.RootContentItem
        //                                                  //.Include(c => c.TypeSpecificDetailObject)
        //                                                  .Where(c => c.ContentTypeId == powerBiContentType.Id)
        //                                                  .ToList();

        //        PowerBiLibApi targetPbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();

        //        foreach (var pbiContentItem in pbiContentItems)
        //        {
        //            PowerBiContentItemProperties pbiSpecificDetail = pbiContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;
        //            if (!_groupMap.ContainsKey(pbiSpecificDetail.LiveWorkspaceId))
        //            {
        //                _groupMap[pbiSpecificDetail.LiveWorkspaceId] = await targetPbiApi.CreateGroupAsync(pbiContentItem.ClientId.ToString());
        //            }
        //        }

        //        lstContentItems.Items.AddRange(pbiContentItems.Select(c => new { c.ContentName, c }).ToArray());
        //    }

        //}

        //private void LstContentItems_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    var lstItem = lstContentItems.SelectedItem;
        //    btnAccessSelectedContent.Enabled = false;
        //    if (lstItem != null)
        //    {
        //        Type itemType = lstItem.GetType();
        //        PropertyInfo contentItemPropertyInfo = itemType.GetProperty("c");
        //        var contentItem = contentItemPropertyInfo.GetValue(lstItem) as RootContentItem;

        //        txtContentItemDetail.Text = JsonSerializer.Serialize(contentItem, contentItem.GetType(), new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        //        btnAccessSelectedContent.Enabled = true;
        //    }
        //}

        //private void BtnAccessSelectedContent_Click(object sender, EventArgs e)
        //{
        //    var lstItem = lstContentItems.SelectedItem;
        //    Type itemType = lstItem.GetType();
        //    PropertyInfo contentItemPropertyInfo = itemType.GetProperty("c");
        //    RootContentItem contentItem = contentItemPropertyInfo.GetValue(lstItem) as RootContentItem;
        //    TypeSpecificContentItemProperties metaData = contentItem.TypeSpecificDetailObject;

        //    Task<PowerBiLibApi> pbiApiTask = Task.Run(() => new PowerBiLibApi(_sourcePbiConfig).InitializeAsync());
        //    while (!pbiApiTask.IsCompleted) Thread.Sleep(100);
        //    PowerBiLibApi pbiApi = pbiApiTask.Result;

        //    MessageBox.Show($"api has AzureADClientId {pbiApi._config.PbiAzureADClientId}");
        //}

        //private async void BtnListTargetWorkspaces_Click(object sender, EventArgs e)
        //{
        //    var lstItem = lstContentItems.SelectedItem;
        //    Type itemType = lstItem.GetType();
        //    PropertyInfo contentItemPropertyInfo = itemType.GetProperty("c");
        //    RootContentItem contentItem = contentItemPropertyInfo.GetValue(lstItem) as RootContentItem;
        //    TypeSpecificContentItemProperties metaData = contentItem.TypeSpecificDetailObject;

        //    PowerBiLibApi sourcePbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();
        //    //Task<PowerBiLibApi> sourcePbiApiTask = Task.Run(() => new PowerBiLibApi(_targetPbiConfig).InitializeAsync());
        //    //while (!sourcePbiApiTask.IsCompleted) Thread.Sleep(100);
        //    //PowerBiLibApi sourcePbiApi = sourcePbiApiTask.Result;

        //    PowerBiLibApi targetPbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();
        //    //Task<PowerBiLibApi> targetBiApiTask = Task.Run(() => new PowerBiLibApi(_targetPbiConfig).InitializeAsync());
        //    //while (!targetBiApiTask.IsCompleted) Thread.Sleep(100);
        //    //PowerBiLibApi targetPbiApi = targetBiApiTask.Result;

        //    var allGroups = await sourcePbiApi.GetAllGroupsAsync();
        //    //var allGroupsTask = sourcePbiApi.GetAllGroupsAsync();
        //    //while (!allGroupsTask.IsCompleted) Thread.Sleep(100);
        //    //IList<Microsoft.PowerBI.Api.V2.Models.Group> allGroups = allGroupsTask.Result;

        //    MessageBox.Show($"api has AzureADClientId {targetPbiApi._config.PbiAzureADClientId}");
        //}

        private void LstSourceWorkspaces_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void TxtStorageFolder_MouseClick(object sender, MouseEventArgs e)
        {
            folderBrowserDialog1.SelectedPath = txtStorageFolder.Text;
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtStorageFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private async void BtnGetAllInventory_Click(object sender, EventArgs e)
        {
            using (new OperationScope(this, "Getting inventory"))
            {
                ClearAllLists();

                switch (Controls.OfType<RadioButton>().Single(b => b.Checked))
                {
                    case RadioButton b when b.Name == "radioSource":
                        using (var db = new ApplicationDbContext(_dbOptions))
                        {
                            List<RootContentItem> pbiContentItems = await db.RootContentItem
                                                                            .Include(c => c.Client)
                                                                            .Where(c => c.ContentType.TypeEnum == ContentTypeEnum.PowerBi)
                                                                            .ToListAsync();

                            List<Client> clients = pbiContentItems.Select(c => c.Client).Distinct(new IdPropertyComparer<Client>()).ToList();

                            PowerBiLibApi sourcePbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();
                            var allPbiGroups = await sourcePbiApi.GetAllGroupsAsync();

                            ClearAllLists();

                            foreach (Client client in clients)
                            {
                                GroupModel thisGroupModel = new GroupModel(allPbiGroups.SingleOrDefault(g => g.Name == client.Id.ToString()));
                                List<ReportModel> thisGroupReports = await sourcePbiApi.GetAllReportsOfGroupAsync(client.Id.ToString());

                                if (pbiContentItems.Count(c => c.ClientId == client.Id) > thisGroupReports.Count)
                                {
                                    int i = 8;
                                    // something is bad
                                }

                                lstClients.Items.Add(new { client.Name, client });
                                lstPbiWorkspaces.Items.Add(new { thisGroupModel.GroupName, Group = thisGroupModel });
                            }
                        }
                        break;

                    case RadioButton b when b.Name == "radioTarget":
                        PowerBiLibApi targetPbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();
                        IList<Microsoft.PowerBI.Api.V2.Models.Group> allGroups = await targetPbiApi.GetAllGroupsAsync();

                        foreach (var group in allGroups)
                        {
                            GroupModel thisGroupModel = new GroupModel(group);
                            lstPbiWorkspaces.Items.Add(new { thisGroupModel.GroupName, Group = thisGroupModel });
                        }
                        break;

                    default:
                        break;
                }
            }

        }

        private async void LstClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            using (new OperationScope(this))
            {
                object lstItem = lstClients.SelectedItem;
                if (lstItem != null)
                {
                    Type itemType = lstItem.GetType();
                    PropertyInfo contentItemPropertyInfo = itemType.GetProperty("client");
                    Client client = contentItemPropertyInfo.GetValue(lstItem) as Client;

                    using (var db = new ApplicationDbContext(_dbOptions))
                    {
                        lstContentItems.Items.Clear();

                        List<RootContentItem> contentItems = db.RootContentItem
                                                               .Where(c => c.ClientId == client.Id)
                                                               .Where(c => c.ContentType.TypeEnum == ContentTypeEnum.PowerBi)
                                                               .ToList();

                        lstContentItems.Items.AddRange(contentItems.Select(c => new { c.ContentName, c }).ToArray());

                        PowerBiLibApi pbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();

                        int workspaceIndex = lstPbiWorkspaces.FindStringExact(client.Id.ToString());
                        lstPbiWorkspaces.SelectedIndex = workspaceIndex;

                        lstPbiWorkspaces.TopIndex = lstClients.TopIndex;
                    }
                }
            }
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            PowerBiLibApi pbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();
            await pbiApi.DeleteThisMethod();
        }

        private async void LstPbiWorkspaces_SelectedIndexChanged(object sender, EventArgs e)
        {
            lstPowerBiReports.Items.Clear();
            txtReportDetails.Clear();

            object lstItem = lstPbiWorkspaces.SelectedItem;
            if (lstItem != null)
            {
                Type itemType = lstItem.GetType();
                PropertyInfo contentItemPropertyInfo = itemType.GetProperty("Group");
                GroupModel groupModel = contentItemPropertyInfo.GetValue(lstItem) as GroupModel;

                var radios = Controls.OfType<RadioButton>();

                PowerBiLibApi pbiApi = Controls.OfType<RadioButton>().Single(b => b.Checked).Name switch
                {
                    "radioSource" => await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync(),
                    "radioTarget" => await new PowerBiLibApi(_targetPbiConfig).InitializeAsync(),
                    _ => null
                };

                List<ReportModel> reports = await pbiApi.GetAllReportsOfGroupAsync(groupModel.GroupName);
                lstPowerBiReports.Items.AddRange(reports.Select(r => new { r.ReportName, Report = r }).ToArray());

                if (lstClients.Items.Count > 0)
                {
                    lstClients.SelectedIndex = lstPbiWorkspaces.SelectedIndex;
                    lstClients.TopIndex = lstPbiWorkspaces.TopIndex;
                }
            }
        }

        private async void BtnExportAll_Click(object sender, EventArgs e)
        {
            using (var operationScope = new OperationScope(this, "Exporting all Power BI content"))
            {
                if (chkWriteFiles.Enabled && chkWriteFiles.Checked)
                {
                    if (string.IsNullOrWhiteSpace(txtStorageFolder.Text))
                    {
                        MessageBox.Show($"A folder must be selected");
                        return;
                    }
                    else if (Directory.Exists(Path.GetDirectoryName(txtStorageFolder.Text)))
                    {
                        Directory.Delete(txtStorageFolder.Text, true);
                        Thread.Sleep(2000);  // because Windows I/O is asynchronous and sometimes stupid about it
                    }

                    Directory.CreateDirectory(txtStorageFolder.Text);

                    if (!Directory.Exists(Path.GetDirectoryName(txtStorageFolder.Text)))
                    {
                        MessageBox.Show($"Folder {txtStorageFolder.Text} was not created");
                        return;
                    }
                }

                List<Client> relevantClients = null;
                using (var db = new ApplicationDbContext(_dbOptions))
                {
                    List<RootContentItem> pbiContentItems = await db.RootContentItem
                                                                    .Include(c => c.Client)
                                                                    .Where(c => c.ContentType.TypeEnum == ContentTypeEnum.PowerBi)
                                                                    .ToListAsync();
                    relevantClients = pbiContentItems.Select(c => c.Client)
                                                     .Distinct(new IdPropertyComparer<Client>())
                                                     .ToList();

                    // Updating the database is all or nothing
                    var txn = db.Database.BeginTransaction();

                    foreach (Client client in relevantClients)
                    {
                        string newSubFolder = Path.Combine(txtStorageFolder.Text, client.Id.ToString());
                        if (chkWriteFiles.Enabled && chkWriteFiles.Checked)
                        {
                            Directory.CreateDirectory(newSubFolder);
                        }

                        // Query from DB not Power BI
                        List<RootContentItem> contentItems = null;
                        contentItems = db.RootContentItem
                                         .Include(c => c.ContentType)
                                         .Where(c => c.ContentType.TypeEnum == ContentTypeEnum.PowerBi)
                                         .Where(c => c.ClientId == client.Id)
                                         .ToList();

                        foreach (var contentItem in contentItems)
                        {
                            var newProcessedItem = await ExportOneContentItem(client, contentItem, newSubFolder, db);
                            _processedItems.Add(newProcessedItem);
                        }
                    }

                    if (_processedItems.All(i => i.Status == ProcessingStatus.DbUpdateSuccess))
                    {
                        txn.Commit();
                    }
                }

                MessageBox.Show($"Operation completed in {TimeSpan.FromMilliseconds(operationScope.operationTimer.ElapsedMilliseconds)}");
            }
        }

        private void ChkWriteFiles_CheckStateChanged(object sender, EventArgs e)
        {
            chkImportToTarget.Enabled = chkWriteFiles.Checked;
            if (chkImportToTarget.CheckState == CheckState.Indeterminate)
            {
                chkImportToTarget.CheckState = CheckState.Unchecked;
            }
        }

        private void ChkImportToTarget_CheckStateChanged(object sender, EventArgs e)
        {
            chkUpdateDatabase.Enabled = chkImportToTarget.Checked;
        }

        private void ChkImportToTarget_EnabledChanged(object sender, EventArgs e)
        {
            chkUpdateDatabase.Enabled = chkImportToTarget.Enabled && chkImportToTarget.Checked;
        }

        private void RadioSource_CheckedChanged(object sender, EventArgs e)
        {
            ClearAllLists();
        }

        private void RadioTarget_CheckedChanged(object sender, EventArgs e)
        {
            ClearAllLists();
        }

        private void ClearAllLists()
        {
            lstClients.Items.Clear();
            lstContentItems.Items.Clear();
            lstPbiWorkspaces.Items.Clear();
            lstPowerBiReports.Items.Clear();
            txtReportDetails.Clear();
        }

        private async void BtnExportSelectedClient_Click(object sender, EventArgs e)
        {
            using (var operationScope = new OperationScope(this, "Exporting selected Client Power BI content"))
            {
                Type itemType = lstClients.SelectedItem.GetType();
                PropertyInfo contentItemPropertyInfo = itemType.GetProperty("client");
                Client client = contentItemPropertyInfo.GetValue(lstClients.SelectedItem) as Client;

                if (chkWriteFiles.Enabled && chkWriteFiles.Checked)
                {
                    if (string.IsNullOrWhiteSpace(txtStorageFolder.Text))
                    {
                        MessageBox.Show($"A folder must be selected");
                        return;
                    }
                    else if (Directory.Exists(Path.GetDirectoryName(txtStorageFolder.Text)))
                    {
                        Directory.Delete(txtStorageFolder.Text, true);
                        Thread.Sleep(2000);  // because Windows I/O is asynchronous and sometimes stupid about it
                    }

                    Directory.CreateDirectory(txtStorageFolder.Text);

                    if (!Directory.Exists(Path.GetDirectoryName(txtStorageFolder.Text)))
                    {
                        MessageBox.Show($"Folder {txtStorageFolder.Text} was not created");
                        return;
                    }
                }

                string newSubFolder = Path.Combine(txtStorageFolder.Text, client.Id.ToString());
                if (chkWriteFiles.Enabled && chkWriteFiles.Checked)
                {
                    Directory.CreateDirectory(newSubFolder);
                }

                PowerBiLibApi sourcePbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();
                PowerBiLibApi targetPbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();

                using (var db = new ApplicationDbContext(_dbOptions))
                {
                    List<RootContentItem> pbiContentItems = await db.RootContentItem
                                                                    .Include(c => c.Client)
                                                                    .Include(c => c.ContentType)
                                                                    .Where(c => c.ContentType.TypeEnum == ContentTypeEnum.PowerBi)
                                                                    .Where(c => c.ClientId == client.Id)
                                                                    .ToListAsync();

                    foreach (RootContentItem contentItem in pbiContentItems)
                    {
                        ProcessedItem result = await ExportOneContentItem(client, contentItem, newSubFolder, db);
                    }
                }
            }
        }

        private async Task<ProcessedItem> ExportOneContentItem(Client client, RootContentItem contentItem, string clientFolder, ApplicationDbContext db)
        {
            Stopwatch timer = Stopwatch.StartNew();
            string logMsg = string.Empty;

            PowerBiLibApi sourcePbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();
            PowerBiContentItemProperties typeSpecificDetail = contentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;

            // Do the export from the source
            long itemExportStartMs = timer.ElapsedMilliseconds;
            var exportReturn = await sourcePbiApi.ExportReportAsync(typeSpecificDetail.LiveWorkspaceId, typeSpecificDetail.LiveReportId, clientFolder, chkWriteFiles.Checked);

            ProcessedItem newProcessedItem = new ProcessedItem
            {
                ClientId = contentItem.ClientId,
                ContentItemId = contentItem.Id,
                OldGroupId = client.Id.ToString(),
                ExportTime = TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds - itemExportStartMs),
            };

            switch (exportReturn.reportFilePath)
            {
                case null:
                    Log.Information($"Error while exporting content item {contentItem.ContentName}, time {newProcessedItem.ExportTime}");
                    newProcessedItem.Status = ProcessingStatus.ExportFail;
                    return newProcessedItem;
                case "":
                    Log.Information($"Content item <{contentItem.ContentName}> exported, not saved, time {newProcessedItem.ExportTime}");
                    newProcessedItem.Status = ProcessingStatus.ExportSuccess;
                    newProcessedItem.OldReportId = exportReturn.report.ReportId;
                    newProcessedItem.ReportName = exportReturn.report.ReportName;
                    break;
                default:
                    Log.Information($"Content item <{contentItem.ContentName}> exported, saved to file {exportReturn.reportFilePath}, time {newProcessedItem.ExportTime}");
                    newProcessedItem.Status = ProcessingStatus.FileSaveSuccess;
                    newProcessedItem.OldReportId = exportReturn.report.ReportId;
                    newProcessedItem.ReportName = exportReturn.report.ReportName;
                    break;
            }

            // if selected, do the import to the target
            if (chkImportToTarget.Enabled && chkImportToTarget.Checked && File.Exists(exportReturn.reportFilePath))
            {
                PowerBiLibApi targetPbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();

                long importStart = timer.ElapsedMilliseconds;
                PowerBiEmbedModel embedModel = await targetPbiApi.ImportPbixAsync(exportReturn.reportFilePath, client.Id.ToString());

                if (embedModel == null)
                {
                    Log.Error($"Import of report named <{exportReturn.report.ReportName}> to Power BI target failed");
                    newProcessedItem.Status = ProcessingStatus.ImportFail;
                    return newProcessedItem;
                }

                newProcessedItem.ImportTime = TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds - importStart);
                newProcessedItem.NewGroupId = embedModel.WorkspaceId;
                newProcessedItem.NewReportId = embedModel.ReportId;
                newProcessedItem.Status = ProcessingStatus.ImportSuccess;

                if (chkUpdateDatabase.Enabled && chkUpdateDatabase.Checked)
                {
                    typeSpecificDetail.LiveEmbedUrl = embedModel.EmbedUrl;
                    typeSpecificDetail.LiveReportId = embedModel.ReportId;
                    typeSpecificDetail.LiveWorkspaceId = embedModel.WorkspaceId;

                    contentItem.TypeSpecificDetailObject = typeSpecificDetail;
                    try
                    {
                        db.SaveChanges();
                    }
                    catch
                    {
                        newProcessedItem.Status = ProcessingStatus.DbUpdateFail;
                        return newProcessedItem;
                    }
                    newProcessedItem.Status = ProcessingStatus.DbUpdateSuccess;
                    Log.Information($"Database updated for content item {contentItem.ContentName}, {contentItem.Id}");
                }
            }

            return newProcessedItem;
        }

        private void LstPowerBiReports_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstPowerBiReports.SelectedIndex > -1)
            {
                Type itemType = lstPowerBiReports.SelectedItem.GetType();
                PropertyInfo reportModelPropertyInfo = itemType.GetProperty("Report");
                ReportModel reportModel = reportModelPropertyInfo.GetValue(lstPowerBiReports.SelectedItem) as ReportModel;
                txtReportDetails.Text = JsonSerializer.Serialize(reportModel, new JsonSerializerOptions { WriteIndented = true }); ;
            }
            else
            {
                txtReportDetails.Clear();
            }
        }
    }
}
