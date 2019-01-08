using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    public class UserQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        private List<BasicUser> _selectUsersWhereEligibleClientIn(List<Guid> clientIds)
        {
            return _dbContext.UserRoleInClient
                .Where(r => clientIds.Contains(r.ClientId))
                .Where(r => r.Role.RoleEnum == RoleEnum.ContentUser)
                .Select(r => r.User)
                .Distinct()
                .Select(u => new BasicUser
                {
                    Id = u.Id,
                    IsActivated = u.EmailConfirmed,
                    IsSuspended = u.IsSuspended,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    UserName = u.UserName,
                    Email = u.Email,
                })
                .ToList();
        }

        internal List<BasicUser> SelectUsersWhereEligibleClientIn(List<Guid> clientIds)
        {
            if (clientIds == null)
            {
                return new List<BasicUser> { };
            }

            var users = _selectUsersWhereEligibleClientIn(clientIds);

            return users;
        }
    }
}
