export interface Filterable {
  filterString: string;
}

export interface ContentItem {
  Id: string;
  Name: string;
  Description: string;
  ContentURL: string;
  ImageURL?: string;
  UserguideURL?: string;
  ReleaseNotesURL?: string;
}

export interface ContentItemGroup {
  Id: string;
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
