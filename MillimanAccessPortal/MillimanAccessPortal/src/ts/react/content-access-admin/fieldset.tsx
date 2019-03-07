import * as React from 'react';
import { Checkbox, CheckboxData } from '../shared-components/checkbox';

export interface FieldsetData {
  name: string;
  fields: CheckboxData[];
}
export interface FieldsetProps extends FieldsetData {
  readOnly: boolean;
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
    const { fields, readOnly } = this.props;
    return fields.map((field) => (
      <Checkbox key={field.name} {...field} readOnly={readOnly} />
    ));
  }
}
