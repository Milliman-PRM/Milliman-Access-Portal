export interface SystemAdminState {
  primaryColContent: string;
  primaryColContentLabel: string;
  primaryColFilter?: string;
  secondaryColContent?: string;
  secondaryColContentLabel?: string;
  secondaryColFilter?: string;
  addUserDialog: boolean;
  structure: SystemAdminStructure;
}

interface ColumnElement {
  displayValue: string;
  panel: string;
  selectedInstance?: string;
}

interface PrimaryColumnUsersElement extends ColumnElement {
  secColElements: {
    Clients: ColumnElement;
    AuthContent: ColumnElement;
  }
}

interface PrimaryColumnClientsElement extends ColumnElement {
  secColElements: {
    Users: ColumnElement;
    Content: ColumnElement;
  }
}

interface PrimaryColumnProfitCentersElement extends ColumnElement {
  secColElements: {
    AuthUsers: ColumnElement;
    Clients: ColumnElement;
  }
}

interface SystemAdminStructure {
  Users: PrimaryColumnUsersElement;
  Clients: PrimaryColumnClientsElement;
  PC: PrimaryColumnProfitCentersElement;
}

export interface UserPanelProps {
  selectedUser: string;
}

export interface UserPanelState {
  userList: User[];
}

interface User {
  userName: string;
}
