import * as React from 'react';

import '../../../../../src/scss/update-user-agreement.scss';
import '../../../../images/map-logo.svg';
import '../../../../scss/map.scss';
import { convertMarkdownToHTML } from '../../../../ts/convert-markdown';

export class UpdateUserAgreement extends React.Component {
    public state = {
        visible: true ,
    };

    public handleToggle = () => {
        this.setState({ visible: !this.state.visible });

    }
    public setToEdit = () => {
        const rawAgreementMarkdown = (document.getElementById('newAgreementText') as HTMLTextAreaElement).value;
        const processedAgreementHTML = convertMarkdownToHTML(rawAgreementMarkdown);
        document.getElementById('AgreementPreview').innerHTML = processedAgreementHTML;
    }

    public render() {
        const { visible } = this.state;
        return (
            <div id="agreement-container" className="admin-panel-content-container">
                <form id="user-agreement-form" action="updateUserAgreement" method="POST">
                    <h3 className="section-title">User Agreement Text</h3>
                    <div className="markdown-view-button-container">
                        <span
                            className={
                              `markdown-view-toggle markdown-select-edit'${visible ? ' selected' : ''}`}
                            onClick={() => {
                              this.handleToggle();
                              this.setToEdit();
                            }}
                        >
                        Edit
                        </span>
                        <span
                          className={
                            `markdown-view-toggle markdown-select-edit'${!visible ? ' selected' : ''}`}
                          onClick={this.handleToggle}
                        >
                        Preview
                        </span>
                    </div>
                    <div id="user-agreement-text">
                      <textarea
                            id="newAgreementText" // need to change
                       // disabled={visible ? false : true}
                      />
                    </div>
                    <div className="button-container">
                        <button id="update-button" type="submit" className="green-button">Update</button>
                    </div>
                </form>
            </div>
        );

    }

}
