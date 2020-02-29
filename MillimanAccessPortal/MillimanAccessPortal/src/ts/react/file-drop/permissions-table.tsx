import '../../../images/icons/group.svg';
import '../../../images/icons/user.svg';

import '../../../scss/react/file-drop/permissions-table.scss';

import * as React from 'react';

import { ActionIcon, ActionIconButtonContainer } from '../shared-components/action-icon';
import { Checkbox } from '../shared-components/form/checkbox';

// export interface PermissionsTableProps = {
//   rawData: PermissionGroupsReturnModel;
// }

export class PermissionsTable extends React.Component {

  public render() {
    return (
      <table className="permission-group-table">
        <thead>
          <tr>
            <th className="col-group-user-icon" rowSpan={2} />
            <th className="col-name" rowSpan={2}>Name</th>
            <th className="col-email" rowSpan={2}>Email</th>
            <th className="col-permissions content-center" colSpan={3}>Permissions</th>
            <th className="col-actions content-right" rowSpan={2}>Actions</th>
          </tr>
          <tr>
            <th className="col-permission-download content-center">Download</th>
            <th className="col-permission-upload content-center">Upload</th>
            <th className="col-permission-delete content-center">Delete</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><svg className="table-icon"><use xlinkHref="#user" /></svg></td>
            <td>Jane Doe</td>
            <td>jane.doe@email.com</td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to download content from this File Drop"
                onChange={() => false}
                key={1}
                readOnly={false}
                selected={true}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to upload content to this File Drop"
                onChange={() => false}
                key={2}
                readOnly={false}
                selected={true}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to delete content from this File Drop"
                onChange={() => false}
                key={3}
                readOnly={false}
                selected={true}
              />
            </td>
            <td className="content-right">
              <ActionIconButtonContainer color="red">
                <ActionIcon
                  action={() => alert('delete')}
                  icon="delete"
                  label="Delete Permission Group"
                  inline={true}
                />
              </ActionIconButtonContainer>
            </td>
          </tr>
          <tr>
            <td><svg className="table-icon"><use xlinkHref="#group" /></svg></td>
            <td>Aetna</td>
            <td />
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to download content from this File Drop"
                onChange={() => false}
                key={1}
                readOnly={false}
                selected={true}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to upload content to this File Drop"
                onChange={() => false}
                key={2}
                readOnly={false}
                selected={true}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to delete content from this File Drop"
                onChange={() => false}
                key={3}
                readOnly={false}
                selected={false}
              />
            </td>
            <td className="content-right">
              <ActionIconButtonContainer color="blue">
                <ActionIcon
                  action={() => alert('expand')}
                  icon="expand-card"
                  label="Expand Permission Group"
                  inline={true}
                />
              </ActionIconButtonContainer>
              <ActionIconButtonContainer color="green">
                <ActionIcon
                  action={() => alert('edit')}
                  icon="edit"
                  label="Edit Permission Group Name"
                  inline={true}
                />
              </ActionIconButtonContainer>
              <ActionIconButtonContainer color="red">
                <ActionIcon
                  action={() => alert('delete')}
                  icon="delete"
                  label="Delete Permission Group"
                  inline={true}
                />
              </ActionIconButtonContainer>
            </td>
          </tr>
        </tbody>
      </table>
    );
  }
}
