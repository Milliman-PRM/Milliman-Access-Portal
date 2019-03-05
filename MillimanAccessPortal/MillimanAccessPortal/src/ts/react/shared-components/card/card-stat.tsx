import '../../../../images/client.svg';
import '../../../../images/group.svg';
import '../../../../images/reports.svg';
import '../../../../images/user.svg';

import * as React from 'react';

export type CardStatIcon = 'client' | 'group' | 'reports' | 'user';

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
