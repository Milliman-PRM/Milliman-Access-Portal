using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Milliman.Common.TreeUtilities {
    /// <summary>
    /// Generates the hierarchy, model and cononcept files from files extracted by partial reload
    /// The directory must contain a "hierarchy.def" file
    /// </summary>
    public class TreeBuilderProcessor {
        private string HierarchyFileName = @"hierarchy.def";
        public string LastErrorMessage { get; set; }

        //this dictionary uses a key which is the data model name, and the list are
        //unique values for the key,  used to prevent bad values from being passed in
        //when reducing
        private Dictionary<string, List<string>> UniqueValuesPerColumn = new Dictionary<string, List<string>>();

        public TreeBuilderProcessor() {}

        /// <summary>
        /// Primary routine, provided a directory "ExtractedDataDirectory" that contains a "hierarchy.def" file, 
        /// process the data to generated a hierarchy file, a concept file and a data model map file
        /// </summary>
        /// <param name="ExtactedDataDirectory">Directory to process</param>
        /// <param name="HierarchyFiles">The output tree file which will be used  in UI for client administation</param>
        /// <param name="ConceptLabelFiles">File containing the user friendly names of the data model</param>
        /// <param name="DropTableFiles">File containg tables to drop</param>
        /// <param name="DataModelMapFile">File used to map field values to data model</param>
        /// <param name="EncodeData">Debug parameter, set to false to see data, otherwise leave default value to true so data is encoded correctly for transport</param>
        /// <returns></returns>
        public bool BuildTree(string ExtactedDataDirectory, out List<string> HierarchyFiles, out List<string> ConceptLabelFiles, out List<string> DropTableFiles, out List<string> DataModelMapFile, bool EncodeData = true) {
            HierarchyFiles = new List<string>();
            ConceptLabelFiles = new List<string>();
            DropTableFiles = new List<string>();
            DataModelMapFile = new List<string>();


            if( System.IO.Directory.Exists(ExtactedDataDirectory) == false ) {
                LastErrorMessage = "Data extraction directory '" + ExtactedDataDirectory + "' does not exist.";
                return false;
            }


            if( System.IO.File.Exists(System.IO.Path.Combine(ExtactedDataDirectory, HierarchyFileName)) == false ) {
                LastErrorMessage = "Data extraction hierarch defination file '" + System.IO.Path.Combine(ExtactedDataDirectory, HierarchyFileName) + "' does not exist.";
                return false;
            }

            //for multiple hiearchies we will more than one of these Hierarch items
            List<Levels> Hierarchy = new List<Levels>();
            List<string> AllDataItems = new List<string>();

            string QualifiedHierFile = System.IO.Path.Combine(ExtactedDataDirectory, HierarchyFileName);
            System.IO.StreamReader file = new System.IO.StreamReader(QualifiedHierFile);
            string Line = string.Empty;
            int Index = 0;
            while( (Line = file.ReadLine()) != null ) {
                if( Index != 0 )//first row are labels
                {
                    Line = Line.Replace('"', ' ');
                    Line = Line.Replace('~', '|');
                    Line = Line.Trim();
                    string[] Tokens = Line.Split(new char[] { '|' });
                    Levels NewLevel = new Levels(Tokens[0], Tokens[1], Tokens[2]);
                    for( int TokenIndex = 3; TokenIndex < Tokens.Count(); TokenIndex++ ) {
                        NewLevel.DataModelName.Add(Tokens[TokenIndex].ToLower());
                        //each data model name should have one of these
                        UniqueValuesPerColumn.Add(Tokens[TokenIndex], new List<string>());
                    }
                    Hierarchy.Add(NewLevel);
                    AllDataItems.AddRange(NewLevel.DataModelName);
                }
                Index++;
            }

            file.Close();

            //create the data model map file, this file allows different levels of data models to be assocated together
            DataModelMapFile.Add(GenerateDataModelMapFile(QualifiedHierFile));

            ///check levels,  each level should have the same number of data model items
            int NumberDataModels = -1;
            foreach( Levels L in Hierarchy ) {
                if( NumberDataModels == -1 )
                    NumberDataModels = L.DataModelName.Count;

                if( L.DataModelName.Count != NumberDataModels ) {
                    LastErrorMessage = "Hierarchy must contain a entry for each data model to be queried";
                    return false;
                }
            }

            //the drop table processor will create a file transported along with the project to 
            //help determine a run time if a table should be dropped
            // MillimanCommon.TreeUtilities.DropTableProcessor DropTable = new TreeUtilities.DropTableProcessor();
            TreeNode Root = new TreeNode(null);
            List<string> CandidateFiles = FindCandidateFiles(ExtactedDataDirectory, AllDataItems);
            //generate map file so we can determine which tables have been selected from
            DropTableFiles = GenerateMapFile(CandidateFiles, AllDataItems);

            //this code does not take into account hierarchies of hierarchies
            for( int DataModelIndex = 0; DataModelIndex < Hierarchy[0].DataModelName.Count; DataModelIndex++ ) {
                foreach( string CanFile in CandidateFiles ) {
                    List<string> DataModelFields = new List<string>();

                    foreach( Levels L in Hierarchy ) {
                        DataModelFields.Add(L.DataModelName[DataModelIndex]);
                    }

                    List<List<string>> ColumnData = GetColumnData(CanFile, DataModelFields);
                    if( ColumnData != null ) {
                        TreeNode Current = Root;
                        int LevelIndex = 0;
                        foreach( List<string> Row in ColumnData ) {
                            LevelIndex = 0;
                            Current = Root;
                            foreach( string RowItem in Row ) {
                                if( string.IsNullOrEmpty(Current.DisplayFieldName) == true )
                                    Current.DisplayFieldName = Hierarchy[LevelIndex].DisplayLabel;
                                if( Current.DataModelFieldName.Contains(DataModelFields[LevelIndex]) == false )
                                    Current.DataModelFieldName.Add(DataModelFields[LevelIndex]);

                                if( Current.SubNodes.ContainsKey(RowItem) == false ) {
                                    Current.SubNodes.Add(RowItem, new TreeNode(Current));
                                }
                                Current = Current.SubNodes[RowItem];
                                LevelIndex++;
                            }
                        }
                    }
                }
            }

            string TreeXML = Root.ToXML(UniqueValuesPerColumn, EncodeData);
            string TempFile = System.IO.Path.GetTempFileName();

            System.IO.File.WriteAllText(TempFile, TreeXML);

            HierarchyFiles.Add(TempFile);

            //now we need to create the concept file
            string ConceptLabel = string.Empty;
            foreach( Levels L in Hierarchy ) {
                if( string.IsNullOrEmpty(ConceptLabel) == false )
                    ConceptLabel += "|";
                ConceptLabel += L.DisplayLabel;
            }

            string ConceptFile = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(ConceptFile, ConceptLabel);
            ConceptLabelFiles.Add(ConceptFile);

            return true;
        }

        /// <summary>
        /// Return a list of mapping files
        /// </summary>
        /// <param name="CandidateFiles"></param>
        /// <param name="HeaderItems"></param>
        /// <returns></returns>
        private List<string> GenerateMapFile(List<string> CandidateFiles, List<string> HeaderItems) {
            Milliman.Common.TreeUtilities.DropTableProcessor DropTable = new TreeUtilities.DropTableProcessor();
            List<string> DropFiles = new List<string>();
            foreach( string CanFile in CandidateFiles ) {
                DropTable.Tables.Add(new TreeUtilities.DropTableProcessor.TableClass(System.IO.Path.GetFileNameWithoutExtension(CanFile)));
                System.IO.StreamReader file = new System.IO.StreamReader(CanFile);
                //prepend header line with pipe since we are not going to parse this, just search the 
                //string and want it to be consistent - pipe is delimiter
                string HeaderLine = "|" + file.ReadLine().ToLower();
                file.Close();
                foreach( string Header in HeaderItems ) {
                    //search prpended using pipe this will make all the string unique and keep labels/sublabels
                    //such as "mem_hier1" and "exc_mem_hier1" causing issues
                    if( HeaderLine.Contains("|" + Header.ToLower()) )
                        DropTable.Tables[DropTable.Tables.Count - 1].FieldNames.Add(Header);
                }
            }
            string DropTableFile = System.IO.Path.GetTempFileName();
            DropTable.Save(DropTableFile);
            DropFiles.Add(DropTableFile);
            return DropFiles;
        }

        /// <summary>
        /// Reads def file to determine how the data models levels are related
        /// </summary>
        /// <param name="QualifiedHierFile"></param>
        /// <notes> To support multiple hierarchies this rounte needs ot be re-factored to
        /// match on the hierarchy name field of the def file and return a list of data model files
        /// </notes>
        /// <returns></returns>
        private string GenerateDataModelMapFile(string QualifiedHierFile) {
            string DMFile = string.Empty;

            List<string> DataModelMap = new List<string>();

            List<string> FileRows = System.IO.File.ReadLines(QualifiedHierFile).ToList();
            //first row is a header, so skip
            for( int Index = 1; Index < FileRows.Count(); Index++ ) {
                //get the data models fields, delimited by |
                string Line = FileRows[Index];
                Line = Line.Trim();
                string[] Tokens = Line.Split(new char[] { '|' });
                string DMFields = Tokens[3];

                //data model field may include multiple models, delimted by ~
                List<string> DataModelFieldsByLevel = DMFields.Split(new char[] { '~' }).ToList();
                for( int DMIndex = 0; DMIndex < DataModelFieldsByLevel.Count(); DMIndex++ ) {
                    if( DataModelMap.Count() - 1 < DMIndex )
                        DataModelMap.Add("");  //add and empty item just want to make sure we have a bucket
                    if( string.IsNullOrEmpty(DataModelMap[DMIndex]) == false )
                        DataModelMap[DMIndex] += "|";
                    DataModelMap[DMIndex] += DataModelFieldsByLevel[DMIndex];
                }
            }
            DMFile = System.IO.Path.GetTempFileName();
            Polenter.Serialization.SharpSerializer SS = new Polenter.Serialization.SharpSerializer(false);
            SS.Serialize(DataModelMap, DMFile);

            return DMFile;
        }

        /// <summary>
        /// Returns columns as data as specified by ColumnNames, or null if data is missing
        /// </summary>
        /// <param name="Filename"></param>
        /// <param name="ColumnNames"></param>
        /// <returns></returns>
        private List<List<string>> GetColumnData(string Filename, List<string> ColumnNames) {
            List<int> ColumnIndexs = new List<int>();
            List<string> ColumnIndexNames = new List<string>();

            List<List<string>> ListListData = new List<List<string>>();

            System.IO.StreamReader file = new System.IO.StreamReader(Filename);
            string CurrentLine = file.ReadLine();
            if( string.IsNullOrEmpty(CurrentLine) == true ) {
                LastErrorMessage = "Attempted to read header of file '" + Filename + "' but was empty";
                file.Close();
                return null;
            }
            //CurrentLine = CurrentLine.ToLower(); //make header all lower case
            string[] FileColumnNames = CurrentLine.Split(new char[] { '|' });
            List<string> CaseCorrections = new List<string>();
            foreach( string CN in ColumnNames ) {
                //if (FileColumnNames.Contains(CN) == false)
                //    return null;
                int Index = 0;
                foreach( string FCN in FileColumnNames ) {
                    if( string.Compare(FCN, CN, true) == 0 ) {
                        if( CaseCorrections.Contains(FCN) == false )  //create a list that has case corrected
                            CaseCorrections.Add(FCN);
                        ColumnIndexNames.Add(FCN);
                        ColumnIndexs.Add(Index);
                        break;
                    }
                    Index++;
                }
            }
            //this silly loop will correct the case of the data model fiels,  the fields originate from the script added to the 
            //QVW, however thier case may not be correct as to the true data model field value.  This will ensure it matches the
            //data model field value case
            foreach( string CaseCorrection in CaseCorrections ) {
                int Index = ColumnNames.IndexOf(CaseCorrection.ToLower());
                if( Index != -1 )
                    ColumnNames[Index] = CaseCorrection;
            }
            //finally at this point we have a list of header indexes to work on
            if( ColumnIndexs.Count() != ColumnNames.Count() ) {
                LastErrorMessage = "Did not find all columns requested.";
                file.Close();
                return null;
            }
            string[] Tokens = null;
            while( string.IsNullOrEmpty(CurrentLine) == false ) {
                CurrentLine = file.ReadLine();
                if( string.IsNullOrEmpty(CurrentLine) == false ) {
                    Tokens = CurrentLine.Split(new char[] { '|' });

                    List<string> Row = new List<string>();
                    int ColNameIndex = 0;
                    foreach( int Index in ColumnIndexs ) {
                        Row.Add(TokenProcessor(Tokens[Index]));
                        if( UniqueValuesPerColumn[ColumnIndexNames[ColNameIndex]].Contains(TokenProcessor(Tokens[Index])) == false )
                            UniqueValuesPerColumn[ColumnIndexNames[ColNameIndex]].Add(TokenProcessor(Tokens[Index]));
                        ColNameIndex++;
                    }
                    ListListData.Add(Row);
                }
            }
            file.Close();

            return ListListData;
        }

        /// <summary>
        /// remove any invalid chars found
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        private string TokenProcessor(string Token) {
            //remove any leading and training " chars
            return Token.Trim(new char[] { '"' });
        }

        /// <summary>
        /// Read all the files in the extract directory and locate files by reading
        /// headers that match the data model items - return a list of files
        /// </summary>
        /// <param name="ExtractedDataDirectory">data extraction directory</param>
        /// <param name="DataModelItems">Data model fields names - matched to 1st line hearders of each file found in extract directory</param>
        /// <returns></returns>
        public List<string> FindCandidateFiles(string ExtractedDataDirectory, List<string> DataModelItems) {
            string[] AllFiles = System.IO.Directory.GetFiles(ExtractedDataDirectory, "*.txt");
            List<string> CandidateFiles = new List<string>();
            foreach( string FN in AllFiles ) {
                System.IO.StreamReader file = new System.IO.StreamReader(FN);
                string HeaderLine = file.ReadLine();
                file.Close();
                HeaderLine = HeaderLine.ToLower();
                if( string.IsNullOrEmpty(HeaderLine) == false ) {
                    foreach( string DMI in DataModelItems ) {
                        string[] HeaderTokens = HeaderLine.ToLower().Split(new char[] { '|' });
                        //tokeninze the string and check, this will eliminate embedded labels that give invalid matches
                        if( HeaderTokens.Contains(DMI.ToLower()) ) {
                            CandidateFiles.Add(FN);
                            break;
                        }
                    }
                }
            }
            return CandidateFiles;
        }
    }
}