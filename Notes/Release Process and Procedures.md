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

Identifying high priority issues quickly is important to maintaining a stable application.  As issues are created, it is important for the project manager to identify the severity of the issue and prioritize them according to the following criteria:

- Does the issue represent a threat to application security?
- Does the issue prevent users from performing a vital task?
- Does the issue cause the user to receive incorrect results?
- Does the issue make the application appear less secure?

#### Milestone Creation

To better track upcoming work, it is important to create release Milestones for each major release.  As work is planned it should be tagged with the appropriate Milestone, and as pre-release branches are completed and merged they should be closed as part of the release process.

### Release Planning and Prioritization

There are many factors to take into account when planning for an upcoming release of the application. In general, feature releases are built around one or two main features, with several smaller ehancements and bug fixes added to round out the work.  These smaller parts of the release should be chosen based on priority, proximity or relationship to other work in the release, and the availability of development resources during the duration of the release cycle.  Finally, it is generally preferrable to keep releases small to keep turnaround time manageable.

#### Defining Requirements

Before starting development of a new feature or enhancement, it is important to make sure that the work is well-defined.  This should be accomplished by the creation of a set of requirements that outline what constitutes a completed feature.  Once the intial requirements have been authored, they should be signed off on by the project manager to ensure that the feature fits within the larger picture of the application.

#### Research / Proof of Concept

For larger features where there are significant unknowns, it is often a good time investment to research potential options and to create a proof of concept project to verify assumptions.  In general the time spent in this phase will pay off with smoother release cycles and a better end product.  Additionally, having filled in many of the unknowns, it is much easier to judge the amount of time needed to complete the feature.

### Development

#### Branching Strategies

To keep changes to the code base as easily reviewable as possible, it's important to keep branches focused. To accomplish this, branches should be opened with a specific purpose in mind and pull requests should have tasks laid out as checkboxes in the description at the top.  Furthermore, pull requests should be opened early and collaborators identified as soon as possible.

The MAP team, and PRM Analytics more broadly, follows the Git Flow branching strategy.  This pattern requires 2 permanent protected branches, develop and master, with master serving as the currently promoted code base and develop as the current stable working build.  As feature development is begun, a branch should be created off of the develop branch with a pull request back into develop upon completion.  In the case of a larger feature a feature collector branch can be created with branches coming off of the collector and then back in upon completion.  This larger collector branch will then be merged back into develop.  This should ensure that develop is always in a working state.  Once all of the features and bug fixes planned for a release have been reviewed and merged into develop, a pre-release branch should be created.  By convention, pre-release branches should be named "pre-release-vX.X.X" where X.X.X reflects the planned release version number.  Pre-release pull requests act as a Pre-Release Peer Review and should only ever be pointed at the master branch. If any issues are discoverd in pre-release that need to be addressed before release, a new branch should be created off of the pre-release branch and a pull request should be pointed back at the pre-release branch. 

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