export interface SystemAdminState {
  primaryColContent: string;
  primaryColFilter?: string;
  secondaryColContent?: string;
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