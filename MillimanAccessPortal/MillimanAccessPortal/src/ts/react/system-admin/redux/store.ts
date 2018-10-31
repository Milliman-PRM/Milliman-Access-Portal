import { createStore } from 'redux';
import { setPrimaryFilterTextReducer } from './reducers';

export const store = createStore(setPrimaryFilterTextReducer);
