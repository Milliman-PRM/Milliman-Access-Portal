import { SystemAdminColumn } from '../system-admin/system-admin';

export interface QueryFilter {
  userId?: string;
  clientId?: string;
  profitCenterId?: string;
  rootContentItemId?: string;
}

export enum Structure {
  List = 1,
  Tree = 2,
}

export interface DataSource<T> {
  name: SystemAdminColumn;
  parentSources: Array<SystemAdminColumn | DataSourceOverride<T>>;
  displayName: string;
  assignQueryFilter: (id: string) => Partial<QueryFilter>;
}

export interface DataSourceOverride<T> {
  name: SystemAdminColumn;
  overrides: Partial<DataSource<T>>;
}

export enum RoleEnum {
  Admin = 1,
  UserCreator = 2,
  ContentAccessAdmin = 3,
  ContentPublisher = 4,
  ContentUser = 5,
}

export interface NavBarElement {
  Order: number;
  Label: string;
  URL: string;
  View: string;
  Icon: string;
}

export interface ContentContainerProps {
  contentURL: string;
  closeAction: (URL: string) => void;
}
