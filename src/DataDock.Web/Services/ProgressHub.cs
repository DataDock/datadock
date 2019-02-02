using System;
using DataDock.Common.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Serilog;

namespace DataDock.Web.Services
{
    public class ProgressHub : Hub
    {
        /// <summary>
        /// Broadcast a job progress update to all subscribed clients
        /// </summary>
        /// <param name="ownerId">The ID of the owner of the repository where the job is running</param>
        /// <param name="jobId">The ID of the job</param>
        /// <param name="progressMessage">The progress message</param>
        /// <returns></returns>
        public async Task ProgressUpdated(string ownerId, string jobId, string progressMessage)
        {
            await Clients.Group(ownerId).SendAsync("progressUpdated", ownerId, jobId, progressMessage);
        }

        /// <summary>
        /// Broadcast a job status update to all subscribed clients
        /// </summary>
        /// <param name="ownerId">The ID of the owner of the repository where the job is running</param>
        /// <param name="jobId">The ID of the job</param>
        /// <param name="jobStatus">The new status of the job</param>
        /// <returns></returns>
        public async Task StatusUpdated(string ownerId, string jobId, JobStatus jobStatus)
        {
            await Clients.Group(ownerId).SendAsync("statusUpdated", ownerId, jobId, jobStatus);
        }

        /// <summary>
        /// Broadcast a message to all subscribed clients
        /// </summary>
        /// <param name="ownerId">The ID of the owner of the repository/repositories affected by this message</param>
        /// <param name="message">The message content</param>
        /// <returns></returns>
        public async Task SendMessage(string ownerId, string message)
        {
            await Clients.Group(ownerId).SendAsync("sendMessage", ownerId, message);
        }

        /// <summary>
        /// Broadcast a notification of an update to a dataset or creation of a new dataset to all subscribed clients
        /// </summary>
        /// <param name="ownerId">The ID of the owner of the repository where the dataset is located. This is used as the ID of the group that will receive the broadcast</param>
        /// <param name="datasetInfo">The metadata for the updated dataset</param>
        /// <returns></returns>
        public async Task DatasetUpdated(string ownerId, DatasetInfo datasetInfo)
        {
            await Clients.Group(ownerId).SendAsync("datasetUpdated", ownerId, datasetInfo);
        }

        /// <summary>
        /// Broadcast a notification of a deletion of a dataset to all subscribed clients
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <param name="datasetId"></param>
        /// <returns></returns>
        public async Task DatasetDeleted(string ownerId, string repoId, string datasetId)
        {
            await Clients.Group(ownerId).SendAsync("datasetDeleted", ownerId, repoId, datasetId);
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                // By default we always subscribe to the group for the logged in user
                if (Context.User?.Identity?.Name != null)
                {
                    await Subscribe(Context.User.Identity.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ProgressHub.OnConnectedAsync");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Subscribe the client connection to the specified group
        /// </summary>
        /// <remarks>This method can be used from the client to subscribe to the group providing progress messages for an organization</remarks>
        /// <param name="groupId">The name of the organization whose messages we want to subscribe to</param>
        /// <returns></returns>
        public async Task Subscribe(string groupId)
        {
            try
            {
                if (groupId == null)
                {
                    Log.Warning("ProgressHub.Subscribe received a null groupId. Request was ignored");
                    return;
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
                Log.Information("ProgressHub subscribed connection {ConnectionId} to group {GroupId}", Context.ConnectionId, groupId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ProgressHub.Subscribe");
            }
        }
    }
}
