import { createJsonRequestorCreator } from '../../shared-components/redux/api';
import { RequestPublishingAction, ResponsePublishingAction } from './actions';
import * as PublishingActions from './actions';

/**
 * Function for handling request actions.
 * @param method HTTP method to use
 * @param url Request URL
 */
const createJsonRequestor = createJsonRequestorCreator<RequestPublishingAction, ResponsePublishingAction>();

export const fetchClients =
  createJsonRequestor<PublishingActions.FetchClients, PublishingActions.FetchClientsSucceeded>
    ('GET', '/ContentAccessAdmin/Clients');

export const fetchItems =
  createJsonRequestor<PublishingActions.FetchItems, PublishingActions.FetchItemsSucceeded>
    ('GET', '/ContentAccessAdmin/ContentItems');

export const fetchStatusRefresh =
  createJsonRequestor<PublishingActions.FetchStatusRefresh, PublishingActions.FetchStatusRefreshSucceeded>
    ('GET', '/ContentAccessAdmin/Status');

export const fetchSessionCheck =
  createJsonRequestor<PublishingActions.FetchSessionCheck, PublishingActions.FetchSessionCheckSucceeded>
    ('GET', '/Account/SessionStatus');
