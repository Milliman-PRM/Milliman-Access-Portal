/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Implements MVC actions for the FileDrop view
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Models;
using AuditLogLib.Services;
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
using System.IO;
using System.Linq;
using System.Reflection;
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
                List<FileDropFile> files = await _dbContext.FileDropDirectory
                                                           .Where(d => d.FileDropId == fileDrop.Id)
                                                           .SelectMany(d => d.Files)
                                                           .ToListAsync();
                _dbContext.FileDropFile.RemoveRange(files);
                _dbContext.FileDrop.Remove(fileDrop);
                _dbContext.SaveChanges();
                _auditLogger.Log(AuditEventType.FileDropDeleted.ToEvent(fileDrop, fileDrop.Client, fileDrop.SftpAccounts));
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
                FileSystemUtil.DeleteDirectoryWithRetry(fullRootPath, attempts: 4,  baseIntervalMs: 1000);
            } 
            catch(Exception ex)
            {
                Log.Warning(ex, $"Failed to delete root folder {fullRootPath} associated with File Drop with Id {fileDrop.Id}, named {fileDrop.Name}");
            }

            FileDropsModel model = _fileDropQueries.GetFileDropsModelForClient(fileDrop.ClientId, await _userManager.GetUserAsync(User));
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
                Log.Warning($"Requested FileDrop Id {model.FileDropId} not found");
                Response.Headers.Add("Warning", "Failed to complete the request.");
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

            var returnModel = await _fileDropQueries.UpdatePermissionGroups(model);

            return Json(returnModel);
        }
    }

}
