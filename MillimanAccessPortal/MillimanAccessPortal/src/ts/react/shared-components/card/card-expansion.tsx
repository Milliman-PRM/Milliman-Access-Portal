import * as React from 'react';

import CardButton from './card-button';

export interface CardExpansionProps {
  label: string;
  maximized: boolean;
  setMaximized: (value: boolean) => void;
}
export class CardExpansion extends React.Component<CardExpansionProps> {
  public render() {
    const { children, label, maximized, setMaximized } = this.props;
    return children
      ? (
        <div className={`card-expansion-container${maximized ? ' maximized' : ''}`}>
          <h4 className="card-expansion-category-label">{label}</h4>
          {children}
          <div className="card-button-bottom-container">
            <CardButton
              color={'blue'}
              tooltip={(maximized ? 'Collapse' : 'Expand') + ' card'}
              onClick={() => setMaximized(!maximized)}
              icon={'expand-card'}
              additionalClasses={['card-button-expansion']}
            />
          </div>
        </div>
      )
      : null;
  }
}
