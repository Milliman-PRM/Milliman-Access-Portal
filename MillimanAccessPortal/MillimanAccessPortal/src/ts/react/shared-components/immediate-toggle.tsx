import '../../../scss/react/shared-components/toggle.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { QueryFilter } from './interfaces';

interface ImmediateToggleProps {
  controller: string;
  action: string;
  queryFilter: QueryFilter;
  label: string;
  data: object;
}
interface ImmediateToggleState {
  checked: boolean;
  disabled: boolean;
}

type ToggleResponse = boolean;

export class ImmediateToggle extends React.Component<ImmediateToggleProps, ImmediateToggleState> {

  private get url() {
    return `${this.props.controller}/${this.props.action}/`;
  }

  public constructor(props) {
    super(props);

    this.state = {
      checked: false,
      disabled: true,
    };

    this.fetch = this.fetch.bind(this);
    this.push = this.push.bind(this);
  }

  public componentDidMount() {
    this.fetch();
  }

  public render() {
    return (
      <div className="switch-container">
        <div
          className="toggle-switch"
          onClick={this.push}
        >
          <input
            type="checkbox"
            className="toggle-switch-checkbox"
            name={this.props.label}
            checked={this.state.checked}
          />
          <label className="toggle-switch-label" htmlFor={this.props.label}>
            <span className="toggle-switch-inner" />
            <span className="toggle-switch-switch" />
          </label>
        </div>
        <label className="switch-label">{this.props.label}</label>
      </div>
    );
  }

  private fetch() {
    ajax({
      data: Object.assign({}, this.props.queryFilter, this.props.data),
      method: 'GET',
      url: this.url,
    }).done((response: ToggleResponse) => {
      this.setState({
        checked: response,
        disabled: false,
      });
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }

  private push() {
    if (this.state.disabled) {
      return;
    }

    this.setState({
      disabled: true,
    });

    ajax({
      data: Object.assign({}, this.props.queryFilter, this.props.data, {
        value: !this.state.checked,
      }),
      headers: {
        RequestVerificationToken: (
          document.getElementsByName('__RequestVerificationToken')[0] as HTMLInputElement).value,
      },
      method: 'POST',
      url: this.url,
    }).done((response: ToggleResponse) => {
      this.setState({
        checked: response,
        disabled: false,
      });
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }
}
