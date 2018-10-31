import { Guid } from '../react/shared-components/interfaces';
import {
  ContentReductionHierarchy, ReductionFieldValueSelection, RootContentItemStatus, UserInfo,
} from './content-publishing';

export enum ReductionStatus {
  Unspecified = 0,
  Canceled = 1,
  Discarded = 2,
  Replaced = 3,
  Validating = 11,
  Queued = 10,
  Reducing = 20,
  Reduced = 30,
  Live = 40,
  Error = 90,
}
export interface ReductionSummary {
  user: UserInfo;
  statusEnum: ReductionStatus;
  statusName: string;
  selectionGroupId: Guid;
  rootContentItemId?: string;
  queuedDurationMs?: number;
  queuePosition?: number;
}
export interface SelectionGroupStatus {
  status: ReductionSummary[];
}
export interface SelectionDetails {
  id: Guid;
  marked: boolean;
}
export interface SelectionComparison {
  hierarchy: ContentReductionHierarchy<ReductionFieldValueSelection>;
  liveSelections: SelectionDetails[];
  pendingSelections: SelectionDetails[];
  isLiveMaster: boolean;
  isPendingMaster: boolean;
}
export interface SelectionsDetail {
  selectionGroupName: string;
  rootContentItemName: string;
  reductionSummary: ReductionSummary;
  selectionComparison: SelectionComparison;
  isSuspended: boolean;
  doesReduce: boolean;
}

export interface SelectionGroupSummary {
  id: Guid;
  name: string;
  memberList: UserInfo[];
  reductionDetails: ReductionSummary;
  rootContentItemName: string;
  isSuspended: boolean;
}
export interface SelectionGroupList {
  selectionGroups: SelectionGroupSummary[];
  relevantRootContentItemId: Guid;
}

export interface ContentAccessStatus {
  rootContentItemStatusList: RootContentItemStatus;
  selectionGroupStatusList: SelectionGroupStatus;
}
