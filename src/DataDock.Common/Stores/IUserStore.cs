using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DataDock.Common.Models;

namespace DataDock.Common.Stores
{
    public interface IUserStore
    {
        /// <summary>
        /// Retrieve the settings for the specified user
        /// </summary>
        /// <param name="userId">The datadock user id</param>
        /// <returns>The settings for the user or null if no settings could be found</returns>
        Task<UserSettings> GetUserSettingsAsync(string userId);

        /// <summary>
        /// Add or update user settings
        /// </summary>
        /// <param name="userSettings">The settings to be updated / created</param>
        Task CreateOrUpdateUserSettingsAsync(UserSettings userSettings);

        Task<bool> DeleteUserSettingsAsync(string userId);

        /// <summary>
        /// Create a new user account
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<UserAccount> CreateUserAsync(string userId, IEnumerable<Claim> accountClaims);

        /// <summary>
        /// Update an existing user account
        /// </summary>
        /// <param name="userId">The ID of the user account to be updated</param>
        /// <param name="updatedClaims">The new claims to assign to the account. These *overwrite* all existing claims for the account</param>
        /// <returns></returns>
        Task<UserAccount> UpdateUserAsync(string userId, IEnumerable<Claim> updatedClaims);

        /// <summary>
        /// Remove the settings for the specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>True if successfully deleted.</returns>
        Task<bool> DeleteUserAsync(string userId);

        Task<UserAccount> GetUserAccountAsync(string userId);

    }
}
