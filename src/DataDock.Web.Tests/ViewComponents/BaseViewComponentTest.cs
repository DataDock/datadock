using DataDock.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using System.Security.Claims;

namespace DataDock.Web.Tests.ViewComponents
{
    public class BaseViewComponentTest
    {
        protected ViewComponentContext GetViewContext(Mock<HttpContext> mockContext, string withUserId)
        {
            if (!string.IsNullOrEmpty(withUserId))
            {
                WithAuthorizedUser(mockContext, withUserId);
            }
            var viewContext = new ViewContext { HttpContext = mockContext.Object };
            var viewComponentContext = new ViewComponentContext { ViewContext = viewContext };
            return viewComponentContext;
        }

        protected void WithAuthorizedUser(Mock<HttpContext> mockContext, string userId = "test_id")
        {
            var name = new Claim(ClaimTypes.Name, userId);
            var ghUser = new Claim(DataDockClaimTypes.GitHubUser, userId);
            var ghLogin = new Claim(DataDockClaimTypes.GitHubLogin, userId);
            var ghName = new Claim(DataDockClaimTypes.GitHubName, userId);

            var testPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {name, ghUser, ghLogin, ghName}, "MockUserAuthentication"));

            mockContext.Setup(m => m.User).Returns(testPrincipal);
        }

    }
}
