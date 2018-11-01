import { ReductionSummary } from '../view-models/content-access-admin';
import { UserInfo } from './system-admin/interfaces';

export type Guid = string;

export interface User {
  id: Guid;
  activated: boolean;
  isSuspended: boolean;
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
}
export interface ProfitCenter {
  id: Guid;
  name: string;
  code: string;
  office: string;
}
export interface Client {
  id: Guid;
  profitCenterId?: Guid;
  name: string;
  code: string;
}
export interface RootContentItem {
  id: Guid;
  clientId?: Guid;
  isSuspended: boolean;
  name: string;
}
export interface SelectionGroup {
  id: Guid;
  rootContentItemId?: Guid;
  isSuspended: boolean;
  isMaster: boolean;
  name: string;
}
