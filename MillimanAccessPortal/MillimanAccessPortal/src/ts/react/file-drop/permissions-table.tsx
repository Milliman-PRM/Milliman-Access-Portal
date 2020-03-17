import '../../../images/icons/add-group.svg';
import '../../../images/icons/add-user.svg';
import '../../../images/icons/group.svg';
import '../../../images/icons/user.svg';

import '../../../scss/react/file-drop/permissions-table.scss';

import * as React from 'react';
import Select from 'react-select';

import { generateUniqueId } from '../../generate-unique-identifier';
import { AvailableEligibleUsers, Guid, PermissionGroupsReturnModel } from '../models';
import { ActionIcon, ActionIconButtonContainer } from '../shared-components/action-icon';
import { Checkbox } from '../shared-components/form/checkbox';

interface PermissionsTableProps {
  permissions: PermissionGroupsReturnModel;
  readOnly: boolean;
  isReadyToSubmit: boolean;
  unassignedEligibleUsers: AvailableEligibleUsers[];
  addPermissionGroup: ({ tempPGId, isSingleGroup }: {
    tempPGId: string,
    isSingleGroup: boolean,
  }) => void;
  setPermissionValue: ({ pgId, permission, value }: {
    pgId: Guid;
    permission: 'readAccess' | 'writeAccess' | 'deleteAccess';
    value: boolean;
  }) => void;
  removePermissionGroup: ({ pgId }: { pgId: Guid }) => void;
  addUserToPermissionGroup: ({ pgId, userId }: { pgId: Guid; userId: Guid }) => void;
  removeUserFromPermissionGroup: ({ pgId, userId }: { pgId: Guid; userId: Guid }) => void;
  setPermissionGroupNameText: ({ pgId, value }: { pgId: Guid; value: string }) => void;
}

export class PermissionsTable extends React.Component<PermissionsTableProps> {

