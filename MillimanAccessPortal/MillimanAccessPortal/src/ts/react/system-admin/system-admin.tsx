import '../../../images/icons/add.svg';
import '../../../images/icons/client.svg';
import '../../../images/icons/email.svg';
import '../../../images/icons/expand-card.svg';
import '../../../images/icons/group.svg';
import '../../../images/icons/reports.svg';
import '../../../images/icons/user.svg';
import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { getJsonData, postData } from '../../shared';
import { BasicNode } from '../../view-models/content-publishing';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
    PanelSectionToolbar, PanelSectionToolbarButtons,
} from '../shared-components/card-panel/panel-sections';
import { Card, CardAttributes } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import { CardExpansion } from '../shared-components/card/card-expansion';
import {
    CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { ColumnIndicator, ColumnSelector } from '../shared-components/column-selector';
import { EntityHelper } from '../shared-components/entity';
import { Filter } from '../shared-components/filter';
import { Guid, QueryFilter, RoleEnum } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import {
    ClientInfo, ClientInfoWithDepth, EntityInfo, EntityInfoCollection, isClientDetail,
    isClientInfo, isClientInfoTree, isProfitCenterInfo, isRootContentItemDetail, isRootContentItemInfo,
    isUserClientRoles, isUserDetail, isUserInfo, PrimaryDetail, PrimaryDetailData, SecondaryDetail,
    SecondaryDetailData, UserClientRoles, UserInfo,
} from './interfaces';
import { AddUserToProfitCenterModal } from './modals/add-user-to-profit-center';
import { CardModal } from './modals/card-modal';
import { ChangeSystemAdminStatusModal } from './modals/change-system-admin-status-modal';
import { CreateProfitCenterModal } from './modals/create-profit-center';
import { CreateUserModal } from './modals/create-user';
import { RemoveProfitCenterModals } from './modals/remove-profit-center';
import { RemoveUserFromProfitCenterModal } from './modals/remove-user-from-profit-center';
import { SetDomainLimitClientModal } from './modals/set-domain-limit';
import { PrimaryDetailPanel } from './primary-detail-panel';
import { SecondaryDetailPanel } from './secondary-detail-panel';

export interface ContentPanelAttributes {
  selected: {
    column: SystemAdminColumn;
    card: string;
  };
  cards: {
    [id: string]: CardAttributes;
  };
  filter: {
    text: string;
  };
  createModal: {
    open: boolean;
  };
}
export interface SystemAdminState {
  data: {
    primaryEntities: EntityInfoCollection;
    secondaryEntities: EntityInfoCollection;
    primaryDetail: PrimaryDetailData;
    secondaryDetail: SecondaryDetailData;
  };
  primaryPanel: ContentPanelAttributes;
  secondaryPanel: ContentPanelAttributes;
  domainLimitModal: {
    open: boolean;
  };
  profitCenterModal: {
    open: boolean;
    action: string;
  };
  systemAdminModal: {
    open: boolean;
    enabled: boolean;
  };
  removeProfitCenterModal: {
    open: boolean;
    profitCenterId: Guid;
    profitCenterName: string;
  };
}

export enum SystemAdminColumn {
  USER = 'user',
  CLIENT = 'client',
  PROFIT_CENTER = 'profitCenter',
  ROOT_CONTENT_ITEM = 'rootContentItem',
}

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public constructor(props: {}) {
    super(props);

    this.state = {
      data: {
        primaryEntities: null,
        secondaryEntities: null,
        primaryDetail: null,
        secondaryDetail: null,
      },
      primaryPanel: {
        selected: {
          column: null,
          card: null,
        },
        cards: null,
        filter: {
          text: '',
        },
        createModal: {
          open: false,
        },
      },
      secondaryPanel: {
        selected: {
          column: null,
          card: null,
        },
        cards: null,
        filter: {
          text: '',
        },
        createModal: {
          open: false,
        },
      },
      domainLimitModal: {
        open: false,
      },
      profitCenterModal: {
        open: false,
        action: '',
      },
      systemAdminModal: {
        open: false,
        enabled: false,
      },
      removeProfitCenterModal: {
        open: false,
        profitCenterId: null,
        profitCenterName: null,
      },
    };
  }

  public componentDidUpdate() {
    const { primaryEntities, secondaryEntities, primaryDetail, secondaryDetail } = this.state.data;
    const { card: primaryCard } = this.state.primaryPanel.selected;
    const { column: secondaryColumn, card: secondaryCard } = this.state.secondaryPanel.selected;
    if (primaryEntities === null) {
      this.fetchPrimaryEntities();
    }
    if (primaryCard) {
      if (primaryDetail === null) {
        this.fetchPrimaryDetail();
      } else {
        if (isUserDetail(primaryDetail) && primaryDetail.isSuspended === null) {
          this.fetchSystemAdmin();
          this.fetchSuspendUser();
        }
      }
    }
    if (secondaryEntities === null && secondaryColumn) {
      this.fetchSecondaryEntities();
    }
    if (secondaryCard) {
      if (secondaryDetail === null) {
        this.fetchSecondaryDetail();
      } else {
        if (isUserClientRoles(secondaryDetail) && secondaryDetail.isClientAdmin === null) {
          this.fetchUserClient(RoleEnum.Admin);
          this.fetchUserClient(RoleEnum.ContentPublisher);
          this.fetchUserClient(RoleEnum.ContentAccessAdmin);
          this.fetchUserClient(RoleEnum.ContentUser);
          this.fetchUserClient(RoleEnum.FileDropAdmin);
          this.fetchUserClient(RoleEnum.FileDropUser);
        }
        if (isRootContentItemDetail(secondaryDetail) && secondaryDetail.isSuspended === null) {
          this.fetchSuspendContent();
        }
      }
    }
  }

  public render() {
    const { primaryEntities, secondaryEntities, primaryDetail, secondaryDetail } = this.state.data;
    const { column: primaryColumn, card: primaryCard } = this.state.primaryPanel.selected;
    const { column: secondaryColumn, card: secondaryCard } = this.state.secondaryPanel.selected;

    const pEntities = this.filterEntities(primaryEntities, this.state.primaryPanel.filter.text);
    const sEntities = this.filterEntities(secondaryEntities, this.state.secondaryPanel.filter.text);

    const secondaryQueryFilter = this.getSecondaryQueryFilter();
    const secondaryColumnComponent = secondaryColumn
      ? (
        <CardPanel
          entities={sEntities}
          renderEntity={(entity, key) => {
            let cardContents: JSX.Element = null;
            if (primaryColumn === SystemAdminColumn.USER) {
              if (isClientInfo(entity)) {
                cardContents = (
                  <CardSectionMain>
                    <CardText text={entity.name} subtext={entity.code} />
                    <CardSectionStats>
                      <CardStat
                        name={'Content items'}
                        value={entity.rootContentItemCount}
                        icon={'reports'}
                      />
                    </CardSectionStats>
                  </CardSectionMain>
                );
              } else if (isRootContentItemInfo(entity)) {
                cardContents = (
                  <CardSectionMain>
                    <CardText text={entity.name} subtext={entity.clientName} />
                  </CardSectionMain>
                );
              }
            } else if (primaryColumn === SystemAdminColumn.CLIENT) {
              if (isUserInfo(entity)) {
                cardContents = (
                  <>
                    <CardSectionMain>
                      <CardText
                        text={normalizeName(entity)}
                        subtext={entity.userName}
                      />
                      <CardSectionStats>
                        <CardStat
                          name={'Content items'}
                          value={entity.rootContentItemCount}
                          icon={'reports'}
                        />
                      </CardSectionStats>
                      <CardSectionButtons>
                        <CardButton
                          color={'blue'}
                          tooltip={entity.activated ? 'Send password reset email' : 'Resend account activation email'}
                          onClick={() => this.handleSendReset(entity.email)}
                          icon={'email'}
                        />
                      </CardSectionButtons>
                    </CardSectionMain>
                    {entity.rootContentItems && entity.rootContentItems.length
                      ? (
                        <CardExpansion
                          label={'Content Items'}
                          expanded={this.state.secondaryPanel.cards[entity.id].expanded}
                          setExpanded={() => this.handleSecondaryExpandedToggled(entity.id)}
                        >
                          <ul className="detail-item-user-list">
                            {entity.rootContentItems.map((i) => (
                              <li key={i.id}>
                                <span className="detail-item-user">
                                  <div className="detail-item-user-icon">
                                    <svg
                                      className="card-user-icon"
                                      style={{
                                        width: '2em',
                                        height: '2em',
                                      }}
                                    >
                                      <use xlinkHref="#reports" />
                                    </svg>
                                  </div>
                                  <div className="detail-item-user-name">
                                    <h4>{i.name}</h4>
                                  </div>
                                </span>
                              </li>
                            ))}
                          </ul>
                        </CardExpansion>
                      )
                      : null
                    }
                  </>
                );
              } else if (isRootContentItemInfo(entity)) {
                cardContents = (
                  <>
                    <CardSectionMain>
                      <CardText text={entity.name} subtext={entity.clientName} />
                      <CardSectionStats>
                        <CardStat
                          name={'Users with access'}
                          value={entity.userCount}
                          icon={'user'}
                        />
                        <CardStat
                          name={'Selection groups'}
                          value={entity.selectionGroupCount}
                          icon={'group'}
                        />
                      </CardSectionStats>
                    </CardSectionMain>
                    {entity.users && entity.users.length
                      ? (
                        <CardExpansion
                          label={'Members'}
                          expanded={this.state.secondaryPanel.cards[entity.id].expanded}
                          setExpanded={() => this.handleSecondaryExpandedToggled(entity.id)}
                        >
                          <ul className="detail-item-user-list">
                            {entity.users.map((u) => (
                              <li key={u.id}>
                                <span className="detail-item-user">
                                  <div className="detail-item-user-icon">
                                    <svg
                                      className="card-user-icon"
                                      style={{
                                        width: '2em',
                                        height: '2em',
                                      }}
                                    >
                                      <use xlinkHref="#user" />
                                    </svg>
                                  </div>
                                  <div className="detail-item-user-name">
                                    <h4>{normalizeName(u)}</h4>
                                    <span>{u.userName}</span>
                                  </div>
                                </span>
                              </li>
                            ))}
                          </ul>
                        </CardExpansion>
                      )
                      : null
                    }
                  </>
                );
              }
            } else if (primaryColumn === SystemAdminColumn.PROFIT_CENTER) {
              if (isUserInfo(entity)) {
                cardContents = (
                  <CardSectionMain>
                    <CardText
                      text={normalizeName(entity)}
                      subtext={entity.userName}
                    />
                    <CardSectionStats>
                      <CardStat
                        name={'Clients'}
                        value={entity.clientCount}
                        icon={'client'}
                      />
                    </CardSectionStats>
                    <CardSectionButtons>
                      <CardButton
                        color={'blue'}
                        tooltip={entity.activated ? 'Send password reset email' : 'Resend account activation email'}
                        onClick={() => this.handleSendReset(entity.email)}
                        icon={'email'}
                      />
                      <CardButton
                        color={'red'}
                        tooltip="Remove from profit center"
                        onClick={() => {
                          this.handleSecondaryModalOpen();
                          this.handleSecondaryCardSelected(entity.id);
                          this.setState({
                            profitCenterModal: {
                              open: true,
                              action: 'remove',
                            },
                          });
                        }}
                        icon={'remove-circle'}
                      />
                    </CardSectionButtons>
                  </CardSectionMain>
                );
              } else if (isClientInfo(entity)) {
                cardContents = (
                  <CardSectionMain>
                    <CardText text={entity.name} subtext={entity.code} />
                  </CardSectionMain>
                );
              }
            }
            return (
              <Card
                key={key}
                selected={secondaryCard === entity.id}
                onSelect={() => this.handleSecondaryCardSelected(entity.id)}
              >
                {cardContents}
              </Card>
            );
          }}
        >
          <ColumnSelector
            columns={this.getColumns(primaryColumn)}
            onColumnSelect={this.handleSecondaryColumnSelected}
            selectedColumn={this.getColumns(primaryColumn).filter((c) => c.id === secondaryColumn)[0]}
          />
          <PanelSectionToolbar>
            <Filter
              placeholderText={'Filter...'}
              setFilterText={this.handleSecondaryFilterKeyup}
              filterText={this.state.secondaryPanel.filter.text}
            />
            <PanelSectionToolbarButtons>
              {this.state.secondaryPanel.selected.column === SystemAdminColumn.USER
              ? (
                this.state.primaryPanel.selected.column === SystemAdminColumn.PROFIT_CENTER
                ? (
                  <ActionIcon
                    label="Add or create authorized profit center user"
                    icon="add"
                    action={() => {
                      this.handleSecondaryModalOpen();
                      this.setState({
                        profitCenterModal: {
                          open: true,
                          action: 'add',
                        },
                      });
                    }}
                  />
                ) : null
              )
              : null
              }
            </PanelSectionToolbarButtons>
          </PanelSectionToolbar>
          <ChangeSystemAdminStatusModal
            isOpen={this.state.primaryPanel.selected.column === SystemAdminColumn.USER
              && this.state.secondaryPanel.createModal.open && this.state.systemAdminModal.open}
            onRequestClose={this.handleSecondaryModalClose}
            ariaHideApp={false}
            className="modal"
            overlayClassName="modal-overlay"
            userId={this.state.primaryPanel.selected.card}
            value={this.state.systemAdminModal.enabled}
            callback={(response: boolean) => {
              alert(response ? 'System admin enabled for user.' : 'System admin disabled for user.');
              this.setState((prevState) => ({
                ...prevState,
                data: {
                  ...prevState.data,
                  primaryDetail: {
                    ...prevState.data.primaryDetail,
                    isSystemAdmin: response,
                  },
                },
              }));
            }}
          />
          <AddUserToProfitCenterModal
            isOpen={this.state.primaryPanel.selected.column === SystemAdminColumn.PROFIT_CENTER
              && this.state.secondaryPanel.createModal.open
              && this.state.profitCenterModal.open
              && this.state.profitCenterModal.action === 'add'}
            onRequestClose={this.handleSecondaryModalClose}
            ariaHideApp={false}
            className="modal"
            overlayClassName="modal-overlay"
            profitCenterId={this.state.primaryPanel.selected.card}
          />
          <RemoveUserFromProfitCenterModal
            isOpen={this.state.primaryPanel.selected.column === SystemAdminColumn.PROFIT_CENTER
              && this.state.secondaryPanel.createModal.open
              && this.state.profitCenterModal.open
              && this.state.profitCenterModal.action === 'remove'}
            onRequestClose={this.handleSecondaryModalClose}
            ariaHideApp={false}
            className="modal"
            overlayClassName="modal-overlay"
            profitCenterId={this.state.primaryPanel.selected.card}
            userId={this.state.secondaryPanel.selected.card}
          />
          {
            this.state.primaryPanel.selected.column === SystemAdminColumn.CLIENT
            && primaryDetail
            && (
              <SetDomainLimitClientModal
                isOpen={this.state.domainLimitModal.open}
                onRequestClose={this.handleDomainLimitClose}
                ariaHideApp={false}
                className="modal"
                overlayClassName="modal-overlay"
                clientId={primaryDetail.id}
                existingDomainLimit={isClientDetail(primaryDetail) && primaryDetail.domainListCountLimit}
              />
            )
          }
        </CardPanel>
      )
      : null;
    return (
      <>
        <NavBar
          currentView={this.currentView}
        />
        <CardPanel
          entities={pEntities}
          renderEntity={(entity, key) => {
            let cardContents: JSX.Element = null;
            if (isUserInfo(entity)) {
              cardContents = (
                <>
                  <CardText
                    text={normalizeName(entity)}
                    subtext={entity.userName}
                  />
                  <CardSectionStats>
                    <CardStat
                      name={'Clients'}
                      value={entity.clientCount}
                      icon={'client'}
                    />
                    <CardStat
                      name={'Content items'}
                      value={entity.rootContentItemCount}
                      icon={'reports'}
                    />
                  </CardSectionStats>
                  <CardSectionButtons>
                    <CardButton
                      color={'blue'}
                      tooltip={entity.activated ? 'Send password reset email' : 'Resend account activation email'}
                      onClick={() => this.handleSendReset(entity.email)}
                      icon={'email'}
                    />
                  </CardSectionButtons>
                </>
              );
            } else if (isClientInfo(entity)) {
              cardContents = (
                <>
                  <CardText text={entity.name} subtext={entity.code} />
                  <CardSectionStats>
                    <CardStat
                      name={'Client users'}
                      value={entity.userCount}
                      icon={'user'}
                    />
                    <CardStat
                      name={'Content items'}
                      value={entity.rootContentItemCount}
                      icon={'reports'}
                    />
                  </CardSectionStats>
                </>
              );
            } else if (isProfitCenterInfo(entity)) {
              cardContents = (
                <>
                  <CardText text={entity.name} subtext={entity.code} />
                  <CardSectionStats>
                    <CardStat
                      name={'Authorized users'}
                      value={entity.userCount}
                      icon={'user'}
                    />
                    <CardStat
                      name={'Clients'}
                      value={entity.clientCount}
                      icon={'client'}
                    />
                  </CardSectionStats>
                  <CardSectionButtons>
                    <CardButton
                      color={'red'}
                      tooltip="Delete profit center"
                      onClick={() => this.handleRemoveProfitCenterModalOpen(entity.id, entity.name)}
                      icon={'delete'}
                    />
                    <CardButton
                      color={'blue'}
                      tooltip="Edit profit center"
                      onClick={() => this.handleProfitCenterModalOpen(entity.id)}
                      icon={'edit'}
                    />
                  </CardSectionButtons>
                  <div onClick={(event) => event.stopPropagation()}>
                    <CardModal
                      isOpen={this.state.primaryPanel.cards[entity.id].profitCenterModalOpen}
                      onRequestClose={() => this.handleProfitCenterModalClose(entity.id)}
                      ariaHideApp={false}
                      className="modal"
                      overlayClassName="modal-overlay"
                      profitCenterId={entity.id}
                    />
                  </div>
                </>
              );
            }
            const indentation = isClientInfo(entity)
              ? (entity as ClientInfoWithDepth).depth
              : 1;
            return (
              <Card
                key={key}
                selected={primaryCard === entity.id}
                onSelect={() => this.handlePrimaryCardSelected(entity.id)}
                indentation={indentation}
              >
                <CardSectionMain>
                  {cardContents}
                </CardSectionMain>
              </Card>
            );
          }}
        >
          <ColumnSelector
            columns={this.getColumns()}
            onColumnSelect={this.handlePrimaryColumnSelected}
            selectedColumn={this.getColumns().filter((c) => c.id === primaryColumn)[0]}
          />
          <PanelSectionToolbar>
            <Filter
              placeholderText={'Filter...'}
              setFilterText={this.handlePrimaryFilterKeyup}
              filterText={this.state.primaryPanel.filter.text}
            />
            <PanelSectionToolbarButtons>
              {this.state.primaryPanel.selected.column === SystemAdminColumn.USER
              ? (
                <ActionIcon
                  label="Create user"
                  icon="add"
                  action={this.handlePrimaryModalOpen}
                />
              )
              : this.state.primaryPanel.selected.column === SystemAdminColumn.PROFIT_CENTER
              ? (
                <ActionIcon
                  label="Create profit center"
                  icon="add"
                  action={this.handlePrimaryModalOpen}
                />
              )
              : null
              }
            </PanelSectionToolbarButtons>
          </PanelSectionToolbar>
          <CreateUserModal
            isOpen={this.state.primaryPanel.selected.column === SystemAdminColumn.USER
              && this.state.primaryPanel.createModal.open}
            onRequestClose={this.handlePrimaryModalClose}
            ariaHideApp={false}
            className="modal"
            overlayClassName="modal-overlay"
          />
          <CreateProfitCenterModal
            isOpen={this.state.primaryPanel.selected.column === SystemAdminColumn.PROFIT_CENTER
              && this.state.primaryPanel.createModal.open}
            onRequestClose={this.handlePrimaryModalClose}
            ariaHideApp={false}
            className="modal"
            overlayClassName="modal-overlay"
          />
          <RemoveProfitCenterModals
            isOpen={this.state.removeProfitCenterModal.open}
            onRequestClose={this.handleRemoveProfitCenterModalClose}
            ariaHideApp={false}
            className="modal"
            overlayClassName="modal-overlay"
            profitCenterId={this.state.removeProfitCenterModal.profitCenterId}
            profitCenterName={this.state.removeProfitCenterModal.profitCenterName}
          />
        </CardPanel>
        {secondaryColumnComponent}
        <div
          className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-6-12"
          style={{overflowY: 'auto'}}
        >
          <PrimaryDetailPanel
            selectedCard={primaryCard}
            selectedColumn={primaryColumn}
            queryFilter={secondaryQueryFilter}
            detail={primaryDetail}
            onPushSystemAdmin={this.openSystemAdminStatusModal}
            checkedSystemAdmin={isUserDetail(primaryDetail) && primaryDetail.isSystemAdmin}
            onPushSuspend={this.pushSuspendUser}
            checkedSuspended={isUserDetail(primaryDetail) && primaryDetail.isSuspended}
            doDomainLimitOpen={this.handleDomainLimitOpen}
          />
          <SecondaryDetailPanel
            selectedCard={secondaryCard}
            primarySelectedColumn={primaryColumn}
            secondarySelectedColumn={secondaryColumn}
            queryFilter={this.getFinalQueryFilter()}
            detail={secondaryDetail}
            onCancelPublication={this.handlePublicationCanceled}
            onCancelReduction={this.handleReductionCanceled}
            checkedClientAdmin={isUserClientRoles(secondaryDetail) && secondaryDetail.isClientAdmin}
            checkedContentPublisher={isUserClientRoles(secondaryDetail) && secondaryDetail.isContentPublisher}
            checkedAccessAdmin={isUserClientRoles(secondaryDetail) && secondaryDetail.isAccessAdmin}
            checkedContentUser={isUserClientRoles(secondaryDetail) && secondaryDetail.isContentUser}
            checkedFileDropAdmin={isUserClientRoles(secondaryDetail) && secondaryDetail.isFileDropAdmin}
            checkedFileDropUser={isUserClientRoles(secondaryDetail) && secondaryDetail.isFileDropUser}
            checkedSuspended={isRootContentItemDetail(secondaryDetail) && secondaryDetail.isSuspended}
            onPushSuspend={this.pushSuspendContent}
          />
        </div>
      </>
    );
  }

  // utility methods
  private filterEntities(entities: EntityInfoCollection, filterText: string): EntityInfo[] {
    if (!entities) {
      return [];
    }

    let filteredCards: EntityInfo[];
    if (isClientInfoTree(entities)) {
      // flatten basic tree into an array
      const traverse = (node: BasicNode<ClientInfo>, list: ClientInfoWithDepth[] = [], depth = 0) => {
        if (node.value !== null) {
          const clientDepth = {
            ...node.value,
            depth,
          };
          list.push(clientDepth);
        }
        if (node.children.length) {
          node.children.forEach((child) => list = traverse(child, list, depth + 1));
        }
        return list;
      };
      filteredCards = traverse(entities.root);
    } else {
      filteredCards = entities;
    }

    // apply filter
    filteredCards = filteredCards.filter((entity) =>
        EntityHelper.applyFilter(entity, filterText));

    if (filteredCards.length === 0) {
      return [];
    } else if (isClientInfo(filteredCards[0])) {
      const rootIndices: number[] = [];
      filteredCards.forEach((entity: ClientInfoWithDepth, i) => {
        if (!entity.parentId) {
          rootIndices.push(i);
        }
      });
      const cardGroups = rootIndices.map((_, i) =>
        filteredCards.slice(rootIndices[i], rootIndices[i + 1]));
      return cardGroups.reduce((cum, cur) => [...cum, ...cur], []);
    } else {
      return filteredCards;
    }
  }

  private getColumns(parent: SystemAdminColumn = null): ColumnIndicator[] {
    switch (parent) {
      case null:
        return [
          {
            id: SystemAdminColumn.USER,
            name: 'Users',
          },
          {
            id: SystemAdminColumn.CLIENT,
            name: 'Clients',
          },
          {
            id: SystemAdminColumn.PROFIT_CENTER,
            name: 'Profit Centers',
          },
        ];
      case SystemAdminColumn.USER:
        return [
          {
            id: SystemAdminColumn.CLIENT,
            name: 'Clients',
          },
          {
            id: SystemAdminColumn.ROOT_CONTENT_ITEM,
            name: 'Content',
          },
        ];
      case SystemAdminColumn.CLIENT:
        return [
          {
            id: SystemAdminColumn.USER,
            name: 'Users',
          },
          {
            id: SystemAdminColumn.ROOT_CONTENT_ITEM,
            name: 'Content Items',
          },
        ];
      case SystemAdminColumn.PROFIT_CENTER:
        return [
          {
            id: SystemAdminColumn.USER,
            name: 'Authorized Users',
          },
          {
            id: SystemAdminColumn.CLIENT,
            name: 'Clients',
          },
        ];
      default:
        return [];
    }
  }

  private getDataAction(column: SystemAdminColumn) {
    switch (column) {
      case SystemAdminColumn.USER:
        return 'Users';
      case SystemAdminColumn.CLIENT:
        return 'Clients';
      case SystemAdminColumn.PROFIT_CENTER:
        return 'ProfitCenters';
      case SystemAdminColumn.ROOT_CONTENT_ITEM:
        return 'RootContentItems';
      default:
        throw new Error(`'${column}' is not a valid column.`);
    }
  }

  private getDetailAction(column: SystemAdminColumn) {
    switch (column) {
      case SystemAdminColumn.USER:
        return 'UserDetail';
      case SystemAdminColumn.CLIENT:
        return 'ClientDetail';
      case SystemAdminColumn.PROFIT_CENTER:
        return 'ProfitCenterDetail';
      case SystemAdminColumn.ROOT_CONTENT_ITEM:
        return 'RootContentItemDetail';
      default:
        throw new Error(`'${column}' is not a valid column.`);
    }
  }

  private getCreateAction(column: SystemAdminColumn, context: SystemAdminColumn = null) {
    switch (column) {
      case SystemAdminColumn.USER:
        switch (context) {
          case SystemAdminColumn.CLIENT:
            return 'AddUserToClient';
          case SystemAdminColumn.PROFIT_CENTER:
            return 'AddUserToProfitCenter';
          case null:
            return 'CreateUser';
          case SystemAdminColumn.USER:
          case SystemAdminColumn.ROOT_CONTENT_ITEM:
            return null;
          default:
            throw new Error(`'${column}' is not a valid column in context of column ${context}.`);
        }
      case SystemAdminColumn.PROFIT_CENTER:
        return 'CreateProfitCenter';
      case SystemAdminColumn.CLIENT:
      case SystemAdminColumn.ROOT_CONTENT_ITEM:
      case null:
        return null;
      default:
        throw new Error(`'${column}' is not a valid column.`);
    }
  }

  private assignQueryFilter(column: SystemAdminColumn, queryFilter: QueryFilter, id: Guid): QueryFilter {
    switch (column) {
      case SystemAdminColumn.USER:
        return {...queryFilter, userId: id};
      case SystemAdminColumn.CLIENT:
        return {...queryFilter, clientId: id};
      case SystemAdminColumn.PROFIT_CENTER:
        return {...queryFilter, profitCenterId: id};
      case SystemAdminColumn.ROOT_CONTENT_ITEM:
        return {...queryFilter, rootContentItemId: id};
      default:
        return queryFilter;
    }
  }

  private getPrimaryQueryFilter = () => ({});
  private getSecondaryQueryFilter = () => this.assignQueryFilter(
    this.state.primaryPanel.selected.column,
    this.getPrimaryQueryFilter(),
    this.state.primaryPanel.selected.card,
  )
  private getFinalQueryFilter = () => this.assignQueryFilter(
    this.state.secondaryPanel.selected.column,
    this.getSecondaryQueryFilter(),
    this.state.secondaryPanel.selected.card,
  )

  // callbacks for child components
  private handlePrimaryColumnSelected = (column: SystemAdminColumn) => {
    this.setState((prevState) => {
      if (column === prevState.primaryPanel.selected.column) {
        return prevState;
      }
      return {
        data: {
          primaryEntities: null,
          secondaryEntities: null,
          primaryDetail: null,
          secondaryDetail: null,
        },
        primaryPanel: {
          selected: {
            column,
            card: null,
          },
          cards: null,
          filter: {
            text: '',
          },
          createModal: {
            open: false,
          },
          modalName: '',
        },
        secondaryPanel: {
          selected: {
            column: null,
            card: null,
          },
          cards: null,
          filter: {
            text: '',
          },
          createModal: {
            open: false,
          },
          modalName: '',
        },
        domainLimitModal: {
          open: false,
        },
        profitCenterModal: {
          open: false,
          action: '',
        },
        systemAdminModal: {
          open: false,
          enabled: false,
        },
        removeProfitCenterModal: {
          open: false,
          profitCenterId: null,
          profitCenterName: null,
        },
      };
    });
  }

  private handleSecondaryColumnSelected = (column: SystemAdminColumn) => {
    this.setState((prevState) => {
      if (column === prevState.secondaryPanel.selected.column) {
        return prevState;
      }
      return {
        ...prevState,
        data: {
          ...prevState.data,
          secondaryEntities: null,
          secondaryDetail: null,
        },
        secondaryPanel: {
          selected: {
            column,
            card: null,
          },
          cards: null,
          filter: {
            text: '',
          },
          createModal: {
            open: false,
          },
          modalName: '',
        },
        domainLimitModal: {
          open: false,
        },
      };
    });
  }

  private handlePrimaryCardSelected = (id: Guid) => {
    this.setState((prevState) => {
      const defaultColumn = this.getColumns(prevState.primaryPanel.selected.column)[0].id as SystemAdminColumn;
      return {
        data: {
          ...prevState.data,
          secondaryEntities: null,
          primaryDetail: null,
          secondaryDetail: null,
        },
        primaryPanel: {
          ...prevState.primaryPanel,
          selected: {
            ...prevState.primaryPanel.selected,
            card: prevState.primaryPanel.selected.card === id ? null : id,
          },
        },
        secondaryPanel: {
          ...prevState.secondaryPanel,
          selected: {
            column: prevState.secondaryPanel.selected.column || (defaultColumn as SystemAdminColumn),
            card: null,
          },
        },
      };
    });
  }

  private handleSecondaryCardSelected = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      data: {
        ...prevState.data,
        secondaryDetail: null,
      },
      secondaryPanel: {
        ...prevState.secondaryPanel,
        selected: {
          ...prevState.secondaryPanel.selected,
          card: prevState.secondaryPanel.selected.card === id ? null : id,
        },
      },
    }));
  }

  private fetchPrimaryEntities = () => {
    const { column } = this.state.primaryPanel.selected;
    getJsonData(`/SystemAdmin/${this.getDataAction(column)}`, {})
    .then((response: EntityInfoCollection) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryEntities: response,
        },
        primaryPanel: {
          ...prevState.primaryPanel,
          cards: this.initializeCardAttributes(response),
        },
      }));
    });
  }

  private fetchSecondaryEntities() {
    const { column } = this.state.secondaryPanel.selected;
    getJsonData(`/SystemAdmin/${this.getDataAction(column)}`, this.getSecondaryQueryFilter())
    .then((response) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryEntities: response,
        },
        secondaryPanel: {
          ...prevState.secondaryPanel,
          cards: this.initializeCardAttributes(response),
        },
      }));
    });
  }

  private fetchPrimaryDetail = () => {
    const { column, card } = this.state.primaryPanel.selected;
    if (!card) {
      return;
    }

    getJsonData(`/SystemAdmin/${this.getDetailAction(column)}`, this.getSecondaryQueryFilter())
    .then((response: PrimaryDetail) => {
      let responseWithDefaults: PrimaryDetailData;
      if (isUserDetail(response)) {
        responseWithDefaults = {
          ...response,
          isSystemAdmin: null,
          isSuspended: null,
        };
      } else {
        responseWithDefaults = response;
      }
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: responseWithDefaults,
        },
      }));
    });
  }

  private fetchSecondaryDetail = () => {
    const { column, card } = this.state.secondaryPanel.selected;
    if (!card) {
      return;
    }
    getJsonData(`/SystemAdmin/${this.getDetailAction(column)}`, this.getFinalQueryFilter())
    .then((response: SecondaryDetail) => {
      let responseWithDefaults: SecondaryDetailData;
      if (isUserClientRoles(response)) {
        responseWithDefaults = {
          ...response,
          isClientAdmin: null,
          isContentPublisher: null,
          isAccessAdmin: null,
          isContentUser: null,
          isFileDropAdmin: null,
          isFileDropUser: null,
        };
      } else if (isRootContentItemDetail(response)) {
        responseWithDefaults = {
          ...response,
          isSuspended: null,
        };
      } else {
        responseWithDefaults = response;
      }
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: responseWithDefaults,
        },
      }));
    });
  }

  private handlePublicationCanceled = (event: React.MouseEvent<HTMLAnchorElement>) => {
    event.preventDefault();
    postData(
      '/SystemAdmin/CancelPublication',
      { rootContentItemId: this.getFinalQueryFilter().rootContentItemId },
    ).then(() => {
      alert('Publication canceled.');
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: null,
        },
      }));
    });
  }

  private handleReductionCanceled = (event: React.MouseEvent<HTMLAnchorElement>, id: Guid) => {
    event.preventDefault();
    postData(
      '/SystemAdmin/CancelReduction',
      { selectionGroupId: id },
    ).then(() => {
      alert('Reduction canceled.');
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: null,
        },
      }));
    });
  }

  private fetchSystemAdmin = () => {
    getJsonData('/SystemAdmin/SystemRole', {
      ...this.getSecondaryQueryFilter(),
      role: RoleEnum.Admin,
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            isSystemAdmin: response,
          },
        },
      }));
    });
  }

  private openSystemAdminStatusModal = (_event: React.MouseEvent<HTMLButtonElement>, status: boolean) => {
    const { primaryDetail } = this.state.data;
    if (!isUserDetail(primaryDetail)) {
      return;
    }
    this.handleSecondaryModalOpen();
    this.setState({
      systemAdminModal: {
        open: true,
        enabled: status,
      },
    });
  }

  private fetchSuspendUser = () => {
    getJsonData('/SystemAdmin/UserSuspendedStatus', {
      ...this.getSecondaryQueryFilter(),
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            isSuspended: response,
          },
        },
      }));
    });
  }

  private pushSuspendUser = () => {
    const { primaryDetail } = this.state.data;
    if (!isUserDetail(primaryDetail)) {
      return;
    }

    postData('/SystemAdmin/UserSuspendedStatus', {
      ...this.getSecondaryQueryFilter(),
      value: !primaryDetail.isSuspended,
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            isSuspended: response,
          },
        },
      }));
    });
  }

  private fetchUserClient = (role: RoleEnum) => {
    getJsonData('/SystemAdmin/UserClientRoleAssignment', {
      ...this.getFinalQueryFilter(),
      role,
    }).then((response: boolean) => {
      let roleAssignment: Partial<UserClientRoles> = {};
      if (role === RoleEnum.Admin) {
        roleAssignment = { isClientAdmin: response };
      } else if (role === RoleEnum.ContentPublisher) {
        roleAssignment = { isContentPublisher: response };
      } else if (role === RoleEnum.ContentAccessAdmin) {
        roleAssignment = { isAccessAdmin: response };
      } else if (role === RoleEnum.ContentUser) {
        roleAssignment = { isContentUser: response };
      } else if (role === RoleEnum.FileDropAdmin) {
        roleAssignment = { isFileDropAdmin: response };
      } else if (role === RoleEnum.FileDropUser) {
        roleAssignment = { isFileDropUser: response };
      }
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            ...roleAssignment,
          },
        },
      }));
    });
  }

  private fetchSuspendContent = () => {
    getJsonData('/SystemAdmin/ContentSuspendedStatus', {
      ...this.getFinalQueryFilter(),
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            isSuspended: response,
          },
        },
      }));
    });
  }

  private pushSuspendContent = () => {
    const { secondaryDetail } = this.state.data;
    if (!isRootContentItemDetail(secondaryDetail)) {
      return;
    }

    postData('/SystemAdmin/ContentSuspendedStatus', {
      ...this.getFinalQueryFilter(),
      value: !secondaryDetail.isSuspended,
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            isSuspended: response,
          },
        },
      }));
    });
  }

  private handlePrimaryFilterKeyup = (text: string) => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        filter: { text },
      },
    }));
  }

  private handleSecondaryFilterKeyup = (text: string) => {
    this.setState((prevState) => ({
      ...prevState,
      secondaryPanel: {
        ...prevState.secondaryPanel,
        filter: { text },
      },
    }));
  }

  private handlePrimaryModalOpen = () => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        createModal: {
          open: true,
        },
      },
    }));
  }

  private handlePrimaryModalClose = () => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        createModal: {
          open: false,
        },
      },
    }));
  }

  private handleDomainLimitOpen = () => {
    this.setState((prevState) => ({
      ...prevState,
      domainLimitModal: {
          open: true,
      },
    }));
  }

  private handleDomainLimitClose = () => {
    this.setState((prevState) => ({
      ...prevState,
      domainLimitModal: {
        open: false,
      },
    }));
  }

  private handleSecondaryModalOpen = () => {
    this.setState((prevState) => ({
      ...prevState,
      secondaryPanel: {
        ...prevState.secondaryPanel,
        createModal: {
          open: true,
        },
      },
    }));
  }

  private handleSecondaryModalClose = () => {
    this.setState((prevState) => ({
      ...prevState,
      secondaryPanel: {
        ...prevState.secondaryPanel,
        createModal: {
          open: false,
        },
      },
      profitCenterModal: {
        open: false,
        action: '',
      },
      systemAdminModal: {
        open: false,
        enabled: false,
      },
    }));
  }

  private handlePrimaryExpandedToggled = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        cards: {
          ...prevState.primaryPanel.cards,
          [id]: {
            ...prevState.secondaryPanel.cards[id],
            expanded: !prevState.primaryPanel.cards[id].expanded,
          },
        },
      },
    }));
  }

  private handleSecondaryExpandedToggled = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      secondaryPanel: {
        ...prevState.secondaryPanel,
        cards: {
          ...prevState.secondaryPanel.cards,
          [id]: {
            ...prevState.secondaryPanel.cards[id],
            expanded: !prevState.secondaryPanel.cards[id].expanded,
          },
        },
      },
    }));
  }

  private handleProfitCenterModalOpen = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        cards: {
          ...prevState.primaryPanel.cards,
          [id]: {
            ...prevState.primaryPanel.cards[id],
            profitCenterModalOpen: true,
          },
        },
      },
    }));
  }

  private handleProfitCenterModalClose = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        cards: {
          ...prevState.primaryPanel.cards,
          [id]: {
            ...prevState.primaryPanel.cards[id],
            profitCenterModalOpen: false,
          },
        },
      },
    }));
  }

  private handleRemoveProfitCenterModalOpen = (id: Guid, name: string) => {
    this.setState((prevState) => ({
      ...prevState,
      removeProfitCenterModal: {
        open: true,
        profitCenterId: id,
        profitCenterName: name,
      },
    }));
  }

  private handleRemoveProfitCenterModalClose = () => {
    this.setState((prevState) => ({
      ...prevState,
      removeProfitCenterModal: {
        open: false,
        profitCenterId: null,
        profitCenterName: null,
      },
    }));
  }

  private handleSendReset = (email: string) => {
    postData('Account/ForgotPassword', { Email: email }, true)
    .then(() => {
      alert('Password reset email sent.');
    });
  }

  private initializeCardAttributes(entities: EntityInfoCollection): { [id: string]: CardAttributes } {
    let entityInfo: EntityInfo[];
    if (isClientInfoTree(entities)) {
      // flatten basic tree into an array
      const traverse = (node: BasicNode<ClientInfo>, list: ClientInfoWithDepth[] = [], depth = 0) => {
        if (node.value !== null) {
          const clientDepth = {
            ...node.value,
            depth,
          };
          list.push(clientDepth);
        }
        if (node.children.length) {
          node.children.forEach((child) => list = traverse(child, list, depth + 1));
        }
        return list;
      };
      entityInfo = traverse(entities.root);
    } else {
      entityInfo = entities;
    }
    const cards: {
      [id: string]: CardAttributes;
    } = {};
    entityInfo.forEach((entity) => {
        cards[entity.id] = {
          expanded: false,
          profitCenterModalOpen: false,
        };
    });
    return cards;
  }
}

function normalizeName({ firstName, lastName }: UserInfo) {
  return firstName && lastName
    ? `${firstName} ${lastName}`
    : '(Unactivated)';
}
