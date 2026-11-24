namespace Library.Application.Maintenance;

public interface IMaintenanceService
{
    Task<MaintenanceResult> RunAsync(CancellationToken cancellationToken = default);
}
