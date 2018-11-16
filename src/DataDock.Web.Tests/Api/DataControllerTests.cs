using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Web.Api;
using DataDock.Web.Models;
using DataDock.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace DataDock.Web.Tests.Api
{
    public class DataControllerTests
    {
        private readonly Mock<IUserStore> _mockUserStore;
        private readonly Mock<IRepoSettingsStore> _mockRepoSettingsStore;
        private readonly Mock<IJobStore> _mockJobStore;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<IImportService> _mockImportService;

        public DataControllerTests()
        {
            _mockUserStore = new Mock<IUserStore>();
            _mockRepoSettingsStore = new Mock<IRepoSettingsStore>();
            _mockJobStore = new Mock<IJobStore>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockImportService = new Mock<IImportService>();

            _mockHttpContext.Setup(m => m.Request.Method).Returns("POST");
        }

        private void WithAuthorizedUser(string userId = "test_id")
        {
            var testPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new []
                {new Claim(ClaimTypes.Name, userId)}, "MockUserAuthentication"));
            _mockHttpContext.Setup(m => m.User).Returns(testPrincipal);

            var dummyUserSettings = new UserSettings();
            _mockUserStore.Setup(sr => sr.GetUserSettingsAsync(It.IsAny<string>())).Returns(Task.FromResult(dummyUserSettings));
        }



        [Fact]
        public void AnonymousUserNotAllowed()
        {
            // No user added to request context - simulating anonymous access
            var formParserMock = new Mock<IImportFormParser>();
            var controller = new DataController(_mockUserStore.Object, _mockRepoSettingsStore.Object, _mockJobStore.Object, formParserMock.Object, _mockImportService.Object);
            var result = controller.Post().Result;
            Assert.NotNull(result);
            var unauthorizedResult = result as UnauthorizedResult;
            Assert.NotNull(unauthorizedResult);
        }

        [Fact]
        public void UnauthenticatedUserNotAllowed()
        {
            // User added to request context but IsAuthenticated is false - simulating unauthenticated access
            var testPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] {new Claim(ClaimTypes.Name, "test_id")}));
            _mockHttpContext.Setup(m => m.User).Returns(testPrincipal);
            _mockUserStore.Setup(sr => sr.GetUserSettingsAsync(It.IsAny<string>())).ReturnsAsync(new UserSettings());
            var formParserMock = new Mock<IImportFormParser>();
            var controller = new DataController(_mockUserStore.Object, _mockRepoSettingsStore.Object, _mockJobStore.Object, formParserMock.Object, _mockImportService.Object);
            var result = controller.Post().Result;
            Assert.NotNull(result);
            var unauthorizedResult = result as UnauthorizedResult;
            Assert.NotNull(unauthorizedResult);
        }

        [Fact]
        public void UnauthorizedWhenUserSettingsNotFound()
        {
            WithAuthorizedUser();
            _mockUserStore.Setup(x => x.GetUserSettingsAsync("test_id")).ReturnsAsync((UserSettings)null);
            var formParserMock = new Mock<IImportFormParser>();
            var controller = new DataController(_mockUserStore.Object, _mockRepoSettingsStore.Object,
                _mockJobStore.Object, formParserMock.Object, _mockImportService.Object)
            {
                ControllerContext = new ControllerContext(new ActionContext(_mockHttpContext.Object, new RouteData(),
                    new ControllerActionDescriptor()))
            };
            var result = controller.Post().Result;
            Assert.NotNull(result);
            var unauthorizedResult = result as UnauthorizedResult;
            Assert.NotNull(unauthorizedResult);
        }

        [Fact]
        public void BadRequestWhenFormIsInvalid()
        {
            WithAuthorizedUser();
            _mockUserStore.Setup(x => x.GetUserSettingsAsync("test_id")).ReturnsAsync(new UserSettings());
            var formParserMock = new Mock<IImportFormParser>();
            formParserMock
                .Setup(x => x.ParseImportFormAsync(It.IsAny<HttpRequest>(), "test_id",
                    It.IsAny<Func<ImportFormData, IFormCollection, Task<bool>>>()))
                .ReturnsAsync(new ImportFormParserResult("There was some error in your form"));
            var controller = new DataController(_mockUserStore.Object, _mockRepoSettingsStore.Object, _mockJobStore.Object, formParserMock.Object, _mockImportService.Object)
            {
                ControllerContext = new ControllerContext(new ActionContext(_mockHttpContext.Object, new RouteData(),
                    new ControllerActionDescriptor()))
            };
            var result = controller.Post().Result;
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }


        [Fact]
        public void SimpleValidRequest()
        {
            WithAuthorizedUser();
            var parsedForm = new ImportFormParserResult(
                new ImportJobRequestInfo
                {
                    UserId = "test_id"
                },
                @"{'foo':'bar'}");
            var formParserMock = new Mock<IImportFormParser>();
            formParserMock.Setup(x => x.ParseImportFormAsync(It.IsAny<HttpRequest>(), "test_id",
                It.IsAny<Func<ImportFormData, IFormCollection, Task<bool>>>())).ReturnsAsync(parsedForm);
            _mockJobStore.Setup(x => x.SubmitImportJobAsync(It.IsAny<ImportJobRequestInfo>()))
                .ReturnsAsync(new JobInfo() {JobId = "test_job"});
            var controller = new DataController(
                _mockUserStore.Object,
                _mockRepoSettingsStore.Object,
                _mockJobStore.Object,
                formParserMock.Object, _mockImportService.Object)
            {
                ControllerContext = new ControllerContext(new ActionContext(_mockHttpContext.Object, new RouteData(),
                    new ControllerActionDescriptor()))
            };

            var result = controller.Post().Result;
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.IsType<DataControllerResult>(okResult.Value);

            var resultValue = okResult.Value as DataControllerResult;
            Assert.NotNull(resultValue);
            Assert.Equal(@"{'foo':'bar'}", resultValue.Metadata);
            Assert.Equal("test_job", resultValue.JobIds[0]);

            // Check dependency calls
            // Import Form parser should be invoked once
            formParserMock.Verify(x => x.ParseImportFormAsync(It.IsAny<HttpRequest>(), "test_id",
                It.IsAny<Func<ImportFormData, IFormCollection, Task<bool>>>()), Times.Once);

            // An import job should be created
            _mockJobStore.Verify(m =>
                m.SubmitImportJobAsync(It.Is<ImportJobRequestInfo>(x => x.UserId.Equals("test_id"))));
        }

    }
}
