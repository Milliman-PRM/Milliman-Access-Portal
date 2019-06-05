import * as React from 'react';
import { postData } from '../../shared';

interface UserAgreementProps {
  agreementAccept: AgreementAcceptance['request'];
  declineButtonEnabled: boolean;
  acceptButtonEnabled: boolean;
}
export class UserAgreement extends React.Component<UserAgreementProps> {

  private renderAcceptanceSection() {
    const { acceptButtonEnabled } = this.props;
    return (
      <div className="form-submission-section">
        <div className="button-container button-container-update">
          {this.renderDeclineButton()}
          <button
            type="submit"
            className="button-submit blue-button"
            disabled={acceptButtonEnabled ? false : true}
            onClick={(event: React.FormEvent) => {
              event.preventDefault();
              if (acceptButtonEnabled) {
                this.props.AgreementAcceptance(this.props.agreementAccept);
              }
            }}
          >
            Accept
          </button>
        </div>
      </div>
    );
  }

  private renderDeclineButton() {
    const { declineButtonEnabled } = this.props;
    return declineButtonEnabled
    ? (
      <button
        type="button"
        className="button-reset link-button"
        onClick={() => this.logout()}
      >
        Decline
      </button>
    )
    : null;
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
}
