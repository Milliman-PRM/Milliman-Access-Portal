# Milliman Access Portal (MAP)

## Release Notes

### v1.10.0

- Introduced a user interface for interacting with File Drops in MAP
- Improved the formatting of emails notifying about expired or soon to expire client access review deadlines

#### v1.10.1

- Fixed a bug causing two factor authentication to fail when no subsequent URL is included in the login request
- Modified the URL of login requests to prevent internal logging of user passwords
- Fixed a bug that prevented users with newly assigned client admin role from adding a new user to the system
- Improved the content of the notification email for expired or approaching client access review deadlines

### v1.10.0

- Introduced HITRUST compliance features
  - Two factor authentication for all users
  - Logging of administrator selected reasons for all user role changes
  - Periodic administrative reviews of user access for all clients
  - Locking of user accounts after 1 year of inactivity
- Fixed a bug that allowed a user to remove employer and phone number information from their user profile
- Fixed a bug that included Clients in the file drop view for which a past file drop authorization had been revoked

#### v1.9.3

- Added a workaround for an uncommon problem where a user password reset cannot be submitted
- Modified the MAP application log to use event time stamps with UTC time zone

#### v1.9.2

- Allow HTML and Power BI content items to generate downloadable reports

#### v1.9.1

- Fixed a bug that caused the SFTP server configuration file to not deploy in production
- Enabled configurable logging of internal details

### v1.9.0

- Introduced a new File Drop feature
- Enhanced the resiliency of interactions between MAP and Qlikview server
- Fixed a bug that inaccurately displayed the queue order of publication requests in certain circumstances

#### v1.8.1

- Fixed a bug that inaccurately displayed error status for some content items in the Content Access Admin view

### v1.8.0

- Redesigned the Content Publishing form to improve the user experience of creating and editing Content Items
- Added the ability to preview reductions during the pre-live review and approval process
- Added the ability to review thumbnail changes as part of the pre-live review and approval process
- Added the ability to cancel publications during the majority of the publishing process
- Added loading bars to content while it is loading
- Restructured the Content Publishing process to improve general stability and robustness
- Improved error handling in the Content Publishing process to better handle reductions that fail for a variety of reasons
- Improved the resiliency of the Content Publishing process in handling temporary communications interruptions with the Qlikview Publishing Server
- Added the ability for queued and processing publications and reductions to resume after the MAP application restarts
- Improved the publication process by allowing reductions resulting in certain minor errors to become inactive when appropriate without causing a failed publication
- Improved the publication process by failing publications immediately upon errors that cannot be resolved
- Improved messaging for publications and reductions that result in warnings or errors
- Fixed an issue that made it difficult to successfully republish following certain publication failures
- Added the ability to modify Selection Group membership while a publication is actively being processed

#### v1.7.3

- Fixed a bug that allowed a perpetual email sending loop for a user requesting password reset when the user's spam filter followed the embedded link after the link was expired
- Fixed an issue where an errant equal sign was being included in the URL prevented proper token validation

#### v1.7.2

- Prevent accidental submission of multiple selection group reductions in the Content Access Admin view
- The response to a selection change request for reduced content is much faster to provide timely user feedback
- A new status (Validating) has been added to selection change logic, before Queued.  For smaller content, the user may see the status change directly to Queued since validation will be fast

#### v1.7.1

- Improved feedback to user on password reset failure

### v1.7.0

- Added a User Agreement
- Added support for markdown for content disclaimers
- Added the ability to select all, deselect all, and reset selections for reducible QlikView content items in Content Access Admin
- Added ability to show the Bookmarks pane in PowerBI reports
- Allow `<iframe>` to render PDFs in a new window
- Clarified several user messages in the password reset workflow
- Fixed a bug that prevented SSO users from logging into any view other than Authorized Content
- Fixed a bug that allowed the password reset form to be displayed using an expired email link
- Fixed an issue that allowed whitespace on username inputs
- Fixed the styling on republishing icons
- Fixed a bug in displaying user guide contents

### v1.6.0

- Introduced support for PowerBI as a new content type
- Implemented a mechanism for controlling the number of domains that can be added to a client email domain whitelist
- Fixed an issue preventing content publication post-processing from running in parallel
- Redesigned the Forgot Password page
- Redesigned the Reset Password page

#### v1.5.1

- Fixed an issue where user login fails in certain unusual circumstances

### v1.5.0

