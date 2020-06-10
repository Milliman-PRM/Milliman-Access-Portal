import '../../../../images/icons/client.svg';
import '../../../../images/icons/file-drop.svg';
import '../../../../images/icons/group.svg';
import '../../../../images/icons/reports.svg';
import '../../../../images/icons/user.svg';

import * as React from 'react';

export type CardStatIcon = 'client' | 'file-drop' | 'group' | 'reports' | 'user';

export interface CardStatProps {
  name: string;
  value: number;
  icon: CardStatIcon;
}

export class CardStat extends React.Component<CardStatProps> {
  public render() {
    const { name, value, icon } = this.props;
    return (
      <div className="card-stat-container" title={name}>
        <svg className="card-stat-icon">
          <use xlinkHref={`#${icon}`} />
        </svg>
        <h4 className="card-stat-value">{value}</h4>
      </div>
    );
  }
}
