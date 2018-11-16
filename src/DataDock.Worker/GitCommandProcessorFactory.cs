using System;
using System.Collections.Generic;
using System.Text;
using DataDock.Common;
using DataDock.Worker.Processors;
using NetworkedPlanet.Quince.Git;

namespace DataDock.Worker
{
    public class GitCommandProcessorFactory : IGitCommandProcessorFactory
    {
        private readonly WorkerConfiguration _config;
        private readonly IGitHubClientFactory _clientFactory;
        private readonly IGitWrapperFactory _wrapperFactory;

        public GitCommandProcessorFactory(IGitHubClientFactory clientFactory, IGitWrapperFactory wrapperFactory,
            WorkerConfiguration config)
        {
            _clientFactory = clientFactory;
            _wrapperFactory = wrapperFactory;
            _config = config;
        }

        public GitCommandProcessor MakeGitCommandProcessor(IProgressLog progressLog)
        {
            return new GitCommandProcessor(_config, progressLog, _clientFactory, _wrapperFactory);
        }
    }
}
