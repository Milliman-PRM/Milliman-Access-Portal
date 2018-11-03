import * as React from 'react';
import { Checkbox, CheckboxProps } from '../shared-components/checkbox';

export interface FieldsetProps {
  name: string;
  fields: CheckboxProps[];
}

export class Fieldset extends React.Component<FieldsetProps> {
  public render() {
    const { name } = this.props;
    return (
      <fieldset>
        <legend>{name}</legend>
        {this.renderFields()}
      </fieldset>
    );
  }

  private renderFields() {
    const { fields } = this.props;
    return fields.map((field) => (
      <Checkbox key={field.name} {...field} />
    ));
  }
}
