using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MapDbContextLib.Context;
using MillimanAccessPortal.DataQueries;

namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private ApplicationDbContext DbContext;
        private StandardQueries DbQueries;

        public ContentPublishingController(
            ApplicationDbContext ContextArg,
            StandardQueries DbQueriesArg
            )
        {
            DbContext = ContextArg;
            DbQueries = DbQueriesArg;
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
            #endregion

            #region Validation
            #endregion

            int ExistingTaskCountForRootContent = DbContext.ContentReductionTask
                                                           .Include(t => t.ContentPublicationRequest)
                                                           .Where(t => t.ContentPublicationRequest.RootContentItemId == RootContentId)
                                                           .Where(t => t.Status != "Complete")
                                                           .Count();
            if (ExistingTaskCountForRootContent > 0)
            {
                Response.Headers.Add("Warning", "Requested content item not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            ContentPublicationRequest NewPubRequest = new ContentPublicationRequest { ApplicationUser = await DbQueries.GetCurrentApplicationUser(User), RootContentItemId = RootContentId };

            List<ContentReductionTask> NewTasks = new List<ContentReductionTask>();
            foreach (SelectionGroup SelGrp in DbContext.SelectionGroup.Where(sg => sg.RootContentItemId == RootContentId))
            {
                NewTasks.Add(new ContentReductionTask { SelectionGroupId=SelGrp.Id, MasterContentFile="Generate this", ResultContentFile="ThisOutputFile", })
            }

            using (IDbContextTransaction Transaction = DbContext.Database.BeginTransaction())
            {
                DbContext.ContentPublicationRequest.Add(NewPubRequest);
                DbContext.SaveChanges();

                                                                                  

                Transaction.Commit();
            }

            return Json(new object());
        }
    }
}