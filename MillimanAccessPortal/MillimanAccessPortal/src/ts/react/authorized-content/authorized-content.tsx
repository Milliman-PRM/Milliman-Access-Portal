import '../../../scss/react/authorized-content/authorized-content.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { ContentCard } from './content-card';
import { FilterBar } from './filter-bar';
import { ContentContainer } from '../shared-components/content-container';
import { ContentItem, ContentItemGroup, ContentItemGroupList, Filterable, NavBarLocation } from './interfaces';
import { NavBar } from '../shared-components/navbar';

interface AuthorizedContentState extends ContentItemGroupList, Filterable, NavBarLocation { }
export class AuthorizedContent extends React.Component<{}, AuthorizedContentState> {
  public constructor(props) {
    super(props);
    this.state = {
      ItemGroups: [],
      selectedContentURL: null,
      filterString: '',
      navLocation: document.getElementsByTagName("body")[0].getAttribute("data-nav-location")
    };
  }

  public componentDidMount() {
    const itemGroups = ajax({
      method: 'GET',
      url: 'AuthorizedContent/Content/',
    }).done((response: ContentItemGroupList) => {
      this.setState(response);
    });
    window.onpopstate = (e) => {
      if (this.state.selectedContentURL) {
        this.setState({ selectedContentURL: null });
      }
    }
  }

  public selectContentItem = (contentURL: string) => {
    this.setState({ selectedContentURL: contentURL }, () => {
      let display = (this.state.selectedContentURL) ? 'none' : null;
      document.getElementById('page-header').style.display = display;
      document.getElementById('page-footer').style.display = display;
      if (!contentURL) {
        history.back();
      }
    });
  }

  public render() {
    return (
      <React.Fragment>
        <NavBar currentView={this.state.navLocation} />
        {
          this.state.selectedContentURL &&
            <ContentContainer
              closeAction={this.selectContentItem}
              contentURL={this.state.selectedContentURL} />
        }
        {
          !this.state.selectedContentURL &&
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
        }
      </React.Fragment>
    )
  }

  private filteredArray() {
    // Deep copy state
    const groups = JSON.parse(JSON.stringify(this.state.ItemGroups));
    return groups.map((itemGroup: ContentItemGroup) => {
      itemGroup.Items = itemGroup.Items.filter((item) =>
        [itemGroup.Name, item.Name, item.Description].filter((text) =>
          text && text.toLowerCase().indexOf(this.state.filterString.toLowerCase()) > -1).length);
      return itemGroup;
    }).filter((itemGroup: ContentItemGroup) => itemGroup.Items.length);
  }
}
