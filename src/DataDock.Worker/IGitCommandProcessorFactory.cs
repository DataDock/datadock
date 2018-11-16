using DataDock.Worker.Processors;

namespace DataDock.Worker
{
    public interface IGitCommandProcessorFactory
    {
        GitCommandProcessor MakeGitCommandProcessor(IProgressLog progressLog);
    }
}
