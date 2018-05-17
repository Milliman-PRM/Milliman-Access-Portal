using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using TestResourcesLib;
using Newtonsoft.Json;

namespace ContentPublishingServiceTests
{
    class InitializeTests
    {
        public static Mock<ApplicationDbContext> InitializeWithUnspecifiedStatus(Mock<ApplicationDbContext> Db)
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

            #region Initialize FileUpload
            Db.Object.FileUpload.AddRange(new List<FileUpload>
            {
                new FileUpload
                {
                    Id = Guid.NewGuid(),
                    Checksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                    ClientFileIdentifier = "Uploaded Test File 1",
                    CreatedDateTimeUtc = DateTime.Now - new TimeSpan(0, 0, 45),
                    StoragePath = @"\\indy-syn01\prm_test\Uploads\Uploaded Test File 1.qvw",
                },
                new FileUpload
                {
                    Id = Guid.NewGuid(),
                    Checksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                    ClientFileIdentifier = "Uploaded Test File 2",
                    CreatedDateTimeUtc = DateTime.Now - new TimeSpan(0, 0, 45),
                    StoragePath = @"\\indy-syn01\prm_test\Uploads\Uploaded Test File 2.qvw",
                },
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
            ContentReductionHierarchy<ReductionFieldValueSelection> ValidSelectionsObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = 1,
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = 1,
                        Values = new ReductionFieldValueSelection[]
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = 1,
                                Value = "Assigned Provider Clinic (Hier) 0434",
                                SelectionStatus = true,
                            },
                            new ReductionFieldValueSelection
                            {
                                Id = 2,
                                Value = "Assigned Provider Clinic (Hier) 4025",
                                SelectionStatus = true,
                            },
                        },
                    }
                }
            };
            // Valid reducable task
            Db.Object.ContentReductionTask.Add(new ContentReductionTask
            {
                Id = new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTime = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = 1,
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteria = JsonConvert.SerializeObject(ValidSelectionsObject, Formatting.Indented),                
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> InvalidFieldValueObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = 1,
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = 1,
                        Values = new ReductionFieldValueSelection[]
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = 1,
                                Value = "Invalid selection value",
                                SelectionStatus = true,
                            },
                        },
                    }
                }
            };
            // Invalid field value
            Db.Object.ContentReductionTask.Add(new ContentReductionTask
            {
                Id = new Guid(2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTime = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = 1,
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteria = JsonConvert.SerializeObject(InvalidFieldValueObject, Formatting.Indented),
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> OneValidAndOneInvalidFieldValueObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = 1,
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = 1,
                        Values = new ReductionFieldValueSelection[]
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = 1,
                                Value = "Invalid selection value",
                                SelectionStatus = true,
                            },
                        },
                    },
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = 1,
                        Values = new ReductionFieldValueSelection[]
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = 1,
                                Value = "Assigned Provider Clinic (Hier) 4025",
                                SelectionStatus = true,
                            },
                        },
                    },
                }
            };
            // One Valid, one invalid field value
            Db.Object.ContentReductionTask.Add(new ContentReductionTask
            {
                Id = new Guid(3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTime = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = 1,
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteria = JsonConvert.SerializeObject(OneValidAndOneInvalidFieldValueObject, Formatting.Indented),
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> InvalidFieldNameObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = 1,
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Invalid Field Name",
                        DisplayName = "Invalid Field Name",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = 1,
                        Values = new ReductionFieldValueSelection[]
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = 1,
                                Value = "Invalid value",
                                SelectionStatus = true,
                            },
                        },
                    }
                }
            };
            // Invalid field name
            Db.Object.ContentReductionTask.Add(new ContentReductionTask
            {
                Id = new Guid(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTime = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = 1,
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteria = JsonConvert.SerializeObject(InvalidFieldNameObject, Formatting.Indented),
            });

            MockDbSet<ContentReductionTask>.AssignNavigationProperty<SelectionGroup>(Db.Object.ContentReductionTask, "SelectionGroupId", Db.Object.SelectionGroup);
            #endregion

            #region Initialize ContentPublicationRequest

            // Valid request
            Db.Object.ContentPublicationRequest.Add(new ContentPublicationRequest
            {
                Id = 1,
                ApplicationUserId = 1,
                CreateDateTime = DateTime.Now,
                RequestStatus = PublicationStatus.Unknown,
                PublishRequest = new PublishRequest
                {
                    RootContentItemId = 1,
                    RelatedFiles = new ContentRelatedFile[]
                    {
                        new ContentRelatedFile {FilePurpose = "MasterContent", FileUploadId = Db.Object.FileUpload.ElementAt(0).Id},
                        new ContentRelatedFile {FilePurpose = "UserGuide", FileUploadId = Db.Object.FileUpload.ElementAt(1).Id},
                    }
                }
            });

            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(Db.Object.ContentPublicationRequest, "RootContentItemId", Db.Object.RootContentItem);
            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(Db.Object.ContentPublicationRequest, "RootContentItemId", Db.Object.ApplicationUser);
            #endregion

            return Db;
        }
    }
}
