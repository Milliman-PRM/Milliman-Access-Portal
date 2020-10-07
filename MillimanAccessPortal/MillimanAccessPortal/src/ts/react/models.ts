import { PublicationStatus, ReductionStatus } from '../view-models/content-publishing';
import { Dict } from './shared-components/redux/store';

export type Guid = string;

export interface User {
  id: Guid;
  isActivated: boolean;
  isSuspended: boolean;
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  userRoles?: Dict<UserRole>;
}
export interface UserFull extends User {
  isLocal: boolean;
  phone: string;
  employer: string;
}
export interface UserRole {
  roleEnum: number;
  roleDisplayValue: string;
  isAssigned: boolean;
}
export interface ProfitCenter {
  id: Guid;
  name: string;
  code: string;
  office: string;
}
export interface Client {
  id: Guid;
  parentId: Guid;
  profitCenterId?: Guid;
  name: string;
  code: string;
  isChild?: boolean;
}
export interface ClientWithStats extends Client {
  canManage?: boolean;
  contentItemCount: number;
  userCount: number;
}
export interface ClientWithEligibleUsers extends ClientWithStats {
  eligibleUsers: Guid[];
}

export interface RootContentItem {
  id: Guid;
  clientId?: Guid;
  contentTypeId?: Guid;
  isSuspended: boolean;
  doesReduce: boolean;
  name: string;
}
export interface RootContentItemWithStats extends RootContentItem {
  selectionGroupCount: number;
  assignedUserCount: number;
}

export interface RelatedFile {
  fileOriginalName: string;
}
export interface RelatedFileUpload extends RelatedFile {
  uniqueUploadId?: string;
  fileUploadId: Guid;
}

export interface AssociatedFileModel {
  id: Guid;
  fileType: number;
  displayName: string;
  fileOriginalName: string;
  sortOrder: string;
}
export interface AssociatedContentItemUpload extends AssociatedFileModel {
  uniqueUploadId: string;
}
export interface AssociatedFilePreviewSummary extends AssociatedFileModel {
  link: string;
}

export interface RelatedFiles {
  MasterContent: RelatedFileUpload;
  Thumbnail: RelatedFileUpload;
  UserGuide: RelatedFileUpload;
  ReleaseNotes: RelatedFileUpload;
  [key: string]: RelatedFileUpload;
}

export interface ContentItemDetail {
  clientId: Guid;
  contentDisclaimer: string;
  contentName: string;
  contentTypeId: Guid;
  contentDescription: string;
  doesReduce: boolean;
  id: Guid;
  isSuspended: boolean;
  contentNotes: string;
  relatedFiles: RelatedFiles;
  associatedFiles: Dict<AssociatedContentItemUpload>;
  thumbnailLink: string;
  typeSpecificDetailObject: {
    bookmarksPaneEnabled?: boolean;
    filterPaneEnabled?: boolean;
    liveEmbedUrl?: string;
    liveReportId?: Guid;
    liveWorkspaceId?: Guid;
    navigationPaneEnabled?: boolean;
    previewEmbedUrl?: string;
    previewReportId?: Guid;
    previewWorkspaceId?: Guid;
  };
}

export interface ContentItemPublicationDetail {
  Id?: Guid;
  ClientId: Guid;
  ContentName: string;
  ContentTypeId: Guid;
  Description: string;
  Notes: string;
  ContentDisclaimer: string;
  DoesReduce?: boolean;
  TypeSpecificDetailObject?: {
    // PowerBi specific:
    BookmarksPaneEnabled?: boolean;
    FilterPaneEnabled?: boolean;
    NavigationPaneEnabled?: boolean;
  };
}

export interface GoLiveViewModel {
  rootContentItemId: Guid;
  publicationRequestId: Guid;
  validationSummaryId: Guid;
}

