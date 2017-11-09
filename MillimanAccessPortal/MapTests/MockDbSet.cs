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
        public static DbSet<T> New(List<T> Data)
        {
            var data = Data.AsQueryable();

            Mock<DbSet<T>> Set = new Mock<DbSet<T>>();
            Set.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            Set.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            Set.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            Set.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

            // Methods need to be implemented in the mocked object to persist data changes
            Set.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => Data.Add(s));
            Set.Setup(d => d.AddRange(It.IsAny<T[]>())).Callback<T[]>((s) => Data.AddRange(s));
            Set.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>((s) => Data.Remove(s));
            Set.Setup(d => d.Update(It.IsAny<T>())).Callback<T>((s) => ReplaceGenericListElement(s, Data, "Id"));

            return Set.Object;
        }

        private static bool ReplaceGenericListElement(T SearchFor, List<T> InSet, string KeyPropertyName)
        {
            try
            {
                var KeyProperty = SearchFor.GetType().GetProperty(KeyPropertyName);

                dynamic x = KeyProperty.GetValue(SearchFor);
                T ElementFound = InSet.Single(e => {
                    dynamic eKey = KeyProperty.GetValue(e);
                    return x == eKey;
                    });
                int IndexOfElement = InSet.IndexOf(ElementFound);
                InSet[IndexOfElement] = SearchFor;
                return true;
            }
            catch (Exception e)
            {
                string x = e.Message;
                return false;
            }
        }
    }
}
