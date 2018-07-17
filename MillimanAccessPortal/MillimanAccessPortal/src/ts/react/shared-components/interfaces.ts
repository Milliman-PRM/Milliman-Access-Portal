export interface ActionIconProps {
  title: string;
  action: (event: React.MouseEvent<HTMLElement>) => void;
  icon: string;
}

export interface ColumnSelectorProps {
  colContentSelection: (option: SelectionOption) => void;
  colContentOptions: SelectionOption[];
  colContent: string;
}

export interface SelectionOption {
  label: string;
  value: string;
}

export interface FilterProps {
  filterText: string;
  updateFilterString: (filterString: string) => void;
  placeholderText: string;
}

export interface CardProps<T> {
  data: T;
}
