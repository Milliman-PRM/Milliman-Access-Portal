import * as _ from 'lodash';
import * as React from 'react';

import { ShareFileChanges } from '../../view-models/content-publishing';
import { Dict } from '../shared-components/redux/store';

interface FileShareDiffsProps {
  fileShares: Dict<ShareFileChanges>;
}

interface FileChangeWithStatus {
  value: string;
  status: string;
}

export class FileShareDiffs extends React.Component<FileShareDiffsProps, {}> {

  public render() {
    const { fileShares } = this.props;

    const fileDiffs = Object.keys(fileShares).map((value, key) => (
      <div key={key}>
        {this.renderOverwrittenFiles(value, fileShares[value])}
      </div>
    ));

    return (
      <div className="file-share-diffs">
        {fileDiffs}
      </div>
    );
  }

  private mapValuesToStatusValues(fileList: string[], status: string): FileChangeWithStatus[] {
    return _.map(fileList, (value) => (
      { value, status }
    ));
  }

  private renderOverwrittenFiles(_shareName: string, shareChanges: ShareFileChanges) {
    const sortedFileChanges = _.sortBy([
      ...this.mapValuesToStatusValues(shareChanges.newlyAddedShareFiles, 'Added'),
      ...this.mapValuesToStatusValues(shareChanges.replacedShareFiles, 'Overwritten'),
      ...this.mapValuesToStatusValues(shareChanges.untouchedShareFiles, 'Unchanged'),
      ...this.mapValuesToStatusValues(shareChanges.removedShareFiles, 'Removed'),
    ], (f) => f.value);

    return (
      <>
        {
          /* TODO: render name of _shareName. Not currently implemented, as there is no reason to display the internal
             Azure share name to end users, however this will become important in the future if we ever implement
             support for multiple shares.
          */
        }
        {
          sortedFileChanges && sortedFileChanges.length > 0 ? (
            <>
              <h4>The following changes will be made to the persisted dataset:</h4>
              <table>
                <thead>
                  <tr>
                    <th className="file-name">File name</th>
                    <th className="status">Status</th>
                    <th className="file-path">Full path</th>
                  </tr>
                </thead>
                <tbody>
                  {sortedFileChanges.map((fileChange) => (
                    <tr key={fileChange.value} className={fileChange.status.toLowerCase()}>
                      <td>{_.last(fileChange.value.split('/'))}</td>
                      <td>{fileChange.status}</td>
                      <td className="file-path-value">{fileChange.value}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          ) : (
            <span className="no-values">No files overwritten</span>
          )
        }
      </>
    );
  }

}
