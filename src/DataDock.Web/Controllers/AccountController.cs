using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Web.Auth;
using DataDock.Web.Models;
using DataDock.Web.Services;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DataDock.Web.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly IUserStore _userStore;
        private readonly IOwnerSettingsStore _ownerSettingsStore;
        private readonly IRepoSettingsStore _repoSettingsStore;
        private readonly IDatasetStore _datasetStore;
        private readonly IJobStore _jobStore;
        private readonly ISchemaStore _schemaStore;

        public AccountController(IUserStore userStore, 
            IOwnerSettingsStore ownerSettingsStore, 
            IRepoSettingsStore repoSettingsStore, 
            IDatasetStore datasetStore,
            IJobStore jobStore,
            ISchemaStore schemaStore)
        {
            _userStore = userStore;
            _ownerSettingsStore = ownerSettingsStore;
            _repoSettingsStore = repoSettingsStore;
            _datasetStore = datasetStore;
            _jobStore = jobStore;
            _schemaStore = schemaStore;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> LogOff(string returnUrl = "/")
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Forbidden(string returnUrl = "/")
        {
            Log.Warning("User '{0}' has attempted to access forbidden page {1}", User?.Identity?.Name, returnUrl);
            return View("Forbidden");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SignUp()
        {
            Log.Debug("User: {0}. Identities: {1}. Claims Total: {2}", User.Identity.Name, User.Identities.Count(), User.Claims.Count());

            if (User.ClaimExists(DataDockClaimTypes.DataDockUserId))
            {
                return RedirectToAction("Settings");
            }

            try
            {
                // check for user in datadock just in case of login  / claims problems
                var datadockUser = await _userStore.GetUserAccountAsync(User.Identity.Name);
                if (datadockUser != null)
                {
                    // add identity to context User
                    // new datadock identity inc claim
                    var datadockIdentity = new ClaimsIdentity();
                    datadockIdentity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                    User.AddIdentity(datadockIdentity);
                    if (datadockUser.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.DataDockUserId)) ==
                        null)
                    {
                        // update datadock user account if required
                        await _userStore.UpdateUserAsync(User.Identity.Name, User.Claims);

                    }

                    // logout and back in to persist new claims
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Challenge(new AuthenticationProperties() {RedirectUri = "account/settings"});
                }

            }
            catch (UserAccountNotFoundException noUserException)
            {
                var viewModel = new SignUpViewModel
                {
                    Title = "DataDock New User",
                    Heading = "Sign Up to DataDock"
                };
                return View("SignUp", viewModel);
            }
            catch (Exception ex)
            {
                Log.Error("Error creating user account", ex);
                Console.WriteLine(ex);
                throw;
            }

            return View("SignUp");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Cancel()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("SignUpCancelled", "Info");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SignUp(SignUpViewModel signUpViewModel)
        {
            if (ModelState.IsValid && signUpViewModel.AgreeTerms)
            {
                try
                {
                    // new datadock identity inc claim
                    var datadockIdentity = new ClaimsIdentity();
                    datadockIdentity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                    User.AddIdentity(datadockIdentity);

                    // create user in datadock
                    var newUser = await _userStore.CreateUserAsync(User.Identity.Name, User.Claims);
                    if (newUser == null)
                    {
                        Log.Error("Creation of new user account returned null");
                        return RedirectToAction("Error", "Home");
                    }

                    // create userSettings
                    var userSettings = new UserSettings
                    {
                        UserId = User.Identity.Name,
                        LastModified = DateTime.UtcNow,
                        LastModifiedBy = "DataDock"
                    };
                    await _userStore.CreateOrUpdateUserSettingsAsync(userSettings);

                    // create ownerSettings for the github user owner
                    var userOwner = new OwnerSettings
                    {
                        OwnerId = User.Identity.Name,
                        IsOrg = false,
                        DisplayGitHubAvatar = true,
                        DisplayGitHubDescription = true,
                        LastModified = DateTime.UtcNow,
                        LastModifiedBy = "DataDock"
                    };
                    await _ownerSettingsStore.CreateOrUpdateOwnerSettingsAsync(userOwner);

                    // logout and back in to persist new claims
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Challenge(new AuthenticationProperties() { RedirectUri = "account/welcome" });
                }
                catch (UserAccountExistsException existsEx)
                {
                    Log.Warning("User account {0} already exists. Identities: {1}. Claims Total: {2}", User.Identity.Name, User.Identities.Count(), User.Claims.Count());
                    var datadockUser = await _userStore.GetUserAccountAsync(User.Identity.Name);
                    if (datadockUser.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.DataDockUserId)) ==
                        null)
                    {
                        // new datadock identity inc claim
                        var datadockIdentity = new ClaimsIdentity();
                        datadockIdentity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                        User.AddIdentity(datadockIdentity);
                        await _userStore.UpdateUserAsync(User.Identity.Name, User.Claims);
                        // logout and back in to persist new claims
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return Challenge(new AuthenticationProperties() { RedirectUri = "account/settings" });
                    }

                    return RedirectToAction("Settings");

                }
                catch (Exception ex)
                {
                    Log.Error("Error creating user account", ex);
                    Console.WriteLine(ex);
                    throw;
                }
            }

            ViewBag.Success = "failed";
            return View(signUpViewModel);
        }
 
        [HttpGet]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public async Task<IActionResult> Settings(string returnUrl = "/")
        {
            try
            {
                var userSettings = await _userStore.GetUserSettingsAsync(User.Identity.Name);
                var usvm = new UserSettingsViewModel(userSettings) {Title = "User Account"};
                return View(usvm);
            }
            catch (UserAccountNotFoundException notFoundEx)
            {
                var newSettings = new UserSettingsViewModel {UserId = User.Identity.Name};
                return View(newSettings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public async Task<IActionResult> Settings(UserSettingsViewModel usvm)
        {
            try
            {
                usvm.LastModified = DateTime.UtcNow;
                usvm.LastModifiedBy = User.Identity.Name;
                var userSettings = usvm.AsUserSettings();
                await _userStore.CreateOrUpdateUserSettingsAsync(userSettings);

                ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.ChangeSettingSuccess);
                
                return View(usvm);
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred loading user settings for '{0}'", User.Identity.Name);
                ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.Error);
                return View(new UserSettingsViewModel { UserId = User.Identity.Name });
            }
        }

        public string GetSettingsStatusMessage(ManageMessageId? message = null)
        {
            if (message == null)
            {
                // check in TempData if a message isn't directly supplied
                message = TempData["message"] as ManageMessageId?;
            }
            var statusMessage = message == ManageMessageId.ChangeSettingSuccess
                ? @"The settings have been successfully updated."
                : message == ManageMessageId.Error
                    ? @"An error has occurred."
                    : message == ManageMessageId.TokenResetError ?
                        @"Unable to reset token." :
                        @"";
            return statusMessage;
        }

        [HttpGet]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public async Task<IActionResult> Delete(string returnUrl = "")
        {
            try
            {
                // check user exists
                var userAccount = await _userStore.GetUserAccountAsync(User.Identity.Name);
                var davm = new DeleteAccountViewModel();
                return View(davm);
            }
            catch (Exception e)
            {
                Log.Warning($"User account not found: '{User.Identity.Name}'");
                var davm = new DeleteAccountViewModel();
                ModelState.AddModelError("", $"No user found with name '{User.Identity.Name}'");
                return View(davm);
            }
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public async Task<IActionResult> Delete(DeleteAccountViewModel davm)
        {
            try
            {
                if (!ModelState.IsValid || !davm.Confirm)
                {
                    ModelState.AddModelError("", "You must confirm that you want to delete your user account and associated settings.");
                    return View("Delete");
                }
                var deleted = await _userStore.DeleteUserAsync(User.Identity.Name);
                if (deleted)
                {
                    // delete user's settings
                    try
                    {
                        var us = await _userStore.GetUserSettingsAsync(User.Identity.Name);
                        await _userStore.DeleteUserSettingsAsync(User.Identity.Name);
                    }
                    catch (UserSettingsNotFoundException e)
                    {
                        // no action needed 
                    }
                    catch (UserStoreException e)
                    {
                        Log.Error($"Error deleting user settings during user account {User.Identity.Name} deletion", e);
                    }

                    // repositories
                    try
                    {
                        var repos = await _repoSettingsStore.GetRepoSettingsForOwnerAsync(User.Identity.Name);
                        foreach (var r in repos)
                        {
                            await _repoSettingsStore.DeleteRepoSettingsAsync(r.OwnerId, r.RepositoryId);
                        }
                    }
                    catch (RepoSettingsNotFoundException e)
                    {
                        // no action needed
                    }
                    catch (RepoSettingsStoreException e)
                    {
                        Log.Error($"Error deleting repo settings during user account {User.Identity.Name} deletion", e);
                    }

                    // owner settings
                    try
                    {
                        var owner = await _ownerSettingsStore.GetOwnerSettingsAsync(User.Identity.Name);
                        await _ownerSettingsStore.DeleteOwnerSettingsAsync(owner.OwnerId);
                    }
                    catch (OwnerSettingsNotFoundException e)
                    {
                        // no action needed
                    }
                    catch (OwnerSettingsStoreException e)
                    {
                        Log.Error($"Error deleting owner settings during user account {User.Identity.Name} deletion", e);
                    }

                    // datasets
                    try
                    {
                        await _datasetStore.DeleteDatasetsForOwnerAsync(User.Identity.Name);
                    }
                    catch (DatasetStoreException e)
                    {
                        Log.Error($"Error deleting datasets during user account {User.Identity.Name} deletion", e);
                    }

                    // templates
                    try
                    {
                        await _schemaStore.DeleteSchemaRecordsForOwnerAsync(User.Identity.Name);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error deleting templates during user account {User.Identity.Name} deletion", e);
                    }

                    // jobs
                    try
                    {
                        await _jobStore.DeleteJobsForOwnerAsync(User.Identity.Name);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error deleting jobs during user account {User.Identity.Name} deletion", e);
                    }
                        

                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Index", "Home");
                }

                // error
                ViewBag.Message =
                    "Unable to delete account at this time, if the problem persists please open a ticket with support.";
                return View("Delete");

                return View("Delete");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public IActionResult Welcome(string returnUrl = "/")
        {
            return View("Welcome", new BaseLayoutViewModel{Title = "Welcome to DataDock"});
        }
    }
}