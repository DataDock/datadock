using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DataDock.Web.Auth
{
    public class ReverseProxyOAuthHandler<TOptions> : OAuthHandler<TOptions> where TOptions : OAuthOptions, new()
    {
        public ReverseProxyOAuthHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        // Copy from https://github.com/aspnet/Security/blob/dev/src/Microsoft.AspNetCore.Authentication.OAuth/OAuthHandler.cs
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = CurrentUri;
            }

            // OAuth2 10.12 CSRF
            GenerateCorrelationId(properties);

            var authorizationEndpoint = BuildChallengeUrl(properties, BuildRedirectUri(Options.CallbackPath));
            var redirectContext = new RedirectContext<OAuthOptions>(
                Context, Scheme, Options, properties,
                authorizationEndpoint);
            await Events.RedirectToAuthorizationEndpoint(redirectContext);
        }

        protected new string BuildRedirectUri(string targetPath)
        {
            var schema = Request.Headers["X-Forwarded-Proto"].Count > 0
                ? Request.Headers["X-Forwarded-Proto"][0]
                : Request.Scheme;
            var host = Request.Headers["X-Forwarded-Host"].Count > 0
                ? new HostString(Request.Headers["X-Forwarded-Host"][0])
                : Request.Host;
            return schema + "://" + host + OriginalPathBase + targetPath;
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri)
        {
            var tokenRequestParameters = new Dictionary<string, string>()
            {
                {"client_id", Options.ClientId},
                {"redirect_uri", BuildRedirectUri(Options.CallbackPath)},
                {"client_secret", Options.ClientSecret},
                {"code", code},
                {"grant_type", "authorization_code"},
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            var response = await Backchannel.SendAsync(requestMessage, Context.RequestAborted);
            if (response.IsSuccessStatusCode)
            {
                var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
                return OAuthTokenResponse.Success(payload);
            }
            else
            {
                var error = "OAuth token endpoint failure: " + await Display(response);
                return OAuthTokenResponse.Failed(new Exception(error));
            }
        }

        private static async Task<string> Display(HttpResponseMessage response)
        {
            var output = new StringBuilder();
            output.Append("Status: " + response.StatusCode + ";");
            output.Append("Headers: " + response.Headers.ToString() + ";");
            output.Append("Body: " + await response.Content.ReadAsStringAsync() + ";");
            return output.ToString();
        }
    }
}