import { Guid } from '../react/shared-components/interfaces';

export interface Nestable {
  Id: Guid;
  ParentId?: string;
}

export interface BasicTree<T extends Nestable> {
  Root: BasicNode<T>;
}
export interface BasicNode<T extends Nestable> {
  Value: T;
  Children: Array<BasicNode<T>>;
}

export interface UserInfo {
  Id: Guid;
  LastName: string;
  FirstName: string;
  Email: string;
  UserName: string;
  IsSuspended: boolean;
}
export interface ClientSummary extends Nestable {
  Name: string;
  Code: string;

  CanManage: boolean;
  EligibleUserCount: number;
  RootContentItemCount: number;
}
export interface ClientTree extends BasicTree<ClientSummary> {
  SelectedClientId: Guid;
}

export enum PublicationStatus {
  Unknown = 0,
  Canceled = 1,
  Rejected = 2,
  Validating = 9,
  Queued = 10,
  Processing = 20,
  Processed = 30,
  Confirming = 35,
  Confirmed = 40,
  Replaced = 50,
  Error = 90,
}
export enum ReductionStatus {
  Unspecified = 0,
  Canceled = 1,
  Rejected = 2,
  Validating = 11,
  Queued = 10,
  Reducing = 20,
  Reduced = 30,
  Live = 40,
  Replaced = 50,
  Error = 90,
}
export interface PublicationSummary {
  User: UserInfo;
  StatusEnum: PublicationStatus;
  StatusName: string;
  StatusMessage: string;
  SelectionGroupId: Guid;
  RootContentItemId: Guid;
  QueuedDurationMs?: number;
  QueuePosition?: number;
  QueueTotal?: number;
}
export interface RootContentItemSummary {
  Id: Guid;
  ContentName: string;
  ContentTypeName: string;
  GroupCount: number;
  AssignedUserCount: number;
  IsSuspended: boolean;
  ReadOnly: boolean;
  EligibleUserList: UserInfo[];
  PublicationDetails: PublicationSummary;
}
export interface RootContentItemList {
  SummaryList: RootContentItemSummary[];
  SelectedRootContentItemId: Guid;
}

export enum ContentTypeEnum {
  Unknown = 0,
  Qlikview = 1,
  Html = 2,
  Pdf = 3,
  FileDownload = 4,
}

export interface ContentType {
  Id: Guid;
  TypeEnum: ContentTypeEnum;
  Name: string;
  CanReduce: boolean;
  DefaultIconName: string;
  FileExtensions: string[];
}

export interface RootContentItemDetail {
  Id: Guid;
  ClientId: Guid;
  ContentName: string;
  ContentTypeId: Guid;
  DoesReduce: boolean;
  RelatedFiles: ContentRelatedFile[];
  Description: string;
  Notes: string;
  IsSuspended: boolean;
}

export interface RootContentItemSummaryAndDetail {
  summary: RootContentItemSummary;
  detail: RootContentItemDetail;
}

export interface RootContentItemStatus {
  Status: PublicationSummary[];
}

export interface ContentRelatedFile {
  FileOriginalName: string;
  FilePurpose: string;
  FileUploadId: Guid;
}
export interface PublishRequest {
  RootContentItemId: Guid;
  NewRelatedFiles: ContentRelatedFile[];
  DeleteFilePurposes: string[];
}

export interface PreLiveContentValidationSummary {
  ValidationSummaryId: Guid;
  PublicationRequestId: Guid;
  RootContentName: string;
  ContentTypeName: string;
  ContentDescription: string;
  DoesReduce: boolean;
  ClientName: string;
  ClientCode: string;
  AttestationLanguage: string;
  MasterContentLink: string;
  UserGuideLink: string;
  ReleaseNotesLink: string;
  ThumbnailLink: string;
  LiveHierarchy: ContentReductionHierarchy<ReductionFieldValue>;
  NewHierarchy: ContentReductionHierarchy<ReductionFieldValue>;
  SelectionGroups: SelectionGroupSummary[];
}
export interface SelectionGroupSummary {
  Name: string;
  UserCount: number;
  IsMaster: boolean;
}

export interface ContentReductionHierarchy<T extends ReductionFieldValue> {
  Fields: Array<ReductionField<T>>;
  RootContentItemId: Guid;
}
export interface ReductionField<T extends ReductionFieldValue> {
  Id: Guid;
  FieldName: string;
  DisplayName: string;
  ValueDelimiter: string;
  Values: T[];
}
export interface ReductionFieldValue {
  Id: Guid;
  Value: string;
  HasSelectionStatus: boolean;
}
export interface ReductionFieldValueSelection extends ReductionFieldValue {
  SelectionStatus: boolean;
}
