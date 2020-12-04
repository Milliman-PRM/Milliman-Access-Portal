import { PageUploadAction } from '../../../upload/Redux/actions';
import {
  FileDrop, FileDropClientWithStats, FileDropDirectoryContentModel, FileDropEvent,
  FileDropNotificationTypeEnum, FileDropSettings, FileDropsReturnModel, FileDropWithStats,
  Guid, PermissionGroupsChangesModel, PermissionGroupsReturnModel,
} from '../../models';
import { TSError } from '../../shared-components/redux/actions';
import { Dict } from '../../shared-components/redux/store';
import { AfterFormModal, AvailableFileDropTabs } from './store';

// ~~~~~~~~~~~~
// Page Actions
// ~~~~~~~~~~~~

/**
 *  Select the client card specified by id
 *  If id refers to the currently selected card, deselect it
 */
export interface SelectClient {
  type: 'SELECT_CLIENT';
  id: Guid;
}

/**
 *  Select the File Drop card specified by id
 *  If id refers to the currently selected card, deselect it
 */
export interface SelectFileDrop {
  type: 'SELECT_FILE_DROP';
  id: Guid | 'NEW FILE DROP';
}

/** Set filter text for the filter inputs */
export interface SetFilterText {
  type: 'SET_FILTER_TEXT';
  filter: 'client' | 'fileDrop' | 'permissions' | 'activityLog';
  text: string;
}

/** Open the Create File Drop Modal */
export interface OpenCreateFileDropModal {
  type: 'OPEN_CREATE_FILE_DROP_MODAL';
  clientId: Guid;
}

/** Close the Create File Drop Modal */
export interface CloseCreateFileDropModal {
  type: 'CLOSE_CREATE_FILE_DROP_MODAL';
}

/** Update the input values of the Create File Drop Modal form */
export interface UpdateFileDropFormData {
  type: 'UPDATE_FILE_DROP_FORM_DATA';
  updateType: 'create' | 'edit';
  field: 'fileDropName' | 'fileDropDescription';
  value: string;
}

/** Open the modal used to begin File Drop deletion */
export interface OpenDeleteFileDropModal {
  type: 'OPEN_DELETE_FILE_DROP_MODAL';
  fileDrop: FileDropWithStats;
}

/** Close the modal used to begin File Drop deletion */
export interface CloseDeleteFileDropModal {
  type: 'CLOSE_DELETE_FILE_DROP_MODAL';
}

/** Open the modal used to confirm File Drop deletion */
export interface OpenDeleteFileDropConfirmationModal {
  type: 'OPEN_DELETE_FILE_DROP_CONFIRMATION_MODAL';
}

/** Close the modal used to confirm File Drop deletion */
export interface CloseDeleteFileDropConfirmationModal {
  type: 'CLOSE_DELETE_FILE_DROP_CONFIRMATION_MODAL';
}

/** Put the File Drop in edit mode */
export interface EditFileDrop {
  type: 'EDIT_FILE_DROP';
  fileDrop: FileDropWithStats;
}

/** Take the File Drop out of edit mode */
export interface CancelFileDropEdit {
  type: 'CANCEL_FILE_DROP_EDIT';
}

/** Activate a File Drop tab */
export interface SelectFileDropTab {
  type: 'SELECT_FILE_DROP_TAB';
  tab: AvailableFileDropTabs;
}

/** Change the value of a Permission Group permission */
export interface SetPermissionGroupPermissionValue {
  type: 'SET_PERMISSION_GROUP_PERMISSION_VALUE';
  pgId: Guid;
  permission: 'readAccess' | 'writeAccess' | 'deleteAccess';
  value: boolean;
}

/** Remove a Permission Group */
export interface RemovePermissionGroup {
  type: 'REMOVE_PERMISSION_GROUP';
  pgId: Guid;
}

/** Discard pening Permission Group changes */
export interface DiscardPendingPermissionGroupChanges {
  type: 'DISCARD_PENDING_PERMISSION_GROUP_CHANGES';
  originalValues: PermissionGroupsReturnModel;
}

/** Add user to Permission Group */
export interface AddUserToPermissionGroup {
  type: 'ADD_USER_TO_PERMISSION_GROUP';
  pgId: Guid;
  userId: Guid;
  userName?: string;
}

/** Remove user from Permission Group */
export interface RemoveUserFromPermissionGroup {
  type: 'REMOVE_USER_FROM_PERMISSION_GROUP';
  pgId: Guid;
  userId: Guid;
}

