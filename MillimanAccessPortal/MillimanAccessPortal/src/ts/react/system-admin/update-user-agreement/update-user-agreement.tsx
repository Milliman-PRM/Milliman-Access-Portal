import * as React from 'react';

import '../../../../../src/scss/update-user-agreement.scss';
import '../../../../images/map-logo.svg';
import '../../../../scss/map.scss';
import { convertMarkdownToHTML } from '../../../../ts/convert-markdown';
import { postData } from '../../../shared';

interface UpdateUserAgreementState {
    visible: boolean;
    newAgreement: string;

}

export class UpdateUserAgreement extends React.Component<{}, UpdateUserAgreementState> {

    public constructor(props: {}) {
      super(props);

      this.state = {
        visible: true,
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
                        style={{display: !visible ? 'none' : ''}}
                        onChange={this.handleChangeMessage}

                      />
                      <div
                        id="AgreementPreview"
                        style={{display: visible ? 'none' : ''}}
                      />
                    </div>
                    <div className="button-container">
                        <button id="update-button" type="submit" className="green-button">Update</button>
                    </div>
                </form>
            </div>
        );

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
       // const { newAgreement } = this.state.newAgreement;
        postData('/SystemAdmin/UpdateUserAgreement', { newAgreement: this.state.newAgreement }, true)
            .then((response) => {
                if (response.ok) {
                    window.location.replace('/');
                }
            });
    }

}
