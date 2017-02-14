using Newtonsoft.Json;
using System.Collections.Generic;

namespace IdentityServiceModels
{
    public class SuccessfulTokenResponse
    {
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public string IdToken { get; set; }


        public SuccessfulTokenResponse(string tokenType, string accessToken, string idToken)
        {
            TokenType = tokenType;
            AccessToken = accessToken;
            IdToken = idToken;
        }

        public static SuccessfulTokenResponse Deserialize(string responseJson)
        {
            Dictionary<string, string> responseParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseJson);
            return new SuccessfulTokenResponse(
                tokenType: responseParameters.ContainsKey("token_type") ? responseParameters["token_type"] : null,
                accessToken: responseParameters.ContainsKey("access_token") ? responseParameters["access_token"] : null,
                idToken: responseParameters.ContainsKey("id_token") ? responseParameters["id_token"] : null);
        }


        public string GetResponseJson()
        {
            return JsonConvert.SerializeObject(
                new
                {
                    token_type = TokenType,
                    access_token = AccessToken,
                    id_token = IdToken
                });
        }
    }
}