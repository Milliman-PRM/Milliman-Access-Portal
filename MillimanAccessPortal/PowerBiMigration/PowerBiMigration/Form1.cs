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
using System.Drawing;
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

        public Form1()
        {
            InitializeComponent();

            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appSettings.json", false);
            IConfigurationRoot _appConfig = configBuilder.Build();

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

            Log.Logger = new LoggerConfiguration()
                .CreateLogger();

            _mapConnectionString = _appConfig.GetConnectionString("MapDbConnection");
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_mapConnectionString).Options;

            lstContentItems.DisplayMember = "ContentName";
            btnAccessSelectedContent.Enabled = false;

            lstTargetWorkspaces.DisplayMember = "WorkspaceName";
        }

        private async void BtnGetAllSourceDocs_Click(object sender, EventArgs e)
        {
            using (var db = new ApplicationDbContext(_dbOptions))
            {
                ContentType powerBiContentType = db.ContentType.Single(t => t.TypeEnum == ContentTypeEnum.PowerBi);

                List<RootContentItem> pbiContentItems = db.RootContentItem
                                                          //.Include(c => c.TypeSpecificDetailObject)
                                                          .Where(c => c.ContentTypeId == powerBiContentType.Id)
                                                          .ToList();

                PowerBiLibApi targetPbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();

                foreach (var pbiContentItem in pbiContentItems)
                {
                    PowerBiContentItemProperties pbiSpecificDetail = pbiContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;
                    if (!_groupMap.ContainsKey(pbiSpecificDetail.LiveWorkspaceId))
                    {
                        _groupMap[pbiSpecificDetail.LiveWorkspaceId] = await targetPbiApi.CreateGroupAsync(pbiContentItem.ClientId.ToString());
                    }
                }

                lstContentItems.Items.AddRange(pbiContentItems.Select(c => new { c.ContentName, c }).ToArray());
            }

        }

        private void LstContentItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            var lstItem = lstContentItems.SelectedItem;
            btnAccessSelectedContent.Enabled = false;
            if (lstItem != null)
            {
                Type itemType = lstItem.GetType();
                PropertyInfo contentItemPropertyInfo = itemType.GetProperty("c");
                var contentItem = contentItemPropertyInfo.GetValue(lstItem) as RootContentItem;

                txtContentItemDetail.Text = JsonSerializer.Serialize(contentItem, contentItem.GetType(), new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

                btnAccessSelectedContent.Enabled = true;
            }
        }

        private void BtnAccessSelectedContent_Click(object sender, EventArgs e)
        {
            var lstItem = lstContentItems.SelectedItem;
            Type itemType = lstItem.GetType();
            PropertyInfo contentItemPropertyInfo = itemType.GetProperty("c");
            RootContentItem contentItem = contentItemPropertyInfo.GetValue(lstItem) as RootContentItem;
            TypeSpecificContentItemProperties metaData = contentItem.TypeSpecificDetailObject;

            Task<PowerBiLibApi> pbiApiTask = Task.Run(() => new PowerBiLibApi(_sourcePbiConfig).InitializeAsync());
            while (!pbiApiTask.IsCompleted) Thread.Sleep(100);
            PowerBiLibApi pbiApi = pbiApiTask.Result;

            MessageBox.Show($"api has AzureADClientId {pbiApi._config.PbiAzureADClientId}");
        }

        private async void BtnListTargetWorkspaces_Click(object sender, EventArgs e)
        {
            var lstItem = lstContentItems.SelectedItem;
            Type itemType = lstItem.GetType();
            PropertyInfo contentItemPropertyInfo = itemType.GetProperty("c");
            RootContentItem contentItem = contentItemPropertyInfo.GetValue(lstItem) as RootContentItem;
            TypeSpecificContentItemProperties metaData = contentItem.TypeSpecificDetailObject;

            PowerBiLibApi sourcePbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();
            //Task<PowerBiLibApi> sourcePbiApiTask = Task.Run(() => new PowerBiLibApi(_targetPbiConfig).InitializeAsync());
            //while (!sourcePbiApiTask.IsCompleted) Thread.Sleep(100);
            //PowerBiLibApi sourcePbiApi = sourcePbiApiTask.Result;

            PowerBiLibApi targetPbiApi = await new PowerBiLibApi(_targetPbiConfig).InitializeAsync();
            //Task<PowerBiLibApi> targetBiApiTask = Task.Run(() => new PowerBiLibApi(_targetPbiConfig).InitializeAsync());
            //while (!targetBiApiTask.IsCompleted) Thread.Sleep(100);
            //PowerBiLibApi targetPbiApi = targetBiApiTask.Result;

            var allGroups = await sourcePbiApi.GetAllGroupsAsync();
            //var allGroupsTask = sourcePbiApi.GetAllGroupsAsync();
            //while (!allGroupsTask.IsCompleted) Thread.Sleep(100);
            //IList<Microsoft.PowerBI.Api.V2.Models.Group> allGroups = allGroupsTask.Result;

            MessageBox.Show($"api has AzureADClientId {targetPbiApi._config.PbiAzureADClientId}");
        }
    }
}
