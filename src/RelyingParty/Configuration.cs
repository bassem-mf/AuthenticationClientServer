namespace RelyingParty
{
    public static class Configuration
    {
        public const string ISSUER_IDENTIFIER = "https://provider.localhost";
        public const string AUTHORIZATION_ENDPOINT_URL = "http://provider.localhost:50343/IdentityServices/Authenticate";
        public const string TOKEN_ENDPOINT_URL = "http://provider.localhost:50343/IdentityServices/Token";
        public const string CLIENT_ID = "123456789";
        public const string CLIENT_SECRET = "987654321111222333444555666777888999";
    }
}