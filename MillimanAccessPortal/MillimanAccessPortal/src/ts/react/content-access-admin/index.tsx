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
    { Id: 'client1', Name: 'client1', Code: 'c1' },
    { Id: 'client2', Name: 'client2', Code: 'c2' },
    { Id: 'client3', Name: 'client3', Code: 'c3' },
    { Id: 'client4', Name: 'client4', Code: 'c4' },
  ];
  const items: RootContentItemInfo[] = [
    { Id: 'item1', Name: 'item1', IsSuspended: false },
    { Id: 'item2', Name: 'item2', IsSuspended: false },
    { Id: 'item3', Name: 'item3', IsSuspended: false },
    { Id: 'item4', Name: 'item4', IsSuspended: false },
    { Id: 'item5', Name: 'item5', IsSuspended: false },
    { Id: 'item6', Name: 'item6', IsSuspended: false },
  ];
  const groups: SelectionGroupInfo[] = [
    { Id: 'group1', Name: 'group1', IsSuspended: false },
    { Id: 'group2', Name: 'group2', IsSuspended: false },
    { Id: 'group3', Name: 'group3', IsSuspended: false },
    { Id: 'group4', Name: 'group4', IsSuspended: false },
    { Id: 'group5', Name: 'group5', IsSuspended: false },
    { Id: 'group6', Name: 'group6', IsSuspended: false },
    { Id: 'group7', Name: 'group7', IsSuspended: false },
    { Id: 'group8', Name: 'group8', IsSuspended: false },
  ];
  const users: UserInfo[] = [
    { Id: 'user1', Activated: true, FirstName: 'first1', LastName: 'last1', UserName: 'username1',
      Email: 'email1', IsSuspended: false },
    { Id: 'user2', Activated: true, FirstName: 'first2', LastName: 'last2', UserName: 'username2',
      Email: 'email2', IsSuspended: false },
    { Id: 'user3', Activated: true, FirstName: 'first3', LastName: 'last3', UserName: 'username3',
      Email: 'email3', IsSuspended: false },
    { Id: 'user4', Activated: true, FirstName: 'first4', LastName: 'last4', UserName: 'username4',
      Email: 'email4', IsSuspended: false },
    { Id: 'user5', Activated: true, FirstName: 'first5', LastName: 'last5', UserName: 'username5',
      Email: 'email5', IsSuspended: false },
    { Id: 'user6', Activated: true, FirstName: 'first6', LastName: 'last6', UserName: 'username6',
      Email: 'email6', IsSuspended: false },
  ];
  const fields: ReductionFieldInfo[] = [
    { Id: 'field1', FieldName: 'field1', DisplayName: 'Field 1', ValueDelimiter: '|' },
    { Id: 'field2', FieldName: 'field2', DisplayName: 'Field 2', ValueDelimiter: '|' },
    { Id: 'field3', FieldName: 'field3', DisplayName: 'Field 3', ValueDelimiter: '|' },
  ];
  const values: ReductionFieldValueInfo[] = [
    { Id: 'value1', Value: 'value1' },
    { Id: 'value2', Value: 'value2' },
    { Id: 'value3', Value: 'value3' },
    { Id: 'value4', Value: 'value4' },
    { Id: 'value5', Value: 'value5' },
    { Id: 'value6', Value: 'value6' },
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
