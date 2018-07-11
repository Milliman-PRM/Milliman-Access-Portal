export interface ActionIconProps {
  title: string;
  action: () => void;
  icon: string;
}

export interface ColumnSelectorProps {
  colContentSelection: (string) => void;
  colContentOptions: string[];
  colContent: string;
}

export interface FilterProps {
  filterText: string,
  updateFilterString: (string) => void;
  placeholderText: string;
}
