import { ClientSummary, UserInfo } from '../../view-models/content-publishing';

export interface SystemAdminState {
  primaryColContent: string;
  primaryColContentLabel: string;
  primaryColSelection: number;
  primaryColFilter?: string;
  secondaryColContent?: string;
  secondaryColContentLabel?: string;
  secondaryColSelection: number;
  secondaryColFilter?: string;
  addUserDialog: boolean;
  userData: UserInfo[];
  clientData: ClientSummary[];
  profitCenterData: ProfitCenterInfo[];
}

export interface QueryFilter {
  userId?: number;
  clientId?: number;
  profitCenterId?: number;
  rootContentItemId?: number;
}

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

export interface ProfitCenterInfo {
  Id: number;
  Name: string;
}
