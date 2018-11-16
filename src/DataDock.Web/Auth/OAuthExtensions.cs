using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;

namespace DataDock.Web.Auth
{
    public static class OAuthExtensions
    {
        public static AuthenticationBuilder AddReverseProxyOAuth(this AuthenticationBuilder builder, string authenticationScheme,
            Action<OAuthOptions> configureOptions)
            => builder.AddOAuth<OAuthOptions, ReverseProxyOAuthHandler<OAuthOptions>>(authenticationScheme,
                configureOptions);
    }
}
