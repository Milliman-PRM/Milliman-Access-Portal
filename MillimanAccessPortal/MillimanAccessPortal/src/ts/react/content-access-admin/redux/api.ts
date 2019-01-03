import * as _ from 'lodash';

import { getData } from '../../../shared';
import { PublicationStatus, ReductionStatus } from '../../../view-models/content-publishing';
import { Guid } from '../../models';

/* tslint:disable:max-line-length */
export async function fetchClients() {
  return await getData('/ContentAccessAdmin/Clients');
}

export async function fetchItems(clientId: Guid) {
  return await getData('/ContentAccessAdmin/ContentItems', {
    clientId,
  });
}

export async function fetchGroups(itemId: Guid) {
  return await getData('/ContentAccessAdmin/SelectionGroupss', {
    itemId,
  });
}

export async function fetchSelections(groupId: Guid) {
  return await getData('/ContentAccessAdmin/Selectionss', {
    groupId,
  });
}

export async function fetchStatus(clientId, itemId) {
  return await getData('/ContentAccessAdmin/Statuss', {
    clientId,
    itemId,
  });
}

export function createGroup(itemId: Guid, name: string) {
  return new Promise((resolve) => {
    setTimeout(() => {
      alert({ itemId, name });
      resolve({});
    }, 500);
  });
}

export function updateGroup(groupId: Guid, name: string, users: Guid[]) {
  return new Promise((resolve) => {
    setTimeout(() => {
      alert({ groupId, name, users });
      resolve({});
    }, 500);
  });
}

export function deleteGroup(groupId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      alert(groupId);
      resolve({});
    }, 500);
  });
}

export function suspendGroup(groupId: Guid, isSuspended: boolean) {
  return new Promise((resolve) => {
    setTimeout(() => {
      alert({ groupId, isSuspended });
      resolve({});
    }, 500);
  });
}

export function updateSelections(groupId: Guid, isMaster: boolean, selections: Guid[]) {
  return new Promise((resolve) => {
    setTimeout(() => {
      alert({ groupId, isMaster, selections });
      resolve({ groupId, isMaster, selections });
    }, 500);
  });
}

export function cancelReduction(groupId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      alert({ groupId });
      resolve({ groupId });
    }, 500);
  });
}
/* tslint:enable:max-line-length */
