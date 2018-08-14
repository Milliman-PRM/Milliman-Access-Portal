import { ajax } from 'jquery';
import * as React from 'react';
import * as Modal from 'react-modal';

export interface AddUserToClientModalProps extends Modal.Props {
  clientId: number;
}

interface AddUserToClientModalState {
  userText: string;
}

export class AddUserToClientModal
    extends React.Component<AddUserToClientModalProps, AddUserToClientModalState> {

  private url: string = 'SystemAdmin/AddUserToClient';

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
      >
        <h2>Add User</h2>
        <form onSubmit={this.handleSubmit}>
          <h3>User to add:</h3>
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
            Add User
          </button>
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
        clientId: this.props.clientId,
      },
      method: 'POST',
      url: this.url,
    }).done((response) => {
      alert('User added to client.');
      this.props.onRequestClose(null);
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
