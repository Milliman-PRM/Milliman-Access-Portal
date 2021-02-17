import * as moment from 'moment';
import * as React from 'react';
import { Guid } from '../models';
import { UserStatus } from './system-admin';

interface UserStatusDisplayProps {
  onPushEnableUserAccount: (id: Guid, email: string, targetEntitySet: 'primary' | 'secondary') => void;
  userId: Guid;
  userEmail: string;
  targetSet: 'primary' | 'secondary';
  status: UserStatus;
}

export class UserStatusDisplay extends React.Component<UserStatusDisplayProps> {
  public render() {
    return (
      <>
        <h3 className="detail-section-title">Status</h3>
        {!this.props.status.isAccountDisabled && !this.props.status.isAccountNearDisabled &&
          <span>Active</span>
        }
        {this.props.status.isAccountDisabled &&
          <div className="detail-container">
            <div>Disabled</div>
            <button
              name="systemAdminButton"
              className={'systemAdminButton blue-button'}
              onClick={(_event) => {
                this.props.onPushEnableUserAccount(this.props.userId, this.props.userEmail, this.props.targetSet);
              }}
            >
              Re-enable user account
            </button>
          </div>
        }
        {this.props.status.isAccountNearDisabled &&
          <div className="detail-container">
            User account will be disabled on&nbsp;
            {moment.utc(this.props.status.accountDisableDate).local().format('MMM DD, YYYY')}
          </div>
        }
      </>
    );
  }
}
