using IdentityServiceModels;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;

namespace Provider.Controllers
{
    public class IdentityServicesController
        : Controller
    {
        private static readonly TimeSpan AUTHORIZATION_CODE_VALIDITY_PERIOD = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan ID_TOKEN_VALIDITY_PERIOD = TimeSpan.FromMinutes(3);


        // GET or POST: /IdentityServices/Authenticate
        public ActionResult Authenticate()
        {
            NameValueCollection requestParameters = Request.HttpMethod == "GET" ? Request.QueryString : Request.Form;
            AuthenticationRequest authenticationRequest = AuthenticationRequest.Load(requestParameters);

            ActionResult actionResult;

            // For some errors, the authorization server should inform the resource owner (human user) by displaying an error message. For other errors, the authorization server should redirect
            // the user-agent to the client application and put the error information in the query string. For more details see https://tools.ietf.org/html/rfc6749#section-4.1.2.1

            if (string.IsNullOrEmpty(authenticationRequest.ClientId))
            {
                actionResult = View(viewName: "Error", model: "client_id is missing from the request.");
            }
            else
            {
                Database.RegisteredClient registeredClient = Database.RegisteredClients.SingleOrDefault(rc => rc.Id == authenticationRequest.ClientId);
                if (registeredClient == null)
                {
                    actionResult = View(viewName: "Error", model: "Invalid client_id: " + authenticationRequest.ClientId);
                }
                else if (string.IsNullOrEmpty(authenticationRequest.RedirectUri))
                {
                    actionResult = View(viewName: "Error", model: "redirect_uri is missing from the request.");
                }
                else if (authenticationRequest.RedirectUri != registeredClient.RedirectionUri)
                {
                    actionResult = View(viewName: "Error", model: "redirect_uri does not match the redirection URI registered for the client.");
                }
                else
                {
                    string errorCode = null;
                    string errorDescription = null;

                    if (string.IsNullOrEmpty(authenticationRequest.Scope))
                    {
                        errorCode = "invalid_request";
                        errorDescription = "The request is missing the \"scope\" parameter.";
                    }
                    else if (!authenticationRequest.Scope.Split(' ').Contains("openid"))
                    {
                        errorCode = "invalid_scope";
                        errorDescription = "Request parameter \"scope\" should contain the \"openid\" scope value.";
                    }
                    else if (string.IsNullOrEmpty(authenticationRequest.ResponseType))
                    {
                        errorCode = "invalid_request";
                        errorDescription = "The request is missing the \"response_type\" parameter.";
                    }
                    else if (authenticationRequest.ResponseType != "code")
                    {
                        errorCode = "unsupported_response_type";
                        errorDescription = "The only supported response_type value is \"code\".";
                    }

                    if (errorCode != null)
                    {
                        AuthenticationResponse response = AuthenticationResponse.CreateErrorResponse(
                            state: authenticationRequest.State,
                            error: errorCode,
                            error_description: errorDescription);
                        string responseUrl = response.GetResponseUrl(authenticationRequest.RedirectUri);
                        actionResult = Redirect(responseUrl);
                    }
                    else  // Request is valid
                    {
                        if (!User.Identity.IsAuthenticated)  // User needs to login before we can return a response to the relying party.
                        {
                            actionResult = RedirectToAction("Login", "Account", new { returnUrl = Request.Url.PathAndQuery });
                        }
                        else  // User is logged in. Redirect the user to the relying party with a successful authentication response containing an authorization code.
                        {
                            Guid authorizationCode = Guid.NewGuid();
                            Database.IssuedAuthorizations.TryAdd(authorizationCode,
                                new Database.IssuedAuthorization
                                {
                                    UserId = User.Identity.Name,
                                    TimeIssued = DateTime.UtcNow,
                                    ClientId = authenticationRequest.ClientId,
                                    RedirectionUri = authenticationRequest.RedirectUri,
                                    Nonce = authenticationRequest.Nonce
                                });

                            AuthenticationResponse response = AuthenticationResponse.CreateSuccessResponse(
                                state: authenticationRequest.State,
                                code: authorizationCode.ToString());
                            string responseUrl = response.GetResponseUrl(authenticationRequest.RedirectUri);
                            actionResult = Redirect(responseUrl);
                        }
                    }
                }
            }

            return actionResult;
        }

        // POST: /IdentityServices/Token
        [OutputCache(Location = System.Web.UI.OutputCacheLocation.None, NoStore = true)]
        public ActionResult Token()
        {
            ActionResult actionResult;

            if (!Request.Headers.AllKeys.Contains("Authorization"))
            {
                actionResult = TokenErrorJson("invalid_client", "The request is missing the Authorization header.");
            }
            else
            {
                try
                {
                    TokenRequest tokenRequest = TokenRequest.Load(Request.Headers["Authorization"], Request.Form);

                    if (string.IsNullOrEmpty(tokenRequest.GrantType))
                    {
                        actionResult = TokenErrorJson("invalid_request", "The request is missing the 'grant_type' parameter.");
                    }
                    else if (tokenRequest.GrantType != "authorization_code")
                    {
                        actionResult = TokenErrorJson("unsupported_grant_type", $"The grant_type '{tokenRequest.GrantType}' is not supported. The only supported grant_type is 'authorization_code'.");
                    }
                    else if (string.IsNullOrEmpty(tokenRequest.Code))
                    {
                        actionResult = TokenErrorJson("invalid_request", "The request is missing the 'code' parameter.");
                    }
                    else if (string.IsNullOrEmpty(tokenRequest.RedirectUri))
                    {
                        actionResult = TokenErrorJson("invalid_request", "The request is missing the 'redirect_uri' parameter.");
                    }
                    else
                    {
                        Database.RegisteredClient registeredClient = Database.RegisteredClients.SingleOrDefault(rc => rc.Id == tokenRequest.ClientId);
                        if (registeredClient == null || registeredClient.Secret != tokenRequest.ClientSecret)
                        {
                            actionResult = TokenErrorJson("invalid_client", "Invalid client ID or secret.");
                        }
                        else
                        {
                            Guid authorizationCode = Guid.Parse(tokenRequest.Code);
                            Database.IssuedAuthorization issuedAuthorization;
                            bool authorizationCodeExists = Database.IssuedAuthorizations.TryRemove(authorizationCode, out issuedAuthorization);
                            if (!authorizationCodeExists)
                            {
                                actionResult = TokenErrorJson("invalid_grant", "Authorization code is invalid or was previously used.");
                            }
                            else if (issuedAuthorization.ClientId != tokenRequest.ClientId)
                            {
                                actionResult = TokenErrorJson("invalid_grant", "Authorization code was issued to another client.");
                            }
                            else if (issuedAuthorization.RedirectionUri != tokenRequest.RedirectUri)
                            {
                                actionResult = TokenErrorJson("invalid_grant", "redirect_uri of the token request does not match redirect_uri of the authorization request.");
                            }
                            else if ((DateTime.UtcNow - issuedAuthorization.TimeIssued) > AUTHORIZATION_CODE_VALIDITY_PERIOD)
                            {
                                actionResult = TokenErrorJson("invalid_grant", "Authorization code is expired.");
                            }
                            else
                            {
                                DateTime currentUtcTime = DateTime.UtcNow;

                                var idToken = new IdToken(
                                    iss: Configuration.ISSUER_IDENTIFIER,
                                    sub: issuedAuthorization.UserId,
                                    aud: new string[] { registeredClient.Id },
                                    exp: currentUtcTime.Add(ID_TOKEN_VALIDITY_PERIOD),
                                    iat: currentUtcTime,
                                    nonce: issuedAuthorization.Nonce);

                                string idTokenJws = idToken.GetJws(registeredClient.Secret);

                                SuccessfulTokenResponse successfulTokenResponse = new SuccessfulTokenResponse(
                                    tokenType: "Bearer",
                                    accessToken: "Dummy",  // Access token is not used here but it is required by the OAuth 2.0 specifications.
                                    idToken: idTokenJws);

                                actionResult = Content(successfulTokenResponse.GetResponseJson(), "application/json");
                            }
                        }
                    }
                }
                catch (TokenRequestParsingException ex)
                {
                    actionResult = TokenErrorJson("invalid_request", ex.Message);
                }
            }

            return actionResult;
        }

        private ActionResult TokenErrorJson(string error, string errorDescription)
        {
            if (error == "invalid_client")
            {
                Response.StatusCode = 401;
                Response.Headers["WWW-Authenticate"] = "Basic realm=\"IdentityServices\"";
            }
            else
            {
                Response.StatusCode = 400;
            }

            var tokenErrorResponse = new TokenErrorResponse(error, errorDescription);
            return Content(tokenErrorResponse.GetResponseJson(), "application/json");
        }
    }
}