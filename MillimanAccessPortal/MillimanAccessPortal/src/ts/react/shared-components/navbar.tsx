import '../../../scss/react/shared-components/navbar.scss';

import * as React from 'react';
import 'whatwg-fetch';
import 'promise-polyfill/src/polyfill';

import '../../../images/content-grid.svg';
import '../../../images/system-admin.svg';
import '../../../images/client-admin.svg';
import '../../../images/content-publishing.svg';
import '../../../images/content-access.svg';
import '../../../images/user-settings.svg';
import '../../../images/userguide.svg';
import '../../../images/email.svg';
import '../../../images/logout.svg';

import { NavBarProps, NavBarState, NavBarElement } from './interfaces';

export class NavBar extends React.Component<NavBarProps, NavBarState> {
  public constructor(props) {
    super(props);
    this.state = {
      NavBarElements: [],
      NavBarIsLoaded: false,
    }
  }  

  public componentDidMount() {
    const elements = fetch('/Account/NavBarElements', {
      credentials: 'same-origin'
    })
    .then(response => response.json())
    .then(json => {
      this.setState({
        NavBarElements: json,
        NavBarIsLoaded: true,
      });
    })
    .catch(ex => {
      console.log('error:', ex)
    });
  }

  public logout() {
    fetch('/account/logout', {
      credentials: 'same-origin',
      method: 'POST',
      headers: {
        RequestVerificationToken: document.getElementsByName('__RequestVerificationToken')[0].getAttribute('value'),
      },
    })
      .then(() => {
        window.location.replace('/')
      })
      .catch((e) => {
        console.log("An error occured:", e);
      });
  }
  
  public render() {
    const navElements = this.state.NavBarElements.map((element: NavBarElement) => {
      const classes = `nav-element ${(this.props.currentView === element.View) ? 'selected' : null }`;
      return (
        <a href={"/" + element.URL} key={element.View}>
          <div className={ classes } style={{ order: element.Order }}>
            <h3 className="nav-element-label">{ element.Label }</h3>
            <svg className="nav-element-icon">
              <use xlinkHref={`#${element.Icon}`} />
            </svg>
          </div>
        </a>
      )
    })

    return (
      <nav className={ this.state.NavBarIsLoaded ? 'loaded' : null }>
        {navElements}
        <div className="nav-element" style={{ order: 98 }}>
          <h3 className="nav-element-label">User Guide</h3>
          <svg className="nav-element-icon">
            <use xlinkHref="#userguide" />
          </svg>
        </div>
        <div className="nav-element" style={{ order: 99 }}>
          <h3 className="nav-element-label">Contact Support</h3>
          <svg className="nav-element-icon">
            <use xlinkHref="#email" />
          </svg>
        </div>
        <div className="nav-element" style={{ order: 100 }} onClick={this.logout}>
          <h3 className="nav-element-label">Log Out</h3>
          <svg className="nav-element-icon">
            <use xlinkHref="#logout" />
          </svg>
        </div>
      </nav>
    );
  }
}
