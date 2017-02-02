using System;
using System.Collections.Specialized;
using System.Web;

namespace IdentityServiceModels
{
    public class AuthenticationResponse
    {
        public string State { get; set; }
        public string Code { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }


        public static AuthenticationResponse CreateSuccessResponse(string state, string code)
        {
            return new AuthenticationResponse
            {
                State = state,
                Code = code
            };
        }

        public static AuthenticationResponse CreateErrorResponse(string state, string error, string error_description)
        {
            return new AuthenticationResponse
            {
                State = state,
                Error = error,
                ErrorDescription = error_description
            };
        }

        public static AuthenticationResponse LoadFromQueryString(NameValueCollection queryString)
        {
            AuthenticationResponse authenticationResponse;

            if (!string.IsNullOrEmpty(queryString["code"]))
            {
                authenticationResponse = CreateSuccessResponse(
                    state: queryString["state"],
                    code: queryString["code"]);
            }
            else if (!string.IsNullOrEmpty(queryString["error"]))
            {
                authenticationResponse = CreateErrorResponse(
                    state: queryString["state"],
                    error: queryString["error"],
                    error_description: queryString["error_description"]);
            }
            else
            {
                throw new Exception("Query string does not contain the required parameters.");
            }

            return authenticationResponse;
        }


        public string GetResponseUrl(string redirectUrl)
        {
            var uriBuilder = new UriBuilder(redirectUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (!string.IsNullOrEmpty(Code) && string.IsNullOrEmpty(Error))  // Success response
            {
                query["code"] = Code;
            }
            else if (string.IsNullOrEmpty(Code) && !string.IsNullOrEmpty(Error))  // Error response
            {
                query["error"] = Error;
                query["error_description"] = ErrorDescription;
            }
            else
            {
                throw new Exception("You should provide a value for either \"code\" (in case of success response) or \"error\" (in case of error response). Providing a value for none/both of them is not acceptable.");
            }

            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri.AbsoluteUri;
        }
    }
}