/** Set Permission Group name in Permission Group */
export interface SetPermissionGroupNameText {
  type: 'SET_PERMISSION_GROUP_NAME_TEXT';
  pgId: Guid;
  value: string;
}

/** Set Edit Mode for Permission Group tab */
export interface SetEditModeForPermissionGroups {
  type: 'SET_EDIT_MODE_FOR_PERMISSION_GROUPS';
  editModeEnabled: boolean;
}

/** Add a new Permission Group */
export interface AddNewPermissionGroup {
  type: 'ADD_NEW_PERMISSION_GROUP';
  tempPGId: string;
  isSingleGroup: boolean;
}

/** Open the modal used to confirm navigation away from a modified form */
export interface OpenModifiedFormModal {
  type: 'OPEN_MODIFIED_FORM_MODAL';
  afterFormModal: AfterFormModal;
}

/** Close the modal used to confirm navigation away from a modified form */
export interface CloseModifiedFormModal {
  type: 'CLOSE_MODIFIED_FORM_MODAL';
}

/** Close the modal used to display the generated password */
export interface ClosePasswordNotificationModal {
  type: 'CLOSE_PASSWORD_NOTIFICATION_MODAL';
}

/** Enter File Drop edit mode */
export interface EnterFileDropEditMode {
  type: 'ENTER_FILE_DROP_EDIT_MODE';
  editMode: 'file' | 'folder';
  id: Guid;
}

/** Expand or contract the file or folder element in the content table */
export interface SetFileOrFolderExpansion {
  type: 'SET_FILE_OR_FOLDER_EXPANSION';
  id: Guid;
  expanded: boolean;
}

/** Set editing status for file or folder element in the content table */
export interface SetFileOrFolderEditing {
  type: 'SET_FILE_OR_FOLDER_EDITING';
  id: Guid;
  editing: boolean;
  fileName: string;
  description: string;
}

/** Update the file or folder name value */
export interface UpdateFileOrFolderName {
  type: 'UPDATE_FILE_OR_FOLDER_NAME';
  id: Guid;
  name: string;
}

/** Update the file or folder description value */
export interface UpdateFileOrFolderDescription {
  type: 'UPDATE_FILE_OR_FOLDER_DESCRIPTION';
  id: Guid;
  description: string;
}

/** Exit File Drop edit mode */
export interface ExitFileDropEditMode {
  type: 'EXIT_FILE_DROP_EDIT_MODE';
}

/** Set Permission Group  */

// ~~~~~~~~~~~~~~~~~~~~
// Async/Server Actions
// ~~~~~~~~~~~~~~~~~~~~

/**
 * GET:
 *   Clients the current user has access to publish for
 *   Users who are File Drop eligible in those clients
 */
