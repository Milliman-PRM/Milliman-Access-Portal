import '../../../scss/react/shared-components/toggle.scss';

import * as React from 'react';

import { getData, postData } from '../../shared';
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
    getData(this.url, Object.assign({}, this.props.queryFilter, this.props.data))
    .then((response: ToggleResponse) => {
      this.setState({
        checked: response,
        disabled: false,
      });
    });
  }

  private push() {
    if (this.state.disabled) {
      return;
    }

    this.setState({
      disabled: true,
    });

    postData(this.url, Object.assign({}, this.props.queryFilter, this.props.data, {
      value: !this.state.checked,
    }))
    .then((response: ToggleResponse) => {
      this.setState({
        checked: response,
        disabled: false,
      });
    });
  }
}
