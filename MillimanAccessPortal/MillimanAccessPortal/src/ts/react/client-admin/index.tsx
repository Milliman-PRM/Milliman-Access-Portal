import { Provider } from "react-redux";
import { ConnectedClientAdmin as Root } from "./client-admin";
import ReactDOM from "react-dom";
import { store } from "./redux/store";
import React from "react";

let ConnectedClientAdmin: typeof Root = require('./client-admin').ConnectedClientAdmin;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(
    <Provider store={store} >
      <ConnectedClientAdmin />
    </Provider>,
    document.getElementById('content-container'),
  );
});

if (module.hot) {
  module.hot.accept(['./client-admin'], () => {
    ConnectedClientAdmin = require('./client-admin').ConnectedClientAdmin;
  });
}
