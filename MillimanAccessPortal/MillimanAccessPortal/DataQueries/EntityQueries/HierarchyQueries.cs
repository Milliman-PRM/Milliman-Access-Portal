/*
 * CODE OWNERS: tOM pUCKETT
 * OBJECTIVE: Query methods related to content reduction hierarchy
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.HierarchyModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        internal async Task<List<BasicField>> SelectFieldsWhereSelectionGroupAsync(Guid selectionGroupId)
        {
            var selectionGroup = await _dbContext.SelectionGroup.FindAsync(selectionGroupId);
            var fields = await _dbContext.HierarchyField
                .Where(f => f.RootContentItemId == selectionGroup.RootContentItemId)
                .OrderBy(f => f.FieldDisplayName)
                .Select(f => new BasicField
                {
                    Id = f.Id,
                    RootContentItemId = f.RootContentItemId,
                    FieldName = f.FieldName,
                    DisplayName = f.FieldDisplayName,
                })
                .ToListAsync();

            return fields;
        }

        /// <summary>
        /// Select all values for a selection group's content item
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>List of fields</returns>
        internal async Task<List<BasicValue>> SelectValuesWhereSelectionGroupAsync(Guid selectionGroupId)
        {
            var selectionGroup = await _dbContext.SelectionGroup.FindAsync(selectionGroupId);
            var values = await _dbContext.HierarchyFieldValue
                .Where(f => f.HierarchyField.RootContentItemId == selectionGroup.RootContentItemId)
                .OrderBy(f => f.Value)
                .Select(f => new BasicValue
                {
                    Id = f.Id,
                    ReductionFieldId = f.HierarchyFieldId,
                    Value = f.Value,
                })
                .ToListAsync();

            return values;
        }
    }
}
