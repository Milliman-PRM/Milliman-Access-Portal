using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Provides queries related to users.
    /// </summary>
    public class UserQueries
    {
        private readonly ApplicationDbContext _dbContext;

        public UserQueries(
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Select unique users that are content eligible in at least one client in a list
        /// </summary>
        /// <param name="clientIds">List of client IDs</param>
        /// <returns>List of users</returns>
        internal async Task<List<BasicUser>> SelectUsersWhereEligibleClientInAsync(List<Guid> clientIds)
        {
            var users = await _dbContext.UserRoleInClient
                                        .Where(rc => clientIds.Contains(rc.ClientId))
                                        .Where(rc => rc.Role.RoleEnum == RoleEnum.ContentUser)
                                        .OrderBy(rc => rc.User.LastName)
                                            .ThenBy(rc => rc.User.FirstName)
                                                .ThenBy(rc => rc.User.UserName)
                                        .Select(r => r.User)
                                        .ToListAsync();

            return users.Distinct(new IdPropertyComparer<ApplicationUser>())
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
    }
}
