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
  name: string;
  parentSources: Array<string | DataSourceOverride<T>>;
  displayName: string;
  infoAction: string;
  detailAction: string;
  createAction: string;
  assignQueryFilter: (id: string) => Partial<QueryFilter>;
}

export interface DataSourceOverride<T> {
  name: string;
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
