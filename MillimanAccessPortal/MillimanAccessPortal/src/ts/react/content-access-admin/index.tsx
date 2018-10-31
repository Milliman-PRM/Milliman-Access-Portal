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
    { Id: 'client1', Name: 'client1', Code: 'c1', ParentOnly: true },
  ];
  const items: RootContentItemInfo[] = [
    { Id: 'item1', Name: 'item1', ClientName: '', IsSuspended: false },
  ];
  const groups: SelectionGroupInfo[] = [
    { Id: 'group1', Name: 'group1', IsSuspended: false },
  ];
  const users: UserInfo[] = [
    { Id: 'user1', Activated: true, FirstName: 'first1', LastName: 'last1', UserName: 'username1',
      Email: 'email1', IsSuspended: false },
  ];
  const fields: ReductionFieldInfo[] = [
    { Id: 'field1', FieldName: 'field1', DisplayName: 'Field 1', ValueDelimiter: '|' },
  ];
  const values: ReductionFieldValueInfo[] = [
    { Id: 'value1', Value: 'value1' },
  ];

  const clientCards = {
    client1: {
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
