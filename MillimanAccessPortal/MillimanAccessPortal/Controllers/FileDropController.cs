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
using FileDropLib;
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
        private readonly IFileDropUploadTaskTracker _fileDropUploadTaskTracker;

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
            IConfiguration applicationConfigArg,
            IFileDropUploadTaskTracker fileDropUploadTaskTrackerArg
            )
        {
            _auditLogger = auditLoggerArg;
            _authorizationService = authorizationServiceArg;
            _dbContext = contextArg;
            _fileDropQueries = fileDropQueriesArg;
            _fileSystemTasks = fileSystemTasks;
            _userManager = userManagerArg;
            _applicationConfig = applicationConfigArg;
            _fileDropUploadTaskTracker = fileDropUploadTaskTrackerArg;
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
                Clients = await _fileDropQueries.GetAuthorizedClientsModelAsync(await _userManager.GetUserAsync(User)),
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
            Client referencedClient = await _dbContext.Client.FindAsync(fileDropModel.ClientId);

            #region Validation
            if (ModelState.Any(v => v.Value.ValidationState == ModelValidationState.Invalid && !new[] { nameof(FileDrop.RootPath), nameof(FileDrop.ShortHash) }.Contains(v.Key)  ))  // RootPath & ShortHash can/should be invalid here
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

            if (_dbContext.FileDrop.Any(d => d.ClientId == referencedClient.Id &&
                                             EF.Functions.ILike(fileDropModel.Name, d.Name)))
            {
                Log.Warning($"{ControllerContext.ActionDescriptor.DisplayName} Attempt to create FileDrop with name <{fileDropModel.Name}>, already in use for client {referencedClient.Id}");
                Response.Headers.Add("Warning", "The requested FileDrop name is already in use for this client.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            try
            {
                fileDropModel.RootPath = Guid.NewGuid().ToString();

                string propsedShortHash = string.Empty;
                int loopCounter = 0;
                do
                {
                    if (loopCounter > 10)  // Try at most 10 times
                    {
                        Log.Warning($"In action {ControllerContext.ActionDescriptor.DisplayName} failed to generate unique FileDrop hash after {loopCounter} tries");
                        Response.Headers.Add("Warning", "A processing error occurred.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                    loopCounter++;

                    byte[] randomBytes = new byte[24 / 8];
                    new RNGCryptoServiceProvider().GetBytes(randomBytes);
                    propsedShortHash = Convert.ToBase64String(randomBytes);
                }
                while (_dbContext.FileDrop.Any(d => d.ShortHash == propsedShortHash)); // Hash must be unique in the db
                fileDropModel.ShortHash = propsedShortHash;

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

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In action {ControllerContext.ActionDescriptor.DisplayName} failed to create FileDrop root folder using global root path {fileDropGlobalRoot} and subfolder name {fileDropModel.RootPath}");
                Response.Headers.Add("Warning", "Failed to create file drop home folder.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            _auditLogger.Log(AuditEventType.FileDropCreated.ToEvent(fileDropModel, fileDropModel.ClientId, fileDropModel.Client.Name));

            ApplicationUser currentUser = await _userManager.GetUserAsync(User);
            FileDropsModel model = await _fileDropQueries.GetFileDropsModelForClientAsync(fileDropModel.ClientId, currentUser.Id);
            model.CurrentFileDropId = fileDropModel.Id;
            model.PermissionGroups = await _fileDropQueries.GetPermissionGroupsModelForFileDropAsync(fileDropModel.Id, fileDropModel.ClientId, currentUser);

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

            FileDrop fileDropRecord = await _dbContext.FileDrop
                                                      .Include(d => d.Client)
                                                      .SingleAsync(d => d.Id == fileDropModel.Id);
            FileDrop oldFileDrop = new FileDrop { Id = fileDropRecord.Id,
                                                  Name = fileDropRecord.Name,
                                                  Description = fileDropRecord.Description,
                                                  RootPath = fileDropRecord.RootPath,
            };

            #region Validation
            if (ModelState.Any(v => v.Value.ValidationState == ModelValidationState.Invalid && !new[] { nameof(FileDrop.RootPath), nameof(FileDrop.ShortHash) }.Contains(v.Key)))  // RootPath & ShortHash can/should be invalid here
            {
                Log.Warning($"{ControllerContext.ActionDescriptor.DisplayName} ModelState not valid");
                Response.Headers.Add("Warning", "The provided FileDrop information was invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (_dbContext.FileDrop.Any(d => d.ClientId == fileDropRecord.ClientId &&
                                             d.Id != fileDropRecord.Id && 
                                             EF.Functions.ILike(fileDropModel.Name, d.Name)))
            {
                Log.Warning($"{ControllerContext.ActionDescriptor.DisplayName} Attempt to update FileDrop with name <{fileDropModel.Name}>, already in use for client {fileDropRecord.ClientId}");
                Response.Headers.Add("Warning", "The requested FileDrop name is already in use for this client.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (string.IsNullOrWhiteSpace(fileDropModel.Name))
            {
                Log.Warning($"{ControllerContext.ActionDescriptor.DisplayName} new File Drop must have a name");
                Response.Headers.Add("Warning", "The provided FileDrop name was not provided.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            fileDropRecord.Name = fileDropModel.Name;
            fileDropRecord.Description = fileDropModel.Description;
            await _dbContext.SaveChangesAsync();

            _auditLogger.Log(AuditEventType.FileDropUpdated.ToEvent(oldFileDrop, fileDropRecord, fileDropRecord.ClientId, fileDropRecord.Client.Name));

            ApplicationUser currentUser = await _userManager.GetUserAsync(User);
            FileDropsModel model = await _fileDropQueries.GetFileDropsModelForClientAsync(fileDropModel.ClientId, currentUser.Id);
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

            ApplicationUser currentUser = await _userManager.GetUserAsync(User);
            FileDropsModel model = await _fileDropQueries.GetFileDropsModelForClientAsync(clientId, currentUser.Id);

            return Json(model);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFileDrop([FromBody] Guid id)
        {
            FileDrop fileDrop = await _dbContext.FileDrop
                                                .Include(d => d.Client)
                                                .Include(d => d.SftpAccounts)
                                                    .ThenInclude(a => a.ApplicationUser)
                                                .SingleOrDefaultAsync(d => d.Id == id);

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
                List<FileDropPermissionGroupMembershipLogModel> membershipModel = new List<FileDropPermissionGroupMembershipLogModel>();
                foreach (FileDropUserPermissionGroup group in _dbContext.FileDropUserPermissionGroup
                                                                        .Include(g => g.SftpAccounts)
                                                                            .ThenInclude(a => a.ApplicationUser)
                                                                        .Where(g => g.FileDropId == fileDrop.Id))
                {
                    membershipModel.Add(new FileDropPermissionGroupMembershipLogModel(group));
                }

                foreach (FileDropFile file in _dbContext.FileDropDirectory
                                                        .Where(d => d.FileDropId == fileDrop.Id)
                                                        .SelectMany(d => d.Files))
                {
                    _dbContext.FileDropFile.RemoveRange(file);
                }

                _dbContext.FileDrop.Remove(fileDrop);
                
                await _dbContext.SaveChangesAsync();

                _auditLogger.Log(AuditEventType.FileDropDeleted.ToEvent(fileDrop, fileDrop.Client, membershipModel));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to complete the request");
                Response.Headers.Add("Warning", "Failed to complete the request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            string fullRootPath = default;
            try
            {
                fullRootPath = Path.Combine(_applicationConfig.GetValue<string>("Storage:FileDropRoot"), fileDrop.RootPath);
                FileSystemUtil.DeleteDirectoryWithRetry(fullRootPath, true, 4, 1000);
            } 
            catch(Exception ex)
            {
                Log.Warning(ex, $"Failed to delete root folder {fullRootPath} associated with File Drop with Id {fileDrop.Id}, named {fileDrop.Name}");
            }

            ApplicationUser currentUser = await _userManager.GetUserAsync(User);
            FileDropsModel model = await _fileDropQueries.GetFileDropsModelForClientAsync(fileDrop.ClientId, currentUser.Id);
            model.CurrentFileDropId = id;

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

            ApplicationUser currentUser = await _userManager.GetUserAsync(User);
            PermissionGroupsModel model = await _fileDropQueries.GetPermissionGroupsModelForFileDropAsync(FileDropId, ClientId, currentUser);

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
                ApplicationUser currentUser = await _userManager.GetUserAsync(User);
                PermissionGroupsModel returnModel = await _fileDropQueries.UpdatePermissionGroupsAsync(model, currentUser);
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

            var serverFilters = new List<Expression<Func<AuditEvent, bool>>>
            {
                { e => e.TimeStampUtc > oldestTimestamp},
                { e => e.EventCode >= 8000 && e.EventCode < 9000 },
            };

            // TODO When PostgreSQL 12 is deployed create a computed field as text so this search can run server side & move this expression into the server filters
            var clientFilters = new List<Expression<Func<AuditEvent, bool>>>
            {
                { e => EF.Functions.ILike(e.EventData, idCompareString) }
            };

            List<ActivityEventModel> filteredEvents = await _auditLogger.GetAuditEventsAsync(serverFilters, _dbContext, true, clientFilters);

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

            var serverFilters = new List<Expression<Func<AuditEvent, bool>>>
            {
                { e => e.EventCode >= 8000 && e.EventCode < 9000 },
            };

            // TODO When PostgreSQL 12 is deployed create a computed field as text so this search can run server side & move this expression into the server filters
            var clientFilters = new List<Expression<Func<AuditEvent, bool>>>
            {
                { e => EF.Functions.ILike(e.EventData, idCompareString) }
            };

            List<ActivityEventModel> filteredEvents = await _auditLogger.GetAuditEventsAsync(serverFilters, _dbContext, true, clientFilters);

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

            FileDrop fileDrop = _dbContext.FileDrop.Find(fileDropId);
            SftpAccount account = await _dbContext.SftpAccount
                                      .Include(a => a.ApplicationUser)
                                      .Include(a => a.FileDropUserPermissionGroup)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                      .SingleOrDefaultAsync(a => a.FileDropId == fileDropId);

            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, clientId));
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, clientId));
            if (!adminRoleResult.Succeeded && 
                !(userRoleResult.Succeeded && account.FileDropUserPermissionGroupId.HasValue && (account.FileDropUserPermissionGroup.ReadAccess 
                                                                                              || account.FileDropUserPermissionGroup.WriteAccess 
                                                                                              || account.FileDropUserPermissionGroup.DeleteAccess)))
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to access account settings for this file drop.");
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

            FileDrop fileDrop = _dbContext.FileDrop.Find(fileDropId);
            ApplicationUser mapUser = await _userManager.FindByNameAsync(User.Identity.Name);
            SftpAccount account = await _dbContext.SftpAccount
                                                  .Include(a => a.ApplicationUser)
                                                  .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                                  .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                                  .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                  .Where(a => a.FileDropUserPermissionGroup.ReadAccess 
                                                           || a.FileDropUserPermissionGroup.WriteAccess 
                                                           || a.FileDropUserPermissionGroup.DeleteAccess)
                                                  .SingleOrDefaultAsync(a => a.FileDropId == fileDropId);

            #region Validation
            if (account == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} User {User.Identity.Name} cannot generate credentials for file drop {fileDrop.Id} (named \"{fileDrop.Name}\") because there is no authorized sftp account");
                Response.Headers.Add("Warning", "You may not generate a password because you are not currently authorized to this file drop.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded) 
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the requested file drop.");
                return Unauthorized();
            }
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

            SftpAccountCredentialModel returnModel = new SftpAccountCredentialModel
            {
                UserName = account.UserName,
                Password = newPassword,
            };

            account.Password = newPassword;
            await _dbContext.SaveChangesAsync();

            _auditLogger.Log(AuditEventType.SftpAccountCredentialsGenerated.ToEvent(account, fileDrop));

            return Json(returnModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccountSettings([FromBody] UpdateAccountSettingsModel boundModel)
        {
            #region Preliminary validation
            if (!ModelState.IsValid || boundModel.FileDropId == Guid.Empty)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName}, invalid request");
                Response.Headers.Add("Warning", "Invalid request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            FileDrop fileDrop = _dbContext.FileDrop.Find(boundModel.FileDropId);
            ApplicationUser mapUser = await _userManager.FindByNameAsync(User.Identity.Name);
            SftpAccount account = await _dbContext.SftpAccount
                                                  .Include(a => a.FileDropUserPermissionGroup)
                                                  .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                                  .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                                  .SingleOrDefaultAsync(a => a.FileDropId == boundModel.FileDropId);

            if (account == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName}, account not found");
                Response.Headers.Add("Warning", "Invalid request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, fileDrop.ClientId));
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!adminRoleResult.Succeeded && 
                !(userRoleResult.Succeeded && (account.FileDropUserPermissionGroup.ReadAccess 
                                            || account.FileDropUserPermissionGroup.WriteAccess 
                                            || account.FileDropUserPermissionGroup.DeleteAccess))) {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} User {account.UserName} is not authorized to this action");
                Response.Headers.Add("Warning", "You are not authorized to access this file drop.");
                return Unauthorized();
            }

            if (account.IsSuspended)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} The sftp account for user {account.UserName} is suspended. The user may not update account settings.");
                Response.Headers.Add("Warning", "Your account is suspended. You may not update account settings.");
                return Unauthorized();
            }
            #endregion

            #region update the settings
            var accountNotifications = new HashSet<FileDropUserNotificationModel>(account.NotificationSubscriptions, new FileDropUserNotificationModelSameEventComparer());
            foreach (var newNotificationSetting in boundModel.Notifications)
            {
                if (!accountNotifications.TryGetValue(newNotificationSetting, out FileDropUserNotificationModel existingValue) || existingValue.IsEnabled != newNotificationSetting.IsEnabled)
                {
                    Log.Information($"Updating SftpAccount {account.UserName}, setting for notification type {newNotificationSetting.NotificationType} from {(existingValue != null ? existingValue.IsEnabled.ToString() : "unset")} to {newNotificationSetting.IsEnabled} in file drop {fileDrop.Id} ({fileDrop.Name})");
                }
                accountNotifications.Remove(newNotificationSetting);
                accountNotifications.Add(newNotificationSetting);
            }
            account.NotificationSubscriptions = accountNotifications;
            await _dbContext.SaveChangesAsync();
            #endregion

            SftpAccountSettingsModel model = await _fileDropQueries.GetAccountSettingsModelAsync(boundModel.FileDropId, mapUser);

            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetFolderContents(Guid fileDropId, string canonicalPath)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = _dbContext.FileDrop.Find(fileDropId);
            #region Validation
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {fileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                      //.Include(a => a.ApplicationUser)
                                      .Include(a => a.FileDropUserPermissionGroup)
                                          .ThenInclude(g => g.FileDrop)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                      .Where(a => a.FileDropId == fileDropId)
                                      .Where(a => a.FileDropUserPermissionGroup.ReadAccess 
                                               || a.FileDropUserPermissionGroup.WriteAccess 
                                               || a.FileDropUserPermissionGroup.DeleteAccess)
                                      .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to access this file drop.");
                return Unauthorized();
            }
            #endregion

            try
            {
                DirectoryContentModel returnModel = await _fileDropQueries.CreateFolderContentModelAsync(fileDropId, account, canonicalPath);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "The requested folder was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "Error. Please contact support if this issue continues.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUploadedFile([FromBody] ProcessUploadedFileModel requestModel)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = _dbContext.FileDrop.Find(requestModel.FileDropId);

            #region Validation
            if (fileDrop == null)
            {
                // file drop not found
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {requestModel.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (string.IsNullOrWhiteSpace(requestModel.FileName) || requestModel.FileName.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
            {
                // invalid file name
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} invalid file name {requestModel.FileName} specified");
                Response.Headers.Add("Warning", "An invalid file name was requested.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                                  .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                                  .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                                  .Where(a => a.FileDropId == requestModel.FileDropId)
                                                  .Where(a => a.FileDropUserPermissionGroup.WriteAccess)
                                                  .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to access this file drop.");
                return Unauthorized();
            }
            #endregion

            FileDropDirectory directory = await _dbContext.FileDropDirectory.FindAsync(requestModel.FileDropDirectoryId);

            #region Validation
            if (directory == null || directory.FileDropId != requestModel.FileDropId)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDropDirectory with requested Id {requestModel.FileDropDirectoryId} does not belong to FileDrop with requested Id {requestModel.FileDropId}");
                Response.Headers.Add("Warning", "Error completing the request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // This is where the asynchronous task gets queued
            Guid taskId = _fileDropUploadTaskTracker.RequestUploadProcessing(requestModel, account);

            return Json(taskId);
        }

        [HttpGet]
        public async Task<IActionResult> GetFileUploadStatus(Guid taskId, Guid fileDropId)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = _dbContext.FileDrop.Find(fileDropId);
            #region Validation
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {fileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                      //.Include(a => a.ApplicationUser)
                                      .Include(a => a.FileDropUserPermissionGroup)
                                          .ThenInclude(g => g.FileDrop)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                      .Where(a => !a.FileDropUserPermissionGroupId.HasValue)
                                      .Where(a => !a.FileDropUserPermissionGroup.WriteAccess)
                                      .Where(a => a.FileDropId == fileDropId)
                                      .SingleOrDefaultAsync();

            #region Authorization
            var adminRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin, fileDrop.ClientId));
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!adminRoleResult.Succeeded && (!userRoleResult.Succeeded || account == null))
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to access this file drop.");
                return Unauthorized();
            }
            #endregion

            try
            {
                UploadStatusModel returnModel = await _fileDropQueries.GetUploadTaskStatusAsync(taskId, fileDropId);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Response.Headers.Add("Warning", ex.Message);
                return Json(new UploadStatusModel());
            }

        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(Guid FileDropId, Guid FileDropFileId, string CanonicalFilePath)
        {
            if (FileDropId == null || FileDropFileId == null || CanonicalFilePath == null || ModelState.ErrorCount > 0)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} invalid ModelState, errors are: {string.Join("; ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)))}");
                Response.Headers.Add("Warning", "Invalid request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = _dbContext.FileDrop.Find(FileDropId);
            #region Validation
            if (fileDrop == null)
            {
                // file drop not found
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (string.IsNullOrWhiteSpace(CanonicalFilePath) || 
                Path.GetInvalidPathChars().Any(c => Path.GetDirectoryName(CanonicalFilePath).Contains(c)) ||
                Path.GetInvalidFileNameChars().Any(c => Path.GetFileName(CanonicalFilePath).Contains(c)) )
            {
                // invalid file name
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} invalid canonical path <{CanonicalFilePath}> specified");
                Response.Headers.Add("Warning", "An invalid file path was requested.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                                  .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                                  .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                                  .Where(a => a.FileDropId == FileDropId)
                                                  .Where(a => a.FileDropUserPermissionGroup.ReadAccess)
                                                  .SingleOrDefaultAsync();

            FileDropFile fileRecord = await _dbContext.FileDropFile.Include(f => f.Directory).SingleOrDefaultAsync(d => d.Id == FileDropFileId);
            string fullFilePathFromDb = Path.Combine(_applicationConfig.GetValue<string>("Storage:FileDropRoot"), fileDrop.RootPath, fileRecord.Directory.CanonicalFileDropPath.TrimStart('/'), fileRecord.FileName);

            #region Validation
            if (fileRecord?.Directory == null || fileRecord.Directory.FileDropId != fileDrop.Id)
            {
                // file path string inconsistent with database records
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDropFile with requested Id {FileDropFileId} does not belong to FileDrop with requested Id {FileDropId}");
                Response.Headers.Add("Warning", "Error completing the request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (fileRecord.FileName != Path.GetFileName(CanonicalFilePath))
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} the requested file name {Path.GetFileName(CanonicalFilePath)} does not match the name {fileRecord.FileName} in the database");
                Response.Headers.Add("Warning", "An invalid file name was requested.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (!System.IO.File.Exists(fullFilePathFromDb))
            {
                // file at requested path not found in storage
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} file name {CanonicalFilePath} not found");
                Response.Headers.Add("Warning", "An invalid file name was requested.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to access this file drop.");
                return Unauthorized();
            }
            #endregion

            try
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: returning file {fullFilePathFromDb}");
                return PhysicalFile(fullFilePathFromDb, "application/octet-stream", Path.GetFileName(CanonicalFilePath));
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} action: Failed to return requested file {Path.GetFileName(fullFilePathFromDb)}");
                Response.Headers.Add("Warning", $"Failed to return requested file {Path.GetFileName(CanonicalFilePath)}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFileDropFolder([FromBody] CreateFileDropFolderRequestModel requestModel)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = await _dbContext.FileDrop
                                                .Include(d => d.Client)
                                                .SingleOrDefaultAsync(d => d.Id == requestModel.FileDropId);

            #region Validation
            if (fileDrop == null)
            {
                // file drop not found
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {requestModel.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            #endregion

            SftpAccount authorizedAccount = await _dbContext.SftpAccount
                                                            .Include(a => a.FileDropUserPermissionGroup)
                                                                .ThenInclude(g => g.FileDrop)
                                                            .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                                            .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                                            .Where(a => a.FileDropId == requestModel.FileDropId)
                                                            .Where(a => a.FileDropUserPermissionGroup.WriteAccess)
                                                            .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || authorizedAccount == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to access this file drop.");
                return Unauthorized();
            }
            #endregion

            FileDropDirectory containingDirectoryRecord = _dbContext.FileDropDirectory.Find(requestModel.ContainingFileDropDirectoryId);
            IEnumerable<FileDropDirectory> existingSiblingDirectories = _dbContext.FileDropDirectory.Where(d => d.ParentDirectoryId == requestModel.ContainingFileDropDirectoryId).ToList();

            #region Validation
            if (containingDirectoryRecord == null || containingDirectoryRecord.FileDropId != fileDrop.Id)
            {
                // invalid containing directory
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} invalid parent directory ID {requestModel.ContainingFileDropDirectoryId} specified");
                Response.Headers.Add("Warning", "An invalid parent directory was requested.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (requestModel.NewFolderName.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
            {
                // invalid character in new file name
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} invalid character in requested new folder name {requestModel.NewFolderName}");
                Response.Headers.Add("Warning", "The requested folder name contains an invalid character.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (existingSiblingDirectories.Any(d => Path.GetFileName(d.CanonicalFileDropPath).Equals(requestModel.NewFolderName, StringComparison.InvariantCultureIgnoreCase)))
            {
                // directory already exists
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} directory {requestModel.NewFolderName} already exists in the requested containing directory {containingDirectoryRecord.CanonicalFileDropPath}");
                Response.Headers.Add("Warning", "The requested new directory already exists.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            FileDropOperations.CreateDirectory(Path.Combine(containingDirectoryRecord.CanonicalFileDropPath, requestModel.NewFolderName),
                                               Path.Combine(_applicationConfig.GetValue<string>("Storage:FileDropRoot"), fileDrop.RootPath),
                                               fileDrop.Name,
                                               requestModel.Description,
                                               fileDrop.Id,
                                               fileDrop.Client.Id,
                                               fileDrop.Client.Name,
                                               authorizedAccount,
                                               user);
            try
            {
                DirectoryContentModel returnModel = await _fileDropQueries.CreateFolderContentModelAsync(requestModel.FileDropId, authorizedAccount, containingDirectoryRecord.CanonicalFileDropPath);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "The containing folder was not found while building the folder contents list.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "Error. Please contact support if this issue continues.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFileDropFile([FromBody] RemoveFileDropFileRequestModel requestModel)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = await _dbContext.FileDrop.FindAsync(requestModel.FileDropId);
            #region Validation
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {requestModel.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                      //.Include(a => a.ApplicationUser)
                                      .Include(a => a.FileDropUserPermissionGroup)
                                          .ThenInclude(g => g.FileDrop)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-{fileDrop.ShortHash}"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%{fileDrop.ShortHash}"))
                                      .Where(a => a.FileDropId == requestModel.FileDropId)
                                      .Where(a => a.FileDropUserPermissionGroup.DeleteAccess)
                                      .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to perform the requested action.");
                return Unauthorized();
            }
            #endregion

            #region Perform the delete of the file
            var fileRecord = await _dbContext.FileDropFile
                                             .Include(f => f.Directory)
                                             .SingleOrDefaultAsync(f => f.Id == requestModel.FileId);
            string fileDropGlobalRoot = _applicationConfig.GetValue<string>("Storage:FileDropRoot");

            var fileDropRootPath = Path.Combine(fileDropGlobalRoot, fileDrop.RootPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string canonicalPath = Path.Combine(fileRecord.Directory.CanonicalFileDropPath, fileRecord.FileName);
            FileDropOperations.RemoveFile(canonicalPath, fileDrop.Name, fileDropRootPath, requestModel.FileDropId, account, user);
            #endregion

            try
            {
                DirectoryContentModel returnModel = await _fileDropQueries.CreateFolderContentModelAsync(requestModel.FileDropId, account, fileRecord.Directory.CanonicalFileDropPath);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "The requested folder was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "Error. Please contact support if this issue continues.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFileDropFolder([FromBody] RemoveFileDropFolderRequestModel requestModel)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = _dbContext.FileDrop.Find(requestModel.FileDropId);
            #region Validation
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {requestModel.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                      //.Include(a => a.ApplicationUser)
                                      .Include(a => a.FileDropUserPermissionGroup)
                                          .ThenInclude(g => g.FileDrop)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-{fileDrop.ShortHash}"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%{fileDrop.ShortHash}"))
                                      .Where(a => a.FileDropId == requestModel.FileDropId)
                                      .Where(a => a.FileDropUserPermissionGroup.DeleteAccess)
                                      .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to perform the requested action.");
                return Unauthorized();
            }
            #endregion

            #region Perform the delete of the folder and all contents
            var folderRecord = await _dbContext.FileDropDirectory
                                             .Include(f => f.ParentDirectory)
                                             .SingleOrDefaultAsync(f => f.Id == requestModel.FolderId);
            string fileDropGlobalRoot = _applicationConfig.GetValue<string>("Storage:FileDropRoot");

            var fileDropRootPath = Path.Combine(fileDropGlobalRoot, fileDrop.RootPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));            
            FileDropOperations.RemoveDirectory(folderRecord.CanonicalFileDropPath, fileDrop.Name, fileDropRootPath, requestModel.FileDropId, account, user);
            #endregion

            try
            {
                DirectoryContentModel returnModel = await _fileDropQueries.CreateFolderContentModelAsync(requestModel.FileDropId, account, folderRecord.ParentDirectory.CanonicalFileDropPath);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "The requested folder was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "Error. Please contact support if this issue continues.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFileDropFile([FromBody] UpdateFileDropFileRequestModel requestModel)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = _dbContext.FileDrop.Find(requestModel.FileDropId);
            #region Validation
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {requestModel.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                      //.Include(a => a.ApplicationUser)
                                      .Include(a => a.FileDropUserPermissionGroup)
                                          .ThenInclude(g => g.FileDrop)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-{fileDrop.ShortHash}"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%{fileDrop.ShortHash}"))
                                      .Where(a => a.FileDropId == requestModel.FileDropId)
                                      .Where(a => a.FileDropUserPermissionGroup.WriteAccess)
                                      .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to perform the requested action.");
                return Unauthorized();
            }
            #endregion

            #region Update file description
            var fileRecord = await _dbContext.FileDropFile
                                             .Include(f => f.Directory)
                                             .SingleOrDefaultAsync(f => f.Id == requestModel.FileId);
            string canonicalPath = fileRecord?.Directory?.CanonicalFileDropPath;
            fileRecord.Description = requestModel.FileDescription;
            await _dbContext.SaveChangesAsync();
            #endregion

            try
            {
                DirectoryContentModel returnModel = await _fileDropQueries.CreateFolderContentModelAsync(requestModel.FileDropId, account, canonicalPath);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "The requested folder was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "Error. Please contact support if this issue continues.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFileDropFolder([FromBody] UpdateFileDropFolderRequestModel requestModel)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = _dbContext.FileDrop.Find(requestModel.FileDropId);
            #region Validation
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {requestModel.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                      //.Include(a => a.ApplicationUser)
                                      .Include(a => a.FileDropUserPermissionGroup)
                                          .ThenInclude(g => g.FileDrop)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-{fileDrop.ShortHash}"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%{fileDrop.ShortHash}"))
                                      .Where(a => a.FileDropId == requestModel.FileDropId)
                                      .Where(a => a.FileDropUserPermissionGroup.WriteAccess)
                                      .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to perform the requested action.");
                return Unauthorized();
            }
            #endregion

            #region Update directory description            
            var folderRecord = await _dbContext.FileDropDirectory
                                             .Include(f => f.ParentDirectory)
                                             .SingleOrDefaultAsync(f => f.Id == requestModel.FolderId);
            folderRecord.Description = requestModel.FolderDescription;
            await _dbContext.SaveChangesAsync();
            #endregion

            try
            {
                DirectoryContentModel returnModel = await _fileDropQueries.CreateFolderContentModelAsync(requestModel.FileDropId, account, folderRecord.ParentDirectory.CanonicalFileDropPath);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "The requested folder was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "Error. Please contact support if this issue continues.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameFileDropFolder([FromBody] RenameFileDropFolderRequestModel requestModel)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = await _dbContext.FileDrop
                                                .Include(d => d.Client)
                                                .SingleOrDefaultAsync(d => d.Id == requestModel.FileDropId);
            #region Validation
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {requestModel.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                      //.Include(a => a.ApplicationUser)
                                      .Include(a => a.FileDropUserPermissionGroup)
                                          .ThenInclude(g => g.FileDrop)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                      .Where(a => a.FileDropId == requestModel.FileDropId)
                                      .Where(a => a.FileDropUserPermissionGroup.WriteAccess)
                                      .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to perform the requested action.");
                return Unauthorized();
            }
            #endregion

            #region Rename the folder
            var folderRecord = await _dbContext.FileDropDirectory
                                             .SingleOrDefaultAsync(f => f.Id == requestModel.DirectoryId);

            string fileDropGlobalRoot = _applicationConfig.GetValue<string>("Storage:FileDropRoot");
            string folderExistingAbsolutePath = Path.Combine(fileDropGlobalRoot, 
                                                             fileDrop.RootPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), 
                                                             folderRecord.CanonicalFileDropPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string folderFutureAbsolutePath = Path.Combine(fileDropGlobalRoot,
                                                           fileDrop.RootPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                                           requestModel.ParentCanonicalPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                                           requestModel.DirectoryName);

            FileDropOperations.RenameDirectory(folderExistingAbsolutePath,
                                               folderFutureAbsolutePath,
                                               Path.Combine(fileDropGlobalRoot, fileDrop.RootPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                                               fileDrop.Name,
                                               fileDrop.Id,
                                               fileDrop.ClientId,
                                               fileDrop.Client.Name,
                                               account,
                                               user);
            #endregion

            try
            {
                DirectoryContentModel returnModel = await _fileDropQueries.CreateFolderContentModelAsync(requestModel.FileDropId, account, requestModel.ParentCanonicalPath);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "The requested folder was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "Error. Please contact support if this issue continues.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameFileDropFile([FromBody] RenameFileDropFileRequestModel requestModel)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            FileDrop fileDrop = await _dbContext.FileDrop
                                                .Include(d => d.Client)
                                                .SingleOrDefaultAsync(d => d.Id == requestModel.FileDropId);

            #region Validation
            if (fileDrop == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDrop with requested Id {requestModel.FileDropId} not found");
                Response.Headers.Add("Warning", "The requested file drop was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SftpAccount account = await _dbContext.SftpAccount
                                      .Include(a => a.FileDropUserPermissionGroup)
                                          .ThenInclude(g => g.FileDrop)
                                      .Where(a => EF.Functions.ILike(a.UserName, $"{User.Identity.Name}-%"))
                                      .Where(a => EF.Functions.Like(a.UserName, $"%-{fileDrop.ShortHash}"))
                                      .Where(a => a.FileDropId == requestModel.FileDropId)
                                      .Where(a => a.FileDropUserPermissionGroup.WriteAccess)
                                      .SingleOrDefaultAsync();

            #region Authorization
            var userRoleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser, fileDrop.ClientId));
            if (!userRoleResult.Succeeded || account == null)
            {
                Log.Information($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to perform the requested action.");
                return Unauthorized();
            }
            #endregion

            FileDropFile fileRecord = await _dbContext.FileDropFile
                                                      .Include(f => f.Directory)
                                                      .SingleOrDefaultAsync(f => f.Id == requestModel.FileId);

            FileDropDirectory destinationDirectory = requestModel.NewFolderId == fileRecord.DirectoryId
                                                   ? fileRecord.Directory
                                                   : await _dbContext.FileDropDirectory.FindAsync(requestModel.NewFolderId);

            #region Validation
            if (fileRecord == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} FileDropFile with requested Id {requestModel.FileId} not found");
                Response.Headers.Add("Warning", "The requested file was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (!Path.GetExtension(requestModel.FileName).Equals(Path.GetExtension(fileRecord.FileName), StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} request to modify the filename extension is not allowed");
                Response.Headers.Add("Warning", "The file extension cannot be modified.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (destinationDirectory == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} Record for requested destination directory not found");
                Response.Headers.Add("Warning", "The requested destination directory was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Rename the folder

            string fileDropGlobalRoot = _applicationConfig.GetValue<string>("Storage:FileDropRoot");
            string fileExistingCanonicalPath = Path.Combine(fileRecord.Directory.CanonicalFileDropPath, fileRecord.FileName);
            string fileFutureCanonicalPath = Path.Combine(destinationDirectory.CanonicalFileDropPath, requestModel.FileName);

            var opResult = FileDropOperations.RenameFile(fileExistingCanonicalPath,
                                                         fileFutureCanonicalPath,
                                                         Path.Combine(fileDropGlobalRoot, fileDrop.RootPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                                                         fileDrop.Name,
                                                         fileDrop.Id,
                                                         fileDrop.ClientId,
                                                         fileDrop.Client.Name,
                                                         account,
                                                         user);
            if (opResult != FileDropOperations.FileDropOperationResult.OK)
            {
                string warningMessage = opResult switch
                {
                    FileDropOperations.FileDropOperationResult.NO_SUCH_FILE => "The file was not found",
                    FileDropOperations.FileDropOperationResult.NO_SUCH_PATH => "The target directory does not exist",
                    FileDropOperations.FileDropOperationResult.FILE_ALREADY_EXISTS => "A file with the new name already exists",
                    FileDropOperations.FileDropOperationResult.FAILURE => "Failed to rename the file",
                    _ => "Unhandled error"
                };
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} FileDropOperations.RenameFile returned result {opResult.GetDisplayNameString()} with message {warningMessage}");
                Response.Headers.Add("Warning", warningMessage);
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            try
            {
                DirectoryContentModel returnModel = await _fileDropQueries.CreateFolderContentModelAsync(requestModel.FileDropId, account, destinationDirectory.CanonicalFileDropPath);
                return Json(returnModel);
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "The requested folder was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} {ex.Message}");
                Response.Headers.Add("Warning", "Error. Please contact support if this issue continues.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
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
