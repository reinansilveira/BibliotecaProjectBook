using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Library.Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionProvider(IOptions<RabbitMqSettings> options)
    {
        var settings = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            ClientProvidedName = "library-api"
        };
    }

    public bool IsConnected => _connection is { IsOpen: true };

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection = await _factory.CreateConnectionAsync(cancellationToken);

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lock.Dispose();

        if (_connection is null)
        {
            return;
        }

        await _connection.DisposeAsync();
    }
}
