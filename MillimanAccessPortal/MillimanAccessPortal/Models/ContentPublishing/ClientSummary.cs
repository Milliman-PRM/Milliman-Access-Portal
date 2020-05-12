/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Minimal representation of a client
 * DEVELOPER NOTES:
 */

using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.AccountViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ContentPublishing
{

    public class ClientSummary : Nestable
    {
        public string Name { get; set; }
        public string Code { get; set; }

        public bool CanManage { get; set; }
        public long EligibleUserCount { get; set; }
        public long RootContentItemCount { get; set; }
        
        public static async Task<ClientSummary> BuildAsync(ApplicationDbContext dbContext, ApplicationUser currentUser, Client client, RoleEnum roleInClient)
        {
            var clientDetail = new ClientSummary
            {
                Id = client.Id,
                ParentId = client.ParentClientId,
                Name = client.Name,
                Code = client.ClientCode,
            };

            clientDetail.CanManage = await dbContext.UserRoleInClient
                .Where(urc => urc.UserId == currentUser.Id)
                .Where(urc => urc.Role.RoleEnum == roleInClient)
                .Where(urc => urc.ClientId == clientDetail.Id)
                .AnyAsync();

            // Don't provide more information than necessary
            if (!clientDetail.CanManage) return clientDetail;

            clientDetail.EligibleUserCount = await dbContext.UserRoleInClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ContentUser)
                .Where(urc => urc.ClientId == clientDetail.Id)
                .Select(urc => urc.Id)
                .Distinct()
                .CountAsync();

            clientDetail.RootContentItemCount = await dbContext.RootContentItem
                .Where(rci => rci.ClientId == clientDetail.Id)
                .CountAsync();

            return clientDetail;
        }
    }
}
