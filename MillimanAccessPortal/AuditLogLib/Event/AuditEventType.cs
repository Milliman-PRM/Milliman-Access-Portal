/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES:
 *   Guidelines for useful log events:
 *  
 *      1) If a content item, selection group, user, or client is 
 *          relevant to the event, include it in the log
 *          
 *      2) If you include one or more of those items, include both
 *          the Id and Name (or equivalent) properties - UserName for users
 */

using AuditLogLib.Models;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using Serilog;

namespace AuditLogLib.Event
{
    #region Common type declarations used in audit logging
    public class SelectionGroupLogObject
    {
        public Guid ClientId { get; set; }
        public Guid RootContentItemId { get; set; }
        public Guid SelectionGroupId { get; set; }
    }

    public enum PasswordResetRequestReason
    {
        [Display(Name = "User Initiated")]
        UserInitiated,

        [Display(Name = "Password Expired")]
        PasswordExpired,

        [Display(Name = "Previous Password Reset Token Invalid")]
        PasswordResetTokenInvalid,
    }

    public enum LoginFailureReason
    {
        [Display(Name = "User Account Not Found")]
        UserAccountNotFound,

        [Display(Name = "PasswordSignInAsync Failed")]
        PasswordSignInAsyncFailed,

        [Display(Name = "Login Failed")]
        LoginFailed,

        [Display(Name = "Account Disabled")]
        UserAccountDisabled,
    }

    public enum ContentDisclaimerResetReason
    {
        [Display(Name = "Content disclaimer text was changed")]
        DisclaimerTextModified,

        [Display(Name = "Root content item was republished")]
        ContentItemRepublished,

        [Display(Name = "Selections were changed")]
        ContentSelectionsModified,

        [Display(Name = "User removed from selection group")]
        UserRemovedFromSelectionGroup,
    }

    public class HitrustReason
    {
        private static Dictionary<int, HitrustReason> AllReasons;

        public static HitrustReason Unknown;
        public static HitrustReason NewEmployeeHire;
        public static HitrustReason NewMapClient;
        public static HitrustReason ChangeInEmployeeResponsibilities;
        public static HitrustReason EmployeeTermination;
        public static HitrustReason ClientRemoval;
        public static HitrustReason InitialSystemUser;
        public static List<HitrustReason> ClientAddUserReasons;
        public static List<HitrustReason> ClientRemoveUserReasons;
        public static List<HitrustReason> ClientRoleChangeReasons;
        public static List<HitrustReason> AddProfitCenterAdminReasons;
        public static List<HitrustReason> RemoveProfitCenterAdminReasons;
        public static List<HitrustReason> GrantSysAdminReasons;
        public static List<HitrustReason> RevokeSysAdminReasons;

        static HitrustReason()
        {
            AllReasons = new Dictionary<int, HitrustReason>();

            Unknown = new HitrustReason(0, "Unknown");
            NewEmployeeHire = new HitrustReason(1, "New employee hire");
            NewMapClient = new HitrustReason(2, "New MAP Client");
            ChangeInEmployeeResponsibilities = new HitrustReason(3, "Change in employee responsibilities");
            EmployeeTermination = new HitrustReason(4, "Employee termination");
            ClientRemoval = new HitrustReason(5, "Client removal");
            InitialSystemUser = new HitrustReason(100, "Initial system user");

            ClientAddUserReasons = new List<HitrustReason> { NewMapClient, NewEmployeeHire, ChangeInEmployeeResponsibilities };
            ClientRemoveUserReasons = new List<HitrustReason> { EmployeeTermination, ChangeInEmployeeResponsibilities, ClientRemoval };
            ClientRoleChangeReasons = new List<HitrustReason> { NewMapClient, NewEmployeeHire, EmployeeTermination, ChangeInEmployeeResponsibilities, ClientRemoval };
            AddProfitCenterAdminReasons = new List<HitrustReason> { NewMapClient, ChangeInEmployeeResponsibilities };
            RemoveProfitCenterAdminReasons = new List<HitrustReason> { ClientRemoval, EmployeeTermination, ChangeInEmployeeResponsibilities };
            GrantSysAdminReasons = new List<HitrustReason> { NewEmployeeHire, ChangeInEmployeeResponsibilities, InitialSystemUser };
            RevokeSysAdminReasons = new List<HitrustReason> { EmployeeTermination, ChangeInEmployeeResponsibilities };
        }

        private HitrustReason(int numericValue, string description)
        {
            NumericValue = numericValue;
            Description = description;

            AllReasons.Add(numericValue, this);
        }

        public int NumericValue { get; private set; }
        public string Description { get; private set; }

        public static bool TryGetReason(int numericValue, out HitrustReason reason)
        {
            if (AllReasons.TryGetValue(numericValue, out reason))
            {
                return true;
            }
            else
            {
                reason = Unknown;
                return false;
            }
        }
    }
    #endregion

    public sealed class AuditEventType : AuditEventTypeBase
    {

        #region Static event type declarations
        // WARNING!!!  After production begins, never change the numeric ID of any AuditEventType

        #region Uncategorized [0000 - 0999]
        public static readonly AuditEventType Unspecified = new AuditEventType(0001, "Unspecified");
        public static readonly AuditEventType InvalidRequest = new AuditEventType(0002, "Invalid request");
        #endregion

