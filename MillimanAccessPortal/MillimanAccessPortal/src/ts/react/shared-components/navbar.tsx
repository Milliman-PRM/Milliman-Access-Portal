import '../../../images/icons/client.svg';
import '../../../images/icons/content-access.svg';
import '../../../images/icons/content-grid.svg';
import '../../../images/icons/content-publishing.svg';
import '../../../images/icons/email.svg';
import '../../../images/icons/file-drop.svg';
import '../../../images/icons/logout.svg';
import '../../../images/icons/system-admin.svg';
import '../../../images/icons/user-settings.svg';
import '../../../images/icons/userguide.svg';
import '../../../scss/react/shared-components/navbar.scss';

import * as React from 'react';

import { getJsonData, postData } from '../../shared';
import { ContactFormModal } from '../contact-form-modal';
import { UserGuideModal } from '../user-guide-modal';
import { NavBarElement } from './interfaces';

export interface NavBarProps {
  currentView: string;
  userGuidePath?: string;
}

export interface NavBarState {
  navBarElements: NavBarElement[];
  navBarOpen: boolean;
  contactFormOpen: boolean;
  userGuideOpen: boolean;
}

export class NavBar extends React.Component<NavBarProps, NavBarState> {
  public constructor(props: NavBarProps) {
    super(props);

    this.state = {
      navBarElements: null,
      navBarOpen: false,
      contactFormOpen: false,
      userGuideOpen: false,
    };

    this.toggleNavBarOpen = this.toggleNavBarOpen.bind(this);
    this.openContactForm = this.openContactForm.bind(this);
    this.closeContactForm = this.closeContactForm.bind(this);
    this.openUserGuide = this.openUserGuide.bind(this);
    this.closeUserGuide = this.closeUserGuide.bind(this);
  }

  public componentDidMount() {
    getJsonData('/Account/NavBarElements')
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
      const classes = `nav-element ${(this.props.currentView === element.view) ? 'selected' : null }`;
      return (
        <a href={'/' + element.url} key={element.view}>
          <div className={classes} style={{ order: element.order }} title={element.label}>
            <h3 className="nav-element-label">{element.label}</h3>
            <svg className="nav-element-icon">
              <use xlinkHref={`#${element.icon}`} />
            </svg>
          </div>
        </a>
      );
    });

    return (
      <>
        <nav
          className={`${this.state.navBarElements && 'loaded'} ${(this.state.navBarOpen) ? 'open' : 'close'}`}
          onClick={this.toggleNavBarOpen}
        >
          <div onClick={this.stopPropagation}>
            <div id="navbar-button" onClick={this.toggleNavBarOpen}>
              <div id="navbar-button-box">
                <div id="navbar-button-inner" />
              </div>
            </div>
            {navElements}
            <div className="nav-element" style={{ order: 98 }} onClick={this.openUserGuide} title="User Guide">
              <h3 className="nav-element-label">User Guide</h3>
              <svg className="nav-element-icon">
                <use xlinkHref="#userguide" />
              </svg>
            </div>
            <div className="nav-element" style={{ order: 99 }} onClick={this.openContactForm} title="Contact Support">
              <h3 className="nav-element-label">Contact Support</h3>
              <svg className="nav-element-icon">
                <use xlinkHref="#email" />
              </svg>
            </div>
            <div className="nav-element" style={{ order: 100 }} onClick={this.logout} title="Log Out">
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
              source={this.props.userGuidePath ? this.props.userGuidePath : this.props.currentView}
            />
          </div>
        </nav>
        {this.state.navBarOpen && <div id="navBarModalBackground" onClick={this.toggleNavBarOpen} />}
      </>
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

  private stopPropagation(event: React.MouseEvent) {
    event.stopPropagation();
  }

  private toggleNavBarOpen() {
    this.setState({
      navBarOpen: !this.state.navBarOpen,
      contactFormOpen: false,
      userGuideOpen: false,
    });
  }

  private openContactForm() {
    this.setState({
      contactFormOpen: true,
      userGuideOpen: false,
      navBarOpen: false,
    });
  }

  private closeContactForm() {
    this.setState({
      contactFormOpen: false,
      navBarOpen: false,
    });
  }

  private openUserGuide() {
    this.setState({
      contactFormOpen: false,
      userGuideOpen: true,
      navBarOpen: false,
    });
  }

  private closeUserGuide() {
    this.setState({
      userGuideOpen: false,
      navBarOpen: false,
    });
  }
}
