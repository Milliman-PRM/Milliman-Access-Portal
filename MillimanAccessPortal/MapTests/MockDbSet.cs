using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MapTests
{
    public class MockDbSet<T> where T : class
    {
        public static Mock<DbSet<T>> New(ref List<T> Data)
        {
            var data = Data.AsQueryable();

            Mock<DbSet<T>> Set = new Mock<DbSet<T>>();
            Set.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            Set.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            Set.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            Set.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

            return Set;
        }
    }
}
