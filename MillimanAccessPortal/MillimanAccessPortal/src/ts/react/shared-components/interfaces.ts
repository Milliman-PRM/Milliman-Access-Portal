export interface ActionIconProps {
  title: string;
  action: () => void;
  icon: string;
}

export interface ContentContainerProps {
  contentId: number;
  closeAction: (id: number) => void;
}
