using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Milliman.Common.TreeUtilities {
    /// <summary>
    /// this class is used to create a map that shows the field to data table mappings per the data model.  QV
    /// will return all data if no reduction selection is made - so we have to drop tables.  This class is used 
    /// to determine if we have a table that no selection criteria was made and thus need to be dropped
    /// </summary>
    public class DropTableProcessor {
        public class TableClass {
            public string TableName { get; set; }
            public bool DropMe { get; set; }
            public List<string> FieldNames { get; set; }

            public TableClass() {
                FieldNames = new List<string>();
                DropMe = true;
            }
            public TableClass(string _TableName) {
                TableName = _TableName;
                FieldNames = new List<string>();
                DropMe = true;
            }
        }

        public List<TableClass> Tables { get; set; }

        public DropTableProcessor() {
            Tables = new List<TableClass>();
        }

        public bool Save(string QualifiedPath) {
            try {
                Polenter.Serialization.SharpSerializer SS = new Polenter.Serialization.SharpSerializer(false);
                SS.Serialize(Tables, QualifiedPath);
                return true;
            } catch( Exception ) {
                //Milliman.Common.Report.Log(Report.ReportType.Error, "Save error", ex);
            }
            return false;
        }

        static public DropTableProcessor Load(string QualfiedPath) {
            try {
                Polenter.Serialization.SharpSerializer SS = new Polenter.Serialization.SharpSerializer(false);
                DropTableProcessor DTP = new DropTableProcessor();
                DTP.Tables = SS.Deserialize(QualfiedPath) as List<TableClass>;
                return DTP;
            } catch( Exception  ) {
                //MillimanCommon.Report.Log(Report.ReportType.Error, "Load error", ex);
            }
            return null;
        }

        public void SetFieldAccessed(string SelectedFieldName) {
            foreach( TableClass TC in Tables ) {
                foreach( string FieldName in TC.FieldNames ) {
                    if( string.Compare(SelectedFieldName, FieldName, true) == 0 )
                        TC.DropMe = false;
                }
            }
        }

        /// <summary>
        /// This routine looks at the selections made in reference to the
        /// data model and determines if there are tables that need to be 
        /// dropped due to no selections in the data model
        /// </summary>
        /// <param name="DataModelFile"></param>
        /// <returns></returns>
        public List<string> TablesToDrop(string DataModelFile, List<string> AllSelections, out List<string> DroppedSelections) {
            DroppedSelections = new List<string>();
            List<List<string>> DM = new List<List<string>>();
            if( System.IO.File.Exists(DataModelFile) ) {
                Polenter.Serialization.SharpSerializer SS = new Polenter.Serialization.SharpSerializer(false);
                List<string> TempDM = SS.Deserialize(DataModelFile) as List<string>;
                foreach( string DMEntity in TempDM ) {
                    DM.Add(DMEntity.Split(new char[] { '|' }).ToList());
                }
            }

            List<string> DropTableList = new List<string>();
            foreach( TableClass TC in Tables ) {
                if( (TC.DropMe) && (DropTableList.Contains(TC.TableName) == false) )
                    DropTableList.Add(TC.TableName);
            }

            foreach( string HierarchyLevel in AllSelections ) {
                if( IsTopLevel(HierarchyLevel, DM) != null ) {
                    if( ContainsMatchingLeaf(HierarchyLevel, AllSelections, DM) == false ) {  //locate the table to drop and add, this top level item does not have a leaf node
                        foreach( TableClass TC in Tables ) {
                            foreach( string FieldName in TC.FieldNames ) {
                                if( string.Compare(HierarchyLevel, FieldName, true) == 0 ) {
                                    //add the table to drop list
                                    if( DropTableList.Contains(TC.TableName) == false ) {
                                        DropTableList.Add(TC.TableName);
                                        DroppedSelections.Add(HierarchyLevel);
                                    }

                                }
                            }
                        }
                    }
                }
            }

            return DropTableList;
        }

        /// <summary>
        /// Look in the data model data to see if this is a top level item
        /// </summary>
        /// <param name="DataModelHierLevel"></param>
        /// <param name="DataModelHierarchy"></param>
        /// <returns></returns>
        private List<string> IsTopLevel(string DataModelHierLevel, List<List<string>> DataModelHierarchy) {
            foreach( List<string> DMTokens in DataModelHierarchy ) {
                if( DMTokens.Count() > 0 ) {
                    if( string.Compare(DMTokens[0], DataModelHierLevel, true) == 0 )
                        return DMTokens;
                }
            }
            return null;
        }

        /// <summary>
        /// This routine should only be called on a top level hierarchy item, it's behavior is not defined for other
        /// levels of the hierarchy
        /// </summary>
        /// <param name="DataModelHierLevel"></param>
        /// <param name="AllSelections"></param>
        /// <param name="DataModelHierarchy"></param>
        /// <returns></returns>
        private bool ContainsMatchingLeaf(string DataModelHierLevel, List<string> AllSelections, List<List<string>> DataModelHierarchy) {
            List<string> MatchingHierarchy = IsTopLevel(DataModelHierLevel, DataModelHierarchy);
            if( MatchingHierarchy != null ) {
                foreach( string RequiredHierarchyEntry in MatchingHierarchy ) {
                    if( AllSelections.Contains(RequiredHierarchyEntry, StringComparer.CurrentCultureIgnoreCase) == false )
                        return false;
                }
                return true;
            }
            return false;

        }
    }
}