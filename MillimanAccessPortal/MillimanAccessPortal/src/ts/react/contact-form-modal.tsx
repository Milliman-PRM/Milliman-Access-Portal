import * as React from 'react';
import * as Modal from 'react-modal';

import { postData } from '../shared';

import '../../scss/react/shared-components/modal.scss';

interface ContactFormModalState {
  topic: string;
  message: string;
}

export class ContactFormModal extends React.Component<Modal.Props, ContactFormModalState> {

  private readonly recipient: string = 'support.78832.5ad4ee0bf11242a6@helpscout.net';

  public constructor(props) {
    super(props);

    this.state = {
      topic: '',
      message: '',
    };

    this.handleChangeTopic = this.handleChangeTopic.bind(this);
    this.handleChangeMessage = this.handleChangeMessage.bind(this);
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
        <h3 className="title blue">Contact Support</h3>
        <form onSubmit={this.handleSubmit}>
          <div>
            <select
              className="modal-input"
              required={true}
              onChange={this.handleChangeTopic}
            >
              <option value="">Please Select a Topic</option>
              <option value="Account Inquiery">Account Inquiry</option>
              <option value="Bug Report">Report a Bug</option>
              <option value="Other Support Question">Other</option>
            </select>
            <textarea
              className="modal-input"
              placeholder="Message"
              required={true}
              onChange={this.handleChangeMessage}
            />
          </div>
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
              Submit
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleChangeTopic(event: React.ChangeEvent<HTMLSelectElement>) {
    this.setState({
      topic: event.target.value,
    });
  }

  private handleChangeMessage(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState({
      message: event.target.value,
    });
  }

  private handleSubmit(event: React.MouseEvent<HTMLFormElement> | React.KeyboardEvent<HTMLFormElement>) {
    event.preventDefault();
    event.persist();
    postData('Message/SendEmailFromUser', {
      subject: this.state.topic,
      message: this.state.message,
      recipient: this.recipient,
    }, true)
    .then(() => {
      alert('Submission received.');
      this.props.onRequestClose(event.nativeEvent);
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
