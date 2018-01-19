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
    }
}