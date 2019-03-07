import { ContentTypeEnum } from '../../view-models/content-publishing';
import { Guid } from '../shared-components/interfaces';

export interface Filterable {
  filterString: string;
}

export interface ContentItem {
  id: Guid;
  name: string;
  description: string;
  contentTypeEnum: ContentTypeEnum;
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
  selectedContentType: ContentTypeEnum;
}

export interface ContentCardFunctions {
  selectContent: (URL: string, contentType: ContentTypeEnum) => void;
}
