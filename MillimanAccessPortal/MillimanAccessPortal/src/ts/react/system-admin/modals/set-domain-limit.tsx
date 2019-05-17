import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { postData } from '../../../shared';
import { Guid } from '../../shared-components/interfaces';

export interface SetDomainLimitClientModalProps extends Modal.Props {
  clientId: Guid;
}

interface SetDomainLimitClientModalState {
  userText: string;
}

export class SetDomainLimitClientModal
    extends React.Component<SetDomainLimitClientModalProps, SetDomainLimitClientModalState> {

  private url: string = 'SystemAdmin/AddUserToClient';

  public constructor(props: SetDomainLimitClientModalProps) {
    super(props);

    this.state = {
      userText: '',
    };

    this.handleChange = this.handleChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
    this.cancel = this.cancel.bind(this);
  }

  public render() {
    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
        className="modal"
        overlayClassName="modal-overlay"
      >
        <h3 className="title blue">Set Domain Limits</h3>
        <span className="modal-text">Domain limit:</span>
        <form onSubmit={this.handleSubmit}>
          <input
            readOnly={true}
            name="clientId"
            style={{display: 'none'}}
          />
          <label>Domain Limit</label>
          <input
            type="int"
            placeholder="Domain limit"
            onChange={this.handleChange}
          />
          <label>Reason for Changing</label>
          <input
            type="textarea"
            placeholder="Reason..."
            onChange={this.handleChange}
          />
          <label>Who Requested the Change?</label>
          <input
            type="text"
            placeholder="Name"
            onChange={this.handleChange}
          />
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={this.cancel}
            >
              Cancel
            </button>
            <button
              className="blue-button"
              type="submit"
            >
              Update
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      userText: event.target.value,
    });
  }

  private handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    postData(this.url, {
      domainLimit: this.state.userText,
      clientId: this.props.clientId,
    })
    .then(() => {
      alert('Domain limit updated.');
      this.props.onRequestClose(null);
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
