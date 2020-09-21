using System;
using Microsoft.AspNetCore.Http;

namespace FormFlow.State
{
    public class SessionUserInstanceStateStore : IUserInstanceStateStore
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionUserInstanceStateStore(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void DeleteState(string key)
        {
            var session = GetSession();

            session.Remove(key);
        }

        public void SetState(string key, byte[] data)
        {
            var session = GetSession();

            session.Set(key, data);
        }

        public bool TryGetState(string key, out byte[] data)
        {
            var session = GetSession();

            return session.TryGetValue(key, out data);
        }

        private ISession GetSession()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("No active HttpContext.");
            }

            var session = httpContext.Session;
            if (session == null)
            {
                throw new InvalidOperationException("No Session available on HttpContext.");
            }

            return session;
        }
    }
}
