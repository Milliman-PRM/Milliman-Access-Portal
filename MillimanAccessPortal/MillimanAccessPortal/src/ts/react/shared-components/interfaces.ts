export interface ActionIconProps {
  title: string;
  action: () => void;
  icon: string;
}

export interface ColumnSelectorProps {
  colContentSelection: any;
  colContentOptions: string[];
  colContent: string;
  primaryColumn: boolean;
}
