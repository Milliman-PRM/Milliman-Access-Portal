import * as React from 'react';
import * as Modal from 'react-modal';

import '../../scss/react/shared-components/modal.scss';

interface UserGuideModalProps extends Modal.Props {
  source: string;
}

export class UserGuideModal extends React.Component<UserGuideModalProps, {}> {

  private readonly recipient: string = 'support.78832.5ad4ee0bf11242a6@helpscout.net';

  public render() {
    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
        className="modal modal-large"
        overlayClassName="modal-overlay"
      >
        <iframe
          className="content-frame"
          src={`/Documentation/${this.props.source}.html`}
        />
      </Modal>
    );
  }
}
