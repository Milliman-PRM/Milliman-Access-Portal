export interface ActionIconProps {
  title: string;
  action: () => void;
  icon: string;
}

export interface NavBarProps {
  currentView: string;
}

export interface NavBarState {
  NavBarElements: NavBarElement[];
}

export interface NavBarElement {
  Order: number;
  Label: string;
  URL: string;
  Icon: string;
}
