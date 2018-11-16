using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Common.Validators;
using Nest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataDock.Common.Elasticsearch
{
    public class UserStore : IUserStore
    {
        private readonly IElasticClient _client;

        public UserStore(IElasticClient client,ApplicationConfiguration config)
        {
            var userSettingsIndexName = "obsolete";
            var userAccountIndexName = config.UserIndexName;
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(userSettingsIndexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", userSettingsIndexName, typeof(UserSettings));
                var createIndexResponse = _client.CreateIndex(userSettingsIndexName,
                    c => c.Mappings(mappings => mappings.Map<UserSettings>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", userSettingsIndexName, createIndexResponse.DebugInformation);
                    throw new DataDockException(
                        $"Could not create index {userSettingsIndexName} for UserStore. Cause: {createIndexResponse.DebugInformation}");
                }
            }

            // Repeat for user account index
            indexExistsReponse = _client.IndexExists(userAccountIndexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", userAccountIndexName, typeof(UserAccount));
                var createIndexResponse = _client.CreateIndex(userAccountIndexName,
                    c => c.Mappings(mappings => mappings.Map<UserAccount>(m => m.AutoMap())));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", userAccountIndexName, createIndexResponse.DebugInformation);
                    throw new DataDockException(
                        $"Could not create index {userAccountIndexName} for UserStore. Cause: {createIndexResponse.DebugInformation}");
                }
            }
            // Set default indexes for repository types
            _client.ConnectionSettings.DefaultIndices[typeof(UserAccount)] = userAccountIndexName;
            _client.ConnectionSettings.DefaultIndices[typeof(UserSettings)] = userSettingsIndexName;
            

        }

        public async Task<UserSettings> GetUserSettingsAsync(string userId)
        {
            var response = await _client.GetAsync<UserSettings>(userId);
            if (!response.IsValid)
            {
                if (!response.Found) throw new UserSettingsNotFoundException(userId);
                throw new UserStoreException(
                    $"Error retrieving user account for user ID {userId}. Cause: {response.DebugInformation}");
            }
            return response.Source;
        }

        public async Task CreateOrUpdateUserSettingsAsync(UserSettings userSettings)
        {
            if (userSettings == null) throw new ArgumentNullException(nameof(userSettings));
            var validator = new UserSettingsValidator();
            var validationResults = await validator.ValidateAsync(userSettings);
            if (!validationResults.IsValid)
            {
                throw new ValidationException("Invalid user settings", validationResults);
            }
            var updateResponse = await _client.IndexDocumentAsync(userSettings);
            if (!updateResponse.IsValid)
            {
                throw new UserStoreException($"Error udpating user settings for user ID {userSettings.UserId}");
            }
        }

        public async Task<bool> DeleteUserSettingsAsync(string userId)
        {
            var response = await _client.DeleteAsync<UserSettings>(userId);
            return response.IsValid;
        }

        public async Task<UserAccount> CreateUserAsync(string userId, IEnumerable<Claim> claims)
        {
            var user = new UserAccount
            {
                UserId = userId,
                AccountClaims = claims.Select(c=>new AccountClaim(c)).ToList()
            };
            var existsResponse = await _client.DocumentExistsAsync<UserAccount>(user);
            if (existsResponse.Exists) throw new UserAccountExistsException(userId);
            await _client.IndexDocumentAsync(user);
            return user;
        }

        public async Task<UserAccount> UpdateUserAsync(string userId, IEnumerable<Claim> claims)
        {
            var user = new UserAccount
            {
                UserId = userId,
                AccountClaims = claims.Select(c => new AccountClaim(c)).ToList()
            };
            var existsResponse = await _client.DocumentExistsAsync<UserAccount>(user);
            if (!existsResponse.Exists) throw new UserAccountNotFoundException(userId);
            await _client.IndexDocumentAsync(user);
            return user;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var response = await _client.DeleteAsync<UserAccount>(userId);
            return response.IsValid;
        }

        public async Task<UserAccount> GetUserAccountAsync(string userId)
        {
            var response = await _client.GetAsync<UserAccount>(userId);
            if (!response.Found) throw new UserAccountNotFoundException(userId);
            return response.Source;
        }

    }
}
