import * as React from 'react';

import '../../../../../src/scss/update-user-agreement.scss';

import { convertMarkdownToHTML } from '../../../../ts/convert-markdown';
import { postData } from '../../../shared';

interface UpdateUserAgreementState {
    viewSelect: 'edit' | 'preview';
    originalAgreementText: string;
    newAgreement: string;
    previewHTML: string;
    errorMessage: string;
}

export class UpdateUserAgreement extends React.Component<{}, UpdateUserAgreementState> {

    public constructor(props: {}) {
      super(props);

      this.state = {
        viewSelect: 'edit',
        originalAgreementText: '',
        newAgreement: '',
        previewHTML: '',
        errorMessage: '',
      };
      this.handleSubmit = this.handleSubmit.bind(this);

    }

    public render() {
      const { viewSelect } = this.state;
      return (
        <div id="agreement-container" className="admin-panel-content-container">
          <form id="user-agreement-form" onSubmit={this.handleSubmit}>
            <h3 className="section-title">User Agreement Text</h3>
            <div className="markdown-view-button-container">
              <span
                className={
                `markdown-view-toggle markdown-select-edit'${viewSelect === 'edit' ? ' selected' : ''}`}
                onClick={this.setToEdit}
              >
                Edit
              </span>
              <span
               // TODO change span to tab row
                className={
                `markdown-view-toggle markdown-select-edit'${viewSelect === 'preview' ? ' selected' : ''}`}
                onClick={this.setToPreview}
              >
                Preview
              </span>
            </div>
            <div id="user-agreement-text">
              {
                this.state.errorMessage &&
                <div className="error-message">
                {this.state.errorMessage}
                </div>
               }
               {
                viewSelect === 'edit' ?
                  <textarea
                    id="newAgreementText"
                    name="newAgreementText"
                    onChange={(evt) => this.handleChangeMessage(evt)}
                    value={this.state.newAgreement}
                  />
                  :
                  <div dangerouslySetInnerHTML={{ __html: this.state.previewHTML }} />
              }
            </div>
            <div className="button-container">
              <button
                id="update-button"
                type="submit"
                className="green-button"
                disabled={this.state.originalAgreementText === this.state.newAgreement}

              >
                Update
              </button>
            </div>
          </form>
        </div>
        );
    }

    private setToEdit = () => {
        this.setState({
            viewSelect: 'edit',
        });
    }

    private setToPreview = () => {
        const processedAgreementHTML = convertMarkdownToHTML(this.state.newAgreement);
        this.setState({
            previewHTML: processedAgreementHTML,
            viewSelect: 'preview',
        });
    }

    private handleChangeMessage(event: React.ChangeEvent<HTMLTextAreaElement>) {
        this.setState({
            newAgreement: event.target.value,
        });
    }

    private handleSubmit(event: React.MouseEvent<HTMLFormElement> | React.KeyboardEvent<HTMLFormElement>) {
        event.preventDefault();
        postData('/SystemAdmin/UpdateUserAgreement', { newAgreementText: this.state.newAgreement }, true)
            .then((response) => {
                if (response.ok) {
                    window.location.replace('/');
                }
            })
            .catch(() => {
                this.setState({
                    errorMessage: 'an error occured',
                });
            });
    }
}
