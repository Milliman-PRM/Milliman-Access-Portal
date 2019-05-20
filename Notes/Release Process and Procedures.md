# Development and Release Procedures

## Purpose

The purpose of this document is to detail the process by which a release of Milliman Access Portal (MAP) progresses through the software development lifecycle.

## Additional Resources

## Release Process

### Backlog Management

The first step in the process of creating a release candidate is maintaining the pool of tasks yet to be completed.  The issue backlog is where all known bugs, enhancements, and new feature requests are kept. To maximize the usefulness of the issue backlog, it is important to periodically review the contents and make them as accessible as possible to the development team.

#### Creating Issues

As bugs and features are identified, new issues should be created to describe all of the relevant information required to perform the work required to complete the given task.  

For issues created relating to bugs, a description of the issue, the circumstances necessary to recreate the bug, the potential scope of impact, and any other mitigating information should be included in the description.  Additionally, new bugs should be assigned to the project manager immediately upon creation to make a determination on the urgency of the fix.

Issues created to describe enhancements to existing functionality or new features should include a description of the proposed functionality, the potential impact, the reason why the feature is necessary, and any clients that have requested the particular feature or enhancement.  The project manager will make the final determination on whether the proposal provides sufficient benefit to justify the work required to complete and will prioritize accordingly.

#### Issue Labeling

To increase the effectiveness of the issue backlog, it is helpful to categorize issues by their attributes.  This is accomplished through the application of labels to the issues.  By categorizing issues by type, difficulty, technology, and any of a number of other attributes it becomes considerably easier to identify the issues that should be prioritized.  At a minimum each issue should be marked with a difficulty, a type, and any technologies that may be involved.

#### Issue Triage

#### Milestone Creation

To better track upcoming work, it is important to create release Milestones for each major release.  As work is planned it should be tagged with the appropriate Milestone, and as pre-release branches are completed and merged they should be closed as part of the release process.

### Release Planning and Prioritization

#### Determining Business Need

#### Defining Requirements

#### Research / Proof of Concept

### Development

#### Branching Strategies

#### Unit Tests

#### Pull Requests

### Pre-release / User Acceptance Testing (UAT)

#### Determining Release Version Number

#### Determining Risk Level

#### Penetration Testing

#### Creating the Pre-release Pull Request

#### Deployment to Staging

#### Application Testing

#### Browser Testing

#### Triaging Issues Identified in Testing

#### Pre-release Peer Review

### Promotion

#### Tagging the Release

#### Notification of Release

### Deployment

#### Scheduling the Deployment

#### Applying Migrations

#### Deploying the New Release