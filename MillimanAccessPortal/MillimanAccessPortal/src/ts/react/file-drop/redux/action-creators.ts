import * as UploadActionCreators from '../../../upload/Redux/action-creators';
import * as Action from './actions';

import { createActionCreator, createRequestActionCreator } from '../../shared-components/redux/action-creators';

// ~~~~~~~~~~~~
// Page Actions
// ~~~~~~~~~~~~

/** Select a given Client by ID */
export const selectClient =
  createActionCreator<Action.SelectClient>('SELECT_CLIENT');

/** Select a given File Drop by ID */
export const selectFileDrop =
  createActionCreator<Action.SelectFileDrop>('SELECT_FILE_DROP');

/** Set filter input(s) text */
export const setFilterText =
  createActionCreator<Action.SetFilterText>('SET_FILTER_TEXT');

/** Open the Create File Drop modal */
export const openCreateFileDropModal =
  createActionCreator<Action.OpenCreateFileDropModal>('OPEN_CREATE_FILE_DROP_MODAL');

/** Close the Create File Drop modal */
export const closeCreateFileDropModal =
  createActionCreator<Action.CloseCreateFileDropModal>('CLOSE_CREATE_FILE_DROP_MODAL');

/** Update the Create File Drop modal form input values */
export const updateFileDropFormData =
  createActionCreator<Action.UpdateFileDropFormData>('UPDATE_FILE_DROP_FORM_DATA');

/** Open the Delete File Drop modal */
export const openDeleteFileDropModal =
  createActionCreator<Action.OpenDeleteFileDropModal>('OPEN_DELETE_FILE_DROP_MODAL');

/** Close the Delete File Drop modal */
export const closeDeleteFileDropModal =
  createActionCreator<Action.CloseDeleteFileDropModal>('CLOSE_DELETE_FILE_DROP_MODAL');

/** Open the Delete File Drop modal */
export const openDeleteFileDropConfirmationModal =
  createActionCreator<Action.OpenDeleteFileDropConfirmationModal>('OPEN_DELETE_FILE_DROP_CONFIRMATION_MODAL');

/** Close the Delete File Drop modal */
export const closeDeleteFileDropConfirmationModal =
  createActionCreator<Action.CloseDeleteFileDropConfirmationModal>('CLOSE_DELETE_FILE_DROP_CONFIRMATION_MODAL');

/** Put a File Drop in edit mode */
export const editFileDrop =
  createActionCreator<Action.EditFileDrop>('EDIT_FILE_DROP');

/** Take a File Drop out of edit mode */
export const cancelFileDropEdit =
  createActionCreator<Action.CancelFileDropEdit>('CANCEL_FILE_DROP_EDIT');

/** Activate a File Drop tab */
export const selectFileDropTab =
  createActionCreator<Action.SelectFileDropTab>('SELECT_FILE_DROP_TAB');

/** Set the value of a Permission Group permission */
export const setPermissionGroupPermissionValue =
  createActionCreator<Action.SetPermissionGroupPermissionValue>('SET_PERMISSION_GROUP_PERMISSION_VALUE');

/** Remove a Permission Group from a File Drop */
export const removePermissionGroup =
  createActionCreator<Action.RemovePermissionGroup>('REMOVE_PERMISSION_GROUP');

/** Discard pending changes to the Permission Groups */
export const discardPendingPermissionGroupChanges =
  createActionCreator<Action.DiscardPendingPermissionGroupChanges>('DISCARD_PENDING_PERMISSION_GROUP_CHANGES');

/** Add a user to the Permission Group form */
export const addUserToPermissionGroup =
  createActionCreator<Action.AddUserToPermissionGroup>('ADD_USER_TO_PERMISSION_GROUP');

/** Remove a user from the Permission Group form */
export const removeUserFromPermissionGroup =
  createActionCreator<Action.RemoveUserFromPermissionGroup>('REMOVE_USER_FROM_PERMISSION_GROUP');

/** Update the text of the Permission Group name in the form */
export const setPermissionGroupNameText =
  createActionCreator<Action.SetPermissionGroupNameText>('SET_PERMISSION_GROUP_NAME_TEXT');

