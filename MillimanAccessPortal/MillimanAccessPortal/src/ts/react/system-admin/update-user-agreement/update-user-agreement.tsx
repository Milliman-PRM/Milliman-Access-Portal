import * as React from 'react';
import '../../../../../src/scss/update-user-agreement.scss';
import '../../../../images/map-logo.svg';
import '../../../../scss/map.scss';

export class UpdateUserAgreement extends React.Component {
    public constructor(props: {}) {
        super(props);
    }
    public render() {
        return (
            <div id="agreement-container" className="admin-panel-content-container">
                <form id="user-agreement-form" action="updateUserAgreement" method="POST">
                    <h3 className="section-title">User Agreement Text</h3>
                    <div className="markdown-view-button-container">
                        <span className="markdown-view-toggle markdown-select-edit selected">Edit</span>
                        <span className="markdown-view-toggle markdown-select-preview">Preview</span>
                    </div>
                    <div id="user-agreement-text">
                        <div id="AgreementPreview" /* style="display: none;" */ />
                    </div>
                    <div className="button-container">
                        <button id="update-button" type="submit" className="green-button">Update</button>
                    </div>
                </form>
            </div>
        );

    }

}
