using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    }
}