using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CloudResourceLib
{
    public class AzureFileShareChanges
    {
        public List<string> overwrittenFiles = new List<string>();
        public List<string> newFiles = new List<string>();
        public List<string> removedFiles = new List<string>();
        public List<string> untouchedFiles = new List<string>();
    }
}
