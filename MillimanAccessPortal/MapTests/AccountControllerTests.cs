using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.AccountViewModels;
using System;
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
                null,  // SingInManager<ApplicationUser>
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.AuditLoggerObject,
                TestResources.QueriesObj);

            // Generating ControllerContext will throw a NullReferenceException if the provided user does not exist
            if (!string.IsNullOrWhiteSpace(UserName))
            {
                testController.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: (await TestResources.UserManagerObject.FindByNameAsync(UserName)).UserName);
            }
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        [Fact]
        public async Task EnableAccountGETReturnsRightForm()
        {
            #region Arrange
            AccountController controller = await GetController("user1");
            string TestCode = "Code123";
            string TestUserId = "1";
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
        public async Task ForgotPasswordPOSTReturnsConfirmation()
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
            string TestToken = "abcdefg1234567";
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
                PasswordResetToken = "abcdefg1234567",
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
    }
}
