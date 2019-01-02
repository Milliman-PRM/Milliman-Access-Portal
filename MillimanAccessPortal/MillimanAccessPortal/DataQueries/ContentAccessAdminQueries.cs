using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    public class ContentAccessAdminQueries
    {
        private readonly ClientQueries _clientQueries;
        private readonly UserQueries _userQueries;

        public ContentAccessAdminQueries(
            ClientQueries clientQueries,
            UserQueries userQueries)
        {
            _clientQueries = clientQueries;
            _userQueries = userQueries;
        }

        public async Task<ClientsViewModel> SelectClients(ApplicationUser user)
        {
            var clients = await _clientQueries.SelectClientsWithEligibleUsers(user, RoleEnum.ContentAccessAdmin);
            var clientIds = clients.ConvertAll(c => c.Id);

            var users = await _userQueries.SelectUsersWhereEligibleClientIn(clientIds);

            return new ClientsViewModel
            {
                Clients = clients,
                Users = users,
            };
        }
    }
}
