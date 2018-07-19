import { Column } from '../system-admin/interfaces';

export interface ActionIconProps {
  title: string;
  action: (event: React.MouseEvent<HTMLElement>) => void;
  icon: string;
}

export interface ColumnSelectorOption {
  column: Column;
  displayName: string;
}

export interface FilterProps {
  filterText: string;
  setFilterText: (filterString: string) => void;
  placeholderText: string;
}

// Represents an EF entity
export interface Entity {
  Id: number;
  Name: string;
}

export interface QueryFilter {
  userId?: number;
  clientId?: number;
  profitCenterId?: number;
  rootContentItemId?: number;
}
