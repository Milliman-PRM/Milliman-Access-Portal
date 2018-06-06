import * as React from 'react';
import { Component } from 'react';
import '../../../scss/react/authorized-content/authorized-content.scss';
import { ContentCollection } from './content-collection';


export class AuthorizedContent extends Component<{}, {}> {
  public render() {
    return (
      <ContentCollection />
    );
  }
}
