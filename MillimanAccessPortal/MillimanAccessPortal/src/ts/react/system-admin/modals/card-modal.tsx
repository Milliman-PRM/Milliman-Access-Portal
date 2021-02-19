import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { getJsonData, isEmailAddressValid, postData, postJsonData } from '../../../shared';
import { Input, MultiAddInput } from '../../shared-components/form/input';
import { ProfitCenterDetail } from '../interfaces';

// Toastr related imports
import toastr = require('toastr');
import '../../../lib-options';
require('toastr/toastr.scss');

interface CardModalProps extends Modal.Props {
  // render: (props: CardModalProps) => JSX.Element;
  profitCenterId?: string;
}
interface CardModalState {
  name: string;
  code: string;
  office: string;
  contact: string;
  email: string;
  phone: string;
  quarterlyMaintenanceNotificationList: string[];
}

export class CardModal extends React.Component<CardModalProps, CardModalState> {

  private url: string = 'SystemAdmin/UpdateProfitCenter';

  public constructor(props: CardModalProps) {
    super(props);

    this.state = {
      name: '',
      code: '',
      office: '',
      contact: '',
      email: '',
      phone: '',
      quarterlyMaintenanceNotificationList: [],
    };
  }

  public componentDidMount() {
    if (this.props.profitCenterId) {
      getJsonData('SystemAdmin/ProfitCenterDetail', {
        profitCenterId: this.props.profitCenterId,
      })
      .then((response: ProfitCenterDetail) => {
        this.setState({
          name: response.name,
          code: response.code,
          office: response.office,
          contact: response.contactName,
          email: response.contactEmail,
          phone: response.contactPhone,
          quarterlyMaintenanceNotificationList: response.quarterlyMaintenanceNotificationList || [],
        });
      });
    }
  }

  public render() {
    return (
      <>
        {/* {this.props.render(this.props)} */}
        <Modal
          ariaHideApp={false}
          {...this.props}
          className="modal"
          overlayClassName="modal-overlay"
        >
          <h3 className="title blue">Update Existing Profit Center</h3>
          <span className="modal-text">Profit Center Information</span>
          <form onSubmit={this.handleSubmit}>
            <Input
              name="pcName"
              label="Name"
              value={this.state.name}
              error={null}
              type="text"
              onChange={this.handleChangeName}
            />
            <Input
              label="Code"
              value={this.state.code}
              error={null}
              name="pcCode"
              type="text"
              onChange={this.handleChangeCode}
            />
            <Input
              label="Office"
              value={this.state.office}
              error={null}
              name="pcOffice"
              type="text"
              onChange={this.handleChangeOffice}
            />
            <Input
              label="Contact"
              name="pcContact"
              value={this.state.contact}
              error={null}
              type="text"
              onChange={this.handleChangeContact}
            />
            <Input
              label="Email"
              value={this.state.email}
              error={null}
              name="pcEmail"
              type="text"
              onChange={this.handleChangeEmail}
            />
            <Input
              label="Phone"
              value={this.state.phone}
              error={null}
              name="pcPhone"
              type="text"
              onChange={this.handleChangePhone}
            />
            <MultiAddInput
              name="quarterlyMaintenanceEmailRecipients"
              label="Quarterly Maintenance Email Recipients"
              type="text"
              list={this.state.quarterlyMaintenanceNotificationList}
              value={''}
              addItem={(item: string, _overLimit: boolean, itemAlreadyExists: boolean) => {
                if (itemAlreadyExists) {
                  toastr.warning('', 'That email already exists.');
                } else if (!isEmailAddressValid(item)) {
                  toastr.warning('', 'Please enter a valid email address (e.g. username@domain.com)');
                } else {
                  this.setState({
                    quarterlyMaintenanceNotificationList: this.state.quarterlyMaintenanceNotificationList.concat(item),
                  });
                }
              }}
              removeItemCallback={(index: number) => {
                this.setState({
                  quarterlyMaintenanceNotificationList: this.state.quarterlyMaintenanceNotificationList.slice(0, index)
                    .concat(this.state.quarterlyMaintenanceNotificationList.slice(index + 1)),
                });
              }}
              readOnly={false}
              onBlur={() => { return; }}
              error={null}
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
              >
                Update Profit Center
              </button>
            </div>
          </form>
        </Modal>
      </>
    );
  }

  private handleChangeName = (event: React.ChangeEvent<HTMLInputElement>) => {
    event.stopPropagation();
    this.setState({
      name: event.target.value,
    });
  }

  private handleChangeCode = (event: React.ChangeEvent<HTMLInputElement>) => {
    this.setState({
      code: event.target.value,
    });
  }

  private handleChangeOffice = (event: React.ChangeEvent<HTMLInputElement>) => {
    this.setState({
      office: event.target.value,
    });
  }

  private handleChangeContact = (event: React.ChangeEvent<HTMLInputElement>) => {
    this.setState({
      contact: event.target.value,
    });
  }

  private handleChangeEmail = (event: React.ChangeEvent<HTMLInputElement>) => {
    this.setState({
      email: event.target.value,
    });
  }

  private handleChangePhone = (event: React.ChangeEvent<HTMLInputElement>) => {
    this.setState({
      phone: event.target.value,
    });
  }

  private handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    postJsonData(this.url, {
      id: this.props.profitCenterId,
      name: this.state.name,
      profitCenterCode: this.state.code,
      millimanOffice: this.state.office,
      contactName: this.state.contact,
      contactEmail: this.state.email,
      contactPhone: this.state.phone,
      quarterlyMaintenanceNotificationList: this.state.quarterlyMaintenanceNotificationList,
    }).then(() => {
      alert('Profit center updated.');
      this.props.onRequestClose(null);
    });
  }

  private cancel = (event: React.MouseEvent<HTMLButtonElement>) => {
    this.props.onRequestClose(event.nativeEvent);
  }
}
