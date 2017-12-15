using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapTests
{
    class MockSession : ISession 
    {
        public MockSession getSession()
        {
            return new MockSession();
        }

        public string Id { get; } = "Testing";

        public bool IsAvailable => throw new NotImplementedException();

        public IEnumerable<string> Keys => throw new NotImplementedException();

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public void Set(string key, byte[] value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            throw new NotImplementedException();
        }
    }
}