/** Set edit mode state for Permission Groups tab */
export const setEditModeForPermissionGroups =
  createActionCreator<Action.SetEditModeForPermissionGroups>('SET_EDIT_MODE_FOR_PERMISSION_GROUPS');

/** Add a new Permission Group */
export const addNewPermissionGroup =
  createActionCreator<Action.AddNewPermissionGroup>('ADD_NEW_PERMISSION_GROUP');

/** Open the modified form modal */
export const openModifiedFormModal =
  createActionCreator<Action.OpenModifiedFormModal>('OPEN_MODIFIED_FORM_MODAL');

/** Close the modified form modal */
export const closeModifiedFormModal =
  createActionCreator<Action.CloseModifiedFormModal>('CLOSE_MODIFIED_FORM_MODAL');

/** Close the password notification modal */
export const closePasswordNotificationModal =
  createActionCreator<Action.ClosePasswordNotificationModal>('CLOSE_PASSWORD_NOTIFICATION_MODAL');

/** Enter File Drop edit mode */
export const enterFileDropEditMode =
  createActionCreator<Action.EnterFileDropEditMode>('ENTER_FILE_DROP_EDIT_MODE');

/** Exit File Drop edit mode */
export const exitFileDropEditMode =
  createActionCreator<Action.ExitFileDropEditMode>('EXIT_FILE_DROP_EDIT_MODE');

/** Set the File or Folder expansion status */
export const setFileOrFolderExpansion =
  createActionCreator<Action.SetFileOrFolderExpansion>('SET_FILE_OR_FOLDER_EXPANSION');

/** Set the File or Folder editing status */
export const setFileOrFolderEditing =
  createActionCreator<Action.SetFileOrFolderEditing>('SET_FILE_OR_FOLDER_EDITING');

/** Update the File or Folder description */
export const updateFileOrFolderDescription =
  createActionCreator<Action.UpdateFileOrFolderDescription>('UPDATE_FILE_OR_FOLDER_DESCRIPTION');

/** Enter Create Folder Mode */
export const enterCreateFolderMode =
  createActionCreator<Action.EnterCreateFolderMode>('ENTER_CREATE_FOLDER_MODE');

/** Exit Create Folder Mode */
export const exitCreateFolderMode =
  createActionCreator<Action.ExitCreateFolderMode>('EXIT_CREATE_FOLDER_MODE');

/** Update Create Folder Values */
export const updateCreateFolderValues =
  createActionCreator<Action.UpdateCreateFolderValues>('UPDATE_CREATE_FOLDER_VALUES');

// ~~~~~~~~~~~~~~~~~~~~
// Async/Server Actions
// ~~~~~~~~~~~~~~~~~~~~

/** Fetch all authorized Clients from the server */
export const fetchClients =
  createRequestActionCreator<Action.FetchClients>('FETCH_CLIENTS');

/** Fetch all authorized Clients from the server */
export const fetchFileDrops =
  createRequestActionCreator<Action.FetchFileDrops>('FETCH_FILE_DROPS');

/** Create a File Drop */
export const createFileDrop =
  createRequestActionCreator<Action.CreateFileDrop>('CREATE_FILE_DROP');

/** Delete a File Drop */
export const deleteFileDrop =
  createRequestActionCreator<Action.DeleteFileDrop>('DELETE_FILE_DROP');

/** Update a File Drop */
export const updateFileDrop =
  createRequestActionCreator<Action.UpdateFileDrop>('UPDATE_FILE_DROP');

/** Get the permission group information */
export const fetchPermissionGroups =
  createRequestActionCreator<Action.FetchPermissionGroups>('FETCH_PERMISSION_GROUPS');

/** Update the permission group information */
export const updatePermissionGroups =
  createRequestActionCreator<Action.UpdatePermissionGroups>('UPDATE_PERMISSION_GROUPS');

/** Get the Activity Log information */
export const fetchActivityLog =
  createRequestActionCreator<Action.FetchActivityLog>('FETCH_ACTIVITY_LOG');

/** Get the Activity Log information */
export const fetchSettings =
  createRequestActionCreator<Action.FetchSettings>('FETCH_SETTINGS');

/** Generate a new secure SFTP password */
export const generateNewSftpPassword =
  createRequestActionCreator<Action.GenerateNewSftpPassword>('GENERATE_NEW_SFTP_PASSWORD');

