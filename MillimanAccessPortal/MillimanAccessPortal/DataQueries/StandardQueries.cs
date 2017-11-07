/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Wrapper for database queries.  Reusable methods appear in this file, methods for single caller appear in files named for the caller
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Linq;

namespace MillimanAccessPortal.DataQueries
{
    public partial class StandardQueries
    {
        private ApplicationDbContext DataContext = null;

        /// <summary>
        /// Constructor, stores local copy of the caller's IServiceScope
        /// </summary>
        /// <param name="SvcProvider"></param>
        public StandardQueries(ApplicationDbContext ContextArg)
        {
            DataContext = ContextArg;
        }

        /// <summary>
        /// Returns a list of the Clients to which the user is assigned ClientAdministrator role
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            IQueryable<Client> AuthorizedClients =
                DataContext.UserRoleForClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ClientAdministrator)
                .Where(urc => urc.User.UserName == UserName)
                .Join(DataContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

            ListOfAuthorizedClients.AddRange(AuthorizedClients);  // Query executes here

            return ListOfAuthorizedClients;
        }

    }
}
