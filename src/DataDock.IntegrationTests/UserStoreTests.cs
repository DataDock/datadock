using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class UserStoreTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly UserStore _userStore;

        public UserStoreTests(ElasticsearchFixture fixture)
        {
            var esFixture = fixture;
            _userStore = new UserStore(esFixture.Client, fixture.Configuration);
        }

        [Fact]
        public async void CreateAndRetrieveUserAccount()
        {
            var accountClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, "test1@example.org"),
                new Claim(ClaimTypes.Name, "Test User Name"),
                new Claim(DataDockClaimTypes.GitHubAccessToken, "some_access_token_value")
            };
            var userAccount = await _userStore.CreateUserAsync("create1", accountClaims);
            Assert.NotNull(userAccount);
            Assert.Equal("create1", userAccount.UserId);
            foreach (var claim in accountClaims)
            {
                Assert.Contains(userAccount.AccountClaims, c =>c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
                Assert.Contains(userAccount.Claims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
            }

            var retrievedAccount = await _userStore.GetUserAccountAsync("create1");
            Assert.NotNull(retrievedAccount);
            Assert.Equal("create1", retrievedAccount.UserId);
            Assert.Equal(3, retrievedAccount.Claims.Count());
            foreach (var claim in accountClaims)
            {
                Assert.Contains(userAccount.AccountClaims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
                Assert.Contains(userAccount.Claims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
            }
        }

        [Fact]
        public async void UpdateExistingUserAccount()
        {
            var initialAccountClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, "test1@example.org"),
                new Claim(ClaimTypes.Name, "Test User Name"),
                new Claim(DataDockClaimTypes.GitHubAccessToken, "some_access_token_value")
            };
            var updatedAccountClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "updated@example.org"),
                new Claim(ClaimTypes.Name, "Updated User Name"),
                new Claim(DataDockClaimTypes.GitHubAccessToken, "some_access_token_value")
            };
            await _userStore.CreateUserAsync("update1", initialAccountClaims);
            await _userStore.UpdateUserAsync("update1", updatedAccountClaims);
            var retrievedAccount = await _userStore.GetUserAccountAsync("update1");
            Assert.Equal("update1", retrievedAccount.UserId);
            Assert.Equal(3, retrievedAccount.Claims.Count());
            foreach (var claim in updatedAccountClaims)
            {
                Assert.Contains(retrievedAccount.AccountClaims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
                Assert.Contains(retrievedAccount.Claims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
            }
        }

    }
}
