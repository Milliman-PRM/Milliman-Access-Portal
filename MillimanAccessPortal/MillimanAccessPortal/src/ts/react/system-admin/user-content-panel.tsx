import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { UserList, UserPanelProps } from './interfaces';

export class UserContentPanel extends React.Component<UserPanelProps, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const users = this.props.users.map((user) => (
      <li key={user.Id}>{user.UserName}</li>
    ));
    return (
      <div className="admin-panel-content-container">
        <ul className="admin-panel-content">
          {users}
        </ul>
      </div>
    );
  }

  public componentDidMount() {
    this.fetch();
  }

  public fetch() {
    ajax({
      method: 'GET',
      url: 'SystemAdmin/Users/',
    }).done((response: UserList) => {
      this.props.onFetch(response.Users);
    }).fail((response) => {
      console.log(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    });
  }
}
