import '../../../scss/react/authorized-content/authorized-content.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { ContentCard } from './content-card';
import { FilterBar } from './filter-bar';
import { ContentContainer } from '../shared-components/content-container';
import { ContentItem, ContentItemGroup, ContentItemGroupList, Filterable } from './interfaces';

interface AuthorizedContentState extends ContentItemGroupList, Filterable { }
export class AuthorizedContent extends React.Component<{}, AuthorizedContentState> {
  public constructor(props) {
    super(props);
    this.state = {
      ItemGroups: [],
      selectedContentItem: null,
      filterString: '',
    };
  }

  public componentDidMount() {
    const itemGroups = ajax({
      method: 'GET',
      url: 'AuthorizedContent/Content/',
    }).done((response: ContentItemGroupList) => {
      this.setState(response);
    });
  }

  public selectContentItem = (contentItem: number) => {
    this.setState({ selectedContentItem: contentItem });
  }

  public render() {
    if (this.state.selectedContentItem) {
      return (
        <ContentContainer
          closeAction={this.selectContentItem}
          contentId={this.state.selectedContentItem} />
      )
    } else {
      return (
        <div id='authorized-content-container'>
          <div id='authorized-content-header'>
            <FilterBar onFilterStringChanged={(filterString) => this.setState({ filterString })} />
          </div>
          <div id='authorized-content-items'>
            {
              this.filteredArray().map((client: ContentItemGroup, index: number) => (
                <div key={`client-${client.Id}`} className='client-content-container'>
                  <h1 className='client-name'>{client.Name}</h1>
                  {
                    client.Items.map((contentItem: ContentItem) => (
                      <ContentCard
                        key={contentItem.Id.toString()}
                        selectContent={this.selectContentItem}
                        {...contentItem}
                      />
                    ))
                  }
                </div>
              ))
            }
          </div>
        </div>
      );
    }
  }

  private filteredArray() {
    // Deep copy state
    const groups = JSON.parse(JSON.stringify(this.state.ItemGroups));
    return groups.map((itemGroup: ContentItemGroup) => {
      itemGroup.Items = itemGroup.Items.filter((item) =>
        [itemGroup.Name, item.Name, item.Description].filter((text) =>
          text.toLowerCase().indexOf(this.state.filterString.toLowerCase()) > -1).length);
      return itemGroup;
    }).filter((itemGroup: ContentItemGroup) => itemGroup.Items.length);
  }
}
