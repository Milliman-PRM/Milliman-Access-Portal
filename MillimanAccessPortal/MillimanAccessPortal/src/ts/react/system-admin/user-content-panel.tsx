import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

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
        style={this.props.selected === user.Id ? {fontWeight: 'bold'} : {}}
      >
        {user.Name}
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
