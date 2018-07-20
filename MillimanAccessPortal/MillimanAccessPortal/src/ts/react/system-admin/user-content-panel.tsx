import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { UserCard } from '../shared-components/user-card';
import { ContentPanelProps, UserInfo } from './interfaces';

export class UserContentPanel extends React.Component<ContentPanelProps<UserInfo>, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const users = this.props.data.map((user) => (
      <li
        key={user.Id}
        // tslint:disable-next-line:jsx-no-lambda
        onClick={() => this.props.select(user.Id)}
      >
        <UserCard
          data={user}
          selected={this.props.selected === user.Id}
        />
      </li>
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

  public componentWillUnmount() {
    this.props.onFetch([]);
  }

  public fetch() {
    ajax({
      data: this.props.queryFilter,
      method: 'GET',
      url: 'SystemAdmin/Users/',
    }).done((response: UserInfo[]) => {
      this.props.onFetch(response);
    }).fail((response) => {
      console.log(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    });
  }
}
