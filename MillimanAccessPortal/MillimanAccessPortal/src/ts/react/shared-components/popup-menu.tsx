import '../../../scss/react/shared-components/popup-menu.scss';

import '../../../images/icons/menu.svg';

import * as React from 'react';

// interface PopupMenuProps {
//   offsetX: string;
//   offsetY: string;
// }

interface PopupMenuState {
  isOpen: boolean;
}

export class PopupMenu extends React.Component<{}, PopupMenuState> {
  private popup = React.createRef<HTMLDivElement>();

  public constructor(props: any) {
    super(props);

    this.state = {
      isOpen: false,
    };
  }

  public openMenu = (event: React.MouseEvent) => {
    event.preventDefault();
    this.setState({ isOpen: true });
  }

  public closeMenu = () => {
    if (this.state.isOpen) {
      this.setState({ isOpen: false });
    }
  }

  public componentDidUpdate() {
    setTimeout(() => {
      if (this.state.isOpen) {
        window.addEventListener('click', this.closeMenu);
        window.addEventListener('touchend', this.closeMenu);
        window.addEventListener('scroll', this.closeMenu);
      } else {
        window.removeEventListener('click', this.closeMenu);
        window.removeEventListener('touchend', this.closeMenu);
        window.removeEventListener('scroll', this.closeMenu);
      }
    }, 0);
  }

  public componentWillUnmount() {
    window.removeEventListener('click', this.closeMenu);
    window.removeEventListener('touchstart', this.closeMenu);
    window.removeEventListener('scroll', this.closeMenu);
  }

  public render() {
    const { children } = this.props;
    const { isOpen } = this.state;

    return (
      <div className="popup-container">
        <span
          onClick={this.openMenu}
          className="menu-button"
        >
          <svg className="menu-icon">
            <use xlinkHref={'#menu'} />
          </svg>
        </span>
        {
          isOpen &&
          <div
            onClick={this.closeMenu}
            ref={this.popup}
            className={'popup-menu'}
          >
            {children}
          </div>
        }
      </div>
    );
  }
}
