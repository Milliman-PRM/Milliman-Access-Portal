export interface ActionIconProps {
  title: string;
  action: () => void;
  icon: string;
}

export interface ContentContainerProps {
  contentURL: string;
  closeAction: () => void;
}
