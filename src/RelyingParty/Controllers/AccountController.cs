﻿using System;
using System.Web;
using System.Web.Mvc;

namespace RelyingParty.Controllers
{
    public class AccountController
        : Controller
    {
        // POST: /Account/Login
        [HttpPost]
        public RedirectResult Login()
        {
            string exchangeUrl = "https://www.yahoo.com/";
            string antiForgeryStateToken = Guid.NewGuid().ToString();
            Session["OAuthAntiForgeryStateToken"] = antiForgeryStateToken;
            string antiReplayNonceToken = Guid.NewGuid().ToString();
            Session["OAuthAntiReplayNonceToken"] = antiReplayNonceToken;

            string authenticationRequestUrl = $"{Configuration.AUTHORIZATION_ENDPOINT}?scope=openid&response_type=code&client_id={Configuration.CLIENT_ID}&redirect_uri={HttpUtility.UrlEncode(exchangeUrl)}&state={antiForgeryStateToken}&nonce={antiReplayNonceToken}";
            return Redirect(authenticationRequestUrl);
        }
    }
}