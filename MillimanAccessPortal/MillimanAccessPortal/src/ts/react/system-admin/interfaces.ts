import { BasicTree, Nestable } from '../../view-models/content-publishing';
import { SelectionGroup } from '../models';
import { Guid } from '../shared-components/interfaces';

export interface NestedList {
  sections: NestedListSection[];
}
export interface NestedListSection {
  name: string;
  id?: Guid;
  marked?: boolean;
  values: string[];
}

export interface UserInfo {
  id: Guid;
  activated: boolean;
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  isSuspended: boolean;
  clientCount?: number;
  rootContentItemCount?: number;
  rootContentItems?: RootContentItemInfo[];
  profitCenterId?: Guid;
  clientId?: Guid;
}
export interface ClientInfo extends Nestable {
  name: string;
  code: string;
  userCount?: number;
  rootContentItemCount?: number;
  parentOnly?: boolean;
}
export interface ClientInfoWithDepth extends ClientInfo {
  depth: number;
}
export interface ProfitCenterInfo {
  id: Guid;
  name: string;
  code: string;
  office: string;
  userCount: number;
  clientCount: number;
}
export interface RootContentItemInfo {
  id: Guid;
  name: string;
  clientName?: string;
  userCount?: number;
  selectionGroupCount?: number;
  users?: UserInfo[];
  clientId?: Guid;
  isSuspended: boolean;
}
export type EntityInfo = UserInfo | ClientInfo | ProfitCenterInfo | RootContentItemInfo | SelectionGroup;
export function isUserInfo(info: EntityInfo): info is UserInfo {
  return info && (info as UserInfo).userName !== undefined;
}
export function isClientInfo(info: EntityInfo): info is ClientInfo {
  return info && (info as ClientInfo).parentOnly !== undefined;
}
export function isProfitCenterInfo(info: EntityInfo): info is ProfitCenterInfo {
  return info && (info as ProfitCenterInfo).office !== undefined;
}
export function isRootContentItemInfo(info: EntityInfo): info is RootContentItemInfo {
  return info && (info as RootContentItemInfo).clientName !== undefined;
}

export type EntityInfoCollection =
  UserInfo[] | ClientInfo[] | BasicTree<ClientInfo> | ProfitCenterInfo[] | RootContentItemInfo[] | SelectionGroup[];
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
  return clientInfo && clientInfo.root !== undefined;
}
export function isProfitCenterInfoArray(info: EntityInfoCollection): info is ProfitCenterInfo[] {
  const profitCenterInfo = info as ProfitCenterInfo[];
  return profitCenterInfo && profitCenterInfo.length === 0 || isProfitCenterInfo(profitCenterInfo[0]);
}
export function isRootContentItemInfoArray(info: EntityInfoCollection): info is RootContentItemInfo[] {
  const rootContentItemInfo = info as RootContentItemInfo[];
  return rootContentItemInfo && rootContentItemInfo.length === 0 || isRootContentItemInfo(rootContentItemInfo[0]);
}

export interface SystemRoles {
  isSystemAdmin: boolean;
}
export interface Suspendable {
  isSuspended: boolean;
}
export interface UserDetail {
  id: Guid;
  firstName: string;
  lastName: string;
  employer: string;
  userName: string;
  email: string;
  phone: string;
}
export function isUserDetail(detail: PrimaryDetail): detail is UserDetail {
  return detail && (detail as UserDetail).employer !== undefined;
}
export interface ClientDetail {
  id: Guid;
  clientName: string;
  clientCode: string;
  clientContactName: string;
  clientContactEmail: string;
  clientContactPhone: string;
  profitCenter: string;
  office: string;
  consultantName: string;
  consultantEmail: string;
}
export interface ProfitCenterDetail {
  id: Guid;
  name: string;
  code: string;
  office: string;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
}
export type PrimaryDetail = UserDetail | ClientDetail | ProfitCenterDetail;
export type PrimaryDetailData = (UserDetail & SystemRoles & Suspendable) | ClientDetail | ProfitCenterDetail;

export interface UserClientRoles {
  isClientAdmin: boolean;
  isAccessAdmin: boolean;
  isContentPublisher: boolean;
  isContentUser: boolean;
}
export function isUserClientRoles(detail: SecondaryDetail): detail is UserDetailForClient | ClientDetailForUser {
  return detail
    && ((detail as UserDetailForClient).employer !== undefined
    || (detail as ClientDetailForUser).clientName !== undefined);
}
export interface UserDetailForClient {
  id: Guid;
  firstName: string;
  lastName: string;
  employer: string;
  userName: string;
  email: string;
  phone: string;
}
export interface UserDetailForProfitCenter {
  id: Guid;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  assignedClients: NestedList;
}
export interface ClientDetailForUser {
  id: Guid;
  clientName: string;
  clientCode: string;
}
export interface ClientDetailForProfitCenter {
  id: Guid;
  name: string;
  code: string;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
  authorizedUsers: NestedList;
}
export interface RootContentItemDetailForUser {
  id: Guid;
  contentName: string;
  contentType: string;
}
export interface RootContentItemDetailForClient {
  id: Guid;
  contentName: string;
  contentType: string;
  description: string;
  lastUpdated: string;
  lastAccessed: string;
  isPublishing: boolean;
  selectionGroups: NestedList;
}
export function isRootContentItemDetail(detail: SecondaryDetail)
    : detail is RootContentItemDetailForClient | RootContentItemDetailForUser {
  return detail
    && (detail as RootContentItemDetailForClient | RootContentItemDetailForUser).contentName !== undefined;
}
export type SecondaryDetail = UserDetailForClient
  | UserDetailForProfitCenter
  | ClientDetailForUser
  | ClientDetailForProfitCenter
  | RootContentItemDetailForUser
  | RootContentItemDetailForClient;
export type SecondaryDetailData = (UserDetailForClient & UserClientRoles)
  | UserDetailForProfitCenter
  | (ClientDetailForUser & UserClientRoles)
  | ClientDetailForProfitCenter
  | (RootContentItemDetailForUser & Suspendable)
  | (RootContentItemDetailForClient & Suspendable);

export type Detail = PrimaryDetail | SecondaryDetail;
