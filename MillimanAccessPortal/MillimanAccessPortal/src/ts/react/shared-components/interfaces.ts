export interface QueryFilter {
  userId?: number;
  clientId?: number;
  profitCenterId?: number;
  rootContentItemId?: number;
}

export enum Structure {
  List = 1,
  Tree = 2,
}

export interface DataSource<T> {
  name: string;
  structure: Structure;
  parentSources: Array<string | DataSourceOverride<T>>;
  displayName: string;
  infoAction: string;
  detailAction: string;
  createAction: string;
  sublistInfo?: {
    title: string;
    icon: string;
    emptyText: string;
  };
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

export interface NavBarProps {
  currentView: string;
}

export interface NavBarState {
  navBarElements: NavBarElement[];
  navBarIsLoaded: boolean;
  contactFormOpen: boolean;
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

export interface NavBarLocation {
  navLocation: string;
}
