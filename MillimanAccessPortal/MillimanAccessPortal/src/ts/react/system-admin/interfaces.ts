import { Nestable } from '../../view-models/content-publishing';
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

export interface ClientInfo extends Nestable {
  Name: string;
  Code: string;
  UserCount?: number;
  RootContentItemCount?: number;
  ParentOnly: boolean;
}

export interface ProfitCenterInfo {
  Id: number;
  Name: string;
  Office: string;
  ClientCount: number;
}

export interface RootContentItemInfo {
  Id: number;
  Name: string;
  ClientName: string;
  UserCount?: number;
  SelectionGroupCount?: number;
  Users?: UserInfo[];
  IsSuspended: boolean;
}
