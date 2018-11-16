using System;
using System.Threading;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace DataDock.Worker
{
    public class SignalrProgressLogFactory : IProgressLogFactory
    {
        private readonly IJobStore _jobRepository;
        private HubConnection _hubConnection;

        public SignalrProgressLogFactory(IJobStore jobRepository)
        {
            _jobRepository = jobRepository;
        }

        public void SetHubConnection(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
        }

        public async Task<IProgressLog> MakeProgressLogForJobAsync(JobInfo job)
        {
            await EnsureHubConnectionAsync();
            return new SignalrProgressLog(job, _jobRepository, _hubConnection);
        }

        private async Task<HubConnection> EnsureHubConnectionAsync()
        {
            if (_hubConnection != null) return _hubConnection;
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://datadock.web/progress", HttpTransportType.WebSockets)
                .Build();
            _hubConnection.Closed += OnHubConnectionLost;
            var connectionStarted = false;
            while (!connectionStarted)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    connectionStarted = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "signalr connection failed");
                    Console.WriteLine("Error connecting to Signalr Hub: " + ex);
                    Thread.Sleep(1000);
                }
            }
            return _hubConnection;
        }

        private async Task OnHubConnectionLost(Exception exc)
        {
            Log.Warning(exc, "SignalR hub connection was lost.");
            _hubConnection = null;
        }
    }
}
