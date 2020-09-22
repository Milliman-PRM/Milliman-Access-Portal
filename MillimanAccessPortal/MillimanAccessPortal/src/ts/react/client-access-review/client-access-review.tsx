import '../../../scss/react/client-access-review/client-access-review.scss';

import * as moment from 'moment';
import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { setUnloadAlert } from '../../unload-alerts';
import { Client, ClientWithReviewDate } from '../models';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
    PanelSectionToolbar, PanelSectionToolbarButtons,
} from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import { CardSectionMain, CardText } from '../shared-components/card/card-sections';
import { ColumnSpinner } from '../shared-components/column-spinner';
import { Filter } from '../shared-components/filter';
import { NavBar } from '../shared-components/navbar';
import { ProgressIndicator } from './progress-indicator';
import * as ClientAccessReviewActionCreators from './redux/action-creators';
import { activeSelectedClient, clientEntities } from './redux/selectors';
import {
    AccessReviewState, AccessReviewStateCardAttributes, AccessReviewStateFilters, AccessReviewStateModals,
    AccessReviewStatePending, AccessReviewStateSelected, ClientAccessReviewModel, ClientAccessReviewProgress,
    ClientAccessReviewProgressEnum, ClientSummaryModel,
} from './redux/store';

type ClientEntity = (ClientWithReviewDate & { indent: 1 | 2 }) | 'divider';

interface ClientAccessReviewProps {
  clients: ClientEntity[];
  clientSummary: ClientSummaryModel;
  clientAccessReview: ClientAccessReviewModel;
  clientAccessReviewProgress: ClientAccessReviewProgress;
  selected: AccessReviewStateSelected;
  cardAttributes: AccessReviewStateCardAttributes;
  pending: AccessReviewStatePending;
  filters: AccessReviewStateFilters;
  modals: AccessReviewStateModals;
  activeSelectedClient: Client;
}

class ClientAccessReview extends React.Component<ClientAccessReviewProps & typeof ClientAccessReviewActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchGlobalData({});
    this.props.fetchClients({});
    this.props.scheduleSessionCheck({ delay: 0 });
    // TODO: Implement Unload Alert properly
    setUnloadAlert(() => false);
  }

  public render() {
    const { clientAccessReview, clientSummary, pending, selected } = this.props;
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
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending, cardAttributes } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.data.clients}
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
                this.props.selectClient({ id: entity.id });
                if (entity.id !== selected.client) {
                  this.props.fetchClientSummary({ clientId: entity.id });
                }
              }}
              indentation={entity.indent}
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
            <div id="icons" />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderClientSummaryPanel() {
    const { clientSummary, pending, selected } = this.props;
    return (
      <div className="admin-panel-container admin-panel-container flex-item-12-12 flex-item-for-tablet-up-9-12">
        {pending.data.clientSummary && <ColumnSpinner />}
        <h3 className="admin-panel-header">Client Access Review Summary</h3>
        <div className="client-summary-container">
          <div className="header">
            <div className="title">
              <span className="client-name">{clientSummary.clientName}</span>
              <span className="client-code">{clientSummary.clientCode}</span>
            </div>
          </div>
          <div className="details-container">
            <div className="detail-column">
              <div className="detail-section">
                <span className="detail-label">Review due date</span>
                <h2>{moment.utc(clientSummary.reviewDueDate).format('MMM DD, YYYY')}</h2>
              </div>
              <div className="detail-section">
                <span className="detail-label">Last review date</span>
                <span className="detail-value">
                  {moment.utc(clientSummary.lastReviewDate).format('MMM DD, YYYY')}
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
                <span className="detail-value">{clientSummary.assignedProfitCenter}</span>
              </div>
              <div className="detail-section">
                <span className="detail-label">Profit Center Admins</span>
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
    const { clientAccessReview, clientAccessReviewProgress, pending } = this.props;
    return (
      <div className="admin-panel-container admin-panel-container flex-item-12-12 flex-item-for-tablet-up-9-12">
        {pending.data.clientAccessReview && <ColumnSpinner />}
        <h3 className="admin-panel-header">Client Access Review Summary</h3>
        <div className="client-review-container">
          <div className="header">
            <div className="title">
              <span className="client-name">{clientAccessReview.clientName}</span>
              <span className="client-code">{clientAccessReview.clientCode}</span>
              <span className="client-code">Review the Client information to proceed</span>
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
                    <span className="detail-value">{clientAccessReview.clientName}</span>
                  </div>
                  <div className="detail-section">
                    <span className="detail-label">Client code</span>
                    <span className="detail-value">{clientAccessReview.clientCode}</span>
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
                    <span className="detail-value">{clientAccessReview.assignedProfitCenterName}</span>
                  </div>
                  <div className="detail-section">
                    <span className="detail-label">Profit Center Admins</span>
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
                                <span className="detail-value">{domain}</span>
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
                                    <span className="detail-value">{email}</span>
                                  </div>
                                </li>
                              );
                            })
                          }
                        </ul>
                      ) : (
                          <span className="detail-value">N/A</span>
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
                          <tr key={user.userEmail}>
                            <td>
                              <span className="detail-value-name">{user.name}</span><br />
                              <span className="detail-value-email">{user.userEmail}</span></td>
                            <td>
                              {
                                user.lastLoginDate
                                  ? moment.utc(user.lastLoginDate).format('MMM DD, YYYY')
                                  : 'n/a'
                              }
                            </td>
                            <td className="center-text">{user.clientUserRoles.Admin ? 'X' : ''}</td>
                            <td className="center-text">{user.clientUserRoles.ContentPublisher ? 'X' : ''}</td>
                            <td className="center-text">{user.clientUserRoles.ContentAccessAdmin ? 'X' : ''}</td>
                            <td className="center-text">{user.clientUserRoles.ContentUser ? 'X' : ''}</td>
                            <td className="center-text">{user.clientUserRoles.FileDropAdmin ? 'X' : ''}</td>
                            <td className="center-text">{user.clientUserRoles.FileDropUser ? 'X' : ''}</td>
                          </tr>
                        );
                      })
                    }
                  </tbody>
                </table>
              </div>
            }
            <div className="button-container">
              {
                clientAccessReviewProgress.step !== 0 &&
                <button className="link-button align-left" onClick={() => this.props.goToPreviousAccessReviewStep({})}>
                  Back
                </button>
              }
              <button className="link-button" onClick={() => this.props.cancelClientAccessReview({})}>
                Cancel
              </button>
              {
                clientAccessReviewProgress.step !== ClientAccessReviewProgressEnum.attestations ? (
                    <button className="blue-button" onClick={() => this.props.goToNextAccessReviewStep({})}>
                      Continue
                    </button>
                  ) : (
                    <button className="blue-button" onClick={() => false}>
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
}

function mapStateToProps(state: AccessReviewState): ClientAccessReviewProps {
  const { selected, cardAttributes, filters, modals, pending, data } = state;
  return {
    clients: clientEntities(state),
    clientSummary: data.selectedClientSummary,
    clientAccessReview: data.clientAccessReview,
    clientAccessReviewProgress: pending.clientAccessReviewProgress,
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
    activeSelectedClient: activeSelectedClient(state),
  };
}

export const ConnectedClientAccessReview = connect(
  mapStateToProps,
  ClientAccessReviewActionCreators,
)(ClientAccessReview);
