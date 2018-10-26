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

import * as React from 'react';

import { getData, postData } from '../../shared';
import { ContactFormModal } from '../contact-form-modal';
import { UserGuideModal } from '../user-guide-modal';
import { NavBarElement } from './interfaces';

export interface NavBarProps {
  currentView: string;
}

export interface NavBarState {
  navBarElements: NavBarElement[];
  contactFormOpen: boolean;
  userGuideOpen: boolean;
}

export class NavBar extends React.Component<NavBarProps, NavBarState> {
  public constructor(props) {
    super(props);

    this.state = {
      navBarElements: null,
      contactFormOpen: false,
      userGuideOpen: false,
    };

    this.openContactForm = this.openContactForm.bind(this);
    this.closeContactForm = this.closeContactForm.bind(this);
    this.openUserGuide = this.openUserGuide.bind(this);
    this.closeUserGuide = this.closeUserGuide.bind(this);
  }

  public componentDidMount() {
    getData('/Account/NavBarElements')
    .then((response) => {
      this.setState({
        navBarElements: response,
      });
    })
    .catch((e) => {
      throw new Error(e);
    });
  }

  public render() {
    const navElements = this.state.navBarElements && this.state.navBarElements.map((element: NavBarElement) => {
      const classes = `nav-element ${(this.props.currentView === element.View) ? 'selected' : null }`;
      return (
        <a href={'/' + element.URL} key={element.View}>
          <div className={classes} style={{ order: element.Order }}>
            <h3 className="nav-element-label">{element.Label}</h3>
            <svg className="nav-element-icon">
              <use xlinkHref={`#${element.Icon}`} />
            </svg>
          </div>
        </a>
      );
    });

    return (
      <nav className={this.state.navBarElements && 'loaded'}>
        {navElements}
        <div className="nav-element" style={{ order: 98 }} onClick={this.openUserGuide}>
          <h3 className="nav-element-label">User Guide</h3>
          <svg className="nav-element-icon">
            <use xlinkHref="#userguide" />
          </svg>
        </div>
        <div className="nav-element" style={{ order: 99 }} onClick={this.openContactForm}>
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
        <ContactFormModal
          isOpen={this.state.contactFormOpen}
          onRequestClose={this.closeContactForm}
        />
        <UserGuideModal
          isOpen={this.state.userGuideOpen}
          onRequestClose={this.closeUserGuide}
          source={this.props.currentView}
        />
      </nav>
    );
  }

  private logout() {
    postData('/Account/Logout', {}, true)
    .then(() => {
      window.location.replace('/');
    })
    .catch((e) => {
      window.location.replace('/');
      throw new Error(e);
    });
  }

  private openContactForm() {
    this.setState({
      contactFormOpen: true,
      userGuideOpen: false,
    });
  }

  private closeContactForm() {
    this.setState({
      contactFormOpen: false,
    });
  }

  private openUserGuide() {
    this.setState({
      contactFormOpen: false,
      userGuideOpen: true,
    });
  }

  private closeUserGuide() {
    this.setState({
      userGuideOpen: false,
    });
  }
}
