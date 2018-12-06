import { PublicationStatus, ReductionStatus } from '../../../view-models/content-publishing';
import { Guid } from '../../models';

export function fetchClients() {
  return new Promise((resolve) => {
    setTimeout(() => {
      const clients = [
        { id: 'client1', name: 'client1', code: 'c1', eligibleUsers: ['user1', 'user2', 'user3', 'user4', 'user5'] },
        { id: 'client2', name: 'client2', code: 'c2', eligibleUsers: ['user1', 'user2', 'user3', 'user4', 'user5'] },
        { id: 'client3', name: 'client3', code: 'c3', eligibleUsers: ['user1', 'user2', 'user3'] },
        { id: 'client4', name: 'client4', code: 'c4', eligibleUsers: [] },
      ];
      const users = [
        { id: 'user1', firstName: 'Ichi', lastName: 'One',   userName: 'user1', email: 'user1@a.a',
          activated: true, isSuspended: false },
        { id: 'user2', firstName: 'Ni',   lastName: 'Two',   userName: 'user2', email: 'user2@a.a',
          activated: true, isSuspended: false },
        { id: 'user3', firstName: 'San',  lastName: 'Three', userName: 'user3', email: 'user3@a.a',
          activated: true, isSuspended: false },
        { id: 'user4', firstName: 'Shi',  lastName: 'Four',  userName: 'user4', email: 'user4@a.a',
          activated: true, isSuspended: false },
        { id: 'user5', firstName: 'Go',   lastName: 'Five',  userName: 'user5', email: 'user5@a.a',
          activated: true, isSuspended: false },
      ];
      resolve({ clients, users });
    }, 500);
  });
}

export function fetchItems(clientId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const items = [
        { id: 'item1', clientId: 'client1', contentTypeId: '1', name: 'item1', doesReduce: true, isSuspended: false },
        { id: 'item2', clientId: 'client1', contentTypeId: '1', name: 'item2', doesReduce: true, isSuspended: false },
        { id: 'item3', clientId: 'client1', contentTypeId: '1', name: 'item3', doesReduce: true, isSuspended: true },
        { id: 'item4', clientId: 'client2', contentTypeId: '1', name: 'item4', doesReduce: true, isSuspended: false },
        { id: 'item5', clientId: 'client2', contentTypeId: '1', name: 'item5', doesReduce: true, isSuspended: false },
        { id: 'item6', clientId: 'client3', contentTypeId: '1', name: 'item6', doesReduce: false, isSuspended: false },
      ];
      const contentTypes = [
        { id: '1', name: 'QlikView', canReduce: true, fileExtensions: [] },
        { id: '2', name: 'HTML', canReduce: false, fileExtensions: [] },
        { id: '3', name: 'PDF', canReduce: false, fileExtensions: [] },
      ];
      const publications = [
        { id: 'p1', applicationUserId: 'user1', rootContentItemId: 'item2',
          createDateTimeUtc: '2018-04-02T00:00:00.000Z', requestStatus: PublicationStatus.Queued },
      ];
      const publicationQueue = [
        { publicationId: 'p1', queuePosition: 2 },
      ];
      resolve({
        items: items.filter((i) => i.clientId === clientId),
        contentTypes,
        publications,
        publicationQueue,
      });
    }, 500);
  });
}

export function fetchGroups(itemId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const groups = [
        { id: 'group1', rootContentItemId: 'item1', name: 'group1', isMaster: true, isSuspended: false,
          assignedUsers: [ 'user1', 'user2' ] },
        { id: 'group2', rootContentItemId: 'item1', name: 'group2', isMaster: false, isSuspended: true,
          assignedUsers: [ 'user3' ] },
        { id: 'group3', rootContentItemId: 'item1', name: 'group3', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group4', rootContentItemId: 'item2', name: 'group4', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group5', rootContentItemId: 'item3', name: 'group5', isMaster: false, isSuspended: false,
          assignedUsers: [] },
      ];
      const reductions = [
        { id: 'r1', applicationUserId: 'user2', selectionGroupId: 'group1',
          createDateTimeUtc: '2018-02-11T00:00:00.000Z', taskStatus: ReductionStatus.Queued,
          selectedValues: [] },
      ];
      const reductionQueue = [
        { reductionId: 'r1', queuePosition: 1 },
      ];
      resolve({
        groups: groups.filter((g) => g.rootContentItemId === itemId),
        reductions,
        reductionQueue,
      });
    }, 500);
  });
}

