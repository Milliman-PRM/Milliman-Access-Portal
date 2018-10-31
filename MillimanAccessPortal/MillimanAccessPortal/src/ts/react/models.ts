import { ReductionSummary } from '../view-models/content-access-admin';
import { UserInfo } from './system-admin/interfaces';

export type Guid = string;

export interface SelectionGroupInfo {
  id: Guid;
  name: string;
  isSuspended: boolean;
}
