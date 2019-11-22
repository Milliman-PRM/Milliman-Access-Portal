import * as React from 'react';

import { Checkbox } from '../shared-components/form/checkbox';

interface GoLiveSectionProps {
  title: string;
  checkboxLabel: string;
  checkboxFunction: ({ target, status }: { target: string; status: boolean; }) => void;
  checkboxSelectedValue: boolean;
  checkboxTarget: string;
}

export class GoLiveSection extends React.Component<GoLiveSectionProps, {}> {
  public render() {
    const {
      children, checkboxFunction, checkboxLabel, checkboxSelectedValue, checkboxTarget, title,
    } = this.props;
    return (
      <div className="go-live-section">
        <h3>{title}</h3>
        {children}
        <Checkbox
          name={checkboxLabel}
          selected={checkboxSelectedValue}
          onChange={(status) => checkboxFunction({
            target: checkboxTarget,
            status,
          })}
          readOnly={false}
        />
      </div>
    );
  }
}
