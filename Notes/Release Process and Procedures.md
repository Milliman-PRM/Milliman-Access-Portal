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

The purpose of unit tests are to ensure that pieces of the application are behaving as expected.  Ideally, they also act as a mechanism for identifying regression in the behavior of the application.  With this in mind, all new features should have unit tests written to test actions for both success and failures modes.  At this time, unit testing in MAP is focused mainly on back end testing, since this is considered the ultimate source of truth.

These tests will be run as part of the CI process, and no Pull Request should be merged into the Develop or Master branches with failing checks.  The passage of CI will be indicated by a green checkmark next to the commit in the GitHub Pull Request.

#### Pull Requests

The purpose of a Pull Request is to act as a channel for collaboration and review before the changes are merged back into the code base.  In the spirit of collaboration Pull Requests should be opened early in the life cycle of a new branch.  Opening branches early allows you to identify and bring in collaborators early, which can help make your work better and reviews faster.

When opening a new Pull Request, you should first give it a name that helps to describe what the Pull Request is for.  Second, in the description of Pull Request, you should describe the problem that the Pull Request is meant to address and any other relevant information necessary to understand the changes.  Below the description you should create a checklist that lists all of the major tasks necessary to complete the work required to address the stated issue.  The last check in the task list should be a definition of done, which is a simple statement that describes when the work is truly done.  Additionally, if there are any open Issues related to the work being done in the Pull Request, then they should all be tagged at the bottom of the Pull Request description.  Finally, you should add your collaberators to the Pull Request, tag the issue with any appropriate labels, assign it to the MAP Kanboard project, and then specify the correct milestone.

### Pre-release / User Acceptance Testing (UAT)

#### Creating the Pre-Release Pull Request

Once the Develop branch is feature complete for the upcoming release, a Pre-Release Pull Request should be opened and targeted at the Master branch.  The Pre-Release Pull Request is a special type of Pull Request that is used to track the progress of the release process and to record the Pre-Release Peer Review and any documentation that may be necessary for completion of the release.  To open a Pre-Release Pull Request, a new branch must be created off of Develop, and should be named `pre-release-vX.X.X` (with the appropriate version number for the release).  Once this branch is created, a Pull Request should be opened.  Similarly to the branch name, the Pull Request should be named "Pre-Release vX.X.X".  In the description of the new Pull Request, you should paste the contents of the Pre-Release Pull Request template that can be found here:

- https://indy-github.milliman.com/raw/PRM/Milliman-Access-Portal/develop/.github/PRE_RELEASE_CHECKLIST_TEMPLATE.md

Once the template has been copied and pasted into the description of the Pull Request, the contents should be updated to reflect the contents of the proposed release.  This would include updating the release number, tagging any users that will be completing any work in any of the sections, and adding/removing any tasks specific to this release.  Finally, the correct Milestone should be tagged, and the Pull Request should be added to the MAP Kanboard Project.

#### Determining Release Version Number

Milliman Access Portal follows a semantic version numbering system.  This means that the position of the number indicates the type of change from the previous version.  In traditional distributed software applications, the first position is used to indicate breaking changes.  In a web application this isn't as meaningful, so MAP will use changes to this position to indicate major redesigns that would have far-reaching impacts on the users experience of the application.  This will likely be a rare occurence.  The second position is used to indicate a release that includes new features.  Feature releases should happen on a fairly regular basis for the forseeable future.  The final position indicates a patch, or a release containing bug fixes or small enhancements to existing functionality.  Historically we have seen 2 or 3 patch releases for each feature release.  Determining the release number of an upcoming release, based on the above information, should be a fairly strightforward process.

#### Determining Risk Level

The Risk Level of a given release is used to identify the amount of risk involved with the proposed changes to the application.  Determining the Risk Level of a release is based on the judgment of the authorized individual reviewing the release, and can only be performed by someone with Signature Authority.  While software releases are substantially different from the typical consulting work done within Milliman, the Risk Levels themselves act as a useful indicator and guide for is required to perform the Pre-Release Peer Review.

#### Penetration Testing

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
