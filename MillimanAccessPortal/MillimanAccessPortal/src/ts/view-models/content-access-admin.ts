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
  User: UserInfo;
  StatusEnum: ReductionStatus;
  StatusName: string;
  SelectionGroupId: string;
  RootContentItemId?: string;
  QueuedDurationMs?: number;
  QueuePosition?: number;
}
export interface SelectionGroupStatus {
  Status: ReductionSummary[];
}
export interface SelectionDetails {
  Id: string;
  Marked: boolean;
}
export interface SelectionComparison {
  Hierarchy: ContentReductionHierarchy<ReductionFieldValueSelection>;
  LiveSelections: SelectionDetails[];
  PendingSelections: SelectionDetails[];
  IsLiveMaster: boolean;
  IsPendingMaster: boolean;
}
export interface SelectionsDetail {
  SelectionGroupName: string;
  RootContentItemName: string;
  ReductionSummary: ReductionSummary;
  SelectionComparison: SelectionComparison;
  IsSuspended: boolean;
  DoesReduce: boolean;
}

export interface SelectionGroupSummary {
  Id: string;
  Name: string;
  MemberList: UserInfo[];
  ReductionDetails: ReductionSummary;
  RootContentItemName: string;
  IsSuspended: boolean;
}
export interface SelectionGroupList {
  SelectionGroups: SelectionGroupSummary[];
  RelevantRootContentItemId: string;
}

export interface ContentAccessStatus {
  RootContentItemStatusList: RootContentItemStatus;
  SelectionGroupStatusList: SelectionGroupStatus;
}
