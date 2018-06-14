import {
  ContentReductionHierarchy, PublicationDetails, ReductionFieldValueSelection, UserInfo,
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
export interface ReductionDetails {
  User: UserInfo;
  StatusEnum: ReductionStatus;
  StatusName: string;
  SelectionGroupId: number;
  RootContentItemId?: number;
}
export interface SelectionDetail {
  Hierarchy: ContentReductionHierarchy<ReductionFieldValueSelection>;
  OriginalSelections: number[];
  ReductionDetails: ReductionDetails;
}

export interface SelectionGroupDetail {
  Id: number;
  Name: string;
  MemberList: UserInfo[];
  ReductionDetails: ReductionDetails;
}
export interface SelectionGroups {
  SelectionGroupList: SelectionGroupDetail[];
  RelevantRootContentItemId: number;
}

export interface RootContentItems {
  RootContentItemList: RootContentItemDetail[];
  RelevantRootContentItemId: number;
}
export interface RootContentItemDetail {
  Id: number;
  Name: string;
  TypeName: string;
  GroupCount: number;
  EligibleUserList: UserInfo[];
  PublicationDetails: PublicationDetails;
}
