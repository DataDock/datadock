using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace DataDock.Web.Tests.Controllers
{
    public class LinkedDataControllerTests : IClassFixture<MockProxyFixture>
    {
        private readonly MockProxyFixture _fixture;
        public LinkedDataControllerTests(MockProxyFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async void ProxyMethodCopiesCorsHeader()
        {
            var mockOwnerSettings = new Mock<IOwnerSettingsStore>();
            var mockRepoSettings = new Mock<IRepoSettingsStore>();
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequestHeaders = new HttpRequestHeaders {HeaderAccept = new StringValues("application/n-quads")};
            var mockResponseHeaders = new HeaderDictionary();
            var mockResponseStream = new MemoryStream();
            mockHttpContext.Setup(c => c.Request.Headers).Returns(mockRequestHeaders);
            //var response = new DefaultHttpResponse(mockHttpContext.Object);
            mockHttpContext.SetupProperty(c => c.Response.StatusCode);
            mockHttpContext.SetupGet(c=>c.Response.Headers).Returns(mockResponseHeaders);
            mockHttpContext.SetupGet(c => c.Response.Body).Returns(mockResponseStream);
            var controller = new LinkedDataController(mockOwnerSettings.Object, mockRepoSettings.Object)
            {
                ControllerContext = new ControllerContext(new ActionContext(mockHttpContext.Object, new RouteData(),
                    new ControllerActionDescriptor()))
            };
            await controller.ProxyRequest(new Uri("http://localhost:" + _fixture.Server.Ports[0] + "/owner/repo/data/void.nq"));

            mockResponseHeaders.Should().ContainKey("Access-Control-Allow-Origin");
            mockResponseHeaders["Access-Control-Allow-Origin"].Should().HaveCount(1).And.Contain("*");
        }
    }

    public class MockProxyFixture : IDisposable
    {
        public readonly FluentMockServer Server;

        public MockProxyFixture()
        {
            Server = FluentMockServer.Start();

            Server.Given(Request.Create().WithPath("/owner/repo/data/void.nq").UsingGet())
                .RespondWith(
                    Response.Create().WithStatusCode(200)
                        .WithBody("hello world")
                        .WithHeader("Content-Type", "application/n-quads")
                        .WithHeader("Access-Control-Allow-Origin", "*"));
        }

        public void Dispose()
        {
            Server.Stop();
        }
    }
}
