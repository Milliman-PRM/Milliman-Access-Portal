/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines an API for common ContentType specific functionality
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.IO;
using System.Threading.Tasks;

namespace MapCommonLib.ContentTypeSpecific
{
    public abstract class ContentTypeSpecificApiBase
    {
        public abstract Task<UriBuilder> GetContentUri(string SelectionGroupUrl, string UserName, object ConfigInfo);

        /// <summary>
        /// Returns a standardized file name for a non-reduced content or related file
        /// </summary>
        /// <param name="FilePurpose"></param>
        /// <param name="ExtensionWithDot"></param>
        /// <param name="rootContentItemId"></param>
        /// <returns></returns>
        public static string GenerateContentFileName(string FilePurpose, string ExtensionWithDot, Guid rootContentItemId)
        {
            string PreHashedFileName = $"{FilePurpose}.Content[{rootContentItemId}]{ExtensionWithDot}";
            return GlobalFunctions.GetStringChecksum(PreHashedFileName);
        }

        /// <summary>
        /// Returns a standard file name for a reduced content file.  May be overridden for needs specific to a ContentType. 
        /// </summary>
        /// <param name="selectionGroupId"></param>
        /// <param name="rootContentItemId"></param>
        /// <param name="extensionWithDot"></param>
        /// <returns></returns>
        public static string GenerateReducedContentFileName(Guid selectionGroupId, Guid rootContentItemId, string extensionWithDot)
        {
            string PreHashedFileName = $"ReducedContent.SelGrp[{selectionGroupId}].Content[{rootContentItemId}]{extensionWithDot}";
            return GlobalFunctions.GetStringChecksum(PreHashedFileName);
        }

        /// <summary>
        /// Returns a standard file name for a pre-live content related file
        /// </summary>
        /// <param name="filePurpose"></param>
        /// <param name="publicationRequestId"></param>
        /// <param name="rootContentItemId"></param>
        /// <param name="extensionWithDot"></param>
        /// <returns></returns>
        public static string GeneratePreliveRelatedFileName(string filePurpose, Guid publicationRequestId, Guid rootContentItemId, string extensionWithDot)
        {
            string PreHashedFileName = $"{filePurpose}.Pub[{publicationRequestId}].Content[{rootContentItemId}]{extensionWithDot}";
            return GlobalFunctions.GetStringChecksum(PreHashedFileName);
        }

    }
}
