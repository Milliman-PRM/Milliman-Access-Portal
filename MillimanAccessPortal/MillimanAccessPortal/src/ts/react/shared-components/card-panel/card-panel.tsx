import '../../../../scss/react/shared-components/card-panel.scss';

import * as React from 'react';

import { CardPanelSectionContent } from './card-panel-sections';
import { PanelSectionContainer } from './panel-sections';

export interface CardPanelProps<TEntity> {
  entities: TEntity[];
  renderEntity: (entity: TEntity, key: number) => JSX.Element;
}

export class CardPanel<TEntity> extends React.Component<CardPanelProps<TEntity>> {
  public render() {
    const { entities, renderEntity, children } = this.props;
    return (
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
