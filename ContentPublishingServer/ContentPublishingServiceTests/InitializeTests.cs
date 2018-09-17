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
                Id = TestUtil.MakeTestGuid(1),
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
                        Id = TestUtil.MakeTestGuid(1),
                        ContentName = "Test content 1",
                        ClientId = TestUtil.MakeTestGuid(1),
                        ContentTypeId = TestUtil.MakeTestGuid(1),
                        TypeSpecificDetail = "{}",
                        DoesReduce = false,
                        Notes = "DoesReduce false",
                        Description = "For use with a single SelectionGroup where IsMaster == true",
                    },
                    new RootContentItem
                    {
                        Id = TestUtil.MakeTestGuid(2),
                        ContentName = "Test content 2",
                        ClientId = TestUtil.MakeTestGuid(1),
                        ContentTypeId = TestUtil.MakeTestGuid(1),
                        TypeSpecificDetail = "{}",
                        DoesReduce = false,
                        Notes = "DoesReduce false",
                        Description = "For use with no SelectionGroup",
                    },
                    new RootContentItem
                    {
                        Id = TestUtil.MakeTestGuid(3),
                        ContentName = "Test content 3",
                        ClientId = TestUtil.MakeTestGuid(1),
                        ContentTypeId = TestUtil.MakeTestGuid(1),
                        TypeSpecificDetail = "{}",
                        DoesReduce = true,
                        Notes = "DoesReduce true",
                        Description = "For use with no SelectionGroup",
                    },
                    new RootContentItem
                    {
                        Id = TestUtil.MakeTestGuid(4),
                        ContentName = "Test content 4",
                        ClientId = TestUtil.MakeTestGuid(1),
                        ContentTypeId = TestUtil.MakeTestGuid(1),
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
                    new HierarchyField { Id = TestUtil.MakeTestGuid(1), RootContentItemId = TestUtil.MakeTestGuid(3), StructureType = FieldStructureType.Flat, FieldName = "All Members (Hier)", FieldDisplayName = "All Members (Hier)", },
                    new HierarchyField { Id = TestUtil.MakeTestGuid(2), RootContentItemId = TestUtil.MakeTestGuid(3), StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider Clinic (Hier)", FieldDisplayName = "Assigned Provider Clinic (Hier)", },
                    new HierarchyField { Id = TestUtil.MakeTestGuid(3), RootContentItemId = TestUtil.MakeTestGuid(3), StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider (Hier)", FieldDisplayName = "Assigned Provider (Hier)", },

                    // RootContentItem 4
                    new HierarchyField { Id = TestUtil.MakeTestGuid(4), RootContentItemId = TestUtil.MakeTestGuid(4), StructureType = FieldStructureType.Flat, FieldName = "All Members (Hier)", FieldDisplayName = "All Members (Hier)", },
                    new HierarchyField { Id = TestUtil.MakeTestGuid(5), RootContentItemId = TestUtil.MakeTestGuid(4), StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider Clinic (Hier)", FieldDisplayName = "Assigned Provider Clinic (Hier)", },
                    new HierarchyField { Id = TestUtil.MakeTestGuid(6), RootContentItemId = TestUtil.MakeTestGuid(4), StructureType = FieldStructureType.Flat, FieldName = "Assigned Provider (Hier)", FieldDisplayName = "Assigned Provider (Hier)", },
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty(Db.Object.HierarchyField, "RootContentItemId", Db.Object.RootContentItem);
            #endregion

            #region Initialize HierarchyFieldValue
            Db.Object.HierarchyFieldValue.AddRange(
                new List<HierarchyFieldValue>
                {
                    // RootContentItem 3
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(1), HierarchyFieldId = TestUtil.MakeTestGuid(1), Value = "All Members (Hier) 3038", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(3), HierarchyFieldId = TestUtil.MakeTestGuid(2), Value = "Assigned Provider Clinic (Hier) 4025", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(4), HierarchyFieldId = TestUtil.MakeTestGuid(2), Value = "Assigned Provider Clinic (Hier) 9438", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(5), HierarchyFieldId = TestUtil.MakeTestGuid(2), Value = "Assigned Provider Clinic (Hier) 7252", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(6), HierarchyFieldId = TestUtil.MakeTestGuid(2), Value = "Assigned Provider Clinic (Hier) 0434", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(7), HierarchyFieldId = TestUtil.MakeTestGuid(2), Value = "Assigned Provider Clinic (Hier) 7291", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(8), HierarchyFieldId = TestUtil.MakeTestGuid(2), Value = "Assigned Provider Clinic (Hier) 3038", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(9), HierarchyFieldId = TestUtil.MakeTestGuid(2), Value = "Assigned Provider Clinic (Hier) 0871", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(17), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3575", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(18), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3173", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(19), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 8199", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(20), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 5056", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(21), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 0753", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(22), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 8747", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(23), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 0024", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(24), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 4185", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(25), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 0868", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(26), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 8409", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(27), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 2445", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(28), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 0691", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(29), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 6434", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(30), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3373", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(31), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3530", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(32), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 1666", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(33), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3855", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(34), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3552", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(35), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 4860", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(36), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 8064", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(37), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 9029", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(38), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 4300", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(39), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 7205", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(40), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 2846", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(41), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 5229", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(42), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 5251", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(43), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 1858", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(44), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 1432", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(45), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 1078", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(46), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 6262", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(47), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 7811", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(48), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 2576", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(49), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 4309", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(50), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 5342", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(51), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 8087", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(52), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3973", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(53), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 2770", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(54), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 0433", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(55), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 4793", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(56), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 2803", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(57), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 1530", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(58), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 0471", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(59), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 2963", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(60), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 2045", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(61), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3982", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(62), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 1514", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(63), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 2304", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(64), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 0533", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(65), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 3612", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(66), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 1786", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(67), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 5978", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(68), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 4199", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(69), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 0093", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(70), HierarchyFieldId = TestUtil.MakeTestGuid(3), Value = "Assigned Provider (Hier) 4137", },

                    // RootContentItem 4
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(2), HierarchyFieldId = TestUtil.MakeTestGuid(4), Value = "All Members (Hier) 3038", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(10), HierarchyFieldId = TestUtil.MakeTestGuid(5), Value = "Assigned Provider Clinic (Hier) 4025", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(11), HierarchyFieldId = TestUtil.MakeTestGuid(5), Value = "Assigned Provider Clinic (Hier) 9438", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(12), HierarchyFieldId = TestUtil.MakeTestGuid(5), Value = "Assigned Provider Clinic (Hier) 7252", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(13), HierarchyFieldId = TestUtil.MakeTestGuid(5), Value = "Assigned Provider Clinic (Hier) 0434", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(14), HierarchyFieldId = TestUtil.MakeTestGuid(5), Value = "Assigned Provider Clinic (Hier) 7291", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(15), HierarchyFieldId = TestUtil.MakeTestGuid(5), Value = "Assigned Provider Clinic (Hier) 3038", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(16), HierarchyFieldId = TestUtil.MakeTestGuid(5), Value = "Assigned Provider Clinic (Hier) 0871", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(71), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3575", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(72), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3173", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(73), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 8199", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(74), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 5056", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(75), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 0753", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(76), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 8747", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(77), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 0024", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(78), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 4185", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(79), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 0868", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(80), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 8409", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(81), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 2445", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(82), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 0691", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(83), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 6434", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(84), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3373", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(85), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3530", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(86), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 1666", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(87), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3855", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(88), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3552", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(89), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 4860", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(90), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 8064", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(91), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 9029", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(92), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 4300", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(93), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 7205", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(94), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 2846", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(95), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 5229", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(96), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 5251", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(97), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 1858", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(98), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 1432", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(99), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 1078", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(100), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 6262", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(101), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 7811", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(102), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 2576", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(103), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 4309", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(104), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 5342", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(105), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 8087", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(106), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3973", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(107), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 2770", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(108), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 0433", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(109), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 4793", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(110), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 2803", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(111), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 1530", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(112), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 0471", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(113), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 2963", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(114), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 2045", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(115), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3982", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(116), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 1514", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(117), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 2304", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(118), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 0533", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(119), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 3612", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(120), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 1786", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(121), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 5978", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(122), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 4199", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(123), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 0093", },
                    new HierarchyFieldValue { Id = TestUtil.MakeTestGuid(124), HierarchyFieldId = TestUtil.MakeTestGuid(6), Value = "Assigned Provider (Hier) 4137", },

                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty(Db.Object.HierarchyFieldValue, "HierarchyFieldId", Db.Object.HierarchyField);
            #endregion

            #region Initialize SelectionGroup
            Db.Object.SelectionGroup.AddRange(new List<SelectionGroup>
            {
                new SelectionGroup
                {
                    Id = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(1),
                    IsMaster = true,
                },
                new SelectionGroup
                {
                    Id = TestUtil.MakeTestGuid(2),
                    RootContentItemId = TestUtil.MakeTestGuid(4),
                    SelectedHierarchyFieldValueList = new Guid[] {TestUtil.MakeTestGuid(13), TestUtil.MakeTestGuid(16), },  // "Assigned Provider Clinic (Hier) 0434" and "Assigned Provider Clinic (Hier) 0871"
                },
                new SelectionGroup
                {
                    Id = TestUtil.MakeTestGuid(3),
                    RootContentItemId = TestUtil.MakeTestGuid(4),
                    SelectedHierarchyFieldValueList = new Guid[] { TestUtil.MakeTestGuid(12), TestUtil.MakeTestGuid(14), },  // "Assigned Provider Clinic (Hier) 7252" and "Assigned Provider Clinic (Hier) 7291"
                },
            });
            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(Db.Object.SelectionGroup, "RootContentItemId", Db.Object.RootContentItem);
            #endregion

            #region Initialize ContentReductionTask
            ContentReductionHierarchy<ReductionFieldValueSelection> ValidSelectionsObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = TestUtil.MakeTestGuid(1),
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = TestUtil.MakeTestGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = TestUtil.MakeTestGuid(1),
                                Value = "Assigned Provider Clinic (Hier) 0434",
                                SelectionStatus = true,
                            },
                            new ReductionFieldValueSelection
                            {
                                Id = TestUtil.MakeTestGuid(2),
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
                Id = TestUtil.MakeTestGuid(1),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = TestUtil.MakeTestGuid(1),
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteriaObj = ValidSelectionsObject,
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> InvalidFieldValueObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = TestUtil.MakeTestGuid(1),
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = TestUtil.MakeTestGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = TestUtil.MakeTestGuid(1),
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
                Id = TestUtil.MakeTestGuid(2),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = TestUtil.MakeTestGuid(1),
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteriaObj = InvalidFieldValueObject,
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> OneValidAndOneInvalidFieldValueObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = TestUtil.MakeTestGuid(1),
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Assigned Provider Clinic (Hier)",
                        DisplayName = "Assigned Provider Clinic (Hier)",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = TestUtil.MakeTestGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = TestUtil.MakeTestGuid(1),
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
                        Id = TestUtil.MakeTestGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = TestUtil.MakeTestGuid(1),
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
                Id = TestUtil.MakeTestGuid(3),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = TestUtil.MakeTestGuid(1),
                MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteriaObj = OneValidAndOneInvalidFieldValueObject,
            });

            ContentReductionHierarchy<ReductionFieldValueSelection> InvalidFieldNameObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = TestUtil.MakeTestGuid(1),
                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Invalid Field Name",
                        DisplayName = "Invalid Field Name",
                        StructureType = FieldStructureType.Tree,
                        ValueDelimiter = "|",
                        Id = TestUtil.MakeTestGuid(1),
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                Id = TestUtil.MakeTestGuid(1),
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
                Id = TestUtil.MakeTestGuid(4),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\CCR_0273ZDM_New_Reduction_Script.qvw",
                SelectionGroupId = TestUtil.MakeTestGuid(1),
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
                    Id = TestUtil.MakeTestGuid(1),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(1),
                    CreateDateTimeUtc = DateTime.UtcNow,
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                    LiveReadyFilesObj = new List<ContentRelatedFile>(),
                });

            Db.Object.ContentPublicationRequest.Add(
                new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(2),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(2),
                    CreateDateTimeUtc = DateTime.UtcNow,
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                    LiveReadyFilesObj = new List<ContentRelatedFile>(),
                });

            Db.Object.ContentPublicationRequest.Add(
                new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(3),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(3),
                    CreateDateTimeUtc = DateTime.UtcNow,
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                    LiveReadyFilesObj = new List<ContentRelatedFile>(),
                });

            Db.Object.ContentPublicationRequest.Add(
                new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(4),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(4),
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
    }
}
