import * as React from 'react';
import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

export class CreateInitialUser extends React.Component {
    public render() {
        return (
            <div id="create-initial-user-container">
                <div
                  id="create-initial-user"
                  className={'admin-panel-container flex-item-for-phone-only-12-12 ' +
                  'flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-4-12'}
                >
                  <div className="form-content-container">
                    <form
                      autoComplete="off"
                      asp-controller="Account"
                      asp-action="CreateInitialUser"
                      asp-route-returnurl="@ViewData['ReturnUrl']"
                      method="post"
                      className="form-horizontal"
                    >
                      <div className="form-section-container">
                        <div className="form-section">
                          <h4 className="form-section-title">Create a new account</h4>
                          <div asp-validation-summary="All" className="text-danger"/>
                          <div className="form-input-container">
                            <div className="form-input form-input-text flex-item-12-12">
                               <label asp-for="Email" className="form-input-text-title"/>
                               <div className="col-md-10">
                                 <input asp-for="Email" className="form-control"/>
                                 <span asp-validation-for="Email" className="text-danger"/>
                               </div>
                            </div>
                          </div>
                        </div>
                        <div className="form-submission-section">
                          <div className="button-container button-container-update">
                            <button type="submit" className="button-submit blue-button">Create User</button>
                          </div>
                        </div>
                      </div>
                    </form>
                  </div>
                </div>
            </div>
        );
    }

}
