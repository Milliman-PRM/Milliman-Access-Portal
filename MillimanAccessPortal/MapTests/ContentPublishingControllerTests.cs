/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the content publishing controller
 * DEVELOPER NOTES: 
 */

using MapCommonLib;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ContentPublicationViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MapTests
{
    public class ContentPublishingControllerTests
    {
        internal TestInitialization TestResources { get; set; }

        /// <summary>Initializes test resources.</summary>
        /// <remarks>This constructor is called before each test.</remarks>
        public ContentPublishingControllerTests()
        {
            TestResources = new TestInitialization();
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentAccessAdminController</returns>
        public async Task<ContentPublishingController> GetControllerForUser(string Username)
        {
            var testController = new ContentPublishingController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.UploadHelperObject,
                TestResources.LoggerFactory,
                TestResources.QueriesObj
                );

            try
            {
                Username = (await TestResources.UserManagerObject.FindByNameAsync(Username)).UserName;
            }
            catch (NullReferenceException)
            {
                throw new ArgumentException($"Username '{Username}' is not present in the test database.");
            }
            testController.ControllerContext = TestInitialization.GenerateControllerContext(Username);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        [Fact]
        public async Task Index_ReturnsView()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("test1");
            #endregion

            #region Act
            var view = controller.Index();
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            #endregion
        }

        [Theory]
        [InlineData("random.dat", "2339ebed070fd30a869a22193ef2f76284ed333b")]
        public void GetFileChecksum_Match(string fileName, string checksum)
        {
            #region Arrange
            var filePath = Path.Combine("../../../TestData", fileName);
            #endregion

            #region Act
            var fileChecksum = GlobalFunctions.GetFileChecksum(filePath).ToLower();
            #endregion

            #region Assert
            Assert.Equal(fileChecksum, checksum);
            #endregion
        }

        private ResumableInfo BuildResumableInfo(string fileName, string checksum, ulong size, uint chunkNumber)
        {
            const uint chunkSize = (1024 * 1024);
            var resumableData = new ResumableInfo
            {
                ChunkNumber = chunkNumber,
                TotalChunks = ((uint) size) / chunkSize,
                TotalSize = size,
                FileName = fileName,
                UID = $"{String.Join('_', fileName.Split('.'))}-{checksum}",
            };
            resumableData.ChunkSize = (resumableData.ChunkNumber == resumableData.TotalChunks)
                ? ((uint) size) % chunkSize + chunkSize
                : chunkSize;
            return resumableData;
        }

        [Theory]
        [InlineData("nonexistant_random.dat", "2339ebed070fd30a869a22193ef2f76284ed333b", 2097152, 1)]
        [InlineData("incomplete_random.dat", "2339ebed070fd30a869a22193ef2f76284ed333b", 2097152, 1)]
        [InlineData("random.dat", "2339ebed070fd30a869a22193ef2f76284ed333b", 2097152, 2)]
        public async Task ChunkStatus_NotFound(string fileName, string checksum, ulong size, uint chunkNumber)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("test1");
            var resumableInfo = BuildResumableInfo(fileName, checksum, size, chunkNumber);
            #endregion

            #region Act
            var view = controller.ChunkStatus(resumableInfo);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData("random.dat", "2339ebed070fd30a869a22193ef2f76284ed333b", 2097152, 1)]
        public async Task ChunkStatus_Ok(string fileName, string checksum, ulong size, uint chunkNumber)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("test1");
            var resumableInfo = BuildResumableInfo(fileName, checksum, size, chunkNumber);
            #endregion

            #region Act
            var view = controller.ChunkStatus(resumableInfo);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData("random.dat", "2339ebed070fd30a869a22193ef2f76284ed333b", 2097152, 1)]
        public async Task RequestContentPublication_Ok(string fileName, string checksum, ulong size, uint chunkNumber)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("test1");
            var resumableInfo = BuildResumableInfo(fileName, checksum, size, chunkNumber);
            #endregion

            #region Act
            var view = await controller.Publish();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }
    }
}
