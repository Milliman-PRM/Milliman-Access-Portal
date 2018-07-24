export interface ActionIconProps {
  title: string;
  action: (event: React.MouseEvent<HTMLElement>) => void;
  icon: string;
}

export interface FilterProps {
  filterText: string;
  setFilterText: (filterString: string) => void;
  placeholderText: string;
}

// Represents an object displayable on a card
export interface Entity {
  Id: number;
  PrimaryText: string;
}

export interface QueryFilter {
  userId?: number;
  clientId?: number;
  profitCenterId?: number;
  rootContentItemId?: number;
}

export interface DataSource<T> {
  name: string;
  sources: string[];
  displayName: string;
  action: string;
  processResponse: (response: any) => T;
}
