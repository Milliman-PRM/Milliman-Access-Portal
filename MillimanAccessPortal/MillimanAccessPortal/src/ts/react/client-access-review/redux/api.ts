import { createJsonRequestorCreator } from '../../shared-components/redux/api';
import { RequestAccessReviewAction, ResponseAccessReviewAction } from './actions';
import * as AccessReviewActions from './actions';

/**
 * Function for handling request actions.
 */
const createJsonRequestor = createJsonRequestorCreator<RequestAccessReviewAction, ResponseAccessReviewAction>();

export const fetchGlobalData =
  createJsonRequestor<AccessReviewActions.FetchGlobalData, AccessReviewActions.FetchGlobalDataSucceeded>
    ('GET', '/ClientAccessReview/PageGlobalData');

export const fetchClients =
  createJsonRequestor<AccessReviewActions.FetchClients, AccessReviewActions.FetchClientsSucceeded>
    ('GET', '/ClientAccessReview/Clients');

export const fetchClientSummary =
  createJsonRequestor<AccessReviewActions.FetchClientSummary, AccessReviewActions.FetchClientSummarySucceeded>
    ('GET', '/ClientAccessReview/ClientSummary');

export const fetchClientReview =
  createJsonRequestor<AccessReviewActions.FetchClientReview, AccessReviewActions.FetchClientReviewSucceeded>
    ('GET', '/ClientAccessReview/BeginClientAccessReview');

export const approvedClientAccessReview = createJsonRequestor
  <AccessReviewActions.ApproveClientAccessReview, AccessReviewActions.ApproveClientAccessReviewSucceeded>
    ('POST', '/ClientAccessReview/ApproveClientAccessReview');

export const downloadClientAccessReviewSummary = createJsonRequestor
  <AccessReviewActions.DownloadClientAccessReviewSummary,
    AccessReviewActions.DownloadClientAccessReviewSummarySucceeded>
    ('GET', '/ClientAccessReview/DownloadClientAccessReviewSummary');

export const fetchSessionCheck =
  createJsonRequestor<AccessReviewActions.FetchSessionCheck, AccessReviewActions.FetchSessionCheckSucceeded>
    ('GET', '/Account/SessionStatus');
