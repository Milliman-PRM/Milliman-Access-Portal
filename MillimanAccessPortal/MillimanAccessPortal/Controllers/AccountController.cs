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
using Microsoft.Extensions.Logging;
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
using Microsoft.Extensions.Configuration;

namespace MillimanAccessPortal.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMessageQueue _messageSender;
        private readonly ILogger _logger;
        private readonly IAuditLogger _auditLogger;
        private readonly StandardQueries Queries;
        private readonly IConfiguration _confiugration;

        public AccountController(
            ApplicationDbContext ContextArg,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IMessageQueue messageSender,
            ILoggerFactory loggerFactory,
            IAuditLogger AuditLoggerArg,
            StandardQueries QueriesArg,
            IConfiguration ConfigArg)
        {
            DbContext = ContextArg;
            _userManager = userManager;
            _signInManager = signInManager;
            _messageSender = messageSender;
            _logger = loggerFactory.CreateLogger<AccountController>();
            _auditLogger = AuditLoggerArg;
            Queries = QueriesArg;
            _confiugration = ConfigArg;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            Guid.Empty.ToString();

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
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);

                // Only notify of password expiration if the correct password was provided
                // Redirect user to the password reset view to set a new password
                bool passwordSuccess = await _userManager.CheckPasswordAsync(user, model.Password);

                // Set a default value in case the configuration isn't found or isn't an int
                int defaultExpirationDays = 30;
                int expirationDays = defaultExpirationDays;
                try
                {
                    expirationDays = _confiugration.GetValue<int>("PasswordExpirationDays");
                }
                catch
                {
                    expirationDays = defaultExpirationDays;
                    _logger.LogWarning($"PasswordExpirationDays value not found or cannot be cast to an integer. The default value of { expirationDays } will be used.");
                }
                                
                if (user.PasswordChangeDate.AddDays(expirationDays) < DateTime.UtcNow && passwordSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Password Has Expired.");
                    return View("ResetPassword");
                }
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    HttpContext.Session.SetString("SessionId", HttpContext.Session.Id);

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
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    }
                    else if (result.IsNotAllowed)
                    {
                        ModelState.AddModelError(string.Empty, "User login is not allowed.");
                        _logger.LogWarning(2, $"User login not allowed: {model.Username}");
                        _auditLogger.Log(AuditEventType.LoginNotAllowed.ToEvent(), model.Username);
                        return View("Lockout");  // TODO need a better UX
                    }
                    else if (result.IsLockedOut)
                    {
                        ModelState.AddModelError(string.Empty, "User account is locked out.");
                        _logger.LogWarning(2, "User account locked out.");
                        _auditLogger.Log(AuditEventType.LoginIsLockedOut.ToEvent(), model.Username);
                        return View("Lockout");  // TODO need a better UX
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        _logger.LogWarning(2, "User login failed.");
                        _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(), model.Username);
                        return View(model);
                    }
                }

            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action(nameof(EnableAccount), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    _messageSender.QueueEmail(model.Email, "Confirm your account",
                        $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                    _logger.LogInformation(3, "User created a new account with password.");
                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Logout
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            ApplicationUser appUser = await Queries.GetCurrentApplicationUser(User);
            await _signInManager.SignOutAsync();

            _auditLogger.Log(AuditEventType.Logout.ToEvent(), appUser?.UserName);
            _logger.LogInformation(4, "User logged out.");

            Response.Cookies.Delete(".AspNetCore.Session");
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(AccountController.Login), "Account");
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
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
                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
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
                        _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [NonAction]
        public async void SendNewAccountWelcomeEmail(ApplicationUser RequestedUser, IUrlHelper Url, string SettableEmailText = null)
        {
            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(RequestedUser);
            var callbackUrl = Url.Action(nameof(AccountController.EnableAccount), "Account", new { userId = RequestedUser.Id, code = emailConfirmationToken }, protocol: "https");

            // Configurable portion of email body
            string emailBody = string.IsNullOrWhiteSpace(SettableEmailText)
                ? string.Empty
                : SettableEmailText + $"{Environment.NewLine}{Environment.NewLine}";

            // Non-configurable portion of email body
            emailBody += $"To activate your new account please click the below link or paste to your web browser:{Environment.NewLine}{callbackUrl}";
            string emailSubject = "Welcome to Milliman Access Portal";
            // Send welcome email
            _messageSender.QueueEmail(RequestedUser.Email, emailSubject, emailBody /*, optional senderAddress, optional senderName*/);
        }

        // GET: /Account/EnableAccount
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> EnableAccount(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }

            // Prompt for the user's password
            var model = new EnableAccountViewModel
            {
                Id = user.Id,
                Code = code,
            };
            return View(model);
        }

        // POST: /Account/EnableAccount
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAccount(EnableAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user == null)
            {
                return View("Error");
            }

            IdentityResult identityResult = await _userManager.ConfirmEmailAsync(user, model.Code);
            if (identityResult.Succeeded)
            {
                identityResult = await _userManager.AddPasswordAsync(user, model.NewPassword);

                if (identityResult.Succeeded)
                {
                    // Save password hash in history
                    user.PasswordHistoryObj = user.PasswordHistoryObj.Append<PreviousPassword>(new PreviousPassword(model.NewPassword)).ToList<PreviousPassword>();
                    user.PasswordChangeDate = DateTime.Now;
                    var addHistoryResult = await _userManager.UpdateAsync(user);

                    if (!addHistoryResult.Succeeded)
                    {
                        _logger.LogError($"Failed to save password history for {user.UserName }");
                    }

                    _auditLogger.Log(AuditEventType.UserAccountEnabled.ToEvent(user));

                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Employer = model.Employer;
                    user.PhoneNumber = model.Phone;
                    await _userManager.UpdateAsync(user);

                    return View("Login");
                }
            }

            string Errors = string.Join($", ", identityResult.Errors.Select(e => e.Description));
            Response.Headers.Add("Warning", $"Error while enabling account: {Errors}");
            return View("Error");
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
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
            // Sends an email with password reset link to the requested user
            // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && (await _userManager.IsEmailConfirmedAsync(user)))
                {
                    string PasswordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    string linkUrl = Url.Action(nameof(ResetPassword), "Account", new { userEmail = user.Email, passwordResetToken = PasswordResetToken }, protocol: "https");

                    string emailBody = $"A password reset was requested for your Milliman Access Portal account.  Please create a new password at the below linked page.{Environment.NewLine}";
                    emailBody += $"Your user name is {user.UserName}{Environment.NewLine}{Environment.NewLine}";
                    emailBody += $"{linkUrl}";
                    _messageSender.QueueEmail(model.Email, "MAP password reset", emailBody);

                    _auditLogger.Log(AuditEventType.PasswordResetRequested.ToEvent(user));
                }
                else
                {
                    _auditLogger.Log(AuditEventType.PasswordResetRequestedForInvalidEmail.ToEvent(model.Email));
                }
            }

            return View("ForgotPasswordConfirmation", model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userEmail, string passwordResetToken)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return View("Error");
            }

            ResetPasswordViewModel model = new ResetPasswordViewModel
            {
                Email = user.Email,
                PasswordResetToken = passwordResetToken,
            };

            return View(model);
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Message = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                // TODO but do something better than this!
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.PasswordResetToken, model.NewPassword);
            if (result.Succeeded)
            {
                // Save password hash in history
                user.PasswordHistoryObj = user.PasswordHistoryObj.Append<PreviousPassword>(new PreviousPassword(model.NewPassword)).ToList<PreviousPassword>();
                user.PasswordChangeDate = DateTime.Now;
                var addHistoryResult = await _userManager.UpdateAsync(user);

                if (!addHistoryResult.Succeeded)
                {
                    _logger.LogError($"Failed to save password history for {user.UserName }");
                }

                _auditLogger.Log(AuditEventType.PasswordResetCompleted.ToEvent(user));
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            AddErrors(result);
            model.Message = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
            return View(model);
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
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
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
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
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
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
                _logger.LogWarning(7, "User account locked out.");
                return View("Lockout");
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
            return View();
        }

        //
        // GET /Account/Settings
        [HttpGet]
        [Route("Account/Settings")]
        public async Task<IActionResult> AccountSettings()
        {
            ApplicationUser user = await Queries.GetCurrentApplicationUser(User);
            AccountSettingsViewModel model = new AccountSettingsViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Employer = user.Employer
            };

            return View(model);
        }

        // POST /Account/UpdateAccountSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AccountSettings([Bind("UserName,FirstName,LastName,PhoneNumber,Employer")]AccountSettingsViewModel Model)
        {
            ApplicationUser user = await Queries.GetCurrentApplicationUser(User);
            if (Model.UserName != User.Identity.Name)
            {
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

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdatePassword([Bind("UserName,CurrentPassword,NewPassword,ConfirmNewPassword")]AccountSettingsViewModel Model)
        {
            ApplicationUser user = await Queries.GetCurrentApplicationUser(User);
            if (Model.UserName != User.Identity.Name)
            {
                Response.Headers.Add("Warning", "You may not access another user's settings.");
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                IdentityResult result = await _userManager.ChangePasswordAsync(user, Model.CurrentPassword, Model.NewPassword);
                
                if (result.Succeeded)
                {
                    // Save password hash in history
                    user.PasswordHistoryObj = user.PasswordHistoryObj.Append<PreviousPassword>(new PreviousPassword(Model.NewPassword)).ToList<PreviousPassword>();
                    user.PasswordChangeDate = DateTime.Now;
                    var addHistoryResult = await _userManager.UpdateAsync(user);

                    if (!addHistoryResult.Succeeded)
                    {
                        _logger.LogError($"Failed to save password history for {user.UserName }");
                    }

                    return Ok();
                }
                else
                {
                    string errorMessage = string.Join("<br /><br />", result.Errors.Select(x => x.Description));

                    Response.Headers.Add("Warning", errorMessage);
                    return BadRequest();
                }
            }
            else
            {
                Response.Headers.Add("Warning", $"Password update failed");
                return BadRequest();
            }
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
