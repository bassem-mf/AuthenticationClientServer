using Provider.Models;
using Provider.Utilities;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Provider.Controllers
{
    public class IdentityServicesController
        : Controller
    {
        // GET or POST: /IdentityServices/Auhenticate
        public ActionResult Authenticate(AuthenticationRequest request)
        {
            ActionResult actionResult;

            // For some errors, the authorization server should inform the resource owner (human user) by displaying an error message. For other errors, the authorization server should redirect
            // the user-agent to the client application and put the error information in the query string. For more details see https://tools.ietf.org/html/rfc6749#section-4.1.2.1

            if (string.IsNullOrEmpty(request.client_id))
            {
                actionResult = View(viewName: "Error", model: "client_id is missing from the request.");
            }
            else
            {
                Database.RegisteredClient registeredClient = Database.RegisteredClients.SingleOrDefault(rc => rc.Id == request.client_id);
                if (registeredClient == null)
                {
                    actionResult = View(viewName: "Error", model: "Invalid client_id: " + request.client_id);
                }
                else if (string.IsNullOrEmpty(request.redirect_uri))
                {
                    actionResult = View(viewName: "Error", model: "redirect_uri is missing from the request.");
                }
                else if (request.redirect_uri != registeredClient.RedirectionUri)
                {
                    actionResult = View(viewName: "Error", model: "redirect_uri does not match the redirection URI registered for the client.");
                }
                else
                {
                    string errorCode = null;
                    string errorDescription = null;

                    if (string.IsNullOrEmpty(request.scope))
                    {
                        errorCode = "invalid_request";
                        errorDescription = "The request is missing the \"scope\" parameter.";
                    }
                    else if (!request.scope.Split(' ').Contains("openid"))
                    {
                        errorCode = "invalid_scope";
                        errorDescription = "Request parameter \"scope\" should contain the \"openid\" scope value.";
                    }
                    else if (string.IsNullOrEmpty(request.response_type))
                    {
                        errorCode = "invalid_request";
                        errorDescription = "The request is missing the \"response_type\" parameter.";
                    }
                    else if (request.response_type != "code")
                    {
                        errorCode = "unsupported_response_type";
                        errorDescription = "The only supported response_type value is \"code\".";
                    }

                    if (errorCode != null)
                    {
                        string redirectUrl = new Uri(request.redirect_uri)
                            .SetQueryStringParameter("error", errorCode)
                            .SetQueryStringParameter("error_description", errorDescription)
                            .SetQueryStringParameter("state", request.state)
                            .ToString();

                        actionResult = Redirect(redirectUrl);
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
                                ClientId = request.client_id,
                                RedirectionUri = request.redirect_uri
                            });

                            string redirectUrl = new Uri(request.redirect_uri)
                                .SetQueryStringParameter("code", authorizationCode.ToString())
                                .SetQueryStringParameter("state", request.state)
                                .ToString();

                            actionResult = Redirect(redirectUrl);
                        }
                    }
                }
            }

            return actionResult;
        }
    }
}