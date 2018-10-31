import { Guid } from '../shared-components/interfaces';

export interface Filterable {
  filterString: string;
}

export interface ContentItem {
  id: Guid;
  name: string;
  description: string;
  contentURL: string;
  imageURL?: string;
  userguideURL?: string;
  releaseNotesURL?: string;
}

export interface ContentItemGroup {
  id: Guid;
  name: string;
  items: ContentItem[];
}

export interface ContentItemGroupList {
  itemGroups: ContentItemGroup[];
  selectedContentURL: string;
}

export interface ContentCardFunctions {
  selectContent: (URL: string) => void;
}
