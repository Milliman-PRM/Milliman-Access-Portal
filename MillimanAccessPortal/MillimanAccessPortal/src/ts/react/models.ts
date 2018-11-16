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
export interface ClientWithEligibleUsers extends Client {
  eligibleUsers: Guid[];
}
export interface RootContentItem {
  id: Guid;
  clientId?: Guid;
  contentTypeId?: Guid;
  isSuspended: boolean;
  doesReduce: boolean;
  name: string;
}
export interface RootContentItemWithStatus extends RootContentItem {
  status: PublicationWithQueueDetails;
}
export interface SelectionGroup {
  id: Guid;
  rootContentItemId?: Guid;
  selectedValues: Guid[];
  isSuspended: boolean;
  isMaster: boolean;
  name: string;
}
export interface SelectionGroupWithAssignedUsers extends SelectionGroup {
  assignedUsers: Guid[];
}
export interface SelectionGroupWithStatus extends SelectionGroup {
  status: ReductionWithQueueDetails;
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
  contentPublicationRequestId?: Guid;
  applicationUserId: Guid;
  selectionGroupId: Guid;
  selectedValues: Guid[];
  createDateTimeUtc: string;
  taskStatus: ReductionStatus;
}
export interface ContentType {
  id: Guid;
  canReduce: boolean;
  name: string;
  fileExtensions: string[];
}

export interface ReductionFieldset {
  field: ReductionField;
  values: ReductionFieldValue[];
}

export interface PublicationQueueDetails {
  publicationId: Guid;
  queuePosition: number;
}
export interface ReductionQueueDetails {
  reductionId: Guid;
  queuePosition: number;
  queueTotal?: number;
}
export interface PublicationWithQueueDetails extends ContentPublicationRequest {
  queueDetails: PublicationQueueDetails;
  applicationUser: User;
  requestStatusName: string;
}
export interface ReductionWithQueueDetails extends ContentReductionTask {
  queueDetails: ReductionQueueDetails;
  applicationUser: User;
  taskStatusName: string;
}
export function isPublicationRequest(request: ContentPublicationRequest | ContentReductionTask)
    : request is ContentPublicationRequest {
  return (request as ContentPublicationRequest).rootContentItemId !== undefined;
}
export function isReductionTask(request: ContentPublicationRequest | ContentReductionTask)
    : request is ContentReductionTask {
  return (request as ContentReductionTask).selectionGroupId !== undefined;
}