export interface FetchClients {
  type: 'FETCH_CLIENTS';
  request: {};
}
/** Action called upon successful return of the FetchClients API call */
export interface FetchClientsSucceeded {
  type: 'FETCH_CLIENTS_SUCCEEDED';
  response: {
    clients: Dict<FileDropClientWithStats>;
  };
}
/** Action called upon return of an error from the FetchClients API call */
export interface FetchClientsFailed {
  type: 'FETCH_CLIENTS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   File Drops the current user has access to
 */
export interface FetchFileDrops {
  type: 'FETCH_FILE_DROPS';
  request: {
    clientId: Guid;
  };
}
/** Action called upon successful return of the FetchFileDrops API call */
export interface FetchFileDropsSucceeded {
  type: 'FETCH_FILE_DROPS_SUCCEEDED';
  response: FileDropsReturnModel;
}
/** Action called upon return of an error from the FetchFileDrops API call */
export interface FetchFileDropsFailed {
  type: 'FETCH_FILE_DROPS_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Create a new File Drop
 */
export interface CreateFileDrop {
  type: 'CREATE_FILE_DROP';
  request: FileDrop;
}
/** Action called upon successful return of the CreateFileDrop API call */
export interface CreateFileDropSucceeded {
  type: 'CREATE_FILE_DROP_SUCCEEDED';
  response: FileDropsReturnModel;
}
/** Action called upon return of an error from the CreateFileDrop API call */
export interface CreateFileDropFailed {
  type: 'CREATE_FILE_DROP_FAILED';
  error: TSError;
}

/**
 * DELETE:
 *   Delete a File Drop
 */
export interface DeleteFileDrop {
  type: 'DELETE_FILE_DROP';
  request: Guid;
}
/** Action called upon successful return of the DeleteFileDrop API call */
export interface DeleteFileDropSucceeded {
  type: 'DELETE_FILE_DROP_SUCCEEDED';
  response: FileDropsReturnModel;
}
/** Action called upon return of an error from the DeleteFileDrop API call */
export interface DeleteFileDropFailed {
  type: 'DELETE_FILE_DROP_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Update a File Drop
 */
export interface UpdateFileDrop {
  type: 'UPDATE_FILE_DROP';
  request: FileDrop;
}
/** Action called upon successful return of the DeleteFileDrop API call */
export interface UpdateFileDropSucceeded {
  type: 'UPDATE_FILE_DROP_SUCCEEDED';
  response: FileDropsReturnModel;
}
/** Action called upon return of an error from the DeleteFileDrop API call */
export interface UpdateFileDropFailed {
  type: 'UPDATE_FILE_DROP_FAILED';
  error: TSError;
}

/**
 * GET:
 *   All elibile users and permission groups (including membership and permissions)
 */
export interface FetchPermissionGroups {
  type: 'FETCH_PERMISSION_GROUPS';
  request: {
    clientId: Guid;
    fileDropId: Guid;
  };
}
/** Action called upon successful return of the FetchFileDrops API call */
export interface FetchPermissionGroupsSucceeded {
  type: 'FETCH_PERMISSION_GROUPS_SUCCEEDED';
  response: PermissionGroupsReturnModel;
}
/** Action called upon return of an error from the FetchFileDrops API call */
export interface FetchPermissionGroupsFailed {
  type: 'FETCH_PERMISSION_GROUPS_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Update all permission groups (including membership and permissions)
 */
export interface UpdatePermissionGroups {
  type: 'UPDATE_PERMISSION_GROUPS';
  request: PermissionGroupsChangesModel;
}
/** Action called upon successful return of the FetchFileDrops API call */
export interface UpdatePermissionGroupsSucceeded {
  type: 'UPDATE_PERMISSION_GROUPS_SUCCEEDED';
  response: PermissionGroupsReturnModel;
}
/** Action called upon return of an error from the FetchFileDrops API call */
export interface UpdatePermissionGroupsFailed {
  type: 'UPDATE_PERMISSION_GROUPS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   File Drop events for the last 30 days
 */
export interface FetchActivityLog {
  type: 'FETCH_ACTIVITY_LOG';
  request: {
    fileDropId: Guid;
  };
}
/** Action called upon successful return of the FetchActivityLog API call */
export interface FetchActivityLogSucceeded {
  type: 'FETCH_ACTIVITY_LOG_SUCCEEDED';
  response: FileDropEvent[];
}
/** Action called upon return of an error from the FetchActivityLog API call */
export interface FetchActivityLogFailed {
  type: 'FETCH_ACTIVITY_LOG_FAILED';
  error: TSError;
}

/**
 * GET:
 *   File Drop settings for the requesting user
 */
export interface FetchSettings {
  type: 'FETCH_SETTINGS';
  request: {
    fileDropId: Guid;
  };
}
/** Action called upon successful return of the FetchSettings API call */
export interface FetchSettingsSucceeded {
  type: 'FETCH_SETTINGS_SUCCEEDED';
  response: FileDropSettings;
}
/** Action called upon return of an error from the FetchSettings API call */
export interface FetchSettingsFailed {
  type: 'FETCH_SETTINGS_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Generate a new SFTP password for the requesting user
 */
export interface GenerateNewSftpPassword {
  type: 'GENERATE_NEW_SFTP_PASSWORD';
  request: Guid;  // File Drop ID
}
/** Action called upon successful return of the GenerateNewSftpPassword API call */
export interface GenerateNewSftpPasswordSucceeded {
  type: 'GENERATE_NEW_SFTP_PASSWORD_SUCCEEDED';
  response: {
    userName: string;
    password: string;
  };
}
/** Action called upon return of an error from the GenerateNewSftpPassword API call */
export interface GenerateNewSftpPasswordFailed {
  type: 'GENERATE_NEW_SFTP_PASSWORD_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Set the specified File Drop notification setting
 */
export interface SetFileDropNotificationSetting {
  type: 'SET_FILE_DROP_NOTIFICATION_SETTING';
  request: {
    fileDropId: Guid;
    notifications: {
      notificationType: FileDropNotificationTypeEnum;
      isEnabled: boolean;
    }[];
  };
}
/** Action called upon successful return of the SetFileDropNotificationSetting API call */
export interface SetFileDropNotificationSettingSucceeded {
  type: 'SET_FILE_DROP_NOTIFICATION_SETTING_SUCCEEDED';
  response: FileDropSettings;
}
/** Action called upon return of an error from the SetFileDropNotificationSetting API call */
export interface SetFileDropNotificationSettingFailed {
  type: 'SET_FILE_DROP_NOTIFICATION_SETTING_FAILED';
  error: TSError;
}

/**
 * GET:
 *   Folder contents for the requested File Drop
 */
export interface FetchFolderContents {
  type: 'FETCH_FOLDER_CONTENTS';
  request: {
    fileDropId: Guid;
    canonicalPath: string;
  };
}
/** Action called upon successful return of the FetchFolderContents API call */
export interface FetchFolderContentsSucceeded {
  type: 'FETCH_FOLDER_CONTENTS_SUCCEEDED';
  response: FileDropDirectoryContentModel;
}
/** Action called upon return of an error from the FetchFolderContents API call */
export interface FetchFolderContentsFailed {
  type: 'FETCH_FOLDER_CONTENTS_FAILED';
  error: TSError;
}

/**
 * DELETE:
 *   Delete a file from a File Drop
 */
export interface DeleteFileDropFile {
  type: 'DELETE_FILE_DROP_FILE';
  request: {
    fileDropId: Guid;
    fileId: Guid;
  };
}
/** Action called upon successful return of the DeleteFileDropFile API call */
export interface DeleteFileDropFileSucceeded {
  type: 'DELETE_FILE_DROP_FILE_SUCCEEDED';
  response: FileDropDirectoryContentModel;
}
/** Action called upon return of an error from the DeleteFileDropFile API call */
export interface DeleteFileDropFileFailed {
  type: 'DELETE_FILE_DROP_FILE_FAILED';
  error: TSError;
}

/**
 * DELETE:
 *   Delete a folder from a File Drop
 */
export interface DeleteFileDropFolder {
  type: 'DELETE_FILE_DROP_FOLDER';
  request: {
    fileDropId: Guid;
    folderId: Guid;
  };
}
/** Action called upon successful return of the DeleteFileDropFolder API call */
export interface DeleteFileDropFolderSucceeded {
  type: 'DELETE_FILE_DROP_FOLDER_SUCCEEDED';
  response: FileDropDirectoryContentModel;
}
/** Action called upon return of an error from the DeleteFileDropFolder API call */
export interface DeleteFileDropFolderFailed {
  type: 'DELETE_FILE_DROP_FOLDER_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Update a file description.
 */
export interface UpdateFileDropFile {
  type: 'UPDATE_FILE_DROP_FILE';
  request: {
    fileDropId: Guid;
    fileId: Guid;
    fileDescription: string;
  };
}
/** Action called upon successful return of the UpdateFileDropFile API call */
export interface UpdateFileDropFileSucceeded {
  type: 'UPDATE_FILE_DROP_FILE_SUCCEEDED';
  response: FileDropDirectoryContentModel;
}
/** Action called upon return of an error from the UpdateFileDropFile API call */
export interface UpdateFileDropFileFailed {
  type: 'UPDATE_FILE_DROP_FILE_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Update a folder description.
 */
export interface UpdateFileDropFolder {
  type: 'UPDATE_FILE_DROP_FOLDER';
  request: {
    fileDropId: Guid;
    folderId: Guid;
    folderDescription: string;
  };
}
/** Action called upon successful return of the UpdateFileDropFolder API call */
export interface UpdateFileDropFolderSucceeded {
  type: 'UPDATE_FILE_DROP_FOLDER_SUCCEEDED';
  response: FileDropDirectoryContentModel;
}
/** Action called upon return of an error from the UpdateFileDropFolder API call */
export interface UpdateFileDropFolderFailed {
  type: 'UPDATE_FILE_DROP_FOLDER_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Update a file's name.
 */
export interface RenameFileDropFile {
  type: 'RENAME_FILE_DROP_FILE';
  request: {
    fileDropId: Guid;
    fileId: Guid;
    fileName: string;
  };
}

/** Action called upon successful return of the RenameFileDropFile API call */
export interface RenameFileDropFileSucceeded {
  type: 'RENAME_FILE_DROP_FILE_SUCCEEDED';
  response: FileDropDirectoryContentModel;
}

/** Action called upon return of an error from the RenameFileDropFile API call */
export interface RenameFileDropFileFailed {
  type: 'RENAME_FILE_DROP_FILE_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Update a folder's name, along with all other paths below it.
 */
export interface RenameFileDropFolder {
  type: 'RENAME_FILE_DROP_FOLDER';
  request: {
    fileDropId: Guid;
    directoryId: Guid;
    parentCanonicalPath: string;
    directoryName: string,
  };
}

/** Action called upon successful return of the RenameFileDropFolder API call */
export interface RenameFileDropFolderSucceeded {
  type: 'RENAME_FILE_DROP_FOLDER_SUCCEEDED';
  response: FileDropDirectoryContentModel;
}

/** Action called upon return of an error from the RenameFileDropFolder API call */
export interface RenameFileDropFolderFailed {
  type: 'RENAME_FILE_DROP_FOLDER_FAILED';
  error: TSError;
}

// ~~~~~~~~~~~~~~~~~~~~~~
// Status Refresh Actions
// ~~~~~~~~~~~~~~~~~~~~~~

/** Schedule a status refresh after a given delay */
export interface ScheduleStatusRefresh {
  type: 'SCHEDULE_STATUS_REFRESH';
  delay: number;
}

/**
 * GET:
 *   Updates to the selected Client specified by clientId
 */
export interface FetchStatusRefresh {
  type: 'FETCH_STATUS_REFRESH';
  request: {
    clientId: Guid;
  };
}
/** Action called upon successful return of the FetchStatusRefresh API call */
export interface FetchStatusRefreshSucceeded {
  type: 'FETCH_STATUS_REFRESH_SUCCEEDED';
  response: {};
}
/** Action called upon return of an error from the FetchStatusRefresh API call */
export interface FetchStatusRefreshFailed {
  type: 'FETCH_STATUS_REFRESH_FAILED';
  error: TSError;
}

/** Decrement remaining status refresh attempts */
export interface DecrementStatusRefreshAttempts {
  type: 'DECREMENT_STATUS_REFRESH_ATTEMPTS';
}

/** Display a toast indicating that the status refresh polling has stopped */
export interface PromptStatusRefreshStopped {
  type: 'PROMPT_STATUS_REFRESH_STOPPED';
}

// ~~~~~~~~~~~~~~~~~~~~~
// Session Check Actions
// ~~~~~~~~~~~~~~~~~~~~~

/**
 * GET:
 *   A bodiless response that serves as a session heartbeat
 */
export interface FetchSessionCheck {
  type: 'FETCH_SESSION_CHECK';
  request: {};
}
/** Action called upon successful return of the FetchSessionCheck API call */
export interface FetchSessionCheckSucceeded {
  type: 'FETCH_SESSION_CHECK_SUCCEEDED';
  response: {};
}
/** Action called upon return of an error from the FetchSessionCheck API call */
export interface FetchSessionCheckFailed {
  type: 'FETCH_SESSION_CHECK_FAILED';
  error: TSError;
}

/** Schedule a session check after a given delay */
export interface ScheduleSessionCheck {
  type: 'SCHEDULE_SESSION_CHECK';
  delay: number;
}

// ~~~~~~~~~~~~~~~~~~~
// File Upload Actions
// ~~~~~~~~~~~~~~~~~~~

/* Intitialize the first File Upload object after page load */
export interface IntitializeFirstUploadObject {
  type: 'INITIALIZE_FIRST_UPLOAD_OBJECT';
}

export interface BeginFileDropFileUpload {
  type: 'BEGIN_FILE_DROP_FILE_UPLOAD';
  uploadId: string;
  clientId: Guid;
  fileDropId: Guid;
  folderId: Guid;
  canonicalPath: string;
  fileName: string;
}

export interface BeginFileDropUploadCancel {
  type: 'BEGIN_FILE_DROP_UPLOAD_CANCEL';
  uploadId: string;
}

export interface ToggleFileDropCardExpansion {
  type: 'TOGGLE_FILE_DROP_CARD_EXPANSION';
  fileDropId: Guid;
}

export interface FinalizeFileDropUpload {
  type: 'FINALIZE_FILE_DROP_UPLOAD';
  uploadId: string;
  fileDropId: Guid;
  folderId: Guid;
  canonicalPath: string;
}


// ~~~~~~~~~~~~~
// Action Unions
// ~~~~~~~~~~~~~

/** Actions that change the state of the page */
export type FileDropPageActions =
  | SelectClient
  | SelectFileDrop
  | PromptStatusRefreshStopped
  | DecrementStatusRefreshAttempts
  | OpenCreateFileDropModal
  | CloseCreateFileDropModal
  | UpdateFileDropFormData
  | OpenDeleteFileDropModal
  | CloseDeleteFileDropModal
  | OpenDeleteFileDropConfirmationModal
  | CloseDeleteFileDropConfirmationModal
  | EditFileDrop
  | CancelFileDropEdit
  | SelectFileDropTab
  | SetPermissionGroupPermissionValue
  | RemovePermissionGroup
  | DiscardPendingPermissionGroupChanges
  | AddUserToPermissionGroup
  | RemoveUserFromPermissionGroup
  | SetPermissionGroupNameText
  | SetEditModeForPermissionGroups
  | AddNewPermissionGroup
  | OpenModifiedFormModal
  | CloseModifiedFormModal
  | ClosePasswordNotificationModal
  | IntitializeFirstUploadObject
  | BeginFileDropFileUpload
  | BeginFileDropUploadCancel
  | ToggleFileDropCardExpansion
  | FinalizeFileDropUpload
  | EnterFileDropEditMode
  | ExitFileDropEditMode
  | SetFileOrFolderExpansion
  | SetFileOrFolderEditing
  | UpdateFileOrFolderName
  | UpdateFileOrFolderDescription
  ;

/** Actions that schedule another action */
export type FileDropScheduleActions =
  | ScheduleSessionCheck
  | ScheduleStatusRefresh
  ;

/** Actions that makes Ajax requests */
export type FileDropRequestActions =
  | FetchClients
  | FetchFileDrops
  | CreateFileDrop
  | DeleteFileDrop
  | UpdateFileDrop
  | FetchPermissionGroups
  | FetchStatusRefresh
  | FetchSessionCheck
  | UpdatePermissionGroups
  | FetchActivityLog
  | FetchSettings
  | GenerateNewSftpPassword
  | SetFileDropNotificationSetting
  | FetchFolderContents
  | DeleteFileDropFile
  | DeleteFileDropFolder
  | UpdateFileDropFile
  | UpdateFileDropFolder
  | RenameFileDropFile
  | RenameFileDropFolder
  ;

/** Actions that marks the succesful response of an Ajax request */
export type FileDropSuccessResponseActions =
  | FetchClientsSucceeded
  | FetchFileDropsSucceeded
  | CreateFileDropSucceeded
  | DeleteFileDropSucceeded
  | UpdateFileDropSucceeded
  | FetchPermissionGroupsSucceeded
  | FetchStatusRefreshSucceeded
  | FetchSessionCheckSucceeded
  | UpdatePermissionGroupsSucceeded
  | FetchActivityLogSucceeded
  | FetchSettingsSucceeded
  | GenerateNewSftpPasswordSucceeded
  | SetFileDropNotificationSettingSucceeded
  | FetchFolderContentsSucceeded
  | DeleteFileDropFileSucceeded
  | DeleteFileDropFolderSucceeded
  | UpdateFileDropFileSucceeded
  | UpdateFileDropFolderSucceeded
  | RenameFileDropFileSucceeded
  | RenameFileDropFolderSucceeded
  ;

/** Actions that marks the errored response of an Ajax request */
export type FileDropErrorActions =
  | FetchClientsFailed
  | FetchFileDropsFailed
  | CreateFileDropFailed
  | DeleteFileDropFailed
  | UpdateFileDropFailed
  | FetchPermissionGroupsFailed
  | FetchStatusRefreshFailed
  | FetchSessionCheckFailed
  | UpdatePermissionGroupsFailed
  | FetchActivityLogFailed
  | FetchSettingsFailed
  | GenerateNewSftpPasswordFailed
  | SetFileDropNotificationSettingFailed
  | FetchFolderContentsFailed
  | DeleteFileDropFileFailed
  | DeleteFileDropFolderFailed
  | UpdateFileDropFileFailed
  | UpdateFileDropFolderFailed
  | RenameFileDropFileFailed
  | RenameFileDropFolderFailed
  ;

/** Actions that set filter text */
export type FilterActions =
  | SetFilterText
  ;

/** All available File Drop Actions */
export type FileDropActions =
  | FileDropPageActions
  | FileDropScheduleActions
  | FileDropRequestActions
  | FileDropSuccessResponseActions
  | FileDropErrorActions
  | FilterActions
  | PageUploadAction
  ;

/** An action that opens a modal */
export type OpenModalAction =
  | OpenCreateFileDropModal
  | OpenDeleteFileDropModal
  | OpenDeleteFileDropConfirmationModal
  | OpenModifiedFormModal
  | GenerateNewSftpPasswordSucceeded
  ;
