import { ContentTypeEnum } from '../../view-models/content-publishing';

export interface QueryFilter {
  userId?: string;
  clientId?: string;
  profitCenterId?: string;
  rootContentItemId?: string;
}

export enum RoleEnum {
  Admin = 1,
  UserCreator = 2,
  ContentAccessAdmin = 3,
  ContentPublisher = 4,
  ContentUser = 5,
  FileDropAdmin = 6,
  FileDropUser = 7,
}

export enum HitrustReasonEnum {
  NewEmployeeHire = 1,
  NewMapClient  = 2,
  ChangeInEmployeeResponsibilities = 3,
  EmployeeTermination = 4,
  ClientRemoval = 5,
}

export enum EnableDisabledAccountReasonEnum {
  ChangeInEmployeeResponsibilities = 1,
  ReturningEmployee = 2,
}

export interface NavBarElement {
  order: number;
  label: string;
  url: string;
  view: string;
  icon: string;
  badgeNumber?: number;
}

export interface ContentContainerProps {
  contentURL: string;
  contentType: ContentTypeEnum;
}

export type Guid = string;
