import '../../../scss/react/client-access-review/progress-indicator.scss';

import '../../../images/icons/progress-indicator-active.svg';
import '../../../images/icons/progress-indicator-complete.svg';
import '../../../images/icons/progress-indicator-pending.svg';

import * as React from 'react';

interface ProgressIndicatorProps {
  progressObjects: {
    [id: string]: {
      label: string;
    };
  };
  currentStep: number;
}

export class ProgressIndicator extends React.Component<ProgressIndicatorProps> {

  public render() {
    const { progressObjects, currentStep } = this.props;
    return (
      <div className="progress-indicator-container">
        {
          progressObjects && Object.keys(progressObjects).map((step) => {
            const stepInt = parseInt(step, 10);
            let progressStepClass = null;
            let svgIcon = '';
            if (stepInt < currentStep) {
              progressStepClass = 'progress-step-complete';
              svgIcon = 'progress-indicator-complete';
            } else if (stepInt === currentStep) {
              progressStepClass = 'progress-step-active';
              svgIcon = 'progress-indicator-active';
            } else {
              progressStepClass = 'progress-step-pending';
              svgIcon = 'progress-indicator-pending';
            }
            return (
              <div key={step} className={`progress-step-container ${progressStepClass}`}>
                <svg className="progress-indicator-icon">
                  <use xlinkHref={`#${svgIcon}`} />
                </svg>
                <span className="progress-label">
                  {progressObjects[step].label}
                </span>
              </div>
            );
          })
        }
      </div>
    );
  }
}
