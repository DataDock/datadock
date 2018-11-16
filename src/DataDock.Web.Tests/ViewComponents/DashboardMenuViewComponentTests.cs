using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Web.ViewComponents;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DataDock.Web.Tests.ViewComponents
{
    public class DashboardMenuViewComponentTests : BaseViewComponentTest
    {
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<IRepoSettingsStore> _mockRepoSettingsStore;

        public DashboardMenuViewComponentTests()
        {
            _mockHttpContext = new Mock<HttpContext>();
            _mockRepoSettingsStore = new Mock<IRepoSettingsStore>();

            var repo1 = new RepoSettings
            {
                OwnerId = "owner-1",
                RepositoryId = "repo-1",
                Id = "owner-1/repo-1",
                OwnerIsOrg = false
            };
            var repo2 = new RepoSettings
            {
                OwnerId = "owner-1",
                RepositoryId = "repo-2",
                Id = "owner-1/repo-2",
                OwnerIsOrg = false
            };
            var repo3 = new RepoSettings
            {
                OwnerId = "owner-1",
                RepositoryId = "repo-3",
                Id = "owner-1/repo-3",
                OwnerIsOrg = false
            };
            var repos = new List<RepoSettings> { repo1, repo2, repo3 };

            _mockRepoSettingsStore.Setup(m => m.GetRepoSettingsForOwnerAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<IEnumerable<RepoSettings>>(repos));
        }

        
        [Fact]
        public void ViewComponentLoadsPublicWithOwnerId()
        {
            var ownerId = "owner-1";
            var area = "summary";
            var vc = new DashboardMenuViewComponent(_mockRepoSettingsStore.Object);
            var asyncResult = vc.InvokeAsync(ownerId, "", area);
            
            Assert.NotNull(asyncResult.Result);
            _mockRepoSettingsStore.Verify(m => m.GetRepoSettingsForOwnerAsync(ownerId), Times.Never);
            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);
            var model = result.ViewData?.Model as DashboardMenuViewModel;
            Assert.NotNull(model);

            Assert.Equal(ownerId, model.SelectedOwnerId);
            Assert.Equal(area, model.ActiveArea);
            Assert.Equal("", model.SelectedRepoId);
            Assert.Null(model.UserViewModel);
        }

        [Fact]
        public void ViewComponentLoadsPrivateWithOwnerId()
        {
            var userName = "owner-1";
            var ownerId = "owner-1";
            var area = "summary";

            var vc = new DashboardMenuViewComponent(_mockRepoSettingsStore.Object);
            // Set user
            vc.ViewComponentContext = GetViewContext(_mockHttpContext, userName);

            var asyncResult = vc.InvokeAsync(ownerId, "", area);

            Assert.NotNull(asyncResult.Result);
            
            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);
            var model = result.ViewData?.Model as DashboardMenuViewModel;
            Assert.NotNull(model);

            Assert.Equal(ownerId, model.SelectedOwnerId);
            Assert.Equal(area, model.ActiveArea);
            Assert.Equal("", model.SelectedRepoId);
            Assert.NotNull(model.UserViewModel);
            Assert.Equal(userName, model.UserViewModel.GitHubName);
            Assert.NotNull(model.Owners);
            Assert.Single(model.Owners);
            Assert.Equal("owner-1", model.GetDashContext());

            _mockRepoSettingsStore.Verify(m => m.GetRepoSettingsForOwnerAsync(ownerId), Times.Once);
        }

        [Fact]
        public void ViewComponentLoadsPrivateWithOwnerIdandRepoId()
        {
            var userName = "owner-1";
            var ownerId = "owner-1";
            var repoId = "repo-1";
            var area = "summary";

            var vc = new DashboardMenuViewComponent(_mockRepoSettingsStore.Object);
            // Set user
            vc.ViewComponentContext = GetViewContext(_mockHttpContext, userName);

            var asyncResult = vc.InvokeAsync(ownerId, repoId, area);

            Assert.NotNull(asyncResult.Result);

            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);
            var model = result.ViewData?.Model as DashboardMenuViewModel;
            Assert.NotNull(model);

            Assert.Equal(ownerId, model.SelectedOwnerId);
            Assert.Equal(area, model.ActiveArea);
            Assert.Equal(repoId, model.SelectedRepoId);
            Assert.NotNull(model.UserViewModel);
            Assert.Equal(userName, model.UserViewModel.GitHubName);
            Assert.NotNull(model.Owners);
            Assert.Single(model.Owners);
            Assert.Equal("owner-1/repo-1", model.GetDashContext());

            _mockRepoSettingsStore.Verify(m => m.GetRepoSettingsForOwnerAsync(ownerId), Times.Once);
        }
    }
}