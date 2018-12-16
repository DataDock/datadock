using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Web.Models;
using DataDock.Web.ViewModels;
using FluentAssertions;
using Xunit;

namespace DataDock.Web.Tests.ViewModels
{
    public class DashboardMenuViewModelTests
    {
        [Fact]
        public void TestDashContextForOwnerOnly()
        {
            var vm = new DashboardMenuViewModel {SelectedOwnerId = "owner"};
            vm.GetDashContext().Should().Be("owner");
        }

        [Fact]
        public void TestDashContextForOwnerAndRepo()
        {
            var vm = new DashboardMenuViewModel{SelectedOwnerId = "owner", SelectedRepoId = "repo"};
            vm.GetDashContext().Should().Be("owner/repo");
        }

        [Fact]
        public void TestDashContextPreservesCase()
        {
            var vm = new DashboardMenuViewModel {SelectedOwnerId = "OwnerA", SelectedRepoId = "RepoName"};
            vm.GetDashContext().Should().Be("OwnerA/RepoName");
        }

        [Fact]
        public void TestAreaIsActive()
        {
            var vm = new DashboardMenuViewModel {SelectedOwnerId = "owner", SelectedRepoId = "repo", ActiveArea = "someArea"};
            vm.AreaIsActive("notActive").Should().BeEmpty();
            vm.AreaIsActive("someArea").Should().Be("active");
            vm.AreaIsActive("somearea").Should().Be("active");
        }

        [Fact]
        public void GetActiveOwnerReturnsFirstCaseInsensitiveOwnerIdMatch()
        {
            var vm = new DashboardMenuViewModel
            {
                SelectedOwnerId = "owner", Owners = new List<OwnerInfo>
                {
                    new OwnerInfo {OwnerId = "Owner", AvatarUrl = "http://example.org/url1"},
                    new OwnerInfo {OwnerId = "owner", AvatarUrl = "http://example.org/url2"}
                }
            };
            var activeOwner = vm.GetActiveOwner();
            activeOwner.Should().NotBeNull();
            activeOwner.AvatarUrl.Should().Be("http://example.org/url1");
        }
    }
}
