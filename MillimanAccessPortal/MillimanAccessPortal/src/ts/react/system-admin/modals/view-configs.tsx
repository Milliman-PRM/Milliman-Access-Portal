import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { TextAreaInput } from '../../shared-components/form/input';

export interface ViewConfigsModalProps extends Modal.Props {
  configurations: string;
}

export class ViewConfigsModal
  extends React.Component<ViewConfigsModalProps, {}> {

  public constructor(props: ViewConfigsModalProps) {
    super(props);

    this.handleCopy = this.handleCopy.bind(this);
  }

  public render() {
    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
        className="modal"
        overlayClassName="modal-overlay"
        onRequestClose={() => this.props.onRequestClose(null)}
      >
        <h3 className="title blue">View System Configurations</h3>
        <form onSubmit={() => this.props.onRequestClose(null)}>
          <TextAreaInput
            name={'configurations'}
            label={'Configurations'}
            value={this.props.configurations}
            error={null}
            readOnly={true}
          />
          <div className="button-container">
            <button
              className="blue-button"
              type="button"
              onClick={null}
            >
              Copy to Clipboard
            </button>
            <button
              className="link-button"
              type="submit"
              onClick={() => {
                this.props.onRequestClose(null);
              }}
            >
              Close
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleCopy(event: React.ChangeEvent<HTMLButtonElement>) {
    this.setState({
      email: event.target.value,
      emailError: false,
    });
  }
}
