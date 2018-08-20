import '../../../scss/react/shared-components/navbar.scss';

import * as React from 'react';
import 'whatwg-fetch';
import 'promise-polyfill/src/polyfill';

import { NavBarProps, NavBarState } from './interfaces';

export class NavBar extends React.Component<NavBarProps, NavBarState> {
  public constructor(props) {
    super(props);
    this.state = {
      NavBarElements: [],
    }
  }  

  public componentDidMount() {
    const elements = fetch('/Account/NavBarElements', {
      credentials: 'include'
      }).then(response => response.json()
       ).then(json => {
         this.setState({ NavBarElements: json });
      }).catch(ex => {
        console.log('parsing failed', ex)
      });
  }
  
  public render() {
    return (
      <nav>
      </nav>
    );
  }
}
