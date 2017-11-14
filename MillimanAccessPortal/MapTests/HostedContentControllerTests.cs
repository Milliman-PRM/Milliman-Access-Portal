/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Perform unit tests against HostedContentController
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;
using System.Linq;
using System.Collections.Generic;
using Moq;
using Xunit;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.DataQueries;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QlikviewLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MillimanAccessPortal.Models.HostedContentViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MapTests
{
    public class HostedContentControllerTests
    {
        private IOptions<QlikviewConfig> MockQlikViewConfig { get; set; }
        private Mock<ApplicationDbContext> MockDataContext { get; set; }
        private readonly Mock<UserManager<ApplicationUser>> MockUserManager;
        private readonly ILoggerFactory Logger;

        /// <summary>
        /// Constructor sets up requirements and data for the controller being tested.
        /// </summary>
        public HostedContentControllerTests()
        {
            // Mock requirements for HostedContentController

            MockQlikViewConfig = new Mock<IOptions<QlikviewConfig>>().Object;
            Logger = new LoggerFactory();

            // Configure DataContext required by injected services
            MockDataContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());

            // Configure UserManager to avoid accessing a database
            Mock<IUserStore<ApplicationUser>> MockUserStore = new Mock<IUserStore<ApplicationUser>>();
            MockUserManager = new Mock<UserManager<ApplicationUser>>(MockUserStore.Object, null, null, null, null, null, null, null, null);
            MockUserManager.Setup(m => m.GetUserName(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("test1");
        }

        /// <summary>
        /// Test that the Index returns a view containing a list of content items the test user can access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void Index_ReturnsAViewResult()
        {
            // Reference https://msdn.microsoft.com/en-us/library/dn314429(v=vs.113).aspx

            #region Arrange

            #region Mock Context
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            var MockContext = new Mock<ApplicationDbContext>();
            #endregion

            #region Mock Roles
            Mock<DbSet<ApplicationRole>> MoqApplicationRole = MockDbSet<ApplicationRole>.New(GetSystemRolesList());
            MockContext.Setup(m => m.ApplicationRole).Returns(MoqApplicationRole.Object);
            #endregion

            #region Mock Users
            Mock<DbSet<ApplicationUser>> MoqApplicationUser = MockDbSet<ApplicationUser>.New(new List<ApplicationUser>
                {
                    new ApplicationUser {Id=1, UserName="test1", Email="test1@example.com", Employer ="example", FirstName="FN1", 
                                         LastName="LN1", NormalizedEmail="test@example.com".ToUpper(), PhoneNumber="3171234567"},
                    new ApplicationUser {Id=2, UserName="test2", Email="test2@example.com", Employer ="example", FirstName="FN2",
                                         LastName="LN2", NormalizedEmail ="test@example.com".ToUpper(), PhoneNumber="3171234567"},
                });
            MockContext.Setup(m => m.ApplicationUser).Returns(MoqApplicationUser.Object);
            #endregion

            #region Mock ContentType
            Mock<DbSet<ContentType>> MoqContentType = MockDbSet<ContentType>.New(new List<ContentType>
                {
                    new ContentType{Id=1, Name="Qlikview", CanReduce=true},
                });
            MockContext.Setup(m => m.ContentType).Returns(MoqContentType.Object);
            #endregion

            #region Mock ProfitCenters
            Mock<DbSet<ProfitCenter>> MoqProfitCenter = MockDbSet<ProfitCenter>.New(new List<ProfitCenter>
                {
                    new ProfitCenter {Id=1, Name="Profit Center 1", ProfitCenterCode="pc1" },
                    new ProfitCenter {Id=2, Name="Profit Center 2", ProfitCenterCode="pc2" },
                });
            MockContext.Setup(m => m.ProfitCenter).Returns(MoqProfitCenter.Object);
            #endregion

            #region Mock Clients
            Mock<DbSet<Client>> MoqClient = MockDbSet<Client>.New(new List<Client>
                {
                    new Client {Id=1, Name="Name1", ClientCode="ClientCode1", ProfitCenterId=1, ParentClientId=null },
                    new Client {Id=2, Name="Name2", ClientCode="ClientCode2", ProfitCenterId=1, ParentClientId=1 },
                });
            MockDbSet<Client>.AssignNavigationProperty<ProfitCenter>(MoqClient, "ProfitCenterId", MoqProfitCenter);
            MockContext.Setup(m => m.Client).Returns(MoqClient.Object);
            #endregion

            #region Mock UserRoleForClient
            Mock<DbSet<UserAuthorizationToClient>> MoqUserAuthorizationToClient = MockDbSet<UserAuthorizationToClient>.New(new List<UserAuthorizationToClient>
                {
                    new UserAuthorizationToClient {Id = 1, ClientId=1, RoleId=2, UserId=1},
                });
            MockDbSet<UserAuthorizationToClient>.AssignNavigationProperty<Client>(MoqUserAuthorizationToClient, "ClientId", MoqClient);
            MockDbSet<UserAuthorizationToClient>.AssignNavigationProperty<ApplicationUser>(MoqUserAuthorizationToClient, "UserId", MoqApplicationUser);
            MockDbSet<UserAuthorizationToClient>.AssignNavigationProperty<ApplicationRole>(MoqUserAuthorizationToClient, "RoleId", MoqApplicationRole);
            MockContext.Setup(m => m.UserRoleForClient).Returns(MoqUserAuthorizationToClient.Object);
            #endregion

            #region Mock RootContentItem
            Mock<DbSet<RootContentItem>> MoqRootContentItem = MockDbSet<RootContentItem>.New(new List<RootContentItem>
                {
                    new RootContentItem{Id = 1, ClientIdList=new long[]{1}, ContentName="RootContent 1"},
                    new RootContentItem{Id = 2, ClientIdList=new long[]{2}, ContentName="RootContent 2"},
                });
            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(MoqRootContentItem, "ContentTypeId", MoqContentType);
            MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(MoqRootContentItem, "ClientId", MoqClient);
            MockContext.Setup(m => m.RootContentItem).Returns(MoqRootContentItem.Object);
            #endregion

            #region Mock HierarchyFieldValue
            Mock<DbSet<HierarchyFieldValue>> MoqHierarchyFieldValue = MockDbSet<HierarchyFieldValue>.New(new List<HierarchyFieldValue>
                {
                    new HierarchyFieldValue {Id = 1, HierarchyLevel=1, ParentHierarchyFieldValueId=null, RootContentItemId=1},
                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<RootContentItem>(MoqHierarchyFieldValue, "RootContentItemId", MoqRootContentItem);
            MockContext.Setup(m => m.HierarchyFieldValue).Returns(MoqHierarchyFieldValue.Object);
            #endregion

            #region Mock HierarchyField
            Mock<DbSet<HierarchyField>> MoqHierarchyField = MockDbSet<HierarchyField>.New(new List<HierarchyField>
                {
                    new HierarchyField {Id = 1, RootContentItemId=1, HierarchyLevel=0},
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(MoqHierarchyField, "RootContentItemId", MoqRootContentItem);
            MockContext.Setup(m => m.HierarchyField).Returns(MoqHierarchyField.Object);
            #endregion

            #region Mock ContentItemUserGroups
            Mock<DbSet<ContentItemUserGroup>> MoqContentItemUserGroup = MockDbSet<ContentItemUserGroup>.New(new List<ContentItemUserGroup>
                {
                    new ContentItemUserGroup {Id = 1, ClientId=1, ContentInstanceUrl="Folder1/File1", RootContentItemId=1, GroupName="Group1 For Content1"},
                    new ContentItemUserGroup {Id = 2, ClientId=1, ContentInstanceUrl="Folder1/File2", RootContentItemId=1, GroupName="Group2 For Content1"},
                    new ContentItemUserGroup {Id = 3, ClientId=2, ContentInstanceUrl="Folder2/File1", RootContentItemId=2, GroupName="Group1 For Content2"},
                });
            MockDbSet<ContentItemUserGroup>.AssignNavigationProperty<RootContentItem>(MoqContentItemUserGroup, "RootContentItemId", MoqRootContentItem);
            MockDbSet<ContentItemUserGroup>.AssignNavigationProperty<Client>(MoqContentItemUserGroup, "ClientId", MoqClient);
            MockContext.Setup(m => m.ContentItemUserGroup).Returns(MoqContentItemUserGroup.Object);
            #endregion

            #region Mock UserInContentItemUserGroups
            Mock<DbSet<UserInContentItemUserGroup>> MoqUserInContentItemUserGroup = MockDbSet<UserInContentItemUserGroup>.New(new List<UserInContentItemUserGroup>
                {
                    new UserInContentItemUserGroup {Id = 1, ContentItemUserGroupId=1, RoleId=1, UserId=1},
                });
            MockDbSet<UserInContentItemUserGroup>.AssignNavigationProperty<ContentItemUserGroup>(MoqUserInContentItemUserGroup, "ContentItemUserGroupId", MoqContentItemUserGroup);
            MockDbSet<UserInContentItemUserGroup>.AssignNavigationProperty<ApplicationUser>(MoqUserInContentItemUserGroup, "UserId", MoqApplicationUser);
            MockDbSet<UserInContentItemUserGroup>.AssignNavigationProperty<ApplicationRole>(MoqUserInContentItemUserGroup, "RoleId", MoqApplicationRole);
            MockContext.Setup(m => m.UserRoleForContentItemUserGroup).Returns(MoqUserInContentItemUserGroup.Object);
            #endregion

            StandardQueries QueriesObj = new StandardQueries(MockContext.Object, MockUserManager.Object);

            HostedContentController sut = new HostedContentController(MockQlikViewConfig, 
                                                                      MockUserManager.Object, 
                                                                      Logger, 
                                                                      MockContext.Object,
                                                                      QueriesObj);

            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserName: MoqApplicationUser.Object.First().UserName);
            #endregion

            #region Act
            var view = sut.Index();
            #endregion

            #region Assert
            Assert.IsType(typeof(ViewResult), view);

            ViewResult viewResult = view as ViewResult;
            Assert.IsType(typeof(List<HostedContentViewModel>), viewResult.Model);

            List<HostedContentViewModel> ModelReturned = (List<HostedContentViewModel>)viewResult.Model;
            Assert.Equal(1, ModelReturned.Count);

            Assert.Equal(MoqRootContentItem.Object.FirstOrDefault().ContentName, ModelReturned[0].ContentName);
            #endregion
        }

        /// <summary>
        /// Test that WebHostedContent(id) results in an error page when the user is not authorized to the content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void WebHostedContent_ErrorWhenNotAuthorized()
        {
            // Attempt to load the content view for unauthorized content

            // Test that the ErrorController was loaded instead

            // TODO: Boilerplate. Remove when the test is written.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test that WebHostedContent(id) displays the content view when the user is authorized to the content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public Task WebHostedContent_DisplaysWhenAuthorized()
        {
            // Attempt to load the content view for authorized content

            // Test that the content view was returned

            // TODO: Boilerplate. Remove when the test is written.
            throw new NotImplementedException();
        }

        private List<ApplicationRole> GetSystemRolesList()
        {
            List<ApplicationRole> ReturnList = new List<ApplicationRole>();

            foreach (var x in ApplicationRole.MapRoles)
            {
                ReturnList.Add(new ApplicationRole { Id = (long)x.Key, RoleEnum = x.Key, Name = x.Value, NormalizedName = x.Value.ToString() });
            }
            return ReturnList;
        }
    }
}
