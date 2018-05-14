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
  AssignedUsers: Array<UserInfo>;
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
  DetailList: Array<RootContentItemSummary>;
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
  ContentType: ContentType;
  AvailableContentTypes: Array<ContentType>;
  DoesReduce: boolean;
  Description: string;
  Notes: string;
}

export class RootContentItemStatus {
  Status: {
    [id: number]: PublicationStatus;
  }
}
