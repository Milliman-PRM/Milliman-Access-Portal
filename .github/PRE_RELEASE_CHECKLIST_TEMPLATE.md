# Pre-Release Pull Request

## Purpose

- Get and document approval for the new release of Milliman Access Portal (MAP)

## Release Version

- *vX.X.X*

## URL of UAT Environment

- https://map-uat-app.trafficmanager.net/

## Tasks and Assertions

### Pre-Review

*This section should be completed by a developer, and is intended to prepare the new release for review.*

Completed by: ___________

##### Tasks

- [ ] Deploy application to the UAT environment for testing
- [ ] Ensure the correct URL to the UAT system in this pull request above
- [ ] Fill in the release version above and name this pull request `Pre-Release - MAP vX.X.X` matching this release version
- [ ] Prepare formal UAT document
- [ ] Complete all applicable testing 
- [ ] Create issues for any bugs found, prioritize them, and add issues to be fixed in this version to the release milestone
- [ ] Address all issues prioritized for this release milestone and deploy to the UAT environment
- [ ] Check that issues for this milestone have been resolved in UAT
- [ ] Identify peer reviewer(s) by adding to requested reviewers of this pull request
- [ ] Communicate changes and documentation location to peer reviewers
- [ ] Create logins for reviewer(s)
- [ ] Hand off to Peer Reviewer(s)

##### Assertions

- [ ] The Release Version and URL have been updated in this pull request above
- [ ] All tests are passing or outstanding issues prioritized to be addressed later
- [ ] The software version and release versions match
- [ ] The release notes are up to date and reflect the changes included in this release
- [ ] The UAT Documentation has been prepared for the reviewer(s)
- [ ] All developers who have completed this section have been documented above
- [ ] The UAT instance deployed successfuly and is working properly

### Peer Review

*This section should be completed by the reviewer, and is intended to track review progress.*

Completed by: ___________

##### Tasks

- [ ] Determine risk level
- [ ] Perform peer review activities
- [ ] Each reviewer documents your Peer Review with appropriate attestation language in a comment below

##### Assertions

- [ ] The release has been documented appropriately
- [ ] All reviewers have been documented above
- [ ] The risk level for this release has been documented in this pull request
- [ ] All necessary reviewers have added their Peer Review Attestation below

### Post-Review Tasks

*This section should be completed by a developer, and is intended to ensure that the release is promoted and properly documented.*

Completed by: ___________

##### Tasks

- [ ] Leave a comment in the `MAP (Other than engineering)` Teams channel, tagging the Account Manager(s), with release notes for this new version and any needed additional information to be communicated to clients
- [ ] Tag the release in the `master` branch with the new version number
- [ ] Open a pull request to merge `master` to `develop` and obtain a successful CI run (creates the release in the deployment system)
- [ ] Request and confirm deployment to production using [this form](https://hd.milliman.com/support/catalog/items/161). The requested deployment date/time should be no sooner than the next business day, unless a same-day deployment is agreed upon by the Infrastructure & Security team lead.
- [ ] Leave a comment tagging the Account Manager in this PR stating when the deployment is scheduled to take place
- [ ] Merge released code base into `develop` to make sure warm changes are propagated
- [ ] Schedule a retrospective meeting

##### Assertions

- [ ] All tasks have been completed
- [ ] All developers that have completed this section have been documented above
