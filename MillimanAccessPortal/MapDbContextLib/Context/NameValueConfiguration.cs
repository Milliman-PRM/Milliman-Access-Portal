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
    }

    public enum NewGuidValueKeys
    {
        DatabaseInstanceGuid,
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

            #region Values from appSettings
            IConfiguration appConfig = serviceProvider.GetService<IConfiguration>();
            foreach (ConfiguredValueKeys key in Enum.GetValues(typeof(ConfiguredValueKeys)))
            {

                NameValueConfiguration dbRecord = await Db.NameValueConfiguration.SingleOrDefaultAsync(c => c.Key == key.GetDisplayNameString(false));
                string configValue = appConfig.GetValue<string>(key.GetDisplayNameString());

                if (dbRecord == null)
                {
                    Db.NameValueConfiguration.Add(new NameValueConfiguration 
                    { 
                        Key = key.GetDisplayNameString(false), 
                        Value = configValue ?? $"A value has not been set for required configuration key <{key}>." 
                    });
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
            #endregion

            #region Permanent Guid values
            foreach (NewGuidValueKeys key in Enum.GetValues(typeof(NewGuidValueKeys)))
            {
                NameValueConfiguration dbRecord = await Db.NameValueConfiguration.SingleOrDefaultAsync(c => c.Key == key.GetDisplayNameString(false));

                if (dbRecord == null)
                {
                    Db.NameValueConfiguration.Add(new NameValueConfiguration
                    {
                        Key = key.GetDisplayNameString(false),
                        Value = Guid.NewGuid().ToString(),
                    });
                }
                else
                {
                    continue;
                }

                await Db.SaveChangesAsync();
            }
            #endregion
        }

    }
}
