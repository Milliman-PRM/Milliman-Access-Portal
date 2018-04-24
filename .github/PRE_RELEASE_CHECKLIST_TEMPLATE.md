# Pre-Release Pull Request

## Purpose

- Get and document approval for the new release of the Milliman Access Portal (MAP)

## Release Version

- *vX.X.X*

## Staging URL

- *URL to Staging Instance*

## Tasks and Assertions

### Pre-Review

*This section should be completed by a developer, and is intended to prepare the new release for review.*

Completed by: ___________

##### Tasks

- [ ] Deploy application to staging for testing and paste URL in pull request
- [ ] Prepare formal UAT document
- [ ] Complete the testing checklist
- [ ] Create issues for any bugs found, prioritize them, and add to release milestone
- [ ] Address all remaining issues to complete the release milestone
- [ ] Check that issues have been resolved in staging
- [ ] Identify reviewer(s) and communicate changes to them
- [ ] Hand off to Peer Reviewer(s)
- [ ] Create logins for reviewer(s)

##### Assertions

- [ ] All tests are passing
- [ ] The software version and release versions match
- [ ] The release notes are up to date and reflect the changes included in this release
- [ ] The UAT Documentation has been prepared for the reviewer(s)

### PRPR

*This section should be completed by the reviewer, and is intended to track review progress.*

Completed by: ___________

##### Tasks

- [ ] Determine risk level
- [ ] Sign off on release

##### Assertions

- [ ] The release has been documented appropriately

### Post-Review Tasks

*This section should be completed by a developer, and is intended to ensure that the release is promoted and properly documented.*

Completed by: ___________

##### Tasks

- [ ] Schedule release deployment
- [ ] Merge into Master and promote released code base
- [ ] Tag release
- [ ] Merge into Develop to make sure warm changes are propagated

##### Assertions

- [ ] All tasks have been completed
- [ ] All assertions have been met