export interface ContentItemFormErrors {
  clientId?: string;
  contentDisclaimer?: string;
  contentName?: string;
  contentTypeId?: string;
  contentDescription?: string;
  doesReduce?: string;
  id?: string;
  isSuspended?: string;
  contentNotes?: string;
  relatedFiles?: {
    MasterContent?: string;
    Thumbnail?: string;
    UserGuide?: string;
    ReleaseNotes?: string;
  };
  associatedFiles?: {
    [uniqueId: string]: string;
  };
  typeSpecificDetailObject?: {
    bookmarksPaneEnabled?: string;
    filterPaneEnabled?: string;
    liveEmbedUrl?: string;
    liveReportId?: string;
    liveWorkspaceId?: string;
    navigationPaneEnabled?: string;
    previewEmbedUrl?: string;
    previewReportId?: string;
    previewWorkspaceId?: string;
  };
}

export interface RootContentItemWithPublication extends RootContentItemWithStats {
  status: PublicationWithQueueDetails;
}

export interface SelectionGroup {
  id: Guid;
  rootContentItemId?: Guid;
  selectedValues?: Guid[];
  isSuspended: boolean;
  isInactive: boolean;
  isMaster: boolean;
  name: string;
}

export interface SelectionGroupWithAssignedUsers extends SelectionGroup {
  assignedUsers: Guid[];
}

export interface SelectionGroupWithStatus extends SelectionGroup {
  status: ReductionWithQueueDetails;
}

export interface ReductionField {
  id: Guid;
  rootContentItemId?: Guid;
  fieldName: string;
  displayName: string;
}

export interface ReductionFieldValue {
  id: Guid;
  reductionFieldId?: Guid;
  value: string;
}

export interface ContentPublicationRequest {
  id: Guid;
  rootContentItemId: Guid;
  applicationUser: User;
  createDateTimeUtc: string;
  requestStatus: PublicationStatus;
  outcomeMetadata: PublicationRequestOutcomeMetadata;
}

export interface PublicationRequestOutcomeMetadata {
  id: Guid;
  startDateTime: string;
  elapsedTime: string;
  userMessage: string;
  supportMessage: string;
  reductionTaskFailOutcomeList: ReductionTaskOutcomeMetadata[];
  reductionTaskSuccessOutcomeList: ReductionTaskOutcomeMetadata[];
}

export interface ReductionTaskOutcomeMetadata {
  reductionTaskId: Guid;
  elapsedTime: string;
  processingStartedUtc: string;
  outcomeReason: string;
  selectionGroupName: string;
  userMessage: string;
  supportMessage: string;
}

export interface ContentReductionTask {
  id: Guid;
  contentPublicationRequestId?: Guid;
  applicationUser: User;
  selectionGroupId?: Guid;
  selectedValues: Guid[];
  createDateTimeUtc: string;
  taskStatus: ReductionStatus;
  taskStatusMessage: string;
}

export interface ContentType {
  id: Guid;
  canReduce: boolean;
  displayName: string;
  fileExtensions: string[];
}

export interface ContentAssociatedFileType {
  typeEnum: number;
  displayName: string;
  fileExtensions: string[];
}

export interface ReductionFieldset {
  field: ReductionField;
  values: ReductionFieldValue[];
}

export interface PublicationQueueDetails {
  publicationId: Guid;
  queuePosition?: number;
  reductionsCompleted?: number;
  reductionsTotal?: number;
}

export interface ReductionQueueDetails {
  reductionId: Guid;
  queuePosition: number;
}

export interface PublicationWithQueueDetails extends ContentPublicationRequest {
  queueDetails: PublicationQueueDetails;
  applicationUser: User;
  requestStatusName: string;
}

export interface ReductionWithQueueDetails extends ContentReductionTask {
  queueDetails: ReductionQueueDetails;
  applicationUser: User;
  taskStatusName: string;
}

export function isPublicationRequest(
  request: ContentPublicationRequest | ContentReductionTask,
): request is ContentPublicationRequest {
  return request && (request as ContentPublicationRequest).rootContentItemId !== undefined;
}

