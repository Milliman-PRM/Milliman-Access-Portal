using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Select all clients for which the current user can administer access.
        /// </summary>
        /// <param name="user">Current user</param>
        /// <returns>Response model</returns>
        public ClientsResponseModel SelectClients(ApplicationUser user)
        {
            var clients = _clientQueries.SelectClientsWithEligibleUsers(user, RoleEnum.ContentAccessAdmin);
            var clientIds = clients.ConvertAll(c => c.Id);

            var users = _userQueries.SelectUsersWhereEligibleClientIn(clientIds);

            return new ClientsResponseModel
            {
                Clients = clients.ToDictionary(c => c.Id),
                Users = users.ToDictionary(u => u.Id),
            };
        }

        /// <summary>
        /// Select all content items for a client for which the current user can administer access.
        /// </summary>
        /// <param name="user">Current user</param>
        /// <param name="clientId">Selected client</param>
        /// <returns>Response model</returns>
        public ContentItemsResponseModel SelectContentItems(ApplicationUser user, Guid clientId)
        {
            var contentItems = _contentItemQueries
                .SelectContentItemsWithStatsWhereClient(user, RoleEnum.ContentAccessAdmin, clientId);
            var contentItemIds = contentItems.ConvertAll(i => i.Id);

            var contentTypes = _contentItemQueries.SelectContentTypesContentItemIn(contentItemIds);
            var publications = _publicationQueries.SelectPublicationsWhereContentItemIn(contentItemIds);
            var publicationIds = publications.ConvertAll(p => p.Id);

            var queueDetails = _publicationQueries.SelectQueueDetailsWherePublicationIn(publicationIds);

            var clientStats = _clientQueries.SelectClientWithStats(clientId);

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
        public SelectionGroupsResponseModel SelectSelectionGroups(Guid contentItemId)
        {
            var groups = _selectionGroupQueries.SelectSelectionGroupsWithAssignedUsers(contentItemId);
            var groupIds = groups.ConvertAll(g => g.Id);

            var reductions = _publicationQueries.SelectReductionsWhereSelectionGroupIn(groupIds);
            var reductionIds = reductions.ConvertAll(r => r.Id);

            var queueDetails = _publicationQueries.SelectQueueDetailsWhereReductionIn(reductionIds);

            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(contentItemId);
            var clientStats = _clientQueries.SelectClientWithStats(contentItemStats.ClientId);

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
        public SelectionsResponseModel SelectSelections(Guid selectionGroupId)
        {
            var liveSelections = _selectionGroupQueries.SelectSelectionsWhereSelectionGroup(selectionGroupId);
            var reductionSelections = _publicationQueries.SelectReductionSelections(selectionGroupId);
            var fields = _hierarchyQueries.SelectFieldsWhereSelectionGroup(selectionGroupId);
            var values = _hierarchyQueries.SelectValuesWhereSelectionGroup(selectionGroupId);

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
        public StatusResponseModel SelectStatus(ApplicationUser user, Guid clientId, Guid contentItemId)
        {
            var contentItemIds = _contentItemQueries
                .SelectContentItemsWithStatsWhereClient(user, RoleEnum.ContentAccessAdmin, clientId)
                .ConvertAll((i) => i.Id);
            var selectionGroupIds = _selectionGroupQueries
                .SelectSelectionGroupsWhereContentItem(contentItemId)
                .ConvertAll((g) => g.Id);

            var publications = _publicationQueries.SelectPublicationsWhereContentItemIn(contentItemIds);
            var reductions = _publicationQueries.SelectReductionsWhereSelectionGroupIn(selectionGroupIds);
            var publicationQueue = _publicationQueries
                .SelectQueueDetailsWherePublicationIn(publications.ConvertAll((p) => p.Id));
            var reductionQueue = _publicationQueries
                .SelectQueueDetailsWhereReductionIn(reductions.ConvertAll((r) => r.Id));
            var liveSelectionsSet = _selectionGroupQueries.SelectSelectionsWhereSelectionGroupIn(selectionGroupIds);
            var contentItems = _contentItemQueries.SelectContentItemsWithStatsWhereClient(user, RoleEnum.ContentAccessAdmin, clientId)
                .ConvertAll(i => new BasicContentItem
                {
                    Id = i.Id,
                    ClientId = i.ClientId,
                    ContentTypeId = i.ContentTypeId,
                    IsSuspended = i.IsSuspended,
                    DoesReduce = i.DoesReduce,
                    Name = i.Name,
                });
            var groups = _selectionGroupQueries.SelectSelectionGroupsWhereContentItem(contentItemId);

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
        public CreateGroupResponseModel CreateReducingGroup(Guid contentItemId, string name)
        {
            var group = _selectionGroupQueries.CreateReducingSelectionGroup(contentItemId, name);

            var groupWithUsers = _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(group.Id);
            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(contentItemId);

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
        public CreateGroupResponseModel CreateMasterGroup(Guid contentItemId, string name)
        {
            var group = _selectionGroupQueries.CreateMasterSelectionGroup(contentItemId, name);

            var groupWithUsers = _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(group.Id);
            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(contentItemId);

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
        public UpdateGroupResponseModel UpdateGroup(Guid selectionGroupId, string name, List<Guid> users)
        {
            _selectionGroupQueries.UpdateSelectionGroupName(selectionGroupId, name);
            var group = _selectionGroupQueries.UpdateSelectionGroupUsers(selectionGroupId, users);

            var groupWithUsers = _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(group.Id);
            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(group.RootContentItemId);

            return new UpdateGroupResponseModel
            {
                Group = groupWithUsers,
                ContentItemStats = contentItemStats,
            };
        }

        /// <summary>
        /// Delete a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group to delete</param>
        /// <returns>Response model</returns>
        public DeleteGroupResponseModel DeleteGroup(Guid selectionGroupId)
        {
            var group = _selectionGroupQueries.DeleteSelectionGroup(selectionGroupId);
            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(group.RootContentItemId);

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
        public BasicSelectionGroup SetGroupSuspended(Guid selectionGroupId, bool isSuspended)
        {
            var group = _selectionGroupQueries.UpdateSelectionGroupSuspended(selectionGroupId, isSuspended);

            return (BasicSelectionGroup)group;
        }

        /// <summary>
        /// Return a model to reflect a submitted selection update
        /// </summary>
        /// <param name="selectionGroupId">Selected selection group</param>
        /// <param name="isMaster">Master status</param>
        /// <param name="selections">List of selections</param>
        /// <returns>Response model</returns>
        public SingleReductionModel UpdateSelections(Guid selectionGroupId, bool isMaster, List<Guid> selections)
        {
            // use code in the controller for now

            var group = _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(selectionGroupId);
            var reduction = _publicationQueries
                .SelectReductionsWhereSelectionGroupIn(new List<Guid> { selectionGroupId }).SingleOrDefault();
            var reductionQueue = _publicationQueries
                .SelectQueueDetailsWhereReductionIn(reduction == null
                    ? new List<Guid> { }
                    : new List<Guid> { reduction.Id }).SingleOrDefault();
            var liveSelections = _selectionGroupQueries.SelectSelectionsWhereSelectionGroup(selectionGroupId);

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
        public SingleReductionModel CancelReduction(Guid groupId)
        {
            // use code in the controller for now

            var group = _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(groupId);
            var reduction = _publicationQueries
                .SelectReductionsWhereSelectionGroupIn(new List<Guid> { groupId }).SingleOrDefault();
            var reductionQueue = _publicationQueries
                .SelectQueueDetailsWhereReductionIn(reduction == null
                    ? new List<Guid> { }
                    : new List<Guid> { reduction.Id }
                    ).SingleOrDefault();

            return new SingleReductionModel
            {
                Group = group,
                Reduction = reduction,
                ReductionQueue = reductionQueue,
            };
        }
    }
}
