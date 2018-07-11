export interface ActionIconProps {
  title: string;
  action: () => void;
  icon: string;
}

export interface ColumnSelectorProps {
  colContentSelection: (SelectionOption) => void;
  colContentOptions: SelectionOption[];
  colContent: string;
}

export interface SelectionOption {
  label: string;
  value: string;
}

export interface FilterProps {
  filterText: string,
  updateFilterString: (string) => void;
  placeholderText: string;
}
