/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide profit center information for display in the system admin detail panel
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ProfitCenterDetail
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Office { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }

        public static explicit operator ProfitCenterDetail(ProfitCenter profitCenter)
        {
            if (profitCenter == null)
            {
                return null;
            }

            return new ProfitCenterDetail
            {
                Id = profitCenter.Id,
                Name = profitCenter.Name,
                Code = profitCenter.ProfitCenterCode,
                Office = profitCenter.MillimanOffice,
                ContactName = profitCenter.ContactName,
                ContactEmail = profitCenter.ContactEmail,
                ContactPhone = profitCenter.ContactPhone,
            };
        }

    }
}
