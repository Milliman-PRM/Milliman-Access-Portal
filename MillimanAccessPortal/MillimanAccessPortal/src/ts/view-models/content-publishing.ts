import { ContentItemDetail } from '../react/models';
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
  Confirming = 35,
  Confirmed = 40,
  Replaced = 50,
  Error = 90,
}
export enum ReductionStatus {
  Unspecified = 0,
  Canceled = 1,
  Rejected = 2,
  Validating = 9,
  Queued = 10,
  Reducing = 20,
  Reduced = 30,
  Live = 40,
  Replaced = 50,
  Error = 90,
}
export const publicationStatusNames: { [status: number]: string; } = {
  9: 'Virus scanning',
  10: 'Queued',
  20: 'Processing',
  30: 'Processed',
  90: 'Error',
};
export const reductionStatusNames: { [status: number]: string; } = {
   9: 'Validating',
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
  statusMessage: string;
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

export enum ContentTypeEnum {
  Unknown = 0,
  Qlikview = 1,
  Html = 2,
  Pdf = 3,
  FileDownload = 4,
  PowerBi = 5,
}
export enum ContentAssociatedFileTypeEnum {
    Unknown = 0,
    Pdf = 1,
    Html = 2,
    FileDownload = 3,
}

export interface ContentType {
  id: Guid;
  typeEnum: ContentTypeEnum;
  name: string;
  canReduce: boolean;
  defaultIconName: string;
  fileExtensions: string[];
}

export interface ContentRelatedFile {
  fullPath: string;
  filePurpose: string;
  fileOriginalName: string;
  checksum: string;
}
export interface ContentAssociatedFile {
  id: Guid;
  displayName: string;
  fileOriginalName: string;
  sortOrder: string;
  checksum: string;
}

export interface RootContentItemSummaryAndDetail {
  summary: RootContentItemSummary;
  detail: ContentItemDetail;
}

export interface RootContentItemStatus {
  status: PublicationSummary[];
}

export interface UploadedRelatedFile {
  fileOriginalName: string;
  filePurpose: string;
  fileUploadId: Guid;
}
export interface RequestedAssociatedFile {
    id: Guid;
    fileOriginalName: string;
    displayName: string;
    sortOrder: string;
    fileType: ContentAssociatedFileTypeEnum;
}
export interface PublishRequest {
  rootContentItemId: Guid;
  newRelatedFiles?: UploadedRelatedFile[];
  associatedFiles?: RequestedAssociatedFile[];
  deleteFilePurposes?: string[];
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
  reductionHierarchy: ContentReductionHierarchy<ReductionFieldValue>;
  selectionGroups: SelectionGroupSummary[];
  associatedFiles: AssociatedFileSummary[];
}
export interface SelectionGroupSummary {
  id: Guid;
  name: string;
  isMaster: boolean;
  duration: string;
  users: UserInfo[];
  wasInactive: boolean;
  isInactive: boolean;
  inactiveReason?: string;
  PreviewLink: string;
  selectionChanges: ContentReductionHierarchy<ReductionFieldValueSelection>;
}
export interface AssociatedFileSummary {
  id: Guid;
  displayName: string;
  fileOriginalName: string;
  sortOrder: string;
  fileType: ContentAssociatedFileTypeEnum;
  link: string;
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
  valueChange: FieldValueChange;
}
export interface ReductionFieldValue extends ReductionFieldValueInfo {
  hasSelectionStatus: boolean;
}
export interface ReductionFieldValueSelection extends ReductionFieldValue {
  selectionStatus: boolean;
}
export function isSelection(value: ReductionFieldValue): value is ReductionFieldValueSelection {
  return value && (value as ReductionFieldValueSelection).selectionStatus !== undefined;
}

export enum FieldValueChange {
  noChange = 0,
  added = 1,
  removed = 2,
}
export const FieldValueChangeName: { [status: number]: string; } = {
  0: 'No change',
  1: 'Added',
  2: 'Removed',
};
