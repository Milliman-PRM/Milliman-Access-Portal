import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { StatusMonitor } from '../../status-monitor';
import { ReductionFieldInfo, ReductionFieldValueInfo } from '../../view-models/content-publishing';
import { SelectionGroupInfo } from '../models';
import { ClientInfo, RootContentItemInfo, UserInfo } from '../system-admin/interfaces';
import { ContentAccessAdmin } from './content-access-admin';

document.addEventListener('DOMContentLoaded', () => {
  const clients: ClientInfo[] = [
    { id: 'client1', name: 'client1', code: 'c1' },
    { id: 'client2', name: 'client2', code: 'c2' },
    { id: 'client3', name: 'client3', code: 'c3' },
    { id: 'client4', name: 'client4', code: 'c4' },
  ];
  const items: RootContentItemInfo[] = [
    { id: 'item1', name: 'item1', isSuspended: false },
    { id: 'item2', name: 'item2', isSuspended: false },
    { id: 'item3', name: 'item3', isSuspended: false },
    { id: 'item4', name: 'item4', isSuspended: false },
    { id: 'item5', name: 'item5', isSuspended: false },
    { id: 'item6', name: 'item6', isSuspended: false },
  ];
  const groups: SelectionGroupInfo[] = [
    { id: 'group1', name: 'group1', isSuspended: false },
    { id: 'group2', name: 'group2', isSuspended: false },
    { id: 'group3', name: 'group3', isSuspended: false },
    { id: 'group4', name: 'group4', isSuspended: false },
    { id: 'group5', name: 'group5', isSuspended: false },
    { id: 'group6', name: 'group6', isSuspended: false },
    { id: 'group7', name: 'group7', isSuspended: false },
    { id: 'group8', name: 'group8', isSuspended: false },
  ];
  const users: UserInfo[] = [
    { id: 'user1', activated: true, firstName: 'first1', lastName: 'last1', userName: 'username1',
      email: 'email1', isSuspended: false },
    { id: 'user2', activated: true, firstName: 'first2', lastName: 'last2', userName: 'username2',
      email: 'email2', isSuspended: false },
    { id: 'user3', activated: true, firstName: 'first3', lastName: 'last3', userName: 'username3',
      email: 'email3', isSuspended: false },
    { id: 'user4', activated: true, firstName: 'first4', lastName: 'last4', userName: 'username4',
      email: 'email4', isSuspended: false },
    { id: 'user5', activated: true, firstName: 'first5', lastName: 'last5', userName: 'username5',
      email: 'email5', isSuspended: false },
    { id: 'user6', activated: true, firstName: 'first6', lastName: 'last6', userName: 'username6',
      email: 'email6', isSuspended: false },
  ];
  const fields: ReductionFieldInfo[] = [
    { id: 'field1', fieldName: 'field1', displayName: 'Field 1', valueDelimiter: '|' },
    { id: 'field2', fieldName: 'field2', displayName: 'Field 2', valueDelimiter: '|' },
    { id: 'field3', fieldName: 'field3', displayName: 'Field 3', valueDelimiter: '|' },
  ];
  const values: ReductionFieldValueInfo[] = [
    { id: 'value1', value: 'value1' },
    { id: 'value2', value: 'value2' },
    { id: 'value3', value: 'value3' },
    { id: 'value4', value: 'value4' },
    { id: 'value5', value: 'value5' },
    { id: 'value6', value: 'value6' },
  ];

  const clientCards = {
    client1: {
      expanded: false,
      profitCenterModalOpen: false,
    },
    client2: {
      expanded: false,
      profitCenterModalOpen: false,
    },
  };
  const itemCards = {
    item1: {
      expanded: false,
      profitCenterModalOpen: false,
    },
  };
  const groupCards = {
    group1: {
      expanded: false,
      profitCenterModalOpen: false,
    },
  };

  ReactDOM.render(
    <ContentAccessAdmin
      data={{ clients, items, groups, users, fields, values }}
      clientPanel={{ cards: clientCards }}
      itemPanel={{ cards: itemCards }}
      groupPanel={{ cards: groupCards }}
    />,
    document.getElementById('content-container'),
  );
});

// const statusMonitor = new StatusMonitor('/Account/SessionStatus', () => null, 60000);
// statusMonitor.start();

if (module.hot) {
  module.hot.accept();
}
