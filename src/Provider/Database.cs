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
                Secret = "987654321111222333444555666777888999",
                RedirectionUri = "http://relyingparty.localhost:57339/Account/HandleAuthenticationResponse"
            }
        };

        public static ConcurrentDictionary<Guid, IssuedAuthorization> IssuedAuthorizations = new ConcurrentDictionary<Guid, IssuedAuthorization>();


        public class RegisteredClient
        {
            public string Id { get; set; }
            public string Secret { get; set; }
            public string RedirectionUri { get; set; }
        }

        public class IssuedAuthorization
        {
            public string UserId { get; set; }
            public DateTime TimeIssued { get; set; }
            public string ClientId { get; set; }
            public string RedirectionUri { get; set; }
            public string Nonce { get; set; }
        }
    }
}