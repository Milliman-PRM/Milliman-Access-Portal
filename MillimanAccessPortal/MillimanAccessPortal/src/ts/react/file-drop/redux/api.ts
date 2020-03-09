import * as FileDropAction from './actions';

import { createJsonRequestorCreator } from '../../shared-components/redux/api';

/**
 * Function for handling request actions.
 * @param method HTTP method to use
 * @param url Request URL
 */
const createJsonRequestor =
  createJsonRequestorCreator<FileDropAction.FileDropRequestActions, FileDropAction.FileDropSuccessResponseActions>();

// ~~~~~~~~~~~~~~~~~~
// Async/Server Calls
// ~~~~~~~~~~~~~~~~~~

/**
 *  Function for fetching global page data
 */
export const fetchGlobalData =
  createJsonRequestor<FileDropAction.FetchGlobalData, FileDropAction.FetchGlobalDataSucceeded>
    ('GET', '/FileDrop/PageGlobalData');

export const fetchClients =
  createJsonRequestor<FileDropAction.FetchClients, FileDropAction.FetchClientsSucceeded>
    ('GET', '/FileDrop/Clients');

export const fetchFileDrops =
  createJsonRequestor<FileDropAction.FetchFileDrops, FileDropAction.FetchFileDropsSucceeded>
    ('GET', '/FileDrop/FileDrops');

export const createFileDrop =
  createJsonRequestor<FileDropAction.CreateFileDrop, FileDropAction.CreateFileDropSucceeded>
    ('POST', '/FileDrop/CreateFileDrop');

export const deleteFileDrop =
  createJsonRequestor<FileDropAction.DeleteFileDrop, FileDropAction.DeleteFileDropSucceeded>
    ('DELETE', '/FileDrop/DeleteFileDrop');

export const updateFileDrop =
  createJsonRequestor<FileDropAction.UpdateFileDrop, FileDropAction.UpdateFileDropSucceeded>
    ('POST', '/FileDrop/UpdateFileDrop');

export const fetchPermissionGroup =
  createJsonRequestor<FileDropAction.FetchPermissionGroups, FileDropAction.FetchPermissionGroupsSucceeded>
    ('GET', '/FileDrop/PermissionGroups');

// ~~~~~~~~~~~~~~~~~~~
// Status Refresh Call
// ~~~~~~~~~~~~~~~~~~~

export const fetchStatusRefresh =
  createJsonRequestor<FileDropAction.FetchStatusRefresh, FileDropAction.FetchStatusRefreshSucceeded>
    ('GET', '/FileDrop/Status');

// ~~~~~~~~~~~~~~~~~~
// Session Check Call
// ~~~~~~~~~~~~~~~~~~

export const fetchSessionCheck =
  createJsonRequestor<FileDropAction.FetchSessionCheck, FileDropAction.FetchSessionCheckSucceeded>
    ('GET', '/Account/SessionStatus');
