/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Model class representing loggable properties of an SftpAccount and (if exists) associated MAP user
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;

namespace AuditLogLib.Models
{
    public class SftpAccountLogModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public Guid? MapUserId { get; set; }
        public string MapUserName { get; set; }

        public SftpAccountLogModel() { }

        public SftpAccountLogModel(SftpAccount arg)
        {
            Id = arg.Id;
            UserName = arg.UserName;
            MapUserId = arg.ApplicationUserId;
            MapUserName = arg.ApplicationUser?.UserName;
        }
    }
}
