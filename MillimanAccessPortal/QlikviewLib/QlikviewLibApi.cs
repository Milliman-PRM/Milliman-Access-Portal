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
using MapCommonLib;
using MapCommonLib.ContentTypeSpecific;
using Microsoft.AspNetCore.Http;
using QlikviewLib.Internal;
using QlikviewLib.Qms;

namespace QlikviewLib
{
    public class QlikviewLibApi : ContentTypeSpecificApiBase
    {
        public async Task<ServiceInfo> SafeGetServiceInfo(IQMS Client, ServiceTypes Type, int Index = 0)
        {
            ServiceInfo ServiceInfo = null;
            try
            {
                ServiceInfo[] Services = await Client.GetServicesAsync(Type);
                ServiceInfo = Services[Index];
            }
            catch (System.Exception)
            {}

            return ServiceInfo;
        }

        public async Task<(ServiceInfo, DocumentFolder)> SafeGetUserDocFolder(IQMS Client, QlikviewConfig Cfg, int ServiceIndex = 0)
        {
            ServiceInfo SvcInfo = await SafeGetServiceInfo(Client, ServiceTypes.QlikViewServer, ServiceIndex);
            DocumentFolder QvsUserDocFolder = null;
            try
            {
                DocumentFolder[] QvsUserDocFolders = await Client.GetUserDocumentFoldersAsync(SvcInfo.ID, DocumentFolderScope.General);
                QvsUserDocFolder = QvsUserDocFolders.Single(f => f.General.Path == Cfg.QvServerContentUriSubfolder);
            }
            catch (System.Exception)
            {}

            return (SvcInfo, QvsUserDocFolder);
        }

        public override async Task<UriBuilder> GetContentUri(string FilePathRelativeToContentRoot, string UserName, object ConfigInfoArg, HttpRequest thisHttpRequest)
        {
            QlikviewConfig ConfigInfo = (QlikviewConfig)ConfigInfoArg;
            string ContentUrl = string.IsNullOrWhiteSpace(ConfigInfo.QvServerContentUriSubfolder) 
                ? FilePathRelativeToContentRoot 
                : Path.Combine(ConfigInfo.QvServerContentUriSubfolder, FilePathRelativeToContentRoot);

            string QvServerUriScheme = "https";  // Scheme of the iframe should match scheme of the top page

            string QlikviewWebTicket = await QvServerOperations.GetQvWebTicket(/*@"Custom\" +*/ UserName, ConfigInfo as QlikviewConfig);

            UriBuilder backUriBuilder = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = thisHttpRequest.Host.HasValue
                    ? thisHttpRequest.Host.Host
                    : $"localhost",  // result is probably error in production but won't crash
                Port = thisHttpRequest.Host.Port.HasValue
                    ? thisHttpRequest.Host.Port.Value
                    : -1,
                Path = $"/Shared/Message",
                Query = "Msg=An error occurred while loading this content. Please contact MAP support if this problem persists",
            };
            string[] QueryStringItems = new string[]
            {
                $"type=html",
                $"try=/qvajaxzfc/opendoc.htm?document={ContentUrl}",
                $"back='{backUriBuilder.Uri.OriginalString}'",
                $"webticket={QlikviewWebTicket}",
            };

            UriBuilder QvServerUri = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = ConfigInfo.QvServerHost,
                Path = "/qvajaxzfc/Authenticate.aspx",
                Query = string.Join("&", QueryStringItems),
            };

            await AssignDocumentUserLicense(FilePathRelativeToContentRoot, UserName, ConfigInfo);