export function isReductionTask(
  request: ContentPublicationRequest | ContentReductionTask,
): request is ContentReductionTask {
  return request && (request as ContentReductionTask).selectionGroupId !== undefined;
}

export interface PasswordValidation {
  valid: boolean;
  messages?: string[];
}

// File Drop models

export interface FileDropClientWithStats extends Client {
  fileDropCount: number;
  userCount: number;
  canManageFileDrops: boolean;
  authorizedFileDropUser: boolean;
}

export interface FileDrop {
  clientId: Guid;
  id?: Guid;
  name: string;
  description: string;
  isSuspended?: boolean;
}

export interface FileDropWithStats extends FileDrop {
  userCount: number;
}

export interface FileDropsReturnModel {
  clientCard: FileDropClientWithStats;
  fileDrops: Dict<FileDropWithStats>;
  permissionGroups: PermissionGroupsReturnModel;
  currentFileDropId?: Guid;
}

export interface PermissionGroupModel {
  id?: Guid;
  name: string;
  assignedMapUserIds: Guid[];
  assignedSftpAccountIds: Guid[];
  isPersonalGroup: boolean;
  readAccess: boolean;
  writeAccess: boolean;
  deleteAccess: boolean;
}

export interface FileDropEligibleUser {
  id: Guid;
  firstName: string;
  lastName: string;
  userName: string;
  isFileDropAdmin: boolean;
}

export interface PermissionGroupsReturnModel {
  fileDropId: Guid;
  eligibleUsers: Dict<FileDropEligibleUser>;
  permissionGroups: Dict<PermissionGroupModel>;
  clientModel?: FileDropClientWithStats;
}

export interface PGChangeModel {
  id: Guid;
  name: string;
  usersAdded: Guid[];
  usersRemoved: Guid[];
  readAccess: boolean;
  writeAccess: boolean;
  deleteAccess: boolean;
}

export interface PermissionGroupsChangesModel {
  fileDropId: Guid;
  removedPermissionGroupIds: Guid[];
  newPermissionGroups: PermissionGroupModel[];
  updatedPermissionGroups: Dict<PGChangeModel>;
}

export interface AvailableEligibleUsers {
  id: Guid;
  name: string;
  userName: string;
  sortName: string;
}

interface BaseFileDropEvent {
  id: number;
  timeStampUtc: string;
  eventType: string;
  description: string;
  userName: string;
  fullName: string;
}

interface FileDropEventObjectClient {
  clientId: Guid;
  clientName: string;
}

interface FileDropEventObjectFileDrop {
  Id: Guid;
  Name: string;
  Description: string;
}

interface BaseFileDropEventObjectPermissionGroup {
  Id: Guid;
  IsPersonalGroup: boolean;
}

interface FileDropEventObjectPermissionGroupProps {
  Name: string;
  ReadAccess: boolean;
  WriteAccess: boolean;
  DeleteAccess: boolean;
}

type FileDropEventObjectPermissionGroup =
  BaseFileDropEventObjectPermissionGroup & FileDropEventObjectPermissionGroupProps;

interface FileDropEventObjectSftpUser {
  Id: Guid;
  UserName: string;
}

interface FileDropEventObjectMapUser {
  Id: Guid;
  UserName: string;
}

interface FileDropEventObjectDirectory {
  Id: Guid;
  CanonicalFileDropPath: string;
  Description: string;
}

export interface FDEventFDCreated extends BaseFileDropEvent {
  eventData: {
    Client: FileDropEventObjectClient;
    FileDrop: FileDropEventObjectFileDrop;
  };
}

export interface FDEventFDDeleted extends BaseFileDropEvent {
  eventData: {
    Client: FileDropEventObjectClient;
    FileDrop: FileDropEventObjectFileDrop;
    AffectedSftpAccounts: Array<{
      SftpAccount: FileDropEventObjectSftpUser;
      MapUser?: FileDropEventObjectMapUser;
    }>;
  };
}

