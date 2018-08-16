import { ajax } from 'jquery';
import * as React from 'react';
import * as Modal from 'react-modal';

import '../../../../scss/react/shared-components/modal.scss';

export interface AddUserToProfitCenterModalProps extends Modal.Props {
  profitCenterId: number;
}

interface AddUserToProfitCenterModalState {
  userText: string;
}

export class AddUserToProfitCenterModal
    extends React.Component<AddUserToProfitCenterModalProps, AddUserToProfitCenterModalState> {

  private url: string = 'SystemAdmin/AddUserToProfitCenter';

  public constructor(props) {
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
        <h3 className="title blue">Add User</h3>
        <span className="modal-text">User to add:</span>
        <form onSubmit={this.handleSubmit}>
          <input
            type="text"
            placeholder="Email address"
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
              Add User
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
    ajax({
      data: {
        email: this.state.userText,
        profitCenterId: this.props.profitCenterId,
      },
      method: 'POST',
      url: this.url,
    }).done((response) => {
      alert('User added to profit center.');
      this.props.onRequestClose(null);
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
