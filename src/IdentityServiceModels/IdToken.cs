using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;

namespace IdentityServiceModels
{
    public class IdToken
    {
        // ID token fields/claims specified here: http://openid.net/specs/openid-connect-core-1_0.html#IDToken

        public string Iss { get; set; }
        public string Sub { get; set; }
        public string[] Aud { get; set; }
        public DateTime Exp { get; set; }
        public DateTime Iat { get; set; }
        public string Nonce { get; set; }


        public IdToken(string iss, string sub, string[] aud, DateTime exp, DateTime iat, string nonce)
        {
            Iss = iss;
            Sub = sub;
            Aud = aud;
            Exp = exp;
            Iat = iat;
            Nonce = nonce;
        }

        public static ValidateAndLoadResult ValidateAndLoad(string jws, string expectedIssuer, string expectedClientId, string clientSecret)
        {
            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidIssuers = new string[] { expectedIssuer },
                ValidAudiences = new string[] { expectedClientId },
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clientSecret))
            };

            bool isValid;
            SecurityToken validatedToken = null;
            string errorMessage = null;
            try
            {
                new JwtSecurityTokenHandler().ValidateToken(jws, tokenValidationParameters, out validatedToken);
                isValid = true;
            }
            catch (Exception ex)
            {
                isValid = false;
                errorMessage = ex.Message;
            }

            ValidateAndLoadResult result;

            if (!isValid)
            {
                result = new ValidateAndLoadResult
                {
                    IsValid = false,
                    ErrorMessage = errorMessage
                };
            }
            else
            {
                JwtPayload payload = ((JwtSecurityToken)(validatedToken)).Payload;
                var idToken = new IdToken(
                    iss: (string)payload["iss"],
                    sub: (string)payload["sub"],
                    aud: ((JArray)payload["aud"]).Select(item => item.Value<string>()).ToArray(),
                    exp: DateTimeOffset.FromUnixTimeSeconds((long)payload["exp"]).UtcDateTime,
                    iat: DateTimeOffset.FromUnixTimeSeconds((long)payload["iat"]).UtcDateTime,
                    nonce: (string)payload["nonce"]);

                result = new ValidateAndLoadResult
                {
                    IsValid = true,
                    IdToken = idToken
                };
            }

            return result;
        }


        public string GetJws(string clientSecret)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clientSecret));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature);
            var header = new JwtHeader(signingCredentials);

            var payload = new JwtPayload
            {
                { "iss", Iss },
                { "sub", Sub },
                { "aud", Aud },
                { "exp", new DateTimeOffset(Exp, TimeSpan.Zero).ToUnixTimeSeconds() },
                { "iat", new DateTimeOffset(Iat, TimeSpan.Zero).ToUnixTimeSeconds() },
                { "nonce", Nonce }
            };

            var idToken = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(idToken);
        }


        public class ValidateAndLoadResult
        {
            public bool IsValid { get; set; }
            public IdToken IdToken { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}