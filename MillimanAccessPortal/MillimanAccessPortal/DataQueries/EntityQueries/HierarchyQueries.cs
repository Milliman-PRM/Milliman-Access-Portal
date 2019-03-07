using MapDbContextLib.Context;
using MillimanAccessPortal.Models.EntityModels.HierarchyModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.DataQueries.EntityQueries
{
    /// <summary>
    /// Provides queries related to QlikView hierarchies.
    /// </summary>
    public class HierarchyQueries
    {
        private readonly ApplicationDbContext _dbContext;

        public HierarchyQueries(
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Select all fields for a selection group's content item
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>List of fields</returns>
        internal List<BasicField> SelectFieldsWhereSelectionGroup(Guid selectionGroupId)
        {
            var selectionGroup = _dbContext.SelectionGroup.Find(selectionGroupId);
            var fields = _dbContext.HierarchyField
                .Where(f => f.RootContentItemId == selectionGroup.RootContentItemId)
                .OrderBy(f => f.FieldDisplayName)
                .Select(f => new BasicField
                {
                    Id = f.Id,
                    RootContentItemId = f.RootContentItemId,
                    FieldName = f.FieldName,
                    DisplayName = f.FieldDisplayName,
                })
                .ToList();

            return fields;
        }

        /// <summary>
        /// Select all values for a selection group's content item
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>List of fields</returns>
        internal List<BasicValue> SelectValuesWhereSelectionGroup(Guid selectionGroupId)
        {
            var selectionGroup = _dbContext.SelectionGroup.Find(selectionGroupId);
            var values = _dbContext.HierarchyFieldValue
                .Where(f => f.HierarchyField.RootContentItemId == selectionGroup.RootContentItemId)
                .OrderBy(f => f.Value)
                .Select(f => new BasicValue
                {
                    Id = f.Id,
                    ReductionFieldId = f.HierarchyFieldId,
                    Value = f.Value,
                })
                .ToList();

            return values;
        }
    }
}
