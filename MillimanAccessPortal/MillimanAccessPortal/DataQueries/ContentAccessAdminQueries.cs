using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.EntityModels.ClientModels;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.HierarchyModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Queries used by content access admin actions
    /// </summary>
    public class ContentAccessAdminQueries
    {
        private readonly ClientQueries _clientQueries;
        private readonly ContentItemQueries _contentItemQueries;
        private readonly HierarchyQueries _hierarchyQueries;
        private readonly SelectionGroupQueries _selectionGroupQueries;
        private readonly PublicationQueries _publicationQueries;
        private readonly UserQueries _userQueries;

        public ContentAccessAdminQueries(
            ClientQueries clientQueries,
            ContentItemQueries contentItemQueries,
            HierarchyQueries hierarchyQueries,
            SelectionGroupQueries selectionGroupQueries,
            PublicationQueries publicationQueries,
            UserQueries userQueries)
        {
            _clientQueries = clientQueries;
            _contentItemQueries = contentItemQueries;
            _hierarchyQueries = hierarchyQueries;
            _selectionGroupQueries = selectionGroupQueries;
            _publicationQueries = publicationQueries;
            _userQueries = userQueries;
        }
        internal async Task<ContentAccessAdminPageGlobalModel> BuildAccessAdminPageGlobalModelAsync()
        {
            return new ContentAccessAdminPageGlobalModel
            {
                ContentTypes = (await _contentItemQueries.GetAllContentTypesAsync()).ToDictionary(t => t.Id),
            };
        }

        /// <summary>
        /// Select all clients for which the current user can administer access.
        /// </summary>
        /// <param name="user">Current user</param>
        /// <returns>Response model</returns>
        public async Task<ClientsResponseModel> GetAuthorizedClientsModelAsync(ApplicationUser user)
        {
            var clients = await _clientQueries.SelectClientsWithEligibleUsersAsync(user, RoleEnum.ContentAccessAdmin);
            var parentClients = await _clientQueries.SelectParentClientsAsync(clients);
            var clientIds = clients.ConvertAll(c => c.Id);

            var users = await _userQueries.SelectUsersWhereEligibleClientInAsync(clientIds);

            return new ClientsResponseModel
            {
                Clients = clients.ToDictionary(c => c.Id),
                ParentClients = parentClients.ToDictionary(c => c.Id),
                Users = users.ToDictionary(u => u.Id),
            };
        }

        /// <summary>
        /// Select all content items for a client for which the current user can administer access.
        /// </summary>
        /// <param name="user">Current user</param>
        /// <param name="clientId">Selected client</param>
        /// <returns>Response model</returns>
        public async Task<ContentItemsResponseModel> SelectContentItemsAsync(ApplicationUser user, Guid clientId)
        {
            var contentItems = await _contentItemQueries
                .SelectContentItemsWithCardStatsWhereClientAsync(user, RoleEnum.ContentAccessAdmin, clientId);
            var contentItemIds = contentItems.ConvertAll(i => i.Id);

            var contentTypes = await _contentItemQueries.SelectContentTypesContentItemInAsync(contentItemIds);
            var publications = await _publicationQueries.SelectPublicationsWhereContentItemInAsync(contentItemIds);
            var publicationIds = publications.ConvertAll(p => p.Id);

            var queueDetails = await _publicationQueries.SelectQueueDetailsWherePublicationInAsync(publicationIds);

            var clientStats = await _clientQueries.SelectClientWithCardStatsAsync(clientId);

            return new ContentItemsResponseModel
            {
                ContentItems = contentItems.ToDictionary(i => i.Id),
                ContentTypes = contentTypes.ToDictionary(t => t.Id),
                Publications = publications.ToDictionary(p => p.Id),
                PublicationQueue = queueDetails.ToDictionary(q => q.PublicationId),
                ClientStats = clientStats,
            };
        }

        /// <summary>
        /// Select all selection groups for a content item
        /// </summary>
        /// <param name="contentItemId">Selected content item</param>
        /// <returns>Response model</returns>
        public async Task<SelectionGroupsResponseModel> SelectSelectionGroupsAsync(Guid contentItemId)
        {
            var groups = await _selectionGroupQueries.SelectSelectionGroupsWithAssignedUsersAsync(contentItemId);
            var groupIds = groups.ConvertAll(g => g.Id);

            var reductions = await _publicationQueries.SelectReductionsWhereSelectionGroupInAsync(groupIds);
            var reductionIds = reductions.ConvertAll(r => r.Id);

            var queueDetails = await _publicationQueries.SelectQueueDetailsWhereReductionInAsync(reductionIds);

            var contentItemStats = await _contentItemQueries.SelectContentItemWithCardStatsAsync(contentItemId);
            var clientStats = await _clientQueries.SelectClientWithCardStatsAsync(contentItemStats.ClientId);

            return new SelectionGroupsResponseModel
            {
                Groups = groups.ToDictionary(g => g.Id),
                Reductions = reductions.ToDictionary(r => r.Id),
                ReductionQueue = queueDetails.ToDictionary(q => q.ReductionId),
                ContentItemStats = contentItemStats,
                ClientStats = clientStats,
            };
        }

        /// <summary>
        /// Select all selections for a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selected selection group</param>
        /// <returns>Response model</returns>
        public async Task<SelectionsResponseModel> SelectSelectionsAsync(Guid selectionGroupId)
        {
            List<Guid> liveSelections = await _selectionGroupQueries.GetLiveSelectionValueIdsForSelectionGroupAsync(selectionGroupId);
            List<Guid> reductionSelections = await _publicationQueries.SelectReductionSelectionsAsync(selectionGroupId);
            List<BasicField> fields = await _hierarchyQueries.SelectFieldsWhereSelectionGroupAsync(selectionGroupId);
            List<BasicValue> values = await _hierarchyQueries.SelectValuesWhereSelectionGroupAsync(selectionGroupId);

            return new SelectionsResponseModel
            {
                Id = selectionGroupId,
                LiveSelections = liveSelections,
                ReductionSelections = reductionSelections,
                Fields = fields.ToDictionary(f => f.Id),
                Values = values.ToDictionary(v => v.Id),
            };
        }

        /// <summary>
        /// Select publication and reduction status for active content items and selection groups
        /// </summary>
        /// <param name="user">Current user</param>
        /// <param name="clientId">Selected client</param>
        /// <param name="contentItemId">Selected content item</param>
        /// <returns>Response model</returns>
        public async Task<StatusResponseModel> SelectStatusAsync(ApplicationUser user, Guid clientId, Guid contentItemId)
        {
            var contentItemIds = (await _contentItemQueries.SelectContentItemsWithCardStatsWhereClientAsync(user, RoleEnum.ContentAccessAdmin, clientId))
                .ConvertAll((i) => i.Id);
            var selectionGroupIds = (await _selectionGroupQueries.SelectSelectionGroupsWhereContentItemAsync(contentItemId))
                .ConvertAll((g) => g.Id);

            var publications = await _publicationQueries.SelectPublicationsWhereContentItemInAsync(contentItemIds);
            var reductions = await _publicationQueries.SelectReductionsWhereSelectionGroupInAsync(selectionGroupIds);
            var publicationQueue = await _publicationQueries.SelectQueueDetailsWherePublicationInAsync(publications.ConvertAll((p) => p.Id));
            var reductionQueue = await _publicationQueries.SelectQueueDetailsWhereReductionInAsync(reductions.ConvertAll((r) => r.Id));
            var liveSelectionsSet = await _selectionGroupQueries.SelectSelectionsWhereSelectionGroupInAsync(selectionGroupIds);
            var contentItems = (await _contentItemQueries.SelectContentItemsWithCardStatsWhereClientAsync(user, RoleEnum.ContentAccessAdmin, clientId))
                .ConvertAll(i => new BasicContentItem
                {
                    Id = i.Id,
                    ClientId = i.ClientId,
                    ContentTypeId = i.ContentTypeId,
                    IsSuspended = i.IsSuspended,
                    DoesReduce = i.DoesReduce,
                    Name = i.Name,
                });
            var groups = await _selectionGroupQueries.SelectSelectionGroupsWhereContentItemAsync(contentItemId);

            return new StatusResponseModel
            {
                Publications = publications.ToDictionary((p) => p.Id),
                PublicationQueue = publicationQueue.ToDictionary((p) => p.PublicationId),
                Reductions = reductions.ToDictionary((r) => r.Id),
                ReductionQueue = reductionQueue.ToDictionary((r) => r.ReductionId),
                LiveSelectionsSet = liveSelectionsSet,
                ContentItems = contentItems.ToDictionary((i) => i.Id),
                Groups = groups.ToDictionary((g) => g.Id),
            };
        }

        /// <summary>
        /// Create a reducing selection group
        /// </summary>
        /// <param name="contentItemId">Selected content item</param>
        /// <param name="name">Name of the new selection group</param>
        /// <returns>Response model</returns>
        public async Task<CreateGroupResponseModel> CreateReducingGroupAsync(Guid contentItemId, string name)
        {
            var group = await _selectionGroupQueries.CreateReducingSelectionGroupAsync(contentItemId, name);

            var groupWithUsers = await _selectionGroupQueries.SelectSelectionGroupWithAssignedUsersAsync(group.Id);
            var contentItemStats = await _contentItemQueries.SelectContentItemWithCardStatsAsync(contentItemId);

            return new CreateGroupResponseModel
            {
                Group = groupWithUsers,
                ContentItemStats = contentItemStats,
            };
        }

        /// <summary>
        /// Create a master selection group
        /// </summary>
        /// <param name="contentItemId">Selected content item</param>
        /// <param name="name">Name of the new selection group</param>
        /// <returns>Response model</returns>
        public async Task<CreateGroupResponseModel> CreateMasterGroupAsync(Guid contentItemId, string name)
        {
            var group = await _selectionGroupQueries.CreateMasterSelectionGroupAsync(contentItemId, name);

            var groupWithUsers = await _selectionGroupQueries.SelectSelectionGroupWithAssignedUsersAsync(group.Id);
            var contentItemStats = await _contentItemQueries.SelectContentItemWithCardStatsAsync(contentItemId);

            return new CreateGroupResponseModel
            {
                Group = groupWithUsers,
                ContentItemStats = contentItemStats,
            };
        }

        /// <summary>
        /// Update a selection group's name and list of assigned users
        /// </summary>
        /// <param name="selectionGroupId">Selected selection group</param>
        /// <param name="name">New name for the selection group</param>
        /// <param name="users">New list of users for the selection group</param>
        /// <returns>Response model</returns>
        public async Task<UpdateGroupResponseModel> UpdateGroupAsync(Guid selectionGroupId, string name, List<Guid> users)
        {
            await _selectionGroupQueries.UpdateSelectionGroupNameAsync(selectionGroupId, name);

            var updatedGroup = await _selectionGroupQueries.UpdateSelectionGroupUsersAsync(selectionGroupId, users);

            BasicSelectionGroupWithAssignedUsers selectionGroupWithUsers = await _selectionGroupQueries.SelectSelectionGroupWithAssignedUsersAsync(updatedGroup.Id);
            BasicContentItemWithCardStats contentItemWithStats = await _contentItemQueries.SelectContentItemWithCardStatsAsync(updatedGroup.RootContentItemId);

            return new UpdateGroupResponseModel
            {
                Group = selectionGroupWithUsers,
                ContentItemStats = contentItemWithStats,
            };
        }

        /// <summary>
        /// Delete a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group to delete</param>
        /// <returns>Response model</returns>
        public async Task<DeleteGroupResponseModel> DeleteGroupAsync(Guid selectionGroupId)
        {
            var group = await _selectionGroupQueries.DeleteSelectionGroupAsync(selectionGroupId);
            var contentItemStats = await _contentItemQueries.SelectContentItemWithCardStatsAsync(group.RootContentItemId);

            return new DeleteGroupResponseModel
            {
                GroupId = selectionGroupId,
                ContentItemStats = contentItemStats,
            };
        }

        /// <summary>
        /// Set suspended status for a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selected selection group</param>
        /// <param name="isSuspended">Suspended status</param>
        /// <returns>Response model</returns>
        public async Task<BasicSelectionGroup> SetGroupSuspendedAsync(Guid selectionGroupId, bool isSuspended)
        {
            var group = await _selectionGroupQueries.UpdateSelectionGroupSuspendedAsync(selectionGroupId, isSuspended);

            return (BasicSelectionGroup)group;
        }

        /// <summary>
        /// Return a model to reflect a submitted selection update
        /// </summary>
        /// <param name="selectionGroupId">Selected selection group</param>
        /// <param name="isMaster">Master status</param>
        /// <param name="selections">List of selections</param>
        /// <returns>Response model</returns>
        public async Task<SingleReductionModel> GetUpdateSelectionsModelAsync(Guid selectionGroupId, bool isMaster, List<Guid> selections)
        {
            // use code in the controller for now

            var group = await _selectionGroupQueries.SelectSelectionGroupWithAssignedUsersAsync(selectionGroupId);
            var reduction = (await _publicationQueries.SelectReductionsWhereSelectionGroupInAsync(new List<Guid> { selectionGroupId })).SingleOrDefault();
            var reductionQueue = (await _publicationQueries.SelectQueueDetailsWhereReductionInAsync(
                                        reduction == null
                                        ? new List<Guid> { }
                                        : new List<Guid> { reduction.Id }))
                                  .SingleOrDefault();
            var liveSelections = await _selectionGroupQueries.GetLiveSelectionValueIdsForSelectionGroupAsync(selectionGroupId);

            return new SingleReductionModel
            {
                Group = group,
                Reduction = reduction,
                ReductionQueue = reductionQueue,
                LiveSelections = liveSelections,
            };
        }

        /// <summary>
        /// Return a model to reflect a canceled selection update
        /// </summary>
        /// <param name="groupId">Selected selection group</param>
        /// <returns>Response model</returns>
        public async Task<SingleReductionModel> GetCanceledSingleReductionModelAsync(Guid groupId)
        {
            // use code in the controller for now

            var group = await _selectionGroupQueries.SelectSelectionGroupWithAssignedUsersAsync(groupId);
            var reduction = (await _publicationQueries.SelectReductionsWhereSelectionGroupInAsync(new List<Guid> { groupId })).SingleOrDefault();
            var reductionQueue = (await _publicationQueries.SelectQueueDetailsWhereReductionInAsync(
                reduction == null
                ? new List<Guid> { }
                : new List<Guid> { reduction.Id }
                )).SingleOrDefault();

            return new SingleReductionModel
            {
                Group = group,
                Reduction = reduction,
                ReductionQueue = reductionQueue,
            };
        }
    }
}
