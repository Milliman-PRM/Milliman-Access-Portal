/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ClientAccessReview
{
    public class ClientAccessReviewModel
    {
        public Guid Id { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public List<ClientActorModel> ClientAdmins { get; set; }
        public string AssignedProfitCenterName { get; set; }
        public List<ClientActorModel> ProfitCenterAdmins { get; set; } = new List<ClientActorModel>();
        public List<string> ApprovedEmailDomainList { get; set; } = new List<string>();
        public List<string> ApprovedEmailExceptionList { get; set; } = new List<string>();
        public List<ClientActorReviewModel> MemberUsers { get; set; } = new List<ClientActorReviewModel>();
        public List<ClientContentItemModel> ContentItems { get; set; } = new List<ClientContentItemModel>();
        public List<ClientFileDropModel> FileDrops { get; set; } = new List<ClientFileDropModel>();
        public string AttestationLanguage { get; set; }
        public Guid ClientAccessReviewId { get; set; }
    }

    public class ClientActorReviewModel : ClientActorModel
    {
        public DateTime? LastLoginDate { get; set; }
        public Dictionary<RoleEnum, bool> ClientUserRoles { get; set; }

        public ClientActorReviewModel(ApplicationUser user)
            :base(user)
        {}

        public ClientActorReviewModel(SftpAccount account)
            : base(account)
        {}
    }
    public class ClientContentItemModel
    {
        public string ContentItemName { get; set; }
        public string ContentType { get; set; }
        public bool IsSuspended { get; set; }
        public DateTime? LastPublishedDate { get; set; }
        public List<ClientContentItemSelectionGroupModel> SelectionGroups { get; set; } = new List<ClientContentItemSelectionGroupModel>();
    }
    public class ClientContentItemSelectionGroupModel
    {
        public string SelectionGroupName { get; set; }
        public bool IsSuspended { get; set; }
        public List<ClientActorModel> AuthorizedUsers { get; set; } = new List<ClientActorModel>();
    }
    public class ClientFileDropModel
    {
        public string FileDropName { get; set; }
        public List<ClientFileDropPermissionGroupModel> PermissionGroups { get; set; } = new List<ClientFileDropPermissionGroupModel>();
    }
    public class ClientFileDropPermissionGroupModel
    {
        public string PermissionGroupName { get; set; }
        public Dictionary<string, bool> Permissions { get; set; } = new Dictionary<string, bool>();
        public List<ClientActorModel> AuthorizedMapUsers { get; set; } = new List<ClientActorModel>();
        public List<ClientActorModel> AuthorizedServiceAccounts { get; set; } = new List<ClientActorModel>();
    }
}

/*
Response: {
    //Id: Guid,
    //ClientName: string,
    //ClientCode: string,
    //ClientAdmins: [
    //   {
    //        UserName: string,
    //        UserEmail: string,
    //    },
    //],
    //AssignedProfitCenter: string,
    //ProfitCenterAdmins: [
    //    {
    //        UserName: string,
    //        UserEmail: string,
    //    },
    //],
    //ApprovedEmailDomainList: [string],
    //ApprovedEmailExceptionList: [string],
    //UserRoles: [
    //    {
    //        UserName: string,
    //        UserEmail: string,
    //        LastLoginDate: datetime,
    //        Roles: {
    //            ClientAdmin: boolean,
    //            ContentPublisher: boolean,
    //            ContentAccessAdmin: boolean,
    //            ContentUser: boolean,
    //            FileDropAdmin: boolean,
    //            FileDropUser: boolean,
    //        },
    //    },
    //],
    //AuthorizedContentUsers: {
    //    UserId: {
    //        UserName: string,
    //        UserEmail: string,
    //    },
    //},
    //ContentItems: [
    //    {
    //        ContentItemName: string,
    //        ContentType: string,
    //        LastPublishedDate: datetime,
    //        SelectionGroups: [
    //            {
    //                SelectionGroupName: string,
    //                AuthorizedUsers: [Guid]
    //            }
    //        ]
    //    },
    //],
    //AuthorizedFileDropUsers: {
    //    UserId: {
    //        UserName: string,
    //        UserEmail: string,
    //        Admin: boolean,
    //    },
    //},
    FileDrops: [
        {
            FileDropName: string,
            PermissionGroups: [
                {
                    PermissionGroupName: string,
                    Permissions: {
                        Read: boolean,
                        Write: boolean,
                        Delete: boolean,
                    },
                    Users: [Guid],
                },
            ],
        },
    ],
    Attestation: string,
    ReviewId: Guid,
}
}
*/