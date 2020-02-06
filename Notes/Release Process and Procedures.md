# Development and Release Procedures

## Purpose

The purpose of this document is to detail the process by which a release of the Milliman Access Portal (MAP) web application and publishing service progress through the software development lifecycle.

## Release Process

The process of creating new features and improving the overall functionality of MAP involves significantly more than simply writing code.  To create and maintain a high-quality application takes a lot of planning and coordination, smart development practices, and thorough testing.

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

If a determination is made that the issue justifies the creation of a hotfix, then a branch should be opened immediately off of the Master branch and named according to our standard pre-release branch naming conventions outlined in the Pre-Release section of this document.  This branch should follow our standard Pre-Release process with two exceptions.  First, the Pre-Release Pull Request should be tagged with the "Type: Hotfix" label to indicate that this is a high priority pull request.  Second, in order to expedite the release of the hotfix, a determination may be made by the project manager and peer reviewers regarding the amount and scope of the testing necessary to approve the release.  This will be highly dependent on the scope of the changes necessary, the type of changes, and the potential impacts of the changes.

In the event that a security vulnerability is identified, the Project Manager and Infrastructure and Security team will make a joint determination on whether the application should be made unavailable until a fix is in place.  The application will be shut down if either party believes that the vulnerability necessitates that course of action.  This determination will need to be made on a case-by-case basis and will be influenced by factors such as the severity of the vulnerability, the complexity of the circumstances necessary to reproduce, and any other relevant factors.

#### Milestone Creation

To better track upcoming work, it is important to create release Milestones for each major release.  As work is planned it should be tagged with the appropriate Milestone, and as pre-release branches are completed and merged they should be closed as part of the release process.

### Release Planning and Prioritization

There are many factors to take into account when planning for an upcoming release of the application. In general, feature releases are built around one or two main features, with several smaller enhancements and bug fixes added to round out the work.  These smaller parts of the release should be chosen based on priority, proximity or relationship to other work in the release, and the availability of development resources during the duration of the release cycle.  Finally, it is generally preferable to keep releases small to keep turnaround time manageable.

#### Defining Requirements

Before starting development of a new feature or enhancement, it is important to make sure that the work is well-defined.  This should be accomplished by the creation of a set of requirements that outline what constitutes a completed feature.  Once the initial requirements have been authored, they should be signed off on by the project manager to ensure that the feature fits within the larger picture of the application.

#### Research / Proof of Concept

For larger features where there are significant unknowns, it is often a good time investment to research potential options and to create a proof of concept project to verify assumptions.  In general the time spent in this phase will pay off with smoother release cycles and a better end product.  Additionally, having filled in many of the unknowns, it is much easier to judge the amount of time needed to complete the feature.

### Development

Development is where the work of improving MAP takes place.  The majority of day to day work takes place in this stage of the release process, so it is important to ensure that development best practices are being followed.

#### Branching Strategies

To keep changes to the code base as easily reviewable as possible, it's important to keep branches focused. To accomplish this, branches should be opened with a specific purpose in mind and pull requests should have tasks laid out as checkboxes in the description at the top.  Furthermore, pull requests should be opened early and collaborators identified as soon as possible.

The MAP team, and PRM Analytics more broadly, follows the Git Flow branching strategy.  This pattern requires 2 permanent protected branches, develop and master, with master serving as the currently promoted code base and develop as the current stable working build.  As feature development is begun, a branch should be created off of the develop branch with a pull request back into develop upon completion.  In the case of a larger feature a feature collector branch can be created with branches coming off of the collector and then back in upon completion.  This larger collector branch will then be merged back into develop.  This should ensure that develop is always in a working state.  Once all of the features and bug fixes planned for a release have been reviewed and merged into develop, a pre-release branch should be created.  By convention, pre-release branches should be named "pre-release-vX.X.X" where X.X.X reflects the planned release version number.  Pre-release pull requests act as a Pre-Release Peer Review and should only ever be pointed at the master branch. If any issues are discovered in pre-release that need to be addressed before release, a new branch should be created off of the pre-release branch and a pull request should be pointed back at the pre-release branch.

#### Unit Tests

The purpose of unit tests are to ensure that pieces of the application are behaving as expected.  Ideally, they also act as a mechanism for identifying regression in the behavior of the application.  With this in mind, all new features should have unit tests written to test actions for both success and failures modes.  At this time, unit testing in MAP is focused mainly on back end testing, since this is considered the ultimate source of truth.

