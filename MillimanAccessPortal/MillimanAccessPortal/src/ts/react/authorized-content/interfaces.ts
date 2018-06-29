export interface Filterable {
  filterString: string;
}

export interface ContentItem {
  Id: number;
  Name: string;
  Description: string;
  ContentURL: string;
  ImageURL?: string;
  UserguideURL?: string;
  ReleaseNotesURL?: string;
}

export interface ContentItemGroup {
  Id: number;
  Name: string;
  Items: ContentItem[];
}

export interface ContentItemGroupList {
  ItemGroups: ContentItemGroup[];
}
