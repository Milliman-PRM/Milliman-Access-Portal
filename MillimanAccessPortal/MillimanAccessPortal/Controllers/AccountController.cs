/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Actions related to user account management
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Services;
using AuditLogLib.Models;
using MapCommonLib;
using MapCommonLib.ActionFilters;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Models.SharedModels;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Authorization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMessageQueue _messageSender;
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly AuthenticationService _authentService;

        public AccountController(
            ApplicationDbContext ContextArg,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IMessageQueue messageSender,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            IConfiguration ConfigArg,
            IServiceProvider serviceProviderArg,
            IAuthenticationService authentService
            )
        {
            DbContext = ContextArg;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _messageSender = messageSender;
            _auditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            _configuration = ConfigArg;
            _serviceProvider = serviceProviderArg;
            _authentService = (AuthenticationService)authentService;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(User.Identity.Name) && !User.Identity.IsAuthenticated)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
            else
            {
                // Sign out current user and redirect back here to regenerate a valid antiforgery token
                await HttpContext.SignOutAsync();
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(Login), new { returnUrl });
            }
        }

        //
        // POST: /Account/IsLocalAccount
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IsLocalAccount(string userName)
        {
            bool localAccount = await IsUserAccountLocal(userName);
            return Json(new { localAccount });
        }

        /// <summary>
        /// [NonAction] determines whether a username should be authenticated locally from the application's Identity provider
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        [NonAction]
        public async Task<bool> IsUserAccountLocal(string userName)
        {
            MapDbContextLib.Context.AuthenticationScheme scheme = GetExternalAuthenticationScheme(userName);

            bool isLocal = scheme == null ||
                           scheme.Name == (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name;

            return isLocal;
        }

        /// <summary>
        /// Determines the assigned or otherwise appropriate external authentication scheme associated with a username.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns>The identified scheme name or <see langword="null"/> if none is appropriate</returns>
        [NonAction]
        public MapDbContextLib.Context.AuthenticationScheme GetExternalAuthenticationScheme(string userName)
        {
            // 1. If the specified user has an assigned scheme
            MapDbContextLib.Context.AuthenticationScheme assignedScheme = DbContext.ApplicationUser
                                                                                   .Include(u => u.AuthenticationScheme)
                                                                                   .SingleOrDefault(u => EF.Functions.ILike(u.UserName, userName))
                                                                                   ?.AuthenticationScheme;
            if (assignedScheme != null)
            {
                return assignedScheme;
            }

            string userFullDomain = userName.Contains('@')
                ? userName.Substring(userName.IndexOf('@') + 1)
                : userName;

            // 2. If the username's domain is found in a domain list of a scheme
            MapDbContextLib.Context.AuthenticationScheme matchingScheme = DbContext.AuthenticationScheme.SingleOrDefault(s => s.DomainList.Contains(userFullDomain));
            if (matchingScheme != null)
            {
                return matchingScheme;
            }

            // 3. If the username's secondary domain matches a scheme name
            if (userFullDomain.Contains('.'))
            {
                // Secondary domain is the portion of userName between '@' and the last '.'
                string userSecondaryDomain = userFullDomain.Substring(0, userFullDomain.LastIndexOf('.'));
                matchingScheme = DbContext.AuthenticationScheme.SingleOrDefault(s => EF.Functions.ILike(s.Name, userSecondaryDomain));

                return matchingScheme;
            }

            return null;
        }

        //
        // GET: /Account/RemoteAuthenticate
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RemoteAuthenticate(string userName, string returnUrl)
        {
            MapDbContextLib.Context.AuthenticationScheme scheme = GetExternalAuthenticationScheme(userName);

            if (scheme != null && scheme.Name != (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name)
            {
                string redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl = returnUrl });
                AuthenticationProperties properties = _signInManager.ConfigureExternalAuthenticationProperties(scheme.Name, redirectUrl);
                properties.SetString("username", userName);
                switch (scheme.Type)
                {
                    case AuthenticationType.WsFederation:
                        string wauthValue = (scheme.SchemePropertiesObj as WsFederationSchemeProperties)?.Wauth;
                        if (!string.IsNullOrWhiteSpace(wauthValue))
                        {
                            properties.SetString("wauth", wauthValue);
                        }
                        break;
                }

                return Challenge(properties, scheme.Name);
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");

            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);

                if (user == null)
                {
                    Log.Information($"{ControllerContext.ActionDescriptor.DisplayName}, user {model.Username} not found, local login rejected");
                    _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(model.Username, (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name, LoginFailureReason.UserAccountNotFound));
                    Response.Headers.Add("Warning", "Invalid login attempt.");
                    return Ok();
                }

                // Disable login for users with last login date too long ago. Similar logic in Startup.cs for remote authentication
                int idleUserAllowanceMonths = _configuration.GetValue("DisableInactiveUserMonths", 12);
                if (user.LastLoginUtc < DateTime.UtcNow.Date.AddMonths(-idleUserAllowanceMonths))
                {
                    NotifyUserAboutDisabledAccount(user);
                    Log.Information($"{ControllerContext.ActionDescriptor.DisplayName}, user {model.Username} disabled, local login rejected");
                    _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(model.Username, (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name, LoginFailureReason.UserAccountDisabled));
                    Response.Headers.Add("Warning", $"This account is currently disabled.  Please contact your Milliman consultant, or email {_configuration.GetValue<string>("SupportEmailAlias")}");
                    return Ok();
                }

                if (user.IsSuspended)
                {
                    _auditLogger.Log(AuditEventType.LoginIsSuspended.ToEvent(user.UserName));
                    Log.Information($"{ControllerContext.ActionDescriptor.DisplayName}, User {user.UserName} suspended, local login rejected");

                    Response.Headers.Add("Warning", $"This account is currently suspended.  Please contact your Milliman consultant, or email {_configuration.GetValue<string>("SupportEmailAlias")}>");
                    return Ok();
                }

                // Only notify of password expiration if the correct password was provided
                // Redirect user to the password reset view to set a new password
                bool passwordSuccess = await _userManager.CheckPasswordAsync(user, model.Password);

                // Set a default value in case the configuration isn't found or isn't an int
                int defaultExpirationDays = 30;
                int expirationDays = defaultExpirationDays;
                try
                {
                    expirationDays = _configuration.GetValue<int>("PasswordExpirationDays");
                }
                catch
                {
                    expirationDays = defaultExpirationDays;
                    Log.Warning($"PasswordExpirationDays value not found in configuration, or cannot be cast to an integer. The default value of {expirationDays} will be used");
                }

                if (passwordSuccess && user.LastPasswordChangeDateTimeUtc.AddDays(expirationDays) < DateTime.UtcNow)
                {
                    await RequestPasswordReset(user, PasswordResetRequestReason.PasswordExpired, Request.Scheme, Request.Host);

                    Log.Information($"User {model.Username} password is expired, sent password reset email");
                    string WhatHappenedMessage = "Your password has expired. Check your email for a link to reset your password.";
                    Response.Headers.Add("Warning", WhatHappenedMessage);
                    return Ok();
                }

                Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: true);
                switch (result)
                {
                    case var r when r.RequiresTwoFactor:
                        Response.Headers.Add("NavigateTo", Url.Action(nameof(LoginStepTwo), new { model.Username, returnUrl = model.ReturnUrl }));
                        return Ok();

                    case var r when r.Succeeded:
                        await SignInCommon(user, (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name);

                        // Provide the location that should be navigated to (or fall back on default route)
                        Response.Headers.Add("NavigateTo", model.ReturnUrl ?? "/");
                        return Ok();

                    case var r when r.IsLockedOut:
                        Log.Information($"User {model.Username} account locked out");
                        _auditLogger.Log(AuditEventType.LoginIsLockedOut.ToEvent(), model.Username);
                        Response.Headers.Add("Warning", "This account has been locked out, please try again later.");
                        return Ok();

                    case var r when r.IsNotAllowed:
                        Log.Information($"User {model.Username} login not allowed");
                        _auditLogger.Log(AuditEventType.LoginNotAllowed.ToEvent(), model.Username);
                        Response.Headers.Add("Warning", "Invalid login attempt.");
                        return Ok();

                    default:
                        Log.Information($"User {model.Username} PasswordSignInAsync did not succeed");
                        _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(model.Username, (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name, LoginFailureReason.PasswordSignInAsyncFailed));
                        Response.Headers.Add("Warning", "Invalid login attempt.");
                        return Ok();
                }
            }

            // If we got this far, something failed, redisplay form
            Response.Headers.Add("Warning", "Login failed.");
            return Ok();
        }

        /// <summary>
        /// Does everything that is common to externally and internally signed in users
        /// </summary>
        [NonAction]
        private async Task SignInCommon(ApplicationUser user, string scheme)
        {
            try
            {
                user.LastLoginUtc = DateTime.UtcNow;
                await DbContext.SaveChangesAsync();
                HttpContext.Session.SetString("SessionId", HttpContext.Session.Id);
                Log.Information($"User {user.UserName} logged in with scheme {scheme}");
                _auditLogger.Log(AuditEventType.LoginSuccess.ToEvent(scheme), user.UserName, HttpContext.Session.Id);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserAgreement(UserAgreementViewModel model)
        {
            ApplicationUser user = await _userManager.FindByNameAsync(User.Identity.Name);

            if (user == null)
            {
                Log.Error($"Account.UserAgreement: GET Requested user {User.Identity.Name} not found");
                return RedirectToAction(nameof(Login));
            }
            if (user.IsUserAgreementAccepted == true)
            {
                Log.Error($"Account.UserAgreement: GET Request for user {user.UserName} to accept, but user has already accepted");
                return RedirectToAction(nameof(Login));
            }

            model.AgreementText = DbContext.NameValueConfiguration.Find(nameof(ConfiguredValueKeys.UserAgreementText))?.Value ?? "User agreement text is not configured";
            model.ValidationId = Guid.NewGuid();

            UserAgreementLogModel userAgreement = new UserAgreementLogModel
            {
                ValidationId = model.ValidationId,
                AgreementText = model.AgreementText,
            };

            _auditLogger.Log(AuditEventType.UserAgreementPresented.ToEvent(userAgreement), user.UserName);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineUserAgreement(Guid validationId)
        {
            ApplicationUser user = await _userManager.FindByNameAsync(User.Identity.Name);

            _auditLogger.Log(AuditEventType.UserAgreementDeclined.ToEvent(validationId), user.UserName);

            await _signInManager.SignOutAsync();
            Response.Headers.Add("NavigateTo", "/");
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptUserAgreement(UserAgreementViewModel model)
        {
            ApplicationUser user = await _userManager.FindByNameAsync(User.Identity.Name);
            user.IsUserAgreementAccepted = true;
            DbContext.SaveChanges();

            _auditLogger.Log(AuditEventType.UserAgreementAcceptance.ToEvent(model.ValidationId), user.UserName);

            Response.Headers.Add("NavigateTo", string.IsNullOrEmpty(model.ReturnUrl) ? "/" : model.ReturnUrl);
            return Ok();
        }

        //
        // GET: /Account/CreateInitialUser
        [HttpGet]
        [AllowAnonymous]
        public IActionResult CreateInitialUser(string returnUrl = null)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");

            // If any users exist, return 404. We don't want to even hint that this URL is valid.
            if (_userManager.Users.Any())
            {
                return NotFound();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/CreateInitialUser
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInitialUser(CreateInitialUserViewModel model, string returnUrl = null)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action with {{@CreateInitialUserViewModel}}", model);

            IdentityResult createUserResult = null;
            IdentityResult roleGrantResult = null;

            // If any users exist, return 404. We don't want to even hint that this URL is valid.
            if (_userManager.Users.Any())
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName}, unsuccessful, some user(s) already exist");
                return NotFound();
            }

            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                ApplicationUser newUser = new ApplicationUser { UserName = model.Email, Email = model.Email, LastLoginUtc = DateTime.UtcNow };
                ApplicationRole adminRole = await _roleManager.FindByNameAsync(RoleEnum.Admin.ToString());

                using (var txn = await DbContext.Database.BeginTransactionAsync())
                {
                    createUserResult = await _userManager.CreateAsync(newUser);
                    roleGrantResult = await _userManager.AddToRoleAsync(newUser, adminRole.Name);

                    if (createUserResult.Succeeded && roleGrantResult.Succeeded)
                    {
                        await txn.CommitAsync();

                        Log.Information($"Initial user {model.Email} account created new with password.");
                        _auditLogger.Log(AuditEventType.UserAccountCreated.ToEvent(newUser));
                        _auditLogger.Log(AuditEventType.SystemRoleAssigned.ToEvent(newUser, RoleEnum.Admin, HitrustReason.InitialSystemUser.NumericValue));

                        // Send the confirmation message
                        string welcomeText = _configuration["Global:DefaultNewUserWelcomeText"];  // could be null, that's ok
                        await SendNewAccountWelcomeEmail(newUser, Request.Scheme, Request.Host, welcomeText);

                        return RedirectToLocal(returnUrl);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            AddErrors(createUserResult);
            AddErrors(roleGrantResult);
            return View(model);
        }

        //
        // POST: /Account/Logout
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");
            ApplicationUser appUser = null;
            try
            {
                appUser = await _userManager.GetUserAsync(User);
            }
            catch (Exception ex)
            {
                var x = ex;
            }
            await _signInManager.SignOutAsync();

            Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: user {appUser?.UserName ?? "<unknown>"} logged out.");
            _auditLogger.Log(AuditEventType.Logout.ToEvent(), appUser?.UserName);

            Response.Cookies.Delete(SessionDefaults.CookieName);
            HttpContext.Session.Clear();

            return Ok();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action with {{@Provider}}", provider);

            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");

            if (string.IsNullOrWhiteSpace(HttpContext.User?.Identity?.Name) ||
                !HttpContext.User.Identity.IsAuthenticated)
            {
                Log.Warning($"{ControllerContext.ActionDescriptor.DisplayName} action invoked with {{@HttpContextUser}}", HttpContext.User);
                return RedirectToAction(nameof(Login));
            }

            if (remoteError != null)
            {
                Log.Error($"Error during remote authentication: {remoteError}");
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByNameAsync(HttpContext.User.Identity.Name);
            await SignInCommon(user, GetExternalAuthenticationScheme(user.UserName)?.Name);

            returnUrl = returnUrl ?? Url.Content("~/");
            return LocalRedirect(returnUrl);
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action with {{@ExternalLoginConfirmationViewModel}}", model);

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        Log.Debug($"User added a login using provider {info.LoginProvider}");
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [NonAction]
        public async Task SendNewAccountWelcomeEmail(ApplicationUser RequestedUser, string requestScheme, HostString requestHost, string SettableEmailText = null)
        {
            Log.Verbose("Entered AccountController.SendNewAccountWelcomeEmail action with {@UserName}", RequestedUser.UserName);

            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(RequestedUser);

            UriBuilder emailLink = new UriBuilder
            {
                Scheme = requestScheme,
                Host = requestHost.Host,
                Port = requestHost.Port ?? -1,
                Path = $"/{nameof(AccountController).Replace("Controller", "")}/{nameof(AccountController.EnableAccount)}",
                Query = $"userId={RequestedUser.Id}&code={Uri.EscapeDataString(emailConfirmationToken)}"
            };

            UriBuilder rootSiteUrl = new UriBuilder
            {
                Scheme = requestScheme,
                Host = requestHost.Host,
                Port = requestHost.Port ?? -1
            };

            // Configurable portion of email body
            string emailBody = string.IsNullOrWhiteSpace(SettableEmailText)
                ? string.Empty
                : SettableEmailText + $"{Environment.NewLine}{Environment.NewLine}";

            int accountActivationDays = _configuration.GetValue("AccountActivationTokenTimespanDays", GlobalFunctions.fallbackAccountActivationTokenTimespanDays);

            string supportEmailAlias = _configuration.GetValue<string>("SupportEmailAlias");
            // Non-configurable portion of email body
            emailBody += $"Your username is: {RequestedUser.UserName}{Environment.NewLine}{Environment.NewLine}" +
                $"Activate your account by clicking the link below or copying and pasting the link into your web browser.{Environment.NewLine}{Environment.NewLine}" +
                $"{emailLink.Uri.AbsoluteUri}{Environment.NewLine}{Environment.NewLine}" +
                $"This link will expire {accountActivationDays} days after the time it was sent.{Environment.NewLine}{Environment.NewLine}" +
                $"Once you have activated your account, MAP can be accessed at {rootSiteUrl.Uri.AbsoluteUri}{Environment.NewLine}{Environment.NewLine}" +
                $"If you have any questions regarding this email, please contact {supportEmailAlias}";
            string emailSubject = "Welcome to Milliman Access Portal!";

            _messageSender.QueueEmail(RequestedUser.Email, emailSubject, emailBody /*, optional senderAddress, optional senderName*/);

            Log.Information($"Welcome email queued to email address {RequestedUser.Email}");
        }

        /// <summary>
        /// Generates a password reset token for the specified user and notifies the user via. email
        /// </summary>
        /// <param name="RequestedUser"></param>
        /// <param name="reason"></param>
        /// <param name="host"></param>
        /// <param name="requestScheme"></param>
        /// <returns></returns>
        [NonAction]
        public async Task RequestPasswordReset(ApplicationUser RequestedUser, PasswordResetRequestReason reason, string requestScheme, HostString host)
        {
            Log.Verbose("Entered AccountControllerContext.RequestPasswordReset action with {@UserName}", RequestedUser.UserName);

            if (!DbContext.ApplicationUser.Any(u => u.Id == RequestedUser.Id))
            {
                Log.Information($"Password reset requested by user <{User?.Identity?.Name}> for non-existing user with email {RequestedUser.Email}");
                return;
            }

            string emailBody, appLogMsg;
            if (await IsUserAccountLocal(RequestedUser.UserName))
            {
                string PasswordResetToken = await _userManager.GeneratePasswordResetTokenAsync(RequestedUser);

                UriBuilder link = new UriBuilder
                {
                    Scheme = requestScheme,
                    Host = host.Host,
                    Port = host.Port ?? -1,
                    Path = $"/{nameof(AccountController).Replace("Controller", "")}/{nameof(AccountController.ResetPassword)}",
                    Query = $"userEmail={RequestedUser.Email}&passwordResetToken={Uri.EscapeDataString(PasswordResetToken)}"
                };

                int expirationHours = _configuration.GetValue("PasswordResetTokenTimespanHours", GlobalFunctions.fallbackPasswordResetTokenTimespanHours);

                emailBody = $"A password reset for your Milliman Access Portal (MAP) account ({RequestedUser.Email}) has been requested. To reset your password, please click on the link below or copy and paste the link into your browser. {Environment.NewLine}";
                emailBody += $"{link.Uri.AbsoluteUri}{Environment.NewLine}";
                emailBody += $"This link will expire {expirationHours} hours after the time it was sent.";

                appLogMsg = $"Password reset email queued to address {RequestedUser.Email}{Environment.NewLine}reason: {reason.GetDisplayNameString()}{Environment.NewLine}emailed link: {link.Uri.AbsoluteUri}";
            }
            else
            {
                MapDbContextLib.Context.AuthenticationScheme authScheme = GetExternalAuthenticationScheme(RequestedUser.UserName);

                emailBody = $"A password reset for your Milliman Access Portal (MAP) account ({RequestedUser.Email}) has been requested. " +
                    $"Your MAP account uses login services from your organization ({authScheme.DisplayName}). Please contact your IT department if you require password assistance.";

                appLogMsg = $"Password reset was requested for an externally authenticated user with email {RequestedUser.Email}. Information email was queued. No other action taken.";
            }

            _messageSender.QueueEmail(RequestedUser.Email, "MAP password reset", emailBody);
            Log.Information(appLogMsg);
            _auditLogger.Log(AuditEventType.PasswordResetRequested.ToEvent(RequestedUser, reason));
        }

        /// <summary>
        /// Sends a user an email notifying them that their account is disabled.
        /// </summary>
        /// <param name="RequestedUser"></param>
        /// <returns></returns>
        [NonAction]
        internal void NotifyUserAboutDisabledAccount(ApplicationUser RequestedUser)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action with {RequestedUser.UserName}");

            string emailBody;
            string supportEmailAlias = _configuration.GetValue<string>("SupportEmailAlias");

            emailBody = $"Your MAP account is currently disabled due to inactivity.  Please contact your ";
            emailBody += $"Milliman consultant, or email us at {supportEmailAlias} for assistance.";

            _messageSender.QueueEmail(RequestedUser.Email, "MAP account is disabled", emailBody);
            Log.Information($"Disabled account email queued to address {RequestedUser.Email}");
        }

        // GET: /Account/EnableAccount
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> EnableAccount(string userId, string code)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} GET action with {{@UserId}}", userId);

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} GET action: missing argument(s), aborting");
                return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error")));
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} GET action: user {userId} not found, aborting");
                return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error")));
            }
            int disableInactiveUserMonths = _configuration.GetValue("DisableInactiveUserMonths", 12);
            if (user.LastLoginUtc < DateTime.UtcNow.Date.AddMonths(-disableInactiveUserMonths))
            {
                NotifyUserAboutDisabledAccount(user);
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} GET action: user account {userId} is disabled due to inactivity over the past {disableInactiveUserMonths} months, aborting");
                return View("UserMessage", new UserMessageModel("Your MAP account is disabled due to inactivity.", "Please see your email for detail."));
            }

            if (user.EmailConfirmed)  // Account is already activated
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} GET action: user {userId} account is already enabled, aborting");
                return RedirectToAction(nameof(Login));
            }

            // If the code is not valid (likely expired), re-send the welcome email and notify the user
            DataProtectorTokenProvider<ApplicationUser> emailConfirmationTokenProvider = (DataProtectorTokenProvider<ApplicationUser>)_serviceProvider.GetService(typeof(DataProtectorTokenProvider<ApplicationUser>));
            bool tokenIsValid;
            for (tokenIsValid = await emailConfirmationTokenProvider.ValidateAsync("EmailConfirmation", code, _userManager, user); !tokenIsValid && code.EndsWith('=');)
            {
                code = code.Remove(code.Length - 1);
                tokenIsValid = await emailConfirmationTokenProvider.ValidateAsync("EmailConfirmation", code, _userManager, user);
            }

            if (!tokenIsValid)
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} GET action: confirmation token is invalid for user name {user.UserName}, may be expired.");

                string supportEmailAlias = _configuration.GetValue<string>("SupportEmailAlias");
                var messageModel = new UserMessageModel
                {
                    PrimaryMessages = { $"Your account activation link has either expired or is invalid. Please click <b>RESEND</b> to receive a new welcome email and try again." },
                    SecondaryMessages = { $"If you continue to be directed to this page, please contact <a href=\"mailto:{supportEmailAlias}\">{supportEmailAlias}</a>." },
                    Buttons = new List<ConfiguredButton>
                        {
                            new ConfiguredButton
                            {
                                Value = "CANCEL",
                                Action = nameof(Login),
                                Controller = nameof(AccountController).Replace("Controller", ""),
                                Method = "get",
                                ButtonClass = "link-button",
                            },
                            new ConfiguredButton
                            {
                                Value = "RESEND",
                                Action = nameof(NewWelcomEmailBecauseInvalidToken),
                                Controller = nameof(AccountController).Replace("Controller", ""),
                                ButtonClass = "blue-button",
                                RouteData = new Dictionary<string, string>
                                {
                                    { "userEmail", user.Email },
                                    { "confirmationMessage", $"Thank you.  A new user welcome email has been sent to {user.Email}."}
                                }
                            },
                        }
                };
                return View("UserMessage", messageModel);
            }

            // Prompt for the user's profile data
            var model = new EnableAccountViewModel
            {
                Id = user.Id,
                Code = code,
                Username = user.UserName,
                IsLocalAccount = await IsUserAccountLocal(user.UserName),
            };
            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} GET action: complete");
            return View(model);
        }

        // POST: /Account/EnableAccount
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAccount(EnableAccountViewModel model)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} POST action with {{@UserName}}", model.Username);

            List<string> nonRequiredKeysForExternalAuthentication = new List<string> {
                nameof(EnableAccountViewModel.NewPassword),
                nameof(EnableAccountViewModel.ConfirmNewPassword),
                nameof(EnableAccountViewModel.PasswordsAreValid) };

            if ((model.IsLocalAccount && !ModelState.IsValid) ||
                (!model.IsLocalAccount && ModelState.Where(v => v.Value.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                                                    .Any(v => !nonRequiredKeysForExternalAuthentication.Contains(v.Key))))
            {
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user == null)
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user {model.Id} not found, aborting");
                return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error")));
            }

            if (user.EmailConfirmed)  // Account is already activated
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user {model.Id} account is already activated, aborting");
                return RedirectToAction(nameof(Login));
            }

            using (var Txn = await DbContext.Database.BeginTransactionAsync())
            {
                // Confirm the user's account
                IdentityResult confirmEmailResult = await _userManager.ConfirmEmailAsync(user, model.Code);
                if (!confirmEmailResult.Succeeded)
                {
                    if (confirmEmailResult.Errors.Any(e => e.Code == "InvalidToken"))  // Happens when token is expired. I don't know whether it could indicate anything else
                    {
                        string WelcomeText = _configuration["Global:DefaultNewUserWelcomeText"];  // could be null, that's ok
                        Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, WelcomeText));

                        Log.Information($"EnableAccount failed for user {model.Username} with code 'InvalidToken', it is likely that the token is expired, new welcome email sent");
                        string WhatHappenedMessage = "Your previous Milliman Access Portal account activation link is invalid and may have expired.  A new link has been emailed to you.";
                        return View("UserMessage", new UserMessageModel(WhatHappenedMessage));
                    }
                    else
                    {
                        string confirmEmailErrors = $"Error while confirming account: {string.Join($", ", confirmEmailResult.Errors.Select(e => e.Description))}";
                        Response.Headers.Add("Warning", confirmEmailErrors);

                        Log.Error($"EnableAccount failed from _userManager.ConfirmEmailAsync(user, model.Code), not due to token expiration: user {user.UserName}, errors: {confirmEmailErrors}");

                        return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error")));
                    }
                }

                if (await IsUserAccountLocal(model.Username))
                {
                    // Set the initial password
                    IdentityResult addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
                    if (!addPasswordResult.Succeeded)
                    {
                        string addPasswordErrors = $"Error while adding initial password: {string.Join($", ", addPasswordResult.Errors.Select(e => e.Description))}";
                        Response.Headers.Add("Warning", addPasswordErrors);

                        Log.Error($"Error for user {model.Username} while adding initial password: {string.Join($", ", addPasswordResult.Errors.Select(e => e.Description))}");

                        return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error")));
                    }

                    // Save password hash in history
                    user.PasswordHistoryObj = user.PasswordHistoryObj.Append<PreviousPassword>(new PreviousPassword(model.NewPassword)).ToList<PreviousPassword>();
                    user.LastPasswordChangeDateTimeUtc = DateTime.UtcNow;
                    var addPasswordHistoryResult = await _userManager.UpdateAsync(user);
                    if (!addPasswordHistoryResult.Succeeded)
                    {
                        string addPasswordHistoryErrors = $"Error while setting password history: {string.Join($", ", addPasswordHistoryResult.Errors.Select(e => e.Description))}";
                        Response.Headers.Add("Warning", addPasswordHistoryErrors);

                        Log.Information($"Error for user {model.Username} while saving history: {string.Join($", ", addPasswordHistoryResult.Errors.Select(e => e.Description))}");

                        return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error")));
                    }
                }

                // Update other user account settings
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Employer = model.Employer;
                user.PhoneNumber = model.Phone;
                var updateAccountSettingsResult = await _userManager.UpdateAsync(user);
                if (!updateAccountSettingsResult.Succeeded)
                {
                    string updateAccountSettingsErrors = $"Error while setting password history: {string.Join($", ", updateAccountSettingsResult.Errors.Select(e => e.Description))}";
                    Response.Headers.Add("Warning", updateAccountSettingsErrors);

                    Log.Information($"Error for user {model.Username} while saving updated user profile {{Profile}}", user);

                    return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error")));
                }

                await Txn.CommitAsync();

                Log.Verbose($"User {model.Username} account enabled and profile saved");
                _auditLogger.Log(AuditEventType.UserAccountEnabled.ToEvent(user));

                return RedirectToAction(nameof(Login));
            }
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} GET action");

            // Simply prompts for user email address so that a reset link can be emailed
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} POST action with {{@ForgotPasswordViewModel}}", model);

            // Sends an email with password reset link to the requested user
            // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (user.LastLoginUtc < DateTime.UtcNow.AddMonths(-_configuration.GetValue("DisableInactiveUserMonths", 12)))
                    {
                        string supportEmailAlias = _configuration.GetValue<string>("SupportEmailAlias");
                        Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: request user with email {model.Email} has a disabled account, current user will be informed of the issue, aborting.");
                        var messageModel = new UserMessageModel
                        {
                            PrimaryMessages = { $"This account is currently disabled. Please contact your Milliman consultant, or email <a href=\"mailto:{supportEmailAlias}\">{supportEmailAlias}</a>." },
                            Buttons = new List<ConfiguredButton>
                            {
                                new ConfiguredButton
                                {
                                    Value = "OK",
                                    Action = nameof(Login),
                                    Controller = nameof(AccountController).Replace("Controller", ""),
                                    Method = "get",
                                    ButtonClass = "link-button",
                                },
                            }
                        };
                        return View("UserMessage", messageModel);
                    }
                    else if (await _userManager.IsEmailConfirmedAsync(user))
                    {
                        await RequestPasswordReset(user, PasswordResetRequestReason.UserInitiated, Request.Scheme, Request.Host);
                        Log.Verbose($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user email address <{model.Email}> reset succeeded");
                    }
                    else
                    {
                        string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                        Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, EmailBodyText));
                        Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: unconfirmed user email address <{model.Email}> requested, welcome email sent.");
                    }
                }
                else
                {
                    Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user email address <{model.Email}> not found");
                    _auditLogger.Log(AuditEventType.PasswordResetRequestedForInvalidEmail.ToEvent(model.Email));
                }
            }

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} post action: success");

            var passwordConfirmationMessage = "Please check your email inbox for a password reset notification.";
            return View("UserMessage", new UserMessageModel(passwordConfirmationMessage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPasswordResetForExistingUser()
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} POST action");
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                if (await IsUserAccountLocal(user.UserName))
                {
                    await RequestPasswordReset(user, PasswordResetRequestReason.UserInitiated, Request.Scheme, Request.Host);
                    Log.Verbose($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user email address <{user.Email}> reset succeeded");
                }
                else
                {
                    Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user email address <{user.Email}> is not local.");
                    // TODO audit log reset password for non-local
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            else
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user <{user.Email}> not found");
                _auditLogger.Log(AuditEventType.PasswordResetRequestedForInvalidEmail.ToEvent(user.Email));
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} post action: success");
            string successMessage = $"Password reset email sent.";
            return Json(new { successMessage });
        }

        /// <summary>
        /// A controller action that initiates a password reset
        /// </summary>
        /// <param name="userEmail"></param>
        /// <param name="confirmationMessage"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordBecauseInvalidToken(string userEmail, string confirmationMessage)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} POST action");

            ApplicationUser user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user <{userEmail}> not found, aborting");
                return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Password Reset Error")));
            }

            await RequestPasswordReset(user, PasswordResetRequestReason.PasswordResetTokenInvalid, Request.Scheme, Request.Host);
            Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: new password reset requested for user {user.UserName}, previous link was expired or invalid");

            return View("UserMessage", new UserMessageModel(confirmationMessage));
        }

        /// <summary>
        /// A controller action that initiates a welcome new user email
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewWelcomEmailBecauseInvalidToken(string userEmail, string confirmationMessage)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} POST action");

            ApplicationUser user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user <{userEmail}> not found, aborting");
                return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error")));
            }

            string WelcomeText = _configuration["Global:DefaultNewUserWelcomeText"];  // could be null, that's ok
            Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, WelcomeText));

            Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: new user welcome email requested for user {user.UserName}, previous link was expired or invalid");

            return View("UserMessage", new UserMessageModel(confirmationMessage));
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userEmail, string passwordResetToken)
        {
            Log.Information($"Entered {ControllerContext.ActionDescriptor.DisplayName} GET action with query string {{@QueryString}},", Request.QueryString);

            ApplicationUser user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} GET action: user <{userEmail}> not found, aborting");
                return View("UserMessage", new UserMessageModel(GlobalFunctions.GenerateErrorMessage(_configuration, "Password Reset Error")));
            }

            PasswordResetSecurityTokenProvider<ApplicationUser> passwordResetTokenProvider = (PasswordResetSecurityTokenProvider<ApplicationUser>)_serviceProvider.GetService(typeof(PasswordResetSecurityTokenProvider<ApplicationUser>));
            bool tokenIsValid;
            for (tokenIsValid = await passwordResetTokenProvider.ValidateAsync("ResetPassword", passwordResetToken, _userManager, user); !tokenIsValid && passwordResetToken.EndsWith('=');)
            {
                passwordResetToken = passwordResetToken.Remove(passwordResetToken.Length - 1);
                tokenIsValid = await passwordResetTokenProvider.ValidateAsync("ResetPassword", passwordResetToken, _userManager, user);
            }

            if (!tokenIsValid)
            {
                if (await _userManager.IsEmailConfirmedAsync(user))
                {
                    Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} GET action: requested for user {user.UserName} having expired or invalid reset token");

                    string supportEmailAlias = _configuration.GetValue<string>("SupportEmailAlias");
                    var messageModel = new UserMessageModel
                    {
                        PrimaryMessages = { "Your password reset link has either expired or is invalid. Please click <b>RESEND</b> to receive a new password reset email and try again." },
                        SecondaryMessages = { $"If you continue to be directed to this page, please contact <a href=\"mailto:{supportEmailAlias}\">{supportEmailAlias}</a>." },
                        Buttons = new List<ConfiguredButton>
                        {
                            new ConfiguredButton
                            {
                                Value = "CANCEL",
                                Action = nameof(Login),
                                Controller = nameof(AccountController).Replace("Controller", ""),
                                Method = "get",
                                ButtonClass = "link-button",
                            },
                            new ConfiguredButton
                            {
                                Value = "RESEND",
                                Action = nameof(ResetPasswordBecauseInvalidToken),
                                Controller = nameof(AccountController).Replace("Controller", ""),
                                ButtonClass = "blue-button",
                                RouteData = new Dictionary<string, string>
                                {
                                    { nameof(userEmail), userEmail },
                                    { "confirmationMessage", $"Thank you.  A new password reset email has been sent to {user.Email}."}
                                }
                            },
                        }
                    };
                    return View("UserMessage", messageModel);
                }
                else
                {
                    string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                    Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, EmailBodyText));

                    Log.Information($"{ControllerContext.ActionDescriptor.DisplayName}  GET action: requested for user {user.UserName} with unconfirmed account, new welcome user email sent");
                    return View("UserMessage", new UserMessageModel("Your Milliman Access Portal account has not yet been activated.  A new account welcome email is being sent to you now.  Please use the link in that email to activate your account."));
                }
            }

            ResetPasswordViewModel model = new ResetPasswordViewModel
            {
                Email = user.Email,
                PasswordResetToken = passwordResetToken,
            };

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} GET action: success");

            return View(model);
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} POST action for user {{@UserEmail}}", model.Email);

            if (!ModelState.IsValid)
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: invalid ModelState, errors {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)))}, aborting");
                model.Message = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
                return View(model);
            }
            string supportEmailAlias = _configuration.GetValue<string>("SupportEmailAlias");
            var passwordResetErrorMessage = $"An error occurred. Please try again. If the issue persists, please contact <a href=\"mailto:{supportEmailAlias}\">{supportEmailAlias}</a>.";
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: requested user with email {model.Email} not found, current user will not be informed of the issue, aborting");
                return View("UserMessage", new UserMessageModel(passwordResetErrorMessage));
            }
            using (var Txn = await DbContext.Database.BeginTransactionAsync())
            {
                IdentityResult result = await _userManager.ResetPasswordAsync(user, model.PasswordResetToken, model.NewPassword);
                if (result.Succeeded)
                {
                    // Save password hash in history
                    user.PasswordHistoryObj = user.PasswordHistoryObj.Append<PreviousPassword>(new PreviousPassword(model.NewPassword)).ToList<PreviousPassword>();
                    user.LastPasswordChangeDateTimeUtc = DateTime.UtcNow;
                    var addHistoryResult = await _userManager.UpdateAsync(user);

                    // Unlock the account
                    var unlock = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MinValue);
                    var resetFailedCount = await _userManager.ResetAccessFailedCountAsync(user);

                    if (!addHistoryResult.Succeeded)
                    {
                        Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} POST action: Failed to save password history for {user.UserName}, ResetPassword action rolled back, aborting");
                    }

                    if (!unlock.Succeeded)
                    {
                        Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} POST action: Failed to unlock account for {user.UserName}, ResetPassword action rolled back, aborting");
                    }

                    if (!resetFailedCount.Succeeded)
                    {
                        Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} POST action: Failed to reset failed login attempt count for {user.UserName}, ResetPassword action rolled back, aborting");
                    }

                    if (unlock.Succeeded && resetFailedCount.Succeeded && addHistoryResult.Succeeded)
                    {
                        await Txn.CommitAsync();
                        Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: succeeded for user {user.UserName }");
                        _auditLogger.Log(AuditEventType.PasswordResetCompleted.ToEvent(user));
                        return View("UserMessage", new UserMessageModel("Your password has been reset. <a href=\"/Account/Login\">Click here to log in</a>."));
                    }
                    else
                    {
                        return View("UserMessage", new UserMessageModel(passwordResetErrorMessage));
                    }

                }
                else if (result.Errors.Any(e => e.Code == "InvalidToken"))  // Could be expired, mismatched security stamp, etc. The reason is not accessible here.
                {
                    string UserMsg = "";
                    if (await _userManager.IsEmailConfirmedAsync(user))
                    {
                        await RequestPasswordReset(user, PasswordResetRequestReason.PasswordResetTokenInvalid, Request.Scheme, Request.Host);
                        Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: for user {user.UserName}, expired reset token, new password reset email sent, aborting");
                        UserMsg = "Your password reset link is invalid or expired.  A new password reset email is being sent to you now.  Please use the link in that email to reset your password.";
                    }
                    else
                    {
                        string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                        Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, EmailBodyText));

                        Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: for user {user.UserName}, the account is not enabled, new welcome email sent, aborting");
                        UserMsg = "Your Milliman Access Portal account has not yet been activated.  A new account welcome email is being sent to you now.  Please use the link in that email to activate your account.";
                    }
                    return View("UserMessage", new UserMessageModel(UserMsg));
                }
                else if (result.Errors.Any())
                {
                    Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} POST action: user: {user.UserName}, errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                AddErrors(result);
            }
            model.Message = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
            return View(model);
        }

        //
        // GET: /Account/NavBar
        [HttpGet]
        [Authorize]
        public async Task<JsonResult> NavBarElements()
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action, user {{@User}}", User.Identity.Name);

            List<NavBarElementModel> NavBarElements = new List<NavBarElementModel> { };
            long order = 1;

            // Add the Content element
            NavBarElements.Add(new NavBarElementModel
            {
                Order = order++,
                Label = "Content",
                URL = nameof(AuthorizedContentController).Replace("Controller", ""),
                View = "Content",
                Icon = "content-grid",
            });

            // Conditionally add the FileDrop element
            AuthorizationResult FileDropAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropAdmin));
            AuthorizationResult FileDropUserResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.FileDropUser));
            if (!FileDropAdminResult.Succeeded && FileDropUserResult.Succeeded)
            {
                TimeSpan expirationTime = TimeSpan.FromDays(_configuration.GetValue<int>("ClientReviewRenewalPeriodDays"));
                // Check whether an sftp account of the user is currently authorized to any File Drop for an authorized client
                List<Guid> authorizedClientIds = (await DbContext.UserRoleInClient
                                                                 .Where(urc => EF.Functions.ILike(urc.User.UserName, User.Identity.Name))
                                                                 .Where(urc => urc.Role.RoleEnum == RoleEnum.FileDropUser)
                                                                 .Select(urc => urc.Client)
                                                                 .ToListAsync())
                                                  .FindAll(c => DateTime.UtcNow.Date - c.LastAccessReview.LastReviewDateTimeUtc.Date <= expirationTime)
                                                  .ConvertAll(c => c.Id);
                if (!await DbContext.SftpAccount.AnyAsync(a => authorizedClientIds.Contains(a.FileDrop.Client.Id)
                                                      && (a.FileDropUserPermissionGroup.ReadAccess || 
                                                          a.FileDropUserPermissionGroup.WriteAccess ||
                                                          a.FileDropUserPermissionGroup.DeleteAccess)
                                                      && EF.Functions.ILike(a.ApplicationUser.UserName, User.Identity.Name))
                    || !authorizedClientIds.Any())
                {
                    FileDropUserResult = AuthorizationResult.Failed();
                }
            }

            if (FileDropAdminResult.Succeeded || FileDropUserResult.Succeeded)
            {
                NavBarElements.Add(new NavBarElementModel
                {
                    Order = order++,
                    Label = "File Drop",
                    URL = nameof(FileDropController).Replace("Controller", ""),
                    View = "FileDrop",
                    Icon = "file-drop",
                });
            }

            // Conditionally add the System Admin element
            AuthorizationResult SystemAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin));
            if (SystemAdminResult.Succeeded)
            {
                NavBarElements.Add(new NavBarElementModel
                {
                    Order = order++,
                    Label = "System Admin",
                    URL = nameof(SystemAdminController).Replace("Controller", ""),
                    View = "SystemAdmin",
                    Icon = "system-admin",
                });
            }

            // Conditionally add the Client Admin element
            AuthorizationResult ClientAdminResult1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            AuthorizationResult ClientAdminResult2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin));
            if (ClientAdminResult1.Succeeded || ClientAdminResult2.Succeeded)
            {
                NavBarElements.Add(new NavBarElementModel
                {
                    Order = order++,
                    Label = "Manage Clients",
                    URL = nameof(ClientAdminController).Replace("Controller", ""),
                    View = "ClientAdmin",
                    Icon = "client",
                });
            }

            // Conditionally add the Client Access Review element
            if (ClientAdminResult1.Succeeded)
            {
                List<Guid> myClientIds = (await DbContext.UserRoleInClient
                                                         .Where(urc => EF.Functions.ILike(urc.User.UserName, User.Identity.Name))
                                                         .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                                                         .Select(urc => urc.ClientId)
                                                         .Distinct()
                                                         .ToListAsync());
                DateTime countableLastReviewTime = DateTime.UtcNow
                                                    - TimeSpan.FromDays(_configuration.GetValue<int>("ClientReviewRenewalPeriodDays"))
                                                    + TimeSpan.FromDays(_configuration.GetValue<int>("ClientReviewEarlyWarningDays"));
                int numClientsDue = (await DbContext.Client
                                                    .Where(c => myClientIds.Contains(c.Id))
                                                    .Where(c => c.LastAccessReview.LastReviewDateTimeUtc < countableLastReviewTime)
                                                    .CountAsync());

                NavBarElements.Add(new NavBarElementModel
                {
                    Order = order++,
                    Label = "Review Client Access",
                    URL = nameof(ClientAccessReviewController).Replace("Controller", ""),
                    View = "ClientAccessReview",
                    Icon = "client-access-review",
                    BadgeNumber = numClientsDue,
                });
            }

            // Conditionally add the Content Publishing element
            AuthorizationResult ContentPublishResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentPublisher));
            if (ContentPublishResult.Succeeded)
            {
                NavBarElements.Add(new NavBarElementModel
                {
                    Order = order++,
                    Label = "Publish Content",
                    URL = nameof(ContentPublishingController).Replace("Controller", ""),
                    View = "ContentPublishing",
                    Icon = "content-publishing",
                });
            }

            // Conditionally add the Content Access element
            AuthorizationResult ContentAccessResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin));
            if (ContentAccessResult.Succeeded)
            {
                NavBarElements.Add(new NavBarElementModel
                {
                    Order = order++,
                    Label = "Manage Access",
                    URL = nameof(ContentAccessAdminController).Replace("Controller", ""),
                    View = "ContentAccessAdmin",
                    Icon = "content-access",
                });
            }

            // Add the Account Settings element
            NavBarElements.Add(new NavBarElementModel
            {
                Order = order++,
                Label = "Account Settings",
                URL = nameof(AccountController).Replace("Controller", "/Settings"),
                View = "AccountSettings",
                Icon = "user-settings",
            });

            Log.Verbose($"{ControllerContext.ActionDescriptor.DisplayName} action completed for user: {@User}, assigned elements: {{@Elements}}", User.Identity.Name, string.Join(", ", NavBarElements.Select(e => e.Label)));

            return Json(NavBarElements);
        }


        //
        // GET: /Account/LoginStepTwo
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> LoginStepTwo(string username = null, string returnUrl = null)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} GET action");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            #region validation
            if (user == null)
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName}, request for unknown user, or two factor user identity cookie was not provided");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (!user.UserName.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName}, provided user name does not match the account identity in the two factor cookie");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (!(await _userManager.GetValidTwoFactorProvidersAsync(user)).Contains(GlobalFunctions.TwoFactorEmailTokenProviderName))
            {
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName}, the required two factor token provider ({GlobalFunctions.TwoFactorEmailTokenProviderName}) is not available for user {user.UserName}");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var token = await _userManager.GenerateTwoFactorTokenAsync(user, GlobalFunctions.TwoFactorEmailTokenProviderName);

            // TODO Convert this to html, looking like the prototype
            string message =
                $"Your two factor authentication code for logging into Milliman Access Portal is:{Environment.NewLine}{Environment.NewLine}" +
                $"{token}{Environment.NewLine}{Environment.NewLine}" +
                $"This code will be valid for {_configuration.GetValue<int>("TwoFactorEmailTokenLifetimeMinutes")} minutes.";

            _messageSender.QueueEmail(user.Email, "Authentication Code", message);

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginStepTwoViewModel { Username = user.UserName, ReturnUrl = returnUrl });
        }

        //
        // POST: /Account/LoginStepTwo
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginStepTwo(LoginStepTwoViewModel model)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} POST action");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("UserMessage", new UserMessageModel("The submitted code is invalid. Codes are valid for 5 minutes. Please login again."));
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, model.Code, false, false);
            switch (result)
            {
                case var r when r.Succeeded:
                    // This gets logged during SignInCommon()
                    string scheme = await IsUserAccountLocal(user.UserName)
                        ? (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name
                        : GetExternalAuthenticationScheme(user.UserName).Name;
                    await SignInCommon(user, scheme);
                    Response.Headers.Add("NavigateTo", model.ReturnUrl ?? "/");
                    return Ok();
                case var r when r.IsLockedOut:
                    Log.Information($"User {user.UserName} account locked out while checking two factor code.");
                    Response.Headers.Add("NavigateTo", Url.Action(nameof(SharedController.UserMessage), nameof(SharedController).Replace("Controller", ""), new { Msg = "This account has been locked out, please try again later." }));
                    return Ok();
                case var r when r.IsNotAllowed:
                    Log.Information("User {user.UserName} account not allowed.");
                    Response.Headers.Add("NavigateTo", Url.Action(nameof(SharedController.UserMessage), nameof(SharedController).Replace("Controller", ""), new { Msg = "Login failed, please try again later." }));
                    return Ok();
                default:
                    Log.Information($"User {user.UserName} provided incorrect two-factor code.  Prompting again.");
                    Response.Headers.Add("Warning", $"The submitted code was incorrect, please try again.");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
        }

        //
        // GET /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");

            return View();
        }

        //
        // GET /Account/Settings
        [HttpGet]
        [Route("Account/Settings")]
        public async Task<IActionResult> AccountSettings()
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} GET action");

            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Log.Warning("AccountSettings action requested for invalid user {@User}, aborting", User.Identity.Name);
                return View("UserMessage", new UserMessageModel("User settings not found. Please contact support if this issue repeats."));
            }

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} GET action, returning view");

            return View();
        }

        [HttpGet]
        public async Task<ActionResult> AccountSettings2()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);

            return Json(new UserFullModel
            {
                Id = user.Id,
                IsActivated = user.EmailConfirmed,
                IsSuspended = user.IsSuspended,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                Employer = user.Employer,
                IsLocal = await IsUserAccountLocal(user.UserName),
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CheckPasswordValidity(CheckPasswordViewModel Model)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");

            var passwordValidationErrors = new List<string>();

            if (ModelState.IsValid)
            {
                ApplicationUser user = await _userManager.GetUserAsync(User);

                foreach (IPasswordValidator<ApplicationUser> passwordValidator in _userManager.PasswordValidators)
                {
                    IdentityResult result = await passwordValidator.ValidateAsync(_userManager, user, Model.ProposedPassword);

                    if (!result.Succeeded)
                    {
                        foreach (var errorResult in result.Errors)
                        {
                            passwordValidationErrors.Add(errorResult.Description);
                        }
                    }
                }
            }

            if (!passwordValidationErrors.Any())
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: proposed password is valid");
                return Ok();
            }
            else
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: proposed password not valid");
                string errorMessage = string.Join("<br /><br />", passwordValidationErrors);
                Response.Headers.Add("Warning", errorMessage);
                return StatusCode(StatusCodes.Status418ImATeapot);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CheckPasswordValidity2([FromBody] CheckPasswordViewModel Model)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");

            var passwordValidationErrors = new List<string>();

            if (ModelState.IsValid)
            {
                ApplicationUser user = await _userManager.GetUserAsync(User);

                foreach (IPasswordValidator<ApplicationUser> passwordValidator in _userManager.PasswordValidators)
                {
                    IdentityResult result = await passwordValidator.ValidateAsync(_userManager, user, Model.ProposedPassword);

                    if (!result.Succeeded)
                    {
                        foreach (var errorResult in result.Errors)
                        {
                            passwordValidationErrors.Add(errorResult.Description);
                        }
                    }
                }
            }

            if (!passwordValidationErrors.Any())
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: proposed password is valid");
                return Json(new PasswordValidationModel { Valid = true });
            }
            else
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: proposed password not valid");
                return Json(new PasswordValidationModel { Valid = false, Messages = passwordValidationErrors });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountModel model)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName} POST action: user {User.Identity.Name} not found, aborting");
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(model.User.FirstName) ||
                string.IsNullOrWhiteSpace(model.User.LastName) ||
                string.IsNullOrWhiteSpace(model.User.Employer) ||
                string.IsNullOrWhiteSpace(model.User.Phone)
            )
            {
                Log.Information($"{ControllerContext.ActionDescriptor.DisplayName}, {model} does not contain all required field.");
                Response.Headers.Add("Warning", "All account fields are required.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            DbContext.Attach(user);
            using (var txn = await DbContext.Database.BeginTransactionAsync())
            {
                if (model.User != null)
                {
                    user.FirstName = model.User.FirstName;
                    user.LastName = model.User.LastName;
                    user.PhoneNumber = model.User.Phone;
                    user.Employer = model.User.Employer;
                }

                await DbContext.SaveChangesAsync();
                txn.Commit();
            }

            return Json(new UserFullModel
            {
                Id = user.Id,
                IsActivated = user.EmailConfirmed,
                IsSuspended = user.IsSuspended,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                Employer = user.Employer,
                IsLocal = await IsUserAccountLocal(user.UserName),
            });
        }

        [HttpGet]
        [PreventAuthRefresh]
        public IActionResult SessionStatus()
        {
            return Json(new { });
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(AccountController.Login), "Account");
            }
        }

        #endregion
    }
}
