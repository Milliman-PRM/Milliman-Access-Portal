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

            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = "abc",
                Port = 123,
            };
            testController.ControllerContext = TestInitialization.GenerateControllerContext(userName: UserName, requestUriBuilder: uriBuilder);
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
            var typedResult = Assert.IsType<RedirectToActionResult>(view);
            Assert.Equal("Login", typedResult.ActionName);
            
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
            ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
            Assert.Equal("UserMessage", viewAsViewResult.ViewName);

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
                Username = controller.HttpContext.User.Identity.Name,
            };
            #endregion

            #region Act
            var view = await controller.EnableAccount(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            RedirectToActionResult typedResult = Assert.IsType<RedirectToActionResult>(view);
            Assert.Equal("Login", typedResult.ActionName);
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
            ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
            Assert.Equal("UserMessage", viewAsViewResult.ViewName);
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
            ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
            Assert.Equal("UserMessage", viewAsViewResult.ViewName);  // This one works because view is named explicitly in controller
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
            ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
            Assert.Equal(nameof(SharedController.UserMessage), viewAsViewResult.ViewName);  // This one works because view is named explicitly in controller
            Assert.IsType<MillimanAccessPortal.Models.SharedModels.UserMessageModel>(viewAsViewResult.Model);
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
            ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
            ResetPasswordViewModel viewModel = Assert.IsType<ResetPasswordViewModel>(viewAsViewResult.Model);
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
            ViewResult viewAsViewResult = Assert.IsType<ViewResult>(view);
            Assert.Equal("UserMessage", viewAsViewResult.ViewName);
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
            Assert.Equal("UserMessage", viewAsViewResult.ViewName);
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
            Assert.Equal("UserMessage", viewAsViewResult.ViewName);
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
            var validator = new PasswordIsNotEmailValidator<ApplicationUser>();
            #endregion

            #region Act
            IdentityResult result = await validator.ValidateAsync(TestResources.UserManagerObject, AppUser, AppUser.Email);
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
            #endregion
        }

        [Fact]
        public async Task UpdateAccountPOSTWorksForUserInformation()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);

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
                Password = null,
            };
            #endregion

            #region Act
            var view = await controller.UpdateAccount(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            Assert.Equal(NewEmployer, UserRecord.Employer);
            Assert.Equal(NewFirstName, UserRecord.FirstName);
            Assert.Equal(NewLastName, UserRecord.LastName);
            Assert.Equal(NewPhone, UserRecord.PhoneNumber);
            #endregion
        }

        [Fact]
        public async Task UpdateAccountPOSTWorksForPasswordChange()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);

            string CurrentPassword = "QWERqwer1234!@$#";
            string NewPassword = "Abcd!@#$1234";
            await TestResources.UserManagerObject.AddPasswordAsync(AppUser, CurrentPassword);

            UpdateAccountModel model = new UpdateAccountModel
            {
                User = null,
                Password = new UpdateAccountModel.PasswordModel
                {
                    New = NewPassword,
                    Confirm = NewPassword,
                    Current = CurrentPassword,
                }
            };
            #endregion

            #region Act
            var view = await controller.UpdateAccount(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            Assert.Equal(NewPassword + "xyz", UserRecord.PasswordHash);
            #endregion
        }

        [Fact]
        public async Task UpdateAccountPOSTFailsForWrongCurrentPassword()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);

            string CurrentPassword = "QWERqwer1234!@$#";
            string NewPassword = "Abcd!@#$1234";
            await TestResources.UserManagerObject.AddPasswordAsync(AppUser, CurrentPassword);

            UpdateAccountModel model = new UpdateAccountModel
            {
                User = null,
                Password = new UpdateAccountModel.PasswordModel
                {
                    New = NewPassword,
                    Confirm = NewPassword,
                    Current = CurrentPassword + "X",
                }
            };
            #endregion

            #region Act
            var view = await controller.UpdateAccount(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<BadRequestResult>(view);
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
            JsonResult typedResult = Assert.IsType<JsonResult>(result);
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

        [Fact]
        public async Task UpdateAccountPOSTFailsForWrongMismatchedPassword()
        {
            #region Arrange
            AccountController controller = GetController("user1");
            var AppUser = await TestResources.UserManagerObject.GetUserAsync(controller.ControllerContext.HttpContext.User);

            string CurrentPassword = "QWERqwer1234!@$#";
            string NewPassword = "Abcd!@#$1234";
            await TestResources.UserManagerObject.AddPasswordAsync(AppUser, CurrentPassword);

            UpdateAccountModel model = new UpdateAccountModel
            {
                User = null,
                Password = new UpdateAccountModel.PasswordModel
                {
                    New = NewPassword,
                    Confirm = NewPassword + "X",
                    Current = CurrentPassword,
                }
            };
            #endregion

            #region Act
            var view = await controller.UpdateAccount(model);
            var UserRecord = TestResources.DbContextObject.ApplicationUser.Single(u => u.UserName == "user1");
            #endregion

            #region Assert
            Assert.IsType<BadRequestResult>(view);
            Assert.Equal(CurrentPassword + "xyz", UserRecord.PasswordHash);
            #endregion
        }
    }
}
