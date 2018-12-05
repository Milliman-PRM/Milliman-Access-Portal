import { Guid } from '../../models';

export function fetchClients() {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(fetchClients.name, {});
      resolve({});
    }, 3000);
  });
}

export function fetchItems(clientId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(fetchItems.name, { clientId });
      resolve({ clientId });
    }, 3000);
  });
}

export function fetchGroups(itemId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(fetchGroups.name, { itemId });
      resolve({ itemId });
    }, 3000);
  });
}

export function fetchSelections(groupId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(fetchSelections.name, { groupId });
      resolve({ groupId });
    }, 3000);
  });
}

export function fetchStatus() {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(fetchStatus.name, {});
      resolve({});
    }, 3000);
  });
}

export function createGroup(itemId: Guid, name: string) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(createGroup.name, { itemId, name });
      resolve({ itemId, name });
    }, 3000);
  });
}

export function updateGroup(groupId: Guid, name: string, users: Guid[]) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(updateGroup.name, { groupId, name, users });
      resolve({ groupId, name, users });
    }, 3000);
  });
}

export function deleteGroup(groupId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(deleteGroup.name, { groupId });
      resolve({ groupId });
    }, 3000);
  });
}

export function suspendGroup(groupId: Guid, isSuspended: boolean) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(suspendGroup.name, { groupId, isSuspended });
      resolve({ groupId, isSuspended });
    }, 3000);
  });
}

export function updateSelections(groupId: Guid, isMaster: boolean, selections: Guid[]) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(updateSelections.name, { groupId, isMaster, selections });
      resolve({ groupId, isMaster, selections });
    }, 3000);
  });
}

export function cancelReduction(groupId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(cancelReduction.name, { groupId });
      resolve({ groupId });
    }, 3000);
  });
}
