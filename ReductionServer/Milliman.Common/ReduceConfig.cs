using Sys = System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milliman.Common {

    public class ReduceConfig {

        private string _config_full_qualified_file_name;

        public string ConfigFQFileName { get { return _config_full_qualified_file_name; } private set { _config_full_qualified_file_name = value; } }
        public string ConfigFileName { get { return Path.GetFileName(_config_full_qualified_file_name); } }
        public string ConfigFileNameWithoutExtension { get {return Path.GetFileNameWithoutExtension(_config_full_qualified_file_name) ; } }
        public string ConfigFQFolderName { get {return Path.GetDirectoryName(_config_full_qualified_file_name) ; } }

        private string _MasterQVW;

        /// <summary>
        /// INPUT: name of the master QVW that contains all data - should not contain a path but QVW must be located in same
        /// directory as reduce.config files
        /// </summary>
        public string MasterQVW {
            get { return _MasterQVW; }
            set { _MasterQVW = value; }
        }

        private string _MasterStatusLog;

        /// <summary>
        /// INPUT: name of master status log - should not contain a path but QVW must be located in same
        /// directory as reduce.config files.  Used when multiple reductions are requested.
        /// </summary>
        public string MasterStatusLog {
            get { return _MasterStatusLog; }
            set { _MasterStatusLog = value; }
        }

        private List<ReductionSettings> _SelectionSets;

        /// <summary>
        /// List of the reduction settings
        /// </summary>
        public List<ReductionSettings> SelectionSets {
            get { return _SelectionSets; }
            set { _SelectionSets = value; }
        }

        public bool Serialize(string FileName) {
            var serializer = new Sys.Web.Script.Serialization.JavaScriptSerializer();
            var Json = serializer.Serialize(this);
            File.WriteAllText(FileName, Json);
            return true;
        }

        static public ReduceConfig Deserialize(string FileName) {
            string JsonData = File.ReadAllText(FileName);
            var serializer = new Sys.Web.Script.Serialization.JavaScriptSerializer();
            var obj = serializer.Deserialize<ReduceConfig>(JsonData) as ReduceConfig ?? new ReduceConfig();
            obj._config_full_qualified_file_name = FileName;
            return obj;
        }
    }
}