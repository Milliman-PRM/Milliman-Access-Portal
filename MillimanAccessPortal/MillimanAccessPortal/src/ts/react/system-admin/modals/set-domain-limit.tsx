import '../../../../scss/react/shared-components/form-elements.scss';
import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';
import * as Yup from 'yup';

import { postJsonData } from '../../../shared';
import { Guid } from '../../shared-components/interfaces';

import { BaseFormState, Form } from '../../shared-components/form';
import { Input, TextAreaInput } from '../../shared-components/input';

export interface SetDomainLimitClientModalProps extends Modal.Props {
  clientId: Guid;
  existingDomainLimit: number;
}

interface SetDomainLimitClientModalState extends BaseFormState {
  data: {
    newDomainLimit: string;
    domainLimitRequestedByPersonName: string;
    domainLimitReason: string;
  };
}

export class SetDomainLimitClientModal extends Form<
  SetDomainLimitClientModalProps,
  SetDomainLimitClientModalState
  > {

  protected schema = Yup.object({
    newDomainLimit: Yup.string()
    .required()
    .label('New Domain Limit'),
    domainLimitRequestedByPersonName: Yup.string()
    .required()
    .label('Who Requested the Change?'),
    domainLimitReason: Yup.string()
    .required()
    .label('Reason for Changing the Domain Limit'),
  });

  public constructor(props: SetDomainLimitClientModalProps) {
    super(props);

    this.state = {
      data: {
        newDomainLimit: this.props.existingDomainLimit.toString(),
        domainLimitRequestedByPersonName: '',
        domainLimitReason: '',
      },
      errors: {},
      formIsValid: false,
    };
  }

  public render() {
    const { data, errors, formIsValid } = this.state;

    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
        className="modal"
        overlayClassName="modal-overlay"
      >
        <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-5-12">
          <div className="form-section">
            <form onSubmit={this.handleSubmit}>
              <h3 className="form-section-title">Set Domain Limits</h3>
              <Input
                name="newDomainLimit"
                label="New Domain Limit"
                type="number"
                value={data.newDomainLimit}
                onChange={this.handleDomainLimitChange}
                onBlur={this.handleBlur}
                error={errors.newDomainLimit}
                autoFocus={true}
              />
              <Input
                name="domainLimitRequestedByPersonName"
                label="Who Requested the Change?"
                type="string"
                value={data.domainLimitRequestedByPersonName}
                onChange={this.handleRequestorChange}
                onBlur={this.handleBlur}
                error={errors.domainLimitRequestedByPersonName}
              />
              <TextAreaInput
                name="domainLimitReason"
                label="Reason for Changing the Domain Limit"
                type="string"
                value={data.domainLimitReason}
                onChange={this.handleReasonChange}
                onBlur={this.handleBlur}
                error={errors.domainLimitReason}
              />
              <div className="button-container">
                <button
                  type="button"
                  className="blue-button"
                  onClick={this.cancel}
                >
                  Cancel
                </button>
              </div>
              <div className="button-container">
                <button
                  type="submit"
                  disabled={!formIsValid}
                  className="blue-button"
                >
                  Update Domain Limits
                </button>
              </div>
            </form>
          </div>
        </div>
      </Modal>
    );
  }

  public handleSubmit = async (e: React.FormEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    postJsonData('SystemAdmin/UpdateClient', {
      clientId: this.props.clientId,
      domainLimitChange: {
        newDomainLimit: this.state.data.newDomainLimit,
        domainLimitReason: this.state.data.domainLimitReason,
        domainLimitRequestedByPersonName : this.state.data.domainLimitRequestedByPersonName,
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

  private handleDomainLimitChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState((prevState) => ({
      ...prevState,
      data: {
        ...prevState.data,
        newDomainLimit: event.target.valueAsNumber.toString(),
      },
    }));
  }

  private handleReasonChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState((prevState) => ({
      ...prevState,
      data: {
        ...prevState.data,
        domainLimitReason: event.target.value,
      },
    }));
  }

  private handleRequestorChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState((prevState) => ({
      ...prevState,
      data: {
        ...prevState.data,
        domainLimitRequestedByPersonName: event.target.value,
      },
    }));
  }

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
