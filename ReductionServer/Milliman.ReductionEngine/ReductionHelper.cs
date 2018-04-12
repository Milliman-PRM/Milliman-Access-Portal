using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Milliman.Common;

namespace Milliman.ReductionEngine {
    public class ReductionHelper {
        
        internal static bool dropTableProcessor(string sourceFolder, bool shouldDropDisassociated,string masterQVWName, List<NVPair> selectionCriteria, List<string> listTablesToDrop) {
            try {
                //preliminary check to shortcut processing
                if( (shouldDropDisassociated == false) && (listTablesToDrop == null) )
                    return false;

                int map_index = 0;
                string map_file_name = masterQVWName.Replace(".qvw", ".map_" + map_index.ToString());
                while( System.IO.File.Exists(map_file_name) ) {
                    Milliman.Common.TreeUtilities.DropTableProcessor drop_table_processor = Milliman.Common.TreeUtilities.DropTableProcessor.Load(map_file_name);
                    if( drop_table_processor != null ) {
                        List<string> AllSelections = new List<string>();
                        foreach( var pair in selectionCriteria ) {
                            drop_table_processor.SetFieldAccessed(pair.FieldName);
                            AllSelections.Add(pair.FieldName);
                        }

                        List<string> list_dropped_selections = new List<string>();
                        List<string> list_tables_to_drop = new List<string>();
                        //requested to determine if associated data model files are selected
                        if( shouldDropDisassociated )
                            list_tables_to_drop = drop_table_processor.TablesToDrop(map_file_name, AllSelections, out list_dropped_selections);
                        //add tables we explicitly said to drop
                        if( listTablesToDrop != null )
                            list_tables_to_drop.AddRange(listTablesToDrop);
                        if( list_tables_to_drop.Count > 0 ) {
                            //remove the selected variables that should be dropped
                            foreach( string drop_selection in list_dropped_selections ) {
                                //must remove all instances, thus the loop to remove all
                                while( selectionCriteria.FindIndex(x => string.Compare(drop_selection, x.FieldName, true) == 0) >= 0 )
                                    selectionCriteria.RemoveAt(selectionCriteria.FindIndex(x => string.Compare(drop_selection, x.FieldName, true) == 0));
                            }
                            string script_file = dropTableScript(list_tables_to_drop);
                            System.IO.File.Copy(script_file, System.IO.Path.Combine(sourceFolder, System.IO.Path.GetFileName(script_file)));

                            return true;
                        }
                    }
                    map_index++;
                    map_file_name = masterQVWName.Replace(".qvw", ".map_" + map_index.ToString());
                }
                if ((listTablesToDrop == null) || (listTablesToDrop.Count == 0))
                    return false;
                return true;
            } catch{
                throw;
            }
        }

        /// <summary>
        /// Script helper functions,  writes the script into a temp file
        /// and returns fully qualfiied path - file must be named "SCRIPT.TXT" to match QVW scripting
        /// </summary>
        private static string dropTableScript(List<string> listTables) {
            if( (listTables == null) || (listTables.Count == 0) ) {
                return string.Empty;
            }
            List<string> list_drop_tables = new List<string>();
            foreach( string Table in listTables ) {
                list_drop_tables.Add("DROP TABLE " + Table + ";");
            }
            list_drop_tables.Add("EXIT SCRIPT;");
            string script_file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "script.txt");
            System.IO.File.WriteAllLines(script_file, list_drop_tables);
            return script_file;
        }


    }
}
