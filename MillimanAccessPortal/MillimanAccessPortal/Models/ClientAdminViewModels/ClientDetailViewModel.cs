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
using MillimanAccessPortal.Models.EntityModels.ClientModels;

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
                RoleDisplayValue = RoleEnumArg.GetDisplayNameString(),
                IsAssigned = false,  // to be assigned externally
            };
        }
    }

    public class UserInfoModel
    {
        public Guid Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSuspended { get; set; } = false;
        public bool IsAccountDisabled { get; set; } = false;
        public bool IsAccountNearDisabled { get; set; } = false;
        public DateTime? LastLoginUtc { get; set; }
        public DateTime? DateOfAccountDisable { get; set; }        
        public Dictionary<int, AssignedRoleInfo> UserRoles { get; set; } = new Dictionary<int, AssignedRoleInfo>();

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
                LastLoginUtc = U.LastLoginUtc,
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
        public ClientDetail ClientDetail { get; set; }
        public List<UserInfoModel> EligibleUsers { get; set; } = new List<UserInfoModel>();
        public List<UserInfoModel> AssignedUsers { get; set; } = new List<UserInfoModel>();
        public List<RootContentItem> ContentItems { get; set; } = new List<RootContentItem>();
        public bool CanManage { get; set; }
        public bool UsesCustomCapacity { get; set; }

        internal async Task GenerateSupportingProperties(ApplicationDbContext DbContext, UserManager<ApplicationUser> UserManager, ApplicationUser CurrentUser, RoleEnum ClientRoleRequiredToManage, bool RequireProfitCenterAuthority, int MonthsBeforeDisableAccount = 12, int EarlyWarningDaysBeforeAccountDisable = 14)
        {
            #region Validation
            if (ClientEntity == null)
            {
                throw new MapException("ClientDetailViewModel.GenerateSupportingProperties called with no ClientEntity set");
            }
            #endregion

            ClientDetail = (ClientDetail) ClientEntity;

            if (ClientDetail.ProfitCenter == null)
            {
              ClientDetail.ProfitCenter = await DbContext.ProfitCenter.FindAsync(ClientEntity.ProfitCenterId);
            }

            ClientEntity.ParentClient = null;

            StandardQueries Queries = new StandardQueries(DbContext, UserManager, null, null);
            List<RoleEnum> RolesToManage = new List<RoleEnum>
            {
                RoleEnum.Admin,
                RoleEnum.ContentAccessAdmin,
                RoleEnum.ContentPublisher,
                RoleEnum.ContentUser,
                RoleEnum.FileDropAdmin,
                RoleEnum.FileDropUser,
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

            var hasRequiredRole = await DbContext.UserRoleInClient
                .Where(urc => urc.UserId == CurrentUser.Id)
                .Where(urc => urc.Role.RoleEnum == ClientRoleRequiredToManage)
                .Where(urc => urc.ClientId == ClientEntity.Id)
                .AnyAsync();
            var hasProfitCenterAuthority = await DbContext.UserRoleInProfitCenter
                .Where(urp => urp.UserId == CurrentUser.Id)
                .Where(urp => urp.Role.RoleEnum == RoleEnum.Admin)
                .Where(urp => urp.ProfitCenterId == ClientEntity.ProfitCenterId)
                .AnyAsync();
            CanManage = hasRequiredRole && (!RequireProfitCenterAuthority || hasProfitCenterAuthority);
            UsesCustomCapacity = ClientEntity.ConfigurationOverride.PowerBiCapacityId != null;

            // Assign the remaining assigned user properties
            if (CanManage)
            {
                // Get all users currently member of any related Client (any descendant of the root client)
                List<Client> AllRelatedClients = await Queries.GetAllRelatedClientsAsync(ClientEntity);
                var UsersAssignedToClientFamily = new List<ApplicationUser>();
                foreach (Client OneClient in AllRelatedClients)
                {
                    ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), OneClient.Id.ToString());
                    IList<ApplicationUser> UsersForThisClaim = await UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim);
                    UsersAssignedToClientFamily = UsersAssignedToClientFamily.Union(UsersForThisClaim).ToList();
                }

                // Populate eligible users
                foreach (string AcceptableDomain in ClientEntity.AcceptedEmailDomainList ?? new List<string>())
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
                    var assignedUserRoles = (await Queries.GetUserRolesForClientAsync(assignedUser.Id, ClientEntity.Id))
                        .Where(ur => RolesToManage.Contains(ur.RoleEnum))
                        .ToList();

                    // any roles that were not found need to be included with IsAssigned=false
                    assignedUserRoles.AddRange(RolesToManage.Except(assignedUserRoles.Select(ur => ur.RoleEnum)).Select(re =>
                        new AssignedRoleInfo
                        {
                            RoleEnum = re,
                            RoleDisplayValue = re.GetDisplayNameString(),
                            IsAssigned = false
                        }));

                    assignedUser.UserRoles = assignedUserRoles.ToDictionary(ur => (int) ur.RoleEnum);
                    assignedUser.IsAccountDisabled = assignedUser.LastLoginUtc < DateTime.Now.Date.AddMonths(-MonthsBeforeDisableAccount);
                    assignedUser.IsAccountNearDisabled = !assignedUser.IsAccountDisabled && assignedUser.LastLoginUtc < DateTime.Now.Date.AddMonths(-MonthsBeforeDisableAccount).AddDays(EarlyWarningDaysBeforeAccountDisable);
                    assignedUser.DateOfAccountDisable = assignedUser.LastLoginUtc?.AddMonths(MonthsBeforeDisableAccount);
                }
                foreach (UserInfoModel eligibleUser in EligibleUsers)
                {
                    var eligibleUserRoles = await Queries.GetUserRolesForClientAsync(eligibleUser.Id, ClientEntity.Id);

                    // any roles that were not found need to be included with IsAssigned=false
                    eligibleUserRoles.AddRange(RolesToManage.Except(eligibleUserRoles.Select(ur => ur.RoleEnum)).Select(re =>
                        new AssignedRoleInfo
                        {
                            RoleEnum = re,
                            RoleDisplayValue = re.GetDisplayNameString(),
                            IsAssigned = false
                        }));

                    eligibleUser.UserRoles = eligibleUserRoles.ToDictionary(ur => (int) ur.RoleEnum);
                }
            }

            ContentItems = await DbContext.RootContentItem
                                    .Where(rc => rc.ClientId == ClientEntity.Id)
                                    .ToListAsync();
        }
    }
}
