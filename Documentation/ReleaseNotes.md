# Milliman Access Portal (MAP)

## Release Notes

### v1.1.0

- Queue information now available for content items in the process of being published
- Improved the flow of publication requests to prevent reducible content from blocking non-reducible content
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

