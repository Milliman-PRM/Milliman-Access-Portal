/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Actions related to user account management
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Services;
using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Services;
using AuditLogLib.Models;
using MillimanAccessPortal.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MapCommonLib;
using MapCommonLib.ActionFilters;
using Serilog;

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
        private readonly StandardQueries Queries;
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
            StandardQueries QueriesArg,
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
            Queries = QueriesArg;
            AuthorizationService = AuthorizationServiceArg;
            _configuration = ConfigArg;
            _serviceProvider = serviceProviderArg;
            _authentService = (AuthenticationService)authentService;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        [LogTiming]
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
            MapDbContextLib.Context.AuthenticationScheme matchingScheme = DbContext.AuthenticationScheme.SingleOrDefault(s => s.DomainListContains(userFullDomain));
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
        public async Task<IActionResult> RemoteAuthenticate(string userName)
        {
            MapDbContextLib.Context.AuthenticationScheme scheme = GetExternalAuthenticationScheme(userName);

            if (scheme != null && scheme.Name != (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name)
            {
                string redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { ReturnUrl = "/AuthorizedContent/Index" });
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
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            Log.Verbose("Entered AccountController.Login action");

            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);

                if (user == null)
                {
                    Log.Debug($"User {model.Username} not found, local login rejected");
                    _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(model.Username, (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name));
                    Response.Headers.Add("Warning", "Invalid login attempt.");
                    return Ok();
                }

                if (user.IsSuspended)
                {
                    _auditLogger.Log(AuditEventType.LoginIsSuspended.ToEvent(user.UserName));
                    Log.Debug($"User {user.UserName} suspended, local login rejected");

                    Response.Headers.Add("Warning", "This account is currently suspended.  Please contact your Milliman consultant, or email map.support@milliman.com");
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

                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    SignInCommon(model.Username, (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name);

                    // Provide the location that should be navigated to (or fall back on default route)
                    Response.Headers.Add("NavigateTo", string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
                    return Ok();
                }
                else
                {
                    var lockoutMessage = "This account has been locked out, please try again later.";
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    }
                    else if (result.IsLockedOut)
                    {
                        ModelState.AddModelError(string.Empty, "User account is locked out.");
                        Log.Information($"User {model.Username} account locked out");
                        _auditLogger.Log(AuditEventType.LoginIsLockedOut.ToEvent(), model.Username);
                        Response.Headers.Add("Warning", lockoutMessage);
                        return Ok();
                    }
                    else
                    {
                        // Log differently, but return the same model
                        if (result.IsNotAllowed)
                        {
                            Log.Information($"User {model.Username} login not allowed");
                            _auditLogger.Log(AuditEventType.LoginNotAllowed.ToEvent(), model.Username);
                        }
                        else
                        {
                            Log.Information($"User {model.Username} PasswordSignInAsync did not succeed");
                            _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(model.Username, (await _authentService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name));
                        }
                        Response.Headers.Add("Warning", "Invalid login attempt.");
                        return Ok();
                    }
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
        private void SignInCommon(string userName, string scheme)
        {
            HttpContext.Session.SetString("SessionId", HttpContext.Session.Id);
            Log.Information($"User {userName} logged in with scheme {scheme}");
            _auditLogger.Log(AuditEventType.LoginSuccess.ToEvent(scheme), userName, HttpContext.Session.Id);
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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineUserAgreement()
        {
            ApplicationUser user = await _userManager.FindByNameAsync(User.Identity.Name);

            _auditLogger.Log(AuditEventType.UserAgreementDeclined.ToEvent(), user.UserName);

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

            _auditLogger.Log(AuditEventType.UserAgreementAcceptance.ToEvent((UserAgreementLogModel)model), user.UserName);

            Response.Headers.Add("NavigateTo", string.IsNullOrEmpty(model.ReturnUrl) ? "/" : model.ReturnUrl);
            return Ok();
        }

        //
        // GET: /Account/CreateInitialUser
        [HttpGet]
        [AllowAnonymous]
        public IActionResult CreateInitialUser(string returnUrl = null)
        {
            Log.Verbose("Entered AccountController.CreateInitialUser action");

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
            Log.Verbose("Entered AccountController.CreateInitialUser action with {@CreateInitialUserViewModel}", model);

            IdentityResult createUserResult = null;
            IdentityResult roleGrantResult = null;

            // If any users exist, return 404. We don't want to even hint that this URL is valid.
            if (_userManager.Users.Any())
            {
                Log.Debug($"CreateInitialUser unsuccessful, user(s) already exist");
                return NotFound();
            }

            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                ApplicationUser newUser = new ApplicationUser { UserName = model.Email, Email = model.Email };
                ApplicationRole adminRole = await _roleManager.FindByNameAsync(RoleEnum.Admin.ToString());

                using (var txn = DbContext.Database.BeginTransaction())
                {
                    createUserResult = await _userManager.CreateAsync(newUser);
                    roleGrantResult = await _userManager.AddToRoleAsync(newUser, adminRole.Name);

                    if (createUserResult.Succeeded && roleGrantResult.Succeeded)
                    {
                        txn.Commit();

                        Log.Information($"Initial user {model.Email} account created new with password.");
                        _auditLogger.Log(AuditEventType.UserAccountCreated.ToEvent(newUser));
                        _auditLogger.Log(AuditEventType.SystemRoleAssigned.ToEvent(newUser, RoleEnum.Admin));

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
            Log.Verbose("Entered AccountController.Logout action");
            ApplicationUser appUser = null;
            try
            {
                appUser = await Queries.GetCurrentApplicationUser(User);
            }
            catch (Exception ex)
            {
                var x = ex;
            }
            await _signInManager.SignOutAsync();

            Log.Verbose($"In AccountController.Logout action: user {appUser?.UserName ?? "<unknown>"} logged out.");
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
            Log.Verbose("Entered AccountController.ExternalLogin action with {@Provider}", provider);

            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            Log.Verbose("Entered AccountController.ExternalLoginCallback action");

            if (string.IsNullOrWhiteSpace(HttpContext.User?.Identity?.Name) ||
                !HttpContext.User.Identity.IsAuthenticated)
            {
                Log.Warning("AccountController.ExternalLoginCallback action invoked with {@HttpContextUser}", HttpContext.User);
                return RedirectToAction(nameof(Login));
            }

            if (remoteError != null)
            {
                Log.Error("Error during remote authentication");
                return RedirectToAction(nameof(Login));
            }

            SignInCommon(HttpContext.User.Identity.Name, GetExternalAuthenticationScheme(HttpContext.User.Identity.Name)?.Name);

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
            Log.Verbose("Entered AccountController.ExternalLoginConfirmatino action with {@ExternalLoginConfirmationViewModel}", model);

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

            string accountActivationDays = _configuration["AccountActivationTokenTimespanDays"] ?? GlobalFunctions.fallbackAccountActivationTokenTimespanDays.ToString();

            // Non-configurable portion of email body
            emailBody += $"Your username is: {RequestedUser.UserName}{Environment.NewLine}{Environment.NewLine}" +
                $"Activate your account by clicking the link below or copying and pasting the link into your web browser.{Environment.NewLine}{Environment.NewLine}" +
                $"{emailLink.Uri.AbsoluteUri}{Environment.NewLine}{Environment.NewLine}" +
                $"This link will expire in {accountActivationDays} days.{Environment.NewLine}{Environment.NewLine}" +
                $"Once you have activated your account, MAP can be accessed at {rootSiteUrl.Uri.AbsoluteUri}{Environment.NewLine}{Environment.NewLine}" +
                $"If you have any questions regarding this email, please contact map.support@milliman.com";
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
            Log.Verbose("Entered AccountController.RequestPasswordReset action with {@UserName}", RequestedUser.UserName);

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

                string expirationHours = _configuration["PasswordResetTokenTimespanHours"] ?? GlobalFunctions.fallbackPasswordResetTokenTimespanHours.ToString();

                emailBody = $"A password reset was requested for your Milliman Access Portal account.  Please create a new password at the below linked page. This link will expire in {expirationHours} hours. {Environment.NewLine}";
                emailBody += $"Your user name is {RequestedUser.UserName}{Environment.NewLine}{Environment.NewLine}";
                emailBody += $"{link.Uri.AbsoluteUri}";

                appLogMsg = $"Password reset email queued to address {RequestedUser.Email}{Environment.NewLine}reason: {reason.GetDisplayValueString()}{Environment.NewLine}emailed link: {link.Uri.AbsoluteUri}";
            }
            else
            {
                MapDbContextLib.Context.AuthenticationScheme authScheme = GetExternalAuthenticationScheme(RequestedUser.UserName);

                emailBody = "A password reset was requested for your Milliman Access Portal account. " +
                    $"Your MAP account uses login services from your organization ({authScheme.DisplayName}). Please contact your IT department if you require password assistance.";

                appLogMsg = $"Password reset was requested for an externally authenticated user with email {RequestedUser.Email}. Information email was queued. No other action taken.";
            }

            _messageSender.QueueEmail(RequestedUser.Email, "MAP password reset", emailBody);
            Log.Information(appLogMsg);
            _auditLogger.Log(AuditEventType.PasswordResetRequested.ToEvent(RequestedUser, reason));
        }


        // GET: /Account/EnableAccount
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> EnableAccount(string userId, string code)
        {
            Log.Verbose("Entered AccountController.EnableAccount GET action with {@UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                Log.Debug("In AccountController.EnableAccount GET action: invalid argument(s), aborting");
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error"));
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                Log.Debug($"In AccountController.EnableAccount GET action: user {userId} not found, aborting");
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error"));
            }

            if (user.EmailConfirmed)  // Account is already activated
            {
                Log.Debug($"In AccountController.EnableAccount GET action: user {userId} account is already enabled, aborting");
                return RedirectToAction(nameof(Login));
            }

            // If the code is not valid (likely expired), re-send the welcome email and notify the user
            DataProtectorTokenProvider<ApplicationUser> emailConfirmationTokenProvider = (DataProtectorTokenProvider<ApplicationUser>) _serviceProvider.GetService(typeof(DataProtectorTokenProvider<ApplicationUser>));
            bool tokenIsValid = await emailConfirmationTokenProvider.ValidateAsync("EmailConfirmation", code, _userManager, user);

            if (!tokenIsValid)
            {
                string WelcomeText = _configuration["Global:DefaultNewUserWelcomeText"];  // could be null, that's ok
                Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, WelcomeText));

                string WhatHappenedMessage = "Your previous account activation link is invalid or may have expired. A new welcome email has been sent, which contains a new account activation link.";
                Log.Information($"In AccountController.EnableAccount GET action: confirmation token is invalid for user name {user.UserName}, may be expired, new welcome email sent, aborting");
                return View("Message", WhatHappenedMessage);
            }

            // Prompt for the user's profile data
            var model = new EnableAccountViewModel
            {
                Id = user.Id,
                Code = code,
                Username = user.UserName,
                IsLocalAccount = await IsUserAccountLocal(user.UserName),
            };
            Log.Verbose($"In AccountController.EnableAccount GET action: complete");
            return View(model);
        }

        // POST: /Account/EnableAccount
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAccount(EnableAccountViewModel model)
        {
            Log.Verbose("Entered AccountController.EnableAccount POST action with {@UserName}", model.Username);

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
                Log.Debug($"In AccountController.EnableAccount POST action: user {model.Id} no found, aborting");
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error"));
            }

            if (user.EmailConfirmed)  // Account is already activated
            {
                Log.Debug($"In AccountController.EnableAccount POST action: user {model.Id} account is already activated, aborting");
                return RedirectToAction(nameof(Login));
            }

            using (var Txn = DbContext.Database.BeginTransaction())
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
                        return View("Message", WhatHappenedMessage);
                    }
                    else
                    {
                        string confirmEmailErrors = $"Error while confirming account: {string.Join($", ", confirmEmailResult.Errors.Select(e => e.Description))}";
                        Response.Headers.Add("Warning", confirmEmailErrors);

                        Log.Error($"EnableAccount failed from _userManager.ConfirmEmailAsync(user, model.Code), not due to token expiration: user {user.UserName}, errors: {confirmEmailErrors}");

                        return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error"));
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

                        return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error"));
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

                        return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error"));
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

                    return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error"));
                }

                Txn.Commit();

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
            Log.Verbose("Entered AccountController.ForgotPassword GET action");

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
            Log.Verbose("Entered AccountController.ForgotPassword post action with {@ForgotPasswordViewModel}", model);

            // Sends an email with password reset link to the requested user
            // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (await _userManager.IsEmailConfirmedAsync(user))
                    {
                        await RequestPasswordReset(user, PasswordResetRequestReason.UserInitiated, Request.Scheme, Request.Host);
                        Log.Verbose($"In AccountController.ForgotPassword post action: user email address <{model.Email}> reset succeeded");
                    }
                    else
                    {
                        string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                        Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, EmailBodyText));
                        Log.Debug($"In AccountController.ForgotPassword post action: unconfirmed user email address <{model.Email}> requested, welcome email sent.");
                    }
                }
                else
                {
                    Log.Debug($"In AccountController.ForgotPassword post action: user email address <{model.Email}> not found");
                    _auditLogger.Log(AuditEventType.PasswordResetRequestedForInvalidEmail.ToEvent(model.Email));
                }
            }

            Log.Verbose("In AccountController.ForgotPassword post action: success");

            var passwordConfirmationMessage = "Please check your email inbox for a password reset notification.";
            return View("Message", passwordConfirmationMessage);
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userEmail, string passwordResetToken)
        {
            Log.Information("Entered AccountController.ResetPassword GET action with query string {@QueryString},", Request.QueryString);

            ApplicationUser user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                Log.Debug($"In AccountController.ResetPassword GET action: user <{userEmail}> not found, aborting");
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Password Reset Error"));
            }

            PasswordResetSecurityTokenProvider<ApplicationUser> passwordResetTokenProvider = (PasswordResetSecurityTokenProvider<ApplicationUser>)_serviceProvider.GetService(typeof(PasswordResetSecurityTokenProvider<ApplicationUser>));
            bool tokenIsValid = await passwordResetTokenProvider.ValidateAsync("ResetPassword", passwordResetToken, _userManager, user);

            if (!tokenIsValid)
            {
                string UserMsg = "";
                if (await _userManager.IsEmailConfirmedAsync(user))
                {
                    await RequestPasswordReset(user, PasswordResetRequestReason.PasswordResetTokenInvalid, Request.Scheme, Request.Host);

                    Log.Debug($"ResetPassword GET action requested for user {user.UserName} having expired or invalid reset token, new password reset email sent");
                    UserMsg = "Your password reset link is invalid or expired.  A new password reset email is being sent to you now.  Please use the link in that email to reset your password.";
                }
                else
                {
                    string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                    Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, EmailBodyText));

                    Log.Debug($"ResetPassword GET action requested for user {user.UserName} with expired password reset token, new password reset email sent");
                    UserMsg = "Your Milliman Access Portal account has not yet been activated.  A new account welcome email is being sent to you now.  Please use the link in that email to activate your account.";
                }
                return View("Message", UserMsg);
            }

            ResetPasswordViewModel model = new ResetPasswordViewModel
            {
                Email = user.Email,
                PasswordResetToken = passwordResetToken,
            };

            Log.Verbose("In AccountController.ResetPassword GET action: success");

            return View(model);
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            Log.Verbose("Entered AccountController.ResetPassword POST action for user {@UserEmail}", model.Email);

            if (!ModelState.IsValid)
            {
                Log.Debug($"In AccountController.ResetPassword POST action: invalid ModelState, errors {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)))}, aborting");
                model.Message = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
                return View(model);
            }
            var passwordResetErrorMessage = "An error occurred. Please try again. If the issue persists, please contact <a href=\"mailto:map.support@milliman.com\">MAP.Support@Milliman.com</a>.";
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                Log.Debug($"In AccountController.ResetPassword POST action: requested user with email {model.Email} not found, current user will not be informed of the issue, aborting");
                return View("Message", passwordResetErrorMessage);
            }
            using (var Txn = DbContext.Database.BeginTransaction())
            {
                var result = await _userManager.ResetPasswordAsync(user, model.PasswordResetToken, model.NewPassword);
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
                        Log.Error($"In AccountController.ResetPassword POST action: Failed to save password history for {user.UserName}, ResetPassword action rolled back, aborting");
                    }

                    if (!unlock.Succeeded)
                    {
                        Log.Error($"In AccountController.ResetPassword POST action: Failed to unlock account for {user.UserName}, ResetPassword action rolled back, aborting");
                    }

                    if (!resetFailedCount.Succeeded)
                    {
                        Log.Error($"In AccountController.ResetPassword POST action: Failed to reset failed login attempt count for {user.UserName}, ResetPassword action rolled back, aborting");
                    }

                    if (unlock.Succeeded && resetFailedCount.Succeeded && addHistoryResult.Succeeded)
                    {
                        Txn.Commit();
                        Log.Debug($"In AccountController.ResetPassword POST action: succeeded for user {user.UserName }");
                        _auditLogger.Log(AuditEventType.PasswordResetCompleted.ToEvent(user));
                        return View("Message", "Your password has been reset. <a href=\"/Account/Login\">Click here to log in</a>.");
                    }
                    else
                    {
                        return View("Message", passwordResetErrorMessage);
                    }

                }
                else if (result.Errors.Any(e => e.Code == "InvalidToken"))  // Could be expired, mismatched security stamp, etc. The reason is not accessible here.
                {
                    string UserMsg = "";
                    if (await _userManager.IsEmailConfirmedAsync(user))
                    {
                        await RequestPasswordReset(user, PasswordResetRequestReason.PasswordResetTokenInvalid, Request.Scheme, Request.Host);
                        Log.Debug($"In AccountController.ResetPassword POST action: for user {user.UserName}, expired reset token, new password reset email sent, aborting");
                        UserMsg = "Your password reset link is invalid or expired.  A new password reset email is being sent to you now.  Please use the link in that email to reset your password.";
                    }
                    else
                    {
                        string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                        Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Request.Scheme, Request.Host, EmailBodyText));

                        Log.Debug($"In AccountController.ResetPassword POST action: for user {user.UserName}, the account is not enabled, new welcome email sent, aborting");
                        UserMsg = "Your Milliman Access Portal account has not yet been activated.  A new account welcome email is being sent to you now.  Please use the link in that email to activate your account.";
                    }
                    return View("Message", UserMsg);
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
            Log.Verbose("Entered AccountController.NavBarElements action, user {@User}", User.Identity.Name);

            List<NavBarElementModel> NavBarElements = new List<NavBarElementModel> { };
            long order = 1;

            // Add the Content Element
            NavBarElements.Add(new NavBarElementModel
            {
                Order = order++,
                Label = "Content",
                URL = nameof(AuthorizedContentController).Replace("Controller", ""),
                View = "Content",
                Icon = "content-grid",
            });

            // Conditionally add the System Admin Element
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

            // Conditionally add the Client Admin Element
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

            // Conditionally add the Content Publishing Element
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

            // Conditionally add the Content Access Element
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

            // Add the Account Settings Element
            NavBarElements.Add(new NavBarElementModel
            {
                Order = order++,
                Label = "Account Settings",
                URL = nameof(AccountController).Replace("Controller", "/Settings"),
                View = "AccountSettings",
                Icon = "user-settings",
            });

            Log.Verbose("AccountController.NavBarElements action completed for user: {@User}, assigned elements: {@Elements}", User.Identity.Name, string.Join(", ", NavBarElements.Select(e => e.Label)));

            return Json(NavBarElements);
        }

        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            Log.Verbose("Entered AccountController.SendCode GET action");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Two Factor Error"));
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            Log.Verbose("Entered AccountController.SendCode POST action");

            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Two Factor Error"));
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Two Factor Error"));
            }

            var message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                _messageSender.QueueEmail(await _userManager.GetEmailAsync(user), "Security Code", message);
            }
            else if (model.SelectedProvider == "Phone")
            {
                _messageSender.QueueSms(await _userManager.GetPhoneNumberAsync(user), message);
            }

            return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            Log.Verbose("Entered AccountController.VerifyCode GET action");

            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Two Factor Verification Error"));
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            Log.Verbose("Entered AccountController.VerifyCode POST action");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                Log.Debug("User account locked out.");
                var lockoutMessage = "This account has been locked out, please try again later.";
                return View("Message", lockoutMessage);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }

        //
        // GET /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            Log.Verbose("Entered AccountController.AccessDenied action");

            return View();
        }

        //
        // GET /Account/Settings
        [HttpGet]
        [Route("Account/Settings")]
        public async Task<IActionResult> AccountSettings()
        {
            Log.Verbose("Entered AccountController.AccountSettings GET action");

            ApplicationUser user = await Queries.GetCurrentApplicationUser(User);
            if (user == null)
            {
                Log.Warning("AccountSettings action requested for invalid user {@User}, aborting", User.Identity.Name);
                return View("Message", $"User settings not found. Please contact support if this issue repeats.");
            }

            Log.Verbose("In AccountController.AccountSettings GET action, returning view");

            return View();
        }

        [HttpGet]
        public async Task<ActionResult> AccountSettings2()
        {
            ApplicationUser user = await Queries.GetCurrentApplicationUser(User);

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
            Log.Verbose("Entered AccountController.CheckPasswordValidity action");

            var passwordValidationErrors = new List<string>();

            if (ModelState.IsValid)
            {
                ApplicationUser user = await Queries.GetCurrentApplicationUser(User);

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
                Log.Verbose("In AccountController.CheckPasswordValidity action: proposed password is valid");
                return Ok();
            }
            else
            {
                Log.Verbose("In AccountController.CheckPasswordValidity action: proposed password not valid");
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
            Log.Verbose("Entered AccountController.CheckPasswordValidity2 action");

            var passwordValidationErrors = new List<string>();

            if (ModelState.IsValid)
            {
                ApplicationUser user = await Queries.GetCurrentApplicationUser(User);

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
                Log.Verbose("In AccountController.CheckPasswordValidity action: proposed password is valid");
                return Json(new PasswordValidationModel { Valid = true });
            }
            else
            {
                Log.Verbose("In AccountController.CheckPasswordValidity action: proposed password not valid");
                return Json(new PasswordValidationModel { Valid = false, Messages = passwordValidationErrors });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountModel model)
        {
            ApplicationUser user = await Queries.GetCurrentApplicationUser(User);
            if (user == null)
            {
                Log.Debug("In AccountController.UpdateAccount POST action: "
                       + $"user {User.Identity.Name} not found, aborting");
                return BadRequest();
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
                if (model.Password != null)
                {
                    bool currentPasswordIsCorrect = await _userManager.CheckPasswordAsync(user, model.Password.Current);
                    if (!currentPasswordIsCorrect)
                    {
                        Log.Debug("In AccountController.UpdateAccount POST action: "
                               + $"user {User.Identity.Name} Current Password incorrect");
                        Response.Headers.Add("warning", "The Current Password provided was incorrect");
                        return BadRequest();
                    }

                    if (model.Password.New != model.Password.Confirm)
                    {
                        Log.Debug("In AccountController.UpdateAccount POST action: "
                               + $"user {User.Identity.Name} New Password != Password");
                        Response.Headers.Add("warning", "New Password and Confirm Password must match");
                        return BadRequest();
                    }

                    IdentityResult result = await _userManager
                        .ChangePasswordAsync(user, model.Password.Current, model.Password.New);

                    if (!result.Succeeded)
                    {
                        Log.Warning("Failed to change password " +
                                   $"for user {user.UserName}, aborting");
                        return BadRequest();
                    }

                    // Save password hash in history
                    user.PasswordHistoryObj = user.PasswordHistoryObj
                        .Append(new PreviousPassword(model.Password.New)).ToList();
                    user.LastPasswordChangeDateTimeUtc = DateTime.UtcNow;
                    var addHistoryResult = await _userManager.UpdateAsync(user);

                    if (!addHistoryResult.Succeeded)
                    {
                        Log.Warning("Failed to save password history or update password timestamp " +
                                   $"for user {user.UserName}, aborting");
                        return BadRequest();
                    }
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
        [LogTiming]
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
