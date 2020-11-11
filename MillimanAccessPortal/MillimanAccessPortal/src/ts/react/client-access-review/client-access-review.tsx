import '../../../scss/react/client-access-review/client-access-review.scss';

import '../../../images/icons/checkmark.svg';

import * as moment from 'moment';
import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { setUnloadAlert } from '../../unload-alerts';
import { Client, ClientWithReviewDate } from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
    PanelSectionToolbar, PanelSectionToolbarButtons,
} from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import { CardSectionMain, CardText } from '../shared-components/card/card-sections';
import { ColumnSpinner } from '../shared-components/column-spinner';
import { Filter } from '../shared-components/filter';
import { Checkbox } from '../shared-components/form/checkbox';
import { NavBar } from '../shared-components/navbar';
import { ProgressIndicator } from './progress-indicator';
import * as ClientAccessReviewActionCreators from './redux/action-creators';
import { activeSelectedClient, clientEntities, clientSortIcon, continueButtonIsActive } from './redux/selectors';
import {
  AccessReviewGlobalData, AccessReviewState, AccessReviewStateCardAttributes, AccessReviewStateFilters,
  AccessReviewStateModals, AccessReviewStatePending, AccessReviewStateSelected, ClientAccessReviewModel,
  ClientAccessReviewProgress, ClientAccessReviewProgressEnum, ClientSummaryModel,
} from './redux/store';

type ClientEntity = (ClientWithReviewDate & { indent: 1 | 2 }) | 'divider';

interface ClientAccessReviewProps {
  clients: ClientEntity[];
  clientSummary: ClientSummaryModel;
  clientAccessReview: ClientAccessReviewModel;
  clientAccessReviewProgress: ClientAccessReviewProgress;
  clientSortIcon: 'sort-alphabetically-asc' | 'sort-alphabetically-desc' | 'sort-date-asc' | 'sort-date-desc';
  globalData: AccessReviewGlobalData;
  selected: AccessReviewStateSelected;
  cardAttributes: AccessReviewStateCardAttributes;
  pending: AccessReviewStatePending;
  filters: AccessReviewStateFilters;
  modals: AccessReviewStateModals;
  activeSelectedClient: Client;
  continueButtonActive: boolean;
}

