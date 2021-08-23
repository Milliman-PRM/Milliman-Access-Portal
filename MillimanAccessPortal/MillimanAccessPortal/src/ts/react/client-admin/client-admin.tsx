import * as _ from 'lodash';

import * as moment from 'moment';

import * as React from 'react';
import * as Modal from 'react-modal';

import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';
import { toastr } from 'react-redux-toastr';

import * as AccessActionCreators from './redux/action-creators';
import {
  allUsersCollapsed, allUsersExpanded, areRolesModified, clientEntities,
  isFormModified, isFormValid, userCanCreateClients, userEntities, userIsRemovingOwnClientAdminRole,
} from './redux/selectors';
import {
  AccessState, AccessStateCardAttributes, AccessStateEdit, AccessStateFilters, AccessStateFormData,
  AccessStateModals, AccessStatePending, AccessStateSelected, AccessStateValid,
} from './redux/store';

import { ClientWithEligibleUsers, ClientWithStats, Guid, ProfitCenter, User } from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { BrowserSupportBanner } from '../shared-components/browser-support-banner';
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
import { Checkbox } from '../shared-components/form/checkbox';
import { Input, MultiAddInput, TextAreaInput } from '../shared-components/form/input';
import { DropDown } from '../shared-components/form/select';
import { Toggle } from '../shared-components/form/toggle';
import { EnableDisabledAccountReasonEnum, HitrustReasonEnum, RoleEnum } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import { ClientDetail } from '../system-admin/interfaces';

import { isDomainNameValid, isEmailAddressValid, isStringNotEmpty } from '../../shared';
import { setUnloadAlert } from '../../unload-alerts';

type ClientEntity = ((ClientWithEligibleUsers | ClientWithStats) & { indent: 1 | 2 }) | 'divider' | 'new';
type UserEntity = User | 'new';
interface ClientAdminProps {
  pending: AccessStatePending;
  clients: ClientEntity[];
  profitCenters: ProfitCenter[];
  details: ClientDetail;
  formData: AccessStateFormData;
  assignedUsers: UserEntity[];
  selected: AccessStateSelected;
  edit: AccessStateEdit;
  filters: AccessStateFilters;
  cardAttributes: AccessStateCardAttributes;
  valid: AccessStateValid;
  modals: AccessStateModals;
  formModified: boolean;
  formValid: boolean;
  allUsersExpanded: boolean;
  allUsersCollapsed: boolean;
  rolesModified: boolean;
  canCreateClients: boolean;
  currentUser: string;
  currentlyRemovingOwnAdminRole: boolean;
}

