import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { postData } from '../../../shared';

interface CreateUserModalState {
  emailText: string;
}

export class CreateUserModal extends React.Component<Modal.Props, CreateUserModalState> {

  private url: string = 'SystemAdmin/CreateUser';

  public constructor(props) {
    super(props);

    this.state = {
      emailText: '',
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
        <h3 className="title blue">Create New User</h3>
        <span className="modal-text">User to create:</span>
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
              Create User
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      emailText: event.target.value,
    });
  }

  private handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    postData(this.url, {
      email: this.state.emailText,
    })
    .then(() => {
      alert('User created.');
      this.props.onRequestClose(null);
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
