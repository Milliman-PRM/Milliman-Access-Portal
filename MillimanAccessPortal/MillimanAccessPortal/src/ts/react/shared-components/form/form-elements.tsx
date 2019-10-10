import * as React from 'react';

interface ContentPanelFormProps {
  readOnly?: boolean;
}

export const ContentPanelForm: React.SFC<ContentPanelFormProps> = (props) => (
  <form autoComplete="off" className={`${props.readOnly ? 'form-disabled' : ''}`}>
    {props.children}
  </form>
);

interface FormSectionProps {
  title?: string;
}

export const FormSection: React.SFC<FormSectionProps> = (props) => (
  <div className="form-section">
    {props.title && <div className="form-section-title">{props.title}</div>}
    {props.children}
  </div>
);

export const FormSectionRow: React.SFC = (props) => (
  <div className="form-section-row">
    {props.children}
  </div>
);

export const FormSectionDivider: React.SFC = (props) => (
  <div className="form-section-divider">
    {props.children}
  </div>
);

interface FormFlexContainerProps {
  alignItems?: 'flex-start' | 'flex-end';
  contentItemFlex?: 'none' | 1 | 2;
  flex?: boolean;
  flexPhone?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
  flexTablet?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
  flexDesktop?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
}

export const FormFlexContainer: React.SFC<FormFlexContainerProps> = (props) => {
  const cssAlign = (props.alignItems) ? `${props.alignItems}` : '';
  const cssPhone = (props.flexPhone) ? ` flex-item-${props.flexPhone}-12` : '';
  const cssTablet = (props.flexTablet) ? ` flex-item-for-tablet-up-${props.flexTablet}-12` : '';
  const cssDesktop = (props.flexDesktop) ? ` flex-item-for-desktop-up-${props.flexDesktop}-12` : '';
  const cssFlex = (props.flex) ? ' form-input-flex' : '';
  const cssCIFlex = (props.contentItemFlex) ? ` content-item-flex-${props.contentItemFlex}` : '';

  return (
    <div className={`form-section-divider${cssAlign}${cssPhone}${cssTablet}${cssDesktop}${cssFlex}${cssCIFlex}`}>
      {props.children}
    </div>
  );
};
