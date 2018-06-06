export interface Filterable {
  filterString: string;
}

export interface HostedFile {
  title: string;
  link: string;
}

export interface ContentItem {
  id: number;
  name: string;
  description: string;
  contentURL: string;
  imageURL?: string;
  userguideURL?: string;
  releaseNotesURL?: string;
}

export interface ContentItemGroup {
  id: number;
  name: string;
  items: ContentItem[];
}

export interface ContentItemGroupList {
  groups: ContentItemGroup[];
}
