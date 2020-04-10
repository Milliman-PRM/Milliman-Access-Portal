/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the file upload controller
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ContentPublishing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using TestResourcesLib;

namespace MapTests
{
    public class FileUploadControllerTests
    {
        internal TestInitialization TestResources { get; set; }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentAccessAdminController</returns>
        public async Task<FileUploadController> GetControllerForUser(string Username)
        {
            var testController = new FileUploadController(
                TestResources.AuditLogger,
                TestResources.DbContext,
                TestResources.UploadHelper,
                TestResources.UploadTaskQueue);

            try
            {
                Username = (await TestResources.UserManager.FindByNameAsync(Username)).UserName;
            }
            catch (NullReferenceException)
            {
                throw new ArgumentException($"Username '{Username}' is not present in the test database.");
            }
            testController.ControllerContext = TestResources.GenerateControllerContext(Username);
            testController.HttpContext.Session = new MockSession();

            return testController;
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
            using (var TestResources = await TestInitialization.Create(Guid.NewGuid(), DataSelection.Basic))
            {
                #region Arrange
                FileUploadController controller = await GetControllerForUser("test1");
                var resumableInfo = BuildResumableInfo(fileName, checksum, size, chunkNumber);
                #endregion

                #region Act
                var view = controller.ChunkStatus(resumableInfo);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<List<uint>>(result.Value);
                #endregion
            }
        }

        [Theory]
        [InlineData("random.dat", "2339ebed070fd30a869a22193ef2f76284ed333b", 2097152, 1)]
        public async Task ChunkStatus_Ok(string fileName, string checksum, ulong size, uint chunkNumber)
        {
            using (var TestResources = await TestInitialization.Create(Guid.NewGuid(), DataSelection.Basic))
            {
                #region Arrange
                FileUploadController controller = await GetControllerForUser("test1");
                var resumableInfo = BuildResumableInfo(fileName, checksum, size, chunkNumber);
                #endregion

                #region Act
                var view = controller.ChunkStatus(resumableInfo);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<List<uint>>(result.Value);
                #endregion
            }
        }
    }
}
