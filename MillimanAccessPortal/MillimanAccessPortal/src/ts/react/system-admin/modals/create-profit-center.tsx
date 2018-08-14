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

    this.handleChangeName = this.handleChangeName.bind(this);
    this.handleChangeCode = this.handleChangeCode.bind(this);
    this.handleChangeOffice = this.handleChangeOffice.bind(this);
    this.handleChangeContact = this.handleChangeContact.bind(this);
    this.handleChangeEmail = this.handleChangeEmail.bind(this);
    this.handleChangePhone = this.handleChangePhone.bind(this);
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
              onChange={this.handleChangeName}
            />
          </span>
          <span>
            <label htmlFor="pcCode">Code:</label>
            <input
              name="pcCode"
              type="text"
              onChange={this.handleChangeCode}
            />
          </span>
          <span>
            <label htmlFor="pcOffice">Office:</label>
            <input
              name="pcOffice"
              type="text"
              onChange={this.handleChangeOffice}
            />
          </span>
          <span>
            <label htmlFor="pcContact">Contact:</label>
            <input
              name="pcContact"
              type="text"
              onChange={this.handleChangeContact}
            />
          </span>
          <span>
            <label htmlFor="pcEmail">Email:</label>
            <input
              name="pcEmail"
              type="text"
              onChange={this.handleChangeEmail}
            />
          </span>
          <span>
            <label htmlFor="pcPhone">Phone:</label>
            <input
              name="pcPhone"
              type="text"
              onChange={this.handleChangePhone}
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

  private handleChangeName(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      name: event.target.value,
    });
  }

  private handleChangeCode(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      code: event.target.value,
    });
  }

  private handleChangeOffice(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      office: event.target.value,
    });
  }

  private handleChangeContact(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      contact: event.target.value,
    });
  }

  private handleChangeEmail(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      email: event.target.value,
    });
  }

  private handleChangePhone(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      phone: event.target.value,
    });
  }

  private handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    ajax({
      data: {
        Name: this.state.name,
        ProfitCenterCode: this.state.code,
        MillimanOffice: this.state.office,
        ContactName: this.state.contact,
        ContactEmail: this.state.email,
        ContactPhone: this.state.phone,
      },
      method: 'POST',
      url: this.url,
    }).done((response) => {
      alert('Profit center created.');
      this.props.onRequestClose(null);
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
