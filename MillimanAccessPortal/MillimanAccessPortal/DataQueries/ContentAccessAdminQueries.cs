using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.ContentAccessAdmin;
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
        private readonly PublicationQueries _publicationQueries;
        private readonly UserQueries _userQueries;

        public ContentAccessAdminQueries(
            ClientQueries clientQueries,
            ContentItemQueries contentItemQueries,
            PublicationQueries publicationQueries,
            UserQueries userQueries)
        {
            _clientQueries = clientQueries;
            _contentItemQueries = contentItemQueries;
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
            var items = await _contentItemQueries.SelectContentItemsWhereClient(clientId);
            var itemIds = items.ConvertAll(i => i.Id);

            var contentTypes = await _contentItemQueries.SelectContentTypesContentItemIn(itemIds);
            var publications = await _publicationQueries.SelectPublicationsWhereContentItemIn(itemIds);
            var publicationIds = publications.ConvertAll(p => p.Id);

            var queueDetails = await _publicationQueries.SelectQueueDetailsWherePublicationIn(publicationIds);

            return new ContentItemsViewModel
            {
                Items = items.ToDictionary(i => i.Id),
                ContentTypes = contentTypes.ToDictionary(t => t.Id),
                Publications = publications.ToDictionary(p => p.Id),
                PublicationQueue = queueDetails.ToDictionary(q => q.PublicationId),
            };
        }
    }
}
