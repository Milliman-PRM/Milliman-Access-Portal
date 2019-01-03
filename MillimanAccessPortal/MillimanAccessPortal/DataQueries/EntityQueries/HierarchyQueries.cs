using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.HierarchyModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries.EntityQueries
{
    public class HierarchyQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public HierarchyQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        internal async Task<List<BasicField>> SelectFieldsWhereSelectionGroup(Guid selectionGroupId)
        {
            var selectionGroup = await _dbContext.SelectionGroup.FindAsync(selectionGroupId);
            var fields = await _dbContext.HierarchyField
                .Where(f => f.RootContentItemId == selectionGroup.RootContentItemId)
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

        internal async Task<List<BasicValue>> SelectValuesWhereSelectionGroup(Guid selectionGroupId)
        {
            var selectionGroup = await _dbContext.SelectionGroup.FindAsync(selectionGroupId);
            var values = await _dbContext.HierarchyFieldValue
                .Where(f => f.HierarchyField.RootContentItemId == selectionGroup.RootContentItemId)
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
