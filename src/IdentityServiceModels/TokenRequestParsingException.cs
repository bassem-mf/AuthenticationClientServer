using System;

namespace IdentityServiceModels
{
    public class TokenRequestParsingException
        : Exception
    {
        public TokenRequestParsingException(string message)
            : base(message)
        { }
    }
}