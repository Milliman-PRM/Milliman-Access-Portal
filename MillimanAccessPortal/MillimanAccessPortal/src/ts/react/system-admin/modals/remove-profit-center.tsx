import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { postData } from '../../../shared';
import { Client } from '../../models';
import { Guid } from '../../shared-components/interfaces';
import { Dict } from '../../shared-components/redux/store';

export interface RemoveProfitCenterModalsProps extends Modal.Props {
  profitCenterId: Guid;
  profitCenterName: string;
}

interface RemoveProfitCenterModalsState {
  confirmDelete: boolean;
  profitCenterHasProblematicSubClients: boolean;
  mismatchingClientSubClientRelationships: Dict<Client[]>;
}

export class RemoveProfitCenterModals
  extends React.Component<RemoveProfitCenterModalsProps, RemoveProfitCenterModalsState> {

  private validateUrl: string = 'SystemAdmin/ValidateProfitCenterCanBeDeleted';
  private deleteUrl: string = 'SystemAdmin/DeleteProfitCenter';

  public constructor(props: RemoveProfitCenterModalsProps) {
    super(props);

    this.state = {
      confirmDelete: false,
      profitCenterHasProblematicSubClients: false,
      mismatchingClientSubClientRelationships: null,
    };

    this.handleInitialModalSubmit = this.handleInitialModalSubmit.bind(this);
    this.handleConfirmationModalSubmit = this.handleConfirmationModalSubmit.bind(this);
    this.resetState = this.resetState.bind(this);
  }

  public render() {
    return (
      <>
        <Modal
          ariaHideApp={false}
          {...this.props}
          isOpen={this.props.isOpen && !this.state.profitCenterHasProblematicSubClients && !this.state.confirmDelete}
          className="modal"
          overlayClassName="modal-overlay"
          onRequestClose={() => {
            this.props.onRequestClose(null);
            this.resetState();
          }}
        >
          <h3 className="title red">Delete Profit Center</h3>
          <div className="modal-text">
            Delete <strong>{this.props.profitCenterName}</strong>? This Profit Center may contain Clients,&nbsp;
            Content, File Drops, and Users.
          </div>
          <div className="modal-text">
            This action <strong><u>cannot</u></strong> be undone.
          </div>
          <form onSubmit={this.handleInitialModalSubmit}>
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => {
                  this.props.onRequestClose(null);
                  this.resetState();
                }}
              >
                Cancel
              </button>
              <button
                className="red-button"
                type="submit"
              >
                Delete
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          ariaHideApp={false}
          isOpen={this.state.profitCenterHasProblematicSubClients}
          className="modal"
          overlayClassName="modal-overlay"
          onRequestClose={() => {
            this.props.onRequestClose(null);
            this.resetState();
          }}
        >
          <h3 className="title red">Unable to Delete Profit Center</h3>
          <span className="modal-text">
            There exist Sub-Clients with different Profit Centers listed from the parent Client.
          </span>
          <span className="modal-text">
            In order to delete the Profit Center, please change the Sub-Client's Profit Center listed&nbsp;
            below or remove the Sub-Client completely.
          </span>
          <span className="modal-text">
            <ul>
              {this.state.mismatchingClientSubClientRelationships &&
                Object.keys(this.state.mismatchingClientSubClientRelationships).map((parentClient, pIndex) =>
                <div key={pIndex}>
                  <strong>{parentClient}</strong>
                  <ul>
                    {this.state.mismatchingClientSubClientRelationships[parentClient].map((subClient, sIndex) =>
                      <li key={sIndex}>{subClient.name}</li>,
                    )}
                  </ul>
                </div>,
              )}
            </ul>
          </span>
          <div className="button-container no-padding-top">
            <button
              className="link-button"
              type="button"
              onClick={() => {
                this.props.onRequestClose(null);
                this.resetState();
              }}
            >
              Ok
            </button>
          </div>
        </Modal>
        <Modal
          ariaHideApp={false}
          isOpen={this.state.confirmDelete}
          className="modal"
          overlayClassName="modal-overlay"
          onRequestClose={() => {
            this.props.onRequestClose(null);
            this.resetState();
          }}
        >
          <h3 className="title red">Delete Profit Center</h3>
          <span className="modal-text">
            Please confirm the deletion of <strong>{this.props.profitCenterName}</strong>.
          </span>
          <form onSubmit={this.handleConfirmationModalSubmit}>
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => {
                  this.props.onRequestClose(null);
                  this.resetState();
                }}
              >
                Cancel
              </button>
              <button
                className="red-button"
                type="submit"
              >
                Delete
              </button>
            </div>
          </form>
        </Modal>
      </>
    );
  }

  private handleInitialModalSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    postData(this.validateUrl, { profitCenterId: this.props.profitCenterId })
      .then((response) => {
        if (Object.keys(response.mismatchingClientSubClientRelationships).length > 0) {
          this.setState({
            profitCenterHasProblematicSubClients: true,
            mismatchingClientSubClientRelationships: response.mismatchingClientSubClientRelationships,
          });
        } else {
          this.setState({
            confirmDelete: true,
          });
        }
      },
    );
  }

  private handleConfirmationModalSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    postData(this.deleteUrl, { profitCenterId: this.props.profitCenterId }).then(() => {
      alert('Profit Center Deleted.');
      this.resetState();
      this.props.onRequestClose(null);
    });
  }

  private resetState() {
    this.setState({
      confirmDelete: false,
      profitCenterHasProblematicSubClients: false,
      mismatchingClientSubClientRelationships: null,
    });
  }
}
