import { Guid } from '../react/shared-components/interfaces';

export interface Nestable {
  id: Guid;
  parentId?: string;
}

export interface BasicTree<T extends Nestable> {
  root: BasicNode<T>;
}
export interface BasicNode<T extends Nestable> {
  value: T;
  children: Array<BasicNode<T>>;
}

export interface UserInfo {
  id: Guid;
  lastName: string;
  firstName: string;
  email: string;
  userName: string;
  isSuspended: boolean;
}
export interface ClientSummary extends Nestable {
  name: string;
  code: string;

  canManage: boolean;
  eligibleUserCount: number;
  rootContentItemCount: number;
}
export interface ClientTree extends BasicTree<ClientSummary> {
  selectedClientId: Guid;
}

export enum PublicationStatus {
  Unknown = 0,
  Canceled = 1,
  Rejected = 2,
  Validating = 9,
  Queued = 10,
  Processing = 20,
  Processed = 30,
  Confirmed = 40,
  Replaced = 50,
  Error = 90,
}
export enum ReductionStatus {
  Unspecified = 0,
  Canceled = 1,
  Rejected = 2,
  Validating = 11,
  Queued = 10,
  Reducing = 20,
  Reduced = 30,
  Live = 40,
  Replaced = 50,
  Error = 90,
}
export const publicationStatusNames = {
  9: 'Virus scanning',
  10: 'Queued',
  20: 'Processing',
  30: 'Processed',
  90: 'Error',
};
export const reductionStatusNames = {
  10: 'Queued',
  20: 'Reducing',
  30: 'Reduced',
  90: 'Error',
};
export function isPublicationActive(status: PublicationStatus) {
  return [
    PublicationStatus.Validating,
    PublicationStatus.Queued,
    PublicationStatus.Processing,
    PublicationStatus.Processed,
  ].indexOf(status) !== -1;
}
export function isReductionActive(status: ReductionStatus) {
  return [
    ReductionStatus.Validating,
    ReductionStatus.Queued,
    ReductionStatus.Reducing,
    ReductionStatus.Reduced,
  ].indexOf(status) !== -1;
}

export interface PublicationSummary {
  user: UserInfo;
  statusEnum: PublicationStatus;
  statusName: string;
  selectionGroupId: Guid;
  rootContentItemId: Guid;
  queuedDurationMs?: number;
  queuePosition?: number;
  queueTotal?: number;
}
export interface RootContentItemSummary {
  id: Guid;
  contentName: string;
  contentTypeName: string;
  groupCount: number;
  assignedUserCount: number;
  isSuspended: boolean;
  readOnly: boolean;
  eligibleUserList: UserInfo[];
  publicationDetails: PublicationSummary;
}
export interface RootContentItemList {
  summaryList: RootContentItemSummary[];
  selectedRootContentItemId: Guid;
}

export interface ContentType {
  id: Guid;
  typeEnum: number;
  name: string;
  canReduce: boolean;
  fileExtensions: string[];
}

export interface RootContentItemDetail {
  id: Guid;
  clientId: Guid;
  contentName: string;
  contentTypeId: Guid;
  doesReduce: boolean;
  relatedFiles: ContentRelatedFile[];
  description: string;
  notes: string;
  isSuspended: boolean;
}

export interface RootContentItemSummaryAndDetail {
  summary: RootContentItemSummary;
  detail: RootContentItemDetail;
}

export interface RootContentItemStatus {
  status: PublicationSummary[];
}

export interface ContentRelatedFile {
  fileOriginalName: string;
  filePurpose: string;
  fileUploadId: Guid;
}
export interface PublishRequest {
  rootContentItemId: Guid;
  newRelatedFiles: ContentRelatedFile[];
  deleteFilePurposes: string[];
}

export interface PreLiveContentValidationSummary {
  validationSummaryId: Guid;
  publicationRequestId: Guid;
  rootContentName: string;
  contentTypeName: string;
  contentDescription: string;
  doesReduce: boolean;
  clientName: string;
  clientCode: string;
  attestationLanguage: string;
  masterContentLink: string;
  userGuideLink: string;
  releaseNotesLink: string;
  thumbnailLink: string;
  liveHierarchy: ContentReductionHierarchy<ReductionFieldValue>;
  newHierarchy: ContentReductionHierarchy<ReductionFieldValue>;
  selectionGroups: SelectionGroupSummary[];
}
export interface SelectionGroupSummary {
  name: string;
  userCount: number;
  isMaster: boolean;
}

export interface ContentReductionHierarchy<T extends ReductionFieldValue> {
  fields: Array<ReductionField<T>>;
  rootContentItemId: Guid;
}
export interface ReductionFieldInfo {
  id: Guid;
  fieldName: string;
  displayName: string;
  valueDelimiter: string;
}
export interface ReductionField<T extends ReductionFieldValue> extends ReductionFieldInfo {
  values: T[];
}
export interface ReductionFieldValueInfo {
  id: Guid;
  value: string;
}
export interface ReductionFieldValue extends ReductionFieldValueInfo {
  hasSelectionStatus: boolean;
}
export interface ReductionFieldValueSelection extends ReductionFieldValue {
  selectionStatus: boolean;
}