These tests will be run as part of the CI process, and no Pull Request should be merged into the Develop or Master branches with failing checks.  The passage of CI will be indicated by a green checkmark next to the commit in the GitHub Pull Request.

#### Pull Requests

The purpose of a Pull Request is to act as a channel for collaboration and review before the changes are merged back into the code base.  In the spirit of collaboration Pull Requests should be opened early in the life cycle of a new branch.  Opening branches early allows you to identify and bring in collaborators early, which can help make your work better and reviews faster.

When opening a new Pull Request, you should first give it a name that helps to describe what the Pull Request is for.  Second, in the description of Pull Request, you should describe the problem that the Pull Request is meant to address and any other relevant information necessary to understand the changes.  Below the description you should create a checklist that lists all of the major tasks necessary to complete the work required to address the stated issue.  The last check in the task list should be a definition of done, which is a simple statement that describes when the work is truly done.  Additionally, if there are any open Issues related to the work being done in the Pull Request, then they should all be tagged at the bottom of the Pull Request description.  Finally, you should add your collaborators to the Pull Request, tag the issue with any appropriate labels, assign it to the MAP Kanboard project, and then specify the correct milestone.

### Pre-release / User Acceptance Testing (UAT)

Pre-Release and UAT are the final steps before a release is promoted into Master.  These steps are vital to ensure that the application is functioning as intended, and that all necessary steps were taken during the development and testing process.

#### Determining Release Version Number

Milliman Access Portal follows a semantic version numbering system.  This means that the position of the number indicates the type of change from the previous version.  In traditional distributed software applications, the first position is used to indicate breaking changes.  In a web application this isn't as meaningful, so MAP will use changes to this position to indicate major redesigns that would have far-reaching impacts on the users’ experience of the application.  This will likely be a rare occurrence.  The second position is used to indicate a release that includes new features.  Feature releases should happen on a fairly regular basis for the foreseeable future.  The final position indicates a patch, or a release containing bug fixes or small enhancements to existing functionality.  Historically we have seen 2 or 3 patch releases for each feature release.  Determining the release number of an upcoming release, based on the above information, should be a fairly straightforward process.

#### Creating the Pre-Release Pull Request

Once the Develop branch is feature complete for the upcoming release, a Pre-Release Pull Request should be opened and targeted at the Master branch.  The Pre-Release Pull Request is a special type of Pull Request that is used to track the progress of the release process and to record the Pre-Release Peer Review and any documentation that may be necessary for completion of the release.  To open a Pre-Release Pull Request, a new branch must be created off of Develop, and should be named `pre-release-vX.X.X` (with the appropriate version number for the release).  Once this branch is created, a Pull Request should be opened.  Similarly to the branch name, the Pull Request should be named "Pre-Release vX.X.X".  In the description of the new Pull Request, you should paste the contents of the Pre-Release Pull Request template that can be found here:

