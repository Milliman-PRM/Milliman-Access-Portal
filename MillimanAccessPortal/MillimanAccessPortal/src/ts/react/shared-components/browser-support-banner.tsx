import '../../../scss/react/shared-components/browser-support-banner.scss';

import * as React from 'react';

interface BrowserSupportBannerState {
  browserWillNoLongerBeSupported: boolean;
  hasAcceptedBrowserSupportNotice: boolean;
}

export class BrowserSupportBanner extends React.Component<{}, BrowserSupportBannerState> {

  public constructor(props: any) {
    super(props);

    // Check if the users browser is one that will no longer be supported
    const browserWillNoLongerBeSupported = this.browserWillBeSupported();

    // Check if the user has accepted the notification during this session already
    const hasAcceptedBrowserSupportNotice = sessionStorage.getItem('hasAcceptedBrowserSupportNotice') ? true : false;

    this.state = {
      browserWillNoLongerBeSupported,
      hasAcceptedBrowserSupportNotice,
    };
  }

  public browserWillBeSupported = () => {
    if ('fetch' in window && 'Promise' in window && 'Map' in window && 'Set' in window) {
      return true;
    }
    return false;
  }

  public acceptNotice = () => {
    if (!this.state.hasAcceptedBrowserSupportNotice) {
      sessionStorage.setItem('hasAcceptedBrowserSupportNotice', 'true');
      this.setState({ hasAcceptedBrowserSupportNotice: true });
    }
  }

  public render() {
    const { browserWillNoLongerBeSupported, hasAcceptedBrowserSupportNotice } = this.state;

    return (
      (browserWillNoLongerBeSupported && !hasAcceptedBrowserSupportNotice) &&
      (
        <div className="browser-support-banner">
          <p>This browser will no longer be supported as of 8/17/2021</p>
          <button onClick={this.acceptNotice}>Accept</button>
        </div>
      )
    );
  }
}
