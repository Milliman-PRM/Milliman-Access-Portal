using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public class ReductionFieldValue
    {
        public virtual bool HasSelectionStatus { get { return false; } }

        public ReductionFieldValue()
        {}

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="Arg"></param>
        public ReductionFieldValue(string Arg)
        {
            Value = Arg;
        }

        public string Value { get; set; }

    }
}
