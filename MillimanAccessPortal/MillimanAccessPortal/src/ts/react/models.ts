import { PublicationStatus, ReductionStatus } from '../view-models/content-publishing';

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
  doesReduce: boolean;
  name: string;
}
export interface RootContentItemWithStatus extends RootContentItem {
  status: ContentPublicationRequest;
}
export interface SelectionGroup {
  id: Guid;
  rootContentItemId?: Guid;
  selectedValues?: Guid[];
  isSuspended: boolean;
  isMaster: boolean;
  name: string;
}
export interface SelectionGroupWithStatus extends SelectionGroup {
  status: ContentReductionTask;
}
export interface ReductionField {
  id: Guid;
  rootContentItemId?: Guid;
  fieldName: string;
  displayName: string;
  valueDelimiter: string;
}
export interface ReductionFieldValue {
  id: Guid;
  reductionFieldId?: Guid;
  value: string;
}
export interface ContentPublicationRequest {
  id: Guid;
  rootContentItemId: Guid;
  applicationUserId: Guid;
  createDateTimeUtc: string;
  requestStatus: PublicationStatus;
}
export interface ContentReductionTask {
  id: Guid;
  contentPublicationRequestId: Guid;
  applicationUserId: Guid;
  selectionGroupId: Guid;
  createDateTimeUtc: string;
  reductionStatus: ReductionStatus;
}

export interface ReductionFieldset {
  field: ReductionField;
  values: ReductionFieldValue[];
}
