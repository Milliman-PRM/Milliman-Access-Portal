import { ContentTypeEnum } from "../../view-models/content-publishing";

export interface QueryFilter {
  userId?: string;
  clientId?: string;
  profitCenterId?: string;
  rootContentItemId?: string;
}

export enum RoleEnum {
  Admin = 1,
  UserCreator = 2,
  ContentAccessAdmin = 3,
  ContentPublisher = 4,
  ContentUser = 5,
}

export interface NavBarElement {
  Order: number;
  Label: string;
  URL: string;
  View: string;
  Icon: string;
}

export interface ContentContainerProps {
  contentURL: string;
  contentType: ContentTypeEnum;
}

export type Guid = string;

export interface OptionalInputProps {
  placeholderText?: string;
  inputIcon?: string;
  actionIcon?: string;
  actionIconEvent?: (event: any) => void;
  autoFocus?: boolean;
}

export interface InputProps extends OptionalInputProps {
  name: string;
  label: string;
  type: string;
  error: string;
  value: string;
  onChange: (currentTarget: any) => void;
  onBlur: (currentTarget: any) => void;
}
