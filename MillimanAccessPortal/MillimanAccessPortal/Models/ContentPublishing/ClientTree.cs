/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Minimal representation of a client tree
 * DEVELOPER NOTES:
 */

using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class ClientTree : BasicTree<ClientSummary>
    {
        public long SelectedClientId { get; set; } = 0;

        async public static Task<ClientTree> Build(ApplicationUser currentUser, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, RoleEnum roleInClient)
        {
            #region Validation
            if (currentUser == null)
            {
                return null;
            }
            #endregion

            ClientTree Model = new ClientTree();

            var clientDetails = new List<ClientSummary>();
            foreach (var client in dbContext.Client.OrderBy(c => c.Name))
            {
                clientDetails.Add(await ClientSummary.Build(dbContext, userManager, currentUser, client, roleInClient));
            }

            Model.Root.Populate(ref clientDetails);
            Model.Root.Prune((ClientSummary cd) => cd.CanManage, (cum, cur) => cum || cur, false);

            return Model;
        }
    }
}
