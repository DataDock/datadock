using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Common;
using DataDock.Web.Auth;
using DataDock.Web.Routing;
using DataDock.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json.Linq;
using Octokit;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using DataDock.Web.Config;
using Elasticsearch.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;
using Nest.JsonNetSerializer;
using HttpMethod = System.Net.Http.HttpMethod;

namespace DataDock.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private readonly string ApiCorsPolicy = "_apiOrigins";

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new WebConfiguration();
            Configuration.Bind(config);

            // Set up logging
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ImageType", "Web")
                .WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri(config.ElasticsearchUrl))
                    {
                        MinimumLogEventLevel = LogEventLevel.Debug,
                        AutoRegisterTemplate = true,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6
                    })
                .CreateLogger();


            services.AddOptions();

            // Angular's default header name for sending the XSRF token.
            services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
            services.AddCors(options =>
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").Equals("Development"))
                {
                    options.AddPolicy(ApiCorsPolicy, builder => { builder.WithOrigins("*"); });
                }
            });

            services.AddRazorPages().AddNewtonsoftJson();
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddSignalR();
            var client = new ElasticClient(
                new ConnectionSettings(
                    new SingleNodeConnectionPool(new Uri(config.ElasticsearchUrl)),
                    JsonNetSerializer.Default));
            Log.Information("Waiting for Elasticsearch cluster");
            client.WaitForInitialization();
            Log.Information("Elasticsearch cluster is available");

            services.AddScoped<AccountExistsFilter>();
            services.AddScoped<OwnerAdminAuthFilter>();

            services.AddSingleton<WebConfiguration>(config);
            services.AddSingleton<ApplicationConfiguration>(config);
            services.AddSingleton<IElasticClient>(client);
            services.AddSingleton<IUserStore, UserStore>();
            services.AddSingleton<IJobStore, JobStore>();
            services.AddSingleton<IOwnerSettingsStore, OwnerSettingsStore>();
            services.AddSingleton<IRepoSettingsStore, RepoSettingsStore>();
            services.AddSingleton<IImportFormParser, DefaultImportFormParser>();
            services.AddSingleton<IDatasetStore, DatasetStore>();
            services.AddSingleton<ISchemaStore, SchemaStore>();
            services.AddSingleton<IImportService, ImportService>();
            services.AddSingleton<IFileStore, DirectoryFileStore>();
            services.AddSingleton<ILogStore, DirectoryLogStore>();
            services.AddSingleton<IDataDockUriService>(new DataDockUriService(config.PublishUrl));

            var gitHubClientHeader = config.GitHubClientHeader;
            services.AddSingleton<IGitHubClientFactory>(new GitHubClientFactory(gitHubClientHeader));
            services.AddTransient<IGitHubApiService, GitHubApiService>();

            // Enable 
            if (string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_FORWARDEDHEADERS_ENABLED"),
                "true", StringComparison.OrdinalIgnoreCase))
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                               ForwardedHeaders.XForwardedProto;
                    // Only loopback proxies are allowed by default.
                    // Clear that restriction because forwarders are enabled by explicit 
                    // configuration.
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            }

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "GitHub";
                })
                .AddCookie(options => {
                    options.LoginPath = "/account/login/";
                    options.LogoutPath = new PathString("/account/logoff/");
                    options.AccessDeniedPath = "/account/forbidden/";
                })
                .AddOAuth("GitHub", options =>
                {
                    options.ClientId = config.OAuthClientId;
                    options.ClientSecret = config.OAuthClientSecret;
                    options.CallbackPath = new PathString("/signin-github");

                    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                    options.UserInformationEndpoint = "https://api.github.com/user";

                    options.Scope.Clear();
                    options.Scope.Add("user:email");
                    options.Scope.Add("read:org");
                    options.Scope.Add("public_repo");

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubId, "id");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubLogin, "login");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubName, "name");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubEmail, "email");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubUrl, "html_url");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubAvatar, "avatar_url");


                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var request =
                                new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization =
                                new AuthenticationHeaderValue("Bearer", context.AccessToken);

                            var response = await context.Backchannel.SendAsync(request,
                                HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                            context.RunClaimActions(user.RootElement);
                            context.Identity.AddClaim(new Claim(DataDockClaimTypes.GitHubAccessToken, context.AccessToken));
                            

                            // check if authorized user exists in DataDock
                            await AddOrganizationClaims(context, user.RootElement);
                            await EnsureUser(context, user.RootElement);
                        }
                    };
                });

            var admins = config.AdminLogins?.Split(",").Select(a=>a.Trim()).ToList();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("User", policy => policy.RequireClaim(DataDockClaimTypes.DataDockUserId));
                if (admins != null)
                {
                    options.AddPolicy("Admin", policy => policy.RequireClaim(DataDockClaimTypes.GitHubLogin, admins));
                }
            });
        }

        private static string GetOAuthLoginClaim(JsonElement claims)
        {
            return !claims.TryGetProperty("login", out var loginElement) ? null : loginElement.GetString();
        }

        private async Task EnsureUser(OAuthCreatingTicketContext context, JsonElement user)
        {
            var login = GetOAuthLoginClaim(user);
            if (string.IsNullOrEmpty(login)) return;

            var userStore = context.HttpContext.RequestServices.GetService<IUserStore>();
            try
            {
                var existingAccount = await userStore.GetUserAccountAsync(login);
                if (existingAccount != null)
                {
                    context.Identity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, login));
                    // refresh claims on user account as claims may have changed since last login
                    await userStore.UpdateUserAsync(existingAccount.UserId, context.Identity.Claims);
                }
            }
            catch (UserAccountNotFoundException)
            {
                // user not found. no action required
            }
        }

        
        private async Task AddOrganizationClaims(OAuthCreatingTicketContext context, JsonElement user)
        {
            var login = GetOAuthLoginClaim(user);
            if (string.IsNullOrEmpty(login)) return;

            var gitHubApiService = context.HttpContext.RequestServices.GetService<IGitHubApiService>();
            if (gitHubApiService == null)
            {
                Log.Error("Unable to instantiate the GitHub API service");
                return;
            }

            var orgs = await gitHubApiService.GetOrganizationsForUserAsync(context.Identity);
            if (orgs != null)
            {
                foreach (var org in orgs)
                {
                    var jsonString = JsonSerializer.Serialize(new {ownerId = org.Login, avatarUrl = org.AvatarUrl});
                    context.Identity.AddClaim(new Claim(DataDockClaimTypes.GitHubUserOrganization, jsonString));
                }
            }
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog();
            app.UseForwardedHeaders();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ProgressHub>("/progress");

                endpoints.MapControllerRoute(
                    name: "Error",
                    pattern: "error",
                    defaults: new {controller = "Home", action = "Error"}
                );

                endpoints.MapControllerRoute(
                    name: "OwnerProfile",
                    pattern: "dashboard/profile/{ownerId}",
                    defaults: new {controller = "Owner", action = "Index"},
                    constraints: new {ownerId = new OwnerIdConstraint()});

                endpoints.MapControllerRoute(
                    name: "OwnerRepos",
                    pattern: "dashboard/repositories/{ownerId}",
                    defaults: new {controller = "Owner", action = "Repositories"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerDatasets",
                    pattern: "dashboard/datasets/{ownerId}",
                    defaults: new {controller = "Owner", action = "Datasets"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerJobs",
                    pattern: "dashboard/jobs/{ownerId}",
                    defaults: new {controller = "Owner", action = "Jobs"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerLibrary",
                    pattern: "dashboard/library/{ownerId}",
                    defaults: new {controller = "Owner", action = "Library"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerDeleteSchema",
                    pattern: "dashboard/library/{ownerId}/{schemaId}/delete",
                    defaults: new {controller = "Owner", action = "DeleteSchema"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerUseSchema",
                    pattern: "dashboard/library/{ownerId}/{schemaId}/import",
                    defaults: new {controller = "Owner", action = "UseSchema"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerImport",
                    pattern: "dashboard/import/{ownerId}",
                    defaults: new {controller = "Owner", action = "Import"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerSettings",
                    pattern: "dashboard/settings/{ownerId}",
                    defaults: new {controller = "Owner", action = "Settings"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerAccount",
                    pattern: "dashboard/account/{ownerId}",
                    defaults: new {controller = "Owner", action = "Account"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerAccountReset",
                    pattern: "dashboard/account/{ownerId}/reset",
                    defaults: new {controller = "Owner", action = "ResetToken"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "OwnerAccountDelete",
                    pattern: "dashboard/account/{ownerId}/delete",
                    defaults: new {controller = "Owner", action = "DeleteAccount"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );


                // {ownerId}/{repoId}

                endpoints.MapControllerRoute(
                    name: "RepoSummary",
                    pattern: "dashboard/repositories/{ownerId}/{repoId}",
                    defaults: new {controller = "Repository", action = "Index"},
                    constraints: new {ownerId = new OwnerIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "RepoDatasets",
                    pattern: "dashboard/datasets/{ownerId}/{repoId}",
                    defaults: new {controller = "Repository", action = "Datasets"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "RepoJobs",
                    pattern: "dashboard/jobs/{ownerId}/{repoId}/{jobId?}",
                    defaults: new {controller = "Repository", action = "Jobs"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "JobLog",
                    pattern: "dashboard/logs/{ownerId}/{repoId}/{jobId}",
                    defaults: new {controller = "Repository", action = "Job"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "RepoLibrary",
                    pattern: "dashboard/library/{ownerId}/{repoId}",
                    defaults: new {controller = "Repository", action = "Library"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "RepoImport",
                    pattern: "dashboard/import/{ownerId}/{repoId}/{schemaId?}",
                    defaults: new {controller = "Repository", action = "Import"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "RepoSettings",
                    pattern: "dashboard/settings/{ownerId}/{repoId}",
                    defaults: new {controller = "Repository", action = "Settings"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "Dataset",
                    pattern: "dashboard/datasets/{ownerId}/{repoId}/{datasetId}",
                    defaults: new {controller = "Dataset", action = "Index"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "DatasetVisibility",
                    pattern: "dashboard/datasets/{ownerId}/{repoId}/{datasetId}/visibilty/{showOrHide}",
                    defaults: new {controller = "Dataset", action = "DatasetVisibility"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );
                endpoints.MapControllerRoute(
                    name: "DeleteDataset",
                    pattern: "dashboard/datasets/{ownerId}/{repoId}/{datasetId}/delete",
                    defaults: new {controller = "Dataset", action = "DeleteDataset"},
                    constraints: new {ownerId = new OwnerIdConstraint(), repoId = new RepoIdConstraint()}
                );

                // Loader
                endpoints.MapControllerRoute(
                    "JobsLoader",
                    "dashboard/loader/jobs",
                    new {controller = "Loader", action = "Jobs"}
                );
                endpoints.MapControllerRoute(
                    "DatasetsLoader",
                    "dashboard/loader/datasets",
                    new {controller = "Loader", action = "Datasets"}
                );

                // account
                endpoints.MapControllerRoute(
                    name: "SignUp",
                    pattern: "account/signup",
                    defaults: new {controller = "Account", action = "SignUp"});

                // Search
                endpoints.MapControllerRoute(
                    "Search",
                    "search",
                    new {controller = "Search", action = "Index"});

                // Info
                endpoints.MapControllerRoute(
                    name: "Info",
                    pattern: "info/{action}",
                    defaults: new {controller = "Info"});

                // Linked Data routing

                endpoints.MapControllerRoute(
                    name: "LinkedDataPortal",
                    pattern: "{ownerId}",
                    defaults: new {controller = "LinkedData", action = "Owner"});

                endpoints.MapControllerRoute(
                    name: "LinkedDataRepo",
                    pattern: "{ownerId}/{repoId}",
                    defaults: new {controller = "LinkedData", action = "Repository"});

                endpoints.MapControllerRoute(
                    name: "LinkedDataPage",
                    pattern: "{ownerId}/{repoId}/page/{*path}",
                    defaults: new {controller = "LinkedData", action = "Page"});

                endpoints.MapControllerRoute(
                    name: "LinkedDataData",
                    pattern: "{ownerId}/{repoId}/data/{*path}",
                    defaults: new {controller = "LinkedData", action = "Data"});

                endpoints.MapControllerRoute(
                    name: "LinkedDataId",
                    pattern: "{ownerId}/{repoId}/id/{*path}",
                    defaults: new {controller = "LinkedData", action = "Identifier"});

                endpoints.MapControllerRoute(
                    name: "LinkedDataCsv",
                    pattern: "{ownerId}/{repoId}/csv/{datasetId}/{filename}.csv",
                    defaults: new {controller = "LinkedData", action = "Csv"});

                endpoints.MapControllerRoute(
                    name: "LinkedDataCsvMetadata",
                    pattern: "{ownerId}/{repoId}/csv/{datasetId}/{filename}.json",
                    defaults: new {controller = "LinkedData", action = "CsvMetadata"});

                // api
                endpoints.MapControllerRoute(
                    name: "Api",
                    pattern: "api/{action}/{id?}",
                    defaults: new {controller = "Api"});

                // default
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }

}
