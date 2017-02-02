using System;
using System.Collections.Specialized;
using System.Web;

namespace IdentityServiceModels
{
    public class AuthenticationRequest
    {
        public string Scope { get; set; }
        public string ResponseType { get; set; }
        public string ClientId { get; set; }
        public string RedirectUri { get; set; }
        public string State { get; set; }
        public string Nonce { get; set; }


        public AuthenticationRequest(string scope, string responseType, string clientId, string redirectUri, string state, string nonce)
        {
            Scope = scope;
            ResponseType = responseType;
            ClientId = clientId;
            RedirectUri = redirectUri;
            State = state;
            Nonce = nonce;
        }

        public static AuthenticationRequest Load(NameValueCollection requestParameters)
        {
            return new AuthenticationRequest(
                scope: requestParameters["scope"],
                responseType: requestParameters["response_type"],
                clientId: requestParameters["client_id"],
                redirectUri: requestParameters["redirect_uri"],
                state: requestParameters["state"],
                nonce: requestParameters["nonce"]);
        }


        public string GetRequestUrl(string authorizationEndpointUrl)
        {
            var uriBuilder = new UriBuilder(authorizationEndpointUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["scope"] = Scope;
            query["response_type"] = ResponseType;
            query["client_id"] = ClientId;
            query["redirect_uri"] = RedirectUri;
            query["state"] = State;
            query["nonce"] = Nonce;
            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri.AbsoluteUri;
        }
    }
}