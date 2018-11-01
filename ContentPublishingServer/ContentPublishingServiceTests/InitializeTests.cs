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
                    SelectedHierarchyFieldValueList = new List<Guid> {TestUtil.MakeTestGuid(13), TestUtil.MakeTestGuid(16), },  // "Assigned Provider Clinic (Hier) 0434" and "Assigned Provider Clinic (Hier) 0871"
                },
                new SelectionGroup
                {
                    Id = TestUtil.MakeTestGuid(3),
                    RootContentItemId = TestUtil.MakeTestGuid(4),
                    SelectedHierarchyFieldValueList = new List<Guid> { TestUtil.MakeTestGuid(12), TestUtil.MakeTestGuid(14), },  // "Assigned Provider Clinic (Hier) 7252" and "Assigned Provider Clinic (Hier) 7291"
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

            var veryLongValidSelectionsObject = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = TestUtil.MakeTestGuid(1),

                Fields = new List<ReductionField<ReductionFieldValueSelection>>
                {
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Population",
                        DisplayName = "Population",
                        StructureType = FieldStructureType.Flat,
                        ValueDelimiter = "",
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Population 2844",
                            },
                        },
                    },
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Practice",
                        DisplayName = "Practice",
                        StructureType = FieldStructureType.Flat,
                        ValueDelimiter = "",
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0009",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0031",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0105",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0163",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0277",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0373",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0675",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0692",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0729",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0817",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0827",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0832",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0874",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 0974",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 1001",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 1116",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 1243",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 1739",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 1847",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 1879",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2190",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2328",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2370",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2467",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2598",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2616",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2665",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2702",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2711",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 2763",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3134",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3215",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3218",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3232",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3287",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3298",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3332",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3340",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3360",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3366",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3416",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3447",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3637",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3694",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3701",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3783",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3793",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3800",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 3816",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4136",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4211",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4381",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4553",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4557",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4600",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4698",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4757",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 4927",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5078",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5079",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5234",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5235",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5267",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5312",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5595",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5651",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5903",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5915",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 5935",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6050",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6180",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6221",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6307",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6313",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6333",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6548",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6658",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6677",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6797",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 6825",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 7183",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 7259",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 7574",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 7622",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 7827",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 7846",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 7970",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8079",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8081",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8123",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8164",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8311",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8414",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8499",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8568",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8730",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8819",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8859",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 8948",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 9036",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 9153",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 9246",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 9494",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 9618",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 9632",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 9701",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Practice 9779",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Unknown",
                            },
                        },
                    },
                    new ReductionField<ReductionFieldValueSelection>
                    {
                        FieldName = "Provider",
                        DisplayName = "Provider",
                        StructureType = FieldStructureType.Flat,
                        ValueDelimiter = "",
                        Values = new List<ReductionFieldValueSelection>
                        {
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0019",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0031",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0033",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0045",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0057",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0071",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0092",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0118",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0162",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0172",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0174",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0209",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0224",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0241",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0277",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0287",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0306",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0330",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0362",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0387",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0417",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0420",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0431",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0454",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0464",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0481",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0489",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0492",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0502",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0517",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0532",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0537",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0586",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0613",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0616",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0622",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0630",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0641",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0648",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0658",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0664",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0682",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0694",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0702",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0704",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0711",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0715",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0728",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0744",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0762",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0792",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0817",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0837",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0841",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0843",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0854",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0872",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0887",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0902",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0928",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0978",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0998",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 0999",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1006",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1028",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1049",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1059",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1062",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1077",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1078",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1113",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1126",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1149",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1155",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1160",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1173",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1176",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1196",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1197",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1206",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1212",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1220",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1233",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1234",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1235",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1273",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1327",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1336",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1379",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1398",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1400",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1402",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1421",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1423",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1433",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1448",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1483",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1515",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1536",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1542",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1578",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1591",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1593",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1598",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1605",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1611",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1618",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1650",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1668",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1718",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1726",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1727",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1758",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1821",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1836",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1840",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1842",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1899",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1918",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1921",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1925",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1934",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1939",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1961",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 1972",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2002",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2041",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2075",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2084",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2118",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2140",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2147",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2152",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2172",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2185",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2222",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2228",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2232",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2241",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2294",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2310",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2324",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2338",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2363",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2369",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2392",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2412",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2419",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2423",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2440",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2449",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2465",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2497",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2513",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2529",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2584",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2591",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2613",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2618",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2632",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2657",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2667",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2681",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2724",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2743",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2761",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2763",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2773",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2786",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2830",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2865",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2870",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2872",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2904",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2949",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2950",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2962",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2983",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2996",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 2997",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3022",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3026",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3044",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3048",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3051",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3059",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3061",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3090",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3096",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3112",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3116",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3142",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3165",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3208",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3222",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3240",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3242",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3252",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3256",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3286",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3292",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3317",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3330",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3334",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3341",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3349",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3377",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3394",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3414",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3422",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3436",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3437",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3463",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3510",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3540",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3558",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3575",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3598",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3605",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3644",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3655",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3658",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3697",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3704",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3717",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3725",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3738",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3741",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3767",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3772",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3783",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3803",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3807",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3831",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3833",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3841",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3846",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3861",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3881",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3907",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3913",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3929",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3941",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3946",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3983",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3987",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 3994",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4005",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4021",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4027",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4034",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4053",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4060",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4073",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4101",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4105",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4120",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4141",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4142",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4150",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4156",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4164",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4183",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4198",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4200",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4222",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4235",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4246",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4257",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4264",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4273",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4281",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4285",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4320",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4355",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4375",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4405",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4423",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4459",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4472",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4495",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4498",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4499",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4560",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4562",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4564",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4576",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4581",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4588",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4612",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4641",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4652",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4658",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4688",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4702",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4707",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4708",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4718",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4733",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4745",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4758",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4760",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4766",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4772",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4778",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4795",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4803",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4804",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4814",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4839",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4850",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4863",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4874",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4879",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4888",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4897",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4954",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4972",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4984",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 4994",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5026",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5029",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5031",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5044",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5059",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5072",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5085",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5147",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5173",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5192",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5200",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5212",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5213",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5218",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5253",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5260",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5271",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5281",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5291",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5296",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5329",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5340",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5355",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5358",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5420",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5429",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5430",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5450",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5451",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5482",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5492",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5495",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5497",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5504",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5537",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5564",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5566",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5638",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5644",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5661",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5662",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5670",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5673",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5676",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5720",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5726",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5735",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5761",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5775",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5784",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5789",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5793",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5795",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5811",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5821",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5822",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5850",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5855",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5883",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5925",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5979",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 5994",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6003",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6007",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6017",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6019",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6020",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6022",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6023",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6025",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6052",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6072",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6099",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6105",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6147",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6153",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6177",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6182",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6200",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6224",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6238",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6251",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6253",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6273",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6296",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6336",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6337",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6339",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6367",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6403",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6428",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6439",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6440",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6463",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6480",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6486",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6498",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6516",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6524",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6527",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6528",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6538",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6577",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6580",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6595",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6601",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6612",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6628",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6638",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6642",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6659",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6662",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6663",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6670",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6674",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6680",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6715",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6722",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6744",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6775",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6779",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6814",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6816",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6824",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6832",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6840",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6846",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6921",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6934",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6936",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6939",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6950",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6956",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6957",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 6990",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7006",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7015",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7031",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7095",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7109",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7144",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7147",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7183",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7190",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7199",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7221",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7239",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7241",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7255",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7281",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7305",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7320",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7329",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7347",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7348",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7350",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7362",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7368",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7374",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7379",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7423",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7468",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7488",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7496",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7514",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7517",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7575",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7579",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7612",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7615",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7646",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7693",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7699",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7703",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7704",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7708",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7714",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7761",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7769",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7790",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7799",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7824",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7837",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7855",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7858",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7866",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7867",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7896",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7917",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7923",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7928",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7958",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7964",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 7974",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8015",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8066",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8068",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8083",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8105",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8113",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8115",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8136",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8149",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8159",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8165",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8195",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8213",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8216",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8231",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8255",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8257",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8272",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8288",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8308",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8311",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8322",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8330",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8358",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8371",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8373",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8378",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8381",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8388",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8410",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8411",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8427",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8441",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8450",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8472",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8497",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8526",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8530",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8618",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8628",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8668",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8685",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8694",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8699",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8708",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8735",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8738",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8777",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8801",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8807",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8826",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8846",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8849",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8893",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8894",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8903",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8905",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8910",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8911",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8916",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8929",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8934",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8939",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8962",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8977",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8987",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 8990",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9008",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9021",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9027",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9036",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9038",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9048",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9088",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9108",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9123",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9174",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9195",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9205",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9210",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9217",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9228",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9231",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9256",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9258",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9262",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9274",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9280",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9295",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9302",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9320",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9374",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9388",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9396",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9423",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9440",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9472",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9498",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9512",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9516",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9526",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9532",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9554",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9555",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9568",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9582",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9626",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9630",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9632",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9639",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9640",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9643",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9657",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9662",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9668",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9673",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9674",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9682",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9703",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9712",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9717",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9745",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9761",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9781",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9798",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9799",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9824",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9826",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9845",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9849",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9872",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9876",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9877",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9901",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9902",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9915",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9917",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9941",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9942",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9973",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9985",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9987",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Provider 9992",
                            },
                            new ReductionFieldValueSelection
                            {
                                SelectionStatus = true,
                                Value = "Unknown",
                            },
                        },
                    },
                },
            };

            Db.Object.ContentReductionTask.Add(new ContentReductionTask
            {
                Id = TestUtil.MakeTestGuid(5),
                TaskAction = TaskActionEnum.HierarchyAndReduction,
                CreateDateTimeUtc = DateTime.UtcNow,
                MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\Test1\Care_Coordinator_Report.qvw",
                SelectionGroupId = TestUtil.MakeTestGuid(1),
                MasterContentChecksum = "0756809FC22CB3429D9960611E68AE8131691AB0",
                ReductionStatus = ReductionStatusEnum.Unspecified,
                SelectionCriteriaObj = veryLongValidSelectionsObject,
            });


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
            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(Db.Object.ContentPublicationRequest, "ApplicationUserId", Db.Object.ApplicationUser);
            #endregion

            return Db;
        }
    }
}
