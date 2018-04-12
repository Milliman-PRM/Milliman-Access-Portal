/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the content access admin controller
 * DEVELOPER NOTES: 
 */

using MapCommonLib;
using MapDbContextLib.Context;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ContentPublicationViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MapTests
{
    public class ContentPublicationControllerTests
    {
        internal TestInitialization TestResources { get; set; }

        /// <summary>Initializes test resources.</summary>
        /// <remarks>This constructor is called before each test.</remarks>
        public ContentPublicationControllerTests()
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
        [InlineData("3MB.txt", "51a3bcc4149bd86bd6a635d36ecc2d0a39d01f75")]
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
                RootContentItemId = 1,
            };
            resumableData.ChunkSize = (resumableData.ChunkNumber == resumableData.TotalChunks)
                ? ((uint) size) % chunkSize + chunkSize
                : chunkSize;
            return resumableData;
        }

        [Theory]
        [InlineData("nonexistant_3MB.txt", "51a3bcc4149bd86bd6a635d36ecc2d0a39d01f75", 3000000, 1)]
        [InlineData("incomplete_3MB.txt", "51a3bcc4149bd86bd6a635d36ecc2d0a39d01f75", 3000000, 1)]
        [InlineData("3MB.txt", "51a3bcc4149bd86bd6a635d36ecc2d0a39d01f75", 3000000, 2)]
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
            Assert.IsType<NoContentResult>(view);
            #endregion
        }

        [Theory]
        [InlineData("3MB.txt", "51a3bcc4149bd86bd6a635d36ecc2d0a39d01f75", 3000000, 1)]
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
            Assert.IsType<OkResult>(view);
            #endregion
        }

        [Theory]
        [InlineData("3MB.txt", "51a3bcc4149bd86bd6a635d36ecc2d0a39d01f75", 3000000, 1)]
        public async Task RequestContentPublication_Ok(string fileName, string checksum, ulong size, uint chunkNumber)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("test1");
            var resumableInfo = BuildResumableInfo(fileName, checksum, size, chunkNumber);
            #endregion

            #region Act
            var view = await controller.Publish(resumableInfo);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }
    }
}
