import * as _ from 'lodash';
import { AccessState, AccessStateFormData, AccessStateValid } from './store';

import { ClientWithStats, User } from '../../models';
import { ClientDetail } from '../../system-admin/interfaces';

/**
 * Returns whether or not a string is considered valid by the client-admin form.
 *
 * @param value string to be tested
 */
export function isStringNotEmpty(value: string): boolean {
  return value !== null && value.trim() !== '';
}

/**
 * Returns whether a valid email address is non-empty and also a valid email address.
 *
 * @param email email address to be tested
 */
export function isEmailAddressValid(email: string): boolean {
  const emailRegex = /\S+@\S+\.\S+/;
  return email.trim() === '' || email === null || emailRegex.test(email);
}

/**
 * Determines whether the form has the necessary fields filled out in order to submit.
 *
 * @param valid state of all necessary fields and whether their current input is valid or not.
 */
export function isFormValid(valid: AccessStateValid) {
  return valid.name &&
    valid.profitCenterId &&
    valid.contactEmail &&
    valid.consultantEmail;
}

/**
 * Determines whether any of the form data returned from the backend has been modified in order to determine
 * if the user should be allowed to submit the form.
 *
 * @param formData the form data that may/may not have been modified by the user.
 * @param detail the initial data returned from the server from the last FetchClientDetail call.
 */
export function isFormModified(formData: AccessStateFormData, detail: ClientDetail) {
  return formData.name !== detail.name ||
    formData.clientCode !== detail.clientCode ||
    formData.contactName !== detail.clientContactName ||
    formData.contactTitle !== detail.clientContactTitle ||
    formData.contactEmail !== detail.clientContactEmail ||
    formData.contactPhone !== detail.clientContactPhone ||
    // TODO: Wait on remaining email adding code
    // formData.acceptedEmailDomainList !== detail.acceptedEmailDomainList ||
    // formData.acceptedEmailAddressExceptionList !== detail.acceptedEmailAddressExceptionList ||
    formData.consultantName !== detail.consultantName ||
    formData.consultantEmail !== detail.consultantEmail ||
    formData.consultantOffice !== detail.office ||
    formData.profitCenterId !== detail.profitCenter.id;
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
