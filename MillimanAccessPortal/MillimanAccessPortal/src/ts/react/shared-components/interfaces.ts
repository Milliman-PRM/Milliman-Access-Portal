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
}

export interface NavBarElement {
  order: number;
  label: string;
  url: string;
  view: string;
  icon: string;
}

export interface ContentContainerProps {
  contentURL: string;
  closeAction: (URL: string) => void;
}

export type Guid = string;
