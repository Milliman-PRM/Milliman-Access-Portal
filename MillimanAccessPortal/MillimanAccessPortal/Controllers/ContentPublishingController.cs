/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Authorization;
using AuditLogLib;
using AuditLogLib.Services;
using Newtonsoft.Json;

namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly StandardQueries DbQueries;
        private readonly ILogger Logger;
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;

        public ContentPublishingController(
            ApplicationDbContext ContextArg,
            StandardQueries DbQueriesArg,
            ILoggerFactory LoggerFactoryArg,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg
            )
        {
            DbContext = ContextArg;
            DbQueries = DbQueriesArg;
            Logger = LoggerFactoryArg.CreateLogger<ContentPublishingController>(); ;
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Queues a request for publication and associated reduction tasks
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="RootContentId"></param>
        /// <returns></returns>
        public async Task<IActionResult> RequestContentPublication (string FileName, long RootContentId)
        {
            #region Preliminary Validation
            RootContentItem RootContent = DbContext.RootContentItem.SingleOrDefault(rc => rc.Id == RootContentId);
            if (RootContent == null)
            {
                Response.Headers.Add("Warning", "Requested content item not found.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, RootContentId));
            if (!Result1.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to publish this content");
                return Unauthorized();
            }
            #endregion

            #region Validation
            int ExistingTaskCountForRootContent = DbContext.ContentReductionTask
                                                           .Include(t => t.ContentPublicationRequest)
                                                           .Where(t => t.ContentPublicationRequest.RootContentItemId == RootContentId)
                                                           .Where(t => t.Status != "Complete")
                                                           .Count();
            if (ExistingTaskCountForRootContent > 0)
            {
                Response.Headers.Add("Warning", "Tasks are already pending for this content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Virus checking here?

            // more?
            #endregion

            try
            {
                using (IDbContextTransaction Transaction = DbContext.Database.BeginTransaction())
                {
                    ContentPublicationRequest NewPubRequest = new ContentPublicationRequest { ApplicationUser = await DbQueries.GetCurrentApplicationUser(User),
                                                                                              RootContentItemId = RootContentId,
                                                                                              MasterFilePath = "ThisInputFile"};
                    DbContext.ContentPublicationRequest.Add(NewPubRequest);
                    DbContext.SaveChanges();

                    foreach (SelectionGroup SelGrp in DbContext.SelectionGroup.Where(sg => sg.RootContentItemId == RootContentId).ToList())
                    {
                        string SelectionCriteriaString = JsonConvert.SerializeObject(DbQueries.GetFieldSelectionsForSelectionGroup(SelGrp.Id), Formatting.Indented);
                        var NewTask = new ContentReductionTask
                        {
                            ApplicationUser = await DbQueries.GetCurrentApplicationUser(User),
                            SelectionGroupId = SelGrp.Id,
                            MasterFilePath = "ThisInputFile",
                            ResultFilePath = "ThisOutputFile",
                            ContentPublicationRequest = NewPubRequest,
                            SelectionCriteria = SelectionCriteriaString,
                            Status = "New",  // TODO improve to enum
                        };

                        DbContext.ContentReductionTask.Add(NewTask);
                    }

                    DbContext.SaveChanges();
                    Transaction.Commit();
                }
            }
            catch (Exception e)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): failed to store request to database");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", "Error processing request.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Json(new object());
        }

        /// <summary>
        /// Queues a reduction task for an existing content item
        /// </summary>
        /// <param name="SelectionGroupId"></param>
        /// <returns></returns>
        public async Task<IActionResult> RequestSingleReduction(long SelectionGroupId)
        {
            SelectionGroup RequestedSelectionGroup= DbContext.SelectionGroup
                                                             .Include(sg => sg.RootContentItem)
                                                             .SingleOrDefault(sg => sg.Id == SelectionGroupId);
            #region Preliminary validation
            if (RequestedSelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, RequestedSelectionGroup.RootContentItemId));
            if (!Result1.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to modify user selections for this content");
                return Unauthorized();
            }
            #endregion

            #region Validation
            bool PublicationUnderway = DbContext.ContentPublicationRequest
                                                .Any(r => r.RootContentItemId == RequestedSelectionGroup.RootContentItemId);
                                                    /* && r.status != completed */
            bool ConflictingTask = DbContext.ContentReductionTask
                                            .Any(t => t.SelectionGroupId == SelectionGroupId);
                                                /* && r.status != completed */

            if (PublicationUnderway || ConflictingTask)
            {
                Response.Headers.Add("Warning", "A task is already pending for this selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            try
            {
                string SelectionCriteriaString = JsonConvert.SerializeObject(DbQueries.GetFieldSelectionsForSelectionGroup(SelectionGroupId), Formatting.Indented);
                string MasterFilePath = "ThisInputFile";  // TODO Determine master file. Does master file need to be coppied elsewhere for reduction server

                var NewTask = new ContentReductionTask
                {
                    ApplicationUser = await DbQueries.GetCurrentApplicationUser(User),
                    SelectionGroupId = 33,
                    MasterFilePath = MasterFilePath,
                    ResultFilePath = "ThisOutputFile",
                    ContentPublicationRequest = null,
                    SelectionCriteria = SelectionCriteriaString,
                    Status = "New",  // TODO improve to enum
                };

                DbContext.ContentReductionTask.Add(NewTask);
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): failed to store request to database");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", "Error processing request.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Json(new object());

        }
    }
}