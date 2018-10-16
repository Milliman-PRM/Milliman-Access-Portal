import { BasicTree, Nestable } from '../../view-models/content-publishing';
import { Guid, QueryFilter } from '../shared-components/interfaces';

export interface ContentPanelProps<T> {
  onFetch: (data: T[]) => void;
  data: T[];
  select: (id: string) => void;
  selected: number;
  queryFilter: QueryFilter;
}

export interface NestedList {
  Sections: NestedListSection[];
}
export interface NestedListSection {
  Name: string;
  Id?: Guid;
  Marked?: boolean;
  Values: string[];
}

export interface UserInfo {
  Id: Guid;
  Activated: boolean;
  FirstName: string;
  LastName: string;
  UserName: string;
  Email: string;
  IsSuspended: boolean;
  ClientCount?: number;
  RootContentItemCount?: number;
  RootContentItems?: RootContentItemInfo[];
  ProfitCenterId: number;
}
export interface ClientInfo extends Nestable {
  Name: string;
  Code: string;
  UserCount?: number;
  RootContentItemCount?: number;
  ParentOnly: boolean;
}
export interface ClientInfoWithDepth extends ClientInfo {
  depth: number;
}
export interface ProfitCenterInfo {
  Id: Guid;
  Name: string;
  Code: string;
  Office: string;
  UserCount: number;
  ClientCount: number;
}
export interface RootContentItemInfo {
  Id: Guid;
  Name: string;
  ClientName: string;
  UserCount?: number;
  SelectionGroupCount?: number;
  Users?: UserInfo[];
  IsSuspended: boolean;
}
export type EntityInfo = UserInfo | ClientInfo | ProfitCenterInfo | RootContentItemInfo;
export function isUserInfo(info: EntityInfo): info is UserInfo {
  return info && (info as UserInfo).UserName !== undefined;
}
export function isClientInfo(info: EntityInfo): info is ClientInfo {
  return info && (info as ClientInfo).ParentOnly !== undefined;
}
export function isProfitCenterInfo(info: EntityInfo): info is ProfitCenterInfo {
  return info && (info as ProfitCenterInfo).Office !== undefined;
}
export function isRootContentItemInfo(info: EntityInfo): info is RootContentItemInfo {
  return info && (info as RootContentItemInfo).ClientName !== undefined;
}

export type EntityInfoCollection =
  UserInfo[] | ClientInfo[] | BasicTree<ClientInfo> | ProfitCenterInfo[] | RootContentItemInfo[];
export function isUserInfoArray(info: EntityInfoCollection): info is UserInfo[] {
  const userInfo = info as UserInfo[];
  return userInfo && userInfo.length === 0 || isUserInfo(userInfo[0]);
}
export function isClientInfoArray(info: EntityInfoCollection): info is ClientInfo[] {
  const clientInfo = info as ClientInfo[];
  return clientInfo && clientInfo.length === 0 || isClientInfo(clientInfo[0]);
}
export function isClientInfoTree(info: EntityInfoCollection): info is BasicTree<ClientInfo> {
  const clientInfo = info as BasicTree<ClientInfo>;
  return clientInfo && clientInfo.Root !== undefined;
}
export function isProfitCenterInfoArray(info: EntityInfoCollection): info is ProfitCenterInfo[] {
  const profitCenterInfo = info as ProfitCenterInfo[];
  return profitCenterInfo && profitCenterInfo.length === 0 || isProfitCenterInfo(profitCenterInfo[0]);
}
export function isRootContentItemInfoArray(info: EntityInfoCollection): info is RootContentItemInfo[] {
  const rootContentItemInfo = info as RootContentItemInfo[];
  return rootContentItemInfo && rootContentItemInfo.length === 0 || isRootContentItemInfo(rootContentItemInfo[0]);
}

interface Suspendable {
  IsSuspended: boolean;
}
export interface UserDetail extends Suspendable {
  Id: Guid;
  FirstName: string;
  LastName: string;
  Employer: string;
  UserName: string;
  Email: string;
  Phone: string;
  IsSystemAdmin: boolean;
}
export function isUserDetail(detail: PrimaryDetail): detail is UserDetail {
  return detail && (detail as UserDetail).IsSystemAdmin !== undefined;
}
export interface ClientDetail {
  Id: Guid;
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
  Id: Guid;
  Name: string;
  Code: string;
  Office: string;
  ContactName: string;
  ContactEmail: string;
  ContactPhone: string;
}
export type PrimaryDetail = UserDetail | ClientDetail | ProfitCenterDetail;

export interface UserClientRoles {
  IsClientAdmin: boolean;
  IsAccessAdmin: boolean;
  IsContentPublisher: boolean;
  IsContentUser: boolean;
}
export function isUserClientRoles(detail: SecondaryDetail): detail is UserDetailForClient | ClientDetailForUser {
  return detail && (detail as UserClientRoles).IsClientAdmin !== undefined;
}
export interface UserDetailForClient extends UserClientRoles {
  Id: Guid;
  FirstName: string;
  LastName: string;
  Employer: string;
  UserName: string;
  Email: string;
  Phone: string;
}
export interface UserDetailForProfitCenter {
    Id: Guid;
    FirstName: string;
    LastName: string;
    Email: string;
    Phone: string;
    AssignedClients: NestedList;
}
export interface ClientDetailForUser extends UserClientRoles {
    Id: Guid;
    ClientName: string;
    ClientCode: string;
}
export interface ClientDetailForProfitCenter {
    Id: Guid;
    Name: string;
    Code: string;
    ContactName: string;
    ContactEmail: string;
    ContactPhone: string;
    AuthorizedUsers: NestedList;
}
export interface RootContentItemDetailForUser extends Suspendable {
    Id: Guid;
    ContentName: string;
    ContentType: string;
}
export interface RootContentItemDetailForClient extends Suspendable {
    Id: Guid;
    ContentName: string;
    ContentType: string;
    Description: string;
    LastUpdated: string;
    LastAccessed: string;
    IsPublishing: boolean;
    SelectionGroups: NestedList;
}
export function isRootContentItemDetail(detail: SecondaryDetail):
    detail is RootContentItemDetailForClient | RootContentItemDetailForUser {
  return detail && (detail as Suspendable).IsSuspended !== undefined;
}
export type SecondaryDetail = UserDetailForClient | UserDetailForProfitCenter
  | ClientDetailForUser | ClientDetailForProfitCenter
  | RootContentItemDetailForUser | RootContentItemDetailForClient;

export type Detail = PrimaryDetail | SecondaryDetail;
