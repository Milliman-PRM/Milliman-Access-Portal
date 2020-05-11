/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Implements MVC actions for the FileDrop view
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Models;
using AuditLogLib.Services;
using CsvHelper;
using CsvHelper.Configuration;
using MapCommonLib;
using MapCommonLib.ActionFilters;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.Binders;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.FileDropModels;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using Newtonsoft.Json.Linq;
using PowerBiLib;
using QlikviewLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class FileDropController : Controller
    {
        private readonly IAuditLogger _auditLogger;
        private readonly IConfiguration _applicationConfig;
        private readonly IAuthorizationService _authorizationService;
        private readonly ApplicationDbContext _dbContext;
        //private readonly StandardQueries _standardQueries;
        private readonly FileDropQueries _fileDropQueries;
        private readonly FileSystemTasks _fileSystemTasks;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Constructor, stores local references to injected service instances
        /// </summary>
        /// <param name="auditLoggerArg"></param>
        /// <param name="authorizationServiceArg"></param>
        /// <param name="contextArg"></param>
        /// <param name="fileDropQueriesArg"></param>
        /// <param name="fileSystemTasks"></param>
        /// <param name="userManagerArg"></param>
        /// <param name="applicationConfigArg"></param>
        public FileDropController(
            IAuditLogger auditLoggerArg,
            IAuthorizationService authorizationServiceArg,
            ApplicationDbContext contextArg,
            FileDropQueries fileDropQueriesArg,
            FileSystemTasks fileSystemTasks,
            UserManager<ApplicationUser> userManagerArg,
            IConfiguration applicationConfigArg
            )
        {
            _auditLogger = auditLoggerArg;
            _authorizationService = authorizationServiceArg;
            _dbContext = contextArg;
            _fileDropQueries = fileDropQueriesArg;
            _fileSystemTasks = fileSystemTasks;
            _userManager = userManagerArg;
            _applicationConfig = applicationConfigArg;
        }

        /// <summary>
        /// Return the view in which FileDrop UI is presented
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// GET clients authorized to the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin));
            if (!adminRoleResult.Succeeded)
            {
                var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser));
                if (!userRoleResult.Succeeded)
                {
                    Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                    Response.Headers.Add("Warning", "You are not authorized to File Drop access.");
                    return Unauthorized();
                }
            }
            #endregion

            ClientsModel model = new ClientsModel
            {
                Clients = _fileDropQueries.GetAuthorizedClientsModel(await _userManager.GetUserAsync(User)),
            };

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFileDrop([FromBody][Bind("Name,Description,ClientId")] FileDrop fileDropModel)
        {
            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, fileDropModel.ClientId));
            if (!adminRoleResult.Succeeded)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to File Drop access.");
                return Unauthorized();
            }
            #endregion

            string fileDropGlobalRoot = _applicationConfig.GetValue("Storage:FileDropRoot", string.Empty);
            Client referencedClient = _dbContext.Client.Find(fileDropModel.ClientId);

            #region Validation
            if (ModelState.Any(v => v.Value.ValidationState == ModelValidationState.Invalid && v.Key != nameof(FileDrop.RootPath)))  // RootPath can/should be invalid here
            {
                Log.Warning($"In action {ControllerContext.ActionDescriptor.DisplayName} ModelState not valid");
                Response.Headers.Add("Warning", "The provided FileDrop information was invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (string.IsNullOrWhiteSpace(fileDropModel.Name))
            {
                Log.Warning($"In action {ControllerContext.ActionDescriptor.DisplayName} new File Drop must have a name");
                Response.Headers.Add("Warning", "The provided FileDrop name was not provided.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (string.IsNullOrWhiteSpace(fileDropGlobalRoot) || !Directory.Exists(fileDropGlobalRoot))
            {
                Log.Error($"In action {ControllerContext.ActionDescriptor.DisplayName} application configuration for FileDropGlobalRoot <{fileDropGlobalRoot}> is invalid or not found");
                Response.Headers.Add("Warning", "The provided FileDrop information was invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (referencedClient == null)
            {
                Log.Warning($"In action {ControllerContext.ActionDescriptor.DisplayName} referenced client with Id {fileDropModel.ClientId} not found");
                Response.Headers.Add("Warning", "The referenced client was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            try
            {
                fileDropModel.RootPath = Guid.NewGuid().ToString();

                string fileDropHash = string.Empty;
                int counter = 0;
                do
                {
                    byte[] randomBytes = new byte[24 / 8];
                    new RNGCryptoServiceProvider().GetBytes(randomBytes);
                    fileDropHash = Convert.ToBase64String(randomBytes);
                    if (++counter >= 10)
                    {
                        Log.Warning($"In action {ControllerContext.ActionDescriptor.DisplayName} failed to generate unique FileDrop hash after {counter} tries");
                        Response.Headers.Add("Warning", "A processing error occurred.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }
                while (_dbContext.FileDrop.Any(d => false/*d.ShortHash == fileDropHash*/));  // TODO enable the expression when the new migration is done
                // TODO fileDropModel.ShortHash = fileDropHash;

                string fileDropAbsoluteRootFolder = Path.Combine(fileDropGlobalRoot, fileDropModel.RootPath);

                FileDropDirectory rootDirectoryRecord = new FileDropDirectory
                {
                    FileDrop = fileDropModel,
                    ParentDirectoryId = null,
                    FileDropPath = "/",
                    Description = "",
                };

                _dbContext.FileDrop.Add(fileDropModel);
                _dbContext.FileDropDirectory.Add(rootDirectoryRecord);
                Directory.CreateDirectory(fileDropAbsoluteRootFolder);

                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In action {ControllerContext.ActionDescriptor.DisplayName} failed to create FileDrop root folder using global root path {fileDropGlobalRoot} and subfolder name {fileDropModel.RootPath}");
                Response.Headers.Add("Warning", "Failed to create file drop home folder.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            _auditLogger.Log(AuditEventType.FileDropCreated.ToEvent(fileDropModel, fileDropModel.ClientId, fileDropModel.Client.Name));

            FileDropsModel model = _fileDropQueries.GetFileDropsModelForClient(fileDropModel.ClientId, await _userManager.GetUserAsync(User));
            model.CurrentFileDropId = fileDropModel.Id;
            model.PermissionGroups = _fileDropQueries.GetPermissionGroupsModelForFileDrop(fileDropModel.Id, fileDropModel.ClientId);

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFileDrop([FromBody][Bind("Id,Name,Description")] FileDrop fileDropModel)
        {
            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, fileDropModel.ClientId));
            if (!adminRoleResult.Succeeded)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to manage File Drops for this client.");
                return Unauthorized();
            }
            #endregion

            FileDrop fileDropRecord = _dbContext.FileDrop
                                                .Include(d => d.Client)
                                                .Single(d => d.Id == fileDropModel.Id);
            FileDrop oldFileDrop = new FileDrop { Id = fileDropRecord.Id,
                                                  Name = fileDropRecord.Name,
                                                  Description = fileDropRecord.Description,
                                                  RootPath = fileDropRecord.RootPath,
            };

            #region Validation
            if (ModelState.Any(v => v.Value.ValidationState == ModelValidationState.Invalid && v.Key != nameof(FileDrop.RootPath)))  // RootPath can/should be invalid here
            {
                Log.Warning($"In action {ControllerContext.ActionDescriptor.DisplayName} ModelState not valid");
                Response.Headers.Add("Warning", "The provided FileDrop information was invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (string.IsNullOrWhiteSpace(fileDropModel.Name))
            {
                Log.Warning($"In action {ControllerContext.ActionDescriptor.DisplayName} new File Drop must have a name");
                Response.Headers.Add("Warning", "The provided FileDrop name was not provided.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            fileDropRecord.Name = fileDropModel.Name;
            fileDropRecord.Description = fileDropModel.Description;
            _dbContext.SaveChanges();

            _auditLogger.Log(AuditEventType.FileDropUpdated.ToEvent(oldFileDrop, fileDropRecord, fileDropRecord.ClientId, fileDropRecord.Client.Name));

            var model = _fileDropQueries.GetFileDropsModelForClient(fileDropModel.ClientId, await _userManager.GetUserAsync(User));
            model.CurrentFileDropId = fileDropRecord.Id;

            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> FileDrops(Guid clientId)
        {
            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin));
            if (!adminRoleResult.Succeeded)
            {
                var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser));
                if (!userRoleResult.Succeeded)
                {
                    Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                    Response.Headers.Add("Warning", "You are not authorized to File Drop access.");
                    return Unauthorized();
                }
            }
            #endregion

            FileDropsModel model = _fileDropQueries.GetFileDropsModelForClient(clientId, await _userManager.GetUserAsync(User));

            return Json(model);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFileDrop([FromBody] Guid id)
        {
            List<Action> pendingAuditLogActions = new List<Action>();

            FileDrop fileDrop = _dbContext.FileDrop
                                          .Include(d => d.Client)
                                          .Include(d => d.SftpAccounts)
                                              .ThenInclude(a => a.ApplicationUser)
                                          .SingleOrDefault(d => d.Id == id);

            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, fileDrop.ClientId));
            if (!adminRoleResult.Succeeded)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to manage File Drops for this client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // the file drop must exist
            if (fileDrop == null)
            {
                Log.Error($"In action {ControllerContext.ActionDescriptor.DisplayName} requested FileDrop {id} not found");
                Response.Headers.Add("Warning", "The requested File Drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            try
            {
                foreach (SftpAccount account in _dbContext.SftpAccount
                                                          .Include(a => a.ApplicationUser)
                                                          .Include(a => a.FileDropUserPermissionGroup)
                                                          .Where(a => a.FileDropId == fileDrop.Id))
                {
                    if (account.FileDropUserPermissionGroup != null)
                    {
                        var rememberGroupInstance = account.FileDropUserPermissionGroup;
                        pendingAuditLogActions.Add(() => _auditLogger.Log(AuditEventType.AccountRemovedFromPermissionGroup.ToEvent(account, rememberGroupInstance, fileDrop)));
                    }
                    pendingAuditLogActions.Add(() => _auditLogger.Log(AuditEventType.SftpAccountDeleted.ToEvent(account, fileDrop)));
                }

                foreach (FileDropUserPermissionGroup group in _dbContext.FileDropUserPermissionGroup
                                                                        .Where(g => g.FileDropId == fileDrop.Id))
                {
                    pendingAuditLogActions.Add(() => _auditLogger.Log(AuditEventType.FileDropPermissionGroupDeleted.ToEvent(fileDrop, group)));
                }

                foreach (FileDropFile file in _dbContext.FileDropDirectory
                                                        .Where(d => d.FileDropId == fileDrop.Id)
                                                        .SelectMany(d => d.Files))
                {
                    _dbContext.FileDropFile.RemoveRange(file);
                    // Audit log file removal?
                }

                _dbContext.FileDrop.Remove(fileDrop);
                pendingAuditLogActions.Add(() => _auditLogger.Log(AuditEventType.FileDropDeleted.ToEvent(fileDrop, fileDrop.Client, fileDrop.SftpAccounts)));

                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to complete the request");
                Response.Headers.Add("Warning", "Failed to complete the request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            foreach (var logAction in pendingAuditLogActions)
            {
                logAction();
            }

            string fullRootPath = default;
            try
            {
                fullRootPath = Path.Combine(_applicationConfig.GetValue<string>("Storage:FileDropRoot"), fileDrop.RootPath);
                FileSystemUtil.DeleteDirectoryWithRetry(fullRootPath, attempts: 4,  baseIntervalMs: 1000);
            } 
            catch(Exception ex)
            {
                Log.Warning(ex, $"Failed to delete root folder {fullRootPath} associated with File Drop with Id {fileDrop.Id}, named {fileDrop.Name}");
            }

            FileDropsModel model = _fileDropQueries.GetFileDropsModelForClient(fileDrop.ClientId, await _userManager.GetUserAsync(User));
            model.CurrentFileDropId = null;

            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> PermissionGroups(Guid FileDropId, Guid ClientId)
        {
            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, ClientId));
            if (!adminRoleResult.Succeeded)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to manage File Drops for this client.");
                return Unauthorized();
            }
            #endregion

            var model = _fileDropQueries.GetPermissionGroupsModelForFileDrop(FileDropId, ClientId);

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFileDropPermissionGroups([FromBody] UpdatePermissionGroupsModel model)
        {
            var fileDrop = await _dbContext.FileDrop.SingleOrDefaultAsync(fd => fd.Id == model.FileDropId);
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} requested FileDrop Id {model.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, fileDrop.ClientId));
            if (!adminRoleResult.Succeeded)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to manage File Drops for this client.");
                return Unauthorized();
            }
            #endregion

            try
            {
                var returnModel = await _fileDropQueries.UpdatePermissionGroups(model);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Error(ex, "ApplicationException thrown from FileDropQueries.UpdatePermissionGroups");
                Response.Headers.Add("Warning", ex.Message);
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception thrown from FileDropQueries.UpdatePermissionGroups");
                Response.Headers.Add("Warning", "Error while processing updates to file drop permissions.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ActionLog(Guid fileDropId)
        {
            Guid clientId = (await _dbContext.FileDrop.SingleOrDefaultAsync(d => d.Id == fileDropId))?.ClientId ?? Guid.Empty;

            #region Validation
            if (clientId == Guid.Empty)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {fileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, clientId));
            if (!adminRoleResult.Succeeded)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to manage File Drops for this client.");
                return Unauthorized();
            }
            #endregion

            DateTime oldestTimestamp = DateTime.UtcNow - TimeSpan.FromDays(30);
            string idCompareString = $"%{fileDropId}%";

            var filters = new List<Expression<Func<AuditEvent, bool>>>
            {
                { e => e.TimeStampUtc > oldestTimestamp},
                { e => e.EventCode >= 8000 && e.EventCode < 9000 },
                // TODO When PostgreSQL 12 is deployed create a computed field as text so the EventData string search can be done server side
                { e => EF.Functions.ILike(e.EventData.ToString(), idCompareString) },
            };

            List<ActivityEventModel> filteredEvents = await _auditLogger.GetAuditEventsAsync(filters, _dbContext, true);

            return Json(filteredEvents);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFullActivityLog(Guid fileDropId)
        {
            Guid clientId = (await _dbContext.FileDrop.SingleOrDefaultAsync(d => d.Id == fileDropId))?.ClientId ?? Guid.Empty;

            #region Validation
            if (clientId == Guid.Empty)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {fileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, clientId));
            if (!adminRoleResult.Succeeded)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to manage File Drops for this client.");
                return Unauthorized();
            }
            #endregion

            string idCompareString = $"%{fileDropId}%";

            var filters = new List<Expression<Func<AuditEvent, bool>>>
            {
                { e => e.EventCode >= 8000 && e.EventCode < 9000 },
                // TODO When PostgreSQL 12 is deployed create a computed field as text so the EventData string search can be done server side
                { e => EF.Functions.ILike(e.EventData.ToString(), idCompareString) },
            };

            List<ActivityEventModel> filteredEvents = await _auditLogger.GetAuditEventsAsync(filters, _dbContext, true);

            string tempFilePath = Path.Combine(_applicationConfig.GetValue<string>("Storage:FileDropRoot"), $"{Guid.NewGuid()}.csv");
            
            var writerConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { IgnoreQuotes = true };

            #region Example: How to disable default quoting/escaping of serialized json.  Excel likes the default but it isn't real json
            bool doDefaultQuoting = true;
            if (!doDefaultQuoting)
            {
                writerConfig.ShouldQuote = (value, context) =>
                {
                    var index = context.Record.Count;
                    var name = ((PropertyInfo)context.WriterConfiguration.Maps.Find<ActivityEventModel>().MemberMaps[index].Data.Member).Name;
                    if (name == "EventData")
                    {
                        return false;
                    }
                    return ConfigurationFunctions.ShouldQuote(value, context);
                };
            }
            #endregion

            using (var stream = new StreamWriter(tempFilePath))
            using (var csv = new CsvWriter(stream, writerConfig ))
            {
                csv.Configuration.RegisterClassMap<ActivityEventCsvMap>();
                csv.WriteRecords(filteredEvents);
            }

            return new TemporaryPhysicalFileResult(tempFilePath, "text/csv") { FileDownloadName = $"FileDropActivity{DateTime.UtcNow:s}.csv" };
        }

        [HttpGet]
        public async Task<IActionResult> AccountSettings(Guid fileDropId)
        {
            Guid clientId = (await _dbContext.FileDrop.SingleOrDefaultAsync(d => d.Id == fileDropId))?.ClientId ?? Guid.Empty;
            ApplicationUser user = await _userManager.GetUserAsync(User);

            #region Validation
            if (clientId == Guid.Empty)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {fileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, clientId));
            if (!adminRoleResult.Succeeded)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to manage File Drops for this client.");
                return Unauthorized();
            }
            #endregion

            SftpAccountSettingsModel model = await _fileDropQueries.GetAccountSettingsModelAsync(fileDropId, user);

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSftpAccountCredentials([FromBody] Guid fileDropId)
        {
            #region Preliminary validation
            if (!ModelState.IsValid || fileDropId == Guid.Empty)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} Invalid request, no bound value for input parameter fileDropId");
                Response.Headers.Add("Warning", "Invalid request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            ApplicationUser mapUser = await _userManager.FindByNameAsync(User.Identity.Name);
            SftpAccount account = await _dbContext.SftpAccount
                                                  .Include(a => a.ApplicationUser)
                                                  .Where(a => EF.Functions.ILike(User.Identity.Name, a.ApplicationUser.UserName))
                                                  .SingleOrDefaultAsync(a => a.FileDropId == fileDropId);

            if (account == null)
            {
                FileDrop fileDrop = _dbContext.FileDrop.Find(fileDropId);
                account = new SftpAccount(fileDropId)
                {
                    ApplicationUserId = mapUser.Id,
                    UserName = User.Identity.Name /*+ fileDrop.ShortHash*/,  // TODO Add the shorthash when that migration is available
                };
                _dbContext.SftpAccount.Add(account);
                await _dbContext.SaveChangesAsync();
            }

            #region Authorization
            if (account.IsSuspended)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} The sftp account for user {account.UserName} is suspended. The user may not update credentials.");
                Response.Headers.Add("Warning", "Your account is suspended. You may not update account credentials.");
                return Unauthorized();
            }
            #endregion

            // 144 bits yields same base64 encoded length (24) as 128 bits (no padding characters) with 16 more random bits
            byte[] randomBytes = new byte[144 / 8];
            new RNGCryptoServiceProvider().GetBytes(randomBytes);
            string newPassword = Convert.ToBase64String(randomBytes);

            var returnModel = new SftpAccountCredentialModel
            {
                UserName = account.UserName,
                Password = newPassword,
            };

            account.Password = newPassword;
            await _dbContext.SaveChangesAsync();

            return Json(returnModel);
        }
    }

    /// <summary>
    /// A modified version of PhysicalFileResult that deletes the file after streaming its contents to the response
    /// </summary>
    public class TemporaryPhysicalFileResult : PhysicalFileResult
    {
        public TemporaryPhysicalFileResult(string fileName, string contentType)
                     : base(fileName, contentType) { }
        public TemporaryPhysicalFileResult(string fileName, MediaTypeHeaderValue contentType)
                     : base(fileName, contentType) { }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            await base.ExecuteResultAsync(context);
            File.Delete(FileName);
        }
    }
}
