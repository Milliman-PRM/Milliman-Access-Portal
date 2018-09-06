/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Xunit tests for the account controller
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<AccountController> GetController(string UserName = null)
        {
            AccountController testController = new AccountController(TestResources.DbContextObject,
                TestResources.UserManagerObject,
                TestResources.RoleManagerObject,
                null,  // SingInManager<ApplicationUser>
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.AuditLoggerObject,
                TestResources.QueriesObj,
                TestResources.AuthorizationService,
                TestResources.ConfigurationObject,
                TestResources.ServiceProviderObject);

            // Generating ControllerContext will throw a NullReferenceException if the provided user does not exist
            if (!string.IsNullOrWhiteSpace(UserName))
            {
                testController.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: (await TestResources.UserManagerObject.FindByNameAsync(UserName)).UserName);
            }
            testController.HttpContext.Session = new MockSession();
            return testController;
        }

        [Fact]
        public async Task EnableAccountGETReturnsEnableFormWhenNotEnabled()
        {
            #region Arrange
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user2");
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
        public async Task EnableAccountPOSTUpdatesUser()
        {
            #region Arrange
            AccountController controller = await GetController("user1");
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
        public async Task ForgotPasswordPOSTReturnsMessageWhenNotActivated()
        {
            #region Arrange
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user2");
            var model = new ForgotPasswordViewModel
            {
                Email = "user2@example.com"
            };

            // Configure controller's routing
            // This section is required for Url.Action to execute successfully
            var actionContext = new ActionContext()
            {
                HttpContext = controller.HttpContext
            };

            Dictionary<string, string> routeValues = new Dictionary<string, string>() { { "action", "ForgotPassword" }, { "controller", "Account" } };
            RouteValueDictionary valueDictionary = new RouteValueDictionary(routeValues);
            Mock<IRouter> mockRouter = new Mock<IRouter>();
            mockRouter.Setup(m => m.GetVirtualPath(It.IsAny<VirtualPathContext>())).Returns(new VirtualPathData(mockRouter.Object, "/"));
            controller.Url = new UrlHelper(actionContext);
            controller.Url.ActionContext.RouteData = new Microsoft.AspNetCore.Routing.RouteData();
            controller.Url.ActionContext.RouteData.PushState(mockRouter.Object, valueDictionary, null);
            
            #endregion

            #region Act
            var view = await controller.ForgotPassword(model);
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            ViewResult viewAsViewResult = view as ViewResult;
            Assert.Equal("ForgotPasswordConfirmation", viewAsViewResult.ViewName);  // This one works because view is named explicitly in controller
            Assert.IsType<ForgotPasswordViewModel>(viewAsViewResult.Model);
            ForgotPasswordViewModel viewModel = viewAsViewResult.Model as ForgotPasswordViewModel;
            Assert.Equal(model.Email, viewModel.Email);
            #endregion
        }


        [Fact]
        public async Task ResetPasswordGETReturnsRightForm()
        {
            #region Arrange
            AccountController controller = await GetController("user1");
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
        public async Task ResetPasswordPOSTReturnsRightForm()
        {
            #region Arrange
            AccountController controller = await GetController("user1");
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
            Assert.IsType<RedirectToActionResult>(view);
            RedirectToActionResult viewAsViewResult = view as RedirectToActionResult;
            Assert.Equal("ResetPasswordConfirmation", viewAsViewResult.ActionName);
            Assert.Equal("Account", viewAsViewResult.ControllerName);
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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
            AccountController controller = await GetController("user1");
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

    }
}
