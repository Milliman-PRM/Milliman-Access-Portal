import * as React from 'react';
import { Component } from 'react';
import '../../../scss/react/authorized-content/content-card.scss';
import { ContentItem, ContentItemGroup, Filterable } from './interfaces';
import { FileLink } from './file-link';
import { ContentCard } from './content-card';

require('tooltipster');
require('tooltipster/src/css/tooltipster.css');

interface ClientSectionProps extends ContentItemGroup, Filterable { }
export class ClientSection extends Component<ClientSectionProps, {}> {
  public render() {
    // Stop filter string propogation if the section name matches the filter
    const filterString = this.props.name.toLowerCase().indexOf(this.props.filterString) === -1
      ? this.props.filterString
      : '';
    const items = this.props.items.map((item) => 
      <ContentCard {...item} key={item.id.toString()} filterString={filterString} />
    )
    // If no children match the filter, don't render anything
    return this.props.items.filter((item) =>
      item.name.toLowerCase().indexOf(filterString.toLowerCase()) !== -1).length
    ? (
        <div key={`client-${this.props.id}`} className="client-content-container">
          <h1 className="client-name">{this.props.name}</h1>
          {items}
        </div>
      )
    : null;
  }
}
