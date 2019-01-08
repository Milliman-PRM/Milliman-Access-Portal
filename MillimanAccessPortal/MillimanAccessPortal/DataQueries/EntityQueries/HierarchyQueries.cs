using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using MillimanAccessPortal.Models.EntityModels.HierarchyModels;
using System;
using System.Collections.Generic;
using System.Linq;

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
