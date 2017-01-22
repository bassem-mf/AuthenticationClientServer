using System;
using System.Collections.Concurrent;

namespace Provider
{
    public static class Database
    {
        public static RegisteredClient[] RegisteredClients = new RegisteredClient[]
        {
            new RegisteredClient
            {
                Id = "123456789",
                Secret = "987654321",
                RedirectionUri = "http://localhost:57339/Account/HandleAuthenticationResponse"
            }
        };

        public static ConcurrentBag<IssuedAuthorization> IssuedAuthorizations = new ConcurrentBag<IssuedAuthorization>();


        public class RegisteredClient
        {
            public string Id { get; set; }
            public string Secret { get; set; }
            public string RedirectionUri { get; set; }
        }

        public class IssuedAuthorization
        {
            public Guid AuthorizationCode { get; set; }
            public string UserId { get; set; }
            public DateTime TimeIssued { get; set; }
            public string ClientId { get; set; }
            public string RedirectionUri { get; set; }
        }
    }
}