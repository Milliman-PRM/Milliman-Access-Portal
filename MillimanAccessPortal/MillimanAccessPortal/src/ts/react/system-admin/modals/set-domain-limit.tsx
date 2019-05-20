import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { postJsonData } from '../../../shared';
import { Guid } from '../../shared-components/interfaces';

export interface SetDomainLimitClientModalProps extends Modal.Props {
  clientId: Guid;
  existingDomainLimit: number;
}

interface SetDomainLimitClientModalState {
  newDomainLimit: number;
  domainLimitReason: string;
  domainLimitRequestedByPersonName: string;
}

export class SetDomainLimitClientModal
    extends React.Component<SetDomainLimitClientModalProps, SetDomainLimitClientModalState> {

  private url: string = 'SystemAdmin/UpdateClient';

  public constructor(props: SetDomainLimitClientModalProps) {
    super(props);

    this.state = {
      newDomainLimit: this.props.existingDomainLimit,
      domainLimitReason: '',
      domainLimitRequestedByPersonName: '',
    };

    this.handleDomainLimitChange = this.handleDomainLimitChange.bind(this);
    this.handleRequestorChange = this.handleRequestorChange.bind(this);
    this.handleReasonChange = this.handleReasonChange.bind(this);
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
        <h3 className="title blue">Set Domain Limits</h3>
        <span className="modal-text">Domain limit:</span>
        <form>
          <input
            readOnly={true}
            name="clientId"
            style={{display: 'none'}}
          />
          <label>Domain Limit</label>
          <input
            type="number"
            name="newDomainLimit"
            value={this.state.newDomainLimit}
            placeholder="Domain limit"
            onChange={this.handleDomainLimitChange}
          />
          <label>Who Requested the Change?</label>
          <input
            type="text"
            name="domainLimitRequestedByPersonName"
            value={this.state.domainLimitRequestedByPersonName}
            placeholder="Name"
            onChange={this.handleRequestorChange}
          />
          <label>Reason for Changing</label>
          <textarea
            name="domainLimitReason"
            value={this.state.domainLimitReason}
            placeholder="Reason..."
            onChange={this.handleReasonChange}
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
              onClick={this.handleSubmit}
            >
              Update
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleDomainLimitChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      newDomainLimit: event.target.valueAsNumber,
    });
  }

  private handleReasonChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState({
      domainLimitReason: event.target.value,
    });
  }

  private handleRequestorChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      domainLimitRequestedByPersonName: event.target.value,
    });
  }

  private handleSubmit(event: React.FormEvent<HTMLButtonElement>) {
    event.preventDefault();
    postJsonData(this.url, {
      clientId: this.props.clientId,
      domainLimitChange: {
        newDomainLimit: this.state.newDomainLimit,
        domainLimitReason: this.state.domainLimitReason,
        domainLimitRequestedByPersonName : this.state.domainLimitRequestedByPersonName,
      },
    })
    .then(() => {
      alert('Domain limit updated.');
      this.props.onRequestClose(null);
    })
    .catch((err) => {
      alert(err);
    });
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
