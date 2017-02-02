using Newtonsoft.Json;
using System.Collections.Generic;

namespace IdentityServiceModels
{
    public class TokenErrorResponse
    {
        public string Error { get; set; }
        public string ErrorDescription { get; set; }


        public TokenErrorResponse(string error, string errorDescription)
        {
            Error = error;
            ErrorDescription = errorDescription;
        }

        public static TokenErrorResponse Deserialize(string responseJson)
        {
            Dictionary<string, string> responseParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseJson);
            return new TokenErrorResponse(
                error: responseParameters.ContainsKey("error") ? responseParameters["error"] : null,
                errorDescription: responseParameters.ContainsKey("error_description") ? responseParameters["error_description"] : null);
        }
    }
}