        #region User activity [1000 - 1999]
        public static readonly AuditEventType<string> LoginSuccess = new AuditEventType<string>(
            1001, "Login success", (scheme) => new
            {
                Scheme = scheme,
            });
        public static readonly AuditEventType<string, string, LoginFailureReason> LoginFailure = new AuditEventType<string, string, LoginFailureReason>(
            1002, "Login failure", (attemptedUsername, scheme, reason) => new
            {
                AttemptedUsername = attemptedUsername,
                AuthenticationScheme = scheme,
                LoginFailureReason = reason.GetDisplayNameString(),
            });
        public static readonly AuditEventType<RoleEnum> Unauthorized = new AuditEventType<RoleEnum>(
            1003, "Unauthorized request", (role) => new
            {
                Role = role.ToString(),
            });
        public static readonly AuditEventType Logout = new AuditEventType(1004, "Logout success");
        public static readonly AuditEventType AccountLockByUser = new AuditEventType(1005, "Account lock by user");
        public static readonly AuditEventType UserPasswordChanged = new AuditEventType(1006, "User password changed");
        public static readonly AuditEventType<string, string, string, string, int> ManualDatabaseCommand =
            new AuditEventType<string, string, string, string, int>(1007, "Manual database command",
            (userName, githubUrl, approverName, queryText, rows) => new
            {
                UserName = userName,
                GitHubIssue = githubUrl,
                Approver = approverName,
                QueryText = queryText,
                RowsAffected = rows,
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client> UserContentAccess =
           new AuditEventType<SelectionGroup, RootContentItem, Client>(1008, "Content access",
               (selectionGroup, contentItem, client) => new
               {
                   SelectionGroup = new
                   {
                       selectionGroup.Id,
                       selectionGroup.GroupName,
                   },
                   ContentItem = new
                   {
                       contentItem.Id,
                       contentItem.ContentName,
                   },
                   Client = new
                   {
                       client.Id,
                       client.Name,
                   },
               });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, string> UserContentRelatedFileAccess =
            new AuditEventType<SelectionGroup, RootContentItem, Client, string>(1009, "Content related file access",
                (selectionGroup, contentItem, client, relatedFilePurpose) => new
                {
                    SelectionGroup = new
                    {
                        selectionGroup.Id,
                        selectionGroup.GroupName,
                    },
                    ContentItem = new
                    {
                        contentItem.Id,
                        contentItem.ContentName,
                    },
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                    RelatedFilePurpose = relatedFilePurpose,
                });
        #endregion

        #region Client Admin [2000 - 2999]
        public static readonly AuditEventType<Client, ApplicationUser, int> UserAssignedToClient = new AuditEventType<Client, ApplicationUser, int>(
            2001, "User assigned to client", (client, user, reasonVal) =>
            {
                if (!HitrustReason.ClientAddUserReasons.Any(r => r.NumericValue == reasonVal))
                {
                    Log.Error($"Inappropriate reason {reasonVal} provided while audit logging assignment of user to client, expected one of <{string.Join(",", HitrustReason.ClientAddUserReasons.Select(r => r.NumericValue.ToString()))}>");
                }
                HitrustReason.TryGetReason(reasonVal, out HitrustReason reasonObj);

                return new
                {
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                    User = new
                    {
                        user.Id,
                        user.UserName,
                    },
                    Reason = new
                    {
                        reasonObj.NumericValue,
                        reasonObj.Description,
                    }
                };
            });
        public static readonly AuditEventType<Client, ApplicationUser, int> UserRemovedFromClient = new AuditEventType<Client, ApplicationUser, int>(
            2002, "User removed from client", (client, user, reasonVal) =>
            {
                if (!HitrustReason.ClientRemoveUserReasons.Any(r => r.NumericValue == reasonVal))
                {
                    Log.Error($"Inappropriate reason {reasonVal} provided while audit logging removal of user from client, expected one of <{string.Join(",", HitrustReason.ClientRemoveUserReasons.Select(r => r.NumericValue.ToString()))}>");
                }
                HitrustReason.TryGetReason(reasonVal, out HitrustReason reasonObj);

                return new
                {
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                    User = new
                    {
                        user.Id,
                        user.UserName,
                    },
                    Reason = new
                    {
                        reasonObj.NumericValue,
                        reasonObj.Description,
                    }
                };
            });
        public static readonly AuditEventType<Client> ClientCreated = new AuditEventType<Client>(
            2003, "Client created", (client) => new
            {
                Client = client,
            });
        public static readonly AuditEventType<Client> ClientEdited = new AuditEventType<Client>(
            2004, "Client edited", (client) => new
            {
                Client = client,
            });
        public static readonly AuditEventType<Client> ClientDeleted = new AuditEventType<Client>(
            2005, "Client deleted", (client) => new
            {
                Client = client,
            });
        public static readonly AuditEventType<Client, ApplicationUser, List<RoleEnum>, int> ClientRoleAssigned = new AuditEventType<Client, ApplicationUser, List<RoleEnum>, int>(
            2006, "Client role assigned", (client, user, roles, reasonVal) =>
            {
                List<int> acceptableReasons = HitrustReason.ClientRoleChangeReasons.Select(r => r.NumericValue).Union(HitrustReason.ClientRemoveUserReasons.Select(r => r.NumericValue)).ToList();

                if (!acceptableReasons.Any(r => r == reasonVal))
                {
                    Log.Error($"Inappropriate reason {reasonVal} provided while audit logging assignment of a user role to a client, expected one of <{string.Join(",", acceptableReasons.Select(r => r.ToString()))}>");
                }
                HitrustReason.TryGetReason(reasonVal, out HitrustReason reasonObj);

                return new
                {
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                    User = new
                    {
                        user.Id,
                        user.UserName,
                    },
                    Role = roles.Select(r => r.ToString()),
                    Reason = new
                    {
                        reasonObj.NumericValue,
                        reasonObj.Description,
                    }
                };
            });
        public static readonly AuditEventType<Client, ApplicationUser, List<RoleEnum>, int> ClientRoleRemoved = new AuditEventType<Client, ApplicationUser, List<RoleEnum>, int>(
            2007, "Client role removed", (client, user, roles, reasonVal) =>
            {
                List<int> acceptableReasons = HitrustReason.ClientRoleChangeReasons.Select(r => r.NumericValue).Union(HitrustReason.ClientRemoveUserReasons.Select(r => r.NumericValue)).ToList();

                if (!acceptableReasons.Any(r => r == reasonVal))
                {
                    Log.Error($"Inappropriate reason {reasonVal} provided while audit logging removal of a user role to a client, expected one of <{string.Join(",", acceptableReasons.Select(r => r.ToString()))}>");
                }
                HitrustReason.TryGetReason(reasonVal, out HitrustReason reasonObj);

                return new
                {
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                    User = new
                    {
                        user.Id,
                        user.UserName,
                    },
                    Role = roles.Select(r => r.ToString()),
                    Reason = new
                    {
                        reasonObj.NumericValue,
                        reasonObj.Description,
                    }
                };
            });
        #endregion

        #region User Account [3000 - 3999]
        public static readonly AuditEventType<ApplicationUser> UserAccountCreated = new AuditEventType<ApplicationUser>(
            3001, "User account created", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                }
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountModified = new AuditEventType<ApplicationUser>(
            3002, "User account modified", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                }
            });
        public static readonly AuditEventType<ApplicationUser, string> UserAccountLockByAdmin = new AuditEventType<ApplicationUser, string>(
            3003, "User account lock by Admin", (user, reason) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                reason,
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountDeleted = new AuditEventType<ApplicationUser>(
            3004, "User account deleted", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountEnabled = new AuditEventType<ApplicationUser>(
            3005, "New user account enabled", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        public static readonly AuditEventType<ApplicationUser,PasswordResetRequestReason> PasswordResetRequested = new AuditEventType<ApplicationUser, PasswordResetRequestReason>(
            3006, "Account password reset requested", (user,reason) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                Reason = reason.GetDisplayNameString()
            });
        public static readonly AuditEventType<ApplicationUser> PasswordResetCompleted = new AuditEventType<ApplicationUser>(
            3007, "Account password reset completed", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        public static readonly AuditEventType<string> PasswordResetRequestedForInvalidEmail = new AuditEventType<string>(
            3008, "Account password reset requested for invalid email", (email) => new
            {
                RequestedEmail = email,
            });
        public static readonly AuditEventType LoginNotAllowed = new AuditEventType(3009, "Login not allowed");
        public static readonly AuditEventType LoginIsLockedOut = new AuditEventType(3010, "Login account is locked out");

        public static readonly AuditEventType<ApplicationUser> UserPasswordExpired =
            new AuditEventType<ApplicationUser>(3011, "User password expired", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                dateLastSetUtc = user.LastPasswordChangeDateTimeUtc
            });

        public static readonly AuditEventType<string> LoginIsSuspended = new AuditEventType<string>(
            3012, "Login account is suspended", (attemptedUserName) => new
            {
                attemptedUserName,
            });

        public static readonly AuditEventType<string> UserNotifiedAboutDisabledAccount = new AuditEventType<string>(
            3013, "User notified that their account is disabled following login.", (email) => new
            {
                RequestedEmail = email,
            });

        public static readonly AuditEventType<UserAgreementLogModel> UserAgreementPresented =
            new AuditEventType<UserAgreementLogModel>(3101, "User agreement presented to user",
                (UserAgreementLogModel) => new
                {
                    UserAgreementLogModel.ValidationId,
                    UserAgreementLogModel.AgreementText,
                });
        public static readonly AuditEventType<Guid> UserAgreementAcceptance = new AuditEventType<Guid>(
            3102, "User agreement acceptance", (validationId) => new
            {
                ValidationId = validationId.ToString(),
            });
        public static readonly AuditEventType<Guid> UserAgreementDeclined = new AuditEventType<Guid>(
            3103, "User agreement declined", (validationId) => new
            {
                ValidationId = validationId.ToString(),
            });
        public static readonly AuditEventType<ApplicationUser> UserAgreementReset = new AuditEventType<ApplicationUser>(
            3104, "User agreement reset", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        #endregion

        #region Content Access [4000 - 4999]
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client> SelectionGroupCreated = new AuditEventType<SelectionGroup, RootContentItem, Client>(
            4001, "Selection group created", (selectionGroup, contentItem, client) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                    selectionGroup.IsInactive,
                    selectionGroup.IsMaster,
                    selectionGroup.IsSuspended,
                    selectionGroup.ReducedContentChecksum,
                    selectionGroup.SelectedHierarchyFieldValueList,
                    selectionGroup.ContentInstanceUrl,
                },
                RootContentItem = new
                {
                    selectionGroup.RootContentItem.Id,
                    selectionGroup.RootContentItem.ContentName,
                },
                Client = new
                {
                    selectionGroup.RootContentItem.Client.Id,
                    selectionGroup.RootContentItem.Client.Name,
                }
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client> SelectionGroupDeleted = new AuditEventType<SelectionGroup, RootContentItem, Client>(
            4002, "Selection group deleted", (selectionGroup, rootContentItem, client) => new
            {
                SelectionGroup = new 
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new 
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ApplicationUser> SelectionGroupUserAssigned = new AuditEventType<SelectionGroup, RootContentItem, Client, ApplicationUser>(
            4003, "User assigned to selection group", (selectionGroup, rootContentItem, client, user) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ApplicationUser> SelectionGroupUserRemoved = new AuditEventType<SelectionGroup, RootContentItem, Client, ApplicationUser>(
            4004, "User removed from selection group", (selectionGroup, rootContentItem, client, user) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ContentReductionTask> SelectionChangeReductionQueued = new AuditEventType<SelectionGroup, RootContentItem, Client, ContentReductionTask>(
            4005, "Selection change reduction task queued", (selectionGroup, rootContentItem, client, reductionTask) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                Action = reductionTask.TaskAction.ToString(),
                SelectedValues = reductionTask.SelectionCriteriaObj
                                              .Fields
                                              .SelectMany(f => f.Values.Where(v => v.SelectionStatus).Select(v => new { v.Id, v.Value }))
                                              .ToList(),
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ContentReductionTask> SelectionChangeReductionCanceled = new AuditEventType<SelectionGroup, RootContentItem, Client, ContentReductionTask>(
            4006, "Selection change reduction task canceled", (selectionGroup, rootContentItem, client, reductionTask) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                Action = reductionTask.TaskAction.ToString(),
                SelectedValues = reductionTask.SelectionCriteriaObj
                                              ?.Fields
                                              .SelectMany(f => f.Values.Where(v => v.SelectionStatus).Select(v => new { v.Id, v.Value }))
                                              .ToList(),
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client> SelectionChangeMasterAccessGranted = new AuditEventType<SelectionGroup, RootContentItem, Client>(
            4007, "Selection group given master access", (selectionGroup, rootContentItem, client) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, bool> SelectionGroupSuspensionUpdate = new AuditEventType<SelectionGroup, RootContentItem, Client, bool>(
            4008, "Selection group suspension status updated", (selectionGroup, rootContentItem, client, isSuspended) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                IsSuspended = isSuspended,
            });
        public static readonly AuditEventType<ContentReductionTask> SelectionChangeReductionLive = new AuditEventType<ContentReductionTask>(
            4009, "Selection change reduction task go live succeeded", (task) => new
            {
                ContentReductionTaskId = task.Id,
            });
        public static readonly AuditEventType<List<UserInSelectionGroup>, RootContentItem, Client, ContentDisclaimerResetReason>
        ContentDisclaimerAcceptanceReset = new AuditEventType<List<UserInSelectionGroup>, RootContentItem, Client, ContentDisclaimerResetReason>(
            4101, "Content disclaimer acceptance reset", (usersInGroup, rootContentItem, client, reason) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                UserInSelectionGroup = usersInGroup.Select(u => new
                {
                    u.Id,
                    User = new
                    {
                        u.UserId,
                        u.User.UserName,
                    },
                    SelectionGroup = new
                    {
                        u.SelectionGroup.Id,
                        u.SelectionGroup.GroupName,
                    }
                }),
                Reason = reason.GetDisplayNameString(),
            });
        public static readonly AuditEventType<UserInSelectionGroup, RootContentItem, Client, Guid, string> ContentDisclaimerPresented =
            new AuditEventType<UserInSelectionGroup, RootContentItem, Client, Guid, string>(4102, "Content disclaimer presented to user",
                (userInSelectionGroup, rootContentItem, client, validationId, disclaimerText) => new
                {
                    ValidationId = validationId.ToString(),
                    UserInSelectionGroup = new
                    {
                        userInSelectionGroup.Id,
                        User = new
                        {
                            userInSelectionGroup.User.Id,
                            userInSelectionGroup.User.UserName,
                        },
                        SelectionGroup = new
                        {
                            userInSelectionGroup.SelectionGroup.Id,
                            userInSelectionGroup.SelectionGroup.GroupName,
                        }
                    },
                    RootContentItem = new
                    {
                        rootContentItem.Id,
                        rootContentItem.ContentName,
                    },
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                    DisclaimerText = disclaimerText,
                });
        public static readonly AuditEventType<UserInSelectionGroup, RootContentItem, Client, Guid> ContentDisclaimerAccepted =
            new AuditEventType<UserInSelectionGroup, RootContentItem, Client, Guid>(4103, "Content disclaimer accepted by user",
                (userInSelectionGroup, rootContentItem, client, validationId) => new
                {
                    ValidationId = validationId,
                    UserInSelectionGroup = new
                    {
                        userInSelectionGroup.Id,
                        User = new
                        {
                            userInSelectionGroup.User.Id,
                            userInSelectionGroup.User.UserName,
                        },
                        SelectionGroup = new
                        {
                            userInSelectionGroup.SelectionGroup.Id,
                            userInSelectionGroup.SelectionGroup.GroupName,
                        }
                    },
                    RootContentItem = new
                    {
                        rootContentItem.Id,
                        rootContentItem.ContentName,
                    },
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                });

        #endregion

        #region Publishing Server [5000 - 5999]
        // 50xx - Preliminary events
        public static readonly AuditEventType<object> ReductionValidationFailed = new AuditEventType<object>(
            5001, "Reduction validation Failed", (logObject) => logObject);

        // 51xx - Hierarchy extraction and content reduction events
        public static readonly AuditEventType<object> HierarchyExtractionSucceeded = new AuditEventType<object>(
            5101, "Content hierarchy extraction completed", (logObject) => logObject);
        public static readonly AuditEventType<object> HierarchyExtractionFailed = new AuditEventType<object>(
            5102, "Content hierarchy extraction failed", (logObject) => logObject);
        public static readonly AuditEventType<object> ContentFileReductionSucceeded = new AuditEventType<object>(
            5103, "Content file reduction completed", (logObject) => logObject);
        public static readonly AuditEventType<object> ContentFileReductionFailed = new AuditEventType<object>(
            5104, "Content file reduction failed", (logObject) => logObject);

        // 52xx - Reduction task aggregate outcome events
        public static readonly AuditEventType<object> ContentReductionTaskCanceled = new AuditEventType<object>(
            5201, "Content reduction task canceled", (logObject) => logObject);
        public static readonly AuditEventType<object> PublicationRequestProcessingSuccess = new AuditEventType<object>(
            5202, "Content publication request success", (logObject) => logObject);

        // 53xx - Publication request aggregate outcome events
        public static readonly AuditEventType<object> ContentPublicationRequestCanceled = new AuditEventType<object>(
            5301, "Content publication request canceled", (logObject) => logObject);
        #endregion

        #region Content Publishing [6000 - 6999]
        public static readonly AuditEventType<RootContentItem, Client> RootContentItemCreated = new AuditEventType<RootContentItem, Client>(
            6001, "Root content item created", (rootContentItem, client) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                    rootContentItem.Description,
                    rootContentItem.Notes,
                    rootContentItem.ContentDisclaimer,
                },
                ContentType = new
                {
                    rootContentItem.ContentType.Id,
                    Name = rootContentItem.ContentType.TypeEnum.GetDisplayNameString(),
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<RootContentItem, Client, List<SelectionGroupLogModel>> RootContentItemDeleted = new AuditEventType<RootContentItem, Client, List<SelectionGroupLogModel>>(
            6002, "Root content item deleted", (rootContentItem, client, groupsAndMemberUserNames) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                ContentType = new
                {
                    rootContentItem.ContentType.Id,
                    Name = rootContentItem.ContentType.TypeEnum.GetDisplayNameString(),
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                DeletedSelectionGroups = groupsAndMemberUserNames,
            });
        public static readonly AuditEventType<RootContentItem, Client> RootContentItemUpdated = new AuditEventType<RootContentItem, Client>(
            6003, "Root content item updated", (rootContentItem, client) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                    rootContentItem.Description,
                    rootContentItem.Notes,
                    rootContentItem.ContentDisclaimer,
                },
                ContentType = new
                {
                    rootContentItem.ContentType.Id,
                    Name = rootContentItem.ContentType.TypeEnum.GetDisplayNameString(),
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest> PublicationRequestInitiated = new AuditEventType<RootContentItem, Client, ContentPublicationRequest>(
            6101, "Publication request initiated", (rootContentItem, client, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                Uploads = publicationRequest.UploadedRelatedFilesObj,
            });
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest> PublicationCanceled = new AuditEventType<RootContentItem, Client, ContentPublicationRequest>(
            6102, "Publication request canceled", (rootContentItem, client, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest> GoLiveValidationFailed = new AuditEventType<RootContentItem, Client, ContentPublicationRequest>(
            6103, "GoLive Validation Failed", (rootContentItem, client, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ContentRelatedFile, string> ChecksumInvalid = new AuditEventType<SelectionGroup, RootContentItem, Client, ContentRelatedFile, string>(
            6104, "Checksum Invalid", (selectionGroup, rootContentItem, client, contentRelatedFile, SourceAction) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                contentRelatedFile.FullPath,
                contentRelatedFile.FilePurpose,
                SourceAction,
            });
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest, string> ContentPublicationGoLive = new AuditEventType<RootContentItem, Client, ContentPublicationRequest, string>(
            6105, "Content publication golive", (rootContentItem, client, publicationRequest, summaryGUID) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                SummaryGUID = summaryGUID,
            });
        public static readonly AuditEventType<PreLiveContentValidationSummaryLogModel> PreGoLiveSummary = new AuditEventType<PreLiveContentValidationSummaryLogModel>(
            6106, "Content publication pre-golive summary", preliveSummary => preliveSummary);
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest> ContentPublicationRejected = new AuditEventType<RootContentItem, Client, ContentPublicationRequest>(
            6107, "Content publication rejected", (rootContentItem, client, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        #endregion

        #region System Admin [7000 - 7999]
        public static readonly AuditEventType<ApplicationUser, bool, string> UserSuspensionUpdate = new AuditEventType<ApplicationUser, bool, string>(
            7001, "User suspension status updated", (user, isSuspended, reason) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                IsSuspended = isSuspended,
                Reason = reason,
            });
        public static readonly AuditEventType<RootContentItem, Client, bool, string> RootContentItemSuspensionUpdate = new AuditEventType<RootContentItem, Client, bool, string>(
            7002, "Root content item suspension status updated", (rootContentItem, client, isSuspended, reason) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                IsSuspended = isSuspended,
                Reason = reason,
            });
        public static readonly AuditEventType<ApplicationUser, RoleEnum, int> SystemRoleAssigned = new AuditEventType<ApplicationUser, RoleEnum, int>(
            7003, "System role assigned", (user, role, reasonVal) =>
            {
                if (!HitrustReason.GrantSysAdminReasons.Any(r => r.NumericValue == reasonVal))
                {
                    Log.Error($"Inappropriate reason {reasonVal} provided while audit logging assignment of system role {role}, expected one of <{string.Join(",", HitrustReason.GrantSysAdminReasons.Select(r => r.NumericValue.ToString()))}>");
                }
                HitrustReason.TryGetReason(reasonVal, out HitrustReason reasonObj);

                return new
                {
                    User = new
                    {
                        user.Id,
                        user.UserName,
                    },
                    Role = role.ToString(),
                    Reason = new
                    {
                        reasonObj.NumericValue,
                        reasonObj.Description,
                    }
                };
            });
        public static readonly AuditEventType<ApplicationUser, RoleEnum, int> SystemRoleRemoved = new AuditEventType<ApplicationUser, RoleEnum, int>(
            7004, "System role removed", (user, role, reasonVal) =>
            {
                if (!HitrustReason.RevokeSysAdminReasons.Any(r => r.NumericValue == reasonVal))
                {
                    Log.Error($"Inappropriate reason {reasonVal} provided while audit logging revocation of system role {role}, expected one of <{string.Join(",", HitrustReason.RevokeSysAdminReasons.Select(r => r.NumericValue.ToString()))}>");
                }
                HitrustReason.TryGetReason(reasonVal, out HitrustReason reasonObj);

                return new
                {
                    User = new
                    {
                        user.Id,
                        user.UserName,
                    },
                    Role = role.ToString(),
                    Reason = new
                    {
                        reasonObj.NumericValue,
                        reasonObj.Description,
                    }
                };
            });
        public static readonly AuditEventType<ProfitCenter> ProfitCenterCreated = new AuditEventType<ProfitCenter>(
            7101, "Profit center created", (profitCenter) => new
            {
                ProfitCenter = profitCenter,
            });
        public static readonly AuditEventType<ProfitCenter> ProfitCenterUpdated = new AuditEventType<ProfitCenter>(
            7102, "Profit center updated", (profitCenter) => new
            {
                ProfitCenter = profitCenter,
            });
        public static readonly AuditEventType<ProfitCenter> ProfitCenterDeleted = new AuditEventType<ProfitCenter>(
            7103, "Profit center deleted", (profitCenter) => new
            {
                ProfitCenter = new
                {
                    profitCenter.Id,
                    profitCenter.Name,
                }
            });
        public static readonly AuditEventType<ProfitCenter, ApplicationUser, int> UserAssignedToProfitCenter = new AuditEventType<ProfitCenter, ApplicationUser, int>(
            7104, "User assigned to profit center", (profitCenter, user, reasonVal) =>
            {
                if (!HitrustReason.AddProfitCenterAdminReasons.Any(r => r.NumericValue == reasonVal))
                {
                    Log.Error($"Inappropriate reason {reasonVal} provided while audit logging assignment of profit center admin role, expected one of <{string.Join(",", HitrustReason.AddProfitCenterAdminReasons.Select(r => r.NumericValue.ToString()))}>");
                }
                HitrustReason.TryGetReason(reasonVal, out HitrustReason reasonObj);

                return new
                {
                    ProfitCenter = new
                    {
                        profitCenter.Id,
                        profitCenter.Name,
                    },
                    User = new
                    {
                        user.Id,
                        user.UserName,
                    },
                    Reason = new
                    {
                        reasonObj.NumericValue,
                        reasonObj.Description,
                    }
                };
            });
        public static readonly AuditEventType<ProfitCenter, ApplicationUser, int> UserRemovedFromProfitCenter = new AuditEventType<ProfitCenter, ApplicationUser, int>(
            7105, "User removed from profit center", (profitCenter, user, reasonVal) =>
            {
                if (!HitrustReason.RemoveProfitCenterAdminReasons.Any(r => r.NumericValue == reasonVal))
                {
                    Log.Error($"Inappropriate reason {reasonVal} provided while audit logging removal of profit center admin role, expected one of <{string.Join(",", HitrustReason.RemoveProfitCenterAdminReasons.Select(r => r.NumericValue.ToString()))}>");
                }
                HitrustReason.TryGetReason(reasonVal, out HitrustReason reasonObj);

                return new
                {
                    ProfitCenter = new
                    {
                        profitCenter.Id,
                        profitCenter.Name,
                    },
                    User = new
                    {
                        user.Id,
                        user.UserName,
                    },
                    Reason = new
                    {
                        reasonObj.NumericValue,
                        reasonObj.Description,
                    }
                };
            });

        // 72xx - Authentication scheme management
        public static readonly AuditEventType<AuthenticationScheme> NewAuthenticationSchemeAdded = new AuditEventType<AuthenticationScheme>(
            7201, "New authentication scheme added", scheme => new
            {
                SchemeId = scheme.Id,
                SchemeName = scheme.Name,
                scheme.DomainList,
                scheme.DisplayName,
                Type = scheme.Type.ToString(),
                SchemeProperties = scheme.SchemePropertiesObj,
            });
        public static readonly AuditEventType<AuthenticationScheme,AuthenticationScheme> AuthenticationSchemeUpdated = new AuditEventType<AuthenticationScheme,AuthenticationScheme>(
            7202, "Authentication scheme updated", (before,after) => new
            {
                Before = new
                {
                    SchemeId = before.Id,
                    SchemeName = before.Name,
                    before.DomainList,
                    before.DisplayName,
                    Type = before.Type.ToString(),
                    SchemeProperties = before.SchemePropertiesObj,
                },
                After = new
                {
                    SchemeId = after.Id,
                    SchemeName = after.Name,
                    after.DomainList,
                    after.DisplayName,
                    Type = after.Type.ToString(),
                    SchemeProperties = after.SchemePropertiesObj,
                }
            });

        // 73xx - Client management
        public static readonly AuditEventType<UpdateClientDomainLimitLogModel> ClientDomainLimitUpdated = new AuditEventType<UpdateClientDomainLimitLogModel>(
            7301, "Client domain limit updated", logModel => logModel);

        // 74xx - Global system management
        public static readonly AuditEventType<string> UserAgreementUpdated = new AuditEventType<string>(
            7401, "User agreement updated", NewText => NewText);
        #endregion

        #region File Drop [8000 - 8999]
        // 80xx - FileDrop admin events
        // 800x - File Drop entity events
        public static readonly AuditEventType<FileDrop, Guid, string> FileDropCreated = new AuditEventType<FileDrop, Guid, string>(
            8001, "File Drop Created", (fileDrop, clientId, clientName) => new
            {
                FileDrop = (FileDropLogModel)fileDrop,
                Client = new
                {
                    Id = clientId,
                    Name = clientName,
                },
            });

        /// <summary>
        /// membershipModel expected to have nested navigation properties SftpAccounts and ApplicationUser populated
        /// </summary>
        public static readonly AuditEventType<FileDrop, Client, List<FileDropPermissionGroupMembershipLogModel>> FileDropDeleted = new AuditEventType<FileDrop, Client, List<FileDropPermissionGroupMembershipLogModel>>(
            8002, "File Drop Deleted", (fileDrop, client, membershipModel) => new
            {
                FileDrop = (FileDropLogModel)fileDrop,
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                DeletedPermissionGroups = membershipModel,
            });

        public static readonly AuditEventType<FileDrop, FileDrop, Guid, string> FileDropUpdated = new AuditEventType<FileDrop, FileDrop, Guid, string>(
            8003, "File Drop Updated", (oldFileDrop, newFileDrop, clientId, clientName) => new
            {
                OldFileDrop = (FileDropLogModel)oldFileDrop,
                NewFileDrop = (FileDropLogModel)newFileDrop,
                Client = new
                {
                    Id = clientId,
                    Name = clientName,
                },
            });

        // 801x - Permission Group entity events
        public static readonly AuditEventType<FileDrop, FileDropUserPermissionGroup, Guid, string> FileDropPermissionGroupCreated = new AuditEventType<FileDrop, FileDropUserPermissionGroup, Guid, string>(
            8011, "File Drop Permission Group Created", (fileDrop, permissionGroup, clientId, clientName) => new
            {
                PermissionGroup = new FileDropPermissionGroupLogModel(permissionGroup),
                FileDrop = (FileDropLogModel)fileDrop,
                Client = new
                {
                    Id = clientId,
                    Name = clientName,
                },
            });

        public static readonly AuditEventType<FileDrop, FileDropUserPermissionGroup> FileDropPermissionGroupDeleted = new AuditEventType<FileDrop, FileDropUserPermissionGroup>(
            8012, "File Drop Permission Group Deleted", (fileDrop, permissionGroup) => new
            {
                PermissionGroup = new FileDropPermissionGroupLogModel(permissionGroup),
                FileDrop = (FileDropLogModel)fileDrop,
                Client = new
                {
                    fileDrop.Client.Id,
                    fileDrop.Client.Name,
                },
            });

        public static readonly AuditEventType<FileDropUserPermissionGroup, FileDropPermissionGroupLogModel, FileDrop> PermissionGroupUpdated = new AuditEventType<FileDropUserPermissionGroup, FileDropPermissionGroupLogModel, FileDrop>(
            8013, "File Drop Permission Group Updated", (permissionGroup, updatedGroup, fileDrop) => new
            {
                FileDrop = (FileDropLogModel)fileDrop,
                PermissionGroup = new
                {
                    permissionGroup.Id,
                    permissionGroup.IsPersonalGroup,
                },
                PreviousProperties = new 
                {
                    permissionGroup.Name,
                    permissionGroup.ReadAccess,
                    permissionGroup.WriteAccess,
                    permissionGroup.DeleteAccess,
                },
                UpdatedProperties = new 
                {
                    updatedGroup.Name,
                    updatedGroup.ReadAccess,
                    updatedGroup.WriteAccess,
                    updatedGroup.DeleteAccess,
                },
            });

        // 81xx - Sftp user events
        /// <summary>
        /// sftpAccounts expected to have navigation property ApplicationUser populated
        /// </summary>
        public static readonly AuditEventType<SftpAccount, FileDrop> SftpAccountCreated = new AuditEventType<SftpAccount, FileDrop>(
            8100, "SFTP Account Created", (account, fileDrop) => new
            {
                FileDrop = (FileDropLogModel)fileDrop,
                SftpAccount = new SftpAccountLogModel(account),
            });

        public static readonly AuditEventType<SftpAccount, FileDrop> SftpAccountDeleted = new AuditEventType<SftpAccount, FileDrop>(
            8101, "SFTP Account Deleted", (account, fileDrop) => new
            {
                FileDrop = (FileDropLogModel)fileDrop,
                SftpAccount = new SftpAccountLogModel(account),
            });

        public static readonly AuditEventType<SftpAccount, FileDropUserPermissionGroup, FileDrop> AccountAddedToPermissionGroup = new AuditEventType<SftpAccount, FileDropUserPermissionGroup, FileDrop>(
            8102, "SFTP Account Added To Permission Group", (account, permissionGroup, fileDrop) => new
            {
                SftpAccount = new SftpAccountLogModel(account),
                PermissionGroup = new
                {
                    permissionGroup.Id,
                    permissionGroup.Name,
                    permissionGroup.IsPersonalGroup,
                    permissionGroup.ReadAccess,
                    permissionGroup.WriteAccess,
                    permissionGroup.DeleteAccess,
                },
                FileDrop = (FileDropLogModel)fileDrop,
            });

        public static readonly AuditEventType<SftpAccount, FileDropUserPermissionGroup, FileDrop> AccountRemovedFromPermissionGroup = new AuditEventType<SftpAccount, FileDropUserPermissionGroup, FileDrop>(
            8103, "SFTP Account Removed From Permission Group", (account, permissionGroup, fileDrop) => new
            {
                SftpAccount = new SftpAccountLogModel(account),
                PermissionGroup = permissionGroup == null
                ? null
                : new
                    {
                        permissionGroup.Id,
                        permissionGroup.Name,
                        permissionGroup.IsPersonalGroup,
                        permissionGroup.ReadAccess,
                        permissionGroup.WriteAccess,
                        permissionGroup.DeleteAccess,
                    },
                FileDrop = (FileDropLogModel)fileDrop,
            });

        public static readonly AuditEventType<SftpAccount, FileDrop> SftpAccountCredentialsGenerated = new AuditEventType<SftpAccount, FileDrop>(
            8104, "SFTP Account Password Generated", (account, fileDrop) => new
            {
                SftpAccount = new SftpAccountLogModel(account),
                FileDrop = (FileDropLogModel)fileDrop,
            });

        public enum SftpAuthenticationFailReason
        {
            [Display(Description = "The requested SFTP account name was not found")]
            UserNotFound,

            [Display(Description = "The requested SFTP account name is suspended")]
            AccountSuspended,

            [Display(Description = "The requested SFTP account has an expired password")]
            PasswordExpired,

            [Display(Description = "The requested SFTP account credentials are invalid")]
            AuthenticationFailed,

            [Display(Description = "The related MAP user has an expired password or is suspended")]
            MapUserBlocked,

            [Display(Description = "The access review deadline for the client related to this file drop has been exceeded")]
            ClientAccessReviewDeadlineMissed,
        }

        public static readonly AuditEventType<SftpAccount, SftpAuthenticationFailReason, FileDropLogModel, string> SftpAuthenticationFailed = new AuditEventType<SftpAccount, SftpAuthenticationFailReason, FileDropLogModel, string>(
            8105, "Sftp Authentication Failed", (account, reason, fileDropModel, clientAddress) =>
            {
                if (reason != SftpAuthenticationFailReason.UserNotFound)
                {
                    return new
                    {
                        ClientAddress = clientAddress,
                        Account = new
                        {
                            account.Id,
                            account.UserName,
                            account.IsSuspended,
                            PasswordResetDateTimeUtc = account.PasswordResetDateTimeUtc.ToString("u"),
                        },
                        Reason = (int)reason,
                        ReasonDescription = reason.GetDisplayDescriptionString(),
                        FileDrop = fileDropModel,
                    };
                }
                else
                {
                    return new
                    {
                        ClientAddress = clientAddress,
                        Account = new
                        {
                            account.UserName,
                        },
                        Reason = (int)reason,
                        ReasonDescription = reason.GetDisplayDescriptionString(),
                        FileDrop = fileDropModel,
                    };
                }
            }
);

        public static readonly AuditEventType<FileDropDirectory, FileDropLogModel, SftpAccount, Client, ApplicationUser> SftpDirectoryCreated = new AuditEventType<FileDropDirectory, FileDropLogModel, SftpAccount, Client, ApplicationUser>(
            8110, "SFTP Directory Created", (fileDropDirectory, fileDropModel, sftpAccount, client, mapUser) => new
            {
                FileDropDirectory = (FileDropDirectoryLogModel)fileDropDirectory,
                FileDrop = fileDropModel,
                SftpAccount = new SftpAccountLogModel(sftpAccount),
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                MapUser = mapUser != null 
                    ? new { mapUser.Id, mapUser.UserName, }
                    : null,
            });

        public static readonly AuditEventType<FileDropDirectoryLogModel, FileDropDirectoryInventoryModel, FileDropLogModel, SftpAccount, ApplicationUser> SftpDirectoryRemoved = new AuditEventType<FileDropDirectoryLogModel, FileDropDirectoryInventoryModel, FileDropLogModel, SftpAccount, ApplicationUser>(
            8111, "SFTP Directory Removed", (fileDropDirectory, DeletedInventory, fileDropModel, sftpAccount, mapUser) => new
            {
                FileDropDirectory = fileDropDirectory,
                DeletedInventory,
                FileDrop = fileDropModel,
                SftpAccount = new SftpAccountLogModel(sftpAccount),
            });

        public static readonly AuditEventType<SftpFileOperationLogModel> SftpFileWriteAuthorized = new AuditEventType<SftpFileOperationLogModel>(
            8112, "SFTP File Upload", (model) => new
            {
                model.FileName,
                model.FileDrop,
                model.FileDropDirectory,
                SftpAccount = new SftpAccountLogModel(model.Account),
            });

        public static readonly AuditEventType<SftpFileOperationLogModel> SftpFileReadAuthorized = new AuditEventType<SftpFileOperationLogModel>(
            8113, "SFTP File Download", (model) => new
            {
                model.FileName,
                model.FileDrop,
                model.FileDropDirectory,
                SftpAccount = new SftpAccountLogModel(model.Account),
            });

        public static readonly AuditEventType<FileDropFileLogModel, FileDropDirectoryLogModel, FileDropLogModel, SftpAccount, ApplicationUser> SftpFileRemoved = new AuditEventType<FileDropFileLogModel, FileDropDirectoryLogModel, FileDropLogModel, SftpAccount, ApplicationUser>(
            8114, "SFTP File Removed", (fileDropFileModel, fileDropDirectoryModel, fileDropModel, sftpAccount, mapUser) => new
            {
                FileName = fileDropFileModel.FileName,
                FileDropDirectory = fileDropDirectoryModel,
                FileDrop = fileDropModel,
                SftpAccount = new SftpAccountLogModel(sftpAccount),
            });

        public static readonly AuditEventType<SftpRenameLogModel> SftpRename = new AuditEventType<SftpRenameLogModel>(
            8115, "SFTP File Or Directory Renamed", (model) => new
            {
                model.From,
                model.To,
                Type = model.IsDirectory ? "Directory" : "File",
                model.FileDrop,
                SftpAccount = new SftpAccountLogModel(model.Account),
            });

        #endregion

        #region Client Access Review [9000 - 9999]
        public static readonly AuditEventType<Guid, object> ClientAccessReviewPresented = new AuditEventType<Guid, object>(
            9001, "Client Access Review Presented", (clientId, reviewModel) => new
            {
                ClientId = clientId,
                Model = reviewModel,
            });

        public static readonly AuditEventType<Guid, Guid> ClientAccessReviewApproved = new AuditEventType<Guid, Guid>(
            9002, "Client Access Review Approved", (clientId, clientAccessReviewId) => new
            {
                ClientId = clientId,
                ClientAccessReviewId = clientAccessReviewId,
            });

        #endregion

        #endregion

        private readonly Func<object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<object> logObjectTransform = null) : base(id, name) {
            this.logObjectTransform = logObjectTransform ?? new Func<object>(() => new { });
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public new AuditEvent ToEvent(
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = base.ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform();

            return auditEvent;
        }
    }

    // Generics represent what entities are required to log this event
    public sealed class AuditEventType<P1> : AuditEventTypeBase
    {
        private readonly Func<P1, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, object> logObjectTransform) : base(id, name) {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1);

            return auditEvent;
        }
    }

    public sealed class AuditEventType<P1, P2> : AuditEventTypeBase
    {
        private readonly Func<P1, P2, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, P2, object> logObjectTransform) : base(id, name) {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1, P2 param2,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1, param2);

            return auditEvent;
        }
    }

    public sealed class AuditEventType<P1, P2, P3> : AuditEventTypeBase
    {
        private readonly Func<P1, P2, P3, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, P2, P3, object> logObjectTransform) : base(id, name) {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1, P2 param2, P3 param3,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1, param2, param3);

            return auditEvent;
        }
    }

    public sealed class AuditEventType<P1, P2, P3, P4> : AuditEventTypeBase
    {
        private readonly Func<P1, P2, P3, P4, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, P2, P3, P4, object> logObjectTransform) : base(id, name)
        {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1, P2 param2, P3 param3, P4 param4,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1, param2, param3, param4);

            return auditEvent;
        }
    }

    public sealed class AuditEventType<P1, P2, P3, P4, P5> : AuditEventTypeBase
    {
        private readonly Func<P1, P2, P3, P4, P5, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, P2, P3, P4, P5, object> logObjectTransform) : base(id, name)
        {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1, P2 param2, P3 param3, P4 param4, P5 param5,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1, param2, param3, param4, param5);

            return auditEvent;
        }
    }
}
