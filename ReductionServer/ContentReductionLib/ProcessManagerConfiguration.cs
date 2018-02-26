using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentReductionLib
{
    public class ProcessManagerConfiguration
    {
        public string RootPath { get; set; }
        public int MaxConcurrentTasks { get; set; }

        public override string ToString()
        {
            return string.Format("RootPath: {0}, MaxConcurrentTasks: {1}", RootPath, MaxConcurrentTasks);
        }
    }
}
