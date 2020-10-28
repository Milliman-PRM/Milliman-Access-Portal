import * as _ from 'lodash';

import * as React from 'react';
import * as Modal from 'react-modal';

import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import * as AccessActionCreators from './redux/action-creators';
import {
  activeUsers, areRolesModified, clientEntities, isFormModified, isFormValid,
} from './redux/selectors';
import {
  AccessState, AccessStateCardAttributes, AccessStateEdit, AccessStateFilters, AccessStateFormData,
  AccessStateModals, AccessStatePending, AccessStateSelected, AccessStateValid,
} from './redux/store';

import { ClientWithEligibleUsers, ClientWithStats, Guid, ProfitCenter, User, UserRole } from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import { PanelSectionToolbar, PanelSectionToolbarButtons } from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import { CardExpansion } from '../shared-components/card/card-expansion';
import { ColumnSpinner } from '../shared-components/column-spinner';

import { ButtonSpinner } from '../shared-components/button-spinner';
import {
  CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { Filter } from '../shared-components/filter';
import { Input, TextAreaInput } from '../shared-components/form/input';
import { DropDown } from '../shared-components/form/select';
import { Toggle } from '../shared-components/form/toggle';
import { HitrustReasonEnum, RoleEnum } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import { ClientDetail } from '../system-admin/interfaces';

import { isEmailAddressValid, isStringNotEmpty } from '../../shared';
import { Checkbox } from '../shared-components/form/checkbox';

type ClientEntity = ((ClientWithEligibleUsers | ClientWithStats) & { indent: 1 | 2 }) | 'divider' | 'new';
interface ClientAdminProps {
  pending: AccessStatePending;
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
  modals: AccessStateModals;
  formModified: boolean;
  formValid: boolean;
  rolesModified: boolean;
}

class ClientAdmin extends React.Component<ClientAdminProps & typeof AccessActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  private readonly addUserHitrustReasons: Array<{selectionValue: number, selectionLabel: string}> = [
    { selectionValue: HitrustReasonEnum.NewMapClient, selectionLabel: 'New MAP Client' },
    { selectionValue: HitrustReasonEnum.NewEmployeeHire, selectionLabel: 'New employee hire' },
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
  ];
  private readonly removeUserHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    { selectionValue: HitrustReasonEnum.EmployeeTermination, selectionLabel: 'Employee termination' },
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.ClientRemoval, selectionLabel: 'Client removal' },
  ];
  private readonly clientRoleChangeHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    { selectionValue: HitrustReasonEnum.NewMapClient, selectionLabel: 'New MAP Client' },
    { selectionValue: HitrustReasonEnum.NewEmployeeHire, selectionLabel: 'New employee hire' },
    { selectionValue: HitrustReasonEnum.EmployeeTermination, selectionLabel: 'Employee termination' },
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.ClientRemoval, selectionLabel: 'Client removal' },
  ];

  public componentDidMount() {
    this.props.fetchProfitCenters({});
    this.props.fetchClients({});
  }

  public render() {
    return (
      <>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-right"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
        <NavBar currentView={this.currentView} />
        {this.renderClientPanel()}
        {this.props.selected.client !== null ?
          <div
            className="admin-panel-container
                       flex-item-12-12 flex-item-for-tablet-up-6-12 flex-item-for-desktop-up-6-12"
          >
            {this.props.pending.data.details ?
              <ColumnSpinner />
              : this.renderClientDetail()
            }
          </div> : null
        }
        {this.props.selected.client !== null && !this.props.pending.data.details && this.props.edit.disabled
          ? this.renderClientUsers() : null}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending, edit, formModified } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.data.clients}
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
                  this.changeClientFormState(formModified, !edit.disabled, selected.client, 'new', true, true);
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
              selected={entity.id === selected.client || this.clientIsNewChild(entity)}
              readonly={!entity.canManage}
              onSelect={() => {
                this.changeClientFormState(formModified, !edit.disabled, selected.client, entity.id, false, true);
              }}
              indentation={entity.indent}
              insertCard={this.clientIsNewChild(entity)}
            >
              <CardSectionMain>
                <CardText
                  text={!this.clientIsNewChild(entity) ? entity.name : 'New Sub-Client'}
                  isNewChild={this.clientIsNewChild(entity)}
                  subtext={entity.code}
                />
                {!this.clientIsNewChild(entity) ?
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
                  </CardSectionStats> : null
                }
                {entity.canManage ?
                  <CardSectionButtons>
                    {!this.clientHasChildren(clients, entity.id) ?
                      <CardButton
                        icon={'delete'}
                        color={'red'}
                        onClick={() => {
                          this.props.openDeleteClientModal({ id: entity.id, name: entity.name });
                        }}
                      /> : null
                    }
                    {!this.clientIsNewChild(entity) ?
                      <CardButton
                        icon={'edit'}
                        color={'blue'}
                        onClick={() => {
                          this.changeClientFormState(formModified, !edit.disabled, selected.client,
                            entity.id, true, true);
                        }}
                      /> : null
                    }
                    {entity.parentId === null ?
                      <CardButton
                        icon={'add'}
                        color={'green'}
                        onClick={() => {
                          this.changeClientFormStateForNewSubClient(formModified, !edit.disabled, entity.id);
                        }}
                      /> : null
                    }
                  </CardSectionButtons>
                : null}
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
                this.changeClientFormState(formModified, !edit.disabled, selected.client, 'new', true, true);
              }}
            />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        {this.renderModals()}
      </CardPanel>
    );
  }

  private renderClientDetail() {
    const { formData, profitCenters, details, selected, edit, valid, formModified, formValid } = this.props;
    return (
      <>
        <div
          id="client-info"
          className="admin-panel-container"
          style={{ height: '100%' }}
        >
          <h3 className="admin-panel-header">Client Information</h3>
          <PanelSectionToolbar>
            {!selected.readonly ?
              <PanelSectionToolbarButtons>
                {edit.disabled ?
                  <ActionIcon
                    label="Edit client details"
                    icon="edit"
                    action={() => {
                      this.props.setEditStatus({ disabled: false });
                    }}
                  /> :
                  <ActionIcon
                    label="Cancel edit"
                    icon="cancel"
                    action={() => {
                      this.props.resetFormData({ details });
                      this.props.resetValidity({});
                      this.props.setEditStatus({ disabled: true });

                      if (selected.client === 'new') { this.props.selectClient({ id: null }); }
                    }}
                  />
                }
              </PanelSectionToolbarButtons> : null
            }
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
                            this.props.setValidityForField({
                              field: 'name',
                              valid: isStringNotEmpty(event.currentTarget.value),
                            });
                            this.props.setFormFieldValue({ field: 'name', value: event.currentTarget.value });
                          }}
                          readOnly={edit.disabled}
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
                                 flex-item-for-tablet-up-6-12"
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
                                 flex-item-for-tablet-up-6-12"
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
                        <TextAreaInput
                          label={null}
                          name="NewUserWelcomeText"
                          onChange={() => {
                            return false;
                          }}
                          error={null}
                          value={null}
                          readOnly={edit.disabled}
                        />
                      </div>
                    </div>
                  </div>
                </div>
                {!edit.disabled ?
                  <div className="form-submission-section">
                    {selected.client === 'new' || selected.client === 'child' ?
                      <div className="button-container button-container-new">
                        <button
                          type="button"
                          disabled={!formModified}
                          className="button-reset link-button"
                          onClick={() => {
                            this.props.resetValidity({});
                            this.props.clearFormData({});
                          }}
                        >
                          Reset Form
                        </button>
                        <button
                          disabled={!formModified ||
                            (formModified && (!formValid
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

                            if (formValid) {
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
                          disabled={!formModified}
                          type="button"
                          className="button-reset link-button"
                          onClick={() => {
                            this.props.openDiscardEditModal({});
                          }}
                        >
                          Discard Changes
                        </button>
                        <button
                          type="button"
                          className="button-submit blue-button"
                          disabled={!formModified ||
                            (formModified && !formValid)
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
    const { assignedUsers, selected, edit, cardAttributes, pending, filters, rolesModified } = this.props;
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
                disabled={selected.readonly}
                onSelect={() => {
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
                  {!selected.readonly || (edit.userEnabled && selected.user === entity.id) ?
                    <CardSectionButtons>
                      {edit.userEnabled ?
                        <>
                          <CardButton
                            icon="checkmark"
                            color={'green'}
                            onClick={() => {
                              if (rolesModified) {
                                this.props.openChangeUserRolesModal({});
                              } else {
                                this.props.selectUser({ id: null });
                              }
                            }}
                          />
                          <CardButton
                            icon="cancel"
                            color={'red'}
                            onClick={() => {
                              this.props.selectUser({ id: null });
                              this.props.setExpandedUser({ id: entity.id });
                            }}
                          />
                        </> :
                        <>
                          <CardButton
                            icon="edit"
                            color={'blue'}
                            onClick={() => {
                              this.props.selectUser({ id: entity.id });
                              _.forEach(entity.userRoles, (role) => {
                                this.props.changeUserRolePending({
                                  roleEnum: role.roleEnum,
                                  isAssigned: role.isAssigned,
                                });
                              });
                              this.props.setExpandedUser({ id: entity.id });
                            }}
                          />
                          <CardButton
                            icon="remove-circle"
                            color={'red'}
                            onClick={() => this.props.openRemoveUserFromClientModal({
                              clientId: selected.client,
                              userId: entity.id,
                              name: entity.firstName && entity.lastName ?
                                `${entity.firstName} ${entity.lastName}` : entity.email,
                            })}
                          />
                        </>
                      }
                    </CardSectionButtons> : null
                  }
                </CardSectionMain>
                {entity.userRoles && Object.keys(entity.userRoles).length > 0 ?
                  <CardExpansion
                    label={'User roles'}
                    expanded={card && card.expanded}
                    setExpanded={(value) => value
                      ? this.props.setExpandedUser({ id: entity.id })
                      : this.props.setCollapsedUser({ id: entity.id })}
                  >
                    <Checkbox
                      name={entity.userRoles[RoleEnum.Admin].roleDisplayValue}
                      selected={this.isRoleSelected(RoleEnum.Admin, entity, selected.user,
                        pending.roles.roleAssignments)}
                      onChange={(checked) => {
                        this.props.changeUserRolePending({ roleEnum: RoleEnum.Admin, isAssigned: checked });
                      }}
                      readOnly={entity.id !== selected.user || !edit.userEnabled}
                    />
                    <Checkbox
                      name={entity.userRoles[RoleEnum.ContentAccessAdmin].roleDisplayValue}
                      selected={this.isRoleSelected(RoleEnum.ContentAccessAdmin, entity, selected.user,
                        pending.roles.roleAssignments)}
                      onChange={(checked) => {
                        this.props.changeUserRolePending({
                          roleEnum: RoleEnum.ContentAccessAdmin,
                          isAssigned: checked,
                        });
                      }}
                      readOnly={entity.id !== selected.user || !edit.userEnabled}
                    />
                    <Checkbox
                      name={entity.userRoles[RoleEnum.ContentPublisher].roleDisplayValue}
                      selected={this.isRoleSelected(RoleEnum.ContentPublisher, entity, selected.user,
                        pending.roles.roleAssignments)}
                      onChange={(checked) => {
                        this.props.changeUserRolePending({
                          roleEnum: RoleEnum.ContentPublisher,
                          isAssigned: checked,
                        });
                      }}
                      readOnly={entity.id !== selected.user || !edit.userEnabled}
                    />
                    <Checkbox
                      name={entity.userRoles[RoleEnum.ContentUser].roleDisplayValue}
                      selected={this.isRoleSelected(RoleEnum.ContentUser, entity, selected.user,
                        pending.roles.roleAssignments)}
                      onChange={(checked) => {
                        this.props.changeUserRolePending({
                          roleEnum: RoleEnum.ContentUser,
                          isAssigned: checked,
                        });
                      }}
                      readOnly={entity.id !== selected.user || !edit.userEnabled}
                    />
                    <Checkbox
                      name={entity.userRoles[RoleEnum.FileDropAdmin].roleDisplayValue}
                      selected={this.isRoleSelected(RoleEnum.FileDropAdmin, entity, selected.user,
                        pending.roles.roleAssignments)}
                      onChange={(checked) => {
                        this.props.changeUserRolePending({
                          roleEnum: RoleEnum.FileDropAdmin,
                          isAssigned: checked,
                        });
                      }}
                      readOnly={entity.id !== selected.user || !edit.userEnabled}
                    />
                    <Checkbox
                      name={entity.userRoles[RoleEnum.FileDropUser].roleDisplayValue}
                      selected={this.isRoleSelected(RoleEnum.FileDropUser, entity, selected.user,
                        pending.roles.roleAssignments)}
                      onChange={(checked) => {
                        this.props.changeUserRolePending({
                          roleEnum: RoleEnum.FileDropUser,
                          isAssigned: checked,
                        });
                      }}
                      readOnly={entity.id !== selected.user || !edit.userEnabled}
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
              {!selected.readonly ?
                <PanelSectionToolbarButtons>
                  <ActionIcon
                    label="Expand all user cards"
                    icon="expand-cards"
                    action={() => false}
                  />
                  <ActionIcon
                    label="Add or create a new client user"
                    icon="add"
                    action={() => {
                      this.props.selectUser({ id: 'new' });
                      this.props.openCreateClientUserModal({ clientId: selected.client });
                    }}
                  />
                </PanelSectionToolbarButtons> : null
              }
            </PanelSectionToolbarButtons>
          </PanelSectionToolbar>
        </CardPanel>
      </>
    );
  }

  private renderModals() {
    const { modals, pending, details, selected, rolesModified } = this.props;
    return (
      <>
        <Modal
          isOpen={modals.deleteClient.isOpen}
          onRequestClose={() => this.props.closeDeleteClientModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title red">Delete Client</h2>
          <span className="modal-text">Delete <strong>{pending.deleteClient.name}</strong>?</span>
          <span className="modal-text">This action <u><strong>cannot</strong></u> be undone.</span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              this.props.openDeleteClientConfirmationModal({});
            }}
          >
            <div className="button-container">
              <button className="link-button" type="button" onClick={() => this.props.closeDeleteClientModal({})}>
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
          isOpen={modals.deleteClientConfirmation.isOpen}
          onRequestClose={() => this.props.closeDeleteClientConfirmationModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title red">Delete Client</h2>
          <span className="modal-text">
            Please confirm the deletion of <strong>{pending.deleteClient.name}</strong>.
          </span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              this.props.deleteClient(pending.deleteClient.id);
            }}
          >
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => this.props.closeDeleteClientConfirmationModal({})}
              >
                Cancel
              </button>
              <button
                className="red-button"
                type="submit"
              >
                Delete
                {this.props.pending.data.clients
                  ? <ButtonSpinner version="circle" />
                  : null
                }
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.createClientUser.isOpen}
          onRequestClose={() => this.props.closeCreateClientUserModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title blue">Add User</h2>
          <span className="modal-text text-muted">
            Please provide a valid email address using an approved email domain and a reason
            for adding the user to this Client.
          </span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              this.props.saveNewClientUser({
                memberOfClientId: pending.createClientUser.memberOfClientId,
                email: pending.createClientUser.email,
                userName: pending.createClientUser.userName,
                reason: pending.hitrustReason.reason,
              });
            }}
          >
            <Input
              type="text"
              name="Email"
              label="Email *"
              onChange={(event) => this.props.setCreateClientUserModalEmail({
                email: event.currentTarget.value,
              })}
              value={pending.createClientUser.email}
              autoFocus={true}
              onBlur={() => this.props.setCreateClientUserModalEmailError({
                showError: !isEmailAddressValid(pending.createClientUser.email),
              })}
              error={pending.createClientUser.displayEmailError ? 'Please enter a valid email address.' : null}
            />
            <div className="checkbox-container">
              <span className="modal-text">
                <strong>User Roles</strong>
              </span>
              <Checkbox
                name={'Client Admin'}
                selected={this.isRoleSelected(RoleEnum.Admin, null, 'new', pending.roles.roleAssignments)}
                onChange={(checked) => {
                  this.props.changeUserRolePending({ roleEnum: RoleEnum.Admin, isAssigned: checked });
                }}
                readOnly={false}
              />
              <Checkbox
                name={'Content Access Admin'}
                selected={this.isRoleSelected(RoleEnum.ContentAccessAdmin, null, 'new', pending.roles.roleAssignments)}
                onChange={(checked) => {
                  this.props.changeUserRolePending({ roleEnum: RoleEnum.ContentAccessAdmin, isAssigned: checked });
                }}
                readOnly={false}
              />
              <Checkbox
                name={'Content Publisher'}
                selected={this.isRoleSelected(RoleEnum.ContentPublisher, null, 'new', pending.roles.roleAssignments)}
                onChange={(checked) => {
                  this.props.changeUserRolePending({ roleEnum: RoleEnum.ContentPublisher, isAssigned: checked });
                }}
                readOnly={false}
              />
              <Checkbox
                name={'Content User'}
                selected={this.isRoleSelected(RoleEnum.ContentUser, null, 'new', pending.roles.roleAssignments)}
                onChange={(checked) => {
                  this.props.changeUserRolePending({ roleEnum: RoleEnum.ContentUser, isAssigned: checked });
                }}
                readOnly={false}
              />
              <Checkbox
                name={'File Drop Admin'}
                selected={this.isRoleSelected(RoleEnum.FileDropAdmin, null, 'new', pending.roles.roleAssignments)}
                onChange={(checked) => {
                  this.props.changeUserRolePending({ roleEnum: RoleEnum.FileDropAdmin, isAssigned: checked });
                }}
                readOnly={false}
              />
              <Checkbox
                name={'File Drop User'}
                selected={this.isRoleSelected(RoleEnum.FileDropUser, null, 'new', pending.roles.roleAssignments)}
                onChange={(checked) => {
                  this.props.changeUserRolePending({ roleEnum: RoleEnum.FileDropUser, isAssigned: checked });
                }}
                readOnly={false}
              />
            </div>
            <DropDown
              name="reason"
              label="Reason *"
              value={null}
              values={this.addUserHitrustReasons}
              onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                this.props.setRoleChangeReason({ reason: parseInt(target.value, 10) });
              }}
              error={null}
              placeholderText={'Choose an option'}
            />
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => this.props.closeCreateClientUserModal({})}
              >
                Cancel
              </button>
              <button
                className="blue-button"
                type="submit"
                disabled={!pending.hitrustReason.reason || !pending.createClientUser.email ||
                  pending.createClientUser.displayEmailError}
              >
                Add User
                {this.props.pending.data.clientUsers
                  ? <ButtonSpinner version="circle" />
                  : null
                }
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.removeClientUser.isOpen}
          onRequestClose={() => this.props.closeRemoveUserFromClientModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title red">Remove User</h2>
          <span className="modal-text">
            Please provide a reason for removing the user from this Client.
          </span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              this.props.removeClientUser({
                clientId: pending.removeClientUser.clientId,
                userId: pending.removeClientUser.userId,
                reason: pending.hitrustReason.reason,
              });
            }}
          >
            <DropDown
              name="reason"
              label="Reason"
              value={null}
              values={this.removeUserHitrustReasons}
              onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                this.props.setRoleChangeReason({ reason: parseInt(target.value, 10) });
              }}
              error={null}
              placeholderText={'Choose an option'}
            />
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => this.props.closeRemoveUserFromClientModal({})}
              >
                Cancel
              </button>
              <button
                className="red-button"
                type="submit"
                disabled={!pending.hitrustReason.reason}
              >
                Remove
                {this.props.pending.data.clientUsers
                  ? <ButtonSpinner version="circle" />
                  : null
                }
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.discardEdit.isOpen}
          onRequestClose={() => this.props.closeDiscardEditModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title blue">Reset Form</h2>
          <span className="modal-text text-muted">
            Would you like to reset the form?
          </span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              this.props.resetValidity({});
              this.props.resetFormData({ details });
              this.props.closeDiscardEditModal({});
              this.props.setEditStatus({ disabled: true });
            }}
          >
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => this.props.closeDiscardEditModal({})}
              >
                Continue Editing
              </button>
              <button
                className="blue-button"
                type="submit"
              >
                Reset
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.discardEditAfterSelect.isOpen}
          onRequestClose={() => this.props.closeDiscardEditAfterSelectModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title blue">Discard Changes</h2>
          <span className="modal-text">
            Would you like to discard any unsaved changes?
          </span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              if (pending.discardEditAfterSelect.newSubClientParentId === null) {
                this.changeClientFormState(false, false,
                  selected.client,
                  pending.discardEditAfterSelect.newlySelectedClientId,
                  pending.discardEditAfterSelect.editAfterSelect,
                  true);
              } else {
                this.changeClientFormStateForNewSubClient(false, false,
                  pending.discardEditAfterSelect.newSubClientParentId);
              }
              this.props.closeDiscardEditAfterSelectModal({});

            }}
          >
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => this.props.closeDiscardEditAfterSelectModal({})}
              >
                Continue Editing
              </button>
              <button
                className="blue-button"
                type="submit"
              >
                Discard
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.changeUserRoles.isOpen}
          onRequestClose={() => this.props.closeChangeUserRolesModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title blue">Change User Role</h2>
          <span className="modal-text">
            Please provide a reason for changing the user's role in this Client.
          </span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              this.props.selectUser({ id: null });
              this.props.updateAllUserRolesInClient({
                clientId: selected.client,
                userId: selected.user,
                reason: pending.hitrustReason.reason,
                roleAssignments: pending.roles.roleAssignments,
              });
              this.props.closeChangeUserRolesModal({});
            }}
          >
            <DropDown
              name="reason"
              label="Reason"
              value={pending.hitrustReason.reason}
              values={this.clientRoleChangeHitrustReasons}
              onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                this.props.setRoleChangeReason({ reason: parseInt(target.value, 10) });
              }}
              error={null}
              placeholderText={'Choose an option'}
            />
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => {
                  this.props.selectUser({ id: null });
                  this.props.closeChangeUserRolesModal({});
                }}
              >
                Cancel
              </button>
              <button
                className="blue-button"
                type="submit"
                disabled={!pending.hitrustReason.reason}
              >
                Change roles
              </button>
            </div>
          </form>
        </Modal>
      </>
    );
  }

  private changeClientFormState(formModified: boolean, currentlyEditing: boolean, oldClient: Guid, newClient: Guid,
                                edit: boolean, resetValidityAfterSelect: boolean) {
    if (currentlyEditing && formModified) {
      this.props.openDiscardEditAfterSelectModal({
        newlySelectedClientId: newClient,
        editAfterSelect: edit,
        newSubClientParentId: null,
      });
    } else {
      if (!(edit && oldClient === newClient)) { // Handles clicking 'edit' for an already selected client.
        this.props.selectClient({ id: newClient });
      }
      this.props.setEditStatus({ disabled: !edit });

      if (resetValidityAfterSelect) {
        this.props.resetValidity({});
      }

      if (newClient !== 'new') {
        this.props.fetchClientDetails({ clientId: newClient });
      } else {
        this.props.resetClientDetails({});
        this.props.clearFormData({});
      }
    }
  }

  private changeClientFormStateForNewSubClient(formModified: boolean, currentlyEditing: boolean, parent: Guid) {
    if (currentlyEditing && formModified) {
      this.props.openDiscardEditAfterSelectModal({
        newlySelectedClientId: 'child',
        editAfterSelect: true,
        newSubClientParentId: parent,
      });
    } else {
      this.props.selectNewSubClient({ parentId: parent });
      this.props.setFormFieldValue({ field: 'parentClientId', value: parent });
    }
  }

  private async editClient(formData: AccessStateFormData) {
    return await this.props.editClient(formData);
  }

  private clientHasChildren(clients: ClientEntity[], clientId: Guid) {
    return clients.filter((c) => {
      if (c === 'divider' || c === 'new') {
        return false;
      } else if (c.parentId === clientId) {
        return true;
      }
      return false;
    }).length > 0;
  }

  private clientIsNewChild(client: ClientEntity) {
    return client !== 'divider'
      && client !== 'new'
      && client.parentId !== null
      && client.id === null;
  }

  private isRoleSelected(roleEnum: RoleEnum, entity: User, selectedUserId: Guid,
                         pendingRoleAssignments: Array<{ roleEnum: RoleEnum, isAssigned: boolean }>) {
    if (entity && entity.id === selectedUserId || (entity === null && selectedUserId === 'new')) {
      const role = pendingRoleAssignments.find((ra) => ra.roleEnum === roleEnum);
      if (role) {
        return role.isAssigned;
      }
      return false;
    }
    return entity.userRoles[roleEnum].isAssigned;
  }
}

function mapStateToProps(state: AccessState): ClientAdminProps {
  const { data, selected, edit, cardAttributes, formData, filters, pending, valid, modals } = state;

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
    modals,
    formModified: isFormModified(state),
    formValid: isFormValid(state),
    rolesModified: areRolesModified(state),
  };
}

export const ConnectedClientAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ClientAdmin);
