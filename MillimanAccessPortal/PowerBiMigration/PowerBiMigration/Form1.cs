using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PowerBiLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PowerBiMigration
{
    public partial class Form1 : Form
    {
        private string _mapConnectionString = null;
        private DbContextOptions<ApplicationDbContext> _dbOptions = null;
        private PowerBiConfig _pbiConfig = null;

        public Form1()
        {
            InitializeComponent();

            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appSettings.json", false);
            IConfigurationRoot _appConfig = configBuilder.Build();

            _pbiConfig = new PowerBiConfig
            {
                PbiGrantType = _appConfig.GetValue<string>("PbiGrantType"),
                PbiAuthenticationScope = _appConfig.GetValue<string>("PbiAuthenticationScope"),
                PbiAzureADClientId = _appConfig.GetValue<string>("PbiAzureADClientID"),
                PbiAzureADClientSecret = _appConfig.GetValue<string>("PbiAzureADClientSecret"),
                PbiAzureADUsername = _appConfig.GetValue<string>("PbiAzureADUsername"),
                PbiAzureADPassword = _appConfig.GetValue<string>("PbiAzureADPassword"),
                PbiTenantId = _appConfig.GetValue<string>("PbiTenantId"),
            };

            _mapConnectionString = "Server=localhost;Database=MillimanAccessPortalWithGuids;User Id=postgres;Password=postgres;";
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_mapConnectionString).Options;

            lstContentItems.DisplayMember = "ContentName";
            btnAccessSelectedContent.Enabled = false;
        }

        private void BtnGetAllDocs_Click(object sender, EventArgs e)
        {
            using (var db = new ApplicationDbContext(_dbOptions))
            {
                ContentType powerBiContentType = db.ContentType.Single(t => t.TypeEnum == ContentTypeEnum.PowerBi);

                List<RootContentItem> pbiContentItems = db.RootContentItem
                                                          //.Include(c => c.TypeSpecificDetailObject)
                                                          .Where(c => c.ContentTypeId == powerBiContentType.Id)
                                                          .ToList();

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

                txtContentItemDetail.Text = JsonSerializer.Serialize(contentItem, contentItem.GetType(), new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true });

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

            Task<PowerBiLibApi> pbiApiTask = Task.Run(() => new PowerBiLibApi(_pbiConfig).InitializeAsync());
            while (!pbiApiTask.IsCompleted) Thread.Sleep(100);
            var pbiApi = pbiApiTask.Result;
        }
    }
}
