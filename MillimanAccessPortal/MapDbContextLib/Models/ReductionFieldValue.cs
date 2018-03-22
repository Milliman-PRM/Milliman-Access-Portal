using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public class ReductionFieldValue
    {
        public virtual bool HasSelectionStatus { get { return false; } }
        public long Id { get; set; }
        public string Value { get; set; }

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

    }
}
