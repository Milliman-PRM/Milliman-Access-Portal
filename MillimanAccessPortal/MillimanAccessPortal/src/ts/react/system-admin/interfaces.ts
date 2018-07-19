import { QueryFilter } from '../shared-components/interfaces';

export interface ContentPanelProps<T> {
  onFetch: (data: T[]) => void;
  data: T[];
  select: (id: number) => void;
  selected: number;
  queryFilter: QueryFilter;
}

export interface ClientPanelProps {
  selectedClient: string;
  makeClientSelection: (id: string) => void;
}

export interface ProfitCenterPanelProps {
  selectedProfitCenter: string;
  makeProfitCenterSelection: (id: string) => void;
}

export interface UserInfo {
  Id: number;
  Name: string;
}

export interface ClientInfo {
  Id: number;
  Name: string;
}

export interface ProfitCenterInfo {
  Id: number;
  Name: string;
}

export interface RootContentItemInfo {
  Id: number;
  Name: string;
}

export enum Column {
  Undefined = 0,
  User = 1,
  Client = 2,
  ProfitCenter = 3,
  RootContentItem = 4,
}
