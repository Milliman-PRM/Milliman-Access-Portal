import * as _ from 'lodash';
import { AccessState, AccessStateFormData, AccessStateValid } from './store';

import { ClientWithStats, User } from '../../models';
import { ClientDetail } from '../../system-admin/interfaces';

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
    // TODO: Wait on remaining email adding code
    // state.formData.acceptedEmailDomainList !== state.data.details.acceptedEmailDomainList ||
    // state.formData.acceptedEmailAddressExceptionList !== state.data.details.acceptedEmailAddressExceptionList ||
    state.formData.consultantName !== state.data.details.consultantName ||
    state.formData.consultantEmail !== state.data.details.consultantEmail ||
    state.formData.consultantOffice !== state.data.details.office ||
    state.formData.profitCenterId !== state.data.details.profitCenter.id;
}

/**
 * Select all clients as a tree
 *
 * This selector supports client tree structures with at most 2 layers.
 *
 * @param state Redux store
 */
export function clientsTree(state: AccessState) {
  const clients = _.toArray(state.data.clients);
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
