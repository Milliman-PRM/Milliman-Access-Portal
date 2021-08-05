import * as React from 'react';

import '../../../../../src/scss/update-user-agreement.scss';

import { convertMarkdownToHTML } from '../../../../ts/convert-markdown';
import { postData } from '../../../shared';
import { TabRow } from '../../shared-components/tab-row';

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
    }

    public componentDidMount() {
        const newAgreementText = (document.getElementById('userAgreementText') as HTMLTextAreaElement).value;
        this.setState({
            originalAgreementText: newAgreementText,
            newAgreement: newAgreementText,
        });
    }

    public render() {
      const { viewSelect } = this.state;
      return (
        <div id="agreement-container" className="admin-panel-content-container">
          <form id="user-agreement-form" onSubmit={(evt) => this.handleSubmit(evt)}>
            <h3 className="section-title">User Agreement Text</h3>
            <div className="markdown-view-button-container">
              <TabRow
                tabs={[{ id: 'edit', label: 'Edit' }, { id: 'preview', label: 'Preview' }]}
                selectedTab={viewSelect}
                onTabSelect={(tab: 'edit' | 'preview') => this.tabSelect( tab )}
                fullWidth={true}
              />
            </div>
            <div id="user-agreement-text">
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
              {
                this.state.errorMessage &&
                <div className="error-message">
                  {this.state.errorMessage}
                </div>
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

    private tabSelect = (selectedTab: 'edit' | 'preview') => {
        const agreementHTML = (selectedTab === 'preview') ? convertMarkdownToHTML(this.state.newAgreement) : null;
        this.setState({
            viewSelect: selectedTab,
            previewHTML: agreementHTML,
        });
    }

    private handleChangeMessage(event: React.ChangeEvent<HTMLTextAreaElement>) {
        this.setState({
            newAgreement: event.target.value,
        });
    }

    private handleSubmit(event: React.FormEvent<HTMLFormElement>) {
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
