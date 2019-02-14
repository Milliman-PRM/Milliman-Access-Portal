# Milliman Access Portal (MAP)

## Release Notes

### v1.4.0

- Reconfigure the NavBar to open on click instead of hover

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
