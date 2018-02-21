using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using MillimanAccessPortal.DataQueries;
using AuditLogLib;
using AuditLogLib.Services;
using Newtonsoft.Json;


namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private ApplicationDbContext DbContext;
        private StandardQueries DbQueries;
        private ILogger AppLogger;
        private IAuditLogger AuditLogger;

        public ContentPublishingController(
            ApplicationDbContext ContextArg,
            StandardQueries DbQueriesArg,
            ILoggerFactory LoggerFactoryArg,
            IAuditLogger AuditLoggerArg
            )
        {
            DbContext = ContextArg;
            DbQueries = DbQueriesArg;
            AppLogger = LoggerFactoryArg.CreateLogger<ContentPublishingController>(); ;
            AuditLogger = AuditLoggerArg;
        }

        public IActionResult Index()
        {
            return View();
        }

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
            // TODO
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
                            Status = "New"
                        };

                        DbContext.ContentReductionTask.Add(NewTask);
                    }

                    DbContext.SaveChanges();
                    Transaction.Commit();
                }
            }
            catch (Exception e)
            {
                AppLogger.LogError(e.Message);
            }

            return Json(new object());
        }
    }
}