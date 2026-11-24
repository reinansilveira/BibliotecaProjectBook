using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Library.Infrastructure.Messaging;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqConnectionProvider _connectionProvider;

    public RabbitMqHealthCheck(RabbitMqConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);

            return connection.IsOpen
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The RabbitMQ connection is closed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Could not connect to RabbitMQ.", exception);
        }
    }
}
