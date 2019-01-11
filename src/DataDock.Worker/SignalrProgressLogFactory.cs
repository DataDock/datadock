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
    public class SignalRProgressLogFactory : IProgressLogFactory
    {
        private readonly string _hubConnectionUrl;
        private readonly IJobStore _jobRepository;
        private HubConnection _hubConnection;

        public SignalRProgressLogFactory(WorkerConfiguration configuration, IJobStore jobRepository)
        {
            _hubConnectionUrl = configuration.SignalRHubUrl;
            _jobRepository = jobRepository;
        }

        public async Task<IProgressLog> MakeProgressLogForJobAsync(JobInfo job)
        {
            await EnsureHubConnectionAsync();
            return new SignalRProgressLog(job, _jobRepository, _hubConnection);
        }

        private async Task<HubConnection> EnsureHubConnectionAsync()
        {
            if (_hubConnection != null) return _hubConnection;
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubConnectionUrl, HttpTransportType.WebSockets)
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
                    Log.Error(ex, "SignalR connection failed");
                    Console.WriteLine("Error connecting to SignalR Hub: " + ex);
                    Thread.Sleep(1000);
                }
            }
            return _hubConnection;
        }

        private async Task OnHubConnectionLost(Exception exc)
        {
            await Task.Run(() =>
            {
                Log.Warning(exc, "SignalR hub connection was lost.");
                _hubConnection = null;
            });
        }
    }
}
