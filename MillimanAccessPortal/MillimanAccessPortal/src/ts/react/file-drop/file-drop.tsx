import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import * as FileDropActionCreator from './redux/action-creators';
import * as Selector from './redux/selectors';
import * as State from './redux/store';

import { Client, FileDropClientWithStats, FileDropWithStats } from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { ButtonSpinner } from '../shared-components/button-spinner';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import { PanelSectionToolbar, PanelSectionToolbarButtons } from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import {
  CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { Filter } from '../shared-components/filter';
import { Input, TextAreaInput } from '../shared-components/form/input';
import { NavBar } from '../shared-components/navbar';

type ClientEntity = (FileDropClientWithStats & { indent: 1 | 2 }) | 'divider';

interface FileDropProps {
  clients: ClientEntity[];
  fileDrops: FileDropWithStats[];
  selected: State.FileDropSelectedState;
  cardAttributes: State.FileDropCardAttributesState;
  pending: State.FileDropPendingState;
  filters: State.FileDropFilterState;
  modals: State.FileDropModals;
  activeSelectedClient: Client;
}

class FileDrop extends React.Component<FileDropProps & typeof FileDropActionCreator> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.scheduleStatusRefresh({ delay: 0 });
    this.props.scheduleSessionCheck({ delay: 0 });

    // TODO: Implement these actions properly
    // this.props.fetchGlobalData({});
    this.props.fetchClients({});
  }

  public render() {
    const { selected, modals, pending } = this.props;
    return (
      <>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-center"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
        <NavBar currentView={this.currentView} />
        {this.renderClientPanel()}
        {selected.client && this.renderFileDropPanel()}
        <Modal
          isOpen={modals.createFileDrop.isOpen}
          onRequestClose={() => this.props.closeCreateFileDropModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title blue">Add File Drop</h3>
          <form>
            <Input
              autoFocus={true}
              error={pending.createFileDrop.errors.fileDropName}
              label="File Drop Name"
              name="File Drop Name"
              onBlur={() => false}
              onChange={() => false}
              placeholderText="New File Drop Name"
              type="text"
              value={pending.createFileDrop.fileDropName}
            />
            <TextAreaInput
              error={pending.createFileDrop.errors.fileDropDescription}
              label="File Drop Description"
              name="File Drop Description"
              onBlur={() => false}
              onChange={() => false}
              placeholderText="File Drop Description (Optional)"
              value={pending.createFileDrop.fileDropDescription}
            />
            <div className="button-container">
              <button className="link-button" type="button" onClick={() => this.props.closeCreateFileDropModal({})}>
                Cancel
              </button>
              <button
                className={`blue-button${pending.createFileDrop.fileDropName ? '' : ' disabled'}`}
                type="submit"
              >
                Add
                  {this.props.pending.async.createFileDrop
                  ? <ButtonSpinner version="circle" />
                  : null
                }
              </button>
            </div>
          </form>
        </Modal>
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending, cardAttributes } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.async.clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          const card = cardAttributes.client[entity.id];
          return (
            <Card
              key={key}
              selected={selected.client === entity.id}
              disabled={card.disabled}
              onSelect={() => {
                // TODO: Update this section once all of the necessary actions and data are available
                // if (this.props.formChangesPending || this.props.uploadChangesPending) {
                //   this.props.openModifiedFormModal({
                //     afterFormModal:
                //     {
                //       entityToSelect: entity.id,
                //       entityType: 'Select Client',
                //     },
                //   });
                // } else {
                //   if (selected.client !== entity.id) {
                //     this.props.fetchItems({ clientId: entity.id });
                //   }
                //   this.props.selectClient({ id: entity.id });
                // }
                this.props.selectClient({ id: entity.id });
              }}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
                <CardSectionStats>
                  <CardStat
                    name={'Reports'}
                    value={entity.contentItemCount}
                    icon={'reports'}
                  />
                  <CardStat
                    name={'Users'}
                    value={entity.userCount}
                    icon={'user'}
                  />
                </CardSectionStats>
              </CardSectionMain>
            </Card>
          );
        }}
      >
        <h3 className="admin-panel-header">Clients</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter clients...'}
            setFilterText={(text) => this.props.setFilterTextClient({ text })}
            filterText={filters.client.text}
          />
          <PanelSectionToolbarButtons>
            <div id="icons" />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderFileDropPanel() {
    const { activeSelectedClient, clients, selected, filters, pending, cardAttributes, fileDrops } = this.props;
    const createNewFileDropIcon = (
      <ActionIcon
        label="New File Drop"
        icon="add"
        action={() => {
          if (false) {
            // TODO: implement any modals necessary before opening this modal
          } else {
            this.props.openCreateFileDropModal({ clientId: selected.client });
          }
        }}
      />
    );
    const cardButtons = (_entityId: string, canManageFileDrops: boolean) => {
      return canManageFileDrops
        ? (
          <>
            <CardButton
              color={'blue'}
              tooltip={'Edit File Drop'}
              onClick={() => {
                if (false) {
                  // TODO: Implement any necessary modals before performing action
                } else {
                  // TODO: Implement this action
                }
              }}
              icon={'edit'}
            />
            <CardButton
              color={'red'}
              tooltip={'Delete File Drop'}
              onClick={() => {
                if (false) {
                  // TODO: Implement any necessary modals before performing action
                } else {
                  // TODO: Implement this action
                }
              }}
              icon={'delete'}
            />
          </>
        ) : (
          null
        );
    };

    return Selector.activeSelectedClient && (
      <>
        <CardPanel
          entities={fileDrops}
          loading={pending.async.fileDrops}
          renderEntity={(entity, key) => {
            return (
              <Card
                key={key}
                selected={selected.fileDrop === entity.id}
                onSelect={() => {
                  if (false) {
                    // TODO: Implement necessary modals
                  } else {
                    if (selected.fileDrop !== entity.id) {
                      // this.props.fetchFileDropDetail({ FileDropId: entity.id });
                    }
                    // this.props.selectFileDrop({ id: entity.id });
                  }
                }}
                // suspended={entity.isSuspended}
              >
                <CardSectionMain>
                  <CardText
                    text={entity.name}
                    // TODO: Implement this when isSuspended is available
                    // textSuffix={entity.isSuspended ? '[Suspended]' : ''}
                    subtext={entity.description}
                  />
                  <CardSectionStats
                    // TODO: Make this dynamic when canManage is available
                  >
                    <CardStat
                      name={'Authorized Users'}
                      value={entity.userCount}
                      icon={'user'}
                    />
                  </CardSectionStats>
                  <CardSectionButtons>
                    // TODO: Implement the second value properly
                    {cardButtons(entity.id, true)}
                  </CardSectionButtons>
                </CardSectionMain>
              </Card>
            );
          }}
          renderNewEntityButton={() => (
            <div
              className="card-container action-card-container"
              onClick={() => {
                if (false) {
                  // TODO Implement any necessary modals
                } else {
                  this.props.openCreateFileDropModal({ clientId: selected.client });
                }
              }}
            >
              <div className="admin-panel-content">
                <div
                  className={
                    `
                    card-body-container card-100 action-card
                    ${this.props.selected.fileDrop === 'NEW FILE DROP' ? 'selected' : ''}
                  `
                  }
                >
                  <h2 className="card-body-primary-text">
                    <svg className="action-card-icon">
                      <use href="#add" />
                    </svg>
                    <span>CREATE FILE DROP</span>
                  </h2>
                </div>
              </div>
            </div>
          )}
        >
          <h3 className="admin-panel-header">File Drops</h3>
          <PanelSectionToolbar>
            <Filter
              placeholderText={'Filter content items...'}
              setFilterText={(text) => this.props.setFilterTextFileDrop({ text })}
              filterText={filters.fileDrop.text}
            />
            <PanelSectionToolbarButtons>
              {createNewFileDropIcon}
            </PanelSectionToolbarButtons>
          </PanelSectionToolbar>
        </CardPanel>
      </>
    );
  }

}

function mapStateToProps(state: State.FileDropState): FileDropProps {
  const { data, selected, cardAttributes, pending, filters, modals } = state;

  return {
    clients: Selector.clientEntities(state),
    fileDrops: Selector.fileDropEntities(state),
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
    activeSelectedClient: Selector.activeSelectedClient(state),
  };
}

export const ConnectedFileDrop = connect(
  mapStateToProps,
  FileDropActionCreator,
)(FileDrop);
