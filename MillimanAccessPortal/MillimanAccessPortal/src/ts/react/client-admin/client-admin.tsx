import * as React from 'react';
import { connect } from 'react-redux';

import * as AccessActionCreators from './redux/action-creators';
import { AccessState, AccessStateCardAttributes, AccessStateFilters, AccessStateSelected } from './redux/store';

import { ClientWithEligibleUsers, ClientWithStats, Guid, User, UserRole } from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import { PanelSectionToolbar, PanelSectionToolbarButtons } from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import { CardExpansion } from '../shared-components/card/card-expansion';
import {
  CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { Filter } from '../shared-components/filter';
import { Toggle } from '../shared-components/form/toggle';
import { RoleEnum } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import { ClientDetail } from '../system-admin/interfaces';
import { activeUsers, clientEntities } from './redux/selectors';

type ClientEntity = ((ClientWithEligibleUsers | ClientWithStats) & { indent: 1 | 2 }) | 'divider';
interface ClientAdminProps {
  clients: ClientEntity[];
  details: ClientDetail;
  assignedUsers: User[];
  selected: AccessStateSelected;
  filters: AccessStateFilters;
  cardAttributes: AccessStateCardAttributes;
}

class ClientAdmin extends React.Component<ClientAdminProps & typeof AccessActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchClients({});
  }

  public render() {
    return (
      <>
        <NavBar currentView={this.currentView} />
        {this.renderClientPanel()}
        {this.props.selected.client !== null ? this.renderClientDetail() : null}
        {this.props.selected.client !== null ? this.renderClientUsers() : null}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters } = this.props;
    return (
      <CardPanel
        entities={clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          return (
            <Card
              key={key}
              selected={entity.id === selected.client}
              disabled={false}
              onSelect={() => {
                this.props.fetchClientDetails({ clientId: entity.id });
                this.props.selectClient({ id: entity.id });
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
                    onClick={null}
                  />
                  <CardButton
                    icon="edit"
                    color={'blue'}
                    onClick={null}
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
              action={() => false}
            />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderClientDetail() {
    const { details } = this.props;
    return (
      <>
        <div
          id="client-info"
          className="admin-panel-container flex-item-12-12
                     flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-6-12"
        >
          <h3 className="admin-panel-header">Client Information</h3>
          <div className="admin-panel-toolbar">
            <div className="admin-panel-action-icons-container" />
          </div>
          <div className="admin-panel-content-container">
            <form className="admin-panel-content form-disabled">
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
                        <input placeholder={details.clientName} disabled={true} />
                        <span asp-validation-for="Name" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-3-12"
                    >
                      <label className="form-input-text-title">Client Code</label>
                      <div>
                        <input placeholder={details.clientCode} disabled={true} />
                        <span asp-validation-for="ClientCode" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title">Primary Client Contact</label>
                      <div>
                        <input placeholder={details.clientContactName} disabled={true} />
                        <span asp-validation-for="ContactName" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title" asp-for="ContactTitle">Client Contact Title</label>
                      <div>
                        <input asp-for="ContactTitle" disabled={true} />
                        <span asp-validation-for="ContactTitle" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-9-12"
                    >
                      <label className="form-input-text-title">Client Contact Email</label>
                      <div>
                        <input placeholder={details.clientContactEmail} disabled={true} />
                        <span asp-validation-for="ContactEmail" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-3-12"
                    >
                      <label className="form-input-text-title">Client Contact Phone</label>
                      <div>
                        <input placeholder={details.clientContactPhone} disabled={true}/>
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
                          placeholder={details.acceptedEmailDomainList.join(', ')}
                          disabled={true}
                        />
                      </div>
                    </div>
                    <div className="form-input form-input-selectized flex-item-12-12">
                      <label className="form-input-selectized-title">Approved Email Address Exception List</label>
                      <div>
                        <input
                          className="selectize-custom-input"
                          placeholder={details.acceptedEmailAddressExceptionList.join(', ')}
                          disabled={true}
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
                        <input placeholder={details.consultantName} disabled={true} />
                        <span asp-validation-for="ConsultantName" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title" asp-for="ConsultantEmail">Consultant Email</label>
                      <div>
                        <input asp-for={details.consultantEmail} disabled={true} />
                        <span asp-validation-for="ConsultantEmail" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-text-title" asp-for="ConsultantOffice">Office</label>
                      <div>
                        <input placeholder={details.office} disabled={true} />
                        <span asp-validation-for="ConsultantOffice" className="text-danger" />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-dropdown flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <label className="form-input-dropdown-title" asp-for="ProfitCenterId">Profit Center *</label>
                      <div>
                        <select asp-for="ProfitCenterId" disabled={true}>
                          <option value="">Make a Selection</option>
                          <option>{details.profitCenter}</option>
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
                        <textarea id="NewUserWelcomeText" name="NewUserWelcomeText" />
                      </div>
                    </div>
                  </div>
                </div>
                <div className="form-submission-section">
                  <div className="button-container button-container-new">
                    <button type="button" className="button-reset link-button">Reset Form</button>
                    <button type="button" className="button-submit green-button">Create Client</button>
                  </div>
                  <div className="button-container button-container-edit">
                    <button type="button" className="button-reset link-button">Discard Changes</button>
                    <button type="button" className="button-submit blue-button">Save Changes</button>
                  </div>
                </div>
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
                {Object.keys(entity.userRoles).length > 0 ?
                  <CardExpansion
                    label={'User roles'}
                    expanded={card && card.expanded}
                    setExpanded={(value) => value
                      ? this.props.setExpandedUser({ id: entity.id })
                      : this.props.setCollapsedUser({ id: entity.id })}
                  >
                    <Toggle
                      label={entity.userRoles[RoleEnum.Admin].roleDisplayValue}
                      checked={entity.userRoles[RoleEnum.Admin].isAssigned}
                      onClick={(event) =>
                        this.changeUserRole(event, entity.userRoles[RoleEnum.Admin], selected.client, entity.id)
                      }
                    />
                    <Toggle
                      label={entity.userRoles[RoleEnum.ContentAccessAdmin].roleDisplayValue}
                      checked={entity.userRoles[RoleEnum.ContentAccessAdmin].isAssigned}
                      onClick={(event) =>
                        this.changeUserRole(event, entity.userRoles[RoleEnum.ContentAccessAdmin],
                                            selected.client, entity.id)
                      }
                    />
                    <Toggle
                      label={entity.userRoles[RoleEnum.ContentPublisher].roleDisplayValue}
                      checked={entity.userRoles[RoleEnum.ContentPublisher].isAssigned}
                      onClick={(event) =>
                        this.changeUserRole(event, entity.userRoles[RoleEnum.ContentPublisher],
                                            selected.client, entity.id)
                      }
                    />
                    <Toggle
                      label={entity.userRoles[RoleEnum.ContentUser].roleDisplayValue}
                      checked={entity.userRoles[RoleEnum.ContentUser].isAssigned}
                      onClick={(event) =>
                        this.changeUserRole(event, entity.userRoles[RoleEnum.ContentUser], selected.client, entity.id)
                      }
                    />
                    <Toggle
                      label={entity.userRoles[RoleEnum.FileDropAdmin].roleDisplayValue}
                      checked={entity.userRoles[RoleEnum.FileDropAdmin].isAssigned}
                      onClick={(event) =>
                        this.changeUserRole(event, entity.userRoles[RoleEnum.FileDropAdmin],
                                            selected.client, entity.id)
                      }
                    />
                    <Toggle
                      label={entity.userRoles[RoleEnum.FileDropUser].roleDisplayValue}
                      checked={entity.userRoles[RoleEnum.FileDropUser].isAssigned}
                      onClick={(event) =>
                        this.changeUserRole(event, entity.userRoles[RoleEnum.FileDropUser], selected.client, entity.id)
                      }
                    />
                  </CardExpansion> : null}
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
                  action={() => false}
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
  const { data, selected, cardAttributes, filters } = state;

  return {
    clients: clientEntities(state),
    details: data.details,
    cardAttributes,
    assignedUsers: activeUsers(state),
    selected,
    filters,
  };
}

export const ConnectedClientAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ClientAdmin);
