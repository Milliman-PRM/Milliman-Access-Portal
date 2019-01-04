import { getData, postData } from '../../../shared';
import { Guid } from '../../models';

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

export async function fetchStatus(clientId: Guid, itemId: Guid) {
  return await getData('/ContentAccessAdmin/Statuss', {
    clientId,
    itemId,
  });
}

export async function createGroup(itemId: Guid, name: string) {
  return await postData('/ContentAccessAdmin/CreateGroup', {
    itemId,
    name,
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

export async function deleteGroup(groupId: Guid) {
  return await postData('/ContentAccessAdmin/DeleteGroup', {
    groupId,
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
