using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<ClientsViewModel> SelectClients(ApplicationUser user)
        {
            var clients = await _clientQueries.SelectClientsWithEligibleUsers(user, RoleEnum.ContentAccessAdmin);
            var clientIds = clients.ConvertAll(c => c.Id);

            var users = await _userQueries.SelectUsersWhereEligibleClientIn(clientIds);

            return new ClientsViewModel
            {
                Clients = clients.ToDictionary(c => c.Id),
                Users = users.ToDictionary(u => u.Id),
            };
        }

        public async Task<ContentItemsViewModel> SelectContentItems(ApplicationUser user, Guid clientId)
        {
            var items = await _contentItemQueries
                .SelectContentItemsWhereClient(user, RoleEnum.ContentAccessAdmin, clientId);
            var itemIds = items.ConvertAll(i => i.Id);

            var contentTypes = await _contentItemQueries.SelectContentTypesContentItemIn(itemIds);
            var publications = await _publicationQueries.SelectPublicationsWhereContentItemIn(itemIds);
            var publicationIds = publications.ConvertAll(p => p.Id);

            var queueDetails = await _publicationQueries.SelectQueueDetailsWherePublicationIn(publicationIds);

            var clientStats = await _clientQueries.SelectClientWithStats(clientId);

            return new ContentItemsViewModel
            {
                Items = items.ToDictionary(i => i.Id),
                ContentTypes = contentTypes.ToDictionary(t => t.Id),
                Publications = publications.ToDictionary(p => p.Id),
                PublicationQueue = queueDetails.ToDictionary(q => q.PublicationId),
                ClientStats = clientStats,
            };
        }

        public async Task<SelectionGroupsViewModel> SelectSelectionGroups(Guid rootContentItemId)
        {
            var groups = await _selectionGroupQueries.SelectSelectionGroupsWithAssignedUsers(rootContentItemId);
            var groupIds = groups.ConvertAll(g => g.Id);

            var reductions = await _publicationQueries.SelectReductionsWhereSelectionGroupIn(groupIds);
            var reductionIds = reductions.ConvertAll(r => r.Id);

            var queueDetails = await _publicationQueries.SelectQueueDetailsWhereReductionIn(reductionIds);

            var contentItemStats = await _contentItemQueries.SelectContentItemWithStats(rootContentItemId);
            var clientStats = await _clientQueries.SelectClientWithStats(contentItemStats.ClientId);

            return new SelectionGroupsViewModel
            {
                Groups = groups.ToDictionary(g => g.Id),
                Reductions = reductions.ToDictionary(r => r.Id),
                ReductionQueue = queueDetails.ToDictionary(q => q.ReductionId),
                ContentItemStats = contentItemStats,
                ClientStats = clientStats,
            };
        }

        public async Task<SelectionsViewModel> SelectSelections(Guid selectionGroupId)
        {
            var liveSelections = await _selectionGroupQueries.SelectSelectionGroupSelections(selectionGroupId);
            var reductionSelections = await _publicationQueries.SelectReductionSelections(selectionGroupId);
            var fields = await _hierarchyQueries.SelectFieldsWhereSelectionGroup(selectionGroupId);
            var values = await _hierarchyQueries.SelectValuesWhereSelectionGroup(selectionGroupId);

            return new SelectionsViewModel
            {
                Id = selectionGroupId,
                LiveSelections = liveSelections,
                ReductionSelections = reductionSelections,
                Fields = fields.ToDictionary(f => f.Id),
                Values = values.ToDictionary(v => v.Id),
            };
        }

        public async Task<StatusViewModel> SelectStatus(ApplicationUser user, Guid clientId, Guid rootContentItemId)
        {
            var contentItemIds = (await _contentItemQueries
                .SelectContentItemsWhereClient(user, RoleEnum.ContentAccessAdmin, clientId))
                .ConvertAll((i) => i.Id);
            var selectionGroupIds = (await _selectionGroupQueries
                .SelectSelectionGroupsWhereContentItem(rootContentItemId))
                .ConvertAll((g) => g.Id);

            var publications = await _publicationQueries.SelectPublicationsWhereContentItemIn(contentItemIds);
            var reductions = await _publicationQueries.SelectReductionsWhereSelectionGroupIn(selectionGroupIds);
            var publicationQueue = await _publicationQueries
                .SelectQueueDetailsWherePublicationIn(publications.ConvertAll((p) => p.Id));
            var reductionQueue = await _publicationQueries
                .SelectQueueDetailsWhereReductionIn(reductions.ConvertAll((r) => r.Id));

            return new StatusViewModel
            {
                Publications = publications.ToDictionary((p) => p.Id),
                PublicationQueue = publicationQueue.ToDictionary((p) => p.PublicationId),
                Reductions = reductions.ToDictionary((r) => r.Id),
                ReductionQueue = reductionQueue.ToDictionary((r) => r.ReductionId),
            };
        }

        public async Task<CreateGroupResponseModel> CreateReducingGroup(Guid itemId, string name)
        {
            var group = await _selectionGroupQueries.CreateReducingSelectionGroup(itemId, name);

            var groupWithUsers = await _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(group.Id);
            var contentItemStats = await _contentItemQueries.SelectContentItemWithStats(itemId);

            return new CreateGroupResponseModel
            {
                Group = groupWithUsers,
                ContentItemStats = contentItemStats,
            };
        }

        public async Task<CreateGroupResponseModel> CreateMasterGroup(Guid itemId, string name)
        {
            var group = await _selectionGroupQueries.CreateMasterSelectionGroup(itemId, name);

            var groupWithUsers = await _selectionGroupQueries.SelectSelectionGroupWithAssignedUsers(group.Id);
            var contentItemStats = await _contentItemQueries.SelectContentItemWithStats(itemId);

            return new CreateGroupResponseModel
            {
                Group = groupWithUsers,
                ContentItemStats = contentItemStats,
            };
        }

        public async Task<BasicSelectionGroupWithAssignedUsers> UpdateGroup(
            Guid groupId, string name, List<Guid> users)
        {
            await _selectionGroupQueries.UpdateSelectionGroupName(groupId, name);
            var group = await _selectionGroupQueries.UpdateSelectionGroupUsers(groupId, users);

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

        public async Task<DeleteGroupResponseModel> DeleteGroup(Guid id)
        {
            var group = await _selectionGroupQueries.DeleteSelectionGroup(id);
            var contentItemStats = await _contentItemQueries.SelectContentItemWithStats(group.RootContentItemId);

            return new DeleteGroupResponseModel
            {
                GroupId = id,
                ContentItemStats = contentItemStats,
            };
        }

        public async Task<BasicSelectionGroup> SetGroupSuspended(Guid id, bool isSuspended)
        {
            var group = await _selectionGroupQueries.UpdateSelectionGroupSuspended(id, isSuspended);

            return (BasicSelectionGroup)group;
        }

        public async Task<SingleReductionModel> UpdateSelections(
            Guid groupId, bool isMaster, List<Guid> selections)
        {
            // use code in the controller for now

            var reduction = (await _publicationQueries
                .SelectReductionsWhereSelectionGroupIn(new List<Guid> { groupId })).Single();
            var reductionQueue = (await _publicationQueries
                .SelectQueueDetailsWhereReductionIn(new List<Guid> { reduction.Id })).SingleOrDefault();

            return new SingleReductionModel
            {
                Reduction = reduction,
                ReductionQueue = reductionQueue,
            };
        }

        public async Task<SingleReductionModel> CancelReduction(Guid groupId)
        {
            // use code in the controller for now

            var reduction = (await _publicationQueries
                .SelectReductionsWhereSelectionGroupIn(new List<Guid> { groupId })).Single();
            var reductionQueue = (await _publicationQueries
                .SelectQueueDetailsWhereReductionIn(new List<Guid> { reduction.Id })).SingleOrDefault();

            return new SingleReductionModel
            {
                Reduction = reduction,
                ReductionQueue = reductionQueue,
            };
        }
    }
}
