import * as React from 'react';

import * as moment from 'moment';

import { SelectionGroupSummary } from '../../view-models/content-publishing';
import { HierarchyDiffs } from './hierarchy-diffs';

interface SelectionGroupDetailsProps {
  selectionGroup: SelectionGroupSummary;
  changedOnly: boolean;
}

export class SelectionGroupDetails extends React.Component<SelectionGroupDetailsProps, {}> {
  public render() {
    const { changedOnly, selectionGroup } = this.props;
    const previewLink = selectionGroup
      && selectionGroup.previewLink
      && !selectionGroup.isInactive
      && (
        <span className="preview-link">
          (<a href={selectionGroup.previewLink} target="_blank">Preview</a>)
        </span>
    );
    const statusClass = selectionGroup.isInactive
      ? ' inactive'
      : '';
    const statusDetail = selectionGroup.isInactive
      ? <span className="inactive-status">INACTIVE</span>
      : <span className="active-status">ACTIVE</span>;
    const duration = moment.duration(selectionGroup.duration);
    const timeDisplay = ((hours, minutes, seconds) => (`
        ${hours ? hours + ' hour' + (hours === 1 ? ' ' : 's ') : ''}
        ${(hours || minutes) ? minutes + ' minute' + (minutes === 1 ? ' ' : 's ') : ''}
        ${seconds + ' second' + (seconds === 1 ? ' ' : 's ')}
      `))(duration.hours(), duration.minutes(), duration.seconds());
    return (
      <div className={`selection-group-detail${statusClass}`}>
        <h3>{selectionGroup.name}  {previewLink}</h3>
        <h4>Status:  {statusDetail}</h4>
        {
          selectionGroup.duration && (duration.hours() > 0 || duration.minutes() > 0 || duration.seconds() > 0) &&
          <h4>Duration:  {timeDisplay}</h4>
        }
        <h4>Authorized Users:</h4>
        <ul>
          {
            selectionGroup.users && selectionGroup.users.length > 0
              ? selectionGroup.users.map((user, i) => <li key={i}>{user.userName}</li>)
              : <li><i>No Authorized Users</i></li>
          }
        </ul>
        {
          selectionGroup.selectionChanges && (
            <>
              <h4>Reduction Value Changes:</h4>
              <HierarchyDiffs
                hierarchy={selectionGroup.selectionChanges}
                changedOnly={changedOnly}
              />
            </>
          )
        }
      </div>
    );
  }
}
