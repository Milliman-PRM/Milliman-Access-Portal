import * as _ from 'lodash';
import { AccessState } from './store';

import { ClientWithStats, User } from '../../models';

/**
 * Determines whether the form has the necessary fields filled out in order to submit.
 *
 * @param state Redux store.
 */
export function isFormValid(state: AccessState) {
  return state.valid.name &&
    state.valid.profitCenterId &&
    state.valid.contactEmail &&
    state.valid.consultantEmail;
}

/**
 * Determines whether any of the form data returned from the backend has been modified in order to determine
 * if the user should be allowed to submit the form.
 *
 * @param state Redux store.
 */
export function isFormModified(state: AccessState) {
  return state.formData.name !== state.data.details.name ||
    state.formData.clientCode !== state.data.details.clientCode ||
    state.formData.contactName !== state.data.details.clientContactName ||
    state.formData.contactTitle !== state.data.details.clientContactTitle ||
    state.formData.contactEmail !== state.data.details.clientContactEmail ||
    state.formData.contactPhone !== state.data.details.clientContactPhone ||
    !_.isEqual(state.formData.acceptedEmailDomainList, state.data.details.acceptedEmailDomainList) ||
    !_.isEqual(state.formData.acceptedEmailAddressExceptionList,
      state.data.details.acceptedEmailAddressExceptionList) ||
    state.formData.consultantName !== state.data.details.consultantName ||
    state.formData.consultantEmail !== state.data.details.consultantEmail ||
    state.formData.consultantOffice !== state.data.details.office ||
    state.formData.profitCenterId !== state.data.details.profitCenter.id ||
    (state.formData.useNewUserWelcomeText && state.formData.initialUseNewUserWelcomeText &&
      (state.formData.newUserWelcomeText !== state.data.details.newUserWelcomeText &&
      !(state.formData.newUserWelcomeText === '' && state.data.details === null))) ||
    (state.formData.useNewUserWelcomeText && !state.formData.initialUseNewUserWelcomeText &&
      state.formData.newUserWelcomeText !== '') ||
    (!state.formData.useNewUserWelcomeText && state.formData.initialUseNewUserWelcomeText);
}

/**
 * Determines whether any of the roles in a selected ClientUser have been modified from their initial state.
 *
 * @param state Redux store.
 */
export function areRolesModified(state: AccessState) {
  const currentlySelectedUser = state.data.assignedUsers.find((u) => u.id === state.selected.user);
  if (!currentlySelectedUser) { return false; }

  const currentlySelectedRolesAsArray = _.map(currentlySelectedUser.userRoles, (role) => {
    return {
      roleEnum: role.roleEnum,
      isAssigned: role.isAssigned,
    };
  }).sort((a, b) => {
    return a.roleEnum === b.roleEnum ? 0 : (a.roleEnum > b.roleEnum ? 1 : -1);
  });
  const pendingRolesSorted = state.pending.roles.roleAssignments.sort((a, b) => {
    return a.roleEnum === b.roleEnum ? 0 : (a.roleEnum > b.roleEnum ? 1 : -1);
  });

  return !_.isEqual(currentlySelectedRolesAsArray, pendingRolesSorted);
}

/**
 * Select all clients as a tree
 *
 * This selector supports client tree structures with at most 2 layers.
 *
 * @param state Redux store
 */
export function clientsTree(state: AccessState) {
  const allClientsDict = Object.assign({}, state.data.clients, state.data.parentClients);
  const clients = _.toArray(allClientsDict);
  const parentGroups: { [id: string]: ClientWithStats[] } = clients.reduce((groups, cur) =>
    groups[cur.parentId]
      ? { ...groups, [cur.parentId]: [...groups[cur.parentId], cur] }
      : { ...groups, [cur.parentId]: [cur] },
    {} as { [id: string]: ClientWithStats[] });
  const iteratees = ['name', 'code'];
  const clientTree = _.sortBy(parentGroups.null, iteratees).map((c) => ({
    parent: c,
    children: _.sortBy(parentGroups[c.id] || [], iteratees),
  }));
  return clientTree;
}

/**
 * Select all clients that match the client filter.
 * @param state Redux store
 */
export function filteredClients(state: AccessState) {
  const filterTextLower = state.filters.client.text.toLowerCase();
  const filterFunc = (client: ClientWithStats) => (
    filterTextLower === ''
    || (client.name && client.name.toLowerCase().indexOf(filterTextLower) !== -1)
    || (client.code && client.code.toLowerCase().indexOf(filterTextLower) !== -1)
  );
  return clientsTree(state).map(({ parent, children }) => ({
    parent,
    children: filterFunc(parent)
      ? children
      : children.filter((child) => filterFunc(child)),
  })).filter(({ parent, children }) => filterFunc(parent) || children.length);
}

/**
 * Select all users that match the client filter.
 * @param state Redux store
 */
export function filteredUsers(state: AccessState) {
  const filterTextLower = state.filters.user.text.toLowerCase();
  const filterFunc = (user: User) => {
    const userFullName = user.firstName + ' ' + user.lastName;
    return filterTextLower === ''
      || (userFullName && userFullName.toLowerCase().indexOf(filterTextLower) !== -1)
      || (user.userName && user.userName.toLowerCase().indexOf(filterTextLower) !== -1);
  };
  return state.data.assignedUsers.filter(filterFunc);
}

interface ClientWithIndent extends ClientWithStats {
  indent: 1 | 2;
}
/**
 * Select clients with additional rendering data.
 * @param state Redux store
 */
export function clientEntities(state: AccessState) {
  const entities: Array<ClientWithIndent | 'divider' | 'new'> = [];
  activeClients(state).forEach(({ parent, children }) => {
    entities.push({
      ...parent,
      indent: 1,
    });
    children.forEach((child) => {
      entities.push({
        ...child,
        indent: 2,
      });
    });
    entities.push('divider');
  });
  entities.push('new');
  return entities;
}

/**
 * Select all clients that are visible to the user.
 * @param state Redux store
 */
export function activeClients(state: AccessState) {
  return filteredClients(state);
}

export function activeUsers(state: AccessState) {
  return filteredUsers(state);
}

/**
 * Select users with additional rendering data.
 * @param state Redux store
 */
export function userEntities(state: AccessState) {
  const entities: Array<User | 'new'> = [];
  activeUsers(state).forEach((entity) => {
    entities.push(entity);
  });
  entities.push('new');
  return entities;
}

/**
 * Select whether all client user cards are expanded.
 * @param state Redux store
 */
export function allUsersExpanded(state: AccessState) {
  return activeUsers(state)
    .reduce((prev, u) => {
      const card = state.cardAttributes.user[u.id];
      return prev && card && card.expanded;
    }, true);
}

/**
 * Select whether all client user cards are collapsed.
 * @param state Redux store
 */
export function allUsersCollapsed(state: AccessState) {
  return activeUsers(state)
    .reduce((prev, u) => {
      const card = state.cardAttributes.user[u.id];
      return prev && (!card || !card.expanded);
    }, true);
}
