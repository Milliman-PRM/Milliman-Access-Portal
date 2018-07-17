import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../scss/react/shared-components/card.scss';

import * as React from 'react';

export class Card<T> extends React.Component<CardProps<T>, {}> {
  public constructor(props) {
    super(props);
  }
}

interface CardProps<T> {
  data: T;
}
