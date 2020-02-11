using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.FileDrop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Queries used by content access admin actions
    /// </summary>
    public class FileDropQueries
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ClientQueries _clientQueries;
        private readonly HierarchyQueries _hierarchyQueries;
        private readonly UserQueries _userQueries;

        public FileDropQueries(
            ApplicationDbContext dbContextArg,
            ClientQueries clientQueries,
            HierarchyQueries hierarchyQueries,
            UserQueries userQueries)
        {
            _dbContext = dbContextArg;
            _clientQueries = clientQueries;
            _hierarchyQueries = hierarchyQueries;
            _userQueries = userQueries;
        }

        /// <summary>
        /// Select all clients for which the current user can administer access.
        /// </summary>
        /// <param name="user">Current user</param>
        /// <returns>Response model</returns>
        internal Dictionary<Guid, BasicClientWithCardStats> GetAuthorizedClientsModel(ApplicationUser user)
        {
            // TODO: Flesh this out since this will necessarily need to be more 
            //    complicated (i.e. all FileDropAdmins, and clients where a FileDropUser has access to a FileDrop)
            List<Client> clientList = _dbContext.UserRoleInClient
                                                .Where(urc => urc.UserId == user.Id && urc.Role.RoleEnum == RoleEnum.FileDropAdmin)
                                                .Select(urc => urc.Client)
                                                .ToList();
            clientList = _clientQueries.AddUniqueAncestorClientsNonInclusiveOf(clientList);

            List<BasicClientWithCardStats> returnList = new List<BasicClientWithCardStats>();

            foreach (Client oneClient in clientList)
            {
                // TODO: Create a new query specifically for File Drop to stop using the Publishing one.
                returnList.Add(_clientQueries.SelectClientWithPublishingCardStats(oneClient, RoleEnum.FileDropAdmin, user.Id));
            }

            return returnList.ToDictionary(c => c.Id);
        }

    }
}
