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
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ILogger _logger;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContentAccessAdminController(
            ApplicationDbContext DbContextArg,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ILoggerFactory LoggerFactoryArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> userManager
            )
        {
            DbContext = DbContextArg;
            _auditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            _logger = LoggerFactoryArg.CreateLogger<ContentAccessAdminController>();
            Queries = QueriesArg;
            _userManager = userManager;
        }
    }
}