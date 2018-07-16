import { UserInfo } from '../../view-models/content-publishing';

export interface SystemAdminState {
  primaryColContent: string;
  primaryColContentLabel: string;
  primaryColSelection: string;
  primaryColFilter?: string;
  secondaryColContent?: string;
  secondaryColContentLabel?: string;
  secondaryColSelection: string;
  secondaryColFilter?: string;
  addUserDialog: boolean;
  userData: UserInfo[];
}

export interface UserPanelProps {
  onFetch: (data: UserInfo[]) => void;
  users: UserInfo[];
  selectedUser: string;
  makeUserSelection: (string) => void;
  queryFilter: QueryFilter;
}

export interface QueryFilter {
  userId?: number;
  clientId?: number;
  profitCenterId?: number;
  rootContentItemId?: number;
}

export interface ClientPanelProps {
  selectedClient: string;
  makeClientSelection: (string) => void;
}

export interface UserList {
  Users: UserInfo[];
}

export interface ClientPanelState {
  clientList: Client[];
}

interface Client {
  clientName: string;
}

export interface ProfitCenterPanelProps {
  selectedProfitCenter: string;
  makeProfitCenterSelection: (string) => void;
}

export interface ProfitCenterPanelState {
  profitCenterList: ProfitCenter[];
}

interface ProfitCenter {
  profitCenterName: string;
}