export function fetchSelections(groupId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const groups = [
        { id: 'group1', selectedValues: [] },
        { id: 'group2', selectedValues: [] },
        { id: 'group3', selectedValues: [] },
        { id: 'group4', selectedValues: [] },
        { id: 'group5', selectedValues: [] },
      ];
      const fields = [
        { id: 'field1', fieldName: 'field1', displayName: 'field1', rootContentItemId: 'item1', valueDelimiter: '' },
        { id: 'field2', fieldName: 'field2', displayName: 'field2', rootContentItemId: 'item1', valueDelimiter: '' },
        { id: 'field3', fieldName: 'field3', displayName: 'field3', rootContentItemId: 'item1', valueDelimiter: '' },
      ];
      const values = [
        { id: 'value1', reductionFieldId: 'field1', value: 'value1' },
        { id: 'value2', reductionFieldId: 'field1', value: 'value2' },
        { id: 'value3', reductionFieldId: 'field1', value: 'value3' },
        { id: 'value4', reductionFieldId: 'field2', value: 'value4' },
        { id: 'value5', reductionFieldId: 'field2', value: 'value5' },
      ];
      resolve({
        groupId,
        groups,
        fields,
        values,
      });
    }, 500);
  });
}

export function fetchStatus() {
  return new Promise((resolve) => {
    setTimeout(() => {
      const publications = [
        { id: 'p1', applicationUserId: 'user1', rootContentItemId: 'item2',
          createDateTimeUtc: '2018-04-02T00:00:00.000Z', requestStatus: PublicationStatus.Queued },
      ];
      const publicationQueue = [
        { publicationId: 'p1', queuePosition: 2 },
      ];
      const reductions = [
        { id: 'r1', applicationUserId: 'user2', selectionGroupId: 'group1',
          createDateTimeUtc: '2018-02-11T00:00:00.000Z', taskStatus: ReductionStatus.Queued,
          selectedValues: [] },
      ];
      const reductionQueue = [
        { reductionId: 'r1', queuePosition: 1 },
      ];
      resolve({ publications, publicationQueue, reductions, reductionQueue });
    }, 500);
  });
}

export function createGroup(itemId: Guid, name: string) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const groups = [
        { id: 'group1', rootContentItemId: 'item1', name: 'group1', isMaster: true, isSuspended: false,
          assignedUsers: [ 'user1', 'user2' ] },
        { id: 'group2', rootContentItemId: 'item1', name: 'group2', isMaster: false, isSuspended: true,
          assignedUsers: [ 'user3' ] },
        { id: 'group3', rootContentItemId: 'item1', name: 'group3', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group4', rootContentItemId: 'item2', name: 'group4', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group5', rootContentItemId: 'item3', name: 'group5', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group6', rootContentItemId: itemId, name, isMaster: false, isSuspended: false,
          assignedUsers: [] },
      ];
      resolve({ groups });
    }, 500);
  });
}

export function updateGroup(groupId: Guid, name: string, users: Guid[]) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const groups = [
        { id: 'group1', rootContentItemId: 'item1', name: 'group1', isMaster: true, isSuspended: false,
          assignedUsers: [ 'user1', 'user2' ] },
        { id: 'group2', rootContentItemId: 'item1', name: 'group2', isMaster: false, isSuspended: true,
          assignedUsers: [ 'user3' ] },
        { id: 'group3', rootContentItemId: 'item1', name: 'group3', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group4', rootContentItemId: 'item2', name: 'group4', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group5', rootContentItemId: 'item3', name: 'group5', isMaster: false, isSuspended: false,
          assignedUsers: [] },
      ];
      const i = groups.findIndex((g) => g.id === groupId);
      groups[i].name = name;
      groups[i].assignedUsers = users;
      resolve({ groups });
    }, 500);
  });
}

export function deleteGroup(groupId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const groups = [
        { id: 'group1', rootContentItemId: 'item1', name: 'group1', isMaster: true, isSuspended: false,
          assignedUsers: [ 'user1', 'user2' ] },
        { id: 'group2', rootContentItemId: 'item1', name: 'group2', isMaster: false, isSuspended: true,
          assignedUsers: [ 'user3' ] },
        { id: 'group3', rootContentItemId: 'item1', name: 'group3', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group4', rootContentItemId: 'item2', name: 'group4', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group5', rootContentItemId: 'item3', name: 'group5', isMaster: false, isSuspended: false,
          assignedUsers: [] },
      ];
      resolve({ groups: groups.filter((g) => g.id !== groupId) });
    }, 500);
  });
}

export function suspendGroup(groupId: Guid, isSuspended: boolean) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const groups = [
        { id: 'group1', rootContentItemId: 'item1', name: 'group1', isMaster: true, isSuspended: false,
          assignedUsers: [ 'user1', 'user2' ] },
        { id: 'group2', rootContentItemId: 'item1', name: 'group2', isMaster: false, isSuspended: true,
          assignedUsers: [ 'user3' ] },
        { id: 'group3', rootContentItemId: 'item1', name: 'group3', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group4', rootContentItemId: 'item2', name: 'group4', isMaster: false, isSuspended: false,
          assignedUsers: [] },
        { id: 'group5', rootContentItemId: 'item3', name: 'group5', isMaster: false, isSuspended: false,
          assignedUsers: [] },
      ];
      const i = groups.findIndex((g) => g.id === groupId);
      groups[i].isSuspended = isSuspended;
      resolve({ groups });
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
