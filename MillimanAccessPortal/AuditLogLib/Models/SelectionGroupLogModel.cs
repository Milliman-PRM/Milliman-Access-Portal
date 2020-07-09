/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A model representing a SelectionGroup and related items
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuditLogLib.Models
{
    public class SelectionGroupLogModel
    {
        public Guid Id { get; set; }
        public string GroupName { get; set; }
        public List<IdAndNameModel> MemberUsers { get; set; }

        public SelectionGroupLogModel() { }

        public SelectionGroupLogModel(SelectionGroup arg, IEnumerable<IdAndNameModel> memberUsers = null)
        {
            Id = arg.Id;
            GroupName = arg.GroupName;
            MemberUsers = memberUsers?.ToList();
        }
    }

    public class IdAndNameModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
    }
}
