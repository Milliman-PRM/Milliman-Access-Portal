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
        private int _pendingOperationsCount = 0;
        private string _mapConnectionString = null;
        private DbContextOptions<ApplicationDbContext> _dbOptions = null;
        private PowerBiConfig _sourcePbiConfig = null;
        private PowerBiConfig _targetPbiConfig = null;
        private Dictionary<string, string> _groupMap = new Dictionary<string,string>();

        public Form1()
        {
            InitializeComponent();

            folderBrowserDialog1.ShowNewFolderButton = true;

            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appSettings.json", false);
            configBuilder.AddUserSecrets<Form1>(true);
            IConfigurationRoot _appConfig = configBuilder.Build();

            lstClients.DisplayMember = "Name";
            lstContentItems.DisplayMember = "ContentName";
            lstPbiWorkspaces.DisplayMember = "GroupName";
            lstPowerBiReports.DisplayMember = "ReportName";
            chkWriteFiles.Checked = _appConfig.GetValue("WriteFiles", false);

            _sourcePbiConfig = new PowerBiConfig
            {
                PbiGrantType = _appConfig.GetValue<string>("SourcePbiGrantType"),
                PbiAuthenticationScope = _appConfig.GetValue<string>("SourcePbiAuthenticationScope"),
                PbiAzureADClientId = _appConfig.GetValue<string>("SourcePbiAzureADClientID"),
                PbiAzureADClientSecret = _appConfig.GetValue<string>("SourcePbiAzureADClientSecret"),
                PbiAzureADUsername = _appConfig.GetValue<string>("SourcePbiAzureADUsername"),
                PbiAzureADPassword = _appConfig.GetValue<string>("SourcePbiAzureADPassword"),
                PbiTenantId = _appConfig.GetValue<string>("SourcePbiTenantId"),
            };

            _targetPbiConfig = new PowerBiConfig
            {
                PbiGrantType = _appConfig.GetValue<string>("TargetPbiGrantType"),
                PbiAuthenticationScope = _appConfig.GetValue<string>("TargetPbiAuthenticationScope"),
                PbiAzureADClientId = _appConfig.GetValue<string>("TargetPbiAzureADClientID"),
                PbiAzureADClientSecret = _appConfig.GetValue<string>("TargetPbiAzureADClientSecret"),
                PbiAzureADUsername = _appConfig.GetValue<string>("TargetPbiAzureADUsername"),
                PbiAzureADPassword = _appConfig.GetValue<string>("TargetPbiAzureADPassword"),
                PbiTenantId = _appConfig.GetValue<string>("TargetPbiTenantId"),
            };

            _mapConnectionString = _appConfig.GetConnectionString("MapDbConnection");
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
                using (var db = new ApplicationDbContext(_dbOptions))
                {
                    List<RootContentItem> pbiContentItems = await db.RootContentItem
                                                                    .Include(c => c.Client)
                                                                    .Where(c => c.ContentType.TypeEnum == ContentTypeEnum.PowerBi)
                                                                    .ToListAsync();

                    List<Client> clients = pbiContentItems.Select(c => c.Client).Distinct(new IdPropertyComparer<Client>()).ToList();

                    PowerBiLibApi sourcePbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();
                    var allPbiGroups = await sourcePbiApi.GetAllGroupsAsync();

                    lstClients.Items.Clear();
                    lstContentItems.Items.Clear();
                    lstPbiWorkspaces.Items.Clear();
                    lstPowerBiReports.Items.Clear();

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

                        //List<ReportModel> reports = await pbiApi.GetAllReportsOfGroupAsync(client.Id.ToString());
                        //lstPowerBiReports.Items.Clear();
                        //lstPowerBiReports.Items.AddRange(reports.Select(r => new { r.ReportName, r }).ToArray());
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

            object lstItem = lstPbiWorkspaces.SelectedItem;
            if (lstItem != null)
            {
                Type itemType = lstItem.GetType();
                PropertyInfo contentItemPropertyInfo = itemType.GetProperty("Group");
                GroupModel groupModel = contentItemPropertyInfo.GetValue(lstItem) as GroupModel;

                PowerBiLibApi pbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();

                List<ReportModel> reports = await pbiApi.GetAllReportsOfGroupAsync(groupModel.GroupName);
                lstPowerBiReports.Items.AddRange(reports.Select(r => new { r.ReportName, r }).ToArray());

                int clientIndex = lstClients.FindStringExact(groupModel.GroupName);
                lstContentItems.SelectedIndex = clientIndex;

                lstClients.TopIndex = lstPbiWorkspaces.TopIndex;
            }
        }

        private async void BtnExportAll_Click(object sender, EventArgs e)
        {
            using (new OperationScope(this))
            {
                Stopwatch operationTimer = new Stopwatch();
                operationTimer.Start();

                if (chkWriteFiles.Checked && !Directory.Exists(Path.GetDirectoryName(txtStorageFolder.Text)))  // Check that the parent folder exists
                {
                    MessageBox.Show($"Parent folder of target <{txtStorageFolder.Text}> does not exist");
                    return;
                }

                Directory.Delete(txtStorageFolder.Text, true);
                Thread.Sleep(2000);
                Directory.CreateDirectory(txtStorageFolder.Text);

                foreach (var lstItem in lstClients.Items)
                {
                    Type itemType = lstItem.GetType();
                    PropertyInfo contentItemPropertyInfo = itemType.GetProperty("client");
                    var client = contentItemPropertyInfo.GetValue(lstItem) as Client;

                    string newSubFolder = Path.Combine(txtStorageFolder.Text, client.Id.ToString());
                    Directory.CreateDirectory(newSubFolder);

                    PowerBiLibApi pbiApi = await new PowerBiLibApi(_sourcePbiConfig).InitializeAsync();

                    // Query from DB not Power BI
                    List<RootContentItem> contentItems = null;
                    using (var db = new ApplicationDbContext(_dbOptions))
                    {
                        contentItems = db.RootContentItem
                                         .Include(c => c.ContentType)
                                         .Where(c => c.ContentType.TypeEnum == ContentTypeEnum.PowerBi)
                                         .Where(c => c.ClientId == client.Id)
                                         .ToList();
                    }

                    foreach (var contentItem in contentItems)
                    {
                        PowerBiContentItemProperties typeSpecificDetail = contentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;

                        long itemStartMs = operationTimer.ElapsedMilliseconds;
                        var writtenFilePath = await pbiApi.ExportReportAsync(typeSpecificDetail.LiveWorkspaceId, typeSpecificDetail.LiveReportId, newSubFolder, chkWriteFiles.Checked);
                        long itemStopMs = operationTimer.ElapsedMilliseconds;

                        string msg = writtenFilePath switch
                            {
                                null => $"Error while processing content item {contentItem.ContentName}, time {(itemStopMs - itemStartMs) / 1000} seconds",
                                "" => $"Content item <{contentItem.ContentName}> processed, not saved, in {(itemStopMs - itemStartMs) / 1000} seconds",
                                _ => $"Content item <{contentItem.ContentName}> exported to file {writtenFilePath} in {(itemStopMs - itemStartMs) / 1000} seconds"
                            };
                        Log.Information(msg);
                    }
                }

                MessageBox.Show($"Operation completed in {operationTimer.ElapsedMilliseconds / 1000} seconds");
            }
        }
    }
}
