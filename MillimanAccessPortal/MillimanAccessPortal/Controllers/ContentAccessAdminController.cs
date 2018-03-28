/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using AuditLogLib;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentAccessAdminViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MillimanAccessPortal.Controllers
{
    public class ContentAccessAdminController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DbContext;
        private readonly ILogger Logger;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;

        public ContentAccessAdminController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DbContextArg,
            ILoggerFactory LoggerFactoryArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> UserManagerArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = DbContextArg;
            Logger = LoggerFactoryArg.CreateLogger<ContentAccessAdminController>();
            Queries = QueriesArg;
            UserManager = UserManagerArg;
        }

        /// <summary>Action for content access administration index.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in at least one client.</remarks>
        /// <returns>ViewResult</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
            if (!RoleInClientResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            return View();
        }

        /// <summary>Returns the list of client families visible to the user.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in at least one client.</remarks>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientFamilyList()
        {
            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
            if (!RoleInClientResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            ContentAccessAdminClientListViewModel Model = await ContentAccessAdminClientListViewModel.Build(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext);

            return Json(Model);
        }

        /// <summary>Returns the root content items available to a client.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified client.</remarks>
        /// <param name="ClientId">The client whose root content items are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> RootContentItems(long ClientId)
        {
            Client Client = DbContext.Client.Find(ClientId);

            #region Preliminary validation
            if (Client == null)
            {
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, ClientId));
            if (!RoleInClientResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            ContentAccessAdminRootContentItemListViewModel Model = ContentAccessAdminRootContentItemListViewModel.Build(DbContext, await Queries.GetCurrentApplicationUser(User), Client);

            return Json(Model);
        }

        /// <summary>Returns the selection groups associated with a root content item.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="RootContentItemId">The root content item whose selection groups are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> SelectionGroups(long RootContentItemId)
        {
            RootContentItem RootContentItem = DbContext.RootContentItem.Find(RootContentItemId);

            #region Preliminary validation
            if (RootContentItem == null)
            {
                Response.Headers.Add("Warning", "The requested root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            ContentAccessAdminSelectionGroupListViewModel Model = ContentAccessAdminSelectionGroupListViewModel.Build(DbContext, RootContentItem);

            return Json(Model);
        }

        /// <summary>Creates a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="RootContentItemId">The root content item to be assigned to the new selection group.</param>
        /// <param name="SelectionGroupName">The name of the new selection group.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSelectionGroup(long RootContentItemId, String SelectionGroupName)
        {
            RootContentItem RootContentItem = DbContext.RootContentItem.Find(RootContentItemId);

            #region Preliminary validation
            if (RootContentItem == null)
            {
                Response.Headers.Add("Warning", "The requested root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to create selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { RootContentItem.ClientId, RootContentItemId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            SelectionGroup SelectionGroup = new SelectionGroup
            {
                RootContentItemId = RootContentItem.Id,
                GroupName = SelectionGroupName,
                SelectedHierarchyFieldValueList = new long[] { },
                ContentInstanceUrl = ""
            };

            try
            {
                DbContext.SelectionGroup.Add(SelectionGroup);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while creating selection group \"{SelectionGroup.Id}\"");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            #region Log audit event
            AuditEvent SelectionGroupCreatedEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Selection group created",
                AuditEventId.SelectionGroupCreated,
                new { RootContentItem.ClientId, RootContentItemId, SelectionGroupId = SelectionGroup.Id },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(SelectionGroupCreatedEvent);
            #endregion

            ContentAccessAdminSelectionGroupDetailViewModel Model = ContentAccessAdminSelectionGroupDetailViewModel.Build(DbContext, SelectionGroup);

            return Json(Model);
        }

        /// <summary>Updates the users assigned to a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group to be updated.</param>
        /// <param name="UserAssignments">A dictionary that maps client IDs to a boolean value indicating whether to add or remove the client.</param>
        /// <returns>JsonResult</returns>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSelectionGroupUserAssignments(long SelectionGroupId, Dictionary<long, Boolean> UserAssignments)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.Client)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to update selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Argument processing
            var CurrentAssignments = DbContext.UserInSelectionGroup
                .Where(usg => usg.SelectionGroupId == SelectionGroup.Id)
                .Select(usg => usg.UserId)
                .ToList();
            var UserAdditions = UserAssignments
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .Except(CurrentAssignments);
            var UserRemovals = UserAssignments
                .Where(kvp => !kvp.Value)
                .Select(kvp => kvp.Key)
                .Intersect(CurrentAssignments);
            #endregion

            #region Validation
            var Existent = DbContext.ApplicationUser
                .Where(u => UserAssignments.Keys.Contains(u.Id));
            if (Existent.Count() < UserAssignments.Count())
            {
                Response.Headers.Add("Warning", "One or more requested users do not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var Permissioned = DbContext.UserRoleInRootContentItem
                .Where(ur => UserAdditions.Contains(ur.UserId))
                .Where(ur => ur.RootContentItemId == SelectionGroup.RootContentItemId)
                .Where(ur => ur.RoleId == ((long) RoleEnum.ContentUser));
            if (Permissioned.Count() < UserAdditions.Count())
            {
                Response.Headers.Add("Warning", "One or more requested users do not have permission to use this content.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var AlreadyInGroup = DbContext.UserInSelectionGroup
                .Where(usg => UserAdditions.Contains(usg.UserId))
                .Where(usg => usg.SelectionGroupId != SelectionGroup.Id)
                .Where(usg => usg.SelectionGroup.RootContentItemId == SelectionGroup.RootContentItemId);
            if (AlreadyInGroup.Any())
            {
                Response.Headers.Add("Warning", "One or more requested users to add are already in a different selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            try
            {
                using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
                {
                    DbContext.UserInSelectionGroup.RemoveRange(
                        DbContext.UserInSelectionGroup
                            .Where(usg => UserRemovals.Contains(usg.UserId))
                            .Where(usg => usg.SelectionGroupId == SelectionGroup.Id)
                        );
                    DbContext.SaveChanges();

                    DbContext.UserInSelectionGroup.AddRange(
                        UserAdditions
                            .Select(uid =>
                                new UserInSelectionGroup
                                {
                                    SelectionGroupId = SelectionGroup.Id,
                                    UserId = uid,
                                }
                            )
                        );
                    DbContext.SaveChanges();

                    DbTransaction.Commit();
                }
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while updating selection group \"{SelectionGroupId}\" user assignments");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            #region Log audit event(s)
            foreach (var UserAddition in UserAdditions)
            {
                AuditEvent SelectionGroupUserAssignmentsUpdatedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    "User assigned to selection group",
                    AuditEventId.SelectionGroupUserAssigned,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId, UserId = UserAddition },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(SelectionGroupUserAssignmentsUpdatedEvent);
            }
            foreach (var UserRemoval in UserRemovals)
            {
                AuditEvent SelectionGroupUserAssignmentsUpdatedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    "User removed from selection group",
                    AuditEventId.SelectionGroupUserRemoved,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId, UserId = UserRemoval },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(SelectionGroupUserAssignmentsUpdatedEvent);
            }
            #endregion

            ContentAccessAdminSelectionGroupListViewModel Model = ContentAccessAdminSelectionGroupListViewModel.Build(DbContext, SelectionGroup.RootContentItem);

            return Json(Model);
        }

        /// <summary>Deletes a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group to be deleted.</param>
        /// <returns>JsonResult</returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelectionGroup(long SelectionGroupId)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to delete selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            List<long> RemovedUsers = new List<long>();

            try
            {
                using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
                {
                    List<UserInSelectionGroup> UsersToRemove = DbContext.UserInSelectionGroup
                        .Where(usg => usg.SelectionGroupId == SelectionGroup.Id)
                        .ToList();
                    DbContext.UserInSelectionGroup.RemoveRange(UsersToRemove);
                    DbContext.SaveChanges();

                    DbContext.SelectionGroup.Remove(
                        DbContext.SelectionGroup
                            .Where(sg => sg.Id == SelectionGroup.Id)
                            .Single()
                        );
                    DbContext.SaveChanges();

                    DbTransaction.Commit();

                    RemovedUsers = UsersToRemove
                        .Select(uug => uug.UserId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while deleting selection group \"{SelectionGroupId}\" or removing members: [{string.Join(",", RemovedUsers)}]");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            #region Log audit event(s)
            foreach (var UserId in RemovedUsers)
            {
                AuditEvent SelectionGroupUpdatedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    "User removed from selection group",
                    AuditEventId.SelectionGroupUserRemoved,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId, UserId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(SelectionGroupUpdatedEvent);
            }

            AuditEvent SelectionGroupDeletedEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Selection group deleted",
                AuditEventId.SelectionGroupDeleted,
                new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(SelectionGroupDeletedEvent);
            #endregion

            ContentAccessAdminSelectionGroupListViewModel Model = ContentAccessAdminSelectionGroupListViewModel.Build(DbContext, SelectionGroup.RootContentItem);

            return Json(Model);
        }

        /// <summary>Returns the selections associated with a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the related root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group whose selections are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> Selections(long SelectionGroupId)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup.Find(SelectionGroupId);

            #region Preliminary validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            ContentAccessAdminSelectionsDetailViewModel Model = ContentAccessAdminSelectionsDetailViewModel.Build(DbContext, Queries, SelectionGroup);

            return Json(Model);
        }

        /// <summary>Submits a new reduction task.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the related root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group to reduce.</param>
        /// <param name="Selections">A list of selected selection IDs</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SingleReduction(long SelectionGroupId, long[] Selections)
        {
            SelectionGroup RequestedSelectionGroup = DbContext.SelectionGroup
                                                              .Include(sg => sg.RootContentItem)
                                                              .Where(sg => sg.Id == SelectionGroupId)
                                                              .SingleOrDefault();

            ContentReductionTask CurrentLiveReduction = DbContext.ContentReductionTask
                                                                 .SingleOrDefault(t => t.ReductionStatus == ReductionStatusEnum.Live
                                                                                    && t.SelectionGroupId == SelectionGroupId);

            #region Preliminary validation
            if (RequestedSelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (CurrentLiveReduction == null)
            {
                Response.Headers.Add("Warning", "There is no live content for the requested selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, RequestedSelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to update selections without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { RequestedSelectionGroup.RootContentItem.ClientId, RequestedSelectionGroup.RootContentItemId, SelectionGroupId, Selections },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // There must be no pending or unexpected reduction task created after the current live reduction
            var ConflictingStatus = new List<ReductionStatusEnum>
            {
                ReductionStatusEnum.Queued, ReductionStatusEnum.Reducing, ReductionStatusEnum.Reduced,  // pending
                ReductionStatusEnum.Unspecified, ReductionStatusEnum.Replaced,  // unexpected
            };
            if (DbContext.ContentReductionTask
                         .Where(t => t.CreateDateTime > CurrentLiveReduction.CreateDateTime)
                         .Any(t => ConflictingStatus.Contains(t.ReductionStatus)))
            {
                Response.Headers.Add("Warning", "An unresolved publication or selection change prevents this action.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // The requested selections must be valid for the content item
            int ValidSelectionCount = DbContext.HierarchyFieldValue
                                               .Include(hfv => hfv.HierarchyField)
                                               .Where(hfv => hfv.HierarchyField.RootContentItemId == RequestedSelectionGroup.RootContentItemId)
                                               .Where(hfv => Selections.Contains(hfv.Id))
                                               .Count();
            if (ValidSelectionCount < Selections.Count())
            {
                Response.Headers.Add("Warning", "One or more requested selections do not exist or do not belong to the specified content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // The requested selections must be modified from the live selections for this SelectionGroup
            if (Selections.ToHashSet().SetEquals(RequestedSelectionGroup.SelectedHierarchyFieldValueList))
            {
                Response.Headers.Add("Warning", "The requested selections are not different from the active document.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            string SelectionCriteriaString = JsonConvert.SerializeObject(Queries.GetFieldSelectionsForSelectionGroup(SelectionGroupId, Selections), Formatting.Indented);

            var ContentReductionTask = new ContentReductionTask
            {
                ApplicationUser = await Queries.GetCurrentApplicationUser(User),
                SelectionGroupId = RequestedSelectionGroup.Id,
                MasterFilePath = "TODO: Fix this",
                ContentPublicationRequest = null,
                SelectionCriteria = SelectionCriteriaString,
                ReductionStatus = ReductionStatusEnum.Queued,
            };
            DbContext.ContentReductionTask.Add(ContentReductionTask);

            DbContext.SaveChanges();

            #region Log audit event
            AuditEvent SelectionChangeReductionQueuedEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Selection change reduction task queued",
                AuditEventId.SelectionChangeReductionQueued,
                new { RequestedSelectionGroup.RootContentItem.ClientId, RequestedSelectionGroup.RootContentItemId, SelectionGroupId, Selections },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(SelectionChangeReductionQueuedEvent);
            #endregion

            ContentAccessAdminSelectionsDetailViewModel Model = ContentAccessAdminSelectionsDetailViewModel.Build(DbContext, Queries, RequestedSelectionGroup);

            return Json(Model);
        }

        /// <summary>Cancel a pending or completed reduction task.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the related root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group associated with the reduction to be canceled.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReduction(long SelectionGroupId)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                .Where(sg => sg.Id == SelectionGroupId)
                .SingleOrDefault();

            #region Preliminary validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to cancel reduction without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var CancelableStatus = new List<ReductionStatusEnum>
            {
                ReductionStatusEnum.Queued,
            };
            var CancelableTasks = DbContext.ContentReductionTask
                .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                .Where(crt => CancelableStatus.Contains(crt.ReductionStatus))
                .Where(crt => crt.ContentPublicationRequestId == null);
            if (CancelableTasks.Count() == 0)
            {
                Response.Headers.Add("Warning", "There are no cancelable tasks for this root content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // There should always be at most one cancelable task
            var UpdatedTasks = new List<ContentReductionTask>();
            foreach (var Task in CancelableTasks)
            {
                Task.ReductionStatus = ReductionStatusEnum.Canceled;
                DbContext.Update(Task);

                UpdatedTasks.Add(Task);
            }
            DbContext.SaveChanges();

            #region Log audit event
            AuditEvent SelectionChangeReductionCanceledEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Selection change reduction task canceled",
                AuditEventId.SelectionChangeReductionCanceled,
                new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId, UpdatedTasks },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(SelectionChangeReductionCanceledEvent);
            #endregion

            ContentAccessAdminSelectionsDetailViewModel Model = ContentAccessAdminSelectionsDetailViewModel.Build(DbContext, Queries, SelectionGroup);

            return Json(Model);
        }
    }
}
