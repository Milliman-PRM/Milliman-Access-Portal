import '../../../../scss/react/shared-components/card-panel.scss';

import * as React from 'react';

import { CardAttributes } from '../card/card';
import { CardPanelSectionContainer, CardPanelSectionContent } from './card-panel-sections';

export interface CardPanelProps<TEntity> {
  cards: {
    [id: string]: CardAttributes;
  };
  entities: TEntity[];
  renderEntity: (entity: TEntity, key: number) => JSX.Element;
}

export class CardPanel<TEntity> extends React.Component<CardPanelProps<TEntity>> {
  public render() {
    const { entities, renderEntity, children } = this.props;
    return (
      <CardPanelSectionContainer>
        {children}
        <CardPanelSectionContent>
          <ul className="admin-panel-content">
            {entities.map((entity, i) => renderEntity(entity, i))}
          </ul>
        </CardPanelSectionContent>
      </CardPanelSectionContainer>
    );
  }

}
