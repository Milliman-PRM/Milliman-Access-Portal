import '../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { postData } from '../shared';
import { TextAreaInput } from './shared-components/form/input';
import { DropDown } from './shared-components/form/select';

// Toastr related imports
import toastr = require('toastr');
import '../lib-options';
require('toastr/toastr.scss');

interface ContactFormModalState {
  topic: string;
  message: string;
  errors: {
    topic: string;
    message: string;
  };
}

interface ContactFormModalProps extends Modal.Props {
  topics: string[];
}

export class ContactFormModal extends React.Component<ContactFormModalProps, ContactFormModalState> {

  public constructor(props: ContactFormModalProps) {
    super(props);

    this.state = {
      topic: '',
      message: '',
      errors: {
        topic: '',
        message: '',
      },
    };

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
            <DropDown
              name="supportTopic"
              label="Support Topic *"
              placeholderText="Select a Topic..."
              values={this.props.topics.map((topic) => {
                return {
                  selectionValue: topic,
                  selectionLabel: topic,
                };
              })}
              value={this.state.topic}
              onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                this.setState({
                  topic: target.value,
                });
              }}
              readOnly={false}
              error={this.state.errors.topic}
            />
            <TextAreaInput
              label="Message *"
              name="ContactFormMessage"
              onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                this.setState({
                  message: target.value,
                });
              }}
              error={this.state.errors.message}
              value={this.state.message}
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
              disabled={!(this.state.topic && this.state.message)}
            >
              Submit
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleSubmit(event: React.MouseEvent<HTMLFormElement> | React.KeyboardEvent<HTMLFormElement>) {
    event.preventDefault();
    event.persist();
    postData('/Message/SendSupportEmail', {
      subject: this.state.topic,
      message: this.state.message,
    }, true)
    .then(() => {
      toastr.success('Submission received.');
      this.props.onRequestClose(event.nativeEvent);
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
