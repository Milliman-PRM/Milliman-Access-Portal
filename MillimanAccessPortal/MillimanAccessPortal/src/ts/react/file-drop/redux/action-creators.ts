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

// ~~~~~~~~~~~~~~~~~~~~
// Async/Server Actions
// ~~~~~~~~~~~~~~~~~~~~

/** Fetch global page data from the server */
export const fetchGlobalData =
  createRequestActionCreator<Action.FetchGlobalData>('FETCH_GLOBAL_DATA');

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
