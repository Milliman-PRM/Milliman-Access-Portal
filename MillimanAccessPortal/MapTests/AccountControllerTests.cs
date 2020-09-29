/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Xunit tests for the account controller
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Models.SharedModels;
using MillimanAccessPortal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TestResourcesLib;
using Xunit;

namespace MapTests
{
    [Collection("DatabaseLifetime collection")]
    [LogTestBeginEnd]
    public class AccountControllerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        public AccountControllerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
        }

        /// <summary>
        /// Common controller constructor to be used by all tests
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        private AccountController GetController(TestInitialization testResources, string UserName = null)
        {
            AccountController testController = new AccountController(testResources.DbContext,
                testResources.UserManager,
                testResources.RoleManager,
                testResources.SignInManager,
                testResources.MessageQueueServicesObject,
                testResources.AuditLogger,
                testResources.AuthorizationService,
                testResources.Configuration,
                testResources.ScopedServiceProvider,
                testResources.AuthenticationService)
                ;

            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = "abc",
                Port = 123,
            };
            testController.ControllerContext = testResources.GenerateControllerContext(userName: UserName, requestUriBuilder: uriBuilder);
            testController.HttpContext.Session = new MockSession();
            return testController;
        }

        [Fact]
        public async Task EnableAccountGETReturnsEnableFormWhenNotEnabled()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                ApplicationUser user = await TestResources.UserManager.FindByNameAsync("user1");
                string TestCode = await TestResources.UserManager.GenerateEmailConfirmationTokenAsync(user);
                string TestUserId = TestUtil.MakeTestGuid(1).ToString();
                #endregion

                #region Act
                var view = await controller.EnableAccount(TestUserId, TestCode);
                #endregion

                #region Assert
                ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
                //Assert.Equal("EnableAccount", viewAsViewResult.ViewName);  // .ViewName is null when the action is driven by xunit
                EnableAccountViewModel viewModel = Assert.IsType<EnableAccountViewModel>(viewAsViewResult.Model);
                Assert.Equal(TestCode, viewModel.Code);
                Assert.Equal(TestUserId, viewModel.Id.ToString());
                Assert.Null(viewModel.FirstName);
                Assert.Null(viewModel.LastName);
                Assert.Null(viewModel.Phone);
                Assert.Null(viewModel.Employer);
                Assert.Null(viewModel.NewPassword);
                Assert.Null(viewModel.ConfirmNewPassword);
                #endregion
            }
        }

        [Fact]
        public async Task EnableAccountGETReturnsLoginWhenEnabled()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user2");
                MockRouter.AddToController(controller, new Dictionary<string, string>() { { "action", "EnableAccount" }, { "controller", "Account" } });
                ApplicationUser user = await TestResources.UserManager.FindByNameAsync("user2");
                string TestCode = await TestResources.UserManager.GenerateEmailConfirmationTokenAsync(user);
                string TestUserId = TestUtil.MakeTestGuid(2).ToString();
                #endregion

                #region Act
                var view = await controller.EnableAccount(TestUserId, TestCode);
                #endregion

                #region Assert
                var typedResult = Assert.IsType<RedirectToActionResult>(view);
                Assert.Equal("Login", typedResult.ActionName);

                #endregion
            }
        }

        [Fact]
        public async Task EnableAccountGETReturnsMessageWhenTokenIsInvalid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                string TestUserId = TestUtil.MakeTestGuid(1).ToString();
                #endregion

                #region Act
                var view = await controller.EnableAccount(TestUserId, "BadToken!!!!!!!!!!!!!");
                #endregion

                #region Assert
                ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
                Assert.Equal("UserMessage", viewAsViewResult.ViewName);

                #endregion
            }
        }

        [Fact]
        public async Task EnableAccountPOSTUpdatesUserWhenTokenIsValid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                MockRouter.AddToController(controller, new Dictionary<string, string>() { { "action", "EnableAccount" }, { "controller", "Account" } });
                ApplicationUser user = await TestResources.UserManager.FindByNameAsync("user1");
                string NewToken = await TestResources.UserManager.GenerateEmailConfirmationTokenAsync(user);
                string NewPass = "TestPassword1!";
                string NewEmployer = "Milliman";
                string FirstName = "MyFirstName";
                string LastName = "MyLastName";
                string Phone = "3173171212";
                EnableAccountViewModel model = new EnableAccountViewModel
                {
                    Id = TestUtil.MakeTestGuid(1),
                    Code = NewToken,
                    NewPassword = NewPass,
                    ConfirmNewPassword = NewPass,
                    Employer = NewEmployer,
                    FirstName = FirstName,
                    LastName = LastName,
                    Phone = Phone,
                    IsLocalAccount = true,
                    Username = controller.HttpContext.User.Identity.Name,
                };
                #endregion

                #region Act
                var view = await controller.EnableAccount(model);
                var UserRecord = TestResources.DbContext.ApplicationUser.Single(u => u.UserName == "user1");
                #endregion

                #region Assert
                RedirectToActionResult typedResult = Assert.IsType<RedirectToActionResult>(view);
                Assert.Equal("Login", typedResult.ActionName);
                Assert.True(await TestResources.UserManager.CheckPasswordAsync(user, NewPass));
                Assert.Equal(NewEmployer, UserRecord.Employer);
                Assert.Equal(FirstName, UserRecord.FirstName);
                Assert.Equal(LastName, UserRecord.LastName);
                Assert.Equal(Phone, UserRecord.PhoneNumber);
                #endregion
            }
        }

        [Fact]
        public async Task EnableAccountPOSTReturnsMessageWhenTokenIsInvalid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                string NewPass = "TestPassword";
                string NewEmployer = "Milliman";
                string FirstName = "MyFirstName";
                string LastName = "MyLastName";
                string Phone = "3173171212";
                EnableAccountViewModel model = new EnableAccountViewModel
                {
                    Id = TestUtil.MakeTestGuid(1),
                    Code = "BadToken!!!!!!!!!!!!",
                    NewPassword = NewPass,
                    ConfirmNewPassword = NewPass,
                    Employer = NewEmployer,
                    FirstName = FirstName,
                    LastName = LastName,
                    Phone = Phone,
                };
                #endregion

                #region Act
                var view = await controller.EnableAccount(model);
                var UserRecord = TestResources.DbContext.ApplicationUser.Single(u => u.UserName == "user1");
                #endregion

                #region Assert
                ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
                Assert.Equal("UserMessage", viewAsViewResult.ViewName);
                #endregion
            }
        }

        [Fact]
        public async Task ForgotPasswordPOSTReturnsMessageWhenNotActivated()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var model = new ForgotPasswordViewModel
                {
                    Email = "user1@example.com"
                };
                #endregion

                #region Act
                var view = await controller.ForgotPassword(model);
                #endregion

                #region Assert
                ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
                Assert.Equal("UserMessage", viewAsViewResult.ViewName);  // This one works because view is named explicitly in controller
                #endregion
            }
        }

        [Fact]
        public async Task ForgotPasswordPOSTReturnsConfirmationWhenActivated()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user2");
                MockRouter.AddToController(controller, new Dictionary<string, string>() { { "action", "ForgotPassword" }, { "controller", "Account" } });

                var model = new ForgotPasswordViewModel
                {
                    Email = "user2@example.com"
                };
                #endregion

                #region Act
                var view = await controller.ForgotPassword(model);
                #endregion

                #region Assert
                ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
                Assert.Equal(nameof(SharedController.UserMessage), viewAsViewResult.ViewName);  // This one works because view is named explicitly in controller
                Assert.IsType<MillimanAccessPortal.Models.SharedModels.UserMessageModel>(viewAsViewResult.Model);
                #endregion
            }
        }


        [Fact]
        public async Task ResetPasswordGETReturnsFormWhenTokenIsValid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                string TestEmail = "user1@example.com";
                ApplicationUser user = await TestResources.UserManager.FindByNameAsync("user1");
                string TestToken = await TestResources.UserManager.GeneratePasswordResetTokenAsync(user);
                #endregion

                #region Act
                var view = await controller.ResetPassword(TestEmail, TestToken);
                #endregion

                #region Assert
                ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
                ResetPasswordViewModel viewModel = Assert.IsType<ResetPasswordViewModel>(viewAsViewResult.Model);
                Assert.Equal(TestEmail, viewModel.Email);
                Assert.Equal(TestToken, viewModel.PasswordResetToken);
                Assert.Equal(string.Empty, viewModel.Message);
                Assert.Null(viewModel.ConfirmNewPassword);
                Assert.Null(viewModel.NewPassword);
                #endregion
            }
        }

        [Fact]
        public async Task ResetPasswordGETReturnsMessageWhenTokenIsInvalid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                string TestEmail = "user1@example.com";
                string TestToken = "IncorrectToken";
                #endregion

                #region Act
                var view = await controller.ResetPassword(TestEmail, TestToken);
                #endregion

                #region Assert
                ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
                Assert.Equal("UserMessage", viewAsViewResult.ViewName);
                #endregion
            }
        }

        [Fact]
        public async Task ResetPasswordPOSTReturnsRightFormWhenTokenIsValid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                ApplicationUser user1 = TestResources.DbContext.ApplicationUser.Single(u => u.UserName == "user1");
                ResetPasswordViewModel model = new ResetPasswordViewModel
                {
                    Email = "user1@example.com",
                    PasswordResetToken = await TestResources.UserManager.GeneratePasswordResetTokenAsync(user1),
                    NewPassword = "Password123$",
                    ConfirmNewPassword = "Password123$",
                };
                #endregion

                #region Act
                var view = await controller.ResetPassword(model);
                #endregion

                #region Assert
                var viewAsViewResult = Assert.IsType<ViewResult>(view);
                Assert.Equal("UserMessage", viewAsViewResult.ViewName);
                var viewModel = Assert.IsType<UserMessageModel>(viewAsViewResult.Model);
                Assert.Contains("Your password has been reset", viewModel.PrimaryMessages[0]);
                #endregion
            }
        }

        [Fact]
        public async Task ResetPasswordPOSTReturnsMessageWhenTokenIsInvalid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                ResetPasswordViewModel model = new ResetPasswordViewModel
                {
                    Email = "user1@example.com",
                    PasswordResetToken = "BadToken!!!!!!!!!!!!!",
                    NewPassword = "Password123",
                    ConfirmNewPassword = "Password123",
                };
                #endregion

                #region Act
                var view = await controller.ResetPassword(model);
                #endregion

                #region Assert
                var viewAsViewResult = view as ViewResult;
                Assert.Equal("UserMessage", viewAsViewResult.ViewName);
                #endregion
            }
        }

        /// <summary>
        /// Verify that The PasswordRecentDaysValidator rejects passwords within the specified range of recent history
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PasswordInRecentDaysNotAllowed()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);
                var validator = new PasswordRecentDaysValidator<ApplicationUser>() { numberOfDays = 1 };

                string newPassword = "Passw0rd!";
                var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword));
                AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
                #endregion

                #region Act

                IdentityResult result = await validator.ValidateAsync(TestResources.UserManager, AppUser, newPassword);
                #endregion

                #region Assert
                Assert.False(result.Succeeded);
                #endregion
            }
        }

        /// <summary>
        /// Verify that The PasswordRecentDaysValidator accepts passwords earlier in the user's history than the specified time frame
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PasswordNotInRecentDaysAllowed()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);
                var validator = new PasswordRecentDaysValidator<ApplicationUser>() { numberOfDays = 1 };

                string newPassword = "Passw0rd!";
                var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword) { dateSetUtc = DateTime.UtcNow.Subtract(new TimeSpan(2, 0, 0, 0)) });
                AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
                #endregion

                #region Act
                IdentityResult result = await validator.ValidateAsync(TestResources.UserManager, AppUser, newPassword);
                #endregion

                #region Assert
                Assert.True(result.Succeeded);
                #endregion
            }
        }

        /// <summary>
        /// Verify that The PasswordRecentNumberValidator rejects passwords within the specified range of recent history
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PasswordInRecentNumberNotAllowed()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);
                var validator = new PasswordRecentNumberValidator<ApplicationUser>() { numberOfPasswords = 1 };

                string newPassword = "Passw0rd!";
                var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword));
                AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();

                string secondNewPassword = "Passw0rd!2";
                passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(secondNewPassword));
                AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
                #endregion

                #region Act
                IdentityResult result = await validator.ValidateAsync(TestResources.UserManager, AppUser, secondNewPassword);
                #endregion

                #region Assert
                Assert.False(result.Succeeded);
                #endregion
            }
        }

        /// <summary>
        /// Verify that The PasswordRecentNumberValidator accepts passwords earlier in the user's history than the specified number
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PasswordNotInRecentNumberAllowed()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);
                var validator = new PasswordRecentNumberValidator<ApplicationUser>() { numberOfPasswords = 1 };

                string newPassword = "Passw0rd!";
                var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword));
                AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();

                string secondNewPassword = "Passw0rd!2";
                passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(secondNewPassword));
                AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
                #endregion

                #region Act
                IdentityResult result = await validator.ValidateAsync(TestResources.UserManager, AppUser, newPassword);
                #endregion

                #region Assert
                Assert.True(result.Succeeded);
                #endregion
            }
        }

        /// <summary>
        /// Verify that The PasswordRecentNumberValidator accepts passwords earlier in the user's history than the specified number
        /// </summary>
        /// <returns></returns>
        
        [Theory]
        [InlineData("Passw0rd!")]
        [InlineData("Passw0rd!2")]
        public async Task PasswordEverInHistoryNotAllowed(string inputPassword)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);
                var validator = new PasswordHistoryValidator<ApplicationUser>();

                string newPassword = "Passw0rd!";
                var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword));
                AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();

                string secondNewPassword = "Passw0rd!2";
                passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(secondNewPassword));
                AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
                #endregion

                #region Act
                IdentityResult result = await validator.ValidateAsync(TestResources.UserManager, AppUser, inputPassword);
                #endregion

                #region Assert
                Assert.False(result.Succeeded);
                #endregion
            }
        }

        [Fact]
        public async Task EmailInPasswordNotAllowed()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);
                var validator = new PasswordIsNotEmailValidator<ApplicationUser>();
                #endregion

                #region Act
                IdentityResult result = await validator.ValidateAsync(TestResources.UserManager, AppUser, AppUser.Email);
                #endregion

                #region Assert
                Assert.False(result.Succeeded);
                #endregion
            }
        }

        /// <summary>
        /// Verify that PasswordContainsCommonWordsValidator will not allow passwords that contain a banned word or phrase
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CommonWordInPasswordNotAllowed()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);
                var validator = new PasswordContainsCommonWordsValidator<ApplicationUser>() { commonWords = { "milliman" } };
                #endregion

                #region Act
                IdentityResult result = await validator.ValidateAsync(TestResources.UserManager, AppUser, "Milliman123");
                #endregion

                #region Assert
                Assert.False(result.Succeeded);
                #endregion
            }
        }

        [Fact]
        public async Task AccountSettingsGETWorks()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);
                #endregion

                #region Act
                var view = await controller.AccountSettings();
                var UserRecord = TestResources.DbContext.ApplicationUser.Single(u => u.UserName == "user1");
                #endregion

                #region Assert
                Assert.IsType<ViewResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateAccountPOSTWorksForUserInformation()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                var AppUser = await TestResources.UserManager.GetUserAsync(controller.ControllerContext.HttpContext.User);

                string NewEmployer = "Milliman";
                string NewFirstName = "MyFirstName";
                string NewLastName = "MyLastName";
                string NewPhone = "3173171212";
                UpdateAccountModel model = new UpdateAccountModel
                {
                    User = new UpdateAccountModel.UserModel
                    {
                        FirstName = NewFirstName,
                        Employer = NewEmployer,
                        LastName = NewLastName,
                        Phone = NewPhone,
                    },
                };
                #endregion

                #region Act
                var view = await controller.UpdateAccount(model);
                var UserRecord = TestResources.DbContext.ApplicationUser.Single(u => u.UserName == "user1");
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(view);
                Assert.Equal(NewEmployer, UserRecord.Employer);
                Assert.Equal(NewFirstName, UserRecord.FirstName);
                Assert.Equal(NewLastName, UserRecord.LastName);
                Assert.Equal(NewPhone, UserRecord.PhoneNumber);
                #endregion
            }
        }

        [Theory]
        [InlineData("NonUser", true)]
        [InlineData("user2", true)]
        [InlineData("user3-confirmed-defaultscheme", true)]
        [InlineData("user4-confirmed-wsscheme", false)]
        [InlineData("user6-confirmed@domainmatch.local", false)]
        [InlineData("user7-confirmed@domainnomatch.local", true)]
        public async Task IsLocalAccount(string userName, bool isLocalTruth)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources);
                #endregion

                #region Act
                var result = await controller.IsLocalAccount(userName);
                #endregion

                #region Assert
                JsonResult typedResult = Assert.IsType<JsonResult>(result);
                PropertyInfo info = typedResult.Value.GetType().GetProperty("localAccount");
                Assert.Equal(typeof(bool), info.PropertyType);
                Assert.Equal(isLocalTruth, (bool)info.GetValue(typedResult.Value));
                #endregion
            }
        }

        [Theory]
        [InlineData("NonUser", typeof(RedirectToActionResult), "Login")]
        [InlineData("user2", typeof(RedirectToActionResult), "Login")]
        [InlineData("user3-confirmed-defaultscheme", typeof(RedirectToActionResult), "Login")]
        [InlineData("user4-confirmed-wsscheme", typeof(ChallengeResult), "prmtest")]
        [InlineData("user6-confirmed@domainmatch.local", typeof(ChallengeResult), "domainmatch")]
        [InlineData("user7-confirmed@domainnomatch.local", typeof(RedirectToActionResult), "Login")]
        public async Task RemoteAuthenticateReturnsCorrectResult(string userName, Type expectedType, string expectedString)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources);
                MockRouter.AddToController(controller, new Dictionary<string, string>() { { "action", "RemoteAuthenticate" }, { "controller", "Account" } });
                #endregion

                #region Act
                var result = await controller.RemoteAuthenticate(userName, "%2F");
                #endregion

                #region Assert
                Assert.IsType(expectedType, result);
                switch (expectedType.Name)
                {
                    case "RedirectToActionResult":
                        RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
                        Assert.Equal(expectedString, redirectToActionResult.ActionName);
                        break;
                    case "ChallengeResult":
                        ChallengeResult challengeResult = (ChallengeResult)result;
                        Assert.Equal(1, challengeResult.AuthenticationSchemes.Count);
                        Assert.Equal(expectedString, challengeResult.AuthenticationSchemes.ElementAt(0), StringComparer.OrdinalIgnoreCase);
                        break;
                    default:
                        throw new NotImplementedException($"Unsupported return type <{expectedType.Name}> expected. This unit test needs work.");
                }
                #endregion
            }
        }

        [Fact]
        public async Task LoginStepTwo_ErrorWithout2FACookie()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Account))
            {
                #region Arrange
                AccountController controller = GetController(TestResources, "user1");
                MockRouter.AddToController(controller, new Dictionary<string, string>() { { "action", "LoginStepTwo" }, { "controller", "Account" } });
                #endregion

                #region Act
                var result = await controller.LoginStepTwo("test1", "%2F");
                #endregion

                #region Assert
                var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
                Assert.Equal(StatusCodes.Status422UnprocessableEntity, statusCodeResult.StatusCode);
                #endregion
            }
        }
    }
}
