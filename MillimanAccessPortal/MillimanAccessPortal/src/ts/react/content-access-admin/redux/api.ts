import { getData, getJsonData, postJsonData } from '../../../shared';
import { Guid } from '../../models';

export async function fetchClients() {
  return await getJsonData('/ContentAccessAdmin/Clients');
}

export async function fetchItems(clientId: Guid) {
  return await getJsonData('/ContentAccessAdmin/ContentItems', {
    clientId,
  });
}

export async function fetchGroups(itemId: Guid) {
  return await getJsonData('/ContentAccessAdmin/SelectionGroups', {
    itemId,
  });
}

export async function fetchSelections(groupId: Guid) {
  return await getJsonData('/ContentAccessAdmin/Selections', {
    groupId,
  });
}

export async function fetchStatusRefresh(clientId: Guid, itemId: Guid) {
  return await getJsonData('/ContentAccessAdmin/Status', {
    clientId,
    itemId,
  });
}

export async function fetchSessionCheck() {
  return await getData('/Account/SessionStatus');
}

export async function createGroup(itemId: Guid, name: string) {
  return await postJsonData('/ContentAccessAdmin/CreateGroup', {
    itemId,
    name,
  });
}

export async function updateGroup(groupId: Guid, name: string, users: Guid[]) {
  return await postJsonData('/ContentAccessAdmin/UpdateGroup', {
    groupId,
    name,
    users,
  });
}

export async function deleteGroup(groupId: Guid) {
  return await postJsonData('/ContentAccessAdmin/DeleteGroup', {
    groupId,
  });
}

export async function suspendGroup(groupId: Guid, isSuspended: boolean) {
  return await postJsonData('/ContentAccessAdmin/SuspendGroup', {
    groupId,
    isSuspended,
  });
}

export async function updateSelections(groupId: Guid, isMaster: boolean, selections: Guid[]) {
  return await postJsonData('/ContentAccessAdmin/UpdateSelections', {
    groupId,
    isMaster,
    selections,
  });
}

export async function cancelReduction(groupId: Guid) {
  return await postJsonData('/ContentAccessAdmin/CancelReduction', {
    groupId,
  });
}
