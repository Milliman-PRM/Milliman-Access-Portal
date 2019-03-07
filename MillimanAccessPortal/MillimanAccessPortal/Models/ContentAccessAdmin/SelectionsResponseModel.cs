using MillimanAccessPortal.Models.EntityModels.HierarchyModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SelectionsResponseModel
    {
        /// <summary>
        /// Selection group that these selections belong to.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Live selections for this selection group.
        /// </summary>
        public List<Guid> LiveSelections { get; set; }

        /// <summary>
        /// Pending selections for this selection group.
        /// </summary>
        public List<Guid> ReductionSelections { get; set; }

        /// <summary>
        /// Hierarchy fields for this selection group.
        /// </summary>
        public Dictionary<Guid, BasicField> Fields { get; set; }

        /// <summary>
        /// Hierarchy field values for this selection group.
        /// </summary>
        public Dictionary<Guid, BasicValue> Values { get; set; }
    }
}
