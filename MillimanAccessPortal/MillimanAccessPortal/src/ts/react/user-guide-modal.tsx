import * as React from 'react';
import * as Modal from 'react-modal';

import '../../scss/react/shared-components/modal.scss';

interface UserGuideModalProps extends Modal.Props {
  source: string;
}

export class UserGuideModal extends React.Component<UserGuideModalProps, {}> {
  public render() {
    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
        className="modal modal-large"
        overlayClassName="modal-overlay"
      >
        <h3 className="title blue">User Guide</h3>
        <iframe
          className="content-frame"
          src={`/Documentation/${this.props.source}.html`}
        />
      </Modal>
    );
  }
}
