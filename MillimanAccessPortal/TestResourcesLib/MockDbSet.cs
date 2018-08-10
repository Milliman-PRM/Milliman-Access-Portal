using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TestResourcesLib
{
    public class MockDbSet<T> where T : class
    {
        public static Mock<DbSet<T>> New(List<T> Data)
        {
            var data = Data.AsQueryable();

            Mock<DbSet<T>> Set = new Mock<DbSet<T>>();
            Set.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new DbSetAsyncQueryProvider<T>(data.Provider));
            Set.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            Set.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            Set.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());
            Set.As<IAsyncEnumerable<T>>().Setup(m => m.GetEnumerator()).Returns(() => new DbSetAsyncEnumerator<T>(data.GetEnumerator()));

            // Setup mocked object methods to interact with persisted data
            Set.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => Data.Add(s));
            Set.Setup(d => d.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>((s) => Data.AddRange(s));
            Set.Setup(d => d.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>())).Callback<IEnumerable<T>, CancellationToken>((s, ct) =>Data.AddRange(s));
            Set.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>((s) =>
            {
                int foundIndex = -1;

                // Iterate over collection to find Index of item that matches input
                foreach (T item in Data)
                {
                    if (GetIdValue(item) == GetIdValue(s))
                    {
                        foundIndex = Data.IndexOf(item);
                        break;
                    }
                }

                if (foundIndex > -1)
                {
                    Data.RemoveAt(foundIndex);
                }

            });
            Set.Setup(d => d.RemoveRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>((removeSet) =>
            {
                List<T> removeSetList = removeSet.ToList();
                foreach (T item in removeSetList)
                {
                    Set.Object.Remove(item);
                }
            }
            );
            Set.Setup(d => d.Update(It.IsAny<T>())).Callback<T>((s) => ReplaceGenericListElement(s, Data, "Id"));
            Set.Setup(d => d.Find(It.IsAny<object[]>())).Returns<object[]>((input) => Data.FirstOrDefault(d => GetPkValue(d) == input[0].ToString()));
            return Set;
        }

        public static string GetIdValue(object TargetedItem)
        {
            Type TType = TargetedItem.GetType();

            PropertyInfo IdPropertyInfo = TType.GetMembers().OfType<PropertyInfo>().Single(m => m.Name == "Id");

            return IdPropertyInfo.GetValue(TargetedItem).ToString();
        }

        public static string GetPkValue(object TargetedItem)
        {
            Type TType = TargetedItem.GetType();

            PropertyInfo PkPropertyInfo = TType.GetMembers().OfType<PropertyInfo>().Single(m => m.CustomAttributes.Any(at => at.AttributeType == typeof(KeyAttribute)));
            
            return PkPropertyInfo.GetValue(TargetedItem).ToString();
        }

        public static void AssignNavigationProperty<U>(DbSet<T> ReferencingDbSet, string ReferencingFkFieldName, DbSet<U> ReferencedDbSet) where U : class
        {
            Type TType = typeof(T);
            Type UType = typeof(U);

            PropertyInfo NavigationPropertyInfo = TType.GetMembers().OfType<PropertyInfo>().Single(p => p.PropertyType == UType);
            PropertyInfo ForeignKeyPropertyInfo = TType.GetProperty(ReferencingFkFieldName);

            Type ReferenceKeyPropertyType = ForeignKeyPropertyInfo.PropertyType;
            var a = UType.GetMembers().OfType<PropertyInfo>();
            var b = a.Where(m => m.PropertyType == ReferenceKeyPropertyType);
            var c = b.Where(m => m.PropertyType == ReferenceKeyPropertyType).Where(m => m.CustomAttributes.Any(at => at.AttributeType == typeof(KeyAttribute)));
            // Use Type.IsAssignableFrom() here to handle a nullable (non-required) foreign key:
            PropertyInfo ReferencedPkPropertyInfo = UType.GetMembers().OfType<PropertyInfo>().Single(m => ReferenceKeyPropertyType.IsAssignableFrom(m.PropertyType) && m.CustomAttributes.Any(at => at.AttributeType == typeof(KeyAttribute)));

            foreach (T ReferencingRecord in ReferencingDbSet)
            {
                // To handle null values of nullable foreign keys
                if (ForeignKeyPropertyInfo.GetValue(ReferencingRecord) == null)
                {
                    continue;
                }

                var ReferencingKeyValue = ForeignKeyPropertyInfo.GetValue(ReferencingRecord).ToString();

                foreach (U UItem in ReferencedDbSet)
                {
                    var PkValue = ReferencedPkPropertyInfo.GetValue(UItem).ToString();
                    if (PkValue == ReferencingKeyValue)
                    {
                        NavigationPropertyInfo.SetMethod.Invoke(ReferencingRecord, new object[] { UItem });
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// To support DbSet.Update, replaces one list element with another when an element with matching key value is found in the list
        /// </summary>
        /// <param name="SearchFor"></param>
        /// <param name="InList"></param>
        /// <param name="KeyPropertyName"></param>
        /// <returns></returns>
        private static bool ReplaceGenericListElement(T SearchFor, List<T> InList, string KeyPropertyName)
        {
            try
            {
                PropertyInfo KeyProperty = SearchFor.GetType().GetProperty(KeyPropertyName);  // uses reflection

                dynamic KeyValue = KeyProperty.GetValue(SearchFor);
                // following returns the List element with matching key, or throws if no match is found
                T ElementFound = InList.Single(e => 
                    {
                        dynamic eKey = KeyProperty.GetValue(e);
                        return KeyValue == eKey;  // returns only from this code block
                    });
                int IndexOfElement = InList.IndexOf(ElementFound);
                InList[IndexOfElement] = SearchFor;
                return true;
            }
            catch (Exception e)
            {
                string x = e.Message;
                return false;
            }
        }

        // If we could make this work it would be a much better alternative to hard coding the assignment of navigation properties during context data initialization using MockDbSet<T>.AssignNavigationProperty(...)
        //internal static void Include<U>(DbSet<T> ReferencingSet, string TReferencingKeyName, string NavigationPropertyName, string UPkName, ref DbSet<object> ReferencedSet)
        //{
        //    // uses reflection
        //    var TFkPropertyInfo = typeof(T).GetProperty(TReferencingKeyName);  // e.g. RootContentId
        //    var TNavigationPropertyInfo = typeof(T).GetProperty(NavigationPropertyName);  // RootContent
        //    var UPkPropertyInfo = typeof(U).GetProperty(UPkName);  // e.g. Id

        //    if (TFkPropertyInfo.PropertyType != typeof(long) || 
        //        UPkPropertyInfo.PropertyType != typeof(long))
        //    {
        //        // when the pk type changes, modify this function
        //        throw new NotImplementedException();
        //    }

        //    foreach (T Element in ReferencingSet) // Do this for each record
        //    {
        //        dynamic TFkValue = TFkPropertyInfo.GetValue(Element);
        //        long LongKey = TFkValue;

        //        U ReferencedInstance = (U)ReferencedSet.FirstOrDefault(s => (long)UPkPropertyInfo.GetValue(s) == LongKey);

        //        TNavigationPropertyInfo.SetMethod.Invoke(Element, new object[] { ReferencedInstance });
        //    }

        //}
    }

    // Reference: https://msdn.microsoft.com/en-us/library/dn314429.aspx
    internal class DbSetAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal DbSetAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new DbSetAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new DbSetAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            return new DbSetAsyncEnumerable<TResult>(expression);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute<TResult>(expression));
        }
    }

    internal class DbSetAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public DbSetAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public DbSetAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetEnumerator()
        {
            return new DbSetAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }
    }

    internal class DbSetAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public DbSetAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public void Dispose()
        {
            _inner.Dispose();
        }

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            return Task.FromResult(_inner.MoveNext());
        }
    }
}
