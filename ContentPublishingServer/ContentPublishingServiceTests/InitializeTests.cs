using System;
using System.IO;
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
                Id = MakeGuid(1),
                Name = "Qlikview",
                CanReduce = true,
                TypeEnum = ContentTypeEnum.Qlikview,
            });
            #endregion

            #region Initialize RootContentItem
            Db.Object.RootContentItem.AddRange(
                new List<RootContentItem>
                {
                    new RootContentItem
                    {
                        Id = MakeGuid(1),
                        ContentName = "Test content 1",
                        ClientId = MakeGuid(1),
                        ContentTypeId = MakeGuid(1),
                        TypeSpecificDetail = "{}",
                        DoesReduce = false,
                        Notes = "DoesReduce false",
                        Description = "For use with a single SelectionGroup where IsMaster == true",
                    },
                    new RootContentItem
                    {
                        Id = MakeGuid(2),
                        ContentName = "Test content 2",
                        ClientId = MakeGuid(1),
                        ContentTypeId = MakeGuid(1),
                        TypeSpecificDetail = "{}",
                        DoesReduce = false,
                        Notes = "DoesReduce false",
                        Description = "For use with no SelectionGroup",
                    },
                    new RootContentItem
                    {
                        Id = MakeGuid(3),
                        ContentName = "Test content 3",
                        ClientId = MakeGuid(1),
                        ContentTypeId = MakeGuid(1),
                        TypeSpecificDetail = "{}",
                        DoesReduce = true,
                        Notes = "DoesReduce true",
                        Description = "For use with no SelectionGroup",
                    },
                    new RootContentItem
                    {
                        Id = MakeGuid(4),
                        ContentName = "Test content 4",
                        ClientId = MakeGuid(1),
                        ContentTypeId = MakeGuid(1),
                        TypeSpecificDetail = "{}",
                        DoesReduce = true,
                        Notes = "DoesReduce true",
                        Description = "For use with multiple SelectionGroup",
                    },
                });
            MockDbSet<RootContentItem>.AssignNavigationProperty(Db.Object.RootContentItem, "ContentTypeId", Db.Object.ContentType);
            Directory.CreateDirectory(@"\\indy-syn01\prm_test\ContentRoot");
            #endregion

            #region Initialize HierarchyField
            Db.Object.HierarchyField.AddRange(
                new List<HierarchyField>
                {
                    // RootContentItem 3
                    new HierarchyField { Id = MakeGuid(1), RootContentItemId = MakeGuid(3), StructureType = FieldStructureType.Flat, FieldName = "All Members (Hier)", FieldDisplayName = "All Members (Hier)", },
                    new HierarchyField { Id = MakeGuid(2), RootContentItemId = MakeGuid(3), StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider Clinic (Hier)", FieldDisplayName = "Assigned Provider Clinic (Hier)", },
                    new HierarchyField { Id = MakeGuid(3), RootContentItemId = MakeGuid(3), StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider (Hier)", FieldDisplayName = "Assigned Provider (Hier)", },

                    // RootContentItem 4
                    new HierarchyField { Id = MakeGuid(4), RootContentItemId = MakeGuid(4), StructureType = FieldStructureType.Flat, FieldName = "All Members (Hier)", FieldDisplayName = "All Members (Hier)", },
                    new HierarchyField { Id = MakeGuid(5), RootContentItemId = MakeGuid(4), StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider Clinic (Hier)", FieldDisplayName = "Assigned Provider Clinic (Hier)", },
                    new HierarchyField { Id = MakeGuid(6), RootContentItemId = MakeGuid(4), StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider (Hier)", FieldDisplayName = "Assigned Provider (Hier)", },
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty(Db.Object.HierarchyField, "RootContentItemId", Db.Object.RootContentItem);
            #endregion

            #region Initialize HierarchyFieldValue
            Db.Object.HierarchyFieldValue.AddRange(
                new List<HierarchyFieldValue>
                {
                    // RootContentItem 3
                    new HierarchyFieldValue { Id = MakeGuid(1), HierarchyFieldId = MakeGuid(1), Value = "All Members (Hier) 3038", },
                    new HierarchyFieldValue { Id = MakeGuid(3), HierarchyFieldId = MakeGuid(2), Value = "Assigned Provider Clinic (Hier) 4025", },
                    new HierarchyFieldValue { Id = MakeGuid(4), HierarchyFieldId = MakeGuid(2), Value = "Assigned Provider Clinic (Hier) 9438", },
                    new HierarchyFieldValue { Id = MakeGuid(5), HierarchyFieldId = MakeGuid(2), Value = "Assigned Provider Clinic (Hier) 7252", },
                    new HierarchyFieldValue { Id = MakeGuid(6), HierarchyFieldId = MakeGuid(2), Value = "Assigned Provider Clinic (Hier) 0434", },
                    new HierarchyFieldValue { Id = MakeGuid(7), HierarchyFieldId = MakeGuid(2), Value = "Assigned Provider Clinic (Hier) 7291", },
                    new HierarchyFieldValue { Id = MakeGuid(8), HierarchyFieldId = MakeGuid(2), Value = "Assigned Provider Clinic (Hier) 3038", },
                    new HierarchyFieldValue { Id = MakeGuid(9), HierarchyFieldId = MakeGuid(2), Value = "Assigned Provider Clinic (Hier) 0871", },
                    new HierarchyFieldValue { Id = MakeGuid(17), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3575", },
                    new HierarchyFieldValue { Id = MakeGuid(18), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3173", },
                    new HierarchyFieldValue { Id = MakeGuid(19), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 8199", },
                    new HierarchyFieldValue { Id = MakeGuid(20), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 5056", },
                    new HierarchyFieldValue { Id = MakeGuid(21), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 0753", },
                    new HierarchyFieldValue { Id = MakeGuid(22), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 8747", },
                    new HierarchyFieldValue { Id = MakeGuid(23), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 0024", },
                    new HierarchyFieldValue { Id = MakeGuid(24), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 4185", },
                    new HierarchyFieldValue { Id = MakeGuid(25), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 0868", },
                    new HierarchyFieldValue { Id = MakeGuid(26), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 8409", },
                    new HierarchyFieldValue { Id = MakeGuid(27), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 2445", },
                    new HierarchyFieldValue { Id = MakeGuid(28), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 0691", },
                    new HierarchyFieldValue { Id = MakeGuid(29), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 6434", },
                    new HierarchyFieldValue { Id = MakeGuid(30), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3373", },
                    new HierarchyFieldValue { Id = MakeGuid(31), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3530", },
                    new HierarchyFieldValue { Id = MakeGuid(32), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 1666", },
                    new HierarchyFieldValue { Id = MakeGuid(33), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3855", },
                    new HierarchyFieldValue { Id = MakeGuid(34), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3552", },
                    new HierarchyFieldValue { Id = MakeGuid(35), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 4860", },
                    new HierarchyFieldValue { Id = MakeGuid(36), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 8064", },
                    new HierarchyFieldValue { Id = MakeGuid(37), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 9029", },
                    new HierarchyFieldValue { Id = MakeGuid(38), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 4300", },
                    new HierarchyFieldValue { Id = MakeGuid(39), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 7205", },
                    new HierarchyFieldValue { Id = MakeGuid(40), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 2846", },
                    new HierarchyFieldValue { Id = MakeGuid(41), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 5229", },
                    new HierarchyFieldValue { Id = MakeGuid(42), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 5251", },
                    new HierarchyFieldValue { Id = MakeGuid(43), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 1858", },
                    new HierarchyFieldValue { Id = MakeGuid(44), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 1432", },
                    new HierarchyFieldValue { Id = MakeGuid(45), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 1078", },
                    new HierarchyFieldValue { Id = MakeGuid(46), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 6262", },
                    new HierarchyFieldValue { Id = MakeGuid(47), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 7811", },
                    new HierarchyFieldValue { Id = MakeGuid(48), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 2576", },
                    new HierarchyFieldValue { Id = MakeGuid(49), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 4309", },
                    new HierarchyFieldValue { Id = MakeGuid(50), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 5342", },
                    new HierarchyFieldValue { Id = MakeGuid(51), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 8087", },
                    new HierarchyFieldValue { Id = MakeGuid(52), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3973", },
                    new HierarchyFieldValue { Id = MakeGuid(53), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 2770", },
                    new HierarchyFieldValue { Id = MakeGuid(54), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 0433", },
                    new HierarchyFieldValue { Id = MakeGuid(55), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 4793", },
                    new HierarchyFieldValue { Id = MakeGuid(56), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 2803", },
                    new HierarchyFieldValue { Id = MakeGuid(57), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 1530", },
                    new HierarchyFieldValue { Id = MakeGuid(58), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 0471", },
                    new HierarchyFieldValue { Id = MakeGuid(59), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 2963", },
                    new HierarchyFieldValue { Id = MakeGuid(60), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 2045", },
                    new HierarchyFieldValue { Id = MakeGuid(61), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3982", },
                    new HierarchyFieldValue { Id = MakeGuid(62), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 1514", },
                    new HierarchyFieldValue { Id = MakeGuid(63), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 2304", },
                    new HierarchyFieldValue { Id = MakeGuid(64), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 0533", },
                    new HierarchyFieldValue { Id = MakeGuid(65), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 3612", },
                    new HierarchyFieldValue { Id = MakeGuid(66), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 1786", },
                    new HierarchyFieldValue { Id = MakeGuid(67), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 5978", },
                    new HierarchyFieldValue { Id = MakeGuid(68), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 4199", },
                    new HierarchyFieldValue { Id = MakeGuid(69), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 0093", },
                    new HierarchyFieldValue { Id = MakeGuid(70), HierarchyFieldId = MakeGuid(3), Value = "Assigned Provider (Hier) 4137", },

                    // RootContentItem 4
                    new HierarchyFieldValue { Id = MakeGuid(2), HierarchyFieldId = MakeGuid(4), Value = "All Members (Hier) 3038", },
                    new HierarchyFieldValue { Id = MakeGuid(10), HierarchyFieldId = MakeGuid(5), Value = "Assigned Provider Clinic (Hier) 4025", },
                    new HierarchyFieldValue { Id = MakeGuid(11), HierarchyFieldId = MakeGuid(5), Value = "Assigned Provider Clinic (Hier) 9438", },
                    new HierarchyFieldValue { Id = MakeGuid(12), HierarchyFieldId = MakeGuid(5), Value = "Assigned Provider Clinic (Hier) 7252", },
                    new HierarchyFieldValue { Id = MakeGuid(13), HierarchyFieldId = MakeGuid(5), Value = "Assigned Provider Clinic (Hier) 0434", },
                    new HierarchyFieldValue { Id = MakeGuid(14), HierarchyFieldId = MakeGuid(5), Value = "Assigned Provider Clinic (Hier) 7291", },
                    new HierarchyFieldValue { Id = MakeGuid(15), HierarchyFieldId = MakeGuid(5), Value = "Assigned Provider Clinic (Hier) 3038", },
                    new HierarchyFieldValue { Id = MakeGuid(16), HierarchyFieldId = MakeGuid(5), Value = "Assigned Provider Clinic (Hier) 0871", },
                    new HierarchyFieldValue { Id = MakeGuid(71), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3575", },
                    new HierarchyFieldValue { Id = MakeGuid(72), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3173", },
                    new HierarchyFieldValue { Id = MakeGuid(73), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 8199", },
                    new HierarchyFieldValue { Id = MakeGuid(74), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 5056", },
                    new HierarchyFieldValue { Id = MakeGuid(75), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 0753", },
                    new HierarchyFieldValue { Id = MakeGuid(76), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 8747", },
                    new HierarchyFieldValue { Id = MakeGuid(77), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 0024", },
                    new HierarchyFieldValue { Id = MakeGuid(78), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 4185", },
                    new HierarchyFieldValue { Id = MakeGuid(79), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 0868", },
                    new HierarchyFieldValue { Id = MakeGuid(80), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 8409", },
                    new HierarchyFieldValue { Id = MakeGuid(81), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 2445", },
                    new HierarchyFieldValue { Id = MakeGuid(82), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 0691", },
                    new HierarchyFieldValue { Id = MakeGuid(83), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 6434", },
                    new HierarchyFieldValue { Id = MakeGuid(84), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3373", },
                    new HierarchyFieldValue { Id = MakeGuid(85), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3530", },
                    new HierarchyFieldValue { Id = MakeGuid(86), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 1666", },
                    new HierarchyFieldValue { Id = MakeGuid(87), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3855", },
                    new HierarchyFieldValue { Id = MakeGuid(88), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3552", },
                    new HierarchyFieldValue { Id = MakeGuid(89), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 4860", },
                    new HierarchyFieldValue { Id = MakeGuid(90), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 8064", },
                    new HierarchyFieldValue { Id = MakeGuid(91), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 9029", },
                    new HierarchyFieldValue { Id = MakeGuid(92), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 4300", },
                    new HierarchyFieldValue { Id = MakeGuid(93), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 7205", },
                    new HierarchyFieldValue { Id = MakeGuid(94), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 2846", },
                    new HierarchyFieldValue { Id = MakeGuid(95), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 5229", },
                    new HierarchyFieldValue { Id = MakeGuid(96), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 5251", },
                    new HierarchyFieldValue { Id = MakeGuid(97), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 1858", },
                    new HierarchyFieldValue { Id = MakeGuid(98), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 1432", },
                    new HierarchyFieldValue { Id = MakeGuid(99), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 1078", },
                    new HierarchyFieldValue { Id = MakeGuid(100), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 6262", },
                    new HierarchyFieldValue { Id = MakeGuid(101), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 7811", },
                    new HierarchyFieldValue { Id = MakeGuid(102), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 2576", },
                    new HierarchyFieldValue { Id = MakeGuid(103), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 4309", },
                    new HierarchyFieldValue { Id = MakeGuid(104), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 5342", },
                    new HierarchyFieldValue { Id = MakeGuid(105), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 8087", },
                    new HierarchyFieldValue { Id = MakeGuid(106), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3973", },
                    new HierarchyFieldValue { Id = MakeGuid(107), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 2770", },
                    new HierarchyFieldValue { Id = MakeGuid(108), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 0433", },
                    new HierarchyFieldValue { Id = MakeGuid(109), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 4793", },
                    new HierarchyFieldValue { Id = MakeGuid(110), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 2803", },
                    new HierarchyFieldValue { Id = MakeGuid(111), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 1530", },
                    new HierarchyFieldValue { Id = MakeGuid(112), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 0471", },
                    new HierarchyFieldValue { Id = MakeGuid(113), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 2963", },
                    new HierarchyFieldValue { Id = MakeGuid(114), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 2045", },
                    new HierarchyFieldValue { Id = MakeGuid(115), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3982", },
                    new HierarchyFieldValue { Id = MakeGuid(116), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 1514", },
                    new HierarchyFieldValue { Id = MakeGuid(117), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 2304", },
                    new HierarchyFieldValue { Id = MakeGuid(118), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 0533", },
                    new HierarchyFieldValue { Id = MakeGuid(119), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 3612", },
                    new HierarchyFieldValue { Id = MakeGuid(120), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 1786", },
                    new HierarchyFieldValue { Id = MakeGuid(121), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 5978", },
                    new HierarchyFieldValue { Id = MakeGuid(122), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 4199", },
                    new HierarchyFieldValue { Id = MakeGuid(123), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 0093", },
                    new HierarchyFieldValue { Id = MakeGuid(124), HierarchyFieldId = MakeGuid(6), Value = "Assigned Provider (Hier) 4137", },

                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty(Db.Object.HierarchyFieldValue, "HierarchyFieldId", Db.Object.HierarchyField);
            #endregion

            #region Initialize SelectionGroup
            Db.Object.SelectionGroup.AddRange(new List<SelectionGroup>
            {
                new SelectionGroup
                {
                    Id = MakeGuid(1),
                    RootContentItemId = MakeGuid(1),
                    IsMaster = true,
                },
                new SelectionGroup
                {
                    Id = MakeGuid(2),
                    RootContentItemId = MakeGuid(4),
                    SelectedHierarchyFieldValueList = new Guid[] {MakeGuid(13), MakeGuid(16), },  // "Assigned Provider Clinic (Hier) 0434" and "Assigned Provider Clinic (Hier) 0871"
                },
                new SelectionGroup
                {
                    Id = MakeGuid(3),
                    RootContentItemId = MakeGuid(4),
                    SelectedHierarchyFieldValueList = new Guid[] { MakeGuid(12), MakeGuid(14), },  // "Assigned Provider Clinic (Hier) 7252" and "Assigned Provider Clinic (Hier) 7291"
                },
            });
            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(Db.Object.SelectionGroup, "RootContentItemId", Db.Object.RootContentItem);
            #endregion

            #region Initialize ContentReductionTask
            ContentReductionHierarchy<ReductionFieldValueSelection> ValidSelectionsObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = MakeGuid(1),
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = MakeGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = MakeGuid(1),
                                Value = "Assigned Provider Clinic (Hier) 0434",
                                SelectionStatus = true,
                            },
                            new ReductionFieldValueSelection
                            {
                                Id = MakeGuid(2),
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
                Id = MakeGuid(1),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = MakeGuid(1),
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteriaObj = ValidSelectionsObject,
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> InvalidFieldValueObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = MakeGuid(1),
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = MakeGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = MakeGuid(1),
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
                Id = MakeGuid(2),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = MakeGuid(1),
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteriaObj = InvalidFieldValueObject,
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> OneValidAndOneInvalidFieldValueObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = MakeGuid(1),
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = MakeGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = MakeGuid(1),
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
                        Id = MakeGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = MakeGuid(1),
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
                Id = MakeGuid(3),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = MakeGuid(1),
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteriaObj = OneValidAndOneInvalidFieldValueObject,
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> InvalidFieldNameObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = MakeGuid(1),
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Invalid Field Name",
                        DisplayName = "Invalid Field Name",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = MakeGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = MakeGuid(1),
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
                Id = MakeGuid(4),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = MakeGuid(1),
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteriaObj = InvalidFieldNameObject,
            });

            MockDbSet<ContentReductionTask>.AssignNavigationProperty<SelectionGroup>(Db.Object.ContentReductionTask, "SelectionGroupId", Db.Object.SelectionGroup);
            #endregion

            #region Initialize ContentPublicationRequest
            // Valid request
            Db.Object.ContentPublicationRequest.Add(
                new ContentPublicationRequest
                {
                    Id = MakeGuid(1),
                    ApplicationUserId = MakeGuid(1),
                    RootContentItemId = MakeGuid(1),
                    CreateDateTimeUtc = DateTime.UtcNow,
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                    LiveReadyFilesObj = new List<ContentRelatedFile>(),
                });

            Db.Object.ContentPublicationRequest.Add(
                new ContentPublicationRequest
                {
                    Id = MakeGuid(2),
                    ApplicationUserId = MakeGuid(1),
                    RootContentItemId = MakeGuid(2),
                    CreateDateTimeUtc = DateTime.UtcNow,
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                    LiveReadyFilesObj = new List<ContentRelatedFile>(),
                });

            Db.Object.ContentPublicationRequest.Add(
                new ContentPublicationRequest
                {
                    Id = MakeGuid(3),
                    ApplicationUserId = MakeGuid(1),
                    RootContentItemId = MakeGuid(3),
                    CreateDateTimeUtc = DateTime.UtcNow,
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                    LiveReadyFilesObj = new List<ContentRelatedFile>(),
                });

            Db.Object.ContentPublicationRequest.Add(
                new ContentPublicationRequest
                {
                    Id = MakeGuid(4),
                    ApplicationUserId = MakeGuid(1),
                    RootContentItemId = MakeGuid(4),
                    CreateDateTimeUtc = DateTime.UtcNow,
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                    LiveReadyFilesObj = new List<ContentRelatedFile>(),
                });

            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(Db.Object.ContentPublicationRequest, "RootContentItemId", Db.Object.RootContentItem);
            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(Db.Object.ContentPublicationRequest, "RootContentItemId", Db.Object.ApplicationUser);
            #endregion

            return Db;
        }

        private static Guid MakeGuid(int intVal)
        {
            return new Guid(intVal, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        }
    }
}
