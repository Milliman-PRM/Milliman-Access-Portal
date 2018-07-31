export interface QueryFilter {
  userId?: number;
  clientId?: number;
  profitCenterId?: number;
  rootContentItemId?: number;
}

export interface DataSource<T> {
  name: string;
  parentSources: Array<string | DataSourceOverride<T>>;
  displayName: string;
  infoAction: string;
  detailAction: string;
  createAction: string;
  processInfo: (response: any) => T[];
  assignQueryFilter: (id: number) => Partial<QueryFilter>;
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
