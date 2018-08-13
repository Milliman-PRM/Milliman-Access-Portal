import { ajax } from 'jquery';
import * as React from 'react';
import * as Modal from 'react-modal';

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
    this.cancel = this.cancel.bind(this);
  }

  public render() {
    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
      >
        <h2>Create New User</h2>
        <form onSubmit={this.handleCreate}>
          <h3>User to create:</h3>
          <input
            type="text"
            placeholder="Email address"
            onChange={this.handleChange}
          />
          <button
            type="button"
            onClick={this.cancel}
          >
            Cancel
          </button>
          <button
            type="submit"
          >
            Create User
          </button>
        </form>
      </Modal>
    );
  }

  private handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      emailText: event.target.value,
    });
  }

  private handleCreate(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    ajax({
      method: 'POST',
      url: this.url,
    }).done((response) => {
      throw new Error('Not implemented');
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
