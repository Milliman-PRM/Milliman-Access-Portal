import * as React from 'react';

import CardButton from './card-button';

export interface CardExpansionProps {
  label: string;
  expanded: boolean;
  setExpanded: (value: boolean) => void;
}
export class CardExpansion extends React.Component<CardExpansionProps> {
  public render() {
    const { children, label, expanded, setExpanded } = this.props;
    return children
      ? (
        <div className={`card-expansion-container${expanded ? ' maximized' : ''}`}>
          <h4 className="card-expansion-category-label">{label}</h4>
          {children}
          <div className="card-button-bottom-container">
            <CardButton
              color={'blue'}
              tooltip={(expanded ? 'Collapse' : 'Expand') + ' card'}
              onClick={() => setExpanded(!expanded)}
              icon={'expand-card'}
              additionalClasses={['card-button-expansion']}
            />
          </div>
        </div>
      )
      : null;
  }
}
