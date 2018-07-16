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
}

export interface UserPanelProps {
  selectedUser: string;
  makeUserSelection: (string) => void;
}

export interface UserPanelState {
  userList: User[];
}

interface User {
  userName: string;
}

export interface ClientPanelProps {
  selectedClient: string;
  makeClientSelection: (string) => void;
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

