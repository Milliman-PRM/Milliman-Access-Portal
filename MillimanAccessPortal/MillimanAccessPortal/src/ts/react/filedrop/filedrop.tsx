import * as React from 'react';
import { NavBar } from '../shared-components/navbar';

export class FileDrop extends React.Component<{}, {}> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public render() {
    return (
        <NavBar currentView={this.currentView} />
    );
  }
}
