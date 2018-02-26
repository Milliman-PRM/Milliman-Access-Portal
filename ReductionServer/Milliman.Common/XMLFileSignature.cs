using Sys = System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milliman.Common {
    public class XMLFileSignature {
        #region private variables
        private string _QualifiedFilename;
        private string _ErrorMessage;
        private Dictionary<string, string> _SignatureDictionary = new Dictionary<string, string>();
        #endregion

        #region ctor
        public XMLFileSignature() { }
        #endregion

        #region properties
        public Dictionary<string, string> SignatureDictionary {
            get { return _SignatureDictionary; }
            set { _SignatureDictionary = value; }
        }

        public string ErrorMessage { get { return _ErrorMessage; } set { _ErrorMessage = value; } }

        public bool InErrorState { get { return string.IsNullOrEmpty(_ErrorMessage); } }
        #endregion

        public XMLFileSignature(string QualifiedFilename) {
            if( Sys.IO.File.Exists(QualifiedFilename) == false ) {
                _ErrorMessage = "'" + QualifiedFilename + "' does not exist for signature retrieval.";
                return;
            }
            _QualifiedFilename = QualifiedFilename;
            Milliman.Common.FileSignature FS = new FileSignature();
            string Signature = FS.GetSignature(QualifiedFilename);
            if( string.IsNullOrEmpty(Signature) == true ) {
                _ErrorMessage = "'" + QualifiedFilename + "' has not been signed.";
                return;
            }

            if( string.IsNullOrEmpty(_ErrorMessage) )
                SignatureToDictionary(Signature);
        }

        private void SignatureToDictionary(string Signature) {
            Polenter.Serialization.SharpSerializer serializer = new Polenter.Serialization.SharpSerializer();
            Sys.IO.MemoryStream stream = new Sys.IO.MemoryStream(Utilities.StringToByteArray(Signature));

            _SignatureDictionary = serializer.Deserialize(stream) as Dictionary<string, string>;
        }

        private string DictionaryToString() {
            Polenter.Serialization.SharpSerializer serializer = new Polenter.Serialization.SharpSerializer();
            string XML = "";
            using( var stream = new Sys.IO.MemoryStream() ) {
                // serialize
                serializer.Serialize(_SignatureDictionary, stream);
                // reset stream
                stream.Position = 0;
                XML = Encoding.ASCII.GetString(stream.ToArray());
            }
            return XML.Substring(XML.IndexOf("<"));  //trim off all the prefix ??? junk
        }

        public bool SaveChanges() {
            try {
                if( Sys.IO.File.Exists(_QualifiedFilename) ) {
                    string XML = DictionaryToString();
                    if( string.IsNullOrEmpty(XML) == false ) {
                        FileSignature FS = new FileSignature();
                        FS.SignFile(_QualifiedFilename, XML);
                    }
                    return true;
                }
            } catch(Sys.Exception ) {
            }
            return false;
        }
    }
}