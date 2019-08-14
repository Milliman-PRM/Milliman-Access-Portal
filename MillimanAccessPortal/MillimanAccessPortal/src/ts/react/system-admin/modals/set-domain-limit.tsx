import '../../../../scss/react/shared-components/form-elements.scss';
import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';
import * as Yup from 'yup';

import { postJsonData } from '../../../shared';
import { Guid } from '../../shared-components/interfaces';

import { BaseFormState, Form } from '../../shared-components/form/form';
import { Input, TextAreaInput } from '../../shared-components/form/input';

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
      .min(0)
      .label('New Domain Limit'),
    domainLimitRequestedByPersonName: Yup.string()
      .email()
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

    this.handleDomainLimitChange = this.handleDomainLimitChange.bind(this);
    this.handleRequestorChange = this.handleRequestorChange.bind(this);
    this.handleReasonChange = this.handleReasonChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
    this.cancel = this.cancel.bind(this);
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
            type="text"
            value={data.domainLimitRequestedByPersonName}
            onChange={this.handleRequestorChange}
            onBlur={this.handleBlur}
            error={errors.domainLimitRequestedByPersonName}
          />
          <TextAreaInput
            name="domainLimitReason"
            label="Reason for Changing the Domain Limit"
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
            <button
              type="submit"
              disabled={!formIsValid
                || this.props.existingDomainLimit.toString() === this.state.data.newDomainLimit}
              className="blue-button"
            >
              Update Domain Limits
            </button>
          </div>
        </form>
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
      this.setState({
        data: {
          newDomainLimit: this.state.data.newDomainLimit,
          domainLimitRequestedByPersonName: '',
          domainLimitReason: '',
        },
        errors: {},
        formIsValid: false,
      });
    })
    .catch((err) => {
      alert(err);
    });
  }

  private handleDomainLimitChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.valueAsNumber >= 0) {
      const data = {
        ...this.state.data,
        newDomainLimit: event.target.valueAsNumber.toString(),
      };

      const errorMessage = this.validateProperty(event.target.valueAsNumber.toString());
      const errors = {...this.state.errors};
      this.validate();

      if (!errorMessage) {
        delete errors[data.newDomainLimit];
      }

      this.setState({ data, errors });
    }
  }

  private handleRequestorChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const data = {
      ...this.state.data,
      domainLimitRequestedByPersonName: event.target.value,
    };

    const errorMessage = this.validateProperty(event.target.value);
    const errors = {...this.state.errors};
    this.validate();

    if (!errorMessage) {
      delete errors[data.domainLimitRequestedByPersonName];
    }

    this.setState({ data, errors });
  }

  private handleReasonChange = (event: React.ChangeEvent<HTMLTextAreaElement>) => {
    const data = {
      ...this.state.data,
      domainLimitReason: event.target.value,
    };

    const errorMessage = this.validateProperty(event.target.value);
    const errors = {...this.state.errors};
    this.validate();

    if (!errorMessage) {
      delete errors[data.domainLimitReason];
    }

    this.setState({ data, errors });
}

  private cancel(event: React.MouseEvent<HTMLButtonElement>) {
    this.props.onRequestClose(event.nativeEvent);
  }
}
