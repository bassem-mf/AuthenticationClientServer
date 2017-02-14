using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace IdentityServiceModels
{
    public class TokenRequest
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string GrantType { get; set; }
        public string Code { get; set; }
        public string RedirectUri { get; set; }


        public TokenRequest(string clientId, string clientSecret, string grantType, string code, string redirectUri)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            GrantType = grantType;
            Code = code;
            RedirectUri = redirectUri;
        }

        public static TokenRequest Load(string authorizationHeaderValue, NameValueCollection formParameters)
        {
            if (!authorizationHeaderValue.StartsWith("Basic "))
                throw new TokenRequestParsingException("Invalid authorization header value format.");

            string encodedCredentials = authorizationHeaderValue.Substring(6);
            string[] urlEncodedCredentials = Encoding.GetEncoding("ISO-8859-1").GetString(Convert.FromBase64String(encodedCredentials)).Split(':');
            string clientId = HttpUtility.UrlDecode(urlEncodedCredentials[0]);
            string clientSecret = HttpUtility.UrlDecode(urlEncodedCredentials[1]);

            return new TokenRequest(
                clientId: clientId,
                clientSecret: clientSecret,
                grantType: formParameters["grant_type"],
                code: formParameters["code"],
                redirectUri: formParameters["redirect_uri"]);
        }


        public async Task<HttpWebResponse> SendAsync(string tokenEndpointUrl)
        {
            WebRequest tokenWebRequest = WebRequest.Create(tokenEndpointUrl);
            tokenWebRequest.Method = "POST";
            tokenWebRequest.ContentType = "application/x-www-form-urlencoded";

            string urlEncodedClientId = HttpUtility.UrlEncode(ClientId);
            string urlEncodedClientSecret = HttpUtility.UrlEncode(ClientSecret);
            string encodedCredentials = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(urlEncodedClientId + ":" + urlEncodedClientSecret));
            string authorizationHeaderValue = "Basic " + encodedCredentials;
            tokenWebRequest.Headers.Add(HttpRequestHeader.Authorization, authorizationHeaderValue);

            NameValueCollection tokenRequestData = HttpUtility.ParseQueryString(string.Empty);
            tokenRequestData.Add("grant_type", GrantType);
            tokenRequestData.Add("code", Code);
            tokenRequestData.Add("redirect_uri", RedirectUri);
            byte[] tokenRequestBody = Encoding.UTF8.GetBytes(tokenRequestData.ToString());
            tokenWebRequest.ContentLength = tokenRequestBody.Length;

            WebResponse tokenWebResponse;
            try
            {
                using (Stream tokenRequestStream = await tokenWebRequest.GetRequestStreamAsync())  // This line may throw a WebException
                {
                    await tokenRequestStream.WriteAsync(tokenRequestBody, 0, tokenRequestBody.Length);
                }

                tokenWebResponse = await tokenWebRequest.GetResponseAsync();  // This line may throw a WebException
            }
            catch (WebException ex)
            {
                tokenWebResponse = ex.Response;
            }
            return (HttpWebResponse)tokenWebResponse;
        }
    }
}