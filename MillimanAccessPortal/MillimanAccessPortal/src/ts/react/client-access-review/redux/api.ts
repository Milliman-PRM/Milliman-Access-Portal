import { createJsonRequestorCreator } from '../../shared-components/redux/api';
import { RequestAccessReviewAction, ResponseAccessReviewAction } from './actions';
import * as AccessReviewActions from './actions';

/**
 * Function for handling request actions.
 */
const createJsonRequestor = createJsonRequestorCreator<RequestAccessReviewAction, ResponseAccessReviewAction>();

export const fetchGlobalData =
  createJsonRequestor<AccessReviewActions.FetchGlobalData, AccessReviewActions.FetchGlobalDataSucceeded>
    ('GET', '/ClientAccessReview/FetchGlobalData');

export const fetchClients =
  createJsonRequestor<AccessReviewActions.FetchClients, AccessReviewActions.FetchClientsSucceeded>
    ('GET', '/ClientAccessReview/Clients');

export const fetchSessionCheck =
  createJsonRequestor<AccessReviewActions.FetchSessionCheck, AccessReviewActions.FetchSessionCheckSucceeded>
  ('GET', '/Account/SessionStatus');
