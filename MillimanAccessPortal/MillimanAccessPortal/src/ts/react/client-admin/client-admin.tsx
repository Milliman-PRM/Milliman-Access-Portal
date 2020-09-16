import * as React from 'react';
import { connect } from 'react-redux';

import * as AccessActionCreators from './redux/action-creators';
import {
  AccessState, AccessStateCardAttributes, AccessStateEdit, AccessStateFilters, AccessStateFormData,
  AccessStateSelected, PendingDataState,
} from './redux/store';

import { Client, ClientWithEligibleUsers, ClientWithStats, Guid, ProfitCenter, User, UserRole } from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import { PanelSectionToolbar, PanelSectionToolbarButtons } from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import { CardExpansion } from '../shared-components/card/card-expansion';
import { ColumnSpinner } from '../shared-components/column-spinner';

import {
  CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { Filter } from '../shared-components/filter';
import { Toggle } from '../shared-components/form/toggle';
import { NavBar } from '../shared-components/navbar';
import { ClientDetail } from '../system-admin/interfaces';
import { activeUsers, clientEntities } from './redux/selectors';

type ClientEntity = ((ClientWithEligibleUsers | ClientWithStats) & { indent: 1 | 2 }) | 'divider' | 'new';
interface ClientAdminProps {
  pending: PendingDataState;
  clients: ClientEntity[];
  profitCenters: ProfitCenter[];
  details: ClientDetail;
  formData: AccessStateFormData;
  assignedUsers: User[];
  selected: AccessStateSelected;
  edit: AccessStateEdit;
  filters: AccessStateFilters;
  cardAttributes: AccessStateCardAttributes;
}

class ClientAdmin extends React.Component<ClientAdminProps & typeof AccessActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchProfitCenters({});
    this.props.fetchClients({});
  }

  public render() {
    return (
      <>
        <NavBar currentView={this.currentView} />
        {this.renderClientPanel()}
        {this.props.selected.client !== null ?
          <div>
            {this.props.pending.details ?
              <ColumnSpinner />
              : this.renderClientDetail()
            }
          </div> : null
        }
        {this.props.selected.client !== null ? this.renderClientUsers() : null}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, details, selected, filters, pending } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          if (entity === 'new') {
            return (
              <div
                key={key}
                className="card-container action-card-container"
                onClick={() => {
                  this.props.selectClient({ id: 'new' });
                  this.props.clearFormData({});
                }}
              >
                <div className="card-body-container card-100 action-card">
                  <h2 className="card-body-primary-text">
                    <svg className="action-card-icon">
                      <use href="#add" />
                    </svg>
                    <span>NEW CLIENT</span>
                  </h2>
                </div>
              </div>
            );
          }
          return (
            <Card
              key={key}
              selected={entity.id === selected.client}
              disabled={false}
              onSelect={() => {
                this.props.selectClient({ id: entity.id });
                this.props.fetchClientDetails({ clientId: entity.id });
              }}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
                <CardSectionStats>
                  <CardStat
                    name={'Eligible users'}
                    value={entity.userCount}
                    icon={'user'}
                  />
                  <CardStat
                    name={'Content items'}
                    value={entity.contentItemCount}
                    icon={'reports'}
                  />
                </CardSectionStats>
                <CardSectionButtons>
                  <CardButton
                    icon="delete"
                    color={'red'}
                    onClick={() => this.props.deleteClient(entity.id)}
                  />
                  <CardButton
                    icon="edit"
                    color={'blue'}
                    onClick={() => {
                      this.props.selectClient({ id: entity.id });
                      this.props.fetchClientDetails({ clientId: entity.id });
                      this.props.setEditStatus({ status: true });
                    }}
                  />
                  <CardButton
                    icon="add"
                    color={'green'}
                    onClick={null}
                  />
                </CardSectionButtons>
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
            <ActionIcon
              label="Add or create a new client"
              icon="add"
              action={() => {
                this.props.selectClient({ id: 'new' });
                this.props.clearFormData({});
                this.props.setEditStatus({ status: true });
              }}
            />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderClientDetail() {
    const { formData, details, profitCenters, selected, edit } = this.props;
    return (
      <>
        <div
          id="client-info"
          className="admin-panel-container"
          style={{ height: '100%' }}
        >
          <h3 className="admin-panel-header">Client Information</h3>
          <PanelSectionToolbar>
            <PanelSectionToolbarButtons>
              {!edit.status ?
                <ActionIcon
                  label="Edit client details"
                  icon="edit"
                  action={() => {
                    this.props.setEditStatus({ status: true });
                  }}
                /> :
                <ActionIcon
                  label="Cancel edit"
                  icon="cancel"
                  action={() => {
                    this.props.setEditStatus({ status: false });
                  }}
                />
              }
            </PanelSectionToolbarButtons>
          </PanelSectionToolbar>
          <div className="admin-panel-content-container" style={{ overflowY: 'scroll' }}>
            <form className={`admin-panel-content ${!edit.status ? 'form-disabled' : ''}`}>
              <div className="form-section-container">
                <div className="form-section">
                  <h4 className="form-section-title">Client Information</h4>
                  <div className="form-input-container">
                    <div
                      className="form-input form-input-text
                                 flex-item-for-phone-only-12-12 flex-item-for-tablet-up-9-12"
                    >
                      <label className="form-input-text-title">Client Name *</label>
                      <div>
                        <input
                          value={formData.name}
                          onChange={(event) => {
                            this.props.setClientName({ name: event.target.value });
                          }}
                        />
                        <span asp-validation-for="Name" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-3-12"
                    >
                      <label className="form-input-text-title">Client Code</label>
                      <div>
                        <input
                          placeholder={formData.clientCode}
                          onChange={(event) => {
                            this.props.setClientCode({ clientCode: event.currentTarget.value });
                          }}
                        />
                        <span className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title">Primary Client Contact</label>
                      <div>
                        <input
                          value={formData.contactName}
                          onClick={(event) => {
                            this.props.setClientContactName({ contactName: event.currentTarget.value });
                          }}
                        />
                        <span asp-validation-for="ContactName" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title" asp-for="ContactTitle">Client Contact Title</label>
                      <div>
                        <input
                          value={formData.contactTitle}
                          onChange={(event) => {
                            this.props.setClientContactTitle({ clientContactTitle: event.target.value });
                          }}
                        />
                        <span asp-validation-for="ContactTitle" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-9-12"
                    >
                      <label className="form-input-text-title">Client Contact Email</label>
                      <div>
                        <input
                          value={formData.contactEmail}
                          onChange={(event) => {
                            this.props.setClientContactEmail({ clientContactEmail: event.target.value });
                          }}
                        />
                        <span asp-validation-for="ContactEmail" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-3-12"
                    >
                      <label className="form-input-text-title">Client Contact Phone</label>
                      <div>
                        <input
                          value={formData.contactPhone}
                          onChange={(event) => {
                            this.props.setClientContactPhone({ clientContactPhone: event.target.value });
                          }}
                        />
                        <span asp-validation-for="ContactPhone" className="text-danger" />
                      </div>
                    </div>
                  </div>
                </div>
                <div className="form-section">
                  <h4 className="form-section-title">Security Information</h4>
                  <div className="form-input-container">
                    <div className="form-input form-input-selectized flex-item-12-12">
                      <label className="form-input-selectized-title">
                        Approved Email Domain List <span id="email-domain-limit-label" />
                      </label>
                      <div>
                        <input
                          className="selectize-custom-input"
                          placeholder={formData.acceptedEmailDomainList.join(', ')}
                        />
                      </div>
                    </div>
                    <div className="form-input form-input-selectized flex-item-12-12">
                      <label className="form-input-selectized-title">Approved Email Address Exception List</label>
                      <div>
                        <input
                          className="selectize-custom-input"
                          placeholder={formData.acceptedEmailAddressExceptionList.join(', ')}
                        />
                      </div>
                    </div>
                  </div>
                </div>
                <div className="form-section">
                  <h4 className="form-section-title">Billing Information</h4>
                  <div className="form-input-container">
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title" asp-for="ConsultantName">Primary Consultant</label>
                      <div>
                        <input
                          value={formData.consultantName}
                          onChange={(event) => {
                            this.props.setConsultantName({ consultantName: event.target.value });
                          }}
                        />
                        <span asp-validation-for="ConsultantName" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title" asp-for="ConsultantEmail">Consultant Email</label>
                      <div>
                        <input
                          value={formData.consultantEmail}
                          onChange={(event) => {
                            this.props.setConsultantEmail({ consultantEmail: event.target.value });
                          }}
                        />
                        <span asp-validation-for="ConsultantEmail" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title" asp-for="ConsultantOffice">Office</label>
                      <div>
                        <input
                          value={formData.office}
                          onChange={(event) => {
                            this.props.setOffice({ office: event.target.value });
                          }}
                        />
                        <span asp-validation-for="ConsultantOffice" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-dropdown flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-dropdown-title" asp-for="ProfitCenterId">Profit Center *</label>
                      <div>
                        <select
                          value={details.profitCenter.id}
                          onChange={(event) => this.props.setProfitCenter((event.target.value) ? {
                            profitCenterId: event.target.value,
                          } : null)}
                        >
                          <option value="">Make a Selection</option>
                          {profitCenters.map((profitCenter) => (
                            <option
                              key={profitCenter.id}
                              value={profitCenter.id}
                            >
                              {profitCenter.name}
                            </option>
                            ))
                          }
                        </select>
                        <span asp-validation-for="ProfitCenterId" className="text-danger" />
                      </div>
                    </div>
                  </div>
                </div>
                <div className="form-section">
                  <h4 className="form-section-title">New User Welcome Text</h4>
                  <div className="form-input-container">
                    <div className="form-input-container form-input form-input-nullable-textarea content-item-flex-1">
                      <div className="switch-container flex-item-for-phone-only-12-12 content-item-flex-none">
                        <Toggle
                          label={'Custom Welcome Text'}
                          checked={false}
                          onClick={null}
                        />
                      </div>
                      <div className="flex-item-for-phone-only-12-12 content-item-flex-1">
                        <textarea
                          id="NewUserWelcomeText"
                          name="NewUserWelcomeText"
                          onChange={() => {
                            return false;
                          }}
                        />
                      </div>
                    </div>
                  </div>
                </div>
                {edit.status ?
                  <div className="form-submission-section">
                    {selected.client === 'new' ?
                      <div className="button-container button-container-new">
                        <button type="button" className="button-reset link-button">Reset Form</button>
                        <button
                          type="button"
                          className="button-submit green-button"
                          onClick={() => this.props.saveNewClient(formData)}
                        >
                          Create Client
                        </button>
                      </div> :
                      <div className="button-container button-container-edit">
                        <button type="button" className="button-reset link-button">Discard Changes</button>
                        <button
                          type="button"
                          className="button-submit blue-button"
                          onClick={() => {
                            this.props.editClient(formData);
                            this.props.setEditStatus({ status: false });
                          }}
                        >
                          Save Changes
                        </button>
                      </div>
                    }
                  </div> : null
                }
              </div>
            </form>
          </div>
        </div>
      </>
    );
  }

  private renderClientUsers() {
    const { assignedUsers, selected, cardAttributes, filters } = this.props;
    return (
      <>
        <CardPanel
          entities={assignedUsers}
          renderEntity={(entity, key) => {
            const card = cardAttributes.user[entity.id];
            return (
              <Card
                key={key}
                selected={false}
                disabled={false}
                onSelect={() => {
                  this.props.selectUser({ id: entity.id });
                  (card && card.expanded) ?
                    this.props.setCollapsedUser({ id: entity.id }) :
                    this.props.setExpandedUser({ id: entity.id });
                }}
              >
                <CardSectionMain>
                  <CardText
                    text={
                      entity.firstName && entity.lastName ? `${entity.firstName} ${entity.lastName}` : entity.email}
                    subtext={entity.firstName && entity.lastName ? entity.email : ''}
                  />
                  <CardSectionButtons>
                    <CardButton
                      icon="remove-circle"
                      color={'red'}
                      onClick={null}
                    />
                  </CardSectionButtons>
                </CardSectionMain>
                <CardExpansion
                  label={'User roles'}
                  expanded={card && card.expanded}
                  setExpanded={(value) => value
                    ? this.props.setExpandedUser({ id: entity.id })
                    : this.props.setCollapsedUser({ id: entity.id })}
                >
                  <Toggle
                    label={'Client Admin'}
                    checked={entity.userRoles[0].isAssigned}
                    onClick={(event) => this.changeUserRole(event, entity.userRoles[0], selected.client, entity.id)}
                  />
                  <Toggle
                    label={'Content Access Admin'}
                    checked={entity.userRoles[1].isAssigned}
                    onClick={(event) => this.changeUserRole(event, entity.userRoles[1], selected.client, entity.id)}
                  />
                  <Toggle
                    label={'Content Publisher'}
                    checked={entity.userRoles[2].isAssigned}
                    onClick={(event) => this.changeUserRole(event, entity.userRoles[2], selected.client, entity.id)}
                  />
                  <Toggle
                    label={'Content User'}
                    checked={entity.userRoles[3].isAssigned}
                    onClick={(event) => this.changeUserRole(event, entity.userRoles[3], selected.client, entity.id)}
                  />
                  <Toggle
                    label={'File Drop Admin'}
                    checked={entity.userRoles[4].isAssigned}
                    onClick={(event) => this.changeUserRole(event, entity.userRoles[4], selected.client, entity.id)}
                  />
                  <Toggle
                    label={'File Drop User'}
                    checked={entity.userRoles[5].isAssigned}
                    onClick={(event) => this.changeUserRole(event, entity.userRoles[5], selected.client, entity.id)}
                  />
                </CardExpansion>
              </Card>
            );
          }}
        >
          <h3 className="admin-panel-header">Client Users</h3>
          <PanelSectionToolbar>
            <Filter
              placeholderText={'Filter users...'}
              setFilterText={(text) => this.props.setFilterTextUser({ text })}
              filterText={filters.user.text}
            />
            <PanelSectionToolbarButtons>
              <PanelSectionToolbarButtons>
                <ActionIcon
                  label="Expand all user cards"
                  icon="expand-cards"
                  action={() => false}
                />
                <ActionIcon
                  label="Add or create a new client"
                  icon="add"
                  action={() => {
                    this.props.selectClient({ id: 'new' });
                    this.props.clearFormData({});
                    this.props.setEditStatus({ status: true });
                  }}
                />
              </PanelSectionToolbarButtons>
            </PanelSectionToolbarButtons>
          </PanelSectionToolbar>
        </CardPanel>
      </>
    );
  }

  private changeUserRole(event: React.MouseEvent, entityRole: UserRole, client: Guid, user: Guid) {
    event.stopPropagation();
    this.props.setUserRoleInClient({
      clientId: client,
      isAssigned: !entityRole.isAssigned,
      roleEnum: entityRole.roleEnum,
      userId: user,
    });
  }
}

function mapStateToProps(state: AccessState): ClientAdminProps {
  const { data, selected, edit, cardAttributes, formData, filters, pending } = state;

  return {
    clients: clientEntities(state),
    profitCenters: data.profitCenters,
    details: data.details,
    cardAttributes,
    assignedUsers: activeUsers(state),
    formData,
    selected,
    edit,
    filters,
    pending,
  };
}

export const ConnectedClientAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ClientAdmin);
