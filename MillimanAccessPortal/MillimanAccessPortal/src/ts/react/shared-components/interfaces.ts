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
  action: string;
  processResponse: (response: any) => T;
}

export interface DataSourceOverride<T> {
  name: string;
  overrides: Partial<DataSource<T>>;
}
