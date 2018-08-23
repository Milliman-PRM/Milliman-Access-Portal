import { Nestable } from '../../view-models/content-publishing';
import { QueryFilter } from '../shared-components/interfaces';

export interface ContentPanelProps<T> {
  onFetch: (data: T[]) => void;
  data: T[];
  select: (id: number) => void;
  selected: number;
  queryFilter: QueryFilter;
}

export interface NestedList {
  Sections: NestedListSection[];
}
export interface NestedListSection {
  Name: string;
  Values: string[];
}

export interface UserInfo {
  Id: number;
  Activated: boolean;
  FirstName: string;
  LastName: string;
  UserName: string;
  Email: string;
  IsSuspended: boolean;
  ClientCount?: number;
  RootContentItemCount?: number;
  RootContentItems?: RootContentItemInfo[];
}
export interface ClientInfo extends Nestable {
  Name: string;
  Code: string;
  UserCount?: number;
  RootContentItemCount?: number;
  ParentOnly: boolean;
}
export interface ProfitCenterInfo {
  Id: number;
  Name: string;
  Office: string;
  UserCount: number;
  ClientCount: number;
}
export interface RootContentItemInfo {
  Id: number;
  Name: string;
  ClientName: string;
  UserCount?: number;
  SelectionGroupCount?: number;
  Users?: UserInfo[];
  IsSuspended: boolean;
}

export interface UserDetail {
  Id: number;
  FirstName: string;
  LastName: string;
  Employer: string;
  UserName: string;
  Email: string;
  Phone: string;
  IsSuspended: boolean;
}
export interface ClientDetail {
  Id: number;
  ClientName: string;
  ClientCode: string;
  ClientContactName: string;
  ClientContactEmail: string;
  ClientContactPhone: string;
  ProfitCenter: string;
  Office: string;
  ConsultantName: string;
  ConsultantEmail: string;
}
export interface ProfitCenterDetail {
  Id: number;
  Name: string;
  Office: string;
  ContactName: string;
  ContactEmail: string;
  ContactPhone: string;
}
export type PrimaryDetail = UserDetail | ClientDetail | ProfitCenterDetail;

export interface UserDetailForClient {
  Id: number;
  FirstName: string;
  LastName: string;
  Employer: string;
  UserName: string;
  Email: string;
  Phone: string;
}
export interface UserDetailForProfitCenter {
    Id: number;
    FirstName: string;
    LastName: string;
    Email: string;
    Phone: string;
    AssignedClients: NestedList;
}
export interface ClientDetailForUser {
    Id: number;
    ClientName: string;
    ClientCode: string;
}
export interface ClientDetailForProfitCenter {
    Id: number;
    Name: string;
    Code: string;
    ContactName: string;
    ContactEmail: string;
    ContactPhone: string;
    AuthorizedUsers: NestedList;
}
export interface RootContentItemDetailForUser {
    Id: number;
    ContentName: string;
    ContentType: string;
}
export interface RootContentItemDetailForClient {
    Id: string;
    ContentName: string;
    ContentType: string;
    Description: string;
    LastUpdated: string;
    LastAccessed: string;
    SelectionGroups: NestedList;
}
export type SecondaryDetail = UserDetailForClient | UserDetailForProfitCenter
  | ClientDetailForUser | ClientDetailForProfitCenter
  | RootContentItemDetailForUser | RootContentItemDetailForClient;

export type Detail = PrimaryDetail | SecondaryDetail;
