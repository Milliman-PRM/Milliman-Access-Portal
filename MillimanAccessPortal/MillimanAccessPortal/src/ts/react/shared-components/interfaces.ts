export interface ActionIconProps {
  title: string;
  action: () => void;
  icon: string;
}

export interface ColumnSelectorProps {
  colContentSelection: (string, boolean) => void;
  colContentOptions: string[];
  colContent: string;
  primaryColumn: boolean;
}

export interface FilterProps {
  updateFilterString: (string) => void;
  placeholderText: string;
}