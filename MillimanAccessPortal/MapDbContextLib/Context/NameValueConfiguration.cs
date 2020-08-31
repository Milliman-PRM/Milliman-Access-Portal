/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MapDbContextLib.Context
{
    public enum ConfiguredValueKeys
    {
        UserAgreementText,
        ClientReviewRenewalPeriodDays,
        ClientReviewEarlyWarningDays,
        ClientReviewGracePeriodDays,
    }

    public class NameValueConfiguration
    {
        [Key]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        internal static async Task InitializeNameValueConfigurationAsync(IServiceProvider serviceProvider)
        {
            ApplicationDbContext Db = serviceProvider.GetService<ApplicationDbContext>();
            IConfiguration appConfig = serviceProvider.GetService<IConfiguration>();

            foreach (ConfiguredValueKeys key in Enum.GetValues(typeof(ConfiguredValueKeys)))
            {
                NameValueConfiguration dbRecord = await Db.NameValueConfiguration.SingleOrDefaultAsync(c => c.Key == key.ToString());
                string configValue = appConfig.GetValue<string>(key.ToString());

                if (dbRecord == null)
                {
                    Db.NameValueConfiguration.Add(new NameValueConfiguration { Key = key.ToString(), Value = "This configuration item has not been set." });
                }
                else if (configValue != null && dbRecord.Value != configValue)
                {
                    dbRecord.Value = configValue;
                }
                else
                {
                    continue;
                }

                await Db.SaveChangesAsync();
            }
        }
        
    }
}
