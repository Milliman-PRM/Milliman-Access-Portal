import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ajax } from 'jquery';

import { UserInfo } from '../../view-models/content-publishing';
import { UserPanelProps, UserPanelState } from './interfaces';

export class UserContentPanel extends React.Component<UserPanelProps, UserPanelState> {
  public constructor(props) {
    super(props);
    this.state = {
      userList: [],
    };
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
    // ajax({
    //   method: 'GET',
    //   url: 'SystemAdmin/Users/',
    // }).done((response: UserInfo[]) => {
    //   this.props.onFetch(response);
    // }).fail((response) => {
    //   console.log(response.getResponseHeader('Warning')
    //     || 'An unknown error has occurred.');
    // });
    this.props.onFetch([
      {
        Email: 'johndoe@domain.domain',
        FirstName: 'John',
        Id: 1,
        LastName: 'Doe',
        UserName: 'johndoe',
      },
    ]);
  }
}
