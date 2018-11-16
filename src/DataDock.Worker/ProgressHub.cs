using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace DataDock.Worker
{
    public class ProgressHub : Hub
    {
        public async Task ProgressUpdated(string userId, string jobId, string progressMessage)
        {
            await Clients.All.SendAsync("progressUpdated", userId, jobId, progressMessage);
        }
    }

}
