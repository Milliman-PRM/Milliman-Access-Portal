import '../../../scss/react/shared-components/upload-status-bar.scss';

import * as React from 'react';

import { ProgressSummary } from '../../upload/progress-monitor';

interface UploadStatusBarProps {
  checksumProgress: ProgressSummary;
  uploadProgress: ProgressSummary;
  errorMsg?: string;
}

export class UploadStatusBar extends React.Component<UploadStatusBarProps, {}> {
  public render() {
    const { checksumProgress, uploadProgress, errorMsg } = this.props;
    const checksumEasing =
      (checksumProgress.percentage === '0%' || checksumProgress.percentage === '100%') ? '' : ' progress-easing';
    const uploadEasing =
      (uploadProgress.percentage === '0%' || uploadProgress.percentage === '100%') ? '' : ' progress-easing';
    return (
      <>
        <div className="progress-bars">
          {!errorMsg &&
            <div
              className={`progress-bar-checksum${checksumEasing}`}
              style={{ width: checksumProgress.percentage }}
            />}
          {!errorMsg &&
            <div
              className={`progress-bar-upload${uploadEasing}`}
              style={{ width: uploadProgress.percentage }}
            />}
          {errorMsg &&
            <div
              className="progress-bar-error"
              style={{ width: '100%' }}
            />}
        </div>
        {errorMsg && <div className="status-bar-error-message">{errorMsg}</div>}
      </>
    );
  }
}
