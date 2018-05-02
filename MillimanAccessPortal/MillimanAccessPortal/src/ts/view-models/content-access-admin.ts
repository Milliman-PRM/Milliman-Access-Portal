export class Client {
  Id: number;
  Name: string;
  ClientCode: string;
  ContactName: string;
  ContactTitle: string;
  ContactEmail: string;
  ContactPhone: string;
  ConsultantName: string;
  ConsultantEmail: string;
  ConsultantOffice: string;
  AcceptedEmailDomainList: Array<string>;
  AcceptedEmailAddressExceptionList: Array<string>;
  ParentClientId?: number;
  ParentClient: Client;
  ProfitCenterId: number;
  ProfitCenter: any; // TODO: write ProfitCenter interface
}
export class UserInfo {
  Id: number;
  LastName: string;
  FirstName: string;
  Email: string;
  UserName: string;
}
export class ClientDetail {
  ClientEntity: Client;
  AssignedUsers: Array<UserInfo>;
  EligibleUserCount: number;
  RootContentItemCount: number;
  CanManage: boolean;
}
export class ClientWithChildren {
  ClientDetailModel: ClientDetail;
  ChildClientModels: Array<ClientWithChildren>;
}
export class ClientTree {
  ClientTreeList: Array<ClientWithChildren>;
  RelevantClientId: number;
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
  Client: Client;
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
