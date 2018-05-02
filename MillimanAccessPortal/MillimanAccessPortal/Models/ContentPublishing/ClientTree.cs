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
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class ClientTree : BasicTree<ClientDetail>
    {
        public long SelectedClientId { get; set; } = 0;

        async public static Task<ClientTree> Build(ApplicationUser currentUser, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            #region Validation
            if (currentUser == null)
            {
                return null;
            }
            #endregion

            ClientTree Model = new ClientTree();

            var clientDetails = new List<ClientDetail>();
            foreach (var client in dbContext.Client)
            {
                clientDetails.Add(await ClientDetail.Build(dbContext, userManager, currentUser, client));
            }

            Model.Root.Populate(ref clientDetails);
            Model.Root.Prune((ClientDetail cd) => cd.CanManage, (cum, cur) => cum || cur, false);

            return Model;
        }
    }
}
