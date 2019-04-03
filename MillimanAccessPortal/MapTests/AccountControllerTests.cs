/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Xunit tests for the account controller
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.AccountViewModels;
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
    public class AccountControllerTests
    {
        internal TestInitialization TestResources { get; set; }

        /// <summary>
        /// Constructor is called for each test execution
        /// </summary>
        public AccountControllerTests()
        {
            TestResources = new TestInitialization();
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Account });
        }

        /// <summary>
        /// Common controller constructor to be used by all tests
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public AccountController GetController(string UserName = null)
        {
            AccountController testController = new AccountController(TestResources.DbContextObject,
                TestResources.UserManagerObject,
                TestResources.RoleManagerObject,
                TestResources.SignInManagerObject,
                TestResources.MessageQueueServicesObject,
                TestResources.AuditLoggerObject,
                TestResources.QueriesObj,
                TestResources.AuthorizationService,
                TestResources.ConfigurationObject,
                TestResources.ServiceProviderObject,
                TestResources.AuthenticationServiceObject)
                ;

            testController.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: UserName);
            testController.HttpContext.Session = new MockSession();
            return testController;
        }

        [Fact]
        public async Task EnableAccountGETReturnsEnableFormWhenNotEnabled()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            string TestCode = MockUserManager.GoodToken;
            string TestUserId = TestUtil.MakeTestGuid(1).ToString();
            #endregion

            #region Act
            var view = await controller.EnableAccount(TestUserId, TestCode);
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            //Assert.Equal("EnableAccount", viewAsViewResult.ViewName);  // .ViewName is null when the action is driven by xunit
            Assert.IsType<EnableAccountViewModel>(viewAsViewResult.Model);
            EnableAccountViewModel viewModel = viewAsViewResult.Model as EnableAccountViewModel;
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

        [Fact]
        public async Task EnableAccountGETReturnsLoginWhenEnabled()
        {
            #region Arrange
            AccountController controller = GetController("user2");
            string TestCode = MockUserManager.GoodToken;
            string TestUserId = TestUtil.MakeTestGuid(2).ToString();
            #endregion

            #region Act
            var view = await controller.EnableAccount(TestUserId, TestCode);
            #endregion

            #region Assert
            ViewResult viewAsViewResult = view as ViewResult;

            Assert.IsType<ViewResult>(view);
            Assert.Equal("Login", viewAsViewResult.ViewName);
            
            #endregion
        }

        [Fact]
        public async Task EnableAccountGETReturnsMessageWhenTokenIsInvalid()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            string TestCode = MockUserManager.BadToken;
            string TestUserId = TestUtil.MakeTestGuid(1).ToString();
            #endregion

            #region Act
            var view = await controller.EnableAccount(TestUserId, TestCode);
            #endregion

            #region Assert
            ViewResult viewAsViewResult = view as ViewResult;

            Assert.IsType<ViewResult>(view);
            Assert.Equal("Message", viewAsViewResult.ViewName);

            #endregion
        }

        [Fact]
        public async Task EnableAccountPOSTUpdatesUserWhenTokenIsValid()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            string NewToken = MockUserManager.GoodToken;
            string NewPass = "TestPassword";
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
            };
            #endregion

            #region Act
            var view = await controller.EnableAccount(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            Assert.Equal("Login", viewAsViewResult.ViewName);
            Assert.Equal(NewPass + "xyz", UserRecord.PasswordHash);
            Assert.Equal(NewEmployer, UserRecord.Employer);
            Assert.Equal(FirstName, UserRecord.FirstName);
            Assert.Equal(LastName, UserRecord.LastName);
            Assert.Equal(Phone, UserRecord.PhoneNumber);
            #endregion
        }

        [Fact]
        public async Task EnableAccountPOSTReturnsMessageWhenTokenIsInvalid()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            string NewToken = MockUserManager.BadToken;
            string NewPass = "TestPassword";
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
            };
            #endregion

            #region Act
            var view = await controller.EnableAccount(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            Assert.Equal("Message", viewAsViewResult.ViewName);
            #endregion
        }

        [Fact]
        public async Task ForgotPasswordPOSTReturnsMessageWhenNotActivated()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var model = new ForgotPasswordViewModel
            {
                Email = "user1@example.com"
            };
            #endregion

            #region Act
            var view = await controller.ForgotPassword(model);
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            Assert.Equal("Message", viewAsViewResult.ViewName);  // This one works because view is named explicitly in controller
            #endregion

        }

        [Fact]
        public async Task ForgotPasswordPOSTReturnsConfirmationWhenActivated()
        {
            #region Arrange
            AccountController controller = GetController("user2");
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
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            Assert.Equal(nameof(SharedController.Message), viewAsViewResult.ViewName);  // This one works because view is named explicitly in controller
            Assert.IsType<string>(viewAsViewResult.Model);
            #endregion
        }


        [Fact]
        public async Task ResetPasswordGETReturnsFormWhenTokenIsValid()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            string TestEmail = "user1@example.com";
            string TestToken = MockUserManager.GoodToken;
            #endregion

            #region Act
            var view = await controller.ResetPassword(TestEmail, TestToken);
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            Assert.IsType<ResetPasswordViewModel>(viewAsViewResult.Model);
            ResetPasswordViewModel viewModel = viewAsViewResult.Model as ResetPasswordViewModel;
            Assert.Equal(TestEmail, viewModel.Email);
            Assert.Equal(TestToken, viewModel.PasswordResetToken);
            Assert.Equal(string.Empty, viewModel.Message);
            Assert.Null(viewModel.ConfirmNewPassword);
            Assert.Null(viewModel.NewPassword);
            #endregion
        }

        [Fact]
        public async Task ResetPasswordGETReturnsMessageWhenTokenIsInvalid()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            string TestEmail = "user1@example.com";
            string TestToken = MockUserManager.BadToken;
            #endregion

            #region Act
            var view = await controller.ResetPassword(TestEmail, TestToken);
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            Assert.Equal("Message", viewAsViewResult.ViewName);
            #endregion
        }

        [Fact]
        public async Task ResetPasswordPOSTReturnsRightFormWhenTokenIsValid()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            ResetPasswordViewModel model = new ResetPasswordViewModel
            {
                Email = "user1@example.com",
                PasswordResetToken = MockUserManager.GoodToken,
                NewPassword = "Password123",
                ConfirmNewPassword = "Password123",
            };
            #endregion

            #region Act
            var view = await controller.ResetPassword(model);
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            var viewAsViewResult = view as ViewResult;
            Assert.Equal("Message", viewAsViewResult.ViewName);
            #endregion
        }

        [Fact]
        public async Task ResetPasswordPOSTReturnsMessageWhenTokenIsInvalid()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            ResetPasswordViewModel model = new ResetPasswordViewModel
            {
                Email = "user1@example.com",
                PasswordResetToken = MockUserManager.BadToken,
                NewPassword = "Password123",
                ConfirmNewPassword = "Password123",
            };
            #endregion

            #region Act
            var view = await controller.ResetPassword(model);
            #endregion

            #region Assert
            var viewAsViewResult = view as ViewResult;
            Assert.Equal("Message", viewAsViewResult.ViewName);
            #endregion
        }

        /// <summary>
        /// Verify that The PasswordRecentDaysValidator rejects passwords within the specified range of recent history
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PasswordInRecentDaysNotAllowed()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            var validator = new PasswordRecentDaysValidator<ApplicationUser>() { numberOfDays = 1 };

            string newPassword = "Passw0rd!";
            var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword));
            AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
            #endregion

            #region Act

            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, newPassword);
            #endregion

            #region Assert
            Assert.False(result.Succeeded);
            #endregion
        }

        /// <summary>
        /// Verify that The PasswordRecentDaysValidator accepts passwords earlier in the user's history than the specified time frame
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PasswordNotInRecentDaysAllowed()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            var validator = new PasswordRecentDaysValidator<ApplicationUser>() { numberOfDays = 1 };

            string newPassword = "Passw0rd!";
            var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword) { dateSetUtc = DateTime.UtcNow.Subtract(new TimeSpan(2, 0, 0, 0)) });
            AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
            #endregion

            #region Act
            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, newPassword);
            #endregion

            #region Assert
            Assert.True(result.Succeeded);
            #endregion
        }

        /// <summary>
        /// Verify that The PasswordRecentNumberValidator rejects passwords within the specified range of recent history
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PasswordInRecentNumberNotAllowed()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            var validator = new PasswordRecentNumberValidator<ApplicationUser>() { numberOfPasswords = 1 };

            string newPassword = "Passw0rd!";
            var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword));
            AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();

            string secondNewPassword = "Passw0rd!2";
            passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(secondNewPassword));
            AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
            #endregion

            #region Act
            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, secondNewPassword);
            #endregion

            #region Assert
            Assert.False(result.Succeeded);
            #endregion
        }

        /// <summary>
        /// Verify that The PasswordRecentNumberValidator accepts passwords earlier in the user's history than the specified number
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PasswordNotInRecentNumberAllowed()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            var validator = new PasswordRecentNumberValidator<ApplicationUser>() { numberOfPasswords = 1 };

            string newPassword = "Passw0rd!";
            var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword));
            AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();

            string secondNewPassword = "Passw0rd!2";
            passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(secondNewPassword));
            AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
            #endregion

            #region Act
            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, newPassword);
            #endregion

            #region Assert
            Assert.True(result.Succeeded);
            #endregion
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
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            var validator = new PasswordHistoryValidator<ApplicationUser>();

            string newPassword = "Passw0rd!";
            var passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(newPassword));
            AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();

            string secondNewPassword = "Passw0rd!2";
            passwordHistory = AppUser.PasswordHistoryObj.Append(new PreviousPassword(secondNewPassword));
            AppUser.PasswordHistoryObj = passwordHistory.ToList<PreviousPassword>();
            #endregion

            #region Act
            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, inputPassword);
            #endregion

            #region Assert
            Assert.False(result.Succeeded);
            #endregion
        }

        [Fact]
        public async Task EmailInPasswordNotAllowed()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            var validator = new PasswordIsNotEmailOrUsernameValidator<ApplicationUser>();
            #endregion

            #region Act
            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, AppUser.Email);
            #endregion

            #region Assert
            Assert.False(result.Succeeded);
            #endregion
        }

        [Fact]
        public async Task UsernameInPasswordNotAllowed()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            var validator = new PasswordIsNotEmailOrUsernameValidator<ApplicationUser>();
            #endregion

            #region Act
            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, AppUser.UserName);
            #endregion

            #region Assert
            Assert.False(result.Succeeded);
            #endregion
        }

        /// <summary>
        /// Verify that PasswordContainsCommonWordsValidator will not allow passwords that contain a banned word or phrase
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CommonWordInPasswordNotAllowed()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            var validator = new PasswordContainsCommonWordsValidator<ApplicationUser>() { commonWords = { "milliman" } };
            #endregion

            #region Act
            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, "Milliman123");
            #endregion

            #region Assert
            Assert.False(result.Succeeded);
            #endregion
        }

        [Fact]
        public async Task AccountSettingsGETWorks()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);
            #endregion

            #region Act
            var view = await controller.AccountSettings();
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            Assert.IsType<AccountSettingsViewModel>(viewAsViewResult.Model);
            AccountSettingsViewModel viewModel = viewAsViewResult.Model as AccountSettingsViewModel;
            Assert.Equal(AppUser.Email, viewModel.Email);
            Assert.Equal(AppUser.FirstName, viewModel.FirstName);
            Assert.Equal(AppUser.LastName, viewModel.LastName);
            Assert.Equal(AppUser.UserName, viewModel.UserName);
            Assert.Equal(AppUser.PhoneNumber, viewModel.PhoneNumber);
            Assert.Equal(AppUser.Employer, viewModel.Employer);
            #endregion
        }

        [Fact]
        public async Task AccountSettingsPOSTWorks()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);

            string NewEmployer = "Milliman";
            string FirstName = "MyFirstName";
            string LastName = "MyLastName";
            string Phone = "3173171212";
            AccountSettingsViewModel model = new AccountSettingsViewModel
            {
                UserName = AppUser.UserName,
                Employer = NewEmployer,
                FirstName = FirstName,
                LastName = LastName,
                PhoneNumber = Phone,
            };
            #endregion

            #region Act
            var view = await controller.AccountSettings(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<OkResult>(view);
            Assert.Equal(NewEmployer, UserRecord.Employer);
            Assert.Equal(FirstName, UserRecord.FirstName);
            Assert.Equal(LastName, UserRecord.LastName);
            Assert.Equal(Phone, UserRecord.PhoneNumber);
            #endregion
        }

        [Fact]
        public async Task UpdatePasswordPOSTWorks()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);

            string CurrentPassword = "QWERqwer1234!@$#";
            string NewPassword = "Abcd!@#$1234";
            await TestResources.UserManagerObject.AddPasswordAsync(AppUser, CurrentPassword);

            AccountSettingsViewModel model = new AccountSettingsViewModel
            {
                UserName = AppUser.UserName,
                NewPassword = NewPassword,
                ConfirmNewPassword = NewPassword+"X",
                CurrentPassword = CurrentPassword,
            };
            #endregion

            #region Act
            var view = await controller.UpdatePassword(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<OkResult>(view);
            Assert.Equal(NewPassword + "xyz", UserRecord.PasswordHash);
            #endregion
        }

        [Fact]
        public async Task AccountSettingsPOSTFailsForWrongUser()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);

            string NewEmployer = "Milliman";
            string FirstName = "MyFirstName";
            string LastName = "MyLastName";
            string Phone = "3173171212";
            AccountSettingsViewModel model = new AccountSettingsViewModel
            {
                UserName = "SomeNobody",
                Employer = NewEmployer,
                FirstName = FirstName,
                LastName = LastName,
                PhoneNumber = Phone,
            };
            #endregion

            #region Act
            var view = await controller.AccountSettings(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
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
            #region Arrange
            AccountController controller = GetController();
            #endregion

            #region Act
            var result = await controller.IsLocalAccount(userName);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(result);
            JsonResult typedResult = result as JsonResult;
            PropertyInfo info = typedResult.Value.GetType().GetProperty("localAccount");
            Assert.Equal(typeof(bool), info.PropertyType);
            Assert.Equal(isLocalTruth, (bool)info.GetValue(typedResult.Value));
            #endregion
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
            #region Arrange
            AccountController controller = GetController();
            MockRouter.AddToController(controller, new Dictionary<string, string>() { { "action", "RemoteAuthenticate" }, { "controller", "Account" } });
            #endregion

            #region Act
            var result = await controller.RemoteAuthenticate(userName);
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
}
