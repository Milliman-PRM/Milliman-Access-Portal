interface Nestable {
  Id: number;
  ParentId?: number;
}

interface BasicTree<T extends Nestable> {
  Root: BasicNode<T>;
}
export interface BasicNode<T extends Nestable> {
  Value: T;
  Children: Array<BasicNode<T>>;
}

export interface UserInfo {
  Id: number;
  LastName: string;
  FirstName: string;
  Email: string;
  UserName: string;
}
export interface ClientSummary extends Nestable {
  Name: string;
  Code: string;

  CanManage: boolean;
  AssignedUsers: UserInfo[];
  EligibleUserCount: number;
  RootContentItemCount: number;
}
export interface ClientTree extends BasicTree<ClientSummary> {
  SelectedClientId: number;
}

export enum PublicationStatus {
  Unknown = 0,
  Queued = 10,
  Processing = 20,
  Complete = 30,
}
export interface PublicationSummary {
  User: UserInfo;
  StatusEnum: PublicationStatus;
  StatusName: string;
  SelectionGroupId: number;
  RootContentItemId: number;
}
export interface RootContentItemSummary {
  Id: number;
  ContentName: string;
  ContentTypeName: string;
  GroupCount: number;
  EligibleUserCount: number;
  PublicationDetails: PublicationSummary;
}
export interface RootContentItemList {
  SummaryList: RootContentItemSummary[];
  SelectedRootContentItemId: number;
}

export interface ContentType {
  Id: number;
  TypeEnum: number;
  Name: string;
  CanReduce: boolean;
}

export interface RootContentItemDetail {
  Id: number;
  ClientId: number;
  ContentName: string;
  ContentTypeId: number;
  DoesReduce: boolean;
  Description: string;
  Notes: string;
}

export interface RootContentItemSummaryAndDetail {
  summary: RootContentItemSummary;
  detail: RootContentItemDetail;
}

export interface PublicationDetails {
  User: UserInfo;
  StatusEnum: PublicationStatus;
  StatusName: string;
  RootContentItemId: number;
}
export interface RootContentItemStatus {
  Status: PublicationDetails[];
}

export interface ContentRelatedFile {
  FilePurpose: string;
  FileUploadId: string;
}
export interface PublishRequest {
  RootContentItemId: number;
  RelatedFiles: ContentRelatedFile[];
}

export interface PreLiveContentValidationSummary {
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
  RootContentItemId: number;
}
export interface ReductionField<T extends ReductionFieldValue> {
  Id: number;
  FieldName: string;
  DisplayName: string;
  ValueDelimiter: string;
  Values: T[];
}
export interface ReductionFieldValue {
  Id: number;
  Value: string;
  HasSelectionStatus: boolean;
}
export interface ReductionFieldValueSelection extends ReductionFieldValue {
  SelectionStatus: boolean;
}
