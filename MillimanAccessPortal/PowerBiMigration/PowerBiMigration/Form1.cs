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
                        IList<Microsoft.PowerBI.Api.Models.Group> allGroups = await targetPbiApi.GetAllGroupsAsync();

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
                            Log.Information($"Preparing to process content item <{contentItem.ContentName}> ({contentItem.Id}) in Client {client.Name} ({client.Id})");
                            ProcessedItem newProcessedItem = await ExportOneContentItem(client, contentItem, newSubFolder, db);
                            _processedItems.Add(newProcessedItem);
                        }
                    }

                    if (chkUpdateDatabase.Enabled && chkUpdateDatabase.Checked)
                    {
                        if (_processedItems.All(i => i.Status == ProcessingStatus.DbUpdateSuccess))
                        {
                            DialogResult confirmation = MessageBox.Show("Please review the operation log and confirm that no errors are indicated. Click \"Yes\" to commit all updates to the MAP application database", "Processing Complete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                            if (confirmation == DialogResult.Yes)
                            {
                                txn.Commit();
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Not all content metadata was successfully updated in the database{Environment.NewLine}" +
                                string.Join(Environment.NewLine, _processedItems.Where(i => i.Status != ProcessingStatus.DbUpdateSuccess).Select(i => $"Client {i.ClientName}, Content {i.ContentItemName}, Status {i.Status}"))
                                );
                        }
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
            ProcessedItem newProcessedItem = new ProcessedItem
            {
                ClientId = contentItem.ClientId,
                ClientName = client.Name,
                ContentItemId = contentItem.Id,
                ContentItemName = contentItem.ContentName,
                OldGroupId = client.Id,
            };

            Stopwatch timer = Stopwatch.StartNew();

            PowerBiLibApi sourcePbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();
            PowerBiContentItemProperties typeSpecificDetail = contentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;

            Log.Information($"Preparing to export from workspace ID {typeSpecificDetail.LiveWorkspaceId}, report ID {typeSpecificDetail.LiveReportId}");

            // Do the export from the source
            long itemExportStartMs = timer.ElapsedMilliseconds;
            var exportReturn = await sourcePbiApi.ExportReportAsync(typeSpecificDetail.LiveWorkspaceId.Value, typeSpecificDetail.LiveReportId.Value, clientFolder, chkWriteFiles.Checked);

            newProcessedItem.ExportTime = TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds - itemExportStartMs);

            switch (exportReturn.reportFilePath)
            {
                case null:
                    Log.Error($"Error while exporting content item {contentItem.ContentName}, time {newProcessedItem.ExportTime}");
                    newProcessedItem.Status = ProcessingStatus.ExportFail;
                    return newProcessedItem;
                case "":
                    Log.Information($"Exported, not saved, time {newProcessedItem.ExportTime}");
                    newProcessedItem.Status = ProcessingStatus.ExportSuccess;
                    newProcessedItem.OldReportId = exportReturn.report.ReportId;
                    newProcessedItem.ReportName = exportReturn.report.ReportName;
                    break;
                default:
                    Log.Information($"Exported and saved to file {exportReturn.reportFilePath}, time {newProcessedItem.ExportTime}");
                    newProcessedItem.Status = ProcessingStatus.FileSaveSuccess;
                    newProcessedItem.OldReportId = exportReturn.report.ReportId;
                    newProcessedItem.ReportName = exportReturn.report.ReportName;
                    break;
            }

            // if selected, do the import to the target
            if (chkImportToTarget.Enabled && chkImportToTarget.Checked && File.Exists(exportReturn.reportFilePath))
            {
                Log.Information($"Preparing to import to target, report name <{exportReturn.report.ReportName}>");

                PowerBiLibApi targetPbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();

                long importStart = timer.ElapsedMilliseconds;
                PowerBiEmbedModel embedModel = await targetPbiApi.ImportPbixAsync(exportReturn.reportFilePath, client.Id.ToString(), Guid.Parse(_targetPbiConfig.PbiCapacityId));
                newProcessedItem.ImportTime = TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds - importStart);

                if (embedModel == null)
                {
                    newProcessedItem.Status = ProcessingStatus.ImportFail;
                    Log.Error($"Import failed to Power BI target");
                    return newProcessedItem;
                }
                else
                {
                    newProcessedItem.NewGroupId = embedModel.WorkspaceId;
                    newProcessedItem.NewReportId = embedModel.ReportId;
                    newProcessedItem.Status = ProcessingStatus.ImportSuccess;
                    Log.Information($"Import completed to target, new workspace Id {embedModel.WorkspaceId}, new report Id {embedModel.ReportId}");
                }

                if (chkUpdateDatabase.Enabled && chkUpdateDatabase.Checked)
                {
                    Log.Information("Preparing to save new embed model properties to database");

                    typeSpecificDetail.LiveEmbedUrl = embedModel.EmbedUrl;
                    typeSpecificDetail.LiveReportId = embedModel.ReportId;
                    typeSpecificDetail.LiveWorkspaceId = embedModel.WorkspaceId;

                    contentItem.TypeSpecificDetailObject = typeSpecificDetail;
                    try
                    {
                        db.SaveChanges();
                        newProcessedItem.Status = ProcessingStatus.DbUpdateSuccess;
                        Log.Information($"Pending transaction commit, database successfully updated");
                    }
                    catch (Exception ex)
                    {
                        newProcessedItem.Status = ProcessingStatus.DbUpdateFail;
                        Log.Error(ex, "Exception while saving new embed model properties to database");
                        return newProcessedItem;
                    }
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

        private async void GetCapacityIDs_Click(object sender, EventArgs e)
        {
            using (new OperationScope(this, "Getting capacity IDs")) ;
            PowerBiLibApi pbiApi = Controls.OfType<RadioButton>().Single(b => b.Checked) switch
            {
                RadioButton b when b.Name == "radioSource" => await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync(),
                RadioButton b when b.Name == "radioTarget" => await new PowerBiLibApi(_targetPbiConfig).InitializeAsync(),
                _ => throw new ApplicationException("Unhandled radio button is checked"),
            };

            string capacityInfo = await pbiApi.GetAllCapacityInfo();
            MessageBox.Show($"Capacities for the {Controls.OfType<RadioButton>().Single(b => b.Checked).Name.Replace("radio", "")} credentials:{Environment.NewLine}{capacityInfo}");
        }
    }
}
