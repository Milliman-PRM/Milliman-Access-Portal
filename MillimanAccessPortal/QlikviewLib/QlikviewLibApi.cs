/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Public API of Qlikview functionality, including overrides of MapCommonLib and type specific items
 * DEVELOPER NOTES: This API should typically provide relatively thin API methods and invoke methods from the .internal namespace
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib.Internal;
using QlikviewLib.Qms;

namespace QlikviewLib
{
    public class QlikviewLibApi : ContentTypeSpecificApiBase
    {
        public override async Task<UriBuilder> GetContentUri(string FilePathRelativeToContentRoot, string UserName, object ConfigInfoArg)
        {
            QlikviewConfig ConfigInfo = (QlikviewConfig)ConfigInfoArg;
            string ContentUrl = string.IsNullOrWhiteSpace(ConfigInfo.QvServerContentUriSubfolder) 
                ? FilePathRelativeToContentRoot 
                : Path.Combine(ConfigInfo.QvServerContentUriSubfolder, FilePathRelativeToContentRoot);

            string QvServerUriScheme = "https";  // Scheme of the iframe should match scheme of the top page

            // TODO Resolve the user naming convention for the QV server.  
            string QlikviewWebTicket = await QvServerOperations.GetQvWebTicket(/*@"Custom\" +*/ UserName, ConfigInfo as QlikviewConfig);

            string[] QueryStringItems = new string[]
            {
                $"type=html",
                $"try=/qvajaxzfc/opendoc.htm?document={ContentUrl}",  // TODO use the relative document path/name in the following
                $"back=/",  // TODO probably use something other than "/" (such as a proper error page)
                $"webticket={QlikviewWebTicket}",
            };

            UriBuilder QvServerUri = new UriBuilder
            {
                // Note that the UriBuilder manages the insertion of literal '?' before the query string.  
                // Don't include a query string with '?' in the Path property because the '?' gets UrlEncoded.  
                Scheme = QvServerUriScheme,
                Host = ConfigInfo.QvServerHost,
                Path = "/qvajaxzfc/Authenticate.aspx",
                Query = string.Join("&", QueryStringItems),
            };

            return QvServerUri;
        }

        /// <summary>
        ///   Grants QV server authorization for all QVWs in a specified subfolder of the UserDocuments path named in config param "QvServerContentUriSubfolder"
        ///     Corresponds to document authorization that can be interactively configured in QMC
        /// </summary>
        /// <param name="ContentPathRelativeToNamedUserDocFolder"></param>
        /// <param name="ConfigInfo"></param>
        /// <param name="SpecificFileName">If provided, authorizes only the named file in the designated path</param>
        /// <returns></returns>
        public async Task AuthorizeUserDocumentsInFolder(string ContentPathRelativeToNamedUserDocFolder, QlikviewConfig ConfigInfo, string SpecificFileName = null)
        {
            IQMS Client = QmsClientCreator.New(ConfigInfo.QvsQmsApiUrl);

            ServiceInfo[] QvsServicesArrray = await Client.GetServicesAsync(ServiceTypes.QlikViewServer);
            ServiceInfo QvsServiceInfo = QvsServicesArrray[0];

            DocumentFolder[] QvsUserDocFolders = await Client.GetUserDocumentFoldersAsync(QvsServiceInfo.ID, DocumentFolderScope.General);
            DocumentFolder QvsUserDocFolder = QvsUserDocFolders.Single(f => f.General.Path == ConfigInfo.QvServerContentUriSubfolder);

            await Client.ClearQVSCacheAsync(QVSCacheObjects.UserDocumentList);  // Is this really needed?

            DocumentNode[] AllDocNodesInRequestedFolder = await Client.GetUserDocumentNodesAsync(QvsServiceInfo.ID, QvsUserDocFolder.ID, ContentPathRelativeToNamedUserDocFolder);
            foreach (DocumentNode DocNode in AllDocNodesInRequestedFolder)
            {
                if (!string.IsNullOrEmpty(SpecificFileName) && DocNode.Name != SpecificFileName)
                    continue;

                var DocAuthorizationMetadata = await Client.GetDocumentMetaDataAsync(DocNode, DocumentMetaDataScope.Authorization);

                if (!DocAuthorizationMetadata.Authorization.Access.Any(a => a.UserName == ""))
                {
                    List<DocumentAccessEntry> DAL = DocAuthorizationMetadata.Authorization.Access.ToList();
                    DAL.Add(new DocumentAccessEntry
                    {
                        UserName = "",
                        AccessMode = DocumentAccessEntryMode.Always,
                        DayOfWeekConstraints = new DayOfWeek[0],
                    });
                    DocAuthorizationMetadata.Authorization.Access = DAL.ToArray();
                    await Client.SaveDocumentMetaDataAsync(DocAuthorizationMetadata);
                }
            }
        }

    }
}
