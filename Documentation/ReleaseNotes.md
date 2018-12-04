# Milliman Access Portal (MAP)

## Release Notes

### v1.2.0

- Added support for PDF content type
- Added support for HTML content type
- Added support for downloadable file content type
- Added the ability to delete previously published supporting files (e.g. release notes, etc.) from a content item
- Added the ability to open content and related files (e.g. user guide) in a new browser tab
- Removed the close button in the content view to avoid blocking content
- Fixed an issue where long content names were not being displayed properly in the content view
- Removed status polling for hidden pages

### v1.1.5

- Fixed an error during republished content go-live, where a hierarchy field value being removed exists in more than one live hierarchy field

### v1.1.4

- Fixed an issue preventing nonreducing content from going live

### v1.1.3

- Fixed an issue where content go-live processing did not complete for large content files and/or many selection groups

### v1.1.2

- Allow heirarchy comparison values to wrap in the Go Live preview to better support long values
- Fixed an issue with Qlikview content where a special character (e.g. ~ ' ") in a selectable value of a reduction hierarchy field would become quoted
- Fixed an issue with upload of large content files
- Fixed an issue where long running Qlikview reductions would fail to complete

### v1.1.1

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

### v1.0.3

- Account enablement now occurs transactionally with error logging in case of failure

### v1.0.2

- Improved stability of publishing server

### v1.0.1

- Improved exception handing in the audit logger

### v1.0.0

- Intitial Release of the Milliman Access Portal (MAP)

