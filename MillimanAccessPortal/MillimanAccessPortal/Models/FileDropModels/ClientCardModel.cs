/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents client properties used in the UI for the FileDrop view.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class ClientCardModel
    {
        public ClientCardModel(Client client)
        {
            Id = client.Id;
            Name = client.Name;
            ParentId = client.ParentClientId;
            Code = client.ClientCode;
        }

        public Guid Id { get; set; }

        public Guid? ParentId { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        /// <summary>
        /// Number of FileDrops associated with this client.
        /// </summary>
        public int FileDropCount { get; set; }

        /// <summary>
        /// Number of users who are assigned to at least 1 FileDrop for the client
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Indication of the requesting user's authorization to administer this client based on a client role
        /// </summary>
        public bool CanManageFileDrops { get; set; }

        /// <summary>
        /// Indication of the requesting user's assignment to a permission group of at least one FileDrop of the client
        /// </summary>
        public bool AuthorizedFileDropUser { get; set; }
    }
}
