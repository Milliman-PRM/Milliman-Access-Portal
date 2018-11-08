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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Services;
using AuditLogLib;
using AuditLogLib.Services;
using AuditLogLib.Event;
using MillimanAccessPortal.Authorization;
using Microsoft.Extensions.Configuration;
using MapCommonLib;
using MapCommonLib.ActionFilters;
using Serilog;

namespace MillimanAccessPortal.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        readonly string TwoNewLines = Environment.NewLine + Environment.NewLine;

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
            IServiceProvider serviceProviderArg)
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
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
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

                if (user == null || user.IsSuspended)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    Log.Debug($"User {model.Username} suspended or not found, login rejected");
                    _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(model.Username));
                    return View(model);
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
                    await SendPasswordResetEmail(user, Url);

                    Log.Information($"User {model.Username} password is expired, sent password reset email");
                    _auditLogger.Log(AuditEventType.PasswordResetRequested.ToEvent(user));
                    string WhatHappenedMessage = "Your password has expired. Check your email for a link to reset your password.";
                    return View("Message", WhatHappenedMessage);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    HttpContext.Session.SetString("SessionId", HttpContext.Session.Id);

                    Log.Information($"User {model.Username} logged in");
                    _auditLogger.Log(AuditEventType.LoginSuccess.ToEvent(), model.Username);

                    // The default route is /AuthorizedContent/Index as configured in startup.cs
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction(nameof(AuthorizedContentController.Index), nameof(AuthorizedContentController).Replace("Controller", ""));
                    }
                }
                else
                {
                    var lockoutMessage = "This account has been locked out, please try again later.";
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    }
                    else if (result.IsNotAllowed)
                    {
                        ModelState.AddModelError(string.Empty, "User login is not allowed.");
                        Log.Information($"User {model.Username} login not allowed");
                        _auditLogger.Log(AuditEventType.LoginNotAllowed.ToEvent(), model.Username);
                        return View("Message", lockoutMessage);
                    }
                    else if (result.IsLockedOut)
                    {
                        ModelState.AddModelError(string.Empty, "User account is locked out.");
                        Log.Information($"User {model.Username} account locked out");
                        _auditLogger.Log(AuditEventType.LoginIsLockedOut.ToEvent(), model.Username);
                        return View("Message", lockoutMessage);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        Log.Information($"User {model.Username} PasswordSignInAsync did not succeed");
                        _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(model.Username));
                        return View(model);
                    }
                }

            }

            // If we got this far, something failed, redisplay form
            return View(model);
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
                        await SendNewAccountWelcomeEmail(newUser, Url, welcomeText);

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

            ApplicationUser appUser = await Queries.GetCurrentApplicationUser(User);
            await _signInManager.SignOutAsync();

            Log.Verbose($"In AccountController.Logout action: user {appUser?.UserName ?? "<unknown>"} logged out.");
            _auditLogger.Log(AuditEventType.Logout.ToEvent(), appUser?.UserName);

            Response.Cookies.Delete(".AspNetCore.Session");
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
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            Log.Verbose("Entered AccountController.ExternalLoginCallback action");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                Log.Information($"User logged in with provider {info.LoginProvider}");
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                Log.Information($"From ExternalLoginCallback, ExternalLoginSignInAsync result is LockedOut from provider {info.LoginProvider}");
                var lockoutMessage = "This account has been locked out, please try again later.";
                return View("Message", lockoutMessage);
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RequestedUser"></param>
        /// <param name="Url"></param>
        /// <param name="SettableEmailText">Do not include trailing line feeds</param>
        /// <returns></returns>
        [NonAction]
        public async Task SendNewAccountWelcomeEmail(ApplicationUser RequestedUser, IUrlHelper Url, string SettableEmailText = null)
        {
            Log.Verbose("Entered AccountController.SendNewAccountWelcomeEmail action with {@UserName}", RequestedUser.UserName);

            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(RequestedUser);
            Log.Verbose("In AccountController.SendNewAccountWelcomeEmail action: EmailConfirmationToken created: {@Token}", emailConfirmationToken);
            var callbackUrl = Url.Action(nameof(AccountController.EnableAccount), "Account", new { userId = RequestedUser.Id, code = emailConfirmationToken }, protocol: "https");
            Log.Verbose("In AccountController.SendNewAccountWelcomeEmail action: Url created: {@url}", callbackUrl);

            // Configurable portion of email body
            string emailBody = string.IsNullOrWhiteSpace(SettableEmailText)
                ? string.Empty
                : SettableEmailText + $"{TwoNewLines}";

            string accountActivationDays = _configuration["AccountActivationTokenTimespanDays"] ?? GlobalFunctions.fallbackAccountActivationTokenTimespanDays.ToString();

            // Non-configurable portion of email body
            emailBody += $"Your username is: {RequestedUser.UserName}{TwoNewLines}" +
                         $"Activate your account by clicking the link below or copying and pasting the link into your web browser.{TwoNewLines}" +
                         callbackUrl + TwoNewLines +
                         $"This link will expire in {accountActivationDays} days.{TwoNewLines}"+
                         "If you have any questions regarding this email, please contact map.support@milliman.com";
            string emailSubject = "Welcome to Milliman Access Portal!";
            // Send welcome email
            _messageSender.QueueEmail(RequestedUser.Email, emailSubject, emailBody /*, optional senderAddress, optional senderName*/);

            Log.Debug($"Welcome email queued to email address {RequestedUser.Email}");
        }

        [NonAction]
        public async Task SendPasswordResetEmail(ApplicationUser RequestedUser, IUrlHelper Url)
        {
            Log.Verbose("Entered AccountController.SendPasswordResetEmail action with {@UserName}", RequestedUser.UserName);

            string PasswordResetToken = await _userManager.GeneratePasswordResetTokenAsync(RequestedUser);
            string linkUrl = Url.Action(nameof(ResetPassword), "Account", new { userEmail = RequestedUser.Email, passwordResetToken = PasswordResetToken }, protocol: "https");

            string expirationHours = _configuration["PasswordResetTokenTimespanHours"] ?? GlobalFunctions.fallbackPasswordResetTokenTimespanHours.ToString();

            string emailBody = $"A password reset was requested for your Milliman Access Portal account.  Please create a new password at the below linked page. This link will expire in {expirationHours} hours. {Environment.NewLine}";
            emailBody += $"Your user name is {RequestedUser.UserName}{TwoNewLines}";
            emailBody += $"{linkUrl}";
            _messageSender.QueueEmail(RequestedUser.Email, "MAP password reset", emailBody);

            Log.Debug($"Password reset email queued to email {RequestedUser.Email}");
            _auditLogger.Log(AuditEventType.PasswordResetRequested.ToEvent(RequestedUser));
        }
        

        // GET: /Account/EnableAccount
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> EnableAccount(string userId, string code)
        {
            Log.Verbose("Entered AccountController.EnableAccount GET action with {@UserId}", userId);
            Log.Debug("In AccountController.EnableAccount GET action: Token received: {@Code}", code);

            if (userId == null || code == null)
            {
                Log.Debug("In AccountController.EnableAccount GET action: a null argument is invalid, aborting");
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
                return View("Login");
            }

            // If the code is not valid (likely expired), re-send the welcome email and notify the user
            DataProtectorTokenProvider<ApplicationUser> emailConfirmationTokenProvider = (DataProtectorTokenProvider<ApplicationUser>) _serviceProvider.GetService(typeof(DataProtectorTokenProvider<ApplicationUser>));
            bool tokenIsValid = await emailConfirmationTokenProvider.ValidateAsync("EmailConfirmation", code, _userManager, user);

            if (!tokenIsValid)
            {
                string WelcomeText = _configuration["Global:DefaultNewUserWelcomeText"];  // could be null, that's ok
                Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Url, WelcomeText));

                string WhatHappenedMessage = "Your previous account activation link is invalid or may have expired. A new welcome email has been sent, which contains a new account activation link.";
                Log.Debug($"In AccountController.EnableAccount GET action: confirmation token is invalid for user name {user.UserName}, may be expired, new welcome email sent, aborting");
                return View("Message", WhatHappenedMessage);
            }

            // Prompt for the user's password
            var model = new EnableAccountViewModel
            {
                Id = user.Id,
                Code = code,
                Username = user.UserName,
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
            Log.Verbose("Entered AccountController.EnableAccount POST action for user {@UserName}", model.Username);

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user == null)
            {
                Log.Debug($"In AccountController.EnableAccount POST action: user {model.Id} not found, aborting");
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Account Activation Error"));
            }

            if (user.EmailConfirmed)  // Account is already activated
            {
                Log.Debug($"In AccountController.EnableAccount POST action: user {model.Id} account is already activated, aborting");
                return View("Login");
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
                        Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Url, WelcomeText));

                        Log.Information($"EnableAccount failed for user {model.Username} with code 'InvalidToken', it is likely that the token the is expired, new welcome email sent");
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

                return View("Login");
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
                        await SendPasswordResetEmail(user, Url);
                        Log.Verbose($"In AccountController.ForgotPassword post action: user email address <{model.Email}> reset succeeded");
                    }
                    else
                    {
                        string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                        Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Url, EmailBodyText));
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
            Log.Verbose("Entered AccountController.ResetPassword GET action with {@UserEmail}", userEmail);

            ApplicationUser user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                Log.Debug($"In AccountController.ResetPassword GET action: user <{userEmail}> not found, aborting");
                return View("Message", GlobalFunctions.GenerateErrorMessage(_configuration, "Password Reset Error"));
            }

            DataProtectorTokenProvider<ApplicationUser> passwordResetTokenProvider = (DataProtectorTokenProvider<ApplicationUser>)_serviceProvider.GetService(typeof(DataProtectorTokenProvider<ApplicationUser>));
            bool tokenIsValid = await passwordResetTokenProvider.ValidateAsync("ResetPassword", passwordResetToken, _userManager, user);

            if (!tokenIsValid)
            {
                string UserMsg = "";
                if (await _userManager.IsEmailConfirmedAsync(user))
                {
                    await SendPasswordResetEmail(user, Url);

                    Log.Debug($"ResetPassword GET action requested for user {user.UserName} having expired token, new password reset email sent");
                    UserMsg = "Your password reset link has expired.  A new password reset email is being sent to you now.  Please use the link in that email to reset your password.";
                }
                else
                {
                    string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                    Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Url, EmailBodyText));

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
            var passwordResetMessage = "Your password has been reset. <a href=\"/Account/Login\">Click here to log in</a>.";
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                Log.Debug($"In AccountController.ResetPassword POST action: requested user with email {model.Email} not found, current user will not be informed of the issue, aborting");
                return View("Message", passwordResetMessage);
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

                    if (addHistoryResult.Succeeded)
                    {
                        Txn.Commit();
                        Log.Debug($"In AccountController.ResetPassword POST action: succeeded for user {user.UserName }");
                        _auditLogger.Log(AuditEventType.PasswordResetCompleted.ToEvent(user));
                    }
                    else
                    {
                        Log.Error($"In AccountController.ResetPassword POST action: Failed to save password history for {user.UserName}, ResetPassword action rolled back, aborting");
                    }

                    return View("Message", passwordResetMessage);
                }
                else if (result.Errors.Any(e => e.Code == "InvalidToken"))  // Happens when token is expired. I don't know whether it could indicate anything else
                {
                    string UserMsg = "";
                    if (await _userManager.IsEmailConfirmedAsync(user))
                    {
                        await SendPasswordResetEmail(user, Url);
                        Log.Debug($"In AccountController.ResetPassword POST action: for user {user.UserName}, expired reset token, new password reset email sent, aborting");
                        UserMsg = "Your password reset link has expired.  A new password reset email is being sent to you now.  Please use the link in that email to reset your password.";
                    }
                    else
                    {
                        string EmailBodyText = "Welcome to Milliman Access Portal.  Below is an activation link for your account";
                        Task DontWaitForMe = Task.Run(() => SendNewAccountWelcomeEmail(user, Url, EmailBodyText));

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
        public async Task<JsonResult> NavBarElements() {
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
            AuthorizationResult ClientAdminResult1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, null));
            AuthorizationResult ClientAdminResult2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin, null));
            if (ClientAdminResult1.Succeeded || ClientAdminResult2.Succeeded)
            {
                NavBarElements.Add(new NavBarElementModel
                {
                    Order = order++,
                    Label = "Manage Clients",
                    URL = nameof(ClientAdminController).Replace("Controller", ""),
                    View = "ClientAdmin",
                    Icon = "client-admin",
                });
            }

            // Conditionally add the Content Publishing Element
            AuthorizationResult ContentPublishResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentPublisher, null));
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
            AuthorizationResult ContentAccessResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
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

            AccountSettingsViewModel model = new AccountSettingsViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Employer = user.Employer
            };

            Log.Verbose($"In AccountController.AccountSettings GET action, returning model <{model}>");

            return View(model);
        }

        // POST /Account/UpdateAccountSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AccountSettings([Bind("UserName,FirstName,LastName,PhoneNumber,Employer")]AccountSettingsViewModel Model)
        {
            Log.Verbose("Entered AccountController.AccountSettings POST action with model {@Model}", Model);

            ApplicationUser user = await Queries.GetCurrentApplicationUser(User);
            if (user == null)
            {
                Log.Debug($"In AccountController.AccountSettings POST action: user {User.Identity.Name} not found, aborting");
                return View("Message", "Unable to assign new settings for current user");
            }

            if (Model.UserName != User.Identity.Name)
            {
                Log.Information($"In AccountController.AccountSettings action: user {User.Identity.Name} attempt to update settings for application user {Model.UserName}, aborting");
                Response.Headers.Add("Warning", "You may not access another user's settings.");
                return Unauthorized();
            }

            if (!string.IsNullOrEmpty(Model.FirstName))
            {
                user.FirstName = Model.FirstName;
            }

            if (!string.IsNullOrEmpty(Model.LastName))
            {
                user.LastName = Model.LastName;
            }

            if (!string.IsNullOrEmpty(Model.PhoneNumber))
            {
                user.PhoneNumber = Model.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(Model.Employer))
            {
                user.Employer = Model.Employer;
            }

            DbContext.ApplicationUser.Update(user);
            DbContext.SaveChanges();

            Log.Verbose($"In AccountController.AccountSettings POST action: successful update for user {user.UserName}");

            return Ok();
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdatePassword([Bind("UserName,CurrentPassword,NewPassword,ConfirmNewPassword")]AccountSettingsViewModel Model)
        {
            Log.Verbose("Entered AccountController.UpdatePassword action");

            ApplicationUser user = await Queries.GetCurrentApplicationUser(User);
            if (Model.UserName != User.Identity.Name)
            {
                Log.Warning($"In AccountController.UpdatePassword action: user {User.Identity.Name} attempt to update password for application user {Model.UserName}, aborting");
                Response.Headers.Add("Warning", "You may not access another user's settings.");
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                using (var Txn = DbContext.Database.BeginTransaction())
                {
                    IdentityResult result = await _userManager.ChangePasswordAsync(user, Model.CurrentPassword, Model.NewPassword);

                    if (result.Succeeded)
                    {
                        // Save password hash in history
                        user.PasswordHistoryObj = user.PasswordHistoryObj.Append<PreviousPassword>(new PreviousPassword(Model.NewPassword)).ToList<PreviousPassword>();
                        user.LastPasswordChangeDateTimeUtc = DateTime.UtcNow;
                        var addHistoryResult = await _userManager.UpdateAsync(user);

                        if (addHistoryResult.Succeeded)
                        {
                            Txn.Commit();
                            Log.Verbose($"In AccountController.UpdatePassword action: success for user {user.UserName}");
                            return Ok();
                        }
                        else
                        {
                            Log.Error($"Failed to save password history or update password timestamp for user {user.UserName}, aborting");
                        }
                    }
                    else
                    {
                        Log.Error($"In AccountController.UpdatePassword action: Failed to update password for user {user.UserName}, aborting");
                        string errorMessage = string.Join("<br /><br />", result.Errors.Select(x => x.Description));

                        Response.Headers.Add("Warning", errorMessage);
                    }
                }
            }
            else
            {
                Log.Warning($"In UpdatePassword action for user {user.UserName}, ModelState errors {string.Join(", ", ModelState.SelectMany(s => s.Value.Errors).Select(e => e.ErrorMessage))}");
                Response.Headers.Add("Warning", $"Password update failed");
            }

            return BadRequest();
        }

        [HttpGet]
        [PreventAuthRefresh]
        public IActionResult SessionStatus()
        {
            return Ok();
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
