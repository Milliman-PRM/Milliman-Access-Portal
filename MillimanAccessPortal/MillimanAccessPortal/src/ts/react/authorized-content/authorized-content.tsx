import '../../../scss/react/authorized-content/authorized-content.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { ContentCard } from './content-card';
import { FilterBar } from './filter-bar';
import { ContentItem, ContentItemGroup, ContentItemGroupList, Filterable } from './interfaces';

interface AuthorizedContentState extends ContentItemGroupList, Filterable { }
export class AuthorizedContent extends React.Component<{}, AuthorizedContentState> {
  public constructor(props) {
    super(props);
    this.state = {
      ItemGroups: [],
      filterString: '',
    };

    this.setFilterString = this.setFilterString.bind(this);
  }

  public componentDidMount() {
    const itemGroups = ajax({
      method: 'GET',
      url: 'AuthorizedContent/Content/',
    }).done((response: ContentItemGroupList) => {
      this.setState(response);
    });
  }

  public setFilterString(filterString: string) {
    this.setState({
      filterString,
    });
  }

  public render() {
    const partialSections = this.filteredArray().map((client) => ({
      client,
      contentItems: client.Items.map((contentItem) => (
        <ContentCard
          key={contentItem.Id.toString()}
          {...contentItem}
        />
      )),
    }));
    const sections = partialSections.map((partialSection) => (
      <div
        key={`client-${partialSection.client.Id}`}
        className="client-content-container"
      >
        <h1 className="client-name">
          {partialSection.client.Name}
        </h1>
        {partialSection.contentItems}
      </div>
      ));
    return (
      <div id="authorized-content-container">
        <div id="authorized-content-header">
          <FilterBar onFilterStringChanged={this.setFilterString} />
        </div>
        <div id="authorized-content-items">
          {sections}
        </div>
      </div>
    );
  }

  private filteredArray() {
    // Deep copy state
    const groups = JSON.parse(JSON.stringify(this.state.ItemGroups)) as ContentItemGroup[];
    return groups.map((itemGroup: ContentItemGroup) => {
      itemGroup.Items = itemGroup.Items.filter((item) =>
        [itemGroup.Name, item.Name, item.Description].filter((text) =>
          text.toLowerCase().indexOf(this.state.filterString.toLowerCase()) > -1).length);
      return itemGroup;
    }).filter((itemGroup: ContentItemGroup) => itemGroup.Items.length);
  }
}
