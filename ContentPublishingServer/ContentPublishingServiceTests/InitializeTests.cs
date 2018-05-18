using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using MapDbContextLib.Context;
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
            Db.Object.RootContentItem.AddRange(
                new List<RootContentItem>
                {
                    new RootContentItem
                    {
                        Id = 1,
                        ContentName = "Test content 1",
                        ClientId = 1,
                        ContentTypeId = 1,
                        TypeSpecificDetail = "{}",
                        DoesReduce = false,
                        Notes = "DoesReduce false",
                        Description = "For use with a single SelectionGroup where IsMaster == true",
                    },
                    new RootContentItem
                    {
                        Id = 2,
                        ContentName = "Test content 2",
                        ClientId = 1,
                        ContentTypeId = 1,
                        TypeSpecificDetail = "{}",
                        DoesReduce = false,
                        Notes = "DoesReduce false",
                        Description = "For use with no SelectionGroup",
                    },
                    new RootContentItem
                    {
                        Id = 3,
                        ContentName = "Test content 3",
                        ClientId = 1,
                        ContentTypeId = 1,
                        TypeSpecificDetail = "{}",
                        DoesReduce = true,
                        Notes = "DoesReduce true",
                        Description = "For use with no SelectionGroup",
                    },
                    new RootContentItem
                    {
                        Id = 4,
                        ContentName = "Test content 4",
                        ClientId = 1,
                        ContentTypeId = 1,
                        TypeSpecificDetail = "{}",
                        DoesReduce = true,
                        Notes = "DoesReduce true",
                        Description = "For use with multiple SelectionGroup",
                    },
                });
            MockDbSet<RootContentItem>.AssignNavigationProperty(Db.Object.RootContentItem, "ContentTypeId", Db.Object.ContentType);
            #endregion

            #region Initialize HierarchyField
            Db.Object.HierarchyField.AddRange(
                new List<HierarchyField>
                {
                    // RootContentItem 3
                    new HierarchyField { Id = 1, RootContentItemId = 3, StructureType = FieldStructureType.Flat, FieldName = "All Members (Hier)", FieldDisplayName = "All Members (Hier)", },
                    new HierarchyField { Id = 2, RootContentItemId = 3, StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider Clinic (Hier)", FieldDisplayName = "Assigned Provider Clinic (Hier)", },
                    new HierarchyField { Id = 3, RootContentItemId = 3, StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider (Hier)", FieldDisplayName = "Assigned Provider (Hier)", },

                    // RootContentItem 4
                    new HierarchyField { Id = 4, RootContentItemId = 4, StructureType = FieldStructureType.Flat, FieldName = "All Members (Hier)", FieldDisplayName = "All Members (Hier)", },
                    new HierarchyField { Id = 5, RootContentItemId = 4, StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider Clinic (Hier)", FieldDisplayName = "Assigned Provider Clinic (Hier)", },
                    new HierarchyField { Id = 6, RootContentItemId = 4, StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider (Hier)", FieldDisplayName = "Assigned Provider (Hier)", },
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty(Db.Object.HierarchyField, "RootContentItemId", Db.Object.RootContentItem);
            #endregion

            #region Initialize HierarchyField
            Db.Object.HierarchyFieldValue.AddRange(
                new List<HierarchyFieldValue>
                {
                    // RootContentItem 3
                    new HierarchyFieldValue { Id = 1, HierarchyFieldId = 1, Value = "All Members (Hier) 3038", },
                    new HierarchyFieldValue { Id = 3, HierarchyFieldId = 2, Value = "Assigned Provider Clinic (Hier) 4025", },
                    new HierarchyFieldValue { Id = 4, HierarchyFieldId = 2, Value = "Assigned Provider Clinic (Hier) 9438", },
                    new HierarchyFieldValue { Id = 5, HierarchyFieldId = 2, Value = "Assigned Provider Clinic (Hier) 7252", },
                    new HierarchyFieldValue { Id = 6, HierarchyFieldId = 2, Value = "Assigned Provider Clinic (Hier) 0434", },
                    new HierarchyFieldValue { Id = 7, HierarchyFieldId = 2, Value = "Assigned Provider Clinic (Hier) 7291", },
                    new HierarchyFieldValue { Id = 8, HierarchyFieldId = 2, Value = "Assigned Provider Clinic (Hier) 3038", },
                    new HierarchyFieldValue { Id = 9, HierarchyFieldId = 2, Value = "Assigned Provider Clinic (Hier) 0871", },
                    new HierarchyFieldValue { Id = 17, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3575", },
                    new HierarchyFieldValue { Id = 18, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3173", },
                    new HierarchyFieldValue { Id = 19, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 8199", },
                    new HierarchyFieldValue { Id = 20, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 5056", },
                    new HierarchyFieldValue { Id = 21, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 0753", },
                    new HierarchyFieldValue { Id = 22, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 8747", },
                    new HierarchyFieldValue { Id = 23, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 0024", },
                    new HierarchyFieldValue { Id = 24, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 4185", },
                    new HierarchyFieldValue { Id = 25, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 0868", },
                    new HierarchyFieldValue { Id = 26, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 8409", },
                    new HierarchyFieldValue { Id = 27, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 2445", },
                    new HierarchyFieldValue { Id = 28, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 0691", },
                    new HierarchyFieldValue { Id = 29, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 6434", },
                    new HierarchyFieldValue { Id = 30, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3373", },
                    new HierarchyFieldValue { Id = 31, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3530", },
                    new HierarchyFieldValue { Id = 32, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 1666", },
                    new HierarchyFieldValue { Id = 33, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3855", },
                    new HierarchyFieldValue { Id = 34, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3552", },
                    new HierarchyFieldValue { Id = 35, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 4860", },
                    new HierarchyFieldValue { Id = 36, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 8064", },
                    new HierarchyFieldValue { Id = 37, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 9029", },
                    new HierarchyFieldValue { Id = 38, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 4300", },
                    new HierarchyFieldValue { Id = 39, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 7205", },
                    new HierarchyFieldValue { Id = 40, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 2846", },
                    new HierarchyFieldValue { Id = 41, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 5229", },
                    new HierarchyFieldValue { Id = 42, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 5251", },
                    new HierarchyFieldValue { Id = 43, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 1858", },
                    new HierarchyFieldValue { Id = 44, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 1432", },
                    new HierarchyFieldValue { Id = 45, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 1078", },
                    new HierarchyFieldValue { Id = 46, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 6262", },
                    new HierarchyFieldValue { Id = 47, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 7811", },
                    new HierarchyFieldValue { Id = 48, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 2576", },
                    new HierarchyFieldValue { Id = 49, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 4309", },
                    new HierarchyFieldValue { Id = 50, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 5342", },
                    new HierarchyFieldValue { Id = 51, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 8087", },
                    new HierarchyFieldValue { Id = 52, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3973", },
                    new HierarchyFieldValue { Id = 53, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 2770", },
                    new HierarchyFieldValue { Id = 54, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 0433", },
                    new HierarchyFieldValue { Id = 55, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 4793", },
                    new HierarchyFieldValue { Id = 56, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 2803", },
                    new HierarchyFieldValue { Id = 57, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 1530", },
                    new HierarchyFieldValue { Id = 58, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 0471", },
                    new HierarchyFieldValue { Id = 59, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 2963", },
                    new HierarchyFieldValue { Id = 60, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 2045", },
                    new HierarchyFieldValue { Id = 61, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3982", },
                    new HierarchyFieldValue { Id = 62, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 1514", },
                    new HierarchyFieldValue { Id = 63, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 2304", },
                    new HierarchyFieldValue { Id = 64, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 0533", },
                    new HierarchyFieldValue { Id = 65, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 3612", },
                    new HierarchyFieldValue { Id = 66, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 1786", },
                    new HierarchyFieldValue { Id = 67, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 5978", },
                    new HierarchyFieldValue { Id = 68, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 4199", },
                    new HierarchyFieldValue { Id = 69, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 0093", },
                    new HierarchyFieldValue { Id = 70, HierarchyFieldId = 3, Value = "Assigned Provider (Hier) 4137", },

                    // RootContentItem 4
                    new HierarchyFieldValue { Id = 2, HierarchyFieldId = 4, Value = "All Members (Hier) 3038", },
                    new HierarchyFieldValue { Id = 10, HierarchyFieldId = 5, Value = "Assigned Provider Clinic (Hier) 4025", },
                    new HierarchyFieldValue { Id = 11, HierarchyFieldId = 5, Value = "Assigned Provider Clinic (Hier) 9438", },
                    new HierarchyFieldValue { Id = 12, HierarchyFieldId = 5, Value = "Assigned Provider Clinic (Hier) 7252", },
                    new HierarchyFieldValue { Id = 13, HierarchyFieldId = 5, Value = "Assigned Provider Clinic (Hier) 0434", },
                    new HierarchyFieldValue { Id = 14, HierarchyFieldId = 5, Value = "Assigned Provider Clinic (Hier) 7291", },
                    new HierarchyFieldValue { Id = 15, HierarchyFieldId = 5, Value = "Assigned Provider Clinic (Hier) 3038", },
                    new HierarchyFieldValue { Id = 16, HierarchyFieldId = 5, Value = "Assigned Provider Clinic (Hier) 0871", },
                    new HierarchyFieldValue { Id = 71, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3575", },
                    new HierarchyFieldValue { Id = 72, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3173", },
                    new HierarchyFieldValue { Id = 73, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 8199", },
                    new HierarchyFieldValue { Id = 74, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 5056", },
                    new HierarchyFieldValue { Id = 75, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 0753", },
                    new HierarchyFieldValue { Id = 76, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 8747", },
                    new HierarchyFieldValue { Id = 77, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 0024", },
                    new HierarchyFieldValue { Id = 78, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 4185", },
                    new HierarchyFieldValue { Id = 79, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 0868", },
                    new HierarchyFieldValue { Id = 80, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 8409", },
                    new HierarchyFieldValue { Id = 81, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 2445", },
                    new HierarchyFieldValue { Id = 82, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 0691", },
                    new HierarchyFieldValue { Id = 83, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 6434", },
                    new HierarchyFieldValue { Id = 84, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3373", },
                    new HierarchyFieldValue { Id = 85, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3530", },
                    new HierarchyFieldValue { Id = 86, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 1666", },
                    new HierarchyFieldValue { Id = 87, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3855", },
                    new HierarchyFieldValue { Id = 88, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3552", },
                    new HierarchyFieldValue { Id = 89, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 4860", },
                    new HierarchyFieldValue { Id = 90, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 8064", },
                    new HierarchyFieldValue { Id = 91, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 9029", },
                    new HierarchyFieldValue { Id = 92, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 4300", },
                    new HierarchyFieldValue { Id = 93, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 7205", },
                    new HierarchyFieldValue { Id = 94, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 2846", },
                    new HierarchyFieldValue { Id = 95, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 5229", },
                    new HierarchyFieldValue { Id = 96, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 5251", },
                    new HierarchyFieldValue { Id = 97, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 1858", },
                    new HierarchyFieldValue { Id = 98, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 1432", },
                    new HierarchyFieldValue { Id = 99, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 1078", },
                    new HierarchyFieldValue { Id = 100, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 6262", },
                    new HierarchyFieldValue { Id = 101, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 7811", },
                    new HierarchyFieldValue { Id = 102, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 2576", },
                    new HierarchyFieldValue { Id = 103, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 4309", },
                    new HierarchyFieldValue { Id = 104, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 5342", },
                    new HierarchyFieldValue { Id = 105, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 8087", },
                    new HierarchyFieldValue { Id = 106, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3973", },
                    new HierarchyFieldValue { Id = 107, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 2770", },
                    new HierarchyFieldValue { Id = 108, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 0433", },
                    new HierarchyFieldValue { Id = 109, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 4793", },
                    new HierarchyFieldValue { Id = 110, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 2803", },
                    new HierarchyFieldValue { Id = 111, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 1530", },
                    new HierarchyFieldValue { Id = 112, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 0471", },
                    new HierarchyFieldValue { Id = 113, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 2963", },
                    new HierarchyFieldValue { Id = 114, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 2045", },
                    new HierarchyFieldValue { Id = 115, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3982", },
                    new HierarchyFieldValue { Id = 116, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 1514", },
                    new HierarchyFieldValue { Id = 117, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 2304", },
                    new HierarchyFieldValue { Id = 118, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 0533", },
                    new HierarchyFieldValue { Id = 119, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 3612", },
                    new HierarchyFieldValue { Id = 120, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 1786", },
                    new HierarchyFieldValue { Id = 121, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 5978", },
                    new HierarchyFieldValue { Id = 122, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 4199", },
                    new HierarchyFieldValue { Id = 123, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 0093", },
                    new HierarchyFieldValue { Id = 124, HierarchyFieldId = 6, Value = "Assigned Provider (Hier) 4137", },

                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty(Db.Object.HierarchyFieldValue, "HierarchyFieldId", Db.Object.HierarchyField);
            #endregion

            #region Initialize SelectionGroup
            Db.Object.SelectionGroup.AddRange(new List<SelectionGroup>
            {
                new SelectionGroup
                {
                    Id = 1,
                    RootContentItemId = 1,
                    IsMaster = true,
                },
                new SelectionGroup
                {
                    Id = 2,
                    RootContentItemId = 4,
                    SelectedHierarchyFieldValueList = new long[] {13, 16, },  // "Assigned Provider Clinic (Hier) 0434" and "Assigned Provider Clinic (Hier) 0871"
                },
                new SelectionGroup
                {
                    Id = 3,
                    RootContentItemId = 4,
                    SelectedHierarchyFieldValueList = new long[] {12, 14, },  // "Assigned Provider Clinic (Hier) 7252" and "Assigned Provider Clinic (Hier) 7291"
                },
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
            Db.Object.ContentPublicationRequest.AddRange(
                new List<ContentPublicationRequest>
                {
                    new ContentPublicationRequest
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
                    },
                    new ContentPublicationRequest
                    {
                        Id = 2,
                        ApplicationUserId = 1,
                        CreateDateTime = DateTime.Now,
                        RequestStatus = PublicationStatus.Unknown,
                        PublishRequest = new PublishRequest
                        {
                            RootContentItemId = 2,
                            RelatedFiles = new ContentRelatedFile[]
                            {
                                new ContentRelatedFile {FilePurpose = "MasterContent", FileUploadId = Db.Object.FileUpload.ElementAt(0).Id},
                                new ContentRelatedFile {FilePurpose = "UserGuide", FileUploadId = Db.Object.FileUpload.ElementAt(1).Id},
                            }
                        }
                    },
                    new ContentPublicationRequest
                    {
                        Id = 3,
                        ApplicationUserId = 1,
                        CreateDateTime = DateTime.Now,
                        RequestStatus = PublicationStatus.Unknown,
                        PublishRequest = new PublishRequest
                        {
                            RootContentItemId = 3,
                            RelatedFiles = new ContentRelatedFile[]
                            {
                                new ContentRelatedFile {FilePurpose = "MasterContent", FileUploadId = Db.Object.FileUpload.ElementAt(0).Id},
                                new ContentRelatedFile {FilePurpose = "UserGuide", FileUploadId = Db.Object.FileUpload.ElementAt(1).Id},
                            }
                        }
                    },
                    new ContentPublicationRequest
                    {
                        Id = 4,
                        ApplicationUserId = 1,
                        CreateDateTime = DateTime.Now,
                        RequestStatus = PublicationStatus.Unknown,
                        PublishRequest = new PublishRequest
                        {
                            RootContentItemId = 4,
                            RelatedFiles = new ContentRelatedFile[]
                            {
                                new ContentRelatedFile {FilePurpose = "MasterContent", FileUploadId = Db.Object.FileUpload.ElementAt(0).Id},
                                new ContentRelatedFile {FilePurpose = "UserGuide", FileUploadId = Db.Object.FileUpload.ElementAt(1).Id},
                            }
                        }
                    },
                });

            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(Db.Object.ContentPublicationRequest, "RootContentItemId", Db.Object.RootContentItem);
            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(Db.Object.ContentPublicationRequest, "RootContentItemId", Db.Object.ApplicationUser);
            #endregion

            return Db;
        }
    }
}