class ClientAdmin extends React.Component<ClientAdminProps & typeof AccessActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  private readonly addUserHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.NewEmployeeHire, selectionLabel: 'New employee hire' },
    { selectionValue: HitrustReasonEnum.NewMapClient, selectionLabel: 'New MAP Client' },
  ];
  private readonly removeUserHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.ClientRemoval, selectionLabel: 'Client removal' },
    { selectionValue: HitrustReasonEnum.EmployeeTermination, selectionLabel: 'Employee termination' },
  ];
  private readonly clientRoleChangeHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.ClientRemoval, selectionLabel: 'Client removal' },
    { selectionValue: HitrustReasonEnum.EmployeeTermination, selectionLabel: 'Employee termination' },
    { selectionValue: HitrustReasonEnum.NewEmployeeHire, selectionLabel: 'New employee hire' },
    { selectionValue: HitrustReasonEnum.NewMapClient, selectionLabel: 'New MAP Client' },
  ];
  private readonly requestForRenableDisabledAccountReasons:
    Array<{ selectionValue: number, selectionLabel: string }> = [
      {
        selectionValue: EnableDisabledAccountReasonEnum.ChangeInEmployeeResponsibilities,
        selectionLabel: 'Change in employee responsibilities',
      },
      {
        selectionValue: EnableDisabledAccountReasonEnum.ReturningEmployee,
        selectionLabel: 'Returning employee',
      },
  ];

  public componentDidMount() {
    this.props.setCurrentUser({ username: document.getElementById('current-user-email').innerText });
    this.props.fetchProfitCenters({});
    this.props.fetchClients({});
    setUnloadAlert(() => (this.props.edit.userEnabled && this.props.rolesModified)
      || (!this.props.edit.disabled && this.props.formModified));
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
        <BrowserSupportBanner />
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
    const { clients, selected, filters, pending, edit, formModified, rolesModified, canCreateClients } = this.props;
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
                  this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                    this.changeClientFormState(formModified, !edit.disabled, selected.client, 'new', false,
                      true, true);
                  });
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
                this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                  this.changeClientFormState(formModified, !edit.disabled, selected.client, entity.id,
                    entity.canManage, false, true);
                });
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
                      name={'Client users'}
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
                          this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                            this.changeClientFormState(formModified, !edit.disabled, selected.client,
                              entity.id, entity.canManage, true, true);
                          });
                        }}
                      /> : null
                    }
                    {entity.parentId === null ?
                      <CardButton
                        icon={'add'}
                        color={'green'}
                        onClick={() => {
                          this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                            this.changeClientFormStateForNewSubClient(formModified, !edit.disabled, entity.id);
                          });
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
            {canCreateClients ?
              <ActionIcon
                label="Add or create a new client"
                icon="add"
                action={() => {
                  this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                    this.changeClientFormState(formModified, !edit.disabled, selected.client, 'new', false,
                      true, true);
                  });
                }}
              /> : null}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        {this.renderModals()}
      </CardPanel>
    );
  }

  private renderClientDetail() {
    const {
      formData, profitCenters, details, selected, edit, valid, formModified, rolesModified, formValid,
    } = this.props;
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
                      this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                        this.props.setEditStatus({ disabled: false });
                      });
                    }}
                  /> :
                  <ActionIcon
                    label="Cancel edit"
                    icon="cancel"
                    action={() => {
                      if (formModified) {
                        this.props.openDiscardEditAfterSelectModal({
                          newlySelectedClientId: null, // Same client should be selected after cancel.
                          editAfterSelect: false,
                          newSubClientParentId: null,
                          canManageNewlySelectedClient: !selected.readonly,
                        });
                      } else {
                        this.discardChangesOnSelectedClient(details, selected.client);
                      }
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
                          value={formData.clientCode || ''}
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
                          value={formData.contactName || ''}
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
                          value={formData.contactTitle || ''}
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
                          value={formData.contactEmail || ''}
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'contactEmail',
                              value: event.currentTarget.value.trim() !== '' ? event.currentTarget.value.trim() : null,
                            });
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
                          value={formData.contactPhone || ''}
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'contactPhone',
                              value: event.currentTarget.value.trim() !== '' ?
                                event.currentTarget.value.trim() : null,
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
                  <h4 className="form-section-title">Security Information</h4>
                  <div className="form-input-container">
                    <div className="form-input form-input-selectized flex-item-12-12">
                      <div>
                        {edit.disabled ?
                          <Input
                            name="approvedEmailDomainList"
                            label="Approved Email Domain List"
                            type="text"
                            value={formData.acceptedEmailDomainList.join(', ')}
                            onChange={null}
                            readOnly={edit.disabled}
                            onBlur={() => { return; }}
                            error={null}
                          /> :
                          <MultiAddInput
                            name="approvedEmailDomainList"
                            label="Approved Email Domain List"
                            type="text"
                            limit={formData.domainListCountLimit}
                            limitText={'domains'}
                            list={formData.acceptedEmailDomainList}
                            value={''}
                            exceptions={['milliman.com']}
                            addItem={(item: string, overLimit: boolean, itemAlreadyExists: boolean) => {
                              if (itemAlreadyExists) {
                                toastr.warning('', 'That domain already exists.');
                              } else if (!isDomainNameValid(item)) {
                                toastr.warning('', 'Please enter a valid domain name (e.g. domain.com)');
                              } else if (overLimit) {
                                toastr.warning('', `
                                  You have reached the allowed domain limit for this client.
                                  Contact map.support@milliman.com to request an increase to this limit.
                                `);
                              } else {
                                this.props.appendFormFieldArrayValue({
                                  field: 'acceptedEmailDomainList',
                                  value: item,
                                });
                              }
                            }}
                            removeItemCallback={(index: number) => {
                              this.props.setFormFieldValue({
                                field: 'acceptedEmailDomainList',
                                value: formData.acceptedEmailDomainList.slice(0, index)
                                  .concat(formData.acceptedEmailDomainList.slice(index + 1)),
                              });
                            }}
                            readOnly={edit.disabled}
                            onBlur={() => { return; }}
                            error={null}
                          />
                        }
                      </div>
                    </div>
                    <div className="form-input form-input-selectized flex-item-12-12">
                      <div>
                        {edit.disabled ?
                          <Input
                            name="approvedEmailAddressExceptionList"
                            label="Approved Email Address Exception List"
                            type="text"
                            value={formData.acceptedEmailAddressExceptionList.join(', ')}
                            onChange={null}
                            readOnly={edit.disabled}
                            error={null}
                          /> :
                          <MultiAddInput
                            name="acceptedEmailAddressExceptionList"
                            label="Approved Email Address Exception List"
                            type="text"
                            list={formData.acceptedEmailAddressExceptionList}
                            value={''}
                            addItem={(item: string, _overLimit: boolean, itemAlreadyExists: boolean) => {
                              if (!isEmailAddressValid(item)) {
                                toastr.warning('', 'Please enter a valid email address (e.g. username@domain.com)');
                              } else if (itemAlreadyExists) {
                                toastr.warning('', 'That email address already exists.');
                              } else {
                                this.props.appendFormFieldArrayValue({
                                  field: 'acceptedEmailAddressExceptionList',
                                  value: item,
                                });
                              }
                            }}
                            removeItemCallback={(index: number) => {
                              this.props.setFormFieldValue({
                                field: 'acceptedEmailAddressExceptionList',
                                value: formData.acceptedEmailAddressExceptionList.slice(0, index)
                                  .concat(formData.acceptedEmailAddressExceptionList.slice(index + 1)),
                              });
                            }}
                            readOnly={edit.disabled}
                            error={null}
                          />
                        }
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
                          value={formData.consultantName || ''}
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
                          value={formData.consultantEmail || ''}
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'consultantEmail',
                              value: event.currentTarget.value.trim() ? event.currentTarget.value.trim() : null,
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
                          value={formData.consultantOffice || ''}
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
                          value={formData.profitCenterId || ''}
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
                          checked={formData.useNewUserWelcomeText}
                          onClick={() => {
                            if (!edit.disabled) {
                              this.props.setFormFieldValue({
                                field: 'useNewUserWelcomeText',
                                value: !formData.useNewUserWelcomeText,
                              });
                            }
                          }}
                          readOnly={edit.disabled}
                        />
                      </div>
                      <div className="flex-item-for-phone-only-12-12 content-item-flex-1">
                        <TextAreaInput
                          label={null}
                          name="NewUserWelcomeText"
                          onChange={(event) => {
                            this.props.setFormFieldValue({
                              field: 'newUserWelcomeText',
                              value: event.currentTarget.value,
                            });
                          }}
                          error={null}
                          value={formData.useNewUserWelcomeText ? formData.newUserWelcomeText : ''}
                          readOnly={edit.disabled || !formData.useNewUserWelcomeText}
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
                            this.props.editClient({
                              ...formData,
                              newUserWelcomeText: formData.useNewUserWelcomeText ? formData.newUserWelcomeText : null,
                            });
                            this.props.resetValidity({});
                            this.props.setEditStatus({ disabled: true });
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
    const {
      assignedUsers,
      selected,
      edit,
      cardAttributes,
      pending,
      allUsersExpanded: allExpanded,
      allUsersCollapsed: allCollapsed,
      filters,
      rolesModified,
    } = this.props;
    return (
      <>
        <CardPanel
          entities={assignedUsers}
          renderEntity={(entity, key) => {
            if (entity === 'new') {
              return !selected.readonly ? (
                <div
                  key={key}
                  className="card-container action-card-container"
                  onClick={() => {
                    this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                      this.props.selectUser({ id: null });
                      this.props.openCreateClientUserModal({
                        clientId: selected.client,
                      });
                    });
                  }}
                >
                  <div className="card-body-container card-100 action-card">
                    <h2 className="card-body-primary-text">
                      <svg className="action-card-icon">
                        <use href="#add" />
                      </svg>
                      <span>ADD USER</span>
                    </h2>
                  </div>
                </div>
              ) : null;
            }
            const card = cardAttributes.user[entity.id];
            return (
              <Card
                key={key}
                selected={false}
                disabled={selected.readonly}
                onSelect={null}
                bannerMessage={(entity.isAccountDisabled || entity.isAccountNearDisabled) ?
                  {
                    level: entity.isAccountDisabled ? 'error' : 'informational',
                    message:
                      <div>{(entity.isAccountDisabled ? 'Account disabled on ' : 'Account will be disabled on ') +
                        moment.utc(entity.dateOfAccountDisable).local().format('MMM DD, YYYY')}
                      </div>,
                  } : null
                }
                borderLevel={(entity.isAccountDisabled || entity.isAccountNearDisabled) ?
                  (entity.isAccountDisabled ? 'error' : 'informational') : 'default'
                }
              >
                <CardSectionMain>
                  <svg
                    className="card-user-icon"
                    style={{
                      width: '5em',
                      height: '5em',
                    }}
                  >
                    <use xlinkHref="#user" />
                  </svg>
                  {entity.userRoles &&
                    entity.userRoles[RoleEnum.Admin] &&
                    entity.userRoles[RoleEnum.Admin].isAssigned ?
                    <svg className="card-user-role-indicator admin">
                      <use href="#add" />
                    </svg> : null
                  }
                  <CardText
                    text={
                      entity.firstName && entity.lastName ? `${entity.firstName} ${entity.lastName}` : entity.email}
                    subtext={entity.firstName && entity.lastName ? entity.email : ''}
                  />
                  {!selected.readonly || (edit.userEnabled && selected.user === entity.id) ?
                    <CardSectionButtons>
                      {edit.userEnabled && entity.id === selected.user ?
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
                              if (rolesModified) {
                                this.props.openDiscardUserRoleChangesModal({ callback: null });
                              } else {
                                this.props.selectUser({ id: null });
                                this.props.setExpandedUser({ id: entity.id });
                              }
                            }}
                          />
                        </> :
                        <>
                          {!edit.userEnabled &&
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
                                onClick={() => {
                                  this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                                    this.props.openRemoveUserFromClientModal({
                                      clientId: selected.client,
                                      userId: entity.id,
                                      name: entity.email,
                                    });
                                  });
                                }}
                              />
                            </>
                          }
                        </>
                      }
                    </CardSectionButtons> : null
                  }
                </CardSectionMain>
                <CardExpansion
                  label={'User roles'}
                  expanded={card && card.expanded}
                  setExpanded={(value) => value
                    ? this.props.setExpandedUser({ id: entity.id })
                    : this.props.setCollapsedUser({ id: entity.id })}
                >
                  {!selected.readonly ?
                    <div>
                      <Checkbox
                        name={'Client Admin'}
                        selected={this.isRoleSelected(RoleEnum.Admin, entity, selected.user,
                          pending.roles.roleAssignments)}
                        onChange={(checked) => {
                          this.props.changeUserRolePending({ roleEnum: RoleEnum.Admin, isAssigned: checked });
                        }}
                        readOnly={entity.id !== selected.user || !edit.userEnabled}
                      />
                      <Checkbox
                        name={'Content Access Admin'}
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
                        name={'Content Publisher'}
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
                        name={'Content User'}
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
                        name={'File Drop Admin'}
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
                        name={'File Drop User'}
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
                    </div> : null
                  }
                  {entity.isAccountDisabled ?
                    <button
                      className="link-button small-padding"
                      type="button"
                      onClick={() =>
                        this.props.openRequestReenableDisabledAccountModal({
                          userId: entity.id,
                          userEmail: entity.email,
                        })
                      }
                    >
                      Re-enable User Account
                    </button> : null
                  }
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
              {!selected.readonly ?
                <PanelSectionToolbarButtons>
                  {allExpanded ?
                    null :
                    <ActionIcon
                      label="Expand all user cards"
                      icon="expand-cards"
                      action={() => this.props.setAllExpandedUser({})}
                    />
                  }
                  {allCollapsed ?
                    null :
                    <ActionIcon
                      label="Collapse all user cards"
                      icon="collapse-cards"
                      action={() => this.props.setAllCollapsedUser({})}
                    />
                  }
                  <ActionIcon
                    label="Add or create a new client user"
                    icon="add"
                    action={() => {
                      this.handleCallbackForPendingRoleChanges(edit.userEnabled && rolesModified, () => {
                        this.props.selectUser({ id: null });
                        this.props.openCreateClientUserModal({ clientId: selected.client });
                      });
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
    const { modals, pending, details, selected, currentlyRemovingOwnAdminRole, currentUser } = this.props;
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
                roleAssignments: pending.roles.roleAssignments,
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
            <div className="checkbox-container" style={{ margin: '1rem 0' }}>
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
              value={pending.hitrustReason.reason.toString()}
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

              if (pending.removeClientUser.name === currentUser) {
                this.props.selectClient({ id: null });
              }
            }}
          >
            <DropDown
              name="reason"
              label="Reason"
              value={pending.hitrustReason.reason.toString()}
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
                if (pending.discardEditAfterSelect.newlySelectedClientId === null) {
                  this.discardChangesOnSelectedClient(details, selected.client);
                } else {
                  this.changeClientFormState(false, false,
                    selected.client,
                    pending.discardEditAfterSelect.newlySelectedClientId,
                    pending.discardEditAfterSelect.canManageNewlySelectedClient,
                    pending.discardEditAfterSelect.editAfterSelect,
                    true);
                }
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
              this.props.updateAllUserRolesInClient({
                clientId: selected.client,
                userId: selected.user,
                reason: pending.hitrustReason.reason,
                roleAssignments: pending.roles.roleAssignments,
              });

              this.props.selectUser({ id: null });
              if (currentlyRemovingOwnAdminRole) {
                this.props.selectClient({ id: null });
              }
              this.props.closeChangeUserRolesModal({});
            }}
          >
            <DropDown
              name="reason"
              label="Reason"
              value={pending.hitrustReason.reason.toString()}
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
          isOpen={modals.discardUserRoleChanges.isOpen}
          onRequestClose={() => this.props.closeDiscardUserRoleChangesModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title red">Discard Changes</h2>
          <span className="modal-text">
            Would you like to discard unsaved changes to the User roles?
          </span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              this.props.selectUser({ id: null });
              if (pending.discardEditUserRoles.callback) {
                pending.discardEditUserRoles.callback();
              }
              this.props.closeDiscardUserRoleChangesModal({});
            }}
          >
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => this.props.closeDiscardUserRoleChangesModal({})}
              >
                Cancel
              </button>
              <button
                className="red-button"
                type="submit"
              >
                Discard
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.requestReenableDisabledAccount.isOpen}
          onRequestClose={() => this.props.closeRequestReenableDisabledAccountModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h2 className="title blue">Contact Support</h2>
          <span className="modal-text text-muted">
            Re-enable user <strong>{pending.reenableDisabledAccountReason.userEmail}</strong>
          </span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
            }}
          >
            <DropDown
              name="reason"
              label="Reason *"
              value={pending.reenableDisabledAccountReason.reason.toString()}
              values={this.requestForRenableDisabledAccountReasons}
              onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                this.props.setAcountReenableRequestReason({ reason: parseInt(target.value, 10) });
              }}
              error={null}
              placeholderText={'Select a reason for this request'}
            />
            <div className="button-container">
              <button
                className="link-button"
                type="button"
                onClick={() => this.props.closeRequestReenableDisabledAccountModal({})}
              >
                Cancel
              </button>
              <button
                className="blue-button"
                type="submit"
                disabled={!pending.reenableDisabledAccountReason.reason}
                onClick={() =>
                  this.props.requestReenableUserAccount({
                    clientId: selected.client,
                    userId: pending.reenableDisabledAccountReason.userId,
                    reason: pending.reenableDisabledAccountReason.reason,
                  })
                }
              >
                Submit
                {this.props.pending.data.requestReenableDisabledAccount
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

  private changeClientFormState(formModified: boolean, currentlyEditing: boolean, oldClient: Guid, newClient: Guid,
                                canManage: boolean, edit: boolean, resetValidityAfterSelect: boolean) {
    if (currentlyEditing && formModified) {
      this.props.openDiscardEditAfterSelectModal({
        newlySelectedClientId: newClient,
        editAfterSelect: edit,
        newSubClientParentId: null,
        canManageNewlySelectedClient: canManage,
      });
    } else {
      if (!(edit && oldClient === newClient)) { // Handles clicking 'edit' for an already selected client.
        this.props.selectClient({ id: newClient, readonly: !canManage });
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
        canManageNewlySelectedClient: true,
      });
    } else {
      this.props.selectNewSubClient({ parentId: parent });
      this.props.setFormFieldValue({ field: 'parentClientId', value: parent });
    }
  }

  private handleCallbackForPendingRoleChanges(useCallback: boolean, callbackFunction: () => void) {
    if (useCallback) {
      this.props.openDiscardUserRoleChangesModal({
        callback: callbackFunction,
      });
    } else {
      callbackFunction();
    }
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
      const role = pendingRoleAssignments.filter((ra) => ra.roleEnum === roleEnum)[0];
      if (role) {
        return role.isAssigned;
      }
      return false;
    }

    if (entity.userRoles) {
      return entity.userRoles[roleEnum] && entity.userRoles[roleEnum].isAssigned;
    }
    return false;
  }

  private discardChangesOnSelectedClient(details: ClientDetail, selectedClientId: Guid) {
    this.props.resetFormData({ details });
    this.props.resetValidity({});
    this.props.setEditStatus({ disabled: true });
    if (selectedClientId === 'new') { this.props.selectClient({ id: null }); }
  }
}

function mapStateToProps(state: AccessState): ClientAdminProps {
  const { data, selected, edit, cardAttributes, formData, filters, pending, valid, modals, currentUser } = state;

  return {
    clients: clientEntities(state),
    profitCenters: data.profitCenters,
    details: data.details,
    cardAttributes,
    assignedUsers: userEntities(state),
    formData,
    selected,
    edit,
    filters,
    pending,
    valid,
    modals,
    formModified: isFormModified(state),
    formValid: isFormValid(state),
    allUsersExpanded: allUsersExpanded(state),
    allUsersCollapsed: allUsersCollapsed(state),
    rolesModified: areRolesModified(state),
    canCreateClients: userCanCreateClients(state),
    currentUser,
    currentlyRemovingOwnAdminRole: userIsRemovingOwnClientAdminRole(state),
  };
}

export const ConnectedClientAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ClientAdmin);
