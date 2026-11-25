using Library.Application.Maintenance;
using Quartz;

namespace Library.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class MaintenanceJob : IJob
{
    private readonly IMaintenanceService _maintenance;

    public MaintenanceJob(IMaintenanceService maintenance)
    {
        _maintenance = maintenance;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        await _maintenance.RunAsync(cancellationToken);
    }
}
