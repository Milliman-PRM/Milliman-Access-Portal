using System;
using Moq;
using MapDbContextLib.Context;
using TestResourcesLib;

namespace ContentReductionServiceTests
{
    class InitializeTests
    {
        public static Mock<ApplicationDbContext> Initialize(Mock<ApplicationDbContext> Db)
        {
            #region Initialize ContentType
            Db.Object.ContentType.Add(new ContentType
            {
                Id = 1,
                Name = "Qlikview",
                CanReduce = true,
                TypeEnum = ContentTypeEnum.Qlikview,
            });
            #endregion

            #region Initialize RootContentItem
            Db.Object.RootContentItem.Add(new RootContentItem
            {
                Id = 1,
                ContentName = "Test content",
                ClientId = 1,
                ContentTypeId = 1,
                TypeSpecificDetail = "",
            });
            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(Db.Object.RootContentItem, "ContentTypeId", Db.Object.ContentType);
            #endregion

            #region Initialize SelectionGroup
            Db.Object.SelectionGroup.Add(new SelectionGroup
            {
                Id = 1,
                RootContentItemId = 1,
            });
            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(Db.Object.SelectionGroup, "RootContentItemId", Db.Object.RootContentItem);
            #endregion

            #region Initialize ContentReductionTask
            Db.Object.ContentReductionTask.Add(new ContentReductionTask
            {
                Id = new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTime = DateTime.UtcNow,
                MasterFilePath = "xyz",
                SelectionGroupId = 1,
                MasterContentChecksum = "",
                ReductionStatus = ReductionStatusEnum.Unspecified,
            });
            MockDbSet<ContentReductionTask>.AssignNavigationProperty<SelectionGroup>(Db.Object.ContentReductionTask, "SelectionGroupId", Db.Object.SelectionGroup);
            #endregion

            return Db;
        }
    }
}