export interface FDEventFDUpdated extends BaseFileDropEvent {
  eventData: {
    Client: FileDropEventObjectClient;
    NewFileDrop: FileDropEventObjectFileDrop;
    OldFileDrop: FileDropEventObjectFileDrop;
  };
}

export interface FDEventPGCreated extends BaseFileDropEvent {
  eventData: {
    Client: FileDropEventObjectClient;
    FileDrop: FileDropEventObjectFileDrop;
    PermissionGroup: FileDropEventObjectPermissionGroup;
  };
}

export interface FDEventPGDeleted extends BaseFileDropEvent {
  eventData: {
    Client: FileDropEventObjectClient;
    FileDrop: FileDropEventObjectFileDrop;
    PermissionGroup: FileDropEventObjectPermissionGroup;
  };
}

export interface FDEventPGUpdated extends BaseFileDropEvent {
  eventData: {
    Client: FileDropEventObjectClient;
    FileDrop: FileDropEventObjectFileDrop;
    PermissionGroup: BaseFileDropEventObjectPermissionGroup;
    PreviousProperties: FileDropEventObjectPermissionGroupProps;
    UpdatedProperties: FileDropEventObjectPermissionGroupProps;
  };
}

export interface FDEventAccountCreated extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
  };
}

export interface FDEventAccountDeleted extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
  };
}
export interface FDEventAccountAddedToPG extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
    PermissionGroup: FileDropEventObjectPermissionGroup;
  };
}

export interface FDEventAccountRemovedFromPG extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
    PermissionGroup: FileDropEventObjectPermissionGroup;
  };
}

export interface FDEventDirectoryCreated extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
    FileDropDirectory: FileDropEventObjectDirectory;
  };
}

export interface FDEventDirectoryRemoved extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
    FileDropDirectory: FileDropEventObjectDirectory;
  };
}

export interface FDEventFileWriteAuthorized extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
    FileDropDirectory: FileDropEventObjectDirectory;
    FileName: string;
  };
}

export interface FDEventFileReadAuthorized extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
    FileDropDirectory: FileDropEventObjectDirectory;
    FileName: string;
  };
}

export interface FDEventFileDeleteAuthorized extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
    FileDropDirectory: FileDropEventObjectDirectory;
    FileName: string;
  };
}

export interface FDEventFileOrDirectoryRenamed extends BaseFileDropEvent {
  eventData: {
    FileDrop: FileDropEventObjectFileDrop;
    SftpAccount: FileDropEventObjectSftpUser;
    MapUser?: FileDropEventObjectMapUser;
    Type: 'Directory' | 'File';
    From: string;
    To: string;
  };
}

// Union of all File Drop Event interfaces
export type FileDropEvent =
  | FDEventFDCreated
  | FDEventFDDeleted
  | FDEventFDUpdated
  | FDEventPGCreated
  | FDEventPGDeleted
  | FDEventPGUpdated
  | FDEventAccountCreated
  | FDEventAccountDeleted
  | FDEventAccountAddedToPG
  | FDEventAccountRemovedFromPG
  | FDEventDirectoryCreated
  | FDEventDirectoryRemoved
  | FDEventFileWriteAuthorized
  | FDEventFileReadAuthorized
  | FDEventFileDeleteAuthorized
  | FDEventFileOrDirectoryRenamed
  ;

export interface FileDropSettings {
  fingerprint: string;
  isPasswordExpired: boolean;
  isSuspended: boolean;
  assignedPermissionGroupId: Guid;
  notifications: Array<{
    notificationType: FileDropNotificationTypeEnum;
    isEnabled: boolean;
    canModify: boolean;
  }>;
  sftpHost: string;
  sftpPort: string;
  sftpUserName: string;
  userHasPassword: boolean;
  fileDropPassword?: string;
}

export enum FileDropNotificationTypeEnum {
  FileWritten = 0,
}