            return QvServerUri;
        }

        /// <summary>
        /// Assigns a Qlikview named user CAL or document CAL as appropriate, if none is already assigned
        /// </summary>
        /// <param name="DocumentFilePathRelativeToStorageContentRoot"></param>
        /// <param name="UserName"></param>
        /// <param name="ConfigInfo"></param>
        /// <returns>true if user has a CAL for the document, false otherwise</returns>
        public async Task<bool> AssignDocumentUserLicense(string DocumentFilePathRelativeToStorageContentRoot, string UserName, QlikviewConfig ConfigInfo)
        {
            if (string.IsNullOrWhiteSpace(DocumentFilePathRelativeToStorageContentRoot))
            {
                return false;
            }

            string DocumentRelativeFolderPath = Path.GetDirectoryName(DocumentFilePathRelativeToStorageContentRoot);
            string DocumentFileName = Path.GetFileName(DocumentFilePathRelativeToStorageContentRoot);

            IQMS Client = QmsClientCreator.New(ConfigInfo.QvsQmsApiUrl);

            (ServiceInfo SvcInfo, DocumentFolder QvsUserDocFolder) = await SafeGetUserDocFolder(Client, ConfigInfo, 0);
            if (QvsUserDocFolder == null)
            {
                return false;
            }

            // If user has an available named CAL then don't allocate a document CAL.
            CALConfiguration CalConfig = await Client.GetCALConfigurationAsync(SvcInfo.ID, CALConfigurationScope.NamedCALs);
            if (CalConfig.NamedCALs.AssignedCALs.Any(c => string.Compare(c.UserName, UserName, true) == 0 
                                                       && c.QuarantinedUntil == DateTime.MinValue))
            {
                return true;
            }

            // Define new CAL, will be added as named cal or doc cal
            AssignedNamedCAL NewCal = new AssignedNamedCAL { UserName = UserName };

            // Decide whether the username qualifies for a named user CAL
            {
                List<string> DomainList = ConfigInfo.QvNamedCalDomainList?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                for (int Index= 0; Index < DomainList.Count; Index++)
                {
                    DomainList[Index] = DomainList[Index].Trim();
                }
                List<string> UsernameList = ConfigInfo.QvNamedCalUsernameList?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                for (int Index = 0; Index < UsernameList.Count; Index++)
                {
                    UsernameList[Index] = UsernameList[Index].Trim();
                }

                if (GlobalFunctions.DoesEmailSatisfyClientWhitelists(UserName.Trim(), DomainList, UsernameList))
                {
                    List<AssignedNamedCAL> NamedCalList = CalConfig.NamedCALs.AssignedCALs.ToList();
                    NamedCalList.Add(NewCal);
                    CalConfig.NamedCALs.AssignedCALs = NamedCalList.ToArray();
                    await Client.SaveCALConfigurationAsync(CalConfig);
                    return true;
                }
            }

            DocumentNode RequestedDocNode = null;
            try
            {
                DocumentNode[] AllDocNodesInRequestedFolder = await Client.GetUserDocumentNodesAsync(SvcInfo.ID, QvsUserDocFolder.ID, DocumentRelativeFolderPath);
                RequestedDocNode = AllDocNodesInRequestedFolder.FirstOrDefault(n => n.Name.ToLower() == DocumentFileName.ToLower());
            }
            catch (System.Exception)
            {
                return false;
            }

            if (RequestedDocNode == null)
            {
                return false;
            }

            DocumentMetaData DocMetadata = await Client.GetDocumentMetaDataAsync(RequestedDocNode, DocumentMetaDataScope.Licensing);
            List<AssignedNamedCAL> CurrentDocCals = DocMetadata.Licensing.AssignedCALs.ToList();

            if (CurrentDocCals.Any(c => string.Compare(c.UserName, UserName, true) == 0
                                     && c.QuarantinedUntil == DateTime.MinValue))
            {
                return true; // user already has a doc CAL for this file, dont assign another
            }

            if (CurrentDocCals.Count >= DocMetadata.Licensing.CALsAllocated)
            {
                DocMetadata.Licensing.CALsAllocated = CurrentDocCals.Count + 1;
            }

            CurrentDocCals.Add(NewCal);

            DocMetadata.Licensing.AssignedCALs = CurrentDocCals.ToArray();

            try
            {
                await Client.SaveDocumentMetaDataAsync(DocMetadata);
            }
            catch (System.Exception e)
            {
                if (e.Message.Contains("Too many document CALs allocated"))
                {
                    return false;
                }
                else  // Handle specific errors on case by case basis as we learn about them
                {
                    throw;
                }
            }

            return true;
        }

        public async Task<bool> ReclaimAllDocCalsForFile(string DocumentFilePathRelativeToStorageContentRoot, QlikviewConfig ConfigInfo)
        {
            if (string.IsNullOrWhiteSpace(DocumentFilePathRelativeToStorageContentRoot))
            {
                return false;
            }

            string DocumentRelativeFolderPath = Path.GetDirectoryName(DocumentFilePathRelativeToStorageContentRoot);
            string DocumentFileName = Path.GetFileName(DocumentFilePathRelativeToStorageContentRoot);

            IQMS Client = QmsClientCreator.New(ConfigInfo.QvsQmsApiUrl);

            (ServiceInfo SvcInfo, DocumentFolder QvsUserDocFolder) = await SafeGetUserDocFolder(Client, ConfigInfo, 0);
            if (QvsUserDocFolder == null)
            {
                return false;
            }

            DocumentNode RequestedDocNode = null;
            try
            {
                DocumentNode[] AllDocNodesInRequestedFolder = await Client.GetUserDocumentNodesAsync(SvcInfo.ID, QvsUserDocFolder.ID, DocumentRelativeFolderPath);
                RequestedDocNode = AllDocNodesInRequestedFolder.FirstOrDefault(n => n.Name.ToLower() == DocumentFileName.ToLower());
            }
            catch (System.Exception)
            {
                return false;
            }

            if (RequestedDocNode == null)
            {
                return false;
            }

            DocumentMetaData DocMetadata = await Client.GetDocumentMetaDataAsync(RequestedDocNode, DocumentMetaDataScope.Licensing);
            DocMetadata.Licensing.RemovedAssignedCALs = DocMetadata.Licensing.AssignedCALs.ToList().ToArray();
            DocMetadata.Licensing.AssignedCALs = new AssignedNamedCAL[0];
            DocMetadata.Licensing.CALsAllocated = 0;
            await Client.SaveDocumentMetaDataAsync(DocMetadata);

            return true;
        }

        public async Task<bool> ReclaimUserDocCalForFile(string DocumentFilePathRelativeToStorageContentRoot, string UserName, QlikviewConfig ConfigInfo)
        {
            if (string.IsNullOrWhiteSpace(DocumentFilePathRelativeToStorageContentRoot))
            {
                return false;
            }

            bool ReturnBool = false;

            string DocumentRelativeFolderPath = Path.GetDirectoryName(DocumentFilePathRelativeToStorageContentRoot);
            string DocumentFileName = Path.GetFileName(DocumentFilePathRelativeToStorageContentRoot);

            IQMS Client = QmsClientCreator.New(ConfigInfo.QvsQmsApiUrl);

            (ServiceInfo SvcInfo, DocumentFolder QvsUserDocFolder) = await SafeGetUserDocFolder(Client, ConfigInfo, 0);
            if (QvsUserDocFolder == null)
            {
                return false;
            }

            DocumentNode RequestedDocNode = null;
            try
            {
                DocumentNode[] AllDocNodesInRequestedFolder = await Client.GetUserDocumentNodesAsync(SvcInfo.ID, QvsUserDocFolder.ID, DocumentRelativeFolderPath);
                RequestedDocNode = AllDocNodesInRequestedFolder.FirstOrDefault(n => n.Name.ToLower() == DocumentFileName.ToLower());
            }
            catch (System.Exception)
            {
                return false;
            }

            if (RequestedDocNode == null)
            {
                return false;
            }

            DocumentMetaData DocMetadata = await Client.GetDocumentMetaDataAsync(RequestedDocNode, DocumentMetaDataScope.Licensing);
            List<AssignedNamedCAL> CurrentDocCals = DocMetadata.Licensing.AssignedCALs.ToList();
            List<AssignedNamedCAL> RemovableCALs = new List<AssignedNamedCAL>();

            for (int CalCounter = 0; CalCounter < CurrentDocCals.Count; CalCounter++)
            {
                if (string.Compare(CurrentDocCals.ElementAt(CalCounter).UserName, UserName, true) == 0)
                {
                    RemovableCALs.Add(CurrentDocCals.ElementAt(CalCounter));
                    CurrentDocCals.RemoveAt(CalCounter);
                    ReturnBool = true;
                }
            }

            DocMetadata.Licensing.CALsAllocated = CurrentDocCals.Count;
            DocMetadata.Licensing.AssignedCALs = CurrentDocCals.ToArray();
            DocMetadata.Licensing.RemovedAssignedCALs = RemovableCALs.ToArray();

            await Client.SaveDocumentMetaDataAsync(DocMetadata);

            return ReturnBool;
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
