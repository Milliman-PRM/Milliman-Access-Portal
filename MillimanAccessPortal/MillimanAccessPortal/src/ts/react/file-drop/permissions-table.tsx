import '../../../images/icons/add-group.svg';
import '../../../images/icons/add-user.svg';
import '../../../images/icons/group.svg';
import '../../../images/icons/user.svg';

import '../../../scss/react/file-drop/permissions-table.scss';

import * as React from 'react';

import { Guid, PermissionGroupsReturnModel } from '../models';
import { ActionIcon, ActionIconButtonContainer } from '../shared-components/action-icon';
import { Checkbox } from '../shared-components/form/checkbox';

interface PermissionsTableProps {
  permissions: PermissionGroupsReturnModel;
  readOnly: boolean;
  setPermissionValue: ({ pgId, permission, value }: {
    pgId: Guid;
    permission: 'readAccess' | 'writeAccess' | 'deleteAccess';
    value: boolean;
  }) => void;
  removePermissionGroup: ({ pgId }: { pgId: Guid }) => void;
  addUserToPermissionGroup: ({ pgId, userId }: { pgId: Guid; userId: Guid }) => void;
  removeUserFromPermissionGroup: ({ pgId, userId }: { pgId: Guid; userId: Guid }) => void;
}

export class PermissionsTable extends React.Component<PermissionsTableProps> {

  public render() {
    const {
      setPermissionValue, removePermissionGroup, readOnly, addUserToPermissionGroup, removeUserFromPermissionGroup,
    } = this.props;
    const { permissionGroups, eligibleUsers } = this.props.permissions;
    const permissionGroupsMarkup = Object.keys(permissionGroups).map((pgId) => {
      const thisPG = permissionGroups[pgId];
      const pgIcon = (thisPG.isPersonalGroup) ? '#user' : '#group';
      return (
        <>
          <tr
            key={thisPG.id}
            className={thisPG.isPersonalGroup || thisPG.authorizedMapUsers.length === 0 ? 'last-group-row' : null}
          >
            <td><svg className="table-icon"><use xlinkHref={pgIcon} /></svg></td>
            {
              (thisPG.isPersonalGroup) ?
                (
                  <>
                    <td><strong>{thisPG.name}</strong></td>
                    <td>{eligibleUsers[thisPG.authorizedMapUsers[0]].username}</td>
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
                readOnly={readOnly}
                selected={thisPG.readAccess}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to upload content to this File Drop"
                onChange={(status) =>
                  setPermissionValue({ pgId: thisPG.id, permission: 'writeAccess', value: status })}
                key={2}
                readOnly={readOnly}
                selected={thisPG.writeAccess}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to delete content from this File Drop"
                onChange={(status) =>
                  setPermissionValue({ pgId: thisPG.id, permission: 'deleteAccess', value: status })}
                key={3}
                readOnly={readOnly}
                selected={thisPG.deleteAccess}
              />
            </td>
            {
              !readOnly &&
              <td className="content-right">
                <ActionIconButtonContainer color="red">
                  <ActionIcon
                    action={() => removePermissionGroup({ pgId: thisPG.id })}
                    icon="delete"
                    label="Delete Permission Group"
                    inline={true}
                  />
                </ActionIconButtonContainer>
              </td>
            }
          </tr>
          {
            !thisPG.isPersonalGroup &&
            thisPG.authorizedMapUsers.map((userId, index) => {
              const thisUser = eligibleUsers[userId];
              return (
                <tr
                  key={thisUser.id}
                  className={index === (thisPG.authorizedMapUsers.length - 1) ? 'last-group-row' : null}
                >
                  <td className="remove-user">
                    {
                      !readOnly &&
                      <ActionIcon
                        action={() => removeUserFromPermissionGroup({ pgId: thisPG.id, userId: thisUser.id })}
                        icon="remove-circle"
                        label="Remove user from Permission Group"
                        inline={true}
                      />
                    }
                  </td>
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
        <td colSpan={6} className="action-text">
          <span onClick={() => alert('Add User')}>Add User</span>
        </td>
      </tr>
    );
    const addGroupRow = (
      <tr className="action-row" onClick={() => alert('Add Group')}>
        <td><svg className="table-icon"><use xlinkHref="#add-group" /></svg></td>
        <td colSpan={6} className="action-text">
          <span onClick={() => alert('Add Group')}>Add Group</span>
        </td>
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
            {
              !readOnly &&
              <th className="col-actions content-right" rowSpan={2} />
            }
          </tr>
          <tr>
            <th className="col-permission-download content-center">Download</th>
            <th className="col-permission-upload content-center">Upload</th>
            <th className="col-permission-delete content-center">Delete</th>
          </tr>
        </thead>
        <tbody>
          {permissionGroupsMarkup}
          {!readOnly && addUserRow}
          {!readOnly && addGroupRow}
        </tbody>
      </table>
    );
  }
}
