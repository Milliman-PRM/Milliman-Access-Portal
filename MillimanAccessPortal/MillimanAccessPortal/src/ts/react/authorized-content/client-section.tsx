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
    return (this.props.name.toLowerCase().indexOf(this.props.filterString) !== -1) &&
    (
      <div key={`client-${this.props.id}`} className="client-content-container">
        <h1 className="client-name">{this.props.name}</h1>
        {
          this.props.items.map((item) => 
            <ContentCard {...item} filterString={this.props.filterString} />
          )
        }
      </div>
    )
  }
}
