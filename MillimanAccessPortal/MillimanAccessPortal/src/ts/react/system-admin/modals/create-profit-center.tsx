import { ajax } from 'jquery';
import * as React from 'react';
import * as Modal from 'react-modal';

interface CreateProfitCenterModalState {
  name: string;
  code: string;
  office: string;
  contact: string;
  email: string;
  phone: string;
}

export class CreateProfitCenterModal extends React.Component<Modal.Props, CreateProfitCenterModalState> {

  private url: string = 'SystemAdmin/CreateProfitCenter';

  public constructor(props) {
    super(props);

    this.state = {
      name: '',
      code: '',
      office: '',
      contact: '',
      email: '',
      phone: '',
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
        <h2>Create New Profit Center</h2>
        <form onSubmit={this.handleSubmit}>
          <h3>Profit Center Information</h3>
          <span>
            <label htmlFor="pcName">Name:</label>
            <input
              name="pcName"
              type="text"
              onChange={null}
            />
          </span>
          <button
            type="button"
            onClick={this.cancel}
          >
            Cancel
          </button>
          <button
            type="submit"
          >
            Create Profit Center
          </button>
        </form>
      </Modal>
    );
  }

  private handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      name: event.target.value,
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
