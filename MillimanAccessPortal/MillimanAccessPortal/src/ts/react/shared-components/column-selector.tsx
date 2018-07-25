import '../../../scss/react/shared-components/column-selector.scss';

import * as React from 'react';

import { Entity } from './entity';
import { DataSource } from './interfaces';

interface ColumnSelectorProps {
  dataSources: Array<DataSource<Entity>>;
  setSelectedDataSource: (dataSource: string) => void;
  selectedDataSource: DataSource<Entity>;
}

export class ColumnSelector extends React.Component<ColumnSelectorProps, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const options = this.props.dataSources.map((dataSource) => {
      const selectedDataSourceName = this.props.selectedDataSource && this.props.selectedDataSource.name;
      const selectorClass = (selectedDataSourceName === dataSource.name)
        ? 'selected'
        : null;
      return (
        <div
          key={dataSource.name}
          className={`content-option ${selectorClass}`}
          // tslint:disable-next-line:jsx-no-lambda
          onClick={() => this.props.setSelectedDataSource(dataSource.name)}
        >
          {dataSource.displayName}
        </div>
      );
    });
    return (
      <div className="content-options">
        {options}
      </div>
    );
  }
}
