import { Guid } from '../shared-components/interfaces';
import { ContentTypeEnum } from '../../view-models/content-publishing';

export interface Filterable {
  filterString: string;
}

export interface ContentItem {
  Id: Guid;
  Name: string;
  Description: string;
  ContentTypeEnum: ContentTypeEnum;
  ContentURL: string;
  ImageURL?: string;
  UserguideURL?: string;
  ReleaseNotesURL?: string;
}

export interface ContentItemGroup {
  Id: Guid;
  Name: string;
  Items: ContentItem[];
}

export interface ContentItemGroupList {
  ItemGroups: ContentItemGroup[];
  selectedContentURL: string;
}

export interface ContentCardFunctions {
  selectContent: (URL: string) => void;
}
