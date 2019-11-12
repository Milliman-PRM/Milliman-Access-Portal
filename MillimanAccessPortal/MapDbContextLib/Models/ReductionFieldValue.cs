using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public class ReductionFieldValue
    {
        public virtual bool HasSelectionStatus { get => false; }
        public Guid Id { get; set; }
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

    public class ReductionFieldValueComparer : IEqualityComparer<ReductionFieldValue>
    {
        public bool Equals(ReductionFieldValue x, ReductionFieldValue y)
        {
            return x.Value == y.Value;
        }
        public int GetHashCode(ReductionFieldValue obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}
