import '../../../scss/react/authorized-content/authorized-content.scss';

import * as React from 'react';

import { getData } from '../../shared';
import { ContentTypeEnum } from '../../view-models/content-publishing';
import { ContentContainer } from '../shared-components/content-container';
import { Filter } from '../shared-components/filter';
import { NavBar } from '../shared-components/navbar';
import { ContentCard } from './content-card';
import { ContentItem, ContentItemGroup, ContentItemGroupList, Filterable } from './interfaces';

interface AuthorizedContentState extends ContentItemGroupList, Filterable { }
export class AuthorizedContent extends React.Component<{}, AuthorizedContentState> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public constructor(props) {
    super(props);

    this.state = {
      ItemGroups: [],
      selectedContentURL: null,
      selectedContentType: null,
      filterString: '',
    };

    this.setFilterString = this.setFilterString.bind(this);
  }

  public componentDidMount() {
    getData('AuthorizedContent/Content/')
    .then((json: ContentItemGroupList) => this.setState(json));
    window.onpopstate = () => {
      if (window.history && window.history.pushState) {
        const hashName = location.hash.split('#!/')[1];
        if (hashName !== '' && window.location.hash === '') {
          if (this.state.selectedContentURL) {
            this.setState({
              selectedContentURL: null,
              selectedContentType: null,
            }, () => {
              const display = null;
              document.getElementById('page-header').style.display = display;
              document.getElementById('page-footer').style.display = display;
              document.getElementById('authorized-content-container').style.display = display;
            });
          }
        }
      }
    };
  }

  public selectContentItem = (contentURL: string, contentType: ContentTypeEnum) => {
    this.setState({
      selectedContentURL: contentURL,
      selectedContentType: contentType,
    }, () => {
      const display = (this.state.selectedContentURL) ? 'none' : null;
      document.getElementById('page-header').style.display = display;
      document.getElementById('page-footer').style.display = display;
      document.getElementById('authorized-content-container').style.display = display;
      if (!contentURL) {
        // IE doesn't support popstate on URL hashchange so we need to treat it differently here.
        const isIE = navigator.userAgent.indexOf('MSIE ') > -1 || navigator.userAgent.indexOf('Trident/') > -1;
        if (!isIE) {
          history.back();
        } else {
          location.reload();
        }
      }
    });
  }

  public setFilterString(filterString: string) {
    this.setState({
      filterString,
    });
  }

  public render() {
    const clientGroups = this.filteredArray().map((client: ContentItemGroup) => {
      const clientItems = client.Items.map((contentItem: ContentItem) => (
        <ContentCard
          key={contentItem.Id.toString()}
          selectContent={this.selectContentItem}
          {...contentItem}
        />
      ));
      return (
        <div key={`client-${client.Id}`} className="client-content-container">
          <h1 className="client-name">{client.Name}</h1>
          {clientItems}
        </div>
      );
    });
    const contentContainer = this.state.selectedContentURL
      ? (
        <ContentContainer
          contentURL={this.state.selectedContentURL}
          contentType={this.state.selectedContentType}
        />
      )
      : null;
    return (
      <React.Fragment>
        <NavBar currentView={this.currentView} />
        {contentContainer}
        <div id="authorized-content-container">
          <div id="authorized-content-header">
            <Filter
              filterText={this.state.filterString}
              setFilterText={this.setFilterString}
              placeholderText="Filter content"
            />
          </div>
          <div id="authorized-content-items">
            {clientGroups}
          </div>
        </div>
      </React.Fragment>
    );
  }

  private filteredArray() {
    // Deep copy state
    const groups = JSON.parse(JSON.stringify(this.state.ItemGroups)) as ContentItemGroup[];
    return groups.map((itemGroup: ContentItemGroup) => {
      itemGroup.Items = itemGroup.Items.filter((item) =>
        [itemGroup.Name, item.Name, item.Description].filter((text) =>
          text && text.toLowerCase().indexOf(this.state.filterString.toLowerCase()) > -1).length);
      return itemGroup;
    }).filter((itemGroup: ContentItemGroup) => itemGroup.Items.length);
  }
}
