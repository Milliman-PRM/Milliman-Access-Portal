import '../../../images/client-admin.svg';
import '../../../images/content-access.svg';
import '../../../images/content-grid.svg';
import '../../../images/content-publishing.svg';
import '../../../images/email.svg';
import '../../../images/logout.svg';
import '../../../images/system-admin.svg';
import '../../../images/user-settings.svg';
import '../../../images/userguide.svg';
import '../../../scss/react/shared-components/navbar.scss';

import 'promise-polyfill/src/polyfill';
import 'whatwg-fetch';

import * as React from 'react';

import { NavBarElement, NavBarProps, NavBarState } from './interfaces';

export class NavBar extends React.Component<NavBarProps, NavBarState> {
  public constructor(props) {
    super(props);
    this.state = {
      navBarElements: [],
      navBarIsLoaded: false,
    };
  }

  public componentDidMount() {
    fetch('/Account/NavBarElements', {
      credentials: 'same-origin',
    })
    .then((response) => response.json())
    .then((json) => {
      this.setState({
        navBarElements: json,
        navBarIsLoaded: true,
      });
    })
    .catch((e) => {
      throw new Error(e);
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
      window.location.replace('/');
    })
    .catch((e) => {
      throw new Error(e);
    });
  }

  public render() {
    const navElements = this.state.navBarElements.map((element: NavBarElement) => {
      const classes = `nav-element ${(this.props.currentView === element.view) ? 'selected' : null }`;
      return (
        <a href={'/' + element.url} key={element.view}>
          <div className={classes} style={{ order: element.order }}>
            <h3 className="nav-element-label">{element.label}</h3>
            <svg className="nav-element-icon">
              <use xlinkHref={`#${element.icon}`} />
            </svg>
          </div>
        </a>
      );
    });

    return (
      <nav className={this.state.navBarIsLoaded ? 'loaded' : null}>
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
