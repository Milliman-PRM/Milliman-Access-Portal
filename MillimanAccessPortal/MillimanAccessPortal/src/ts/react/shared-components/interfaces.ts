export interface ActionIconProps {
  title: string;
  action: () => void;
  icon: string;
}

export interface columnSelectorProps {
  colContentSelection: () => void;
  colContentOptions: Array<string>;
  colContent: string;
}
