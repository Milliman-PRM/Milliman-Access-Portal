import * as Action from './actions';
import * as PublishingActions from './actions';

import { createJsonRequestorCreator } from '../../shared-components/redux/api';

/**
 * Function for handling request actions.
 * @param method HTTP method to use
 * @param url Request URL
 */
const createJsonRequestor =
  createJsonRequestorCreator<Action.FileDropRequestActions, Action.FileDropSuccessResponseActions>();

export const fetchGlobalData =
  createJsonRequestor<PublishingActions.FetchGlobalData, PublishingActions.FetchGlobalDataSucceeded>
    ('GET', '/FileDrop/PageGlobalData');

export const fetchClients =
  createJsonRequestor<PublishingActions.FetchClients, PublishingActions.FetchClientsSucceeded>
    ('GET', '/FileDrop/Clients');

export const fetchStatusRefresh =
  createJsonRequestor<PublishingActions.FetchStatusRefresh, PublishingActions.FetchStatusRefreshSucceeded>
    ('GET', '/FileDrop/Status');

export const fetchSessionCheck =
  createJsonRequestor<PublishingActions.FetchSessionCheck, PublishingActions.FetchSessionCheckSucceeded>
    ('GET', '/Account/SessionStatus');
