/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel to communicate selected Client entity and related properties
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MapCommonLib;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using MillimanAccessPortal.DataQueries;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class AssignedRoleInfo
    {
        public RoleEnum RoleEnum { get; set; }
        public string RoleDisplayValue { get; set; }
        public bool IsAssigned { get; set; }

        public static explicit operator AssignedRoleInfo(RoleEnum RoleEnumArg)
        {
            return new AssignedRoleInfo
            {
                RoleEnum = RoleEnumArg,
                RoleDisplayValue = ApplicationRole.RoleDisplayNames[RoleEnumArg],
                IsAssigned = false,  // to be assigned externally
            };
        }
    }

    public class UserInfoModel
    {
        public long Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSuspended { get; set; } = false;
        public List<AssignedRoleInfo> UserRoles { get; set; } = new List<AssignedRoleInfo>();

        public static explicit operator UserInfoModel(ApplicationUser U)
        {
            return new UserInfoModel
            {
                Id = U.Id,
                Email = U.Email,
                FirstName = U.FirstName,
                LastName = U.LastName,
                UserName = U.UserName,
                IsSuspended = U.IsSuspended,
            };
        }
    }

    /// <summary>
    /// This class is required for set subtraction using the IEnumerable.Except() method.  Does not compare role names
    /// </summary>
    class UserInfoModelEqualityComparer : IEqualityComparer<UserInfoModel>
    {
        public bool Equals(UserInfoModel Left, UserInfoModel Right)
        {
            if (Left == null && Right == null)
                return true;
            else if (Left == null | Right == null)
                return false;
            else if (Left.Id == Right.Id)
                return true;
            else
                return false;
        }

        public int GetHashCode(UserInfoModel Arg)
        {
            string hCode = Arg.LastName + Arg.FirstName + Arg.Email + Arg.UserName + Arg.Id.ToString();
            return hCode.GetHashCode();
        }
    }

    public class ClientDetailViewModel
    {
        public Client ClientEntity { get; set; }
        public List<UserInfoModel> EligibleUsers { get; set; } = new List<UserInfoModel>();
        public List<UserInfoModel> AssignedUsers { get; set; } = new List<UserInfoModel>();
        public List<RootContentItem> ContentItems { get; set; } = new List<RootContentItem>();
        public bool CanManage { get; set; }

        internal async Task GenerateSupportingProperties(ApplicationDbContext DbContext, UserManager<ApplicationUser> UserManager, ApplicationUser CurrentUser, RoleEnum ClientRoleRequiredToManage, bool RequireProfitCenterAuthority)
        {
            #region Validation
            if (ClientEntity == null)
            {
                throw new MapException("ClientDetailViewModel.GenerateSupportingProperties called with no ClientEntity set");
            }
            #endregion

            ClientEntity.ParentClient = null;

            StandardQueries Queries = new StandardQueries(DbContext, UserManager, null);
            List<RoleEnum> RolesToManage = new List<RoleEnum>
            {
                RoleEnum.Admin,
                RoleEnum.ContentAccessAdmin,
                RoleEnum.ContentPublisher,
                RoleEnum.ContentUser,
            };

            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientEntity.Id.ToString());

            // Get and sort the list of users already members of this client
            { // isolate scope
                IList<ApplicationUser> UsersForThisClaim = await UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim);
                AssignedUsers = UsersForThisClaim
                                        .Select(ApUser => (UserInfoModel)ApUser)  // use the UserInfo type conversion operator
                                        .OrderBy(u => u.LastName)
                                        .ThenBy(u => u.FirstName)
                                        .ThenBy(u => u.UserName)
                                        .ToList();
            }

            var hasRequiredRole = DbContext.UserRoleInClient
                .Where(urc => urc.UserId == CurrentUser.Id)
                .Where(urc => urc.Role.RoleEnum == ClientRoleRequiredToManage)
                .Where(urc => urc.ClientId == ClientEntity.Id)
                .Any();
            var hasProfitCenterAuthority = DbContext.UserRoleInProfitCenter
                .Where(urp => urp.UserId == CurrentUser.Id)
                .Where(urp => urp.Role.RoleEnum == RoleEnum.Admin)
                .Where(urp => urp.ProfitCenterId == ClientEntity.ProfitCenterId)
                .Any();
            CanManage = hasRequiredRole && (!RequireProfitCenterAuthority || hasProfitCenterAuthority);

            // Assign the remaining assigned user properties
            if (CanManage)
            {
                // Get all users currently member of any related Client (any descendant of the root client)
                List<Client> AllRelatedClients = Queries.GetAllRelatedClients(ClientEntity);
                var UsersAssignedToClientFamily = new List<ApplicationUser>();
                foreach (Client OneClient in AllRelatedClients)
                {
                    ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), OneClient.Id.ToString());
                    IList<ApplicationUser> UsersForThisClaim = await UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim);
                    UsersAssignedToClientFamily = UsersAssignedToClientFamily.Union(UsersForThisClaim).ToList();
                    // TODO Test whether the other overload of .Union() needs to be used with an IEqualityComparer argument.  For this use equality should probably be based on Id only.
                }

                // Populate eligible users
                foreach (string AcceptableDomain in ClientEntity.AcceptedEmailDomainList ?? new string[] { })
                {
                    if (string.IsNullOrWhiteSpace(AcceptableDomain))
                    {
                        continue;
                    }
                    EligibleUsers.AddRange(UsersAssignedToClientFamily
                        .Where(u => u.NormalizedEmail.Contains($"@{AcceptableDomain.ToUpper()}"))
                        .Select(u => (UserInfoModel)u));
                }
                // Subtract the assigned users from the overall list of eligible users
                EligibleUsers = EligibleUsers
                    .Except(AssignedUsers, new UserInfoModelEqualityComparer())
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToList();

                // Query user details
                foreach (UserInfoModel assignedUser in AssignedUsers)
                {
                    assignedUser.UserRoles = Queries.GetUserRolesForClient(assignedUser.Id, ClientEntity.Id)
                        .Where(ur => RolesToManage.Contains(ur.RoleEnum))
                        .ToList();

                    // any roles that were not found need to be included with IsAssigned=false
                    assignedUser.UserRoles.AddRange(RolesToManage.Except(assignedUser.UserRoles.Select(ur => ur.RoleEnum)).Select(re =>
                        new AssignedRoleInfo
                        {
                            RoleEnum = re,
                            RoleDisplayValue = ApplicationRole.RoleDisplayNames[re],
                            IsAssigned = false
                        }));

                    assignedUser.UserRoles = assignedUser.UserRoles.OrderBy(ur => ur.RoleEnum).ToList();
                }
                foreach (UserInfoModel eligibleUser in EligibleUsers)
                {
                    eligibleUser.UserRoles = Queries.GetUserRolesForClient(eligibleUser.Id, ClientEntity.Id)
                        .ToList();

                    // any roles that were not found need to be included with IsAssigned=false
                    eligibleUser.UserRoles.AddRange(RolesToManage.Except(eligibleUser.UserRoles.Select(ur => ur.RoleEnum)).Select(re =>
                        new AssignedRoleInfo
                        {
                            RoleEnum = re,
                            RoleDisplayValue = ApplicationRole.RoleDisplayNames[re],
                            IsAssigned = false
                        }));

                    eligibleUser.UserRoles = eligibleUser.UserRoles.OrderBy(ur => ur.RoleEnum).ToList();
                }
            }

            ContentItems = DbContext.RootContentItem
                                    .Where(rc => rc.ClientId == ClientEntity.Id)
                                    .ToList();
        }
    }
}
