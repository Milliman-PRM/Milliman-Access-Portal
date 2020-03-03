import '../../../images/icons/add-group.svg';
import '../../../images/icons/add-user.svg';
import '../../../images/icons/group.svg';
import '../../../images/icons/remove-circle.svg';
import '../../../images/icons/user.svg';

import '../../../scss/react/file-drop/permissions-table.scss';

import * as React from 'react';

import { Guid, PermissionGroupsReturnModel } from '../models';
import { ActionIcon, ActionIconButtonContainer } from '../shared-components/action-icon';
import { Checkbox } from '../shared-components/form/checkbox';

interface PermissionsTableProps {
  permissions: PermissionGroupsReturnModel;
  setPermissionValue: ({ pgId, permission, value }: {
    pgId: Guid;
    permission: 'readAccess' | 'writeAccess' | 'deleteAccess';
    value: boolean;
  }) => void;
  removePermissionGroup: ({ pgId }: { pgId: Guid }) => void;
}

export class PermissionsTable extends React.Component<PermissionsTableProps> {

  public render() {
    const { setPermissionValue, removePermissionGroup } = this.props;
    const { permissionGroups, eligibleUsers } = this.props.permissions;
    const permissionGroupsMarkup = Object.keys(permissionGroups).map((pgId) => {
      const thisPG = permissionGroups[pgId];
      const pgIcon = (thisPG.isPersonalGroup) ? '#user' : '#group';
      return (
        <>
          <tr
            key={thisPG.id}
            className={thisPG.isPersonalGroup || thisPG.assignedMapUserIds.length === 0 ? 'last-group-row' : null}
          >
            <td><svg className="table-icon"><use xlinkHref={pgIcon} /></svg></td>
            {
              (thisPG.isPersonalGroup) ?
                (
                  <>
                    <td><strong>{thisPG.name}</strong></td>
                    <td>{eligibleUsers[thisPG.assignedMapUserIds[0]].username}</td>
                  </>
                ) : (
                  <td colSpan={2}><strong>{thisPG.name}</strong></td>
                )
            }
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to download content from this File Drop"
                onChange={(status) =>
                  setPermissionValue({ pgId: thisPG.id, permission: 'readAccess', value: status })}
                key={1}
                readOnly={false}
                selected={thisPG.readAccess}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to upload content to this File Drop"
                onChange={(status) =>
                  setPermissionValue({ pgId: thisPG.id, permission: 'writeAccess', value: status })}
                key={2}
                readOnly={false}
                selected={thisPG.writeAccess}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to delete content from this File Drop"
                onChange={(status) =>
                  setPermissionValue({ pgId: thisPG.id, permission: 'deleteAccess', value: status })}
                key={3}
                readOnly={false}
                selected={thisPG.deleteAccess}
              />
            </td>
            <td className="content-right">
              {
                // !thisPG.isPersonalGroup &&
                // <ActionIconButtonContainer color="blue">
                //   <ActionIcon
                //     action={() => alert('expand')}
                //     icon="expand-card"
                //     label="Expand Permission Group"
                //     inline={true}
                //   />
                // </ActionIconButtonContainer>
              }
              {
                // <ActionIconButtonContainer color="green">
                //   <ActionIcon
                //     action={() => alert('edit')}
                //     icon="edit"
                //     label="Edit Permission Group Name"
                //     inline={true}
                //   />
                // </ActionIconButtonContainer>
              }
              <ActionIconButtonContainer color="red">
                <ActionIcon
                  action={() => removePermissionGroup({ pgId: thisPG.id })}
                  icon="delete"
                  label="Delete Permission Group"
                  inline={true}
                />
              </ActionIconButtonContainer>
            </td>
          </tr>
          {
            !thisPG.isPersonalGroup &&
            thisPG.assignedMapUserIds.map((userId, index) => {
              const thisUser = eligibleUsers[userId];
              return (
                <tr
                  key={thisUser.id}
                  className={index === (thisPG.assignedMapUserIds.length - 1) ? 'last-group-row' : null}
                >
                  <td><svg className="table-icon"><use xlinkHref="#remove-cirlce" /></svg></td>
                  <td>{thisUser.firstName + ' ' + thisUser.lastName}</td>
                  <td colSpan={5}>{thisUser.username}</td>
                </tr>
              );
            })
          }
        </>
      );
    });
    const addUserRow = (
      <tr className="action-row" onClick={() => alert('Add User')}>
        <td><svg className="table-icon"><use xlinkHref="#add-user" /></svg></td>
        <td colSpan={6} className="action-text">Add User</td>
      </tr>
    );
    const addGroupRow = (
      <tr className="action-row" onClick={() => alert('Add Group')}>
        <td><svg className="table-icon"><use xlinkHref="#add-group" /></svg></td>
        <td colSpan={6} className="action-text">Add Group</td>
      </tr>
    );

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
          {permissionGroupsMarkup}
          {addUserRow}
          {addGroupRow}
        </tbody>
      </table>
    );
  }
}
