import '../../../../scss/react/shared-components/card-panel.scss';

import * as React from 'react';

import { LoadingSpinner } from '../loading-spinner';
import { CardPanelSectionContent } from './card-panel-sections';
import { PanelSectionContainer } from './panel-sections';

export interface CardPanelProps<TEntity> {
  entities: TEntity[];
  renderEntity: (entity: TEntity, key: number) => JSX.Element;
  loading?: boolean;
}

export class CardPanel<TEntity> extends React.Component<CardPanelProps<TEntity>> {
  public render() {
    const { loading, entities, renderEntity, children } = this.props;
    return  loading
    ? <LoadingSpinner />
    : (
      <PanelSectionContainer>
        {children}
        <CardPanelSectionContent>
          <ul className="admin-panel-content">
            {entities.map((entity, i) => renderEntity(entity, i))}
          </ul>
        </CardPanelSectionContent>
      </PanelSectionContainer>
    );
  }
}
