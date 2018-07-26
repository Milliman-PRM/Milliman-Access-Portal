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
  FirstName: string;
  LastName: string;
  UserName: string;
  ClientCount?: number;
  RootContentItemCount?: number;
  RootContentItems?: RootContentItemInfo[];
}

export interface ClientInfo {
  Id: number;
  Name: string;
  Code: string;
  UserCount?: number;
  RootContentItemCount?: number;
}

export interface ProfitCenterInfo {
  Id: number;
  Name: string;
}

export interface RootContentItemInfo {
  Id: number;
  Name: string;
  Users?: UserInfo[];
}
