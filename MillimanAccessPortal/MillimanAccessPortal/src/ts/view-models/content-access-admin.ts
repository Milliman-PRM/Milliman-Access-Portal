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
export class PublicationDetails {
  User: UserInfo;
  StatusEnum: PublicationStatus;
  StatusName: string;
  SelectionGroupId: number;
  RootContentItemId: number;
}
export class RootContentItemDetail {
  Id: number;
  ContentName: string;
  ContentTypeName: string;
  GroupCount: number;
  EligibleUserCount: number;
  PublicationDetails: PublicationDetails;
}
export class RootContentItemList {
  DetailList: Array<RootContentItemDetail>;
  SelectedRootContentItemId: number;
}
