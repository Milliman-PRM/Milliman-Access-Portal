/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Public API of Qlikview functionality, including overrides of MapCommonLib and type specific items
 * DEVELOPER NOTES: This API should typically provide relatively thin API methods and invoke methods from the .internal namespace
 */

using Serilog;
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
        QlikviewConfig _config;

        private IQMS _newQvsClient => QmsClientCreator.New(_config?.QvsQmsApiUrl);

        private IQMS _newQdsClient => QmsClientCreator.New(_config?.QdsQmsApiUrl);

        public QlikviewLibApi (QlikviewConfig configArg)
        {
            _config = configArg;
        }

        public async Task<ServiceInfo> SafeGetServiceInfo(ServiceTypes serviceType, int Index = 0)
        {
            try
            {
                ServiceInfo[] Services = new ServiceInfo[0];
                switch (serviceType)
                {
                    case ServiceTypes.QlikViewServer:
                        Services = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, ServiceInfo[]>(async () => await _newQvsClient.GetServicesAsync(serviceType), 2, 250);
                        break;
                    case ServiceTypes.QlikViewDistributionService:
                        Services = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, ServiceInfo[]>(async () => await _newQdsClient.GetServicesAsync(serviceType), 2, 250);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"QlikviewLibApi.SafeGetServiceInfo called with not implemented Service Type <{serviceType.ToString()}>");
                }
                return
                    Services.Count() > Index
                    ? Services[Index]
                    : null;
            }
            catch (System.Exception)
            {
                return null;
            }

        }

        /// <summary>
        /// Gets the Qlikview server object representing the 
        /// </summary>
        /// <param name="ServiceIndex"></param>
        /// <returns>Tuple with the ServiceInfo and DocumentFolder objects</returns>
        public async Task<(ServiceInfo, DocumentFolder)> SafeGetUserDocFolder(int ServiceIndex = 0)
        {
            ServiceInfo SvcInfo = await SafeGetServiceInfo(ServiceTypes.QlikViewServer, ServiceIndex);

            DocumentFolder QvsUserDocFolder = null;
            try
            {
                DocumentFolder[] QvsUserDocFolders = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentFolder[]>(
                    async () => await _newQvsClient.GetUserDocumentFoldersAsync(SvcInfo.ID, DocumentFolderScope.General), 2, 200);
                QvsUserDocFolder = QvsUserDocFolders.Single(f => f.General.Path == _config.QvServerContentUriSubfolder);
            }
            catch (System.Exception)
            {}

            return (SvcInfo, QvsUserDocFolder);
        }

        /// <summary>
        /// Qlikview specific implementation of abstract base class method declaration
        /// </summary>
        /// <param name="FilePathRelativeToContentRoot"></param>
        /// <param name="UserName"></param>
        /// <param name="ConfigInfoArg"></param>
        /// <param name="thisHttpRequest"></param>
        /// <returns></returns>
        public override async Task<UriBuilder> GetContentUri(string FilePathRelativeToContentRoot, string UserName, HttpRequest thisHttpRequest)
        {
            string ContentUrl = string.IsNullOrWhiteSpace(_config.QvServerContentUriSubfolder) 
                ? FilePathRelativeToContentRoot 
                : Path.Combine(_config.QvServerContentUriSubfolder, FilePathRelativeToContentRoot);

            string QvServerUriScheme = "https";  // Scheme of the iframe should match scheme of the top page

            string QlikviewWebTicket = await QvServerOperations.GetQvWebTicket(/*@"Custom\" +*/ UserName, _config);

            UriBuilder backUriBuilder = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = thisHttpRequest.Host.Host ?? "localhost",  // localhost is probably error in production but won't crash
                Port = thisHttpRequest.Host.Port ?? -1,
                Path = $"/Shared/Message",
                Query = "Msg=An error occurred while loading this content. Please contact MAP support if this problem persists",
            };
            string[] QueryStringItems = new string[]
            {
                $"type=html",
                $"try=/qvajaxzfc/opendoc.htm?document={ContentUrl}",
                $"webticket={QlikviewWebTicket}",
            };

            UriBuilder QvServerUri = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = _config.QvServerHost,
                Path = "/qvajaxzfc/Authenticate.aspx",
                Query = string.Join("&", QueryStringItems),
            };

            await AssignDocumentUserLicense(FilePathRelativeToContentRoot, UserName);

            return QvServerUri;
        }

        /// <summary>
        /// Assigns a Qlikview named user CAL or document CAL as appropriate, if none is already assigned
        /// </summary>
        /// <param name="DocumentFilePathRelativeToStorageContentRoot"></param>
        /// <param name="UserName"></param>
        /// <param name="ConfigInfo"></param>
        /// <returns>true if user has a CAL for the document, false otherwise</returns>
        public async Task<bool> AssignDocumentUserLicense(string DocumentFilePathRelativeToStorageContentRoot, string UserName)
        {
            if (string.IsNullOrWhiteSpace(DocumentFilePathRelativeToStorageContentRoot))
            {
                return false;
            }

            string DocumentRelativeFolderPath = Path.GetDirectoryName(DocumentFilePathRelativeToStorageContentRoot);
            string DocumentFileName = Path.GetFileName(DocumentFilePathRelativeToStorageContentRoot);

            (ServiceInfo SvcInfo, DocumentFolder QvsUserDocFolder) = await SafeGetUserDocFolder(0);
            if (QvsUserDocFolder == null)
            {
                return false;
            }

            // If user has an available named CAL then don't allocate a document CAL.
            CALConfiguration CalConfig = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, CALConfiguration>(
                async () => await _newQvsClient.GetCALConfigurationAsync(SvcInfo.ID, CALConfigurationScope.NamedCALs), 2, 250);
            if (CalConfig.NamedCALs.AssignedCALs.Any(c => string.Compare(c.UserName, UserName, true) == 0 
                                                       && c.QuarantinedUntil == DateTime.MinValue))
            {
                Log.Information($"User {UserName} already has an assigned Qlikview named CAL, new license not assigned");
                return true;
            }

            // Define new CAL, will be added as named cal or doc cal
            AssignedNamedCAL NewCal = new AssignedNamedCAL { UserName = UserName };

            // Decide whether the username qualifies for a named user CAL
            List<string> DomainList = _config.QvNamedCalDomainList?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>();
            List<string> UsernameList = _config.QvNamedCalUsernameList?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>();

            if (GlobalFunctions.DoesEmailSatisfyClientWhitelists(UserName.Trim(), DomainList, UsernameList))
            {
                if (CalConfig.NamedCALs.Limit > CalConfig.NamedCALs.AssignedCALs.Length)
                {
                    try
                    {
                        List<AssignedNamedCAL> NamedCalList = CalConfig.NamedCALs.AssignedCALs.ToList();
                        NamedCalList.Add(NewCal);
                        CalConfig.NamedCALs.AssignedCALs = NamedCalList.ToArray();
                        await StaticUtil.DoRetryAsyncOperation<AggregateException>(
                            async () => await _newQvsClient.SaveCALConfigurationAsync(CalConfig), 2, 250);
                        Log.Information($"Assigned Qlikview named CAL to user {UserName}, there are now {CalConfig.NamedCALs.AssignedCALs.Length} assigned named CALs");
                        return true;
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e, $"Failed to assign Qlikview named CAL to user {UserName}, proceeding to document CAL assignment");
                    }
                }
                else
                {
                    Log.Warning($"Unable to assign a Qlikview named CAL, limit of {CalConfig.NamedCALs.Limit} would be exceeded, proceeding to document CAL assignment");
                }
            }

            DocumentNode RequestedDocNode = null;
            try
            {
                DocumentNode[] AllDocNodesInRequestedFolder = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentNode[]>(
                    async () => await _newQvsClient.GetUserDocumentNodesAsync(SvcInfo.ID, QvsUserDocFolder.ID, DocumentRelativeFolderPath), 2, 250);
                RequestedDocNode = AllDocNodesInRequestedFolder.FirstOrDefault(n => n.Name.Equals(DocumentFileName, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (System.Exception)
            {
                return false;
            }

            if (RequestedDocNode == null)
            {
                return false;
            }

            DocumentMetaData DocMetadata = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentMetaData>(
                async () => await _newQvsClient.GetDocumentMetaDataAsync(RequestedDocNode, DocumentMetaDataScope.Licensing), 2, 250);
            List<AssignedNamedCAL> CurrentDocCals = DocMetadata.Licensing.AssignedCALs.ToList();

            if (CurrentDocCals.Any(c => string.Compare(c.UserName, UserName, true) == 0
                                     && c.QuarantinedUntil == DateTime.MinValue))
            {
                Log.Information($"User {UserName} already has an assigned Qlikview document CAL, new license not assigned");
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
                await StaticUtil.DoRetryAsyncOperation<AggregateException>(
                    async () => await _newQvsClient.SaveDocumentMetaDataAsync(DocMetadata), 2, 250);
                Log.Information($"Assigned Qlikview document CAL to user {UserName}");
            }
            catch (System.Exception e)
            {
                Log.Information(e, $@"Failed to save document CAL for user {UserName}, document {DocumentRelativeFolderPath}\{DocumentFileName}");
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

        public async Task<bool> ReclaimAllDocCalsForFile(string DocumentFilePathRelativeToStorageContentRoot)
        {
            if (string.IsNullOrWhiteSpace(DocumentFilePathRelativeToStorageContentRoot))
            {
                return false;
            }

            string DocumentRelativeFolderPath = Path.GetDirectoryName(DocumentFilePathRelativeToStorageContentRoot);
            string DocumentFileName = Path.GetFileName(DocumentFilePathRelativeToStorageContentRoot);

            (ServiceInfo SvcInfo, DocumentFolder QvsUserDocFolder) = await SafeGetUserDocFolder(0);
            if (QvsUserDocFolder == null)
            {
                return false;
            }

            DocumentNode RequestedDocNode = null;
            try
            {
                DocumentNode[] AllDocNodesInRequestedFolder = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentNode[]>(
                    async () => await _newQvsClient.GetUserDocumentNodesAsync(SvcInfo.ID, QvsUserDocFolder.ID, DocumentRelativeFolderPath), 2, 250);
                RequestedDocNode = AllDocNodesInRequestedFolder.FirstOrDefault(n => n.Name.Equals(DocumentFileName, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (System.Exception)
            {
                return false;
            }

            if (RequestedDocNode == null)
            {
                return false;
            }

            DocumentMetaData DocMetadata = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentMetaData>(
                async () => await _newQvsClient.GetDocumentMetaDataAsync(RequestedDocNode, DocumentMetaDataScope.Licensing), 2, 250);
            DocMetadata.Licensing.RemovedAssignedCALs = DocMetadata.Licensing.AssignedCALs.ToList().ToArray();
            DocMetadata.Licensing.AssignedCALs = new AssignedNamedCAL[0];
            DocMetadata.Licensing.CALsAllocated = 0;

            await StaticUtil.DoRetryAsyncOperation<AggregateException>(
                async () => await _newQvsClient.SaveDocumentMetaDataAsync(DocMetadata), 2, 250);

            return true;
        }

        public async Task<bool> ReclaimUserDocCalForFile(string DocumentFilePathRelativeToStorageContentRoot, string UserName)
        {
            if (string.IsNullOrWhiteSpace(DocumentFilePathRelativeToStorageContentRoot))
            {
                return false;
            }

            bool ReturnBool = false;

            string DocumentRelativeFolderPath = Path.GetDirectoryName(DocumentFilePathRelativeToStorageContentRoot);
            string DocumentFileName = Path.GetFileName(DocumentFilePathRelativeToStorageContentRoot);

            (ServiceInfo SvcInfo, DocumentFolder QvsUserDocFolder) = await SafeGetUserDocFolder(0);
            if (QvsUserDocFolder == null)
            {
                return false;
            }

            DocumentNode RequestedDocNode = null;
            try
            {
                DocumentNode[] AllDocNodesInRequestedFolder = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentNode[]>(
                    async () => await _newQvsClient.GetUserDocumentNodesAsync(SvcInfo.ID, QvsUserDocFolder.ID, DocumentRelativeFolderPath), 2, 250);
                RequestedDocNode = AllDocNodesInRequestedFolder.FirstOrDefault(n => n.Name.Equals(DocumentFileName, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (System.Exception)
            {
                return false;
            }

            if (RequestedDocNode == null)
            {
                return false;
            }

            DocumentMetaData DocMetadata = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentMetaData>(
                    async () => await _newQvsClient.GetDocumentMetaDataAsync(RequestedDocNode, DocumentMetaDataScope.Licensing), 2, 250);
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

            await StaticUtil.DoRetryAsyncOperation<AggregateException>(
                async () => await _newQvsClient.SaveDocumentMetaDataAsync(DocMetadata), 2, 250);

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
        public async Task AuthorizeUserDocumentsInFolderAsync(string ContentPathRelativeToNamedUserDocFolder, string SpecificFileName = null)
        {
            (ServiceInfo QvsServiceInfo, DocumentFolder QvsUserDocFolder) = await SafeGetUserDocFolder();

            await StaticUtil.DoRetryAsyncOperation<AggregateException>(
                async () => await _newQvsClient.ClearQVSCacheAsync(QVSCacheObjects.UserDocumentList), 2, 250);  // Is this really needed?

            DocumentNode[] AllDocNodesInRequestedFolder = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentNode[]>(
                async () => await _newQvsClient.GetUserDocumentNodesAsync(QvsServiceInfo.ID, QvsUserDocFolder.ID, ContentPathRelativeToNamedUserDocFolder), 2, 250);
            foreach (DocumentNode DocNode in AllDocNodesInRequestedFolder.Where(n => !n.IsSubFolder))
            {
                if (!string.IsNullOrEmpty(SpecificFileName) && DocNode.Name != SpecificFileName)
                    continue;

                DocumentMetaData DocAuthorizationMetadata = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, DocumentMetaData>(
                    async () => await _newQvsClient.GetDocumentMetaDataAsync(DocNode, DocumentMetaDataScope.Authorization), 2, 250);

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
                    await StaticUtil.DoRetryAsyncOperation<AggregateException>(
                        async () => await _newQvsClient.SaveDocumentMetaDataAsync(DocAuthorizationMetadata), 2, 250);
                }
            }
        }

    }
}
