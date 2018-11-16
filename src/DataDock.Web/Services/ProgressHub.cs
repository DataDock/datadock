using System;
using DataDock.Common.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DataDock.Web.Services
{
    public class ProgressHub : Hub
    {
        public async Task ProgressUpdated(string userId, string jobId, string progressMessage)
        {
            await Clients.Group(userId).SendAsync("progressUpdated", userId, jobId, progressMessage);
        }

        public async Task StatusUpdated(string userId, string jobId, JobStatus jobStatus)
        {
            await Clients.Group(userId).SendAsync("statusUpdated", userId, jobId, jobStatus);
        }

        public async Task SendMessage(string userId, string message)
        {
            await Clients.Group(userId).SendAsync("sendMessage", userId, message);
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var name = Context.User.Identity.Name;
                await Groups.AddToGroupAsync(Context.ConnectionId, name);
            }
            catch (Exception)
            {
                // A connection from the worker role will not have a user identity
            }

            await base.OnConnectedAsync();
        }
    }
}
