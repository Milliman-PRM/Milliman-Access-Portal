using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.DataQueries
{
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

        public ContentItemsResponseModel SelectContentItems(ApplicationUser user, Guid clientId)
        {
            var items = _contentItemQueries
                .SelectContentItemsWhereClient(user, RoleEnum.ContentAccessAdmin, clientId);
            var itemIds = items.ConvertAll(i => i.Id);

            var contentTypes = _contentItemQueries.SelectContentTypesContentItemIn(itemIds);
            var publications = _publicationQueries.SelectPublicationsWhereContentItemIn(itemIds);
            var publicationIds = publications.ConvertAll(p => p.Id);

            var queueDetails = _publicationQueries.SelectQueueDetailsWherePublicationIn(publicationIds);

            var clientStats = _clientQueries.SelectClientWithStats(clientId);

            return new ContentItemsResponseModel
            {
                Items = items.ToDictionary(i => i.Id),
                ContentTypes = contentTypes.ToDictionary(t => t.Id),
                Publications = publications.ToDictionary(p => p.Id),
                PublicationQueue = queueDetails.ToDictionary(q => q.PublicationId),
                ClientStats = clientStats,
            };
        }

        public SelectionGroupsResponseModel SelectSelectionGroups(Guid rootContentItemId)
        {
            var groups = _selectionGroupQueries.SelectSelectionGroupsWithAssignedUsers(rootContentItemId);
            var groupIds = groups.ConvertAll(g => g.Id);

            var reductions = _publicationQueries.SelectReductionsWhereSelectionGroupIn(groupIds);
            var reductionIds = reductions.ConvertAll(r => r.Id);

            var queueDetails = _publicationQueries.SelectQueueDetailsWhereReductionIn(reductionIds);

            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(rootContentItemId);
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

        public SelectionsResponseModel SelectSelections(Guid selectionGroupId)
        {
            var liveSelections = _selectionGroupQueries.SelectSelectionGroupSelections(selectionGroupId);
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

        public StatusResponseModel SelectStatus(ApplicationUser user, Guid clientId, Guid rootContentItemId)
        {
            var contentItemIds = _contentItemQueries
                .SelectContentItemsWhereClient(user, RoleEnum.ContentAccessAdmin, clientId)
                .ConvertAll((i) => i.Id);
            var selectionGroupIds = _selectionGroupQueries
                .SelectSelectionGroupsWhereContentItem(rootContentItemId)
                .ConvertAll((g) => g.Id);

            var publications = _publicationQueries.SelectPublicationsWhereContentItemIn(contentItemIds);
            var reductions = _publicationQueries.SelectReductionsWhereSelectionGroupIn(selectionGroupIds);
            var publicationQueue = _publicationQueries
                .SelectQueueDetailsWherePublicationIn(publications.ConvertAll((p) => p.Id));
            var reductionQueue = _publicationQueries
                .SelectQueueDetailsWhereReductionIn(reductions.ConvertAll((r) => r.Id));

            return new StatusResponseModel
            {
                Publications = publications.ToDictionary((p) => p.Id),
                PublicationQueue = publicationQueue.ToDictionary((p) => p.PublicationId),
                Reductions = reductions.ToDictionary((r) => r.Id),
                ReductionQueue = reductionQueue.ToDictionary((r) => r.ReductionId),
            };
        }

        public CreateGroupResponseModel CreateReducingGroup(Guid itemId, string name)
        {
            var group = _selectionGroupQueries.CreateReducingSelectionGroup(itemId, name);

            var groupWithUsers = _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(group.Id);
            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(itemId);

            return new CreateGroupResponseModel
            {
                Group = groupWithUsers,
                ContentItemStats = contentItemStats,
            };
        }

        public CreateGroupResponseModel CreateMasterGroup(Guid itemId, string name)
        {
            var group = _selectionGroupQueries.CreateMasterSelectionGroup(itemId, name);

            var groupWithUsers = _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(group.Id);
            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(itemId);

            return new CreateGroupResponseModel
            {
                Group = groupWithUsers,
                ContentItemStats = contentItemStats,
            };
        }

        public BasicSelectionGroupWithAssignedUsers UpdateGroup(
            Guid groupId, string name, List<Guid> users)
        {
            _selectionGroupQueries.UpdateSelectionGroupName(groupId, name);
            var group = _selectionGroupQueries.UpdateSelectionGroupUsers(groupId, users);

            return new BasicSelectionGroupWithAssignedUsers
            {
                Id = group.Id,
                RootContentItemId = group.RootContentItemId,
                IsSuspended = group.IsSuspended,
                IsMaster = group.IsMaster,
                Name = group.GroupName,
                AssignedUsers = users,
            };
        }

        public DeleteGroupResponseModel DeleteGroup(Guid id)
        {
            var group = _selectionGroupQueries.DeleteSelectionGroup(id);
            var contentItemStats = _contentItemQueries.SelectContentItemWithStats(group.RootContentItemId);

            return new DeleteGroupResponseModel
            {
                GroupId = id,
                ContentItemStats = contentItemStats,
            };
        }

        public BasicSelectionGroup SetGroupSuspended(Guid id, bool isSuspended)
        {
            var group = _selectionGroupQueries.UpdateSelectionGroupSuspended(id, isSuspended);

            return (BasicSelectionGroup)group;
        }

        public SingleReductionModel UpdateSelections(
            Guid groupId, bool isMaster, List<Guid> selections)
        {
            // use code in the controller for now

            var group = _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(groupId);
            var reduction = _publicationQueries
                .SelectReductionsWhereSelectionGroupIn(new List<Guid> { groupId }).SingleOrDefault();
            var reductionQueue = _publicationQueries
                .SelectQueueDetailsWhereReductionIn(reduction == null
                    ? new List<Guid> { }
                    : new List<Guid> { reduction.Id }).SingleOrDefault();

            return new SingleReductionModel
            {
                Group = group,
                Reduction = reduction,
                ReductionQueue = reductionQueue,
            };
        }

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
