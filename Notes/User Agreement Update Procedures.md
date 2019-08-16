# User Agreement Update Procedures

## Purpose

The purpose of this document is to detail the process by which User Agreement language changes are made and deployed.

## Deciding on Changes

Once a need for an update to the existing User Agreement language has been identified, the scope of the changes should be defined in consultation with any interested parties and someone from GCS Legal that will be drafting the changes. Once all parties are in agreement in regards to the proposed changes, the process of drafting the changes should begin.

## Drafting Process

The process of authoring changes to the existing User Agreement language should be performed by GCS Legal. When legal has a first draft of those changes, they should be distributed to the interested parties to confirm that the underlying issue has been adequately addressed by the changes. Once a consensus has been reached, the process of notifying clients of an upcoming change should be initiated.

## Notifying Clients

Once a change to the User Agreement language has been agreed upon, the Account Managers should send a notification to all hosting clients to communicate the proposed change, the underlying reason for the change, and the proposed timeline for the deployment of the new User Agreement language. They should be given a period of 2 business days to respond to the proposed changes with any objections that they may have to the new language. If an objection is made to the changes, a discussion should be had between GCS Legal, the original interested parties, and the person making the objection, and an agreement should be reached to either move forward with the proposed language, or to make further changes to address the new objection. Once the original time period has passed and an agreement has been reached, or the 2 day time limit has passed without objection, another notification should be sent to the hosting clients asking them to let their users know about the upcoming changes to the User Agreement language, and that they will be required to accept the new agreement.  This notification should give a date that is at least a week away when the language changes will officially take effect, so that their users can know when to expect to see the changes.

## Pre-Release Peer Review

When the update to the User Agreement language has been finalized, a branch should be created off of the Master branch with the following the following naming convention "User_Agreement_Update_MM_DD_YYYY". After creating the branch, the changes should be made to the UserAgreement.md and committed. Upon completion of the changes, a pull request should be opened pointing at the Master branch. This pull request should use the "USER_AGREEMENT_UPDATE_TEMPLATE.md" template in the .github directory at the root of the repository.  The template should be filled out completely to include information about the reason for the update, the name of anyone involved in drafting the changes, links to any supporting emails or documentation on the network, and the date that the language was communicated to go into effect. Finally, a Peer Reviewer should be identified, and the changes to the language should be carefully proof read and Peer Reviewed. Once the changes have been Peer Reviewed and proof read, all assertions have been completed, and all attestations have been attached to the pull request the deployment should be scheduled on the correct date by an appropriate person, and the branch can be merged.

## Deploying the Updated Language

The deployment of the updated User Agreement Language must be done by someone with System Admin privileges in MAP, and is done at the following URL:

- https://map.milliman.com/SystemAdmin/UpdateUserAgreement

The updated language should be copied from the "UserAgreement.md" file in the Documentation folder of the Master branch, and should be copied and pasted in its entirety into the input replacing the existing text in full.  Once the new contents have been pasted in, the language should be previewed to ensure that the formatting is correct and that the expected changes are present. Once the user is confident that the update is correct, the "Update" button should be clicked, and the user will then need to accept the new User Agreement language.
