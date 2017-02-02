using IdentityServiceModels;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RelyingParty.Controllers
{
    public class AccountController
        : Controller
    {
        // POST: /Account/RequestAuthentication
        [HttpPost]
        public RedirectResult RequestAuthentication()
        {
            string handleAuthenticationResponseUrl = Url.Action("HandleAuthenticationResponse", "Account", null, Request.Url.Scheme);
            string antiForgeryStateToken = Guid.NewGuid().ToString();
            Session["OAuthAntiForgeryStateToken"] = antiForgeryStateToken;
            string antiReplayNonceToken = Guid.NewGuid().ToString();
            Session["OAuthAntiReplayNonceToken"] = antiReplayNonceToken;

            var request = new AuthenticationRequest(
                scope: "openid",
                responseType: "code",
                clientId: Configuration.CLIENT_ID,
                redirectUri: handleAuthenticationResponseUrl,
                state: antiForgeryStateToken,
                nonce: antiReplayNonceToken);

            string requestUrl = request.GetRequestUrl(Configuration.AUTHORIZATION_ENDPOINT_URL);
            return Redirect(requestUrl);
        }

        // GET: /Account/HandleAuthenticationResponse?code=SplxlOBeZQQYbYS6WxSbIA&state=af0ifjsldkj
        public async Task<ActionResult> HandleAuthenticationResponseAsync()
        {
            AuthenticationResponse authenticationResponse = AuthenticationResponse.LoadFromQueryString(Request.QueryString);

            ActionResult actionResult;

            if (authenticationResponse.State != (string)Session["OAuthAntiForgeryStateToken"])
            {
                actionResult = View(viewName: "Error", model: "Missing or invalid 'state' parameter.");
            }
            else if (string.IsNullOrEmpty(authenticationResponse.Code))
            {
                actionResult = View(viewName: "Error", model: $"Authentication failed. Error code: {authenticationResponse.Error}. Error description: {authenticationResponse.ErrorDescription}");
            }
            else
            {
                var tokenRequest = new TokenRequest(
                    clientId: Configuration.CLIENT_ID,
                    clientSecret: Configuration.CLIENT_SECRET,
                    grantType: "authorization_code",
                    code: authenticationResponse.Code,
                    redirectUri: Url.Action("HandleAuthenticationResponse", "Account", null, Request.Url.Scheme));

                using (var tokenWebResponse = await tokenRequest.SendAsync(Configuration.TOKEN_ENDPOINT_URL))
                {
                    if (tokenWebResponse == null)
                    {
                        actionResult = View(viewName: "Error", model: "Token request failed. Could not connect to the identity provider.");
                    }
                    else if (!tokenWebResponse.ContentType.StartsWith("application/json"))
                    {
                        actionResult = View(viewName: "Error", model: "Token request failed. Unexpected identity provider response.");
                    }
                    else
                    {
                        using (Stream tokenResponseStream = tokenWebResponse.GetResponseStream())
                        {
                            using (var tokenResponseReader = new StreamReader(tokenResponseStream))
                            {
                                string tokenResponseJson = await tokenResponseReader.ReadToEndAsync();
                                if (tokenWebResponse.StatusCode != HttpStatusCode.OK)  // Token error response
                                {
                                    TokenErrorResponse tokenErrorResponse = TokenErrorResponse.Deserialize(tokenResponseJson);
                                    actionResult = View(viewName: "Error", model: $"Token request failed. Error code: {tokenErrorResponse.Error}. Error description: {tokenErrorResponse.ErrorDescription}");
                                }
                                else  // Successful token response
                                {
                                    SuccessfulTokenResponse successfulTokenResponse = SuccessfulTokenResponse.Deserialize(tokenResponseJson);
                                    IdToken.ValidateAndLoadResult validateAndLoadIdTokenResult = IdToken.ValidateAndLoad(successfulTokenResponse.IdToken, Configuration.ISSUER_IDENTIFIER, Configuration.CLIENT_ID, Configuration.CLIENT_SECRET);
                                    if (!validateAndLoadIdTokenResult.IsValid)
                                    {
                                        actionResult = View(viewName: "Error", model: "ID token validation failed. Error: " + validateAndLoadIdTokenResult.ErrorMessage);
                                    }
                                    else if (validateAndLoadIdTokenResult.IdToken.Exp < DateTime.UtcNow)
                                    {
                                        actionResult = View(viewName: "Error", model: "ID token is expired.");
                                    }
                                    else if (validateAndLoadIdTokenResult.IdToken.Nonce != (string)Session["OAuthAntiReplayNonceToken"])
                                    {
                                        actionResult = View(viewName: "Error", model: "Invalid \"nonce\" token. Possible replay attack.");
                                    }
                                    else
                                    {
                                        // All checks passed. Login the user and redirect to the Home page.
                                        string userId = $"{validateAndLoadIdTokenResult.IdToken.Iss}|{validateAndLoadIdTokenResult.IdToken.Sub}";
                                        var claimsIdentity = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, userId) }, DefaultAuthenticationTypes.ApplicationCookie);
                                        HttpContext.GetOwinContext().Authentication.SignIn(new AuthenticationProperties { IsPersistent = false }, claimsIdentity);
                                        actionResult = RedirectToAction("Index", "Home");
                                    }
                                }
                            }
                        }
                    }
                }

                actionResult = null;
            }

            return actionResult;
        }
    }
}