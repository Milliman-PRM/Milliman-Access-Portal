/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MapDbContextLib.Context
{
    public enum ConfiguredValueKeys
    {
        UserAgreementText,
    }

    public class NameValueConfiguration
    {
        [Key]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        internal static void InitializeNameValueConfiguration(IServiceProvider serviceProvider)
        {
            ApplicationDbContext Db = serviceProvider.GetService<ApplicationDbContext>();

            foreach (ConfiguredValueKeys key in Enum.GetValues(typeof(ConfiguredValueKeys)))
            {
                if (!Db.NameValueConfiguration.Any(c => c.Key == key.ToString()))
                {
                    Db.NameValueConfiguration.Add(new NameValueConfiguration { Key = key.ToString(), Value = "This configuration item has not been set." });
                    Db.SaveChanges();
                }
            }
        }
        
    }
}
