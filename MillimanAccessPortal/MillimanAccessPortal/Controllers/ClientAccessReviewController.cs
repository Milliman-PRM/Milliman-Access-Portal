/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing authorized content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries;
using AuditLogLib.Event;
using AuditLogLib.Services;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.Models.ClientAccessReview;
using Microsoft.AspNetCore.Http;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using MillimanAccessPortal.Models.FileDropModels;
using System.Collections.Generic;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAccessReviewController : Controller
    {
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService _authorizationService;
        private readonly IConfiguration _applicationConfig;
        private readonly ClientAccessReviewQueries _clientAccessReviewQueries;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientAccessReviewController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ClientAccessReviewQueries ClientAccessReviewQueriesArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg
            )
        {
            _auditLogger = AuditLoggerArg;
            _authorizationService = AuthorizationServiceArg;
            _clientAccessReviewQueries = ClientAccessReviewQueriesArg;
            _userManager = UserManagerArg;
            _applicationConfig = ApplicationConfigArg;
        }

        // GET: ClientAccessReview
        /// <summary>
        /// Action leading to the main landing page for Client administration UI
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            #region Authorization
            // User must have Admin role to at least 1 Client OR to at least 1 ProfitCenter
            AuthorizationResult Result = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!Result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            return View();
        }

        /// <summary
        /// GET the configured time period values for review warnings
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PageGlobalData()
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            var GlobalData = new ClientAccessReviewGlobalDataModel
            {
                ClientReviewEarlyWarningDays = _applicationConfig.GetValue<int>("ClientReviewEarlyWarningDays"),
            };

            return Json(GlobalData);
        }

        /// <summary>
        /// GET clients authorized to the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _userManager.GetUserAsync(User);
            var model = await _clientAccessReviewQueries.GetClientModelAsync(currentUser);

            return Json(model);
        }

        /// <summary>
        /// GET ClientSummary
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ClientSummary(Guid ClientId)
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            var model = await _clientAccessReviewQueries.GetClientSummaryAsync(ClientId);

            return Json(model);
        }

        /// <summary>
        /// GET 
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BeginClientAccessReview(Guid ClientId)
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            ClientAccessReviewModel model = await _clientAccessReviewQueries.GetClientAccessReviewModel(ClientId);

            var user = await _userManager.GetUserAsync(User);
            _auditLogger.Log(AuditEventType.ClientAccessReviewPresented.ToEvent(ClientId, model), user.UserName, user.Id);

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClientAccessReview([FromBody] ApproveClientAccessReviewModel ReviewModel)
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _userManager.GetUserAsync(User);
            try
            {
                var model = await _clientAccessReviewQueries.ApproveClientAccessReviewAsync(currentUser, ReviewModel.ClientId);

                _auditLogger.Log(AuditEventType.ClientAccessReviewApproved.ToEvent(ReviewModel.ClientId, ReviewModel.ReviewId), currentUser.UserName, currentUser.Id);

                return Json(model);
            }
            catch (ApplicationException ex)
            {
                Log.Information(ex, $"Action {ControllerContext.ActionDescriptor.DisplayName}, query failed for user {User.Identity.Name}");
                Response.Headers.Add("Warning", ex.Message);
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        /// <summary>
        /// GET a .zip of all the information contained in the Client Access Review for the given Client
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadClientAccessReviewSummary(Guid ClientId)
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            ClientAccessReviewModel clientAccessReviewModel = await _clientAccessReviewQueries.GetClientAccessReviewModel(ClientId);
            string clientAccessReviewSummaryExportDirectory = Path.Combine(_applicationConfig.GetValue<string>("Storage:TemporaryExports"), $"{Guid.NewGuid()}");
            var writerConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { IgnoreQuotes = true };

            try
            {
                System.IO.Directory.CreateDirectory(clientAccessReviewSummaryExportDirectory);
            }
            catch (IOException ex)
            {
                Log.Information(ex, $"Action {ControllerContext.ActionDescriptor.DisplayName}, failed on creation of temporary directory for exports.");
                Response.Headers.Add("Warning", ex.Message);
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            #region Client Summary
            ClientSummaryModel clientSummaryModel = await _clientAccessReviewQueries.GetClientSummaryAsync(ClientId);
            string clientSummaryTxtPath = Path.Combine(clientAccessReviewSummaryExportDirectory, "Client Summary.txt");
            using (var stream = new StreamWriter(clientSummaryTxtPath))
            {
                stream.WriteLine($"Client Summary{Environment.NewLine}");
                stream.WriteLine($"Client name: {clientSummaryModel.ClientName}");
                stream.WriteLine($"Client code: {(clientSummaryModel.ClientCode != null && !clientSummaryModel.ClientCode.Equals("")? clientSummaryModel.ClientCode : "N/A")}");
                stream.WriteLine($"Review due date: {(clientSummaryModel.LastReviewDate != null ? clientSummaryModel.LastReviewDate : "N/A")}");
                stream.WriteLine($"Last review by: {(clientSummaryModel.LastReviewedBy != null && !clientSummaryModel.LastReviewedBy.Name.Equals("") ? clientSummaryModel.LastReviewedBy.UserEmail : "N/A")}");
                stream.WriteLine($"Primary Contact: {(clientSummaryModel.PrimaryContactEmail != null ? clientSummaryModel.PrimaryContactEmail : "N/A")}");
                stream.WriteLine("Client Admins:");
                if (clientAccessReviewModel.ClientAdmins.Count == 0)
                {
                    stream.WriteLine("- N/A");
                }
                else
                {
                    clientAccessReviewModel.ClientAdmins.ForEach((clientAdmin) =>
                    {
                        stream.WriteLine($"- {clientAdmin.UserEmail}");
                    });
                }
                stream.WriteLine($"Profit Center: {clientAccessReviewModel.AssignedProfitCenterName}");
                stream.WriteLine("Profit Center Admins:");
                if (clientAccessReviewModel.ProfitCenterAdmins.Count == 0)
                {
                    stream.WriteLine("- N/A");
                }
                else
                {
                    clientAccessReviewModel.ProfitCenterAdmins.ForEach((profitCenterAdmin) =>
                    {
                        stream.WriteLine($"- {profitCenterAdmin.UserEmail}");
                    });
                }

                stream.WriteLine("Approved Email Domain List");
                if (clientAccessReviewModel.ApprovedEmailDomainList.Count == 0)
                {
                    stream.WriteLine("- N/A");
                }
                else
                {
                    clientAccessReviewModel.ApprovedEmailDomainList.ForEach((email) =>
                    {
                        stream.WriteLine($"- {email}");
                    });
                }

                stream.WriteLine("Email address exception list:");
                if (clientAccessReviewModel.ApprovedEmailExceptionList.Count == 0)
                {
                    stream.WriteLine("- N/A");
                }
                else
                {
                    clientAccessReviewModel.ApprovedEmailExceptionList.ForEach((emailException) =>
                    {
                        stream.WriteLine($"- {emailException}");
                    });
                }
                stream.Close();
            }
            #endregion

            #region User Roles
            string userRolesCsvPath = Path.Combine(clientAccessReviewSummaryExportDirectory, "User Roles.csv");
            List<UserRolesRowItem> userRolesRowItems = new List<UserRolesRowItem>();
            clientAccessReviewModel.MemberUsers.ForEach((mu) =>
            {
                bool clientAdminValue;
                bool contentPublisherValue;
                bool contentAccessAdminValue;
                bool contentUserValue;
                bool fileDropAdminValue;
                bool fileDropUserValue;
                userRolesRowItems.Add(new UserRolesRowItem()
                {
                    UserName = mu.Name,
                    UserEmail = mu.UserEmail,
                    LastLoginDate = mu.LastLoginDate.ToString(),
                    IsClientAdmin = mu.ClientUserRoles.TryGetValue(RoleEnum.Admin, out clientAdminValue) ? clientAdminValue : false,
                    IsContentPublisher = mu.ClientUserRoles.TryGetValue(RoleEnum.ContentPublisher, out contentPublisherValue) ? contentPublisherValue : false,
                    IsContentAccessAdmin = mu.ClientUserRoles.TryGetValue(RoleEnum.ContentAccessAdmin, out contentAccessAdminValue) ? contentAccessAdminValue : false,
                    IsContentUser = mu.ClientUserRoles.TryGetValue(RoleEnum.ContentUser, out contentUserValue) ? contentUserValue : false,
                    IsFileDropAdmin = mu.ClientUserRoles.TryGetValue(RoleEnum.FileDropAdmin, out fileDropAdminValue) ? fileDropAdminValue : false,
                    IsFileDropUser = mu.ClientUserRoles.TryGetValue(RoleEnum.FileDropUser, out fileDropUserValue) ? fileDropUserValue : false,
                });
            });
            using (var stream = new StreamWriter(userRolesCsvPath))
            using (var csv = new CsvWriter(stream, writerConfig))
            {
                csv.Configuration.RegisterClassMap<UserRolesCsvMap>();
                csv.WriteRecords(userRolesRowItems);
                stream.Close();
            }
            #endregion

            #region Content Access
            string contentAccessCsvPath = Path.Combine(clientAccessReviewSummaryExportDirectory, "Content Access.csv");
            List<ContentAccessRowItem> contentAccessRowItems = new List<ContentAccessRowItem>();
            clientAccessReviewModel.ContentItems.ForEach((ci) =>
            {
                ci.SelectionGroups.ForEach((sg) =>
                {
                    if (sg.AuthorizedUsers.Count == 0)
                    {
                        contentAccessRowItems.Add(new ContentAccessRowItem()
                        {
                            ContentName = ci.ContentItemName,
                            SelectionGroupName = sg.SelectionGroupName,
                            UserName = "",
                            UserEmail = "",
                            Suspended = sg.IsSuspended,
                        });
                    }
                    else
                    {
                        sg.AuthorizedUsers.ForEach((au) =>
                        {
                            contentAccessRowItems.Add(new ContentAccessRowItem()
                            {
                                ContentName = ci.ContentItemName,
                                SelectionGroupName = sg.SelectionGroupName,
                                UserName = au.Name,
                                UserEmail = au.UserEmail,
                                Suspended = sg.IsSuspended,
                            });
                        });
                    }
                });
            });
            using (var stream = new StreamWriter(contentAccessCsvPath))
            using (var csv = new CsvWriter(stream, writerConfig))
            {
                csv.Configuration.RegisterClassMap<ContentAccessCsvMap>();
                csv.WriteRecords(contentAccessRowItems);
                stream.Close();
            }
            #endregion

            #region File Drop Access
            string fileDropAccessCsvPath = Path.Combine(clientAccessReviewSummaryExportDirectory, "File Drop Access.csv");
            List<FileDropAccessRowItem> fileDropAccessRowItems = new List<FileDropAccessRowItem>();
            clientAccessReviewModel.FileDrops.ForEach(fd =>
            {
                fd.PermissionGroups.ForEach(pg =>
                {
                    if (pg.AuthorizedMapUsers.Count == 0)
                    {
                        fileDropAccessRowItems.Add(new FileDropAccessRowItem()
                        {
                            FileDropName = fd.FileDropName,
                            UserGroupName = pg.PermissionGroupName,
                            UserName = "",
                            UserEmail = "",
                            CanDownload = pg.Permissions.TryGetValue("Read", out _),
                            CanUpload = pg.Permissions.TryGetValue("Write", out _),
                            CanDelete = pg.Permissions.TryGetValue("Delete", out _),
                        });
                    }
                    else
                    {
                        pg.AuthorizedMapUsers.ForEach(amu =>
                        {
                            fileDropAccessRowItems.Add(new FileDropAccessRowItem()
                            {
                                FileDropName = fd.FileDropName,
                                UserGroupName = pg.PermissionGroupName,
                                UserName = amu.Name,
                                UserEmail = amu.UserEmail,
                                CanDownload = pg.Permissions.TryGetValue("Read", out _),
                                CanUpload = pg.Permissions.TryGetValue("Write", out _),
                                CanDelete = pg.Permissions.TryGetValue("Delete", out _),
                            });
                        });
                    }
                });
            });
            using (var stream = new StreamWriter(fileDropAccessCsvPath))
            using (var csv = new CsvWriter(stream, writerConfig))
            {
                csv.Configuration.RegisterClassMap<FileDropAccessCsvMap>();
                csv.WriteRecords(fileDropAccessRowItems);
                stream.Close();
            }
            #endregion

            #region Metadata
            string metadataTxtPath = Path.Combine(clientAccessReviewSummaryExportDirectory, "Client Access Review Summary Metadata.txt");
            using (var stream = new StreamWriter(metadataTxtPath))
            {
                stream.WriteLine($"Client Access Review Metadata{Environment.NewLine}");
                stream.WriteLine($"The information contained in this zip file represents a snapshot of the Client information at the time of export.");
                stream.WriteLine($"Viewing this information does not qualify as a Client Access Review.{Environment.NewLine}");
                stream.WriteLine($"Client: {clientAccessReviewModel.ClientName}");
                stream.WriteLine($"Date of export: {DateTime.UtcNow.ToShortDateString()}");
                stream.WriteLine($"User who exported: {User.Identity.Name}");
                stream.Close();
            }
            #endregion

            string zipFileName = $"{clientAccessReviewModel.ClientName} - Client Access Review Summary - {DateTime.Now.ToString("MM-dd-yyyy")}";

            // Zip files to temporary compressed file for download
            try
            {
                ZipFile.CreateFromDirectory(clientAccessReviewSummaryExportDirectory, Path.Combine(_applicationConfig.GetValue<string>("Storage:TemporaryExports"), zipFileName));
                Directory.Delete(clientAccessReviewSummaryExportDirectory, true);
            }
            catch (IOException ex)
            {
                Log.Information(ex, $"Action {ControllerContext.ActionDescriptor.DisplayName}, failed on export.");
                Response.Headers.Add("Warning", ex.Message);
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            return new TemporaryPhysicalFileResult(Path.Combine(_applicationConfig.GetValue<string>("Storage:TemporaryExports"), zipFileName), "application/zip") { FileDownloadName = $"{zipFileName}.zip" };
        }
    }
}
