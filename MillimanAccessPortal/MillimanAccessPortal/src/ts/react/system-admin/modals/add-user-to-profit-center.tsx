import { ajax } from 'jquery';
import * as React from 'react';
import * as Modal from 'react-modal';

interface AddUserToProfitCenterModalState {
  userText: string;
}

export class AddUserToProfitCenterModal extends React.Component<Modal.Props, AddUserToProfitCenterModalState> {

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
