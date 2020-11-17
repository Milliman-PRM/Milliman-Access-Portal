import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { postData } from '../../../shared';
import { DropDown } from '../../shared-components/form/select';
import { Guid, HitrustReasonEnum, RoleEnum } from '../../shared-components/interfaces';

export interface ChangeSystemAdminStatusModalProps extends Modal.Props {
  userId: Guid;
  value: boolean;
  callback: (response: boolean) => void;
}

interface ChangeSystemAdminStatusModalState {
  reason: HitrustReasonEnum;
}

export class ChangeSystemAdminStatusModal
  extends React.Component<ChangeSystemAdminStatusModalProps, ChangeSystemAdminStatusModalState> {

  private url: string = 'SystemAdmin/SystemRole';
  private readonly enableSystemAdminHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.NewEmployeeHire, selectionLabel: 'New employee hire' },
  ];
  private readonly disableSystemAdminHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.EmployeeTermination, selectionLabel: 'Employee termination' },
  ];

  public constructor(props: ChangeSystemAdminStatusModalProps) {
    super(props);

    this.state = {
      reason: null,
    };

    this.handleHitrustReasonChange = this.handleHitrustReasonChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  public render() {
    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
        className="modal"
        overlayClassName="modal-overlay"
        onRequestClose={() => {
          this.props.onRequestClose(null);
          this.setState({ reason: null });
        }}
      >
        <h3 className={`title ${this.props.value ? 'blue' : 'red'}`}>
          {this.props.value ?
            <>Add System Admin</> :
            <>Remove System Admin</>
          }
        </h3>
        <span className="modal-text">Please select a valid reason to add this system admin.</span>
        <form onSubmit={this.handleSubmit}>
          <DropDown
            name="reason"
            label="Reason"
            placeholderText="Choose an option..."
            values={this.props.value ? this.enableSystemAdminHitrustReasons : this.disableSystemAdminHitrustReasons}
            value={this.state.reason}
            onChange={this.handleHitrustReasonChange}
            error={null}
            autoFocus={true}
          />
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => {
                this.props.onRequestClose(null);
                this.setState({ reason: null });
              }}
            >
              Cancel
            </button>
            <button
              className={`${this.props.value ? 'blue-button' : 'red-button'}`}
              type="submit"
              disabled={!this.state.reason}
            >
              {this.props.value ?
                <>Enable System Admin</> :
                <>Disable System Admin</>
              }
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleHitrustReasonChange(event: React.FormEvent<HTMLSelectElement>) {
    this.setState({
      reason: parseInt(event.currentTarget.value, 10),
    });
  }

  private handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    postData(this.url, {
      userId: this.props.userId,
      role: RoleEnum.Admin,
      value: this.props.value,
      reason: this.state.reason,
    }).then((response: boolean) => {
      this.props.callback(response);
      this.props.onRequestClose(null);
    });
  }
}