  public render() {
    const {
      setPermissionValue, removePermissionGroup, readOnly, isReadyToSubmit, addUserToPermissionGroup,
      removeUserFromPermissionGroup, unassignedEligibleUsers, addPermissionGroup, setPermissionGroupNameText,
    } = this.props;
    const { permissionGroups, eligibleUsers } = this.props.permissions;
    const permissionGroupsMarkup = Object.keys(permissionGroups).map((pgId) => {
      const thisPG = permissionGroups[pgId];
      const pgIcon = (thisPG.isPersonalGroup) ? '#user' : '#group';
      return (
        <>
          <tr
            key={thisPG.id}
            className={
              [
                (thisPG.isPersonalGroup
                  || (thisPG.assignedMapUserIds.length === 0 && readOnly)
                  ? 'last-group-row'
                  : null),
                'first-group-row',
              ].join(' ')
            }
          >
            <td><svg className="table-icon"><use xlinkHref={pgIcon} /></svg></td>
            {
              (() => {
                if (readOnly) {
                  if (thisPG.isPersonalGroup) {
                    return (
                      <>
                        <td>
                          <strong>
                            {
                              (thisPG.assignedMapUserIds.length > 0 &&
                                eligibleUsers[thisPG.assignedMapUserIds[0]].firstName) ?
                                [
                                  eligibleUsers[thisPG.assignedMapUserIds[0]].firstName,
                                  eligibleUsers[thisPG.assignedMapUserIds[0]].lastName,
                                ].join(' ') :
                                '(Inactive)'
                            }
                          </strong>
                        </td>
                        <td>
                          {
                            thisPG.assignedMapUserIds.length > 0 &&
                            eligibleUsers[thisPG.assignedMapUserIds[0]].userName
                          }
                        </td>
                      </>
                    );
                  } else if (!thisPG.isPersonalGroup) {
                    return (
                      <td colSpan={2}><strong>{thisPG.name}</strong></td>
                    );
                  }
                } else {
                  if (thisPG.isPersonalGroup) {
                    if (thisPG.assignedMapUserIds.length === 0) {
                      return (
                        <td colSpan={2}>
                          <Select
                            className="react-select"
                            classNamePrefix="react-select"
                            options={unassignedEligibleUsers && unassignedEligibleUsers.map((u) => ({
                              value: u.id,
                              name: u.name ? u.name : '(Unactivated)',
                              userName: u.userName,
                            }))}
                            styles={{ menuPortal: (base) => ({ ...base, zIndex: 9999 }) }}
                            menuPosition="fixed"
                            menuPortalTarget={document.body}
                            menuPlacement={'auto'}
                            formatOptionLabel={(data) => (
                              <>
                                <div style={{ fontSize: '1em', fontWeight: 'bold' }}>
                                  {data.name}
                                </div>
                                <div style={{ fontSize: '0.85em' }}>
                                  {data.userName}
                                </div>
                              </>
                            )}
                            filterOption={({ data }, rawInput) => (
                              (
                                data.userName &&
                                data.userName.toLowerCase().match(rawInput.toLowerCase())
                              ) || (
                                data.name &&
                                data.name.toLowerCase().match(rawInput.toLowerCase())
                              )
                            )}
                            onChange={(value, action) => {
                              if (action.action === 'select-option') {
                                const singleValue = value as { value: string; };
                                addUserToPermissionGroup({ pgId: thisPG.id, userId: singleValue.value });
                              }
                            }}
                            controlShouldRenderValue={false}
                            placeholder="Add user"
                            autoFocus={false}
                          />
                        </td>
                      );
                    } else {
                      return (
                        <>
                          <td>
                            <strong>
                              {
                                (thisPG.assignedMapUserIds.length > 0 &&
                                  eligibleUsers[thisPG.assignedMapUserIds[0]].firstName) ?
                                  [
                                    eligibleUsers[thisPG.assignedMapUserIds[0]].firstName,
                                    eligibleUsers[thisPG.assignedMapUserIds[0]].lastName,
                                  ].join(' ') :
                                  '(Inactive)'
                              }
                            </strong>
                          </td>
                          <td>
                            {
                              thisPG.assignedMapUserIds.length > 0 &&
                              eligibleUsers[thisPG.assignedMapUserIds[0]].userName
                            }
                          </td>
                        </>
                      );
                    }
                  } else if (!thisPG.isPersonalGroup) {
                    return (
                      <td colSpan={2}>
                        <input
                          type="text"
                          className="group-name-input"
                          placeholder="Permission Group Name *"
                          autoFocus={thisPG.name.length === 0}
                          onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                            setPermissionGroupNameText({ pgId: thisPG.id, value: target.value });
                          }}
                          readOnly={false}
                          value={thisPG.name}
                        />
                      </td>
                    );
                  }
                }
              })()
            }
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to download content from this File Drop"
                onChange={(status) =>
                  setPermissionValue({ pgId: thisPG.id, permission: 'readAccess', value: status })}
                key={1}
                readOnly={readOnly || !isReadyToSubmit}
                selected={thisPG.readAccess}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to upload content to this File Drop"
                onChange={(status) =>
                  setPermissionValue({ pgId: thisPG.id, permission: 'writeAccess', value: status })}
                key={2}
                readOnly={readOnly || !isReadyToSubmit}
                selected={thisPG.writeAccess}
              />
            </td>
            <td className="content-center">
              <Checkbox
                hoverText="Allow users to delete content from this File Drop"
                onChange={(status) =>
                  setPermissionValue({ pgId: thisPG.id, permission: 'deleteAccess', value: status })}
                key={3}
                readOnly={readOnly || !isReadyToSubmit}
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
            thisPG.assignedMapUserIds.map((userId, index) => {
              const thisUser = eligibleUsers[userId];
              return (
                <tr
                  key={thisUser.id}
                  className={
                    readOnly && (index === thisPG.assignedMapUserIds.length - 1)
                      ? 'last-group-row'
                      : null
                  }
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
                  <td colSpan={5}>{thisUser.userName}</td>
                </tr>
              );
            })
          }
          {
            !readOnly &&
            !thisPG.isPersonalGroup &&
            <tr className="last-group-row">
              <td className="add-user">
                {
                  !readOnly &&
                  <ActionIcon
                    action={() => false}
                    icon="add-circle"
                    label="Add user to Permission Group"
                    inline={true}
                  />
                }
              </td>
              <td colSpan={2}>
                <Select
                  className="react-select"
                  classNamePrefix="react-select"
                  options={unassignedEligibleUsers && unassignedEligibleUsers.map((u) => ({
                    value: u.id,
                    name: u.name ? u.name : '(Unactivated)',
                    userName: u.userName,
                  }))}
                  styles={{ menuPortal: (base) => ({ ...base, zIndex: 9999 }) }}
                  menuPosition="fixed"
                  menuPortalTarget={document.body}
                  menuPlacement={'auto'}
                  formatOptionLabel={(data) => (
                    <>
                      <div style={{ fontSize: '1em', fontWeight: 'bold' }}>
                        {data.name}
                      </div>
                      <div style={{ fontSize: '0.85em' }}>
                        {data.userName}
                      </div>
                    </>
                  )}
                  filterOption={({ data }, rawInput) => (
                    (
                      data.userName &&
                      data.userName.toLowerCase().match(rawInput.toLowerCase())
                    ) || (
                      data.name &&
                      data.name.toLowerCase().match(rawInput.toLowerCase())
                    )
                  )}
                  onChange={(value, action) => {
                    if (action.action === 'select-option') {
                      const singleValue = value as { value: string; };
                      addUserToPermissionGroup({ pgId: thisPG.id, userId: singleValue.value });
                    }
                  }}
                  controlShouldRenderValue={false}
                  placeholder="Add user"
                  autoFocus={false}
                />
              </td>
              <td colSpan={4} />
            </tr>
          }
        </>
      );
    });
    const addUserRow = (
      <tr className="action-row">
        <td><svg className="table-icon"><use xlinkHref="#add-user" /></svg></td>
        <td colSpan={6} className="action-text">
          <span
            onClick={() => addPermissionGroup({ isSingleGroup: true, tempPGId: generateUniqueId('temp-pg')})}
          >
            Add User
          </span>
        </td>
      </tr>
    );
    const addGroupRow = (
      <tr className="action-row">
        <td><svg className="table-icon"><use xlinkHref="#add-group" /></svg></td>
        <td colSpan={6} className="action-text">
          <span
            onClick={() => addPermissionGroup({ isSingleGroup: false, tempPGId: generateUniqueId('temp-pg') })}
          >
            Add Group
          </span>
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
