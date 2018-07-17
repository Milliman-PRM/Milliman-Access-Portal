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
  clientData: ClientInfo[];
  profitCenterData: ProfitCenterInfo[];
  rootContentItemData: RootContentItemInfo[];
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
