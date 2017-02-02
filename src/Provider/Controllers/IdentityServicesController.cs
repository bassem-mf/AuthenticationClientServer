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
                            Database.IssuedAuthorizations.Add(new Database.IssuedAuthorization
                            {
                                AuthorizationCode = authorizationCode,
                                UserId = User.Identity.Name,
                                TimeIssued = DateTime.UtcNow,
                                ClientId = authenticationRequest.ClientId,
                                RedirectionUri = authenticationRequest.RedirectUri
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
    }
}