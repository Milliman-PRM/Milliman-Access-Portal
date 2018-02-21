using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milliman.Common.TreeUtilities {
    /// <summary>
    /// Structure that holds the hierarchy tree
    /// </summary>
    public class TreeNode {
        public string DisplayFieldName { get; set; }
        public List<string> DataModelFieldName { get; set; }
        public Dictionary<string, TreeNode> SubNodes { get; set; }

        public TreeNode Parent { get; set; }
        public TreeNode(TreeNode _Parent) {
            DataModelFieldName = new List<string>();
            SubNodes = new Dictionary<string, TreeNode>();
            Parent = _Parent;
        }

        private string GetDataModelFields(string DataValue, Dictionary<string, List<string>> UniqueValuesPerColumn, bool EncodeData = true) {
            string DMFields = string.Empty;
            foreach( string S in DataModelFieldName ) {
                //is a valid value so add it
                if( UniqueValuesPerColumn[S].Contains(DataValue) == true ) {
                    if( DMFields.Length > 0 )
                        DMFields += "|";
                    if( EncodeData )
                        DMFields += Milliman.Common.Utilities.ConvertStringToHex(S);
                    else
                        DMFields += S;
                }
            }
            return DMFields;
        }
        public string ToXML(Dictionary<string, List<string>> UniqueValuesPerColumn, bool EncodeData = true) {
            StringBuilder SB = new StringBuilder();
            foreach( KeyValuePair<string, TreeNode> Node in SubNodes ) {
                //if (Node.Key.ToLower().Contains("robert walt"))
                //    System.Diagnostics.Debugger.Break();
                if( EncodeData )
                    SB.Append("<Node Text=\"" + Milliman.Common.Utilities.ConvertStringToHex(Node.Key) + "\" Value=\"" + GetDataModelFields(Node.Key, UniqueValuesPerColumn, EncodeData) + "\">" + Node.Value.ToXML(UniqueValuesPerColumn, EncodeData) + "</Node>");
                else
                    SB.Append("<Node Text=\"" + Node.Key + "\" Value=\"" + GetDataModelFields(Node.Key, UniqueValuesPerColumn, EncodeData) + "\">" + Node.Value.ToXML(UniqueValuesPerColumn, EncodeData) + "</Node>");
            }
            return SB.ToString();

        }
    }
}
