abstract class Nestable {
  Id: number;
  ParentId?: number;
}

class BasicTree<T extends Nestable> {
  Root: BasicNode<T>;
}
export class BasicNode<T extends Nestable> {
  Value: T;
  Children: Array<BasicNode<T>>;
}

export class UserInfo {
  Id: number;
  LastName: string;
  FirstName: string;
  Email: string;
  UserName: string;
}
export class ClientSummary extends Nestable {
  Name: string;
  Code: string;

  CanManage: boolean;
  AssignedUsers: UserInfo[];
  EligibleUserCount: number;
  RootContentItemCount: number;
}
export class ClientTree extends BasicTree<ClientSummary> {
  SelectedClientId: number;
}

export enum PublicationStatus {
  Unknown = 0,
  Queued = 10,
  Processing = 20,
  Complete = 30,
}
export class PublicationSummary {
  User: UserInfo;
  StatusEnum: PublicationStatus;
  StatusName: string;
  SelectionGroupId: number;
  RootContentItemId: number;
}
export class RootContentItemSummary {
  Id: number;
  ContentName: string;
  ContentTypeName: string;
  GroupCount: number;
  EligibleUserCount: number;
  PublicationDetails: PublicationSummary;
}
export class RootContentItemList {
  DetailList: RootContentItemSummary[];
  SelectedRootContentItemId: number;
}

export class ContentType {
  Id: number;
  TypeEnum: number;
  Name: string;
  CanReduce: boolean;
}

export class RootContentItemDetail {
  Id: number;
  ClientId: number;
  ContentName: string;
  ContentTypeId: number;
  DoesReduce: boolean;
  Description: string;
  Notes: string;
}

export class RootContentItemSummaryAndDetail {
  summary: RootContentItemSummary;
  detail: RootContentItemDetail;
}

export class PublicationDetails {
  User: UserInfo;
  StatusEnum: PublicationStatus;
  StatusName: string;
  RootContentItemId: number;
}
export class RootContentItemStatus {
  Status: PublicationDetails[];
}

export class ContentRelatedFile {
  FilePurpose: string;
  FileUploadId: string;
}
export class PublishRequest {
  RootContentItemId: number;
  RelatedFiles: ContentRelatedFile[];
}

export class PreLiveContentValidationSummary {
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
export class SelectionGroupSummary {
  Name: string;
  UserCount: number;
  IsMaster: boolean;
}

export class ContentReductionHierarchy<T extends ReductionFieldValue> {
  Fields: Array<ReductionField<T>>;
  RootContentItemId: number;
}
export class ReductionField<T extends ReductionFieldValue> {
  Id: number;
  FieldName: string;
  DisplayName: string;
  ValueDelimiter: string;
  Values: T[];
}
export class ReductionFieldValue {
  Id: number;
  Value: string;
  get HasSelectionStatus() {
    return false;
  }
}
export class ReductionFieldValueSelection extends ReductionFieldValue {
  get HasSelectionStatus() {
    return true;
  }
  SelectionStatus: boolean;
}