- Add support for Federated Authentication through external authentication providers
- Redesigned the login page to support externally authenticated users
- Redesigned the account settings page to better support externally authenticated users
- Redesigned the account authorization workflow to support externally authenticated users
- Fixed an issue that allowed users to be added to the same selection group multiple times

#### v1.4.1

- Allow buttons on content card to wrap

### v1.4.0

- Add the ability to set a custom disclaimer to content items that must be accepted before accessing the content
- Restructured Content Access Admin to improve usability and responsiveness
- Reconfigure the NavBar to open on click instead of hover
- Reconfigure the Content Card layout to make accessing content more intuitive
- Unlock locked accounts when the password is reset
- Clear content previews when switching to go live preview for a different content item
- Fixed an issue where existing account activation links became invalidated before 7 days had passed
- Improved logging of user access to content

#### v1.3.6

- Mitigate a potential concurrency issue in email service

#### v1.3.5

- Add Application Insights telemetry
- Add further logging around middleware and action timing
- Add diagnostic logging around reduction post-processing pipeline

#### v1.3.4

- Prevent forced signout while a content item is open in the Content view

#### v1.3.3

- Add audit logging for content access

#### v1.3.2

- Enable the user to retry after an error occurs during content publication go-live

#### v1.3.1

- Fixed an issue where an error during publication go-live left the publication stuck in going-live state and the related content inaccessible
- Fixed an issue where new users appeared to be able to submit a password during account activation that did not meet the password requirements
- Fixed an issue where a user attempting to log in to an account that had not been activated erroneously displayed an account lockout message
- Fixed an issue where usernames were case sensitive when adding users to selection groups
- Added the MAP site URL to the welcome email text to help users find the site after their account has been activated
- Removed the expand icon from the Go Live preview for File Upload content types

### v1.3.0

- Publication requests involving reduction errors will no longer be aborted
- Added the ability to put selection groups in an inactive state
- Added additional information about Selection Group reduction outcomes to the Go-Live preview for reduced content
- Non-reducing content publications will now be published independently of the reducible content items
- Improved process efficiency for reducing content items
- Fixed an issue where the status of a queued publication changed to 'Processing' before processing could actually begin
- Fixed an issue where a timeout of a hierarchy extraction task would cause the publication service to crash

### v1.2.0

- Added support for PDF content type
- Added support for HTML content type
- Added support for downloadable file content type
- Added the ability to delete previously published supporting files from a content item
- Added the ability to open content and supporting files in a new browser tab
- Removed the close button in the content view to avoid blocking content
- Fixed an issue where long content names were not being displayed properly in the content view
- Removed status polling for hidden pages
- Adjusted the navigation bar size to a fixed fraction of the screen to accommodate browser magnification changes

#### v1.1.5

- Fixed an error during republished content go-live, where a hierarchy field value being removed exists in more than one live hierarchy field

#### v1.1.4

- Fixed an issue preventing nonreducing content from going live

#### v1.1.3

- Fixed an issue where content go-live processing did not complete for large content files and/or many selection groups

#### v1.1.2

- Allow hierarchy comparison values to wrap in the Go Live preview to better support long values
- Fixed an issue with Qlikview content where a special character (e.g. ~ ' ") in a selectable value of a reduction hierarchy field would become quoted
- Fixed an issue with upload of large content files
- Fixed an issue where long running Qlikview reductions would fail to complete

#### v1.1.1

- Fixed an issue preventing content item details to be changed without uploading a file

### v1.1.0

- Queue information now available for content items in the process of being published
- Improved the flow of publication requests to allow non-reducible content to be processed before reducible content
- Improved exception logging in the MAP application
- Addressed issue with URL encoding of file names during publishing
- Improved the behavior of the Forgot Password workflow to prevent innadvertently acknowleding accounts exist between their creation and activation
- Fixed an issue where a publication could be submitted before files were finished uploading
- Adjusted card stats in the Content Publisher and Content Access Admin views to show more useful information
- Added sorting for Content Items and Selection Groups
- Restructured the System Admin view to enable future enhancements
- Fixed an issue preventing a confirmation notification from being displayed in the System Admin view
- Fixed text alignment in System Admin view
- Added the ability to remove users from clients in System Admin
- Unified the behavior of removing the Content Eligible role between Client Admin and System Admin

#### v1.0.3

- Account enablement now occurs transactionally with error logging in case of failure

#### v1.0.2

- Improved stability of publishing server

#### v1.0.1

- Improved exception handing in the audit logger

### v1.0.0

- Intitial Release of the Milliman Access Portal (MAP)