- [https://indy-github.milliman.com/raw/PRM/Milliman-Access-Portal/develop/.github/PRE_RELEASE_CHECKLIST_TEMPLATE.md](https://indy-github.milliman.com/raw/PRM/Milliman-Access-Portal/develop/.github/PRE_RELEASE_CHECKLIST_TEMPLATE.md)

Once the template has been copied and pasted into the description of the Pull Request, the contents should be updated to reflect the contents of the proposed release.  This would include updating the release number, tagging any users that will be completing any work in any of the sections, and adding/removing any tasks specific to this release.  Finally, the correct Milestone should be tagged, and the Pull Request should be added to the MAP Kanboard Project.

After the Pre-Release Pull Request has been completed, the following folder should be copied into its parent directory and renamed to reflect the upcoming release version:

- S:\PRM\UAT_Documentation\Milliman Access Portal\Version X.X.X

The folder should follow the existing naming convention for previous releases in the above folder, and this folder will hold any documentation related specifically to this release.  For smaller releases, this folder may not be necessary depending on the scope of the changes, and in these cases, the Pre-Release Pull Request will act as the repository of any documentation of testing that has been completed.

#### Authoring the UAT Document

For major releases, a UAT Document will be required to record the UAT process and identify who has performed testing for the release, as well as to record the Pre-Release Peer Review signature.  A template for this document was added when the UAT release folder was created.  After opening the document titled "UAT Checklist - Milliman Access Portal.xlsx" to the "UAT Acceptance" tab, the information for the pending release should be added in the appropriate locations.  This information include the version number of the upcoming release, the currently deployed version number, the date that the Pre-Release Pull Request was open and the URL.  At the bottom of the tab, please include the release notes for the upcoming release and save the document.  As the testing checklists are completed and UAT progresses, the rest of the document should be filled out to indicate who performed each task, the date, and the location on the network (if applicable).  This document should be completed before the release is merged into Master, and all responsible parties must have signed off on it.

#### Notification of Upcoming Release

At this point, it is important to notify relevant parties (management, developers, account managers, sales, etc.) about the contents of the upcoming release to allow them time to prepare for and notify any hosting clients.  This email should include detailed descriptions of the upcoming features, information about how the release will affect their users, an expected release timeframe, and any other relevant information that would be necessary to help users prepare for the upcoing release.

#### Determining Risk Level

The Risk Level of a given release is used to identify the amount of risk involved with the proposed changes to the application.  Determining the Risk Level of a release is based on the judgment of the authorized individual reviewing the release, and can only be performed by an individual with Signature Authority and familiarity with software development.  While software releases are substantially different from the typical consulting work done within Milliman, the Milliman Risk Levels themselves act as a useful indicator and are required to perform the Pre-Release Peer Review.

#### Penetration Testing

Penetration Testing is a useful tool for ensuring that an application does not contain egregious security vulnerabilities.  For Milliman Access Portal, we routinely work with Naomi Bornemann at Global Corporate Services (GCS) to have penetration testing performed on release candidates to ensure that new features aren't introducing new security vulnerabilities.  Decisions on whether penetration testing is required for a release are based on several factors:  

- The new feature is directly, or indirectly, related to MAP security
- The release includes support of a new content type
- The release has wide-ranging effects in the application that could introduce new attack vectors

If any of these factors are present in a release, then penetration testing will be required for the approval of the release.

In addition to the periodic testing performed by Naomi Bornemann, MAP is also required to go through 3rd party penetration testing on an annual basis.  This will be coordinated through the Infrastructure and Security team.  In either case, any findings must be addressed or mitigated before a release can go forward with the release process.

#### Deployment to Staging

It is vitally important that User Acceptance Testing (UAT) is performed in an environment that resembles the production environment as closely as possible.  To enable this, a staging environment has been created that mirrors production.  When a Pre-Release Pull Request is opened the release candidate should be deployed to Staging to ensure that testing is being performed on the correct code base.  Assuming the proper branch naming conventions have been followed, this should be an automated process, but it's important to confirm that it has been deployed correctly without error.  To manually deploy a release candidate to the staging environment the following steps must be taken by a user with the appropriate permissions:

1. Navigate to [https://indy-prmdeploy.milliman.com/app#/](https://indy-prmdeploy.milliman.com/app#/)
1. Click on "Milliman Access Portal" in the left-most column under "MAP projects"
1. Identify the correct branch in the "Channel" column and push "DEPLOY" in the corresponding row
1. Click the green "DEPLOY" button in the upper right-hand corner
1. Ensure that all deployment steps succeed (indicated by green checkmarks)
1. Repeat the above steps for the "Content Publication Server" project

If the release involves changes to the database, then it will be necessary to run a migration.  This will need to be performed by someone from the Infrastructure and Security team as it involves elevated privileges.  For releases that do not require database changes, this step will be unnecessary.

#### Application Testing

Application testing is extremely important to ensuring the proper function of new releases.  For each feature release, and any patch release that has wide ranging effects, an application checklist should be created to thoroughly test the functionality of all basic application actions.  This checklist should ensure that all pre-existing functionality has not been unintentionally altered or broken, as well as checking all new features to ensure that they are working as intended under a wide variety of circumstances.  In addition to testing the obvious aspects of the application the checklist should also include checks on the audit log to ensure that all loggable events are being captured appropriately.  This completed checklists should live in the UAT folder that was created for the release.

#### Browser Testing

Much like Application Testing, Browser Testing should be used to identify areas where the application is not performing as expected.  This testing should include a comprehensive testing of both existing and new functionality.  A single Browser Checklist template should be created to ensure that each browser is having the same tests performed and should be compiled with the user interface in mind and focus on ensuring that the application is working as expected in all of the browsers supported by Milliman Access Portal.  There should be one completed checklist for each browser, and it should be signed and dated by the tester, and include the release version being tested and the browser version that the tests were performed on.  This completed checklists should live in the UAT folder that was created for the release.

#### Triaging Issues Identified in Testing

Before moving forward with a release, it is important that any issues discovered during the testing of the application should have corresponding issues created in GitHub and their priority must be determined to ensure that they are addressed at the appropriate time.  High priority issues that will broadly affect the application should be addressed before the release moves forward, while lower impact issues may be noted and addressed in a later release.  The determination of priority should be made by the project manager.

#### Pre-release Peer Review

The Pre-Release Peer Review should be the final step in the release process before merging the Pre-Release Pull Request into the Master branch.  The Pre-Release Peer Review can only be completed by authorized personnel based on their Approved Professional (AP)/Signature Authority (SA) status, and their familiarity with the software development process.  For high-risk or high-visibility releases (2B+/2A) an external peer reviewer may be required.  If this is the case, the external peer reviewer should be identified early in the release process to ensure that there won't be a long delay.  

The Pre-Release Peer Review process is less about reviewing the technical aspects of a release, and more about reviewing the appropriateness.  It should include a review of the development process, any documentation that was compiled in regards to the release, the testing that was performed, and anything else that the peer reviewers deem to be within scope of, or relevant to, the release.  Once the peer reviewers have completed their reviews and are satisfied that the release meets all of the requirements for deployment, they should add the appropriate attestation language to the Pre-Release Pull Request in a comment, ensure that all appropriate Pre-Release Pull Request tasks and assertions are checked off, and sign off in the UAT Workbook, if one exists for the release.  

### Promotion

Promotion is the process by which a release candidate is merged into the Master branch and is then made ready for deployment.  This portion of the release process should only take place after the Pre-Release Peer Review has been completed and CI has completed successfully.

#### Merging the Release

Once UAT and Pre-Release Peer Review has been completed, and CI has passed successfully, it is time to merge the release.  Once the branch has been merged into Master, the Pre-Release branch can then be deleted.  The appropriate checks at the top of the Pre-Release Pull Request must still be completed, however.  This merge should automatically trigger a CI synchronize build of Master.  This build will be what is actually deployed, so it is important to make sure that this build completes successfully.

Finally, once the Master branch has been updated with the new release, Master should be merged back into Develop to ensure that it is in sync with the Master branch.  Any existing branches being actively worked on should then be updated from Develop as necessary.

#### Tagging the Release

Tagging the release can be done in one of two ways:

Through the GitHub repo:

  1. Navigate to: [https://indy-github.milliman.com/PRM/Milliman-Access-Portal/releases/new](https://indy-github.milliman.com/PRM/Milliman-Access-Portal/releases/new)
  1. Type in the release version in the format vX.X.X using the targeted release version in the "Tag version" field
  1. Switch to target the Master branch
  1. Add a release title of "Milliman Access Portal vX.X.X" with the targeted release version applied
  1. Click the green "Publish release" button at the bottom of the form

Through the CLI:

  1. Open a command window at the root of the repository on your local repo
  2. Enter the following commands (replacing vX.X.X with the appropriate release version):

    > git checkout master
    > git fetch
    > git tag -a vX.X.X -m "Milliman Access Portal vX.X.X"
    > git push origin vX.X.X

Once the tag has been created, the GitHub repository should be checked to ensure that the new tag was created and was applied correctly.

#### Scheduling the Deployment

Once the release has been merged and tagged, it is then time to schedule deployment with the Infrastructure and Security team.  By this point, the person performing the deployment should be identified, and a deployment time should be agreed upon.  To minimize the impact to users, deployments should generally be scheduled in the evening after 9PM EST.  Deployments of a more urgent nature may be scheduled for normal business hours on a case-by-case basis if there is an overriding need.  If there are any migrations, or ancillary scripts that need to be run in conjunction with the deployment, these should be included in the discussion with the person deploying the application, as well as in a message on the Pre-Release Pull Request about the details deployment.

#### Notification of Release

At this time, an email should be sent informing all interested parties (management, developers, account managers, sales, etc.) that the release has been promoted and when deployment is scheduled for.  This email should be a follow up of the email sent previously describing the contents of the release.

#### Post Deployment

Once the new release has been deployed, someone should log in to ensure that the new features, or fixes, have been properly applied in the application.  If serious issues are found, it may be necessary to revert the deployment, and this will need to be coordinated with the person performing the deployment.

#### Schedule Next Retrospective

After the new release has been deployed, set up the next 1 hour retrospective meeting with the hosting team. Try to set up the next meeting relatively close to the release date so that the team is able to speak to the release in the retrospective.

#### Schedule Next Milestone Meeting

After the new releaes has been deployed (or even a little prior to deployment), set up an hour milestone meeting with the hosting team. This is so that we can clearly define the scope of the milestone at the beginning of the new version release.