class ClientAccessReview extends React.Component<ClientAccessReviewProps & typeof ClientAccessReviewActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');
  private clientReviewContainer: React.RefObject<HTMLDivElement>;

  public componentDidMount() {
    this.props.fetchGlobalData({});
    this.props.fetchClients({});
    this.props.scheduleSessionCheck({ delay: 0 });
    setUnloadAlert(() => this.props.clientAccessReview !== null);
    this.clientReviewContainer = React.createRef<HTMLDivElement>();
  }

  public render() {
    const { clientAccessReview, clientSummary, pending, selected, modals } = this.props;
    return (
      <>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-center"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
        <NavBar
          currentView={this.currentView}
          updateNavBarElements={this.props.pending.navBarRenderInt}
        />
        {this.renderClientPanel()}
        {
          selected.client
          && !pending.data.clientSummary
          && clientSummary
          && clientAccessReview === null
          && this.renderClientSummaryPanel()
        }
        {
          selected.client
          && !pending.data.clientAccessReview
          && clientAccessReview
          && this.renderClientAccessReviewPanel()
        }
        <Modal
          isOpen={modals.leaveActiveReview.isOpen}
          onRequestClose={() => this.props.closeLeavingActiveReviewModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Leave Client Access Review</h3>
          <span className="modal-text">
            Do you want to leave the active Client Access Review?
          </span>
          <span className="modal-text">
            All progress will be lost.
          </span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeLeavingActiveReviewModal({})}
            >
              Cancel
            </button>
            <button
              className="red-button"
              onClick={() => {
                if (pending.pendingClientSelection) {
                  this.props.selectClient({ id: pending.pendingClientSelection });
                  if (pending.pendingClientSelection !== selected.client) {
                    this.props.fetchClientSummary({ clientId: pending.pendingClientSelection });
                  }
                }
                this.props.cancelClientAccessReview({});
                this.props.closeLeavingActiveReviewModal({});
              }}
            >
              Leave Review
            </button>
          </div>
        </Modal>
      </>
    );
  }

  private renderClientPanel() {
    const {
      clients, selected, filters, globalData, pending, cardAttributes, clientAccessReview,
    } = this.props;
    const setClientSortOrderButtons = (
      <>
        <ActionIcon
          label={`Sorted by ${pending.clientSort.sortBy === 'name' ? 'Client Name' : 'Review Date'}`}
          icon={this.props.clientSortIcon}
          action={() => {
            let sortBy: 'name' | 'date';
            if (pending.clientSort.sortOrder === 'desc') {
              sortBy = pending.clientSort.sortBy === 'date' ? 'name' : 'date';
            } else {
              sortBy = pending.clientSort.sortBy;
            }
            const sortOrder = pending.clientSort.sortOrder === 'desc' ? 'asc' : 'desc';
            this.props.setSortOrder({ clientSort: { sortBy, sortOrder } });
          }}
        />
      </>
    );

    return (
      <CardPanel
        entities={clients}
        loading={pending.data.clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          const card = cardAttributes.client[entity.id];
          const daysUntilDue =
            moment.utc(entity.reviewDueDateTimeUtc).local().diff(moment(), 'days');
          const notificationType = () => {
            if (daysUntilDue < 0) {
              return 'error';
            } else if (daysUntilDue < globalData.clientReviewEarlyWarningDays) {
              return 'informational';
            } else {
              return 'message';
            }
          };
          return (
            <Card
              key={key}
              selected={selected.client === entity.id}
              disabled={card.disabled}
              onSelect={() => {
                if (clientAccessReview === null) {
                  this.props.selectClient({ id: entity.id });
                  if (entity.id !== selected.client) {
                    this.props.fetchClientSummary({ clientId: entity.id });
                  }
                } else {
                  this.props.openLeavingActiveReviewModal({ clientId: entity.id });
                }
              }}
              indentation={entity.indent}
              bannerMessage={!card.disabled &&
                (daysUntilDue < globalData.clientReviewEarlyWarningDays
                  || clientAccessReview && clientAccessReview.id === entity.id) ? {
                  level: notificationType(),
                  message: (
                    <div className="review-due-container">
                      {
                        daysUntilDue < globalData.clientReviewEarlyWarningDays &&
                        <>
                          <span className="needs-review">
                            {notificationType() === 'error' ? 'Overdue' : 'Needs Review'}:&nbsp;
                          </span>
                          Due {moment.utc(entity.reviewDueDateTimeUtc).local().format('MMM DD, YYYY')}
                        </>
                      }
                      {
                        clientAccessReview && clientAccessReview.id === entity.id &&
                        <div>
                          <span>Review in progress...</span>
                        </div>
                      }
                    </div>
                  ),
                } : null}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
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
            {setClientSortOrderButtons}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderClientSummaryPanel() {
    const { clientSummary, globalData, pending, selected } = this.props;
    const daysUntilDue =
      moment.utc(clientSummary.reviewDueDate).local().diff(moment(), 'days');
    const dueDateClass = () => {
      if (daysUntilDue < 0) {
        return 'review-overdue';
      } else if (daysUntilDue < globalData.clientReviewEarlyWarningDays) {
        return 'review-approaching';
      } else {
        return null;
      }
    };
    return (
      <div className="admin-panel-container admin-panel-container flex-item-12-12 flex-item-for-tablet-up-9-12">
        {pending.data.clientSummary && <ColumnSpinner />}
        <h3 className="admin-panel-header">Client Access Review Summary</h3>
        <div
          className={
            [
              'client-summary-container',
              dueDateClass(),
            ].join(' ')
          }
        >
          <div className="header">
            <div className="title">
              <div className="title-container">
                <span className="client-name">{clientSummary.clientName}</span>
                <span className="client-code">{clientSummary.clientCode}</span>
              </div>
              {
                dueDateClass() !== null ? (
                  <ActionIcon icon="error" />
                ) : null
              }
            </div>
          </div>
          <div className="details-container">
            <div className="detail-column">
              <div className="detail-section">
                <span className="detail-label">Review due date</span>
                <h2>{moment.utc(clientSummary.reviewDueDate).local().format('MMM DD, YYYY')}</h2>
              </div>
              <div className="detail-section">
                <span className="detail-label">Last review date</span>
                <span className="detail-value-name">
                  {moment.utc(clientSummary.lastReviewDate).local().format('MMM DD, YYYY')}
                </span>
              </div>
              <div className="detail-section">
                <span className="detail-label">Last review by</span>
                <span className="detail-value-name">{clientSummary.lastReviewedBy.name}</span>
                <span className="detail-value-email">{clientSummary.lastReviewedBy.userEmail}</span>
              </div>
            </div>
            <div className="detail-column">
              <div className="detail-section">
                <span className="detail-label">Primary contact</span>
                <span className="detail-value-name">{clientSummary.primaryContactName}</span>
                <span className="detail-value-email">
                  {clientSummary.primaryContactEmail ? clientSummary.primaryContactEmail : '(None assigned)'}
                </span>
              </div>
              <div className="detail-section">
                <span className="detail-label">Client Admins</span>
                <ul className="detail-list">
                  {
                    clientSummary.clientAdmins.map((admin) => {
                      return (
                        <li className="detail-list-item" key={admin.userEmail}>
                          <div className="list-container">
                            <span className="detail-value-name">{admin.name}</span>
                            <span className="detail-value-email">{admin.userEmail}</span>
                          </div>
                        </li>
                      );
                    })
                  }
                </ul>
              </div>
            </div>
            <div className="detail-column">
              <div className="detail-section">
                <span className="detail-label">Profit center</span>
                <span className="detail-value-name">{clientSummary.assignedProfitCenter}</span>
              </div>
              <div className="detail-section">
                <span className="detail-label">
                  Profit Center Admins
                  <ActionIcon
                    icon="information"
                    label="Profit Center Admins are users authorized to create new Clients for the Profit Center"
                  />
                </span>
                <ul className="detail-list">
                  {
                    clientSummary.profitCenterAdmins.map((admin) => {
                      return (
                        <li className="detail-list-item" key={admin.userEmail}>
                          <div className="list-container">
                            <span className="detail-value-name">{admin.name}</span>
                            <span className="detail-value-email">{admin.userEmail}</span>
                          </div>
                        </li>
                      );
                    })
                  }
                </ul>
              </div>
            </div>
          </div>
        </div>
        <div className="button-container">
          <button
            className="blue-button"
            onClick={() => this.props.fetchClientReview({ clientId: selected.client })}
          >
            Begin Review
          </button>
        </div>
      </div>
    );
  }

  private renderClientAccessReviewPanel() {
    const { clientAccessReview, clientAccessReviewProgress, continueButtonActive, pending } = this.props;
    const reviewDescription = () => {
      switch (clientAccessReviewProgress.step) {
        case ClientAccessReviewProgressEnum.clientReview:
          return 'Review the Client information to proceed';
        case ClientAccessReviewProgressEnum.userRoles:
          return 'Review the User information to proceed';
        case ClientAccessReviewProgressEnum.contentAccess:
          return 'Review content access information to proceed';
        case ClientAccessReviewProgressEnum.fileDropAccess:
          return 'Review File Drop access information to proceed';
        case ClientAccessReviewProgressEnum.attestations:
          return 'Attest to the Client information to complete the review';
        default:
          return '';
      }
    };
    return (
      <div className="admin-panel-container admin-panel-container flex-item-12-12 flex-item-for-tablet-up-9-12">
        {pending.data.clientAccessReview && <ColumnSpinner />}
        <h3 className="admin-panel-header">Client Access Review Summary</h3>
        <div className="client-review-container" ref={this.clientReviewContainer}>
          <div className="header">
            <div className="title">
              <div className="title-container">
                <span className="client-name">{clientAccessReview.clientName}</span>
                <span className="client-code">{clientAccessReview.clientCode}</span>
                <span className="client-code">{reviewDescription()}</span>
              </div>
              <ProgressIndicator
                progressObjects={{
                  [ClientAccessReviewProgressEnum.clientReview]: {
                    label: 'Client Review',
                  },
                  [ClientAccessReviewProgressEnum.userRoles]: {
                    label: 'User Roles',
                  },
                  [ClientAccessReviewProgressEnum.contentAccess]: {
                    label: 'Content Access',
                  },
                  [ClientAccessReviewProgressEnum.fileDropAccess]: {
                    label: 'File Drop Access',
                  },
                  [ClientAccessReviewProgressEnum.attestations]: {
                    label: 'Attestations',
                  },
                }}
                currentStep={clientAccessReviewProgress.step}
              />
            </div>
            {
              clientAccessReviewProgress.step === ClientAccessReviewProgressEnum.clientReview &&
              <div className="details-container">
                <div className="detail-column">
                  <div className="detail-section">
                    <span className="detail-label">Client name</span>
                    <span className="detail-value-name">{clientAccessReview.clientName}</span>
                  </div>
                  <div className="detail-section">
                    <span className="detail-label">Client code</span>
                    <span className="detail-value-name">{clientAccessReview.clientCode}</span>
                  </div>
                  <div className="detail-section">
                    <span className="detail-label">Client Admins</span>
                    <ul className="detail-list">
                      {
                        clientAccessReview.clientAdmins.map((admin) => {
                          return (
                            <li className="detail-list-item" key={admin.userEmail}>
                              <div className="list-container">
                                <span className="detail-value-name">{admin.name}</span>
                                <span className="detail-value-email">{admin.userEmail}</span>
                              </div>
                            </li>
                          );
                        })
                      }
                    </ul>
                  </div>
                </div>
                <div className="detail-column">
                  <div className="detail-section">
                    <span className="detail-label">Profit Center</span>
                    <span className="detail-value-name">{clientAccessReview.assignedProfitCenterName}</span>
                  </div>
                  <div className="detail-section">
                    <span className="detail-label">
                      Profit Center Admins
                      <ActionIcon
                        icon="information"
                        label="Profit Center Admins are users authorized to create new Clients for the Profit Center"
                      />
                    </span>
                    <ul className="detail-list">
                      {
                        clientAccessReview.profitCenterAdmins.map((admin) => {
                          return (
                            <li className="detail-list-item" key={admin.userEmail}>
                              <div className="list-container">
                                <span className="detail-value-name">{admin.name}</span>
                                <span className="detail-value-email">{admin.userEmail}</span>
                              </div>
                            </li>
                          );
                        })
                      }
                    </ul>
                  </div>
                </div>
                <div className="detail-column">
                  <div className="detail-section">
                    <span className="detail-label">Approved email domain list</span>
                    <ul className="detail-list">
                      {
                        clientAccessReview.approvedEmailDomainList.map((domain, index) => {
                          return (
                            <li className="detail-list-item" key={index}>
                              <div className="list-container">
                                <span className="detail-value-name">{domain}</span>
                              </div>
                            </li>
                          );
                        })
                      }
                    </ul>
                  </div>
                  <div className="detail-section">
                    <span className="detail-label">Email address exception list</span>
                    {
                      clientAccessReview.approvedEmailExceptionList.length > 0 ? (
                        <ul className="detail-list">
                          {
                            clientAccessReview.approvedEmailExceptionList.map((email, index) => {
                              return (
                                <li className="detail-list-item" key={index}>
                                  <div className="list-container">
                                    <span className="detail-value-name">{email}</span>
                                  </div>
                                </li>
                              );
                            })
                          }
                        </ul>
                      ) : (
                          <span className="detail-value-name">N/A</span>
                        )
                    }
                  </div>
                </div>
              </div>
            }
            {
              clientAccessReviewProgress.step === ClientAccessReviewProgressEnum.userRoles &&
              <div className="details-container">
                <table className="access-review-table">
                  <thead>
                    <tr>
                      <th colSpan={2} />
                      <th colSpan={6} className="center-text header-cell">Roles</th>
                    </tr>
                    <tr>
                      <th>User Name<br />Email</th>
                      <th>Last Login</th>
                      <th className="role-column center-text">Client Admin</th>
                      <th className="role-column center-text">Content Publisher</th>
                      <th className="role-column center-text">Content Access Admin</th>
                      <th className="role-column center-text">Content User</th>
                      <th className="role-column center-text">File Drop Admin</th>
                      <th className="role-column center-text">File Drop User</th>
                    </tr>
                  </thead>
                  <tbody>
                    {
                      clientAccessReview.memberUsers.map((user) => {
                        return (
                          <tr key={user.userEmail} className="table-row-divider">
                            <td>
                              <span className="detail-value-name">{user.name ? user.name : 'n/a'}</span><br />
                              <span className="detail-value-email">{user.userEmail}</span></td>
                            <td>
                              {
                                user.lastLoginDate
                                  ? moment.utc(user.lastLoginDate).local().format('MMM DD, YYYY')
                                  : 'n/a'
                              }
                            </td>
                            <td className="center-text">
                              {user.clientUserRoles.Admin ? this.renderCheckmark() : ''}
                            </td>
                            <td className="center-text">
                              {user.clientUserRoles.ContentPublisher ? this.renderCheckmark() : ''}
                            </td>
                            <td className="center-text">
                              {user.clientUserRoles.ContentAccessAdmin ? this.renderCheckmark() : ''}
                            </td>
                            <td className="center-text">
                              {user.clientUserRoles.ContentUser ? this.renderCheckmark() : ''}
                            </td>
                            <td className="center-text">
                              {user.clientUserRoles.FileDropAdmin ? this.renderCheckmark() : ''}
                            </td>
                            <td className="center-text">
                              {user.clientUserRoles.FileDropUser ? this.renderCheckmark() : ''}
                            </td>
                          </tr>
                        );
                      })
                    }
                  </tbody>
                </table>
              </div>
            }
            {
              clientAccessReviewProgress.step === ClientAccessReviewProgressEnum.contentAccess &&
              clientAccessReview.contentItems.length ? (
                clientAccessReview.contentItems.map((ci) => {
                  return (
                    <div className="details-container" key={ci.id}>
                      <span className="detail-title">{ci.contentItemName}</span>
                      <table className="access-review-table">
                        <thead>
                          <tr>
                            <th>Selection Group</th>
                            <th>User</th>
                            <th>Email</th>
                          </tr>
                        </thead>
                        <tbody>
                          {
                            ci.selectionGroups.map((sg) => {
                              return (
                                <React.Fragment key={sg.selectionGroupName}>
                                  {
                                    sg.authorizedUsers.map((user, index) => {
                                      return (
                                        <tr
                                          key={user.userEmail}
                                          className={
                                            index === sg.authorizedUsers.length - 1 ? 'table-row-divider' : null
                                          }
                                        >
                                          {
                                            index === 0 ? (
                                              <td rowSpan={sg.authorizedUsers.length} className="table-row-divider">
                                                {sg.selectionGroupName}
                                              </td>
                                            ) : null
                                          }
                                          <td>{user.name}</td>
                                          <td>{user.userEmail}</td>
                                        </tr>
                                      );
                                    })
                                  }
                                </React.Fragment>
                              );
                            })
                          }
                        </tbody>
                      </table>
                      <Checkbox
                        name={`'${ci.contentItemName}' Selection Groups are as expected`}
                        selected={clientAccessReviewProgress.contentItemConfirmations[ci.id]}
                        onChange={() => this.props.toggleContentItemReviewStatus({ contentItemId: ci.id })}
                        readOnly={false}
                      />
                    </div>
                  );
                })
              ) : null
            }
            {
              clientAccessReviewProgress.step === ClientAccessReviewProgressEnum.contentAccess &&
              clientAccessReview.contentItems.length === 0 &&
              <span className="content-message">No Content Items</span>
            }
            {
              clientAccessReviewProgress.step === ClientAccessReviewProgressEnum.fileDropAccess &&
                clientAccessReview.fileDrops.length ? (
                  clientAccessReview.fileDrops.map((fd) => {
                    return (
                      <div className="details-container" key={fd.id}>
                        <span className="detail-title">{fd.fileDropName}</span>
                        <table className="access-review-table">
                          <thead>
                            <tr>
                              <th colSpan={2} />
                              <th colSpan={3} className="center-text header-cell">Permissions</th>
                            </tr>
                            <tr>
                              <th className="name-column">Name</th>
                              <th className="email-column">Email</th>
                              <th className="permission-column center-text">Download</th>
                              <th className="permission-column center-text">Upload</th>
                              <th className="permission-column center-text">Delete</th>
                            </tr>
                          </thead>
                          <tbody>
                            {
                              fd.permissionGroups.map((pg) => {
                                if (pg.isPersonalGroup) {
                                  return (
                                    <tr className="table-row-divider" key={pg.permissionGroupName}>
                                      <td className="detail-value-name">
                                        {
                                          pg.authorizedMapUsers.length > 0 ?
                                            pg.authorizedMapUsers[0].name :
                                            pg.authorizedServiceAccounts[0].name
                                        }
                                      </td>
                                      <td>
                                        {
                                          pg.authorizedMapUsers.length > 0 ?
                                            pg.authorizedMapUsers[0].userEmail :
                                            pg.authorizedServiceAccounts[0].userEmail
                                        }
                                      </td>
                                      <td className="center-text">
                                        {pg.permissions.Read ? this.renderCheckmark() : null}
                                      </td>
                                      <td className="center-text">
                                        {pg.permissions.Write ? this.renderCheckmark() : null}
                                      </td>
                                      <td className="center-text">
                                        {pg.permissions.Delete ? this.renderCheckmark() : null}
                                      </td>
                                    </tr>
                                  );
                                } else {
                                  const authUsers =
                                    pg.authorizedMapUsers.length + pg.authorizedServiceAccounts.length;
                                  return (
                                    <React.Fragment key={pg.permissionGroupName}>
                                      <tr className={authUsers === 0 ? 'table-row-divider' : null}>
                                        <td
                                          className="detail-value-name"
                                        >
                                          {pg.permissionGroupName}
                                        </td>
                                        <td className={`${authUsers === 0 ? 'table-row-divider' : null}`} />
                                        <td
                                          className={
                                            [
                                              'center-text',
                                              authUsers === 0 ? 'table-row-divider' : null,
                                            ].join(' ')
                                          }
                                        >
                                          {pg.permissions.Read ? this.renderCheckmark() : null}
                                        </td>
                                        <td
                                          className={
                                            [
                                              'center-text',
                                              authUsers === 0 ? 'table-row-divider' : null,
                                            ].join(' ')
                                          }
                                        >
                                          {pg.permissions.Write ? this.renderCheckmark() : null}
                                        </td>
                                        <td
                                          className={
                                            [
                                              'center-text',
                                              authUsers === 0 ? 'table-row-divider' : null,
                                            ].join(' ')
                                          }
                                        >
                                          {pg.permissions.Delete ? this.renderCheckmark() : null}
                                        </td>
                                      </tr>
                                      {
                                        pg.authorizedMapUsers.map((user, index) => {
                                          return (
                                            <tr
                                              key={user.userEmail}
                                              className={
                                                index === authUsers - 1 ? 'table-row-divider' : null
                                              }
                                            >
                                              <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{user.name}</td>
                                              <td>{user.userEmail}</td>
                                              {
                                                index + 1 === authUsers &&
                                                <>
                                                  <td className="table-row-divider" />
                                                  <td className="table-row-divider" />
                                                  <td className="table-row-divider" />
                                                </>
                                              }
                                            </tr>
                                          );
                                        })
                                      }
                                    </React.Fragment>
                                  );
                                }
                              })
                            }
                          </tbody>
                        </table>
                        <Checkbox
                          name={`'${fd.fileDropName}' Permission Groups are as expected`}
                          selected={clientAccessReviewProgress.fileDropConfirmations[fd.id]}
                          onChange={() => this.props.toggleFileDropReviewStatus({ fileDropId: fd.id })}
                          readOnly={false}
                        />
                      </div>
                    );
                  })
                ) : null
            }
            {
              clientAccessReviewProgress.step === ClientAccessReviewProgressEnum.fileDropAccess &&
              clientAccessReview.fileDrops.length === 0 &&
              <span className="content-message">No File Drops</span>
            }
            {
              clientAccessReviewProgress.step === ClientAccessReviewProgressEnum.attestations &&
              clientAccessReview.attestationLanguage &&
              <div
                className="attestation-block"
                dangerouslySetInnerHTML={{
                  __html: clientAccessReview.attestationLanguage,
                }}
              />
            }
            <div className="button-container">
              {
                clientAccessReviewProgress.step !== 0 &&
                <button
                  className="link-button align-left"
                  onClick={() => {
                    this.props.goToPreviousAccessReviewStep({});
                    this.clientReviewContainer.current.scrollTo(0, 0);
                  }}
                >
                  Back
                </button>
              }
              <button
                className="link-button"
                onClick={() => {
                  this.props.openLeavingActiveReviewModal({ clientId: null });
                }}
              >
                Cancel
              </button>
              {
                clientAccessReviewProgress.step !== ClientAccessReviewProgressEnum.attestations ? (
                  <button
                    className="blue-button"
                    onClick={() => {
                      this.props.goToNextAccessReviewStep({});
                      this.clientReviewContainer.current.scrollTo(0, 0);
                    }}
                    disabled={!continueButtonActive}
                  >
                    Continue
                  </button>
                  ) : (
                    <button
                      className="blue-button"
                      onClick={() => {
                        this.props.approveClientAccessReview({
                          clientId: clientAccessReview.id,
                          reviewId: clientAccessReview.clientAccessReviewId,
                        });
                      }}
                    >
                      Complete Review
                    </button>
                  )
              }
            </div>
          </div>
        </div>
      </div>
    );
  }

  private renderCheckmark() {
    return (
      <svg className="checkmark">
        <use xlinkHref={'#checkmark'} />
      </svg>
    );
  }
}

function mapStateToProps(state: AccessReviewState): ClientAccessReviewProps {
  const { selected, cardAttributes, filters, modals, pending, data } = state;
  return {
    clients: clientEntities(state),
    clientSummary: data.selectedClientSummary,
    clientAccessReview: data.clientAccessReview,
    clientAccessReviewProgress: pending.clientAccessReviewProgress,
    clientSortIcon: clientSortIcon(state),
    globalData: data.globalData,
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
    activeSelectedClient: activeSelectedClient(state),
    continueButtonActive: continueButtonIsActive(state),
  };
}

export const ConnectedClientAccessReview = connect(
  mapStateToProps,
  ClientAccessReviewActionCreators,
)(ClientAccessReview);
