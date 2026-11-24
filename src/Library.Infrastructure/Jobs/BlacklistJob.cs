using Library.Application.Blacklist;
using Quartz;

namespace Library.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class BlacklistJob : IJob
{
    private readonly IBlacklistService _blacklistService;

    public BlacklistJob(IBlacklistService blacklistService)
    {
        _blacklistService = blacklistService;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        await _blacklistService.RefreshAsync(cancellationToken);
    }
}
