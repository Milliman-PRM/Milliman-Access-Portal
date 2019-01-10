using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class UpdateGroupRequestModel
    {
        /// <summary>
        /// ID of the selection group to update.
        /// </summary>
        public Guid GroupId { get; set; }

        /// <summary>
        /// New name for the selection group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of users to assign to the selection group.
        /// </summary>
        public List<Guid> Users { get; set; }
    }
}
