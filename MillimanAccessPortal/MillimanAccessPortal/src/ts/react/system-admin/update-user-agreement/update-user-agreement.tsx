import * as React from 'react';

import '../../../../../src/scss/update-user-agreement.scss';
import { convertMarkdownToHTML } from '../../../../ts/convert-markdown';
import { postData } from '../../../shared';

interface UpdateUserAgreementState {
    visible: boolean;
    originalAgreementText: string;
    newAgreement: string;
}

export class UpdateUserAgreement extends React.Component<{}, UpdateUserAgreementState> {

    public constructor(props: {}) {
      super(props);

      this.state = {
        visible: true,
        originalAgreementText: '',
        newAgreement: '',
      };

      this.handleChangeMessage = this.handleChangeMessage.bind(this);
      this.handleSubmit = this.handleSubmit.bind(this);

    }

    public render() {
      const { visible } = this.state;
      return (
        <div id="agreement-container" className="admin-panel-content-container">
          <form id="user-agreement-form" onSubmit={this.handleSubmit}>
            <h3 className="section-title">User Agreement Text</h3>
            <div className="markdown-view-button-container">
              <span
                className={
                `markdown-view-toggle markdown-select-edit'${visible ? ' selected' : ''}`}
                onClick={this.handleToggle}
              >
                Edit
              </span>
              <span
                className={
                `markdown-view-toggle markdown-select-edit'${!visible ? ' selected' : ''}`}
                onClick={() => {
                this.handleToggle();
                this.setToPreview();
                }}
              >
                Preview
              </span>
            </div>
            <div id="user-agreement-text">
              <textarea
                id="newAgreementText"
                name="newAgreementText"
                style={{ display: !visible ? 'none' : '' }}
                onChange={this.handleChangeMessage}
                value={this.state.newAgreement}
              />
              <div
                id="AgreementPreview"
                style={{display: visible ? 'none' : ''}}
              />
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

    public componentDidMount() {
        const newAgreementText = (document.getElementById('userAgreementText') as HTMLTextAreaElement).value;
        this.setState({
            originalAgreementText: newAgreementText,
            newAgreement: newAgreementText,
        });
        document.getElementById('userAgreementText').innerHTML = newAgreementText;
    }

    private handleToggle = () => {
      this.setState({ visible: !this.state.visible });
    }

    private setToPreview = () => {
        const rawAgreementMarkdown = (document.getElementById('newAgreementText') as HTMLTextAreaElement).value;
        const processedAgreementHTML = convertMarkdownToHTML(rawAgreementMarkdown);
        document.getElementById('AgreementPreview').innerHTML = processedAgreementHTML;
    }

    private handleChangeMessage(event: React.ChangeEvent<HTMLTextAreaElement>) {
        this.setState({
            newAgreement: event.target.value,
        });
    }

    private handleSubmit(event: React.MouseEvent<HTMLFormElement> | React.KeyboardEvent<HTMLFormElement>) {
        event.preventDefault();
        event.persist();
        postData('/SystemAdmin/UpdateUserAgreement', { newAgreementText: this.state.newAgreement }, true)
            .then((response) => {
                if (response.ok) {
                    window.location.replace('/');
                }
            });
    }
}
