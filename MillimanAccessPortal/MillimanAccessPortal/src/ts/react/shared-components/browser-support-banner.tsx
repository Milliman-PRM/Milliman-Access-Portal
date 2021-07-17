import '../../../scss/react/shared-components/browser-support-banner.scss';

import * as React from 'react';

import { ActionIcon } from './action-icon';

interface BrowserSupportBannerState {
  browserSupportHasExpired: boolean;
  browserWillBeSupported: boolean;
  hasAcceptedBrowserSupportNotice: boolean;
}

export class BrowserSupportBanner extends React.Component<{}, BrowserSupportBannerState> {

  public constructor(props: any) {
    super(props);

    // Check if the browser support expiration date has already passed
    const browserSupportHasExpired = this.browserSupportHasExpired();

    // Check if the users browser is one that will no longer be supported
    const browserWillBeSupported = this.browserWillBeSupported();

    // Check if the user has accepted the notification during this session already
    const hasAcceptedBrowserSupportNotice = sessionStorage.getItem('hasAcceptedBrowserSupportNotice') ? true : false;

    this.state = {
      browserSupportHasExpired,
      browserWillBeSupported,
      hasAcceptedBrowserSupportNotice,
    };
  }

  public browserWillBeSupported = () => {
    const WebAssembly: any = (global as any).WebAssembly;
    try {
      if (typeof WebAssembly === 'object' && typeof WebAssembly.instantiate === 'function') {
        const module = new WebAssembly.Module(Uint8Array.of(0x0, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00));
        if (module instanceof WebAssembly.Module) {
          return new WebAssembly.Instance(module) instanceof WebAssembly.Instance;
        }
      }
    } catch (e) {
      return false;
    }
    return false;
  }

  public acceptNotice = () => {
    if (!this.state.hasAcceptedBrowserSupportNotice) {
      sessionStorage.setItem('hasAcceptedBrowserSupportNotice', 'true');
      this.setState({ hasAcceptedBrowserSupportNotice: true });
    }
  }

  public browserSupportHasExpired = () => {
    const expirationDate = new Date('2021-08-17').getTime();
    const today = new Date().getTime();
    return expirationDate <= today;
  }

  public render() {
    const { browserSupportHasExpired, browserWillBeSupported, hasAcceptedBrowserSupportNotice } = this.state;
    const browserSupportBannerClass = (browserSupportHasExpired) ? 'support-expired' : 'support-expiring-soon';

    return (
      (
        !browserWillBeSupported &&
        !hasAcceptedBrowserSupportNotice
      ) &&
      (
        <div className={`browser-support-banner ${browserSupportBannerClass}`}>
          <p>
            {
              browserSupportHasExpired ? (
                <>
                  <strong>Browser Support Has Expired</strong>&nbsp;&nbsp;
                  MAP no longer supports this browser.&nbsp;
                </>
              ) : (
                <>
                  <strong>Browser Support Expiration</strong>&nbsp;&nbsp;
                  MAP will not support this browser after August 17, 2021.&nbsp;
                </>
              )
            }
            Please use one of our supported browsers (Edge, Chrome, Firefox) instead.
          </p>
          <ActionIcon
            icon="cancel"
            action={this.acceptNotice}
            label="Accept"
          />
        </div>
      )
    );
  }
}
