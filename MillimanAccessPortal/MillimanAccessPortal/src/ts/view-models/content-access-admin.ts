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
export class ClientDetail extends Nestable {
  Name: string;
  Code: string;

  CanManage: boolean;
  AssignedUsers: Array<UserInfo>;
  EligibleUserCount: number;
  RootContentItemCount: number;
}
export class ClientTree extends BasicTree<ClientDetail> {
  SelectedClientId: number;
}

export enum PublicationStatus {
  Unknown = 0,
  Queued = 10,
  Processing = 20,
  Complete = 30,
}
export class ApplicationUser {
  Id: number;
  LastName: string;
  FirstName: string;
  Employer: string;
}
export class PublicationDetails {
  User: ApplicationUser;
  StatusEnum: PublicationStatus;
  StatusName: string;
  SelectionGroupId: number;
  RootContentItemId: number;
}
export enum ContentTypeEnum {
  Unknown = 0,
  Qlikview = 1,
}
export class ContentType {
  Id: number;
  TypeEnum: ContentTypeEnum;
  Name: string;
  CanReduce: boolean;
}
export class RootContentItem {
  Id: number;
  ContentName: string;
  ContentTypeId: number;
  ContentType: ContentType;
  ClientId: number;
  Client: ClientDetail;
  TypeSpecificDetails: string;
}
export class RootContentItemDetail {
  RootContentItemEntity: RootContentItem;
  GroupCount: number;
  EligibleUserCount: number;
  PublicationDetails: PublicationDetails;
}
export class RootContentItemList {
  RootContentItemList: Array<RootContentItemDetail>;
  RelevantRootContentItemId: number;
}
