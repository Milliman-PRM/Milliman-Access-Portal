import * as React from 'react';
import { connect } from 'react-redux';

import * as AccessActionCreators from './redux/action-creators';
import {
  activeUsers, clientEntities, isEmailAddressValid, isFormModified,
  isFormValid, isStringNotEmpty,
} from './redux/selectors';
import {
  AccessState, AccessStateCardAttributes, AccessStateEdit, AccessStateFilters, AccessStateFormData,
  AccessStateSelected, AccessStateValid, PendingDataState,
} from './redux/store';

import { ClientWithEligibleUsers, ClientWithStats, Guid, ProfitCenter, User, UserRole } from '../models';
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
import { Input } from '../shared-components/form/input';
import { DropDown } from '../shared-components/form/select';
import { Toggle } from '../shared-components/form/toggle';
import { RoleEnum } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import { ClientDetail } from '../system-admin/interfaces';

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
  valid: AccessStateValid;
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
          <div style={{ width: '100%' }}>
            {this.props.pending.details ?
              <ColumnSpinner />
              : this.renderClientDetail()
            }
          </div> : null
        }
        {this.props.selected.client !== null && !this.props.pending.details  ? this.renderClientUsers() : null}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending } = this.props;
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
                  this.props.resetClientDetails({});
                  this.props.selectClient({ id: 'new' });
                  this.props.clearFormData({});
                  this.props.setEditStatus({ disabled: false });
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
                this.props.setEditStatus({ disabled: true });
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
                    icon={'delete'}
                    color={'red'}
                    onClick={() => {
                      this.props.deleteClient(entity.id);
                      this.props.selectClient({ id: null });
                    }}
                  />
                  <CardButton
                    icon={'edit'}
                    color={'blue'}
                    onClick={() => {
                      if (selected.client !== entity.id) {
                        this.props.selectClient({ id: entity.id });
                      }
                      this.props.fetchClientDetails({ clientId: entity.id });
                      this.props.setEditStatus({ disabled: false });
                    }}
                  />
                  <CardButton
                    icon={'add'}
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
                this.props.setEditStatus({ disabled: false });
              }}
            />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderClientDetail() {
    const { formData, profitCenters, details, selected, edit, valid } = this.props;
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
              {edit.disabled ?
                <ActionIcon
                  label="Edit client details"
                  icon="edit"
                  action={() => {
                    this.props.resetValidity({});
                    this.props.setEditStatus({ disabled: false });
                  }}
                /> :
                <ActionIcon
                  label="Cancel edit"
                  icon="cancel"
                  action={() => {
                    this.props.setEditStatus({ disabled: true });
                    this.props.resetFormData({ details });
                    this.props.resetValidity({});
                  }}
                />
              }
            </PanelSectionToolbarButtons>
          </PanelSectionToolbar>
          <div className="admin-panel-content-container" style={{ overflowY: 'scroll' }}>
            <form className={`admin-panel-content ${edit.disabled ? 'form-disabled' : ''}`}>
              <div className="form-section-container">
                <div className="form-section">
                  <h4 className="form-section-title">Client Information</h4>
                  <div className="form-input-container">
                    <div
                      className="form-input form-input-text
                                 flex-item-for-phone-only-12-12 flex-item-for-tablet-up-9-12"
                    >
                      <div>
                        <Input
                          name="clientName"
                          label="Client Name *"
                          type="text"
                          value={formData.name}
                          onChange={(event) => {
                            this.props.setFormFieldValue({ field: 'name', value: event.currentTarget.value });
                          }}
                          readOnly={edit.disabled}
                          onBlur={(event: React.FormEvent<HTMLInputElement>) => {
                            this.props.setValidityForField({
                              field: 'name',
                              valid: isStringNotEmpty(event.currentTarget.value),
                            });
                          }}
                          error={valid.name ? '' : 'Client Name is a required field.'}
                        />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-3-12"
                    >
                      <div>
                        <Input
                          name="clientCode"
                          label="Client Code"
                          type="text"
                          value={formData.clientCode}
                          onChange={(event) => {
                            this.props.setFormFieldValue({ field: 'clientCode', value: event.currentTarget.value });
                          }}
                          readOnly={edit.disabled}
                          onBlur={() => { return; }}
                          error={null}
                        />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <div>
                        <Input
                          name="contactName"
                          label="Client Contact Name"
                          type="text"
                          value={formData.contactName}
                          onChange={(event) => {
                            this.props.setFormFieldValue({ field: 'contactName', value: event.currentTarget.value });
                          }}
                          readOnly={edit.disabled}
                          onBlur={() => { return; }}
                          error={null}
                        />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <div>
                        <Input
                          name="clientContactTitle"
                          label="Client Contact Title"
                          type="text"
                          value={formData.contactTitle}
                          onChange={(event) => {
                            this.props.setFormFieldValue({ field: 'contactTitle', value: event.currentTarget.value });
                          }}
                          readOnly={edit.disabled}
                          onBlur={() => { return; }}
                          error={null}
                        />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-9-12"
                    >
                      <div>
                        <Input
                          name="contactEmail"
                          label="Client Contact Email"
                          type="text"
                          value={formData.contactEmail ? formData.contactEmail : ''}
                          onChange={(event) => {
                            this.props.setFormFieldValue({ field: 'contactEmail', value: event.currentTarget.value });
                          }}
                          readOnly={edit.disabled}
                          onBlur={(event: React.FormEvent<HTMLInputElement>) => {
                            this.props.setValidityForField({
                              field: 'contactEmail',
                              valid: isEmailAddressValid(event.currentTarget.value),
                            });
                          }}
                          error={valid.contactEmail ? null : 'Client contact email is not valid.'}
                        />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-3-12"
                    >
                      <div>
                        <Input
                          name="contactPhone"
                          label="Client Contact Phone"
                          type="text"
                          value={formData.contactPhone ? formData.contactPhone : ''}
                          onChange={(event) => {
                            this.props.setFormFieldValue({ field: 'contactPhone', value: event.currentTarget.value });
                          }}
                          readOnly={edit.disabled}
                          onBlur={() => { return; }}
                          error={null}
                        />
                      </div>
                    </div>
                  </div>
                </div>
                <div className="form-section">
                  <h4 className="form-section-title">Security Information</h4>
                  <div className="form-input-container">
                    <div className="form-input form-input-selectized flex-item-12-12">
                      <div>
                        <Input
                          name="approvedEmailDomainList"
                          label="Approved Email Domain List"
                          type="text"
                          value={formData.acceptedEmailDomainList.join(', ')}
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'acceptedEmailDomainList',
                              value: event.currentTarget.value.split(', '),
                            });
                          }}
                          readOnly={edit.disabled}
                          onBlur={() => { return; }}
                          error={null}
                        />
                      </div>
                    </div>
                    <div className="form-input form-input-selectized flex-item-12-12">
                      <div>
                        <Input
                          name="approvedEmailAddressExceptionList"
                          label="Approved Email Address Exception List"
                          type="text"
                          value={formData.acceptedEmailAddressExceptionList}
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'approvedEmailAddressExceptionList',
                              value: event.currentTarget.value.split(', '),
                            });
                          }}
                          readOnly={edit.disabled}
                          onBlur={() => { return; }}
                          error={null}
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
                      <div>
                        <Input
                          name="consultantName"
                          label="Primary Consultant"
                          type="text"
                          value={formData.consultantName}
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'consultantName',
                              value: event.currentTarget.value,
                            });
                          }}
                          readOnly={edit.disabled}
                          onBlur={() => { return; }}
                          error={null}
                        />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <div>
                        <Input
                          name="consultantEmail"
                          label="Consultant Email"
                          type="text"
                          value={formData.consultantEmail ? formData.consultantEmail : ''}
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'consultantEmail',
                              value: event.currentTarget.value,
                            });
                          }}
                          readOnly={edit.disabled}
                          onBlur={(event: React.FormEvent<HTMLInputElement>) => {
                            this.props.setValidityForField({
                              field: 'consultantEmail',
                              valid: isEmailAddressValid(event.currentTarget.value),
                            });
                          }}
                          error={valid.consultantEmail ? null : 'Consultant email address is invalid.'}
                        />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-text flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <div>
                        <Input
                          name="office"
                          label="Office"
                          type="text"
                          value={formData.consultantOffice}
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'consultantOffice',
                              value: event.currentTarget.value,
                            });
                          }}
                          readOnly={edit.disabled}
                          onBlur={() => { return; }}
                          error={null}
                        />
                      </div>
                    </div>
                    <div
                      className="form-input form-input-dropdown flex-item-for-phone-only-12-12
                                 flex-item-for-tablet-up-6-12"
                    >
                      <div>
                        <DropDown
                          name="profitCenterId"
                          label="Profit Center *"
                          placeholderText="Select a profit center..."
                          values={profitCenters.map((pc) => {
                            return {
                              selectionValue: pc.id,
                              selectionLabel: pc.name,
                            };
                          })}
                          value={formData.profitCenterId}
                          onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                            const profitCenterId = target.value ? target.value : null;
                            this.props.setFormFieldValue({ field: 'profitCenterId', value: profitCenterId });
                            this.props.setValidityForField({
                              field: 'profitCenterId',
                              valid: isStringNotEmpty(profitCenterId),
                            });
                          }}
                          readOnly={edit.disabled}
                          error={valid.profitCenterId ? null : 'Profit center is a required field.'}
                        />
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
                {!edit.disabled ?
                  <div className="form-submission-section">
                    {selected.client === 'new' ?
                      <div className="button-container button-container-new">
                        <button
                          type="button"
                          disabled={!isFormModified(formData, details)}
                          className="button-reset link-button"
                          onClick={() => {
                            this.props.resetValidity({});
                            this.props.clearFormData({});
                          }}
                        >
                          Reset Form
                        </button>
                        <button
                          disabled={!isFormModified(formData, details) ||
                            (isFormModified(formData, details) && (!isFormValid(valid)
                              || formData.name.trim() === '' || formData.profitCenterId === ''))
                          }
                          type="button"
                          className="button-submit green-button"
                          onClick={() => {
                            // Check validity of fields in case user forgot to fill one out, since errors won't display
                            // initially.
                            this.props.setValidityForField({ field: 'name', valid: isStringNotEmpty(formData.name) });
                            this.props.setValidityForField({
                              field: 'profitCenterId',
                              valid: isStringNotEmpty(formData.profitCenterId),
                            });

                            if (isFormValid(valid)) {
                              this.props.resetValidity({});
                              this.props.saveNewClient(formData);
                              this.props.setEditStatus({ disabled: true });
                            }
                          }}
                        >
                          Create Client
                        </button>

                      </div> :
                      <div className="button-container button-container-edit">
                        <button
                          disabled={!isFormModified(formData, details)}
                          type="button"
                          className="button-reset link-button"
                          onClick={() => {
                            this.props.resetValidity({});
                            this.props.resetFormData({ details });
                          }}
                        >
                          Discard Changes
                        </button>
                        <button
                          type="button"
                          className="button-submit blue-button"
                          disabled={!isFormModified(formData, details) ||
                            (isFormModified(formData, details) && !isFormValid(valid))
                          }
                          onClick={() => {
                            this.editClient(formData).then(() => {
                              this.props.resetValidity({});
                              this.props.setEditStatus({ disabled: true });
                              this.props.fetchClientDetails({ clientId: details.id });
                            });
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
                  action={() => {
                    this.props.selectClient({ id: 'new' });
                    this.props.clearFormData({});
                    this.props.setEditStatus({ disabled: false });
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

  private async editClient(formData: AccessStateFormData) {
    return await this.props.editClient(formData);
  }
}

function mapStateToProps(state: AccessState): ClientAdminProps {
  const { data, selected, edit, cardAttributes, formData, filters, pending, valid } = state;

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
    valid,
  };
}

export const ConnectedClientAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ClientAdmin);
