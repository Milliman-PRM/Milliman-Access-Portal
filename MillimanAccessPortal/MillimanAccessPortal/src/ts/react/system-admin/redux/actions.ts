import { Action } from 'redux';

export enum SystemAdminAction {
  SET_FILTER_TEXT_PRIMARY = 'SET_FILTER_TEXT_PRIMARY',
}

export interface TextAction extends Action<SystemAdminAction> {
  text: string;
}

export function setPrimaryFilterTextAction(text: string): TextAction {
  return {
    type: SystemAdminAction.SET_FILTER_TEXT_PRIMARY,
    text,
  };
}
