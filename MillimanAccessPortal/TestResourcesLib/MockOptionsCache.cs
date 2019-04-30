using MapDbContextLib.Context;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestResourcesLib
{
    public class MockOptionsCache<T> where T : class, new()
    {
        public static OptionsCache<T> New(List<KeyValuePair<string,T>> initData = null)
        {
            Dictionary<string,T> Store = new Dictionary<string, T>();
            if (initData != null)
            {
                initData.ForEach(kvp => Store.Add(kvp.Key, kvp.Value));
            }

            Mock<OptionsCache<T>> newOptionsProvider = new Mock<OptionsCache<T>>();
            newOptionsProvider.Setup(m => m.TryAdd(It.IsAny<string>(), It.IsAny<T>())).Returns<string, T>((n,o) =>
            {
                Store.Add(n,o);
                return true;
            });

            newOptionsProvider.Setup(m => m.TryRemove(It.IsAny<string>())).Returns<string>(n => Store.Remove(n));
            //newWsOptionsProvider.Setup(m => m.Clear()).Callback(() => { });
            newOptionsProvider.Setup(m => m.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<T>>())).Returns<string, Func<T>>((n, f) =>
            {
                if (Store.TryGetValue(n, out T o))
                {
                    return o;
                }
                else
                {
                    T newObject = f();
                    Store.Add(n, newObject);
                    return newObject;
                }
            });

            return newOptionsProvider.Object;
        }
    }
}
