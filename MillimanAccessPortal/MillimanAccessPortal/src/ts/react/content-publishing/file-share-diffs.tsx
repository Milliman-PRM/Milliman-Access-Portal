import * as _ from 'lodash';
import * as React from 'react';

import { ShareFileChanges } from '../../view-models/content-publishing';
import { Dict } from '../shared-components/redux/store';

interface FileShareDiffsProps {
  fileShares: Dict<ShareFileChanges>;
}

export class FileShareDiffs extends React.Component<FileShareDiffsProps, {}> {

  public render() {
    const { fileShares } = this.props;

    const fileDiffs = Object.keys(fileShares).map((value, key) => (
      <div key={key}>
        {this.renderOverwrittenFiles(value, fileShares[value].replacedFiles)}
      </div>
    ));

    return (
      <div className="file-share-diffs">
        {fileDiffs}
      </div>
    );
  }

  private renderOverwrittenFiles(_shareName: string, overWrittenFiles: string[]) {
    return (
      <>
        {
          /* TODO: render name of _shareName. Not currently implemented, as there is no reason to display the internal
             Azure share name to end users, however this will become important in the future if we ever implement
             support for multiple shares.
          */
        }
        {
          overWrittenFiles.length > 0 ? (
            <>
              <h4>The following files will be permanently overwritten:</h4>
              <table>
                <thead>
                  <tr>
                    <th className="file-name">File name</th>
                    <th className="file-path">Full path</th>
                  </tr>
                </thead>
                <tbody>
                  {overWrittenFiles.map((value) => (
                    <tr key={value} className="overwritten">
                      <td>{_.last(value.split('/'))}</td>
                      <td className="file-path-value">{value}</td>
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
