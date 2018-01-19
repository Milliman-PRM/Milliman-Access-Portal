/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Provide all application logic for user administration
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using AuditLogLib;
using AuditLogLib.Services;
using MapCommonLib;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentAccessAdminViewModels;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Controllers
{
    public class ContentAccessAdminController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ILogger Logger;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;

        public ContentAccessAdminController(
            ApplicationDbContext DbContextArg,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ILoggerFactory LoggerFactoryArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> UserManagerArg
            )
        {
            DbContext = DbContextArg;
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            Logger = LoggerFactoryArg.CreateLogger<ContentAccessAdminController>();
            Queries = QueriesArg;
            UserManager = UserManagerArg;
        }

        /// <summary>Action for content access administration index.</summary>
        /// <returns>ViewResult</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return View();
        }

        /// <summary>Returns the list of client families visible to the user.</summary>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientFamilyList()
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Returns the root content items available to a client.</summary>
        /// <param name="ClientId">The client whose root content items are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> RootContentItems(long? ClientId)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Returns the report groups associated with a root content item.</summary>
        /// <param name="ClientId">The client associated with the root content item.</param>
        /// <param name="RootContentItemId">The root content item whose report groups are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> ReportGroups(long? ClientId, long? RootContentItemId)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Creates a report group.</summary>
        /// <param name="ClientId">The client to be assigned to the new report group.</param>
        /// <param name="RootContentItemId">The root content item to be assigned to the new report group.</param>
        /// <param name="ReportGroupName">The name of the new report group.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        public async Task<IActionResult> CreateReportGroup(long? ClientId, long? RootContentItemId, String ReportGroupName)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Updates the users assigned to a report group.</summary>
        /// <param name="ReportGroupId">The report group to be updated.</param>
        /// <param name="Users">The users to be applied to the report group.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateReportGroup(long? ReportGroupId, object Users)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Deletes a report group.</summary>
        /// <param name="ReportGroupId">The report group to be deleted.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteReportGroup(long? ReportGroupId)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Returns the selections associated with a report group.</summary>
        /// <param name="ReportGroupId">The report group whose selections are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> Selections(long? ReportGroupId)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Updates a report group with new selections.</summary>
        /// <param name="ReportGroupId">The report group whose selections are to be updated.</param>
        /// <param name="Selections">The selections to be applied to the report group.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateSelections(long? ReportGroupId, object Selections)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }
    }
}
