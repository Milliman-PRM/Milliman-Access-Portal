import * as React from 'react';

export interface CardProps {
  id: number;
  primaryText: string;
  detailList: string[];
  selected: boolean;
}

export class Card extends React.Component<CardProps, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const detailList = this.props.detailList && this.props.detailList.map((detail, i) => (
      <li
        key={i}
      >
        <div>{detail}</div>
      </li>
    ));
    const expansion = (
      <div className="">
        <ul>
          {detailList}
        </ul>
      </div>
    );
    return (
      <div className="card-container">
        <div
          className={`card-body-container${this.props.selected ? ' selected' : ''}`}
        >
          <div className="card-body-main-container">
            <div className="card-body-primary-container">
              <h2 className="card-body-primary-text">
                {this.props.primaryText}
              </h2>
            </div>
          </div>
          {expansion}
        </div>
      </div>
    );
  }
}
