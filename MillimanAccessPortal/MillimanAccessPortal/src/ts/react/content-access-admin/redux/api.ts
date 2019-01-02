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

export function fetchGroups(itemId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const groups = {
        group1: { id: 'group1', rootContentItemId: 'item1', name: 'group1', isMaster: true, isSuspended: false, assignedUsers: [ 'user1', 'user2' ] },
        group2: { id: 'group2', rootContentItemId: 'item1', name: 'group2', isMaster: false, isSuspended: true, assignedUsers: [ 'user3' ] },
        group3: { id: 'group3', rootContentItemId: 'item1', name: 'group3', isMaster: false, isSuspended: false, assignedUsers: [] },
        group4: { id: 'group4', rootContentItemId: 'item2', name: 'group4', isMaster: false, isSuspended: false, assignedUsers: [] },
        group5: { id: 'group5', rootContentItemId: 'item3', name: 'group5', isMaster: false, isSuspended: false, assignedUsers: [] },
      };
      const reductions = {
        r1: { id: 'r1', applicationUserId: 'user2', selectionGroupId: 'group1', createDateTimeUtc: '2018-02-11T00:00:00.000Z', taskStatus: ReductionStatus.Queued, selectedValues: [] },
      };
      const reductionQueue = {
        r1: { reductionId: 'r1', queuePosition: 1 },
      };

      const ro = {};
      Object.keys(groups).forEach((g) => {
        if (groups[g].rootContentItemId === itemId) {
          ro[g] = groups[g];
        }
      });
      resolve({
        groups: ro,
        reductions,
        reductionQueue,
      });
    }, 500);
  });
}

export function fetchSelections(groupId: Guid) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const groups = {
        group1: { id: 'group1', selectedValues: [] },
        group2: { id: 'group2', selectedValues: [] },
        group3: { id: 'group3', selectedValues: [] },
        group4: { id: 'group4', selectedValues: [] },
        group5: { id: 'group5', selectedValues: [] },
      };
      const fields = {
        field1: { id: 'field1', fieldName: 'field1', displayName: 'field1', rootContentItemId: 'item1', valueDelimiter: '' },
        field2: { id: 'field2', fieldName: 'field2', displayName: 'field2', rootContentItemId: 'item1', valueDelimiter: '' },
        field3: { id: 'field3', fieldName: 'field3', displayName: 'field3', rootContentItemId: 'item1', valueDelimiter: '' },
      };
      const values = {
        value1: { id: 'value1', reductionFieldId: 'field1', value: 'value1' },
        value2: { id: 'value2', reductionFieldId: 'field1', value: 'value2' },
        value3: { id: 'value3', reductionFieldId: 'field1', value: 'value3' },
        value4: { id: 'value4', reductionFieldId: 'field2', value: 'value4' },
        value5: { id: 'value5', reductionFieldId: 'field2', value: 'value5' },
      };
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
      const publications = {
        p1: { id: 'p1', applicationUserId: 'user1', rootContentItemId: 'item2', createDateTimeUtc: '2018-04-02T00:00:00.000Z', requestStatus: PublicationStatus.Queued },
      };
      const publicationQueue = {
        p1: { publicationId: 'p1', queuePosition: 2 },
      };
      const reductions = {
        r1: { id: 'r1', applicationUserId: 'user2', selectionGroupId: 'group1', createDateTimeUtc: '2018-02-11T00:00:00.000Z', taskStatus: ReductionStatus.Queued, selectedValues: [] },
      };
      const reductionQueue = {
        r1: { reductionId: 'r1', queuePosition: 1 },
      };
      resolve({ publications, publicationQueue, reductions, reductionQueue });
    }, 500);
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