/** Set a File Drop notification setting */
export const setFileDropNotificationSetting =
  createRequestActionCreator<Action.SetFileDropNotificationSetting>('SET_FILE_DROP_NOTIFICATION_SETTING');

/** Get folder contents for a given File Drop */
export const fetchFolderContents =
  createRequestActionCreator<Action.FetchFolderContents>('FETCH_FOLDER_CONTENTS');

/** Delete a file from a File Drop */
export const deleteFileDropFile =
  createRequestActionCreator<Action.DeleteFileDropFile>('DELETE_FILE_DROP_FILE');

/** Delete a folder from a File Drop */
export const deleteFileDropFolder =
  createRequestActionCreator<Action.DeleteFileDropFolder>('DELETE_FILE_DROP_FOLDER');

/** Update a file description/name */
export const updateFileDropFile =
  createRequestActionCreator<Action.UpdateFileDropFile>('UPDATE_FILE_DROP_FILE');

/** Create a folder with a name and description */
export const createFileDropFolder =
  createRequestActionCreator<Action.CreateFileDropFolder>('CREATE_FILE_DROP_FOLDER');

/** Update a folder description/name */
export const updateFileDropFolder =
  createRequestActionCreator<Action.UpdateFileDropFolder>('UPDATE_FILE_DROP_FOLDER');

// ~~~~~~~~~~~~~~~~~~~~~~
// Status Refresh Actions
// ~~~~~~~~~~~~~~~~~~~~~~

/** Schedule a status refresh after a given delay */
export const scheduleStatusRefresh =
  createActionCreator<Action.ScheduleStatusRefresh>('SCHEDULE_STATUS_REFRESH');

/** Fetch the refreshed status information from the server */
export const fetchStatusRefresh =
  createRequestActionCreator<Action.FetchStatusRefresh>('FETCH_STATUS_REFRESH');

/** Decrement the number of status refresh attempts to determine when a threshold has been crossed */
export const decrementStatusRefreshAttempts =
  createActionCreator<Action.DecrementStatusRefreshAttempts>('DECREMENT_STATUS_REFRESH_ATTEMPTS');

/** Notify the user that the status refresh has been stopped */
export const promptStatusRefreshStopped =
  createActionCreator<Action.PromptStatusRefreshStopped>('PROMPT_STATUS_REFRESH_STOPPED');

// ~~~~~~~~~~~~~~~~~~~~~
// Session Check Actions
// ~~~~~~~~~~~~~~~~~~~~~

/** Schedule a session check call after a given delay */
export const scheduleSessionCheck =
  createActionCreator<Action.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');

/** Fetch a session check from the server */
export const fetchSessionCheck =
  createRequestActionCreator<Action.FetchSessionCheck>('FETCH_SESSION_CHECK');

// ~~~~~~~~~~~~~~~~~~~
// File Upload Actions
// ~~~~~~~~~~~~~~~~~~~

/** Initialize the first upload object when the page first loads */
export const initializeFirstUploadObject =
  createActionCreator<Action.IntitializeFirstUploadObject>('INITIALIZE_FIRST_UPLOAD_OBJECT');

export const beginFileDropFileUpload =
  createActionCreator<Action.BeginFileDropFileUpload>('BEGIN_FILE_DROP_FILE_UPLOAD');

export const beginFileDropUploadCancel =
  createActionCreator<Action.BeginFileDropUploadCancel>('BEGIN_FILE_DROP_UPLOAD_CANCEL');

export const toggleFileDropCardExpansion =
  createActionCreator<Action.ToggleFileDropCardExpansion>('TOGGLE_FILE_DROP_CARD_EXPANSION');

export const finalizeFileDropUpload =
  createActionCreator<Action.FinalizeFileDropUpload>('FINALIZE_FILE_DROP_UPLOAD');

// Upload Action Creators
export const updateChecksumProgress = UploadActionCreators.updateChecksumProgress;
export const updateUploadProgress = UploadActionCreators.updateUploadProgress;
export const setUploadCancelable = UploadActionCreators.setUploadCancelable;
export const setUploadError = UploadActionCreators.setUploadError;
export const cancelFileUpload = UploadActionCreators.cancelFileUpload